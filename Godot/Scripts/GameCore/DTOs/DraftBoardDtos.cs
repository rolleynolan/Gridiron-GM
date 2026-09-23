namespace GridironGM.GameCore.DTOs;

public sealed class TeamDraftBoardContextDto
{
    public string ProspectId { get; set; } = "";
    public string Position { get; set; } = "";
    public string NeedLevel { get; set; } = "Low";
    public string RolePath { get; set; } = "Established room";
    public int RosteredAtPosition { get; set; }
    public int RequiredStarters { get; set; }
    public string Explanation { get; set; } = "";
}
