namespace GridironGM.GameCore.Models;

public sealed class GamePlayEventState
{
    public int Sequence { get; set; }
    public int Quarter { get; set; }
    public int ClockSeconds { get; set; }
    public string PossessionTeamId { get; set; } = "";
    public int Down { get; set; }
    public int Distance { get; set; }
    public int YardLine { get; set; }
    public int YardsGained { get; set; }
    public string Description { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public bool IsScoringPlay { get; set; }
    public bool IsTurnover { get; set; }
    public bool IsInjury { get; set; }
    public bool IsFinal { get; set; }
    public string PlayType { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string OffensiveTeamId { get; set; } = "";
    public string ScoringTeamId { get; set; } = "";
    public int Points { get; set; }
    public int DriveNumber { get; set; }
    public int StartYardLine { get; set; }
    public int StartDown { get; set; }
    public int StartDistance { get; set; }
    public int StartClockSeconds { get; set; }
    public int ElapsedSeconds { get; set; }
    public bool IsFirstDown { get; set; }
    public string OffensiveCall { get; set; } = "";
    public string DefensiveCall { get; set; } = "";
    public string ManagementCall { get; set; } = "";
    public string SpecialTeamsCall { get; set; } = "";
    public string InjuredPlayerId { get; set; } = "";
    public PlayerInjuryState Injury { get; set; }
    public System.Collections.Generic.List<string> ParticipantIds { get; set; } = new();
    public System.Collections.Generic.List<PlayerGameStats> StatChanges { get; set; } = new();

    public GamePlayEventState Copy()
    {
        var copy = (GamePlayEventState)MemberwiseClone();
        copy.StatChanges = StatChanges.ConvertAll(s => s.Copy());
        copy.ParticipantIds = new(ParticipantIds);
        if (Injury != null) copy.Injury = new PlayerInjuryState { Name = Injury.Name, DaysRemaining = Injury.DaysRemaining, OccurredOn = Injury.OccurredOn, GameId = Injury.GameId };
        return copy;
    }
}
