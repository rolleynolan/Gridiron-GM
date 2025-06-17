using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

public class MainMenuController : MonoBehaviour
{
    [Header("GM Settings")]
    public Dropdown gmDropdown;
    public InputField gmNameInput;
    public Button createGmButton;

    [Header("League Settings")]
    public Dropdown teamDropdown;
    public Text weekText;
    public Text resultsText;
    public Button simulateButton;

    private LeagueState leagueState;
    private string leagueStatePath;

    void Start()
    {
        leagueStatePath = Path.Combine(Application.dataPath, "..", "league_state.json");
        LoadLeagueState();
        PopulateTeamDropdown();
        PopulateGmDropdown();
        UpdateUI();

        if (createGmButton != null)
            createGmButton.onClick.AddListener(CreateGm);
        if (simulateButton != null)
            simulateButton.onClick.AddListener(SimulateWeek);
    }

    void LoadLeagueState()
    {
        if (!File.Exists(leagueStatePath))
        {
            UnityEngine.Debug.LogError("league_state.json not found at " + leagueStatePath);
            return;
        }
        string json = File.ReadAllText(leagueStatePath);
        leagueState = JsonConvert.DeserializeObject<LeagueState>(json);
    }

    void PopulateTeamDropdown()
    {
        if (teamDropdown == null || leagueState == null) return;
        teamDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var team in leagueState.teams)
        {
            options.Add($"{team.abbreviation} - {team.name}");
        }
        teamDropdown.AddOptions(options);
    }

    void PopulateGmDropdown()
    {
        if (gmDropdown == null) return;
        gmDropdown.ClearOptions();
        var gmDir = Path.Combine(Application.dataPath, "..", "gms");
        var options = new List<string>();
        if (Directory.Exists(gmDir))
        {
            foreach (var f in Directory.GetFiles(gmDir, "*.json"))
            {
                options.Add(Path.GetFileNameWithoutExtension(f));
            }
        }
        gmDropdown.AddOptions(options);
    }

    void UpdateUI()
    {
        if (leagueState == null) return;
        if (weekText != null)
            weekText.text = "Week " + leagueState.week;
        if (resultsText != null)
        {
            if (leagueState.results_by_week != null && leagueState.results_by_week.TryGetValue(leagueState.week.ToString(), out var results))
            {
                var lines = new List<string>();
                foreach (var res in results)
                {
                    lines.Add($"{res.away} {res.away_score} @ {res.home} {res.home_score}");
                }
                resultsText.text = string.Join("\n", lines);
            }
            else
            {
                resultsText.text = "No results yet.";
            }
        }
    }

    void CreateGm()
    {
        if (gmNameInput == null) return;
        var name = gmNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var gmDir = Path.Combine(Application.dataPath, "..", "gms");
        Directory.CreateDirectory(gmDir);
        var path = Path.Combine(gmDir, name + ".json");
        if (!File.Exists(path))
            File.WriteAllText(path, "{}");

        PopulateGmDropdown();
        int index = gmDropdown.options.FindIndex(o => o.text == name);
        if (index >= 0)
            gmDropdown.value = index;
    }

    void SimulateWeek()
    {
        var process = new Process();
        process.StartInfo.FileName = "python";
        process.StartInfo.Arguments = "scripts/run_weekly_simulation.py";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();

        UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
        UnityEngine.Debug.LogError(process.StandardError.ReadToEnd());

        LoadLeagueState();
        UpdateUI();
    }
}
