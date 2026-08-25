namespace GridironGM.GameCore.Models;

public sealed class TransactionRecord
{
    public string TransactionId { get; set; } = "";
    public int SeasonYear { get; set; }
    public string DateLabel { get; set; } = "";
    public string Phase { get; set; } = "";
    public string Type { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Details { get; set; } = "";
}
