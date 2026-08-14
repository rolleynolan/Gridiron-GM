namespace GridironGM.GameCore.Models;

public sealed class PlayerState
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public int Overall { get; set; }
    public int Age { get; set; }
    public string Status { get; set; } = "Active";
    public string Injury { get; set; } = "";
}
