using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;

public partial class DashboardController
{
    private void ConfigureLiveGameObserver()
    {
        if (_liveGameObserver != null)
            return;
        _liveGameObserver = new LiveGameObserver { Name = "LiveGameObserver" };
        _liveGameObserver.ExitRequested += OnLiveGameObserverExit;
        _liveGameObserver.BoxScoreRequested += OnLiveGameObserverBoxScore;
        _liveGameObserver.AdvanceRequested += OnLiveGameAdvanceRequested;
        _liveGameObserver.PauseChanged += OnLiveGamePauseChanged;
        _liveGameObserver.AdjustmentsRequested += () => _ = OpenLiveGameAdjustments();
        _liveGameObserver.StepRequested += OnLiveGameStepRequested;
        _liveGameObserver.DecisionChanged += OnLiveGameDecisionChanged;
        AddChild(_liveGameObserver);
    }

    private void ConfigurePostGameHub()
    {
        if (_postGameHub != null)
            return;
        _postGameHub = new PostGameHub { Name = "PostGameHub" };
        _postGameHub.ReturnRequested += () => _ = ClosePostGameRecapPopupAsync();
        AddChild(_postGameHub);
    }

    private async Task OnWatchGamePressed()
    {
        var gameId = FmtString(GetFirstNonNil(_activeGameDayGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
            gameId = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
        {
            SetPrimaryStatus("No current game is available to watch.");
            return;
        }

        _btnGameDayWatch.Disabled = true;
        if (_btnGameDaySim != null) _btnGameDaySim.Disabled = true;
        if (_lblGameDayStatus != null) _lblGameDayStatus.Text = "Starting incremental game session…";
        SetPrimaryStatus("Preparing incremental game session...");
        try
        {
            EnsureNativeGameCoreServices();
            var scheduledGame = _nativeGameDayService.GetCurrentUserGame();
            var response = _nativeLiveGameSessionService.Start(gameId);
            if (response?.Ok != true || response.Session?.Result == null)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to prepare game playback." : response.Error;
                if (_lblGameDayStatus != null) _lblGameDayStatus.Text = error;
                SetPrimaryStatus(error);
                return;
            }

            _observedGameResult = null;
            CloseGameDayPopup();
            await SaveNativeAutosave("Native autosave updated.");
            var preview = response.Session.Result;
            _liveGameObserver.StartSession(preview, scheduledGame?.HomeTeamId ?? "", response.Session.PlayedEvents, response.Session.IsPaused, LoadTeamLogo(preview.AwayTeam), LoadTeamLogo(preview.HomeTeam));
            _liveGameObserver.BindSessionControls(response.Session);
            SetPrimaryStatus("Live game paused and ready.");
        }
        catch (Exception ex)
        {
            if (_lblGameDayStatus != null) _lblGameDayStatus.Text = "Unable to prepare game playback.";
            SetPrimaryStatus($"Watch Game failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            _btnGameDayWatch.Disabled = false;
            if (_btnGameDaySim != null) _btnGameDaySim.Disabled = false;
        }
    }

    private async void OnLiveGameObserverExit()
    {
        if (_observedGameResult != null)
        {
            ShowPostGameRecapFromResult(_observedGameResult);
            _observedGameResult = null;
            SetPrimaryStatus("Game playback complete.");
        }
        else
        {
            var paused = _nativeLiveGameSessionService.SetPaused(true);
            if (!paused.Ok) { SetPrimaryStatus(paused.Error); return; }
            if (await SaveNativeAutosave("Live game saved."))
                SetPrimaryStatus("Live game saved. Reopen Game Day to continue.");
        }
    }

    private void OnLiveGamePauseChanged(bool paused)
    {
        EnsureNativeGameCoreServices();
        var response = _nativeLiveGameSessionService.SetPaused(paused);
        if (!response.Ok)
            _liveGameObserver.SetSessionError(response.Error);
        else
        {
            _liveGameObserver.BindSessionControls(response.Session);
            _ = SaveNativeAutosave("Native autosave updated.");
        }
    }

    private void OnLiveGameAdvanceRequested()
    {
        EnsureNativeGameCoreServices();
        var response = _nativeLiveGameSessionService.Advance(_nativeGameCoreContext.ActiveLeague.ActiveLiveGameSession.NextEventIndex);
        if (!response.Ok)
        {
            _nativeLiveGameSessionService.SetPaused(true);
            _liveGameObserver.SetSessionError(response.Error);
            return;
        }
        _liveGameObserver.ApplySessionEvent(response.Session.CurrentEvent, response.Session.Completed);
        _liveGameObserver.BindSessionControls(response.Session);
        if (response.Session.Completed && response.Session.Result != null)
            _ = FinalizeLiveGameSession(response.Session.Result);
    }

    private void OnLiveGameStepRequested()
    {
        EnsureNativeGameCoreServices();
        if (!_nativeLiveGameSessionService.SetPaused(false).Ok) return;
        OnLiveGameAdvanceRequested();
        if (_nativeGameCoreContext.ActiveLeague.ActiveLiveGameSession.Active) OnLiveGamePauseChanged(true);
    }

    private void OnLiveGameDecisionChanged(string key, string value)
    {
        var session = _nativeGameCoreContext.ActiveLeague.ActiveLiveGameSession;
        var pending = session.PendingResult.ProGame?.PendingDecision ?? new ProGameDecision();
        var decision = new ProGameDecision { Offense = pending.Offense, Defense = pending.Defense, SpecialTeams = pending.SpecialTeams, FourthDown = pending.FourthDown, Tempo = pending.Tempo };
        switch (key)
        {
            case "offense": decision.Offense = value; break;
            case "defense": decision.Defense = value; break;
            case "special": decision.SpecialTeams = value; break;
            case "fourth": decision.FourthDown = value; break;
            case "tempo": decision.Tempo = value; break;
        }
        var response = _nativeLiveGameSessionService.SubmitDecision(decision, session.NextEventIndex);
        if (!response.Ok) _liveGameObserver.SetSessionError(response.Error);
        else
        {
            _liveGameObserver.BindSessionControls(response.Session);
            _ = SaveNativeAutosave("Native autosave updated.");
        }
    }

    private async Task FinalizeLiveGameSession(GameResultDto result)
    {
        _observedGameResult = BuildNativeGameResultDictionary(result);
        await SaveNativeAutosave("Native autosave updated.");
        await RefreshDashboardState();
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
        SetPrimaryStatus("Live game final. Review the box score or continue to postgame.");
    }

    private async Task OpenLiveGameAdjustments()
    {
        EnsureNativeGameCoreServices();
        var paused = _nativeLiveGameSessionService.SetPaused(true);
        if (!paused.Ok)
        {
            _liveGameObserver.SetSessionError(paused.Error);
            return;
        }
        _liveGameAdjustmentMode = true;
        _liveGameObserver.PauseForAdjustments();
        _liveGameObserver.BindSessionControls(paused.Session);
        await SaveNativeAutosave("Live game paused and saved.");
        _liveGameObserver.Visible = false;
        if (_btnReturnToLiveGame != null) _btnReturnToLiveGame.Visible = true;
        await SelectMainTab(ROSTER_TAB_INDEX);
        await SetRosterViewMode(true);
        SetDepthChartActionStatus("LIVE GAME PAUSED · Drag within a position group or set a starter. Changes apply only to unplayed events.");
        SetPrimaryStatus("Live game paused for depth-chart adjustments.");
    }

    private void ReturnToLiveGameObserver()
    {
        _liveGameAdjustmentMode = false;
        if (_btnReturnToLiveGame != null) _btnReturnToLiveGame.Visible = false;
        if (_btnAutoFillDepthChart != null) _btnAutoFillDepthChart.Disabled = false;
        UpdateDepthChartEditButtons();
        if (_liveGameObserver != null) _liveGameObserver.Visible = true;
        SetPrimaryStatus("Returned to paused live game.");
    }

    private void OnLiveGameObserverBoxScore()
    {
        if (_observedGameResult == null)
            return;
        _restoreLiveGameObserverAfterBoxScore = _liveGameObserver?.Visible == true;
        if (_liveGameObserver != null) _liveGameObserver.Visible = false;
        ShowBoxScoreFromResult(_observedGameResult);
    }





}
