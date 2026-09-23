using Godot;
using System;
using System.Threading.Tasks;

public partial class DashboardController
{
    private async Task OnGameDaySimPressed()
    {
        var gameId = FmtString(GetFirstNonNil(_activeGameDayGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
            gameId = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");

        if (string.IsNullOrWhiteSpace(gameId))
        {
            const string error = "No scheduled game is available to simulate.";
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = error;
            SetPrimaryStatus(error);
            return;
        }

        if (_btnGameDaySim != null)
            _btnGameDaySim.Disabled = true;
        if (_lblGameDayStatus != null)
            _lblGameDayStatus.Text = "Simulating game...";
        SetPrimaryStatus("Simulating current game...");

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeGameDayService.SimulateCurrentUserGame(gameId);
            if (response?.Ok != true || response.Result == null)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Sim Game failed." : response.Error;
                if (_lblGameDayStatus != null)
                    _lblGameDayStatus.Text = error;
                SetPrimaryStatus(error);
                return;
            }

            CloseGameDayPopup();
            _activeGameDayGame = new Godot.Collections.Dictionary();
            ShowPostGameRecapFromResult(BuildNativeGameResultDictionary(response.Result));
            await SaveNativeAutosave("Native autosave updated.");
            SetPrimaryStatus("Game complete.");
            await RefreshDashboardState();
            await RefreshStateSummary();
            await RefreshInbox();
            await RefreshLeagueHub();
        }
        catch (Exception ex)
        {
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = "Unable to complete game.";
            SetPrimaryStatus($"Native Sim Game failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            if (_btnGameDaySim != null)
                _btnGameDaySim.Disabled = false;
        }
    }


    private async Task SimSelectedGame()
    {
        if (IsGameDayMessage(_selectedInboxActionItem)) { await OnInboxPrimaryActionPressed(); return; }
        if (string.IsNullOrWhiteSpace(_selectedSimGameId)) return;
        if (_overviewActionButton != null) _overviewActionButton.Disabled = true;
        try
        {
            EnsureNativeGameCoreServices(); var response = _nativeGameDayService.SimulateCurrentUserGame(_selectedSimGameId);
            if (response?.Ok != true) { SetPrimaryStatus(response?.Error ?? "Unable to simulate the selected game."); return; }
            await SaveNativeAutosave("Native autosave updated."); await RefreshStateSummary(); await RefreshInbox(); await RefreshLeagueHub();
        }
        finally { if (_overviewActionButton != null) _overviewActionButton.Disabled = false; }
    }

    private bool OpenGameDayPopupFromDashboardData()
    {
        EnsureNativeGameCoreServices(); var response = _nativeGameDayService.GetCurrentGameDayState();
        if (response?.Ok == true && response.Game != null) { _activeGameDayGame = ConvertNativeGameDayState(response.Game); return OpenGameDayPopup(_activeGameDayGame); }
        _activeGameDayGame = _dashboardNextGame?.Duplicate(true) ?? new Godot.Collections.Dictionary();
        return OpenGameDayPopup(_activeGameDayGame);
    }

    private async Task OpenCompletedScheduleGameAsync(Godot.Collections.Dictionary game)
    {
        var gameId = GetGameId(game);
        if (string.IsNullOrWhiteSpace(gameId)) { SetPrimaryStatus("No game result is available."); if (_lblScheduleActionStatus != null) _lblScheduleActionStatus.Text = "No game result is available."; return; }
        var loaded = TryShowNativeGameResult(gameId, "Game result not found.", "Loaded game result.");
        SetPrimaryStatus(loaded ? "Viewing game recap." : "Unable to load game result.");
        if (_lblScheduleActionStatus != null) _lblScheduleActionStatus.Text = loaded ? "Completed game loaded." : "Unable to load game result.";
        await Task.CompletedTask;
    }

    private async Task OnResultSelected(long index)
    {
        var item = (int)index; if (item < 0 || item >= _resultsList.ItemCount) return;
        var meta = _resultsList.GetItemMetadata(item); var gameId = ""; var resultIndex = item;
        if (meta.VariantType == Variant.Type.Dictionary && TryGetDictionary(meta, out var metadata))
        { if (metadata.ContainsKey("game_id")) gameId = FmtString((Variant)metadata["game_id"], ""); if (metadata.ContainsKey("index")) resultIndex = GetIntValue((Variant)metadata["index"], item); }
        else { gameId = FmtString(meta, ""); resultIndex = GetIntValue(meta, item); }
        if (string.IsNullOrWhiteSpace(gameId) && _resultsGames != null && resultIndex >= 0 && resultIndex < _resultsGames.Count && TryGetDictionary((Variant)_resultsGames[resultIndex], out var fallback))
        { gameId = GetGameId(fallback); if (string.IsNullOrWhiteSpace(gameId)) { ShowBoxScoreForGame(fallback); return; } }
        if (string.IsNullOrWhiteSpace(gameId)) { SetStateDumpText("Box score unavailable."); return; }
        if (_gameCache.TryGetValue(gameId, out var cached)) { ShowBoxScoreForGame(cached); return; }
        if (!TryShowNativeGameResult(gameId, "Unable to load game result.", "Loaded game result.")) SetStateDumpText("Box score unavailable.");
        await Task.CompletedTask;
    }
}
