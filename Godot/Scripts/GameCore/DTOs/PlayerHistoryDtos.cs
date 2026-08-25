using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class PlayerHistoryResponse
{
    public bool Ok { get; set; }
    public string PlayerName { get; set; } = "";
    public PlayerSeasonHistoryDto CurrentSeason { get; set; } = new();
    public List<PlayerSeasonHistoryDto> CareerSeasons { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class PlayerSeasonHistoryDto
{
    public int SeasonYear { get; set; }
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int PassingTouchdowns { get; set; }
    public int RushingYards { get; set; }
    public int RushingTouchdowns { get; set; }
    public int ReceivingYards { get; set; }
    public int ReceivingTouchdowns { get; set; }
    public int Tackles { get; set; }
    public int Sacks { get; set; }
    public int Interceptions { get; set; }
}
