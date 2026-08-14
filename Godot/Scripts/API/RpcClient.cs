using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GridironGM.Client.Api
{
    public partial class RpcClient : Node, IBackendClient
    {
        private readonly object _pendingLock = new();
        private readonly object _writeLock = new();
        private readonly Dictionary<long, TaskCompletionSource<(int status, string body)>> _pending = new();
        private long _nextId = 0;
        private Process _process;
        private StreamWriter _stdin;
        private StreamReader _stdout;
        private Task _readTask;
        private string _attachError = "RPC backend not attached or already exited.";

        public Task<(int status, string body)> GetAsync(string path)
            => RequestAsync("GET", path, null);

        public Task<(int status, string body)> PostAsync(string path, string jsonBody = "{}")
            => RequestAsync("POST", path, jsonBody);

        public void AttachProcess(Process process)
        {
            if (process == null)
            {
                SetAttachError("RPC backend not attached (process is null).");
                return;
            }

            if (process.HasExited)
            {
                SetAttachError($"RPC backend already exited (code {process.ExitCode}).");
                _process = process;
                return;
            }

            _process = process;
            try
            {
                _stdin = process.StandardInput;
                _stdin.AutoFlush = true;
                _stdout = process.StandardOutput;
            }
            catch (Exception ex)
            {
                SetAttachError($"RPC client failed to attach streams: {ex.Message}");
                return;
            }

            _attachError = "";
            if (_readTask == null || _readTask.IsCompleted)
                _readTask = Task.Run(ReadLoopAsync);
        }

        private Task<(int status, string body)> RequestAsync(string op, string path, string jsonBody)
        {
            if (_process == null || _stdin == null || _stdout == null)
                return Task.FromResult(BuildUnavailableResult());
            if (_process.HasExited)
            {
                SetAttachError($"RPC backend already exited (code {_process.ExitCode}).");
                return Task.FromResult(BuildUnavailableResult());
            }

            var requestId = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<(int, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_pendingLock)
            {
                _pending[requestId] = tcs;
            }

            if (!TryBuildRequestLine(requestId, op, path, jsonBody, out var line, out var error))
            {
                lock (_pendingLock)
                {
                    _pending.Remove(requestId);
                }

                tcs.TrySetResult((0, error));
                return tcs.Task;
            }

            try
            {
                lock (_writeLock)
                {
                    _stdin.WriteLine(line);
                    _stdin.Flush();
                }
            }
            catch (Exception ex)
            {
                lock (_pendingLock)
                {
                    _pending.Remove(requestId);
                }

                tcs.TrySetResult((0, $"RPC write failed: {ex.Message}"));
            }

            return tcs.Task;
        }

        private (int status, string body) BuildUnavailableResult()
        {
            var message = string.IsNullOrWhiteSpace(_attachError)
                ? "RPC backend not attached or already exited."
                : _attachError;
            return (503, message);
        }

        private void SetAttachError(string message)
        {
            _attachError = string.IsNullOrWhiteSpace(message)
                ? "RPC backend not attached or already exited."
                : message;
        }

        private bool TryBuildRequestLine(
            long id,
            string op,
            string path,
            string jsonBody,
            out string line,
            out string error)
        {
            line = "";
            error = "";

            var args = new Dictionary<string, object>
            {
                ["path"] = path ?? ""
            };

            if (string.Equals(op, "POST", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseJsonBody(jsonBody, out var jsonValue, out error))
                    return false;

                args["json"] = jsonValue;
            }

            var request = new Dictionary<string, object>
            {
                ["id"] = id,
                ["op"] = op,
                ["args"] = args
            };

            try
            {
                line = JsonSerializer.Serialize(request);
                return true;
            }
            catch (Exception ex)
            {
                error = $"RPC serialize failed: {ex.Message}";
                return false;
            }
        }

        private bool TryParseJsonBody(string jsonBody, out object jsonValue, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(jsonBody))
            {
                jsonValue = new Dictionary<string, object>();
                return true;
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonBody);
                jsonValue = doc.RootElement.Clone();
                return true;
            }
            catch (Exception ex)
            {
                jsonValue = new Dictionary<string, object>();
                error = $"Invalid JSON body: {ex.Message}";
                return false;
            }
        }

        private async Task ReadLoopAsync()
        {
            while (true)
            {
                string line;
                try
                {
                    line = await _stdout.ReadLineAsync();
                }
                catch (Exception ex)
                {
                    FailAllPending($"RPC read failed: {ex.Message}");
                    return;
                }

                if (line == null)
                {
                    FailAllPending("RPC backend closed.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    HandleResponseLine(line);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"RPC response parse failed: {ex.Message}");
                }
            }
        }

        private void HandleResponseLine(string line)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (!root.TryGetProperty("id", out var idProp))
                return;

            long id;
            if (idProp.ValueKind == JsonValueKind.Number)
                id = idProp.GetInt64();
            else if (idProp.ValueKind == JsonValueKind.String && long.TryParse(idProp.GetString(), out var parsed))
                id = parsed;
            else
                return;

            var status = 0;
            if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
                status = statusProp.GetInt32();

            var body = "";
            if (root.TryGetProperty("body", out var bodyProp))
            {
                if (bodyProp.ValueKind == JsonValueKind.String)
                    body = bodyProp.GetString() ?? "";
                else
                    body = bodyProp.GetRawText();
            }

            TaskCompletionSource<(int status, string body)> tcs = null;
            lock (_pendingLock)
            {
                if (_pending.TryGetValue(id, out tcs))
                    _pending.Remove(id);
            }

            tcs?.TrySetResult((status, body));
        }

        private void FailAllPending(string message)
        {
            List<TaskCompletionSource<(int status, string body)>> pending;
            lock (_pendingLock)
            {
                pending = new List<TaskCompletionSource<(int, string)>>(_pending.Values);
                _pending.Clear();
            }

            SetAttachError(message);
            foreach (var tcs in pending)
                tcs.TrySetResult((0, message));
        }
    }
}
