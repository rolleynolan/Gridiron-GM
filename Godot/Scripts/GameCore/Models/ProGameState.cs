using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

/// <summary>Versioned, forward-only pro simulation cursor. Scores and statistics belong to its GameResult.</summary>
public sealed class ProGameState
{
    public const string LegacyRulesVersion = "pro-snap-v1-2025";
    public const string ClockRulesVersion = "pro-snap-v2-clock-2025";
    public string RulesVersion { get; set; } = LegacyRulesVersion;
    public int HomeTimeouts { get; set; } = 3;
    public int AwayTimeouts { get; set; } = 3;
    public int LastWarningQuarter { get; set; }
    public ulong RandomState { get; set; }
    public int Quarter { get; set; } = 1;
    public int ClockSeconds { get; set; } = 900;
    public string PossessionTeamId { get; set; } = "";
    public string OpeningReceiverId { get; set; } = "";
    // Distance from the possessing team's own goal line, independent of stadium orientation.
    public int YardLine { get; set; } = 35;
    public int Down { get; set; } = 1;
    public int Distance { get; set; } = 10;
    public string Phase { get; set; } = "kickoff";
    public bool ClockRunning { get; set; }
    public bool RequireWinner { get; set; }
    public bool Completed { get; set; }
    public int HomeFieldBonus { get; set; }
    public int CurrentDrive { get; set; }
    public List<GameDriveState> Drives { get; set; } = new();
    public List<string> OvertimePossessions { get; set; } = new();
    public List<string> InjuredPlayerIds { get; set; } = new();
    public ProGameDecision PendingDecision { get; set; } = new();
}

public sealed class GameDriveState
{
    public int Number { get; set; }
    public string TeamId { get; set; } = "";
    public int StartSequence { get; set; }
    public int EndSequence { get; set; }
    public int StartYardLine { get; set; }
    public string Outcome { get; set; } = "";
}

/// <summary>Empty choices use staff recommendations. Each nonempty choice is separately authority-validated.</summary>
public sealed class ProGameDecision
{
    public string Offense { get; set; } = "";
    public string Defense { get; set; } = "";
    public string SpecialTeams { get; set; } = "";
    public string FourthDown { get; set; } = "";
    public string Tempo { get; set; } = "";
    public string Timeout { get; set; } = "";

    public ProGameDecision Copy() => (ProGameDecision)MemberwiseClone();
}
