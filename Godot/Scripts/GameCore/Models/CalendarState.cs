namespace GridironGM.GameCore.Models;

public sealed class CalendarState
{
    public int Year { get; set; } = 2026;
    public int Week { get; set; } = 1;
    public int AbsoluteWeek { get; set; } = 1;
    public int PhaseWeek { get; set; } = 1;
    public int DayIndex { get; set; }
    public string Phase { get; set; } = "Preseason";
    public string CurrentDate { get; set; } = "2026-08-01";
    public string WeekLabel { get; set; } = "Week 1 - Preseason";
}
