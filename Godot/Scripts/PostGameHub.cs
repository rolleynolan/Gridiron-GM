using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class PostGameHub : Control
{
    public event Action ReturnRequested;

    private Godot.Collections.Dictionary _result = new();
    private Label _week;
    private Label _away;
    private TextureRect _awayLogo;
    private Label _awayScore;
    private Label _homeScore;
    private Label _home;
    private TextureRect _homeLogo;
    private Label _summary;
    private VBoxContainer _content;
    private readonly List<Button> _tabs = new();
    private int _activeTab;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        ZIndex = 190;
        Visible = false;
        BuildInterface();
    }

    public void ShowResult(Godot.Collections.Dictionary result, Texture2D awayLogo = null, Texture2D homeLogo = null)
    {
        _result = result?.Duplicate(true) ?? new Godot.Collections.Dictionary();
        _week.Text = $"{StringValue(_result, "week_label", "Game")}  •  FINAL";
        _away.Text = StringValue(_result, "away_team", "AWAY");
        _home.Text = StringValue(_result, "home_team", "HOME");
        _awayLogo.Texture = awayLogo; _awayLogo.Visible = awayLogo != null;
        _homeLogo.Texture = homeLogo; _homeLogo.Visible = homeLogo != null;
        _awayScore.Text = IntValue(_result, "away_score").ToString();
        _homeScore.Text = IntValue(_result, "home_score").ToString();
        _summary.Text = StringValue(_result, "summary", "Game complete.");
        Visible = true;
        SelectTab(0);
    }

    private void BuildInterface()
    {
        var background = new ColorRect { Color = new Color("061625"), MouseFilter = MouseFilterEnum.Stop };
        background.SetAnchorsPreset(LayoutPreset.FullRect); AddChild(background);
        var margin = new MarginContainer(); margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 10); margin.AddThemeConstantOverride("margin_top", 10); margin.AddThemeConstantOverride("margin_right", 10); margin.AddThemeConstantOverride("margin_bottom", 10); AddChild(margin);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; margin.AddChild(scroll);
        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; root.AddThemeConstantOverride("separation", 8); scroll.AddChild(root);

        var scorePanel = Panel("071c2d", "31536b", 82); root.AddChild(scorePanel);
        var scoreRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; scoreRow.AddThemeConstantOverride("separation", 8); scorePanel.AddChild(scoreRow);
        _week = TextLabel("FINAL", 17, 120); scoreRow.AddChild(_week);
        _awayLogo = Logo(); scoreRow.AddChild(_awayLogo);
        _away = TextLabel("AWAY", 23, 110); scoreRow.AddChild(_away);
        _awayScore = TextLabel("0", 34, 70); scoreRow.AddChild(_awayScore);
        scoreRow.AddChild(TextLabel("—", 30, 40));
        _homeScore = TextLabel("0", 34, 70); scoreRow.AddChild(_homeScore);
        _homeLogo = Logo(); scoreRow.AddChild(_homeLogo);
        _home = TextLabel("HOME", 23, 110); scoreRow.AddChild(_home);
        _summary = TextLabel("Game complete.", 14, 200); _summary.SizeFlagsHorizontal = SizeFlags.ExpandFill; _summary.AutowrapMode = TextServer.AutowrapMode.WordSmart; scoreRow.AddChild(_summary);

        var tabRow = new HBoxContainer(); tabRow.AddThemeConstantOverride("separation", 6); root.AddChild(tabRow);
        foreach (var title in new[] { "SUMMARY", "BOX SCORE", "TEAM STATS", "PLAYER STATS", "GAME LOG" })
        {
            var index = _tabs.Count;
            var button = new Button { Text = title, ToggleMode = true, CustomMinimumSize = new Vector2(170, 42), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.Pressed += () => SelectTab(index);
            tabRow.AddChild(button); _tabs.Add(button);
        }

        var contentPanel = Panel("071c2d", "31536b", 390); contentPanel.SizeFlagsVertical = SizeFlags.ExpandFill; root.AddChild(contentPanel);
        var contentMargin = new MarginContainer(); contentMargin.AddThemeConstantOverride("margin_left", 10); contentMargin.AddThemeConstantOverride("margin_top", 8); contentMargin.AddThemeConstantOverride("margin_right", 10); contentMargin.AddThemeConstantOverride("margin_bottom", 8); contentPanel.AddChild(contentMargin);
        _content = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; _content.AddThemeConstantOverride("separation", 8); contentMargin.AddChild(_content);

        var footer = new HBoxContainer(); footer.AddThemeConstantOverride("separation", 8); root.AddChild(footer);
        var note = new Label { Text = "Final statistics, injuries, and game log are loaded from the immutable saved result.", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        note.AddThemeColorOverride("font_color", new Color("91a9ba")); footer.AddChild(note);
        var close = new Button { Text = "RETURN TO DASHBOARD", CustomMinimumSize = new Vector2(220, 44) }; close.Pressed += () => { Visible = false; ReturnRequested?.Invoke(); }; footer.AddChild(close);
    }

    private void SelectTab(int index)
    {
        _activeTab = Mathf.Clamp(index, 0, _tabs.Count - 1);
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].ButtonPressed = i == _activeTab;
        foreach (var child in _content.GetChildren()) child.QueueFree();
        switch (_activeTab)
        {
            case 0: RenderSummary(); break;
            case 1: RenderBoxScore(); break;
            case 2: RenderTeamStats(); break;
            case 3: RenderPlayerStats(); break;
            default: RenderGameLog(); break;
        }
    }

    private void RenderSummary()
    {
        _content.AddChild(SectionTitle("SCORING BY QUARTER"));
        _content.AddChild(BuildQuarterTree());
        var split = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; split.AddThemeConstantOverride("separation", 10); _content.AddChild(split);
        var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; split.AddChild(left);
        left.AddChild(SectionTitle("TEAM COMPARISON")); left.AddChild(BuildTeamStatsTree());
        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; split.AddChild(right);
        right.AddChild(SectionTitle("GAME LEADERS")); right.AddChild(BuildLeadersTree());
        right.AddChild(SectionTitle("INJURIES & MILESTONES")); right.AddChild(BuildInjuriesTree());
    }

    private void RenderBoxScore()
    {
        _content.AddChild(SectionTitle("SCORING BY QUARTER")); _content.AddChild(BuildQuarterTree());
        _content.AddChild(SectionTitle("SCORING SUMMARY")); _content.AddChild(BuildScoringTree());
    }

    private void RenderTeamStats() { _content.AddChild(SectionTitle("TEAM STATISTICS")); _content.AddChild(BuildTeamStatsTree()); }

    private void RenderPlayerStats()
    {
        _content.AddChild(SectionTitle("PLAYER STATISTICS"));
        var tree = NewTree(8, "Player", "Team", "Pos", "Pass", "Rush", "Rec", "Defense", "Kicking / Punting");
        for (var column = 0; column < 8; column++)
            tree.SetColumnCustomMinimumWidth(column, column switch { 0 => 160, 1 => 65, 2 => 45, 7 => 265, _ => 210 });
        var root = tree.CreateItem();
        foreach (var player in PlayerRows())
        {
            var row = tree.CreateItem(root);
            row.SetText(0, StringValue(player, "player_name", "Unknown")); row.SetText(1, StringValue(player, "team_id", "")); row.SetText(2, StringValue(player, "position", ""));
            row.SetText(3, $"{IntValue(player, "completions")}/{IntValue(player, "pass_attempts")} · {IntValue(player, "passing_yards")} YD / {IntValue(player, "passing_touchdowns")} TD / {IntValue(player, "interceptions_thrown")} INT");
            row.SetText(4, $"{IntValue(player, "rush_attempts")} ATT · {IntValue(player, "rushing_yards")} YD / {IntValue(player, "rushing_touchdowns")} TD");
            row.SetText(5, $"{IntValue(player, "receptions")} REC · {IntValue(player, "receiving_yards")} YD / {IntValue(player, "receiving_touchdowns")} TD");
            row.SetText(6, $"{IntValue(player, "tackles")} TKL / {IntValue(player, "sacks")} SK / {IntValue(player, "interceptions")} INT");
            row.SetText(7, $"{IntValue(player, "field_goals_made")}/{IntValue(player, "field_goal_attempts")} FG · {IntValue(player, "extra_points_made")}/{IntValue(player, "extra_point_attempts")} XP · {IntValue(player, "punts")} P / {IntValue(player, "punt_yards")} YD");
            if (!BoolValue(player, "detailed_stats_known"))
            {
                // Legacy results recorded yards and scores, but no attempts or special-teams detail.
                row.SetText(3, $"{IntValue(player, "passing_yards")} YD / {IntValue(player, "passing_touchdowns")} TD");
                row.SetText(4, $"{IntValue(player, "rushing_yards")} YD / {IntValue(player, "rushing_touchdowns")} TD");
                row.SetText(5, $"{IntValue(player, "receiving_yards")} YD / {IntValue(player, "receiving_touchdowns")} TD");
                row.SetText(7, "Not recorded");
            }
            for (var column = 0; column < 8; column++) row.SetTooltipText(column, row.GetText(column));
        }
        if (root.GetChildCount() == 0) tree.CreateItem(root).SetText(0, "No player statistics were recorded.");
        _content.AddChild(tree);
    }

    private void RenderGameLog()
    {
        _content.AddChild(SectionTitle("COMPLETE GAME LOG"));
        var tree = NewTree(4, "Quarter", "Clock", "Score", "Play"); var root = tree.CreateItem();
        foreach (var play in PlayRows())
        {
            var row = tree.CreateItem(root); var seconds = IntValue(play, "clock_seconds");
            row.SetText(0, PeriodName(IntValue(play, "quarter"))); row.SetText(1, $"{seconds / 60}:{seconds % 60:00}");
            row.SetText(2, $"{StringValue(_result, "away_team", "AWAY")} {IntValue(play, "away_score")} - {StringValue(_result, "home_team", "HOME")} {IntValue(play, "home_score")}");
            row.SetText(3, StringValue(play, "description", ""));
        }
        if (root.GetChildCount() == 0) tree.CreateItem(root).SetText(3, "No play-by-play was recorded for this legacy result.");
        _content.AddChild(tree);
    }

    private Tree BuildQuarterTree()
    {
        var box = BoxScore(); var quarters = DictValue(box, "quarter_scores"); var away = ArrayValue(quarters, "away"); var home = ArrayValue(quarters, "home");
        var periods = Math.Max(4, Math.Max(away.Count, home.Count));
        var titles = new[] { "Team" }.Concat(Enumerable.Range(1, periods).Select(PeriodName)).Append("Final").ToArray();
        var tree = NewTree(titles.Length, titles); tree.CustomMinimumSize = new Vector2(0, 118); var root = tree.CreateItem();
        var known = BoolValue(box, "quarter_scores_known");
        AddQuarterRow(tree, root, StringValue(_result, "away_team", "AWAY"), away, IntValue(_result, "away_score"), periods, known);
        AddQuarterRow(tree, root, StringValue(_result, "home_team", "HOME"), home, IntValue(_result, "home_score"), periods, known);
        return tree;
    }

    private static string PeriodName(int period) => period <= 4 ? $"Q{period}" : $"OT{period - 4}";

    private static void AddQuarterRow(Tree tree, TreeItem root, string team, Godot.Collections.Array scores, int final, int periods, bool known)
    {
        var row = tree.CreateItem(root); row.SetText(0, team);
        for (var q = 0; q < periods; q++) row.SetText(q + 1, known && q < scores.Count ? scores[q].AsInt32().ToString() : "—");
        row.SetText(periods + 1, final.ToString());
    }

    private Tree BuildScoringTree()
    {
        var tree = NewTree(4, "Period", "Clock", "Score (away–home)", "Scoring play"); var root = tree.CreateItem();
        foreach (var play in PlayRows().Where(p => BoolValue(p, "is_scoring_play")))
        {
            var row = tree.CreateItem(root); var seconds = IntValue(play, "clock_seconds");
            row.SetText(0, PeriodName(IntValue(play, "quarter"))); row.SetText(1, $"{seconds / 60}:{seconds % 60:00}");
            row.SetText(2, $"{IntValue(play, "away_score")}–{IntValue(play, "home_score")}"); row.SetText(3, StringValue(play, "description", ""));
        }
        if (root.GetChildCount() == 0) tree.CreateItem(root).SetText(3, "No scoring plays recorded.");
        return tree;
    }

    private Tree BuildTeamStatsTree()
    {
        var away = StringValue(_result, "away_team", "AWAY"); var home = StringValue(_result, "home_team", "HOME");
        var tree = NewTree(3, away, "Statistic", home); var root = tree.CreateItem(); var stats = DictValue(BoxScore(), "team_stats");
        var pairs = new[] { ("total_yards_away", "Total Yards (net)", "total_yards_home"), ("passing_yards_away", "Passing Yards (gross)", "passing_yards_home"), ("sack_yards_away", "Sack Yards Lost", "sack_yards_home"), ("rushing_yards_away", "Rushing Yards", "rushing_yards_home"), ("first_downs_away", "First Downs", "first_downs_home"), ("plays_away", "Offensive Plays", "plays_home"), ("turnovers_away", "Turnovers", "turnovers_home"), ("field_goals_away", "Field Goals", "field_goals_home"), ("punts_away", "Punts", "punts_home") };
        foreach (var pair in pairs)
        {
            if (!stats.ContainsKey(pair.Item1) && !stats.ContainsKey(pair.Item3)) continue;
            var row = tree.CreateItem(root); row.SetText(0, IntValue(stats, pair.Item1).ToString()); row.SetText(1, pair.Item2); row.SetText(2, IntValue(stats, pair.Item3).ToString());
        }
        return tree;
    }

    private Tree BuildLeadersTree()
    {
        var tree = NewTree(4, "Category", "Player", "Team", "Stat Line"); tree.CustomMinimumSize = new Vector2(0, 172); var root = tree.CreateItem(); var players = PlayerRows().ToList();
        AddLeader(tree, root, "Passing", players.OrderByDescending(row => IntValue(row, "passing_yards")).FirstOrDefault(), "passing_yards", "YD");
        AddLeader(tree, root, "Rushing", players.OrderByDescending(row => IntValue(row, "rushing_yards")).FirstOrDefault(), "rushing_yards", "YD");
        AddLeader(tree, root, "Receiving", players.OrderByDescending(row => IntValue(row, "receiving_yards")).FirstOrDefault(), "receiving_yards", "YD");
        AddLeader(tree, root, "Defense", players.OrderByDescending(row => IntValue(row, "tackles") + (IntValue(row, "sacks") * 3) + (IntValue(row, "interceptions") * 4)).FirstOrDefault(), "tackles", "TKL");
        return tree;
    }

    private static void AddLeader(Tree tree, TreeItem root, string category, Godot.Collections.Dictionary player, string stat, string suffix)
    {
        if (player == null || IntValue(player, stat) <= 0) return;
        var row = tree.CreateItem(root); row.SetText(0, category); row.SetText(1, StringValue(player, "player_name", "Unknown")); row.SetText(2, StringValue(player, "team_id", "")); row.SetText(3, $"{IntValue(player, stat)} {suffix}");
    }

    private Tree BuildInjuriesTree()
    {
        var tree = NewTree(3, "Type", "Subject", "Details"); tree.CustomMinimumSize = new Vector2(0, 120); var root = tree.CreateItem();
        foreach (var play in PlayRows().Where(row => BoolValue(row, "is_injury")))
        {
            var item = tree.CreateItem(root); item.SetText(0, "Injury"); item.SetText(1, StringValue(play, "possession_team_id", "")); item.SetText(2, StringValue(play, "description", "Medical timeout"));
        }
        if (root.GetChildCount() == 0) tree.CreateItem(root).SetText(2, "No game injuries or recorded milestones.");
        return tree;
    }

    private Godot.Collections.Dictionary BoxScore() => DictValue(_result, "box_score");
    private IEnumerable<Godot.Collections.Dictionary> PlayerRows() => Rows(ArrayValue(BoxScore(), "player_stats"));
    private IEnumerable<Godot.Collections.Dictionary> PlayRows() => Rows(ArrayValue(BoxScore(), "play_by_play"));
    private static IEnumerable<Godot.Collections.Dictionary> Rows(Godot.Collections.Array array) { foreach (var value in array) if (value.VariantType == Variant.Type.Dictionary) yield return value.AsGodotDictionary(); }

    private static Tree NewTree(int columns, params string[] titles)
    {
        var tree = new Tree { Columns = columns, HideRoot = true, ColumnTitlesVisible = true, SizeFlagsVertical = SizeFlags.ExpandFill };
        for (var index = 0; index < Math.Min(columns, titles.Length); index++) tree.SetColumnTitle(index, titles[index]);
        tree.AddThemeFontSizeOverride("font_size", 14); return tree;
    }

    private static Label SectionTitle(string text) { var label = new Label { Text = text }; label.AddThemeFontSizeOverride("font_size", 17); label.AddThemeColorOverride("font_color", new Color("f0ba27")); return label; }
    private static PanelContainer Panel(string fill, string border, float height) { var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, height) }; var style = new StyleBoxFlat { BgColor = new Color(fill), BorderColor = new Color(border) }; style.SetBorderWidthAll(1); panel.AddThemeStyleboxOverride("panel", style); return panel; }
    private static Label TextLabel(string text, int size, float width) { var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(width, 0) }; label.AddThemeColorOverride("font_color", new Color("f4f0df")); label.AddThemeFontSizeOverride("font_size", size); return label; }
    private static TextureRect Logo() => new() { CustomMinimumSize = new Vector2(54, 54), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = TextureFilterEnum.Nearest, Visible = false };

    private static Godot.Collections.Dictionary DictValue(Godot.Collections.Dictionary source, string key) => source != null && source.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Dictionary ? value.AsGodotDictionary() : new Godot.Collections.Dictionary();
    private static Godot.Collections.Array ArrayValue(Godot.Collections.Dictionary source, string key) => source != null && source.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Array ? value.AsGodotArray() : new Godot.Collections.Array();
    private static string StringValue(Godot.Collections.Dictionary source, string key, string fallback) => source != null && source.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil ? value.ToString() : fallback;
    private static int IntValue(Godot.Collections.Dictionary source, string key) => source != null && source.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil ? value.AsInt32() : 0;
    private static bool BoolValue(Godot.Collections.Dictionary source, string key) => source != null && source.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil && value.AsBool();
}
