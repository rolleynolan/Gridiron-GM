using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

[GlobalClass]
public partial class LiveGameObserver : Control
{
    public event Action ExitRequested;
    public event Action BoxScoreRequested;
    public event Action AdvanceRequested;
    public event Action<bool> PauseChanged;
    public event Action AdjustmentsRequested;

    private readonly List<GamePlayEventState> _plays = new();
    private Label _week;
    private Label _awayTeam;
    private TextureRect _awayLogo;
    private Label _awayScore;
    private Label _clock;
    private Label _homeScore;
    private Label _homeTeam;
    private TextureRect _homeLogo;
    private Label _situation;
    private Label _lastPlay;
    private Label _status;
    private ItemList _playLog;
    private LiveGameFieldView _field;
    private Button _pauseButton;
    private OptionButton _highlights;
    private Button _boxScoreButton;
    private Button _adjustmentsButton;
    private Timer _timer;
    private int _playIndex = -1;
    private double _speed = 1;
    private string _homeTeamId = "";
    private string _homeAbbreviation = "HOME";
    private string _awayAbbreviation = "AWAY";
    private bool _sessionMode;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        ZIndex = 200;
        Visible = false;
        BuildInterface();
    }

    public void Start(GameResultDto result, string homeTeamId, Texture2D awayLogo = null, Texture2D homeLogo = null)
    {
        _sessionMode = false;
        _plays.Clear();
        if (result?.BoxScore != null
            && result.BoxScore.TryGetValue("play_by_play", out var raw)
            && raw is IEnumerable<GamePlayEventState> timeline)
            _plays.AddRange(timeline.OrderBy(play => play.Sequence));

        _homeTeamId = homeTeamId ?? "";
        _homeAbbreviation = string.IsNullOrWhiteSpace(result?.HomeTeam) ? "HOME" : result.HomeTeam;
        _awayAbbreviation = string.IsNullOrWhiteSpace(result?.AwayTeam) ? "AWAY" : result.AwayTeam;
        _week.Text = result?.WeekLabel ?? "Game Day";
        _homeTeam.Text = _homeAbbreviation;
        _awayTeam.Text = _awayAbbreviation;
        _awayLogo.Texture = awayLogo; _awayLogo.Visible = awayLogo != null;
        _homeLogo.Texture = homeLogo; _homeLogo.Visible = homeLogo != null;
        _homeScore.Text = "0";
        _awayScore.Text = "0";
        _clock.Text = "1ST 15:00";
        _situation.Text = "OPENING KICKOFF";
        _lastPlay.Text = "The authoritative game result is ready for playback.";
        _status.Text = _plays.Count > 0 ? "PLAYING" : "NO PLAYBACK DATA";
        _playLog.Clear();
        _playIndex = -1;
        _pauseButton.Text = "Ⅱ  PAUSE";
        _pauseButton.Disabled = _plays.Count == 0;
        _adjustmentsButton.Disabled = true;
        _boxScoreButton.Disabled = false;
        _field.SetPlay(50, false);
        Visible = true;
        if (_plays.Count > 0)
        {
            _timer.WaitTime = 0.9 / _speed;
            _timer.Start();
        }
    }

    public void StartSession(GameResultDto preview, string homeTeamId, IEnumerable<GamePlayEventState> playedEvents, bool isPaused, Texture2D awayLogo = null, Texture2D homeLogo = null)
    {
        Start(preview, homeTeamId, awayLogo, homeLogo);
        _sessionMode = true;
        _timer.Stop();
        _playLog.Clear();
        _playIndex = -1;
        foreach (var play in playedEvents ?? Enumerable.Empty<GamePlayEventState>())
            DisplayPlay(play);
        _adjustmentsButton.Disabled = false;
        _boxScoreButton.Disabled = true;
        _pauseButton.Disabled = false;
        if (isPaused)
        {
            _pauseButton.Text = "▶  RESUME";
            _status.Text = "PAUSED";
        }
        else
        {
            _pauseButton.Text = "Ⅱ  PAUSE";
            _status.Text = "PLAYING";
            _timer.Start();
        }
    }

    public void ApplySessionEvent(GamePlayEventState play, bool completed)
    {
        if (play != null)
            DisplayPlay(play);
        if (completed)
        {
            _boxScoreButton.Disabled = false;
            _adjustmentsButton.Disabled = true;
            FinishPlayback();
        }
    }

    public void SetSessionError(string message)
    {
        _timer.Stop();
        _status.Text = "PAUSED";
        _pauseButton.Text = "▶  RESUME";
        _lastPlay.Text = message ?? "Live game request failed.";
    }

    public void PauseForAdjustments()
    {
        _timer.Stop();
        _pauseButton.Text = "▶  RESUME";
        _status.Text = "PAUSED";
    }

    private void BuildInterface()
    {
        AddChild(new ColorRect { Color = new Color("061625"), MouseFilter = MouseFilterEnum.Stop, LayoutMode = 1, AnchorsPreset = (int)LayoutPreset.FullRect, AnchorRight = 1, AnchorBottom = 1 });
        var margin = new MarginContainer { LayoutMode = 1, AnchorsPreset = (int)LayoutPreset.FullRect, AnchorRight = 1, AnchorBottom = 1 };
        margin.AddThemeConstantOverride("margin_left", 8); margin.AddThemeConstantOverride("margin_top", 8); margin.AddThemeConstantOverride("margin_right", 8); margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);
        var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 8); margin.AddChild(root);

        var scoreboard = PanelRow(86); root.AddChild(scoreboard);
        var scoreRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; scoreRow.AddThemeConstantOverride("separation", 22); scoreboard.AddChild(scoreRow);
        _week = ScoreLabel("GAME DAY", 18, 150); scoreRow.AddChild(_week);
        _awayLogo = Logo(); scoreRow.AddChild(_awayLogo);
        _awayTeam = ScoreLabel("AWAY", 24, 230); scoreRow.AddChild(_awayTeam);
        _awayScore = ScoreLabel("0", 34, 65); scoreRow.AddChild(_awayScore);
        _clock = ScoreLabel("1ST 15:00", 23, 175); scoreRow.AddChild(_clock);
        _homeScore = ScoreLabel("0", 34, 65); scoreRow.AddChild(_homeScore);
        _homeLogo = Logo(); scoreRow.AddChild(_homeLogo);
        _homeTeam = ScoreLabel("HOME", 24, 230); scoreRow.AddChild(_homeTeam);

        var situationPanel = PanelRow(48); root.AddChild(situationPanel);
        _situation = ScoreLabel("OPENING KICKOFF", 20, 0); _situation.SizeFlagsHorizontal = SizeFlags.ExpandFill; situationPanel.AddChild(_situation);

        var main = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; main.AddThemeConstantOverride("separation", 8); root.AddChild(main);
        var fieldPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(700, 420) };
        fieldPanel.AddThemeStyleboxOverride("panel", Surface("0b2435", "31536b")); main.AddChild(fieldPanel);
        var fieldColumn = new VBoxContainer(); fieldPanel.AddChild(fieldColumn);
        _field = new LiveGameFieldView { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; fieldColumn.AddChild(_field);
        _lastPlay = new Label { Text = "Previous play", CustomMinimumSize = new Vector2(0, 38), VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _lastPlay.AddThemeColorOverride("font_color", new Color("f5f0dd")); _lastPlay.AddThemeFontSizeOverride("font_size", 15); fieldColumn.AddChild(_lastPlay);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(430, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        side.AddThemeStyleboxOverride("panel", Surface("081d2d", "31536b")); main.AddChild(side);
        var sideMargin = new MarginContainer(); sideMargin.AddThemeConstantOverride("margin_left", 12); sideMargin.AddThemeConstantOverride("margin_top", 10); sideMargin.AddThemeConstantOverride("margin_right", 12); sideMargin.AddThemeConstantOverride("margin_bottom", 10); side.AddChild(sideMargin);
        var sideColumn = new VBoxContainer(); sideColumn.AddThemeConstantOverride("separation", 7); sideMargin.AddChild(sideColumn);
        var playHeader = ScoreLabel("PLAY-BY-PLAY", 18, 0); playHeader.HorizontalAlignment = HorizontalAlignment.Left; sideColumn.AddChild(playHeader);
        _playLog = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, AllowReselect = false }; _playLog.AddThemeFontSizeOverride("font_size", 14); sideColumn.AddChild(_playLog);
        var integrity = new Label { Text = "Playback follows the saved GameCore result. It cannot change the final score.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        integrity.AddThemeColorOverride("font_color", new Color("94aabd")); integrity.AddThemeFontSizeOverride("font_size", 12); sideColumn.AddChild(integrity);

        var footer = PanelRow(62); root.AddChild(footer);
        var controls = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; controls.AddThemeConstantOverride("separation", 8); footer.AddChild(controls);
        _pauseButton = ActionButton("Ⅱ  PAUSE", 138); _pauseButton.Pressed += TogglePause; controls.AddChild(_pauseButton);
        controls.AddChild(ScoreLabel("SPEED", 14, 62));
        foreach (var speed in new[] { 1, 2, 4 })
        {
            var button = ActionButton($"{speed}X", 62);
            button.Pressed += () => SetSpeed(speed);
            controls.AddChild(button);
        }
        controls.AddChild(ScoreLabel("HIGHLIGHTS", 14, 100));
        _highlights = new OptionButton { CustomMinimumSize = new Vector2(150, 38) }; _highlights.AddItem("ALL PLAYS"); _highlights.AddItem("KEY PLAYS"); controls.AddChild(_highlights);
        _boxScoreButton = ActionButton("BOX SCORE", 125); _boxScoreButton.Pressed += () => BoxScoreRequested?.Invoke(); controls.AddChild(_boxScoreButton);
        _adjustmentsButton = ActionButton("LIVE ADJUSTMENTS", 170); _adjustmentsButton.Disabled = true; _adjustmentsButton.TooltipText = "Pause to adjust the live depth chart for future snaps."; _adjustmentsButton.Pressed += () => AdjustmentsRequested?.Invoke(); controls.AddChild(_adjustmentsButton);
        var exit = ActionButton("EXIT TO POSTGAME", 165); exit.Pressed += ExitObserver; controls.AddChild(exit);
        _status = ScoreLabel("PAUSED", 17, 115); controls.AddChild(_status);

        _timer = new Timer { OneShot = false, WaitTime = 0.9 };
        _timer.Timeout += Advance;
        AddChild(_timer);
    }

    private void Advance()
    {
        if (_sessionMode)
        {
            AdvanceRequested?.Invoke();
            return;
        }
        var next = _playIndex + 1;
        while (next < _plays.Count && _highlights.Selected == 1 && !_plays[next].IsScoringPlay && _plays[next].ClockSeconds != 0)
            next++;
        if (next >= _plays.Count)
        {
            FinishPlayback();
            return;
        }
        _playIndex = next;
        var play = _plays[_playIndex];
        DisplayPlay(play);
        if (play.ClockSeconds == 0 || _playIndex == _plays.Count - 1)
            FinishPlayback();
    }

    private void DisplayPlay(GamePlayEventState play)
    {
        _awayScore.Text = play.AwayScore.ToString();
        _homeScore.Text = play.HomeScore.ToString();
        _clock.Text = play.ClockSeconds == 0 ? "FINAL" : $"{Ordinal(play.Quarter)} {play.ClockSeconds / 60}:{play.ClockSeconds % 60:00}";
        _situation.Text = play.ClockSeconds == 0 ? "GAME COMPLETE" : $"{TeamFor(play.PossessionTeamId)} BALL  •  {DownText(play.Down, play.Distance)}  •  YARD LINE {play.YardLine}";
        _lastPlay.Text = $"PREVIOUS PLAY: {play.Description}";
        _playLog.AddItem($"Q{play.Quarter} {play.ClockSeconds / 60}:{play.ClockSeconds % 60:00}  {play.Description}");
        _playLog.Select(_playLog.ItemCount - 1);
        _playLog.EnsureCurrentIsVisible();
        _field.SetPlay(play.YardLine, string.Equals(play.PossessionTeamId, _homeTeamId, StringComparison.OrdinalIgnoreCase));
    }

    private void FinishPlayback()
    {
        _timer.Stop();
        _status.Text = "FINAL";
        _pauseButton.Text = "REPLAY COMPLETE";
        _pauseButton.Disabled = true;
    }

    private void TogglePause()
    {
        if (_timer.IsStopped())
        {
            _timer.Start(); _pauseButton.Text = "Ⅱ  PAUSE"; _status.Text = "PLAYING";
            if (_sessionMode) PauseChanged?.Invoke(false);
        }
        else
        {
            _timer.Stop(); _pauseButton.Text = "▶  RESUME"; _status.Text = "PAUSED";
            if (_sessionMode) PauseChanged?.Invoke(true);
        }
    }

    private void SetSpeed(int speed)
    {
        _speed = speed;
        _timer.WaitTime = 0.9 / _speed;
    }

    private void ExitObserver()
    {
        _timer.Stop(); Visible = false; ExitRequested?.Invoke();
    }

    private string TeamFor(string teamId) => string.Equals(teamId, _homeTeamId, StringComparison.OrdinalIgnoreCase) ? _homeAbbreviation : _awayAbbreviation;
    private static string DownText(int down, int distance) => down <= 0 ? "CHANGE OF POSSESSION" : $"{down}{(down == 1 ? "ST" : down == 2 ? "ND" : down == 3 ? "RD" : "TH")} & {distance}";
    private static string Ordinal(int quarter) => quarter switch { 1 => "1ST", 2 => "2ND", 3 => "3RD", 4 => "4TH", _ => $"Q{quarter}" };

    private static PanelContainer PanelRow(float height)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, height) };
        panel.AddThemeStyleboxOverride("panel", Surface("071c2d", "31536b"));
        return panel;
    }

    private static Label ScoreLabel(string text, int size, float width)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(width, 0) };
        label.AddThemeColorOverride("font_color", new Color("f4f0df")); label.AddThemeFontSizeOverride("font_size", size); return label;
    }

    private static Button ActionButton(string text, float width) => new() { Text = text, CustomMinimumSize = new Vector2(width, 38) };
    private static TextureRect Logo() => new() { CustomMinimumSize = new Vector2(54, 54), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = TextureFilterEnum.Nearest, Visible = false };

    private static StyleBoxFlat Surface(string fill, string border)
    {
        var style = new StyleBoxFlat { BgColor = new Color(fill), BorderColor = new Color(border) };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(0); return style;
    }
}
