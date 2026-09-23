using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class CollegeBigBoardsResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public List<CollegeBigBoard> Boards { get; set; } = new();
}

public sealed class CollegeBigBoard
{
    public string Name { get; set; } = "";
    public List<CollegeBigBoardEntry> Entries { get; set; } = new();
}

public sealed class CollegeBigBoardEntry
{
    public int Rank { get; set; }
    public string ProspectId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string College { get; set; } = "";
    public string Summary { get; set; } = "";
}
