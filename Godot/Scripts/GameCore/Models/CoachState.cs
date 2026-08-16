namespace GridironGM.GameCore.Models;

public sealed class CoachState
{
    public string CoachId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public int Overall { get; set; }
    public int Age { get; set; }
}
