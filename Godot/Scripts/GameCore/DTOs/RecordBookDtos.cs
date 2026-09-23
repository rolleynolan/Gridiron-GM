using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class RecordBookResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public List<RecordBookEntryDto> SeasonRecords { get; set; } = new();
    public List<RecordBookEntryDto> CareerRecords { get; set; } = new();
    public List<RecordBookEntryDto> FranchiseRecords { get; set; } = new();
}

public sealed class RecordBookEntryDto
{
    public string Label { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public int Value { get; set; }
    public int SeasonYear { get; set; }
}
