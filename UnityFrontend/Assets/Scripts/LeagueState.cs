using System;
using System.Collections.Generic;

[Serializable]
public class TeamInfo
{
    public string name;
    public string abbreviation;
}

[Serializable]
public class GameResult
{
    public string home;
    public string away;
    public int home_score;
    public int away_score;
    public int home_yards;
    public int away_yards;
}

[Serializable]
public class LeagueState
{
    public int week;
    public Dictionary<string, List<GameResult>> results_by_week;
    public List<TeamInfo> teams;
}
