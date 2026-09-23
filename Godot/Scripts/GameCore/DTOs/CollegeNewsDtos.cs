using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class CollegeNewsResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int SeasonYear { get; set; }
    public List<CollegeNewsItem> Items { get; set; } = new();
}

public sealed class CollegeNewsItem
{
    public string Category { get; set; } = "";
    public int ProAbsoluteWeek { get; set; }
    public string Headline { get; set; } = "";
    public string Detail { get; set; } = "";
}
