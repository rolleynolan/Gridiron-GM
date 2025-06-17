using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

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
    public Button simulateWeekButton;
    public TMP_Text simStatusText;

    [Header("Results UI")]
    public GameObject gameResultRowPrefab;
    public Transform resultsContent;

    private LeagueState leagueState;
    private string leagueStatePath;

    void Start()
    {
        leagueStatePath = Path.Combine(Application.dataPath, "..", "save", "league_state.json");
        LoadLeagueState();
        PopulateTeamDropdown();
        PopulateGmDropdown();
        if (leagueState != null)
            UpdateUI();
        else
        {
            if (weekText != null)
                weekText.text = "Week N/A";
            if (resultsText != null)
                resultsText.text = "No league data.";
        }

        if (createGmButton != null)
            createGmButton.onClick.AddListener(CreateGm);
        if (simulateWeekButton != null)
            simulateWeekButton.onClick.AddListener(RunSimulateWeek);
    }

    void LoadLeagueState()
    {
        if (!File.Exists(leagueStatePath))
        {
            UnityEngine.Debug.LogError("league_state.json not found at " + leagueStatePath);
            return;
        }
        string json = File.ReadAllText(leagueStatePath);
        leagueState = JsonUtility.FromJson<LeagueState>(json);

        UnityEngine.Debug.Log("Current Week: " + leagueState.week);
        if (leagueState.results_by_week != null && leagueState.results_by_week.TryGetValue("1", out var week1))
        {
            for (int i = 0; i < Math.Min(3, week1.Count); i++)
            {
                var res = week1[i];
                UnityEngine.Debug.Log($"{res.away} {res.away_score} @ {res.home} {res.home_score}");
            }
        }
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

    void PopulateGameResults()
    {
        if (resultsContent == null || gameResultRowPrefab == null || leagueState == null)
            return;

        foreach (Transform child in resultsContent)
        {
            Destroy(child.gameObject);
        }

        if (leagueState.results_by_week != null &&
            leagueState.results_by_week.TryGetValue(leagueState.week.ToString(), out var games))
        {
            foreach (var game in games)
            {
                var rowObj = Instantiate(gameResultRowPrefab, resultsContent);
                var row = rowObj.GetComponent<GameResultRow>();
                if (row != null)
                {
                    row.SetData(game);
                }
                else
                {
                    var texts = rowObj.GetComponentsInChildren<TMP_Text>();
                    if (texts.Length >= 4)
                    {
                        texts[0].text = game.home;
                        texts[1].text = game.away;
                        texts[2].text = game.home_score.ToString();
                        texts[3].text = game.away_score.ToString();
                    }
                }
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

    void RunSimulateWeek()
    {
        var process = new Process();
        process.StartInfo.FileName = "python";
        process.StartInfo.WorkingDirectory = Path.Combine(Application.dataPath, "..", "..");
        process.StartInfo.Arguments = "scripts/run_weekly_simulation.py";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        if (simulateWeekButton != null)
            simulateWeekButton.interactable = false;
        if (simStatusText != null)
            simStatusText.text = "Simulating...";
        try
        {
            process.Start();
            process.WaitForExit();
            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
            UnityEngine.Debug.LogError(process.StandardError.ReadToEnd());
            if (simStatusText != null)
                simStatusText.text = "Simulation complete";
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex);
            if (simStatusText != null)
                simStatusText.text = "Simulation failed";
        }
        finally
        {
            if (simulateWeekButton != null)
                simulateWeekButton.interactable = true;
        }

        LoadLeagueState();
        UpdateUI();
        PopulateGameResults();
    }
}

