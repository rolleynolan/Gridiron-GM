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
}
