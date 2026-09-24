using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;

public partial class DashboardController
{
    // Real bootstrap data and public commands, isolated from the user's league and save files.
    private async Task RunGameDayUiSmoke()
    {
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static IEnumerable<Node> Descendants(Node node) => node.GetChildren().SelectMany(child => new[] { child }.Concat(Descendants(child)));
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(GetTeamSeedPath());
        var game = league.Schedule.First(g => g.HomeTeamId == league.UserTeamId || g.AwayTeamId == league.UserTeamId);
        league.Calendar.AbsoluteWeek = game.AbsoluteWeek; league.Calendar.DayIndex = game.DayIndex;
        var service = new LiveGameSessionService(context);
        var start = service.Start(game.GameId); Require(start.Ok, start.Error);
        var viewport = new SubViewport { Size = new Vector2I(1024, 576) }; AddChild(viewport);
        var host = new Control(); host.SetAnchorsPreset(LayoutPreset.FullRect); viewport.AddChild(host);
        var observer = new LiveGameObserver(); host.AddChild(observer);
        observer.StartSession(start.Session.Result, game.HomeTeamId, start.Session.PlayedEvents, true);
        observer.BindSessionControls(start.Session);
        var submitted = false;
        observer.DecisionChanged += (key, value) =>
        {
            var decision = new ProGameDecision { Offense = key == "offense" ? value : "", Defense = key == "defense" ? value : "" };
            var response = service.SubmitDecision(decision, league.ActiveLiveGameSession.NextEventIndex);
            Require(response.Ok, response.Error); submitted = true;
        };
        observer.StepRequested += () =>
        {
            service.SetPaused(false);
            var response = service.Advance(league.ActiveLiveGameSession.NextEventIndex); Require(response.Ok, response.Error);
            observer.ApplySessionEvent(response.Session.CurrentEvent, response.Session.Completed);
            if (!response.Session.Completed) response = service.SetPaused(true);
            observer.BindSessionControls(response.Session);
        };
        observer.FindChild("NextPlay", true, false).EmitSignal(Button.SignalName.Pressed);
        Require(league.ActiveLiveGameSession.NextEventIndex == 1, "Next Play must resolve exactly one kickoff.");
        var choice = service.SetPaused(true).Session.Decisions.First(o => o.CanChoose && o.Key is "offense" or "defense");
        var control = (OptionButton)observer.FindChild($"Decision-{choice.Key}", true, false);
        Require(!control.Disabled, "A retained applicable choice should be enabled.");
        control.Select(1); control.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
        Require(submitted, "The selected decision did not reach GameCore.");
        observer.FindChild("NextPlay", true, false).EmitSignal(Button.SignalName.Pressed);
        var resolved = league.ActiveLiveGameSession.PlayedEvents.Last();
        Require((choice.Key == "offense" ? resolved.OffensiveCall : resolved.DefensiveCall) == choice.Choices[0], "Resolved snap ignored the submitted decision.");
        league.Teams.First(t => t.TeamId == league.UserTeamId).Coaches.First(c => c.Role == "Head Coach").Authority.ControlledDomains.Add(choice.Domain);
        observer.BindSessionControls(service.SetPaused(true).Session);
        Require(((OptionButton)observer.FindChild($"Decision-{choice.Key}", true, false)).Disabled, "Coach-owned control must be disabled.");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var liveScroll = Descendants(observer).OfType<ScrollContainer>().First();
        Require(liveScroll.GetChild<Control>(0).Size.X <= liveScroll.Size.X + 1, "Live controls overflow the minimum viewport horizontally.");
        service.SetPaused(false);
        for (var guard = 0; league.ActiveLiveGameSession.Active && guard < 1000; guard++)
            Require(service.Advance(league.ActiveLiveGameSession.NextEventIndex).Ok, "Full game could not finish.");
        var final = service.Advance(); Require(final.Session.Completed, "Expected completed result.");
        observer.Visible = false;
        var hub = new PostGameHub(); host.AddChild(hub);
        hub.ShowResult(BuildNativeGameResultDictionary(final.Session.Result));
        var tabs = Descendants(hub).OfType<Button>().Where(b => b.ToggleMode).ToList();
        Require(tabs.Count == 5, "Expected all five postgame tabs.");
        foreach (var tab in tabs)
        {
            tab.EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(Descendants(hub).OfType<Tree>().Any(t => t.GetRoot()?.GetChildCount() > 0), $"{tab.Text} has no result rows.");
            var scroll = Descendants(hub).OfType<ScrollContainer>().First();
            Require(scroll.GetChild<Control>(0).Size.X <= scroll.Size.X + 1, $"{tab.Text} overflows the minimum viewport horizontally.");
        }
        viewport.QueueFree();
        GD.Print("[Game Day UI smoke] PASS real kickoff, decision signal, next snap, coach ownership, full-game result, five postgame tabs, 1024x576 horizontal bounds.");
    }
}
