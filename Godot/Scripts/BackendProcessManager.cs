using Godot;
using System;
using SysEnv = System.Environment;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GridironGM.Client.Api;

public partial class BackendProcessManager : Node
{
    [Export] public string BackendCommand = "python";
    [Export] public string BackendArguments = "-m gridiron_gm_pkg.api.server --host 127.0.0.1 --port 8765 --save-path savegame.json";
    [Export] public string BackendWorkingDirectory = "";
    [Export] public bool AutoShutdownBackend = true;

    private const string BackendRootEnvVar = "GRIDIRON_GM_BACKEND_ROOT";
    private const string BackendRootSettingPath = "backend/root_path";
    private const string GodotProjectFileName = "project.godot";

    private IBackendClient _api;
    private Action<string> _reportStateDump;
    private Process _process;
    private bool _ownsProcess;
    private StreamWriter _logWriter;
    private readonly object _logLock = new();
    private bool _logWriterReady;
    private readonly object _stopLock = new();
    private bool _stopInProgress;
    private string _lastLaunchArguments = "";
    private bool _useRpc;

    public void Initialize(IBackendClient api, Action<string> reportStateDump)
    {
        _api = api;
        _reportStateDump = reportStateDump;
        _useRpc = GetUseRpcSetting();
    }

    public async Task<bool> EnsureBackendAsync()
    {
        if (_api == null)
        {
            ReportError("Backend manager is not initialized with a backend client.");
            return false;
        }

        var (status, body) = await _api.GetAsync("/health");
        if (IsSuccess(status))
            return true;

        if (!StartBackendProcess(out var startError))
        {
            ReportError(BuildStartFailureMessage(startError));
            return false;
        }

        const int pollIntervalMs = 250;
        const int maxWaitMs = 10000;
        var lastStatus = status;
        var lastBody = body;

        for (var elapsedMs = 0; elapsedMs <= maxWaitMs; elapsedMs += pollIntervalMs)
        {
            if (elapsedMs > 0)
                await Task.Delay(pollIntervalMs);

            if (_process != null && _ownsProcess && _process.HasExited)
            {
                ReportError(BuildStartFailureMessage($"Process exited early (code {_process.ExitCode})."));
                return false;
            }

            (lastStatus, lastBody) = await _api.GetAsync("/health");
            if (IsSuccess(lastStatus))
                return true;
        }

        ReportError(BuildHealthTimeoutMessage(lastStatus, lastBody));
        return false;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            StopBackend();
    }

    public override void _ExitTree()
        => StopBackend();

    private static bool IsSuccess(int status)
        => status >= 200 && status < 300;

    private bool StartBackendProcess(out string error)
    {
        error = "";
        if (_process != null)
        {
            if (!_process.HasExited)
                return true;
            CleanupProcessLogging();
            _process.Dispose();
            _process = null;
            _ownsProcess = false;
        }

        if (!TryResolveWorkingDirectory(out var workingDir, out var workingDirError))
        {
            error = workingDirError;
            AppendLogLine(workingDirError);
            return false;
        }

        // Acceptance criteria: If Godot is force-killed, backend exits within ~1–2 seconds due to watchdog.
        // Normal close still shuts backend down.
        _useRpc = GetUseRpcSetting();
        var arguments = _useRpc
            ? "-m gridiron_gm_pkg.api.rpc_server --save-path savegame.json"
            : (BackendArguments ?? "");
        if (!arguments.Contains("--parent-pid", StringComparison.OrdinalIgnoreCase))
        {
            var parentPid = Process.GetCurrentProcess().Id;
            arguments = $"{arguments} --parent-pid {parentPid}".Trim();
        }
        _lastLaunchArguments = arguments;

        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveBackendCommand(),
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = _useRpc,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            LogDiagnostic($"use_rpc={_useRpc.ToString().ToLowerInvariant()}");
            LogDiagnostic($"command={startInfo.FileName} {startInfo.Arguments}".Trim());
            LogDiagnostic($"working_dir={workingDir}");
            var logPath = GetBackendLogPath();
            GD.Print($"Backend startup: workingDir={workingDir} | log={logPath}");
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                error = "Process.Start returned null.";
                return false;
            }

            _ownsProcess = true;
            if (_useRpc)
            {
                _api?.AttachProcess(_process);
                LogDiagnostic("attached rpc process");
            }
            LogDiagnostic($"process_started pid={_process.Id} exited={_process.HasExited.ToString().ToLowerInvariant()}");
            SetupProcessLogging(_process, captureStdout: !_useRpc);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void StopBackend()
    {
        if (AutoShutdownBackend)
            _ = RequestShutdownAsync();

        Process process = null;
        lock (_stopLock)
        {
            if (_stopInProgress)
                return;
            _stopInProgress = true;

            if (_ownsProcess && _process != null)
            {
                process = _process;
                _process = null;
                _ownsProcess = false;
            }
        }

        if (process == null)
        {
            lock (_stopLock)
                _stopInProgress = false;
            return;
        }

        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to stop backend process: {ex.Message}");
        }
        finally
        {
            CleanupProcessLogging(process);
            try
            {
                process.Dispose();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to dispose backend process: {ex.Message}");
            }
            lock (_stopLock)
                _stopInProgress = false;
        }
    }

    private async Task RequestShutdownAsync()
    {
        if (_api == null)
            return;

        try
        {
            var (status, body) = await _api.PostAsync("/shutdown");
            if (status < 200 || status >= 300)
                GD.PrintErr($"Backend shutdown request failed: {body}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Backend shutdown request failed: {ex.Message}");
        }
    }

    private void SetupProcessLogging(Process process, bool captureStdout)
    {
        _logWriterReady = TryOpenLogWriter(out var logError);
        if (!_logWriterReady && !string.IsNullOrWhiteSpace(logError))
            ReportError($"Failed to open backend log: {logError}");

        if (captureStdout)
            process.OutputDataReceived += HandleOutputDataReceived;
        process.ErrorDataReceived += HandleErrorDataReceived;
        process.EnableRaisingEvents = true;
        process.Exited += HandleProcessExited;

        if (captureStdout)
        {
            try
            {
                process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                ReportError($"Failed to capture backend stdout: {ex.Message}");
            }
        }

        try
        {
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            ReportError($"Failed to capture backend stderr: {ex.Message}");
        }
    }

    private bool TryOpenLogWriter(out string error)
    {
        error = "";
        try
        {
            if (!TryEnsureLogFile(out var logPath, out error))
                return false;
            _logWriter = new StreamWriter(logPath, append: true)
            {
                AutoFlush = true
            };
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            GD.PrintErr($"Backend logging failed: {ex.Message}");
            _logWriter = null;
            return false;
        }
    }

    private void CleanupProcessLogging()
        => CleanupProcessLogging(_process);

    private void CleanupProcessLogging(Process process)
    {
        if (process != null)
        {
            try
            {
                process.OutputDataReceived -= HandleOutputDataReceived;
                process.ErrorDataReceived -= HandleErrorDataReceived;
                process.Exited -= HandleProcessExited;
                process.CancelOutputRead();
                process.CancelErrorRead();
            }
            catch (InvalidOperationException)
            {
                // Process already exited or not started reading; ignore.
            }
        }

        lock (_logLock)
        {
            _logWriterReady = false;
            _logWriter?.Dispose();
            _logWriter = null;
        }
    }

    private void HandleOutputDataReceived(object sender, DataReceivedEventArgs e)
        => WriteLogLine(e.Data);

    private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        => WriteLogLine(e.Data);

    private void HandleProcessExited(object sender, EventArgs e)
        => CleanupProcessLogging(sender as Process);

    private void AppendLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (_logWriterReady)
        {
            WriteLogLine(line);
            return;
        }

        try
        {
            if (!TryEnsureLogFile(out var logPath, out _))
                return;
            File.AppendAllText(logPath, line + SysEnv.NewLine);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Backend logging failed: {ex.Message}");
        }
    }

    private void WriteLogLine(string line)
    {
        if (!_logWriterReady || line == null)
            return;

        lock (_logLock)
        {
            if (!_logWriterReady || _logWriter == null)
                return;

            try
            {
                _logWriter.WriteLine(line);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Backend logging failed: {ex.Message}");
                _logWriterReady = false;
                _logWriter?.Dispose();
                _logWriter = null;
            }
        }
    }

    private bool TryEnsureLogFile(out string logPath, out string error)
    {
        error = "";
        logPath = GetBackendLogPath();
        try
        {
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(logDir))
                Directory.CreateDirectory(logDir);
            if (!File.Exists(logPath))
                File.WriteAllText(logPath, "");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            GD.PrintErr($"Backend logging failed: {ex.Message}");
            return false;
        }
    }

    private void LogDiagnostic(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        GD.Print(line);
        AppendLogLine(line);
    }

    private bool TryResolveWorkingDirectory(out string workingDirectory, out string error)
    {
        var godotRoot = GetGodotRoot();
        var repoRoot = ResolveRepoRoot(godotRoot);
        var hasPackageDir = HasBackendPackageDir(repoRoot);

        GD.Print($"godotRoot={godotRoot}");
        GD.Print($"repoRoot={repoRoot}");
        GD.Print($"has_gridiron_gm_pkg={hasPackageDir.ToString().ToLowerInvariant()}");

        if (TryResolveBackendRootFromEnvironment(godotRoot, out workingDirectory, out error))
            return true;

        if (!string.IsNullOrWhiteSpace(error))
            return false;

        if (TryResolveBackendRootFromProjectSetting(godotRoot, out workingDirectory, out error))
            return true;

        if (!string.IsNullOrWhiteSpace(error))
            return false;

        if (hasPackageDir)
        {
            workingDirectory = repoRoot;
            error = "";
            return true;
        }

        workingDirectory = "";
        error = BuildRepoRootNotFoundMessage(repoRoot, godotRoot);
        return false;
    }

    private string DescribeWorkingDirectory()
    {
        var godotRoot = GetGodotRoot();
        var repoRoot = ResolveRepoRoot(godotRoot);
        var hasPackageDir = HasBackendPackageDir(repoRoot);

        var envPath = DescribeCandidatePath(SysEnv.GetEnvironmentVariable(BackendRootEnvVar), godotRoot);
        if (!string.IsNullOrWhiteSpace(envPath))
            return envPath;

        var settingPath = DescribeCandidatePath(GetProjectSettingPath(BackendRootSettingPath), godotRoot);
        if (!string.IsNullOrWhiteSpace(settingPath))
            return settingPath;

        if (hasPackageDir)
            return repoRoot;

        return string.IsNullOrWhiteSpace(repoRoot)
            ? $"(not found from {godotRoot})"
            : repoRoot;
    }

    private string ResolveBackendCommand()
        => string.IsNullOrWhiteSpace(BackendCommand) ? "python" : BackendCommand;

    private bool TryResolveBackendRootFromEnvironment(string basePath, out string backendRoot, out string error)
    {
        var envRoot = SysEnv.GetEnvironmentVariable(BackendRootEnvVar);
        if (envRoot == null)
        {
            backendRoot = "";
            error = "";
            return false;
        }

        backendRoot = NormalizeCandidatePath(envRoot, basePath);
        if (IsValidBackendRoot(backendRoot))
        {
            error = "";
            return true;
        }

        backendRoot = "";
        error = BuildBackendRootNotFoundMessage();
        return false;
    }

    private bool TryResolveBackendRootFromProjectSetting(string basePath, out string backendRoot, out string error)
    {
        if (!ProjectSettings.HasSetting(BackendRootSettingPath))
        {
            backendRoot = "";
            error = "";
            return false;
        }

        var settingPath = GetProjectSettingPath(BackendRootSettingPath);
        backendRoot = NormalizeCandidatePath(settingPath, basePath);
        if (IsValidBackendRoot(backendRoot))
        {
            error = "";
            return true;
        }

        backendRoot = "";
        error = BuildBackendRootNotFoundMessage();
        return false;
    }

    private static string GetProjectSettingPath(string settingPath)
    {
        if (!ProjectSettings.HasSetting(settingPath))
            return "";

        var value = ProjectSettings.GetSetting(settingPath);
        if (value.VariantType == Variant.Type.Nil)
            return "";

        return value.AsString();
    }

    private static string DescribeCandidatePath(string candidate, string basePath)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return "";

        var normalized = NormalizeCandidatePath(candidate, basePath);
        return string.IsNullOrWhiteSpace(normalized) ? candidate : normalized;
    }

    private static string NormalizeCandidatePath(string candidate, string basePath)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return "";

        var expanded = SysEnv.ExpandEnvironmentVariables(candidate.Trim());

        try
        {
            if (expanded.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(ProjectSettings.GlobalizePath(expanded));

            if (!Path.IsPathRooted(expanded))
            {
                if (string.IsNullOrWhiteSpace(basePath))
                    return Path.GetFullPath(expanded);

                return Path.GetFullPath(Path.Combine(basePath, expanded));
            }

            return Path.GetFullPath(expanded);
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static string FindBackendRoot(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
            return "";

        DirectoryInfo searchStart;
        try
        {
            searchStart = new DirectoryInfo(startPath);
        }
        catch (Exception)
        {
            return "";
        }

        if (!searchStart.Exists)
            searchStart = searchStart.Parent;

        var projectDir = FindGodotProjectDirectory(searchStart?.FullName ?? "");
        if (!string.IsNullOrWhiteSpace(projectDir) && IsValidBackendRoot(projectDir))
            return projectDir;

        if (!string.IsNullOrWhiteSpace(projectDir))
        {
            var projectParent = Directory.GetParent(projectDir);
            if (projectParent != null)
            {
                foreach (var sibling in projectParent.EnumerateDirectories())
                {
                    if (string.Equals(sibling.FullName, projectDir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (IsValidBackendRoot(sibling.FullName))
                        return sibling.FullName;
                }
            }
        }

        var current = searchStart;
        while (current != null)
        {
            if (IsValidBackendRoot(current.FullName))
                return current.FullName;

            current = current.Parent;
        }

        return "";
    }

    private static string FindGodotProjectDirectory(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
            return "";

        DirectoryInfo current;
        try
        {
            current = new DirectoryInfo(startPath);
        }
        catch (Exception)
        {
            return "";
        }

        if (!current.Exists)
            current = current.Parent;

        while (current != null)
        {
            var projectPath = Path.Combine(current.FullName, GodotProjectFileName);
            if (File.Exists(projectPath))
                return current.FullName;

            current = current.Parent;
        }

        return "";
    }

    private static bool IsValidBackendRoot(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var packagePath = Path.Combine(candidate, "gridiron_gm_pkg");
        if (Directory.Exists(packagePath))
            return true;

        var pyprojectPath = Path.Combine(candidate, "pyproject.toml");
        var setupCfgPath = Path.Combine(candidate, "setup.cfg");
        return (File.Exists(pyprojectPath) || File.Exists(setupCfgPath)) &&
               Directory.Exists(Path.Combine(candidate, "src", "gridiron_gm_pkg"));
    }

    private static string ResolveRepoRoot(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return "";

        try
        {
            var normalized = TrimTrailingDirectorySeparators(basePath);
            var parent = Directory.GetParent(normalized);
            return parent?.FullName ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static string GetGodotRoot()
    {
        var godotRoot = ProjectSettings.GlobalizePath("res://");
        return TrimTrailingDirectorySeparators(godotRoot);
    }

    private static bool HasBackendPackageDir(string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            return false;

        return Directory.Exists(Path.Combine(repoRoot, "gridiron_gm_pkg"));
    }

    private static string TrimTrailingDirectorySeparators(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string BuildRepoRootNotFoundMessage(string repoRoot, string basePath)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            return $"Backend root not found. Unable to resolve repo root from {basePath}.";

        return $"Backend root not found at repo root: {repoRoot}. Expected gridiron_gm_pkg.";
    }

    private void ReportError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _reportStateDump?.Invoke(message);
        GD.PrintErr(message);
    }

    private string BuildStartFailureMessage(string reason)
    {
        var command = $"{ResolveBackendCommand()} {GetLaunchArguments()}".Trim();
        var workingDir = DescribeWorkingDirectory();
        if (string.IsNullOrWhiteSpace(reason))
            reason = "Unknown error.";

        return $"Backend launch failed: {reason}\nCommand: {command}\nWorking dir: {workingDir}";
    }

    private string BuildHealthTimeoutMessage(int lastStatus, string lastBody)
    {
        var command = $"{ResolveBackendCommand()} {GetLaunchArguments()}".Trim();
        var workingDir = DescribeWorkingDirectory();
        var bodySnippet = Truncate(lastBody, 400);
        return "Backend failed to become healthy within 10 seconds.\n" +
               $"Last /health status: {lastStatus}\n" +
               $"Last /health body: {bodySnippet}\n" +
               $"Command: {command}\nWorking dir: {workingDir}";
    }

    private static string BuildBackendRootNotFoundMessage()
        => $"Backend root not found. Set {BackendRootEnvVar} to the backend project folder.";

    private static string GetBackendLogPath()
        => ProjectSettings.GlobalizePath("user://logs/backend.log");

    private static bool GetUseRpcSetting()
    {
        var setting = ProjectSettings.GetSetting("backend/use_rpc", false);
        return setting.VariantType == Variant.Type.Bool ? setting.AsBool() : setting.AsBool();
    }

    private string GetLaunchArguments()
        => string.IsNullOrWhiteSpace(_lastLaunchArguments) ? (BackendArguments ?? "") : _lastLaunchArguments;

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength) + "...";
    }
}
