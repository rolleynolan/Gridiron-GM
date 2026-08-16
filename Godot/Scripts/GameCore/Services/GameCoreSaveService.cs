using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public class GameCoreSaveResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string SavePath { get; set; } = "";
}

public sealed class GameCoreLoadResult : GameCoreSaveResult
{
    public bool SaveMissing { get; set; }
    public LeagueState League { get; set; }
}

public sealed class GameCoreSaveService
{
    public const string AutosaveFileName = "native_autosave.json";
    public const string NamedSaveFileName = "native_save.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public GameCoreSaveResult Save(GameCoreContext context, string saveName = null)
    {
        var logicalPath = BuildLogicalSavePath(saveName);
        var absolutePath = ResolveAbsoluteSavePath(logicalPath);

        if (context?.ActiveLeague == null)
        {
            return new GameCoreSaveResult
            {
                Ok = false,
                Message = "No active native league to save.",
                SavePath = logicalPath,
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(context.ActiveLeague, JsonOptions);
            File.WriteAllText(absolutePath, json);

            return new GameCoreSaveResult
            {
                Ok = true,
                Message = "Native game saved.",
                SavePath = logicalPath,
            };
        }
        catch (Exception ex)
        {
            return new GameCoreSaveResult
            {
                Ok = false,
                Message = $"Unable to save native game. {ex.Message}",
                SavePath = logicalPath,
            };
        }
    }

    public GameCoreLoadResult Load(string saveName = null)
    {
        var logicalPath = BuildLogicalSavePath(saveName);
        var absolutePath = ResolveAbsoluteSavePath(logicalPath);

        try
        {
            if (!File.Exists(absolutePath))
            {
                return new GameCoreLoadResult
                {
                    Ok = false,
                    SaveMissing = true,
                    Message = "No native save found.",
                    SavePath = logicalPath,
                };
            }

            var json = File.ReadAllText(absolutePath);
            var league = JsonSerializer.Deserialize<LeagueState>(json, JsonOptions);
            NormalizeLeague(league);

            return new GameCoreLoadResult
            {
                Ok = true,
                Message = "Native game loaded.",
                SavePath = logicalPath,
                League = league,
            };
        }
        catch (Exception ex)
        {
            return new GameCoreLoadResult
            {
                Ok = false,
                Message = $"Unable to load native save. {ex.Message}",
                SavePath = logicalPath,
            };
        }
    }

    public GameCoreSaveResult Delete(string saveName = null)
    {
        var logicalPath = BuildLogicalSavePath(saveName);
        var absolutePath = ResolveAbsoluteSavePath(logicalPath);

        try
        {
            if (!File.Exists(absolutePath))
            {
                return new GameCoreSaveResult
                {
                    Ok = true,
                    Message = "No native save found.",
                    SavePath = logicalPath,
                };
            }

            File.Delete(absolutePath);
            return new GameCoreSaveResult
            {
                Ok = true,
                Message = "Native save deleted.",
                SavePath = logicalPath,
            };
        }
        catch (Exception ex)
        {
            return new GameCoreSaveResult
            {
                Ok = false,
                Message = $"Unable to delete native save. {ex.Message}",
                SavePath = logicalPath,
            };
        }
    }

    public bool SaveExists(string saveName = null)
    {
        var logicalPath = BuildLogicalSavePath(saveName);
        var absolutePath = ResolveAbsoluteSavePath(logicalPath);
        return File.Exists(absolutePath);
    }

    private static string BuildLogicalSavePath(string saveName)
    {
        var fileName = string.IsNullOrWhiteSpace(saveName) ? AutosaveFileName : saveName.Trim();
        fileName = Path.GetFileName(fileName);
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";
        return $"user://saves/{fileName}";
    }

    private static string ResolveAbsoluteSavePath(string logicalPath)
    {
        var relativePath = logicalPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
            ? logicalPath["user://".Length..].Replace('/', Path.DirectorySeparatorChar)
            : logicalPath.Replace('/', Path.DirectorySeparatorChar);

        var baseDirectory = ResolveUserStorageRoot();
        return Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
    }

    private static string ResolveUserStorageRoot()
        => Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "GridironGM");

    private static void NormalizeLeague(LeagueState league)
    {
        if (league == null)
            throw new InvalidDataException("Save file did not contain a native league.");

        league.SaveVersion = league.SaveVersion <= 0 ? 1 : league.SaveVersion;
        league.SalaryCap = league.SalaryCap <= 0m ? LeagueState.DefaultSalaryCap : league.SalaryCap;
        var isLegacySave = league.SaveVersion < LeagueState.CurrentSaveVersion;
        league.Calendar ??= new CalendarState();
        league.Teams ??= new List<TeamState>();
        league.FreeAgents ??= new List<PlayerState>();
        league.CollegeProspects ??= new List<CollegeProspectState>();
        league.Draft ??= new DraftState();
        league.Schedule ??= new List<ScheduledGame>();
        league.Results ??= new List<GameResult>();
        league.PlayoffBracket ??= new PlayoffBracket();
        league.HistoricalSeasons ??= new List<SeasonHistoryRecord>();
        league.RetirementHistory ??= new List<SeasonRetirementRecord>();
        league.LastContinueResult ??= new ContinueResult();
        league.LastContinueResult.EventsProcessed ??= new List<ContinueEvent>();
        league.FranchiseMetadata ??= new FranchiseMetadata();
        league.FranchiseMetadata.World ??= WorldDefinition.Standard();
        league.FranchiseMetadata.GmProfileSnapshot ??= new GmProfile();
        league.FranchiseMetadata.GmProfileSnapshot.Attributes ??= new GmAttributes();
        league.FranchiseMetadata.GmProfileSnapshot.Appearance ??= new CharacterDesign();

        foreach (var team in league.Teams)
        {
            if (team == null)
                continue;

            team.Roster ??= new List<PlayerState>();
            team.Coaches ??= new List<CoachState>();
            team.DepthChart ??= new Dictionary<string, List<string>>();
        }

        league.PlayoffBracket.GeneratedAtPhaseLabel ??= "";
        league.PlayoffBracket.ConferenceBrackets ??= new List<PlayoffConferenceBracket>();
        league.PlayoffBracket.LeagueChampionshipRound ??= new PlayoffRound();
        league.PlayoffBracket.LeagueChampionRecord ??= new LeagueChampionRecord();
        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets)
        {
            if (conferenceBracket == null)
                continue;

            conferenceBracket.Conference ??= "";
            conferenceBracket.Seeds ??= new List<PlayoffSeed>();
            conferenceBracket.Rounds ??= new List<PlayoffRound>();

            foreach (var seed in conferenceBracket.Seeds)
            {
                if (seed == null)
                    continue;

                seed.TeamId ??= "";
                seed.TeamName ??= "";
                seed.Conference ??= "";
                seed.Division ??= "";
            }

            foreach (var round in conferenceBracket.Rounds)
            {
                if (round == null)
                    continue;

                round.Round ??= "";
                round.Status = string.IsNullOrWhiteSpace(round.Status) ? "scheduled" : round.Status;
                round.Games ??= new List<PlayoffGame>();
                foreach (var game in round.Games)
                {
                    if (game == null)
                        continue;

                    game.GameId ??= "";
                    game.Round ??= "";
                    game.RoundLabel ??= "";
                    game.Conference ??= "";
                    game.Phase ??= "";
                    game.GameType ??= "";
                    game.HomeTeamId ??= "";
                    game.AwayTeamId ??= "";
                    game.HomeTeamName ??= "";
                    game.AwayTeamName ??= "";
                    game.Status = string.IsNullOrWhiteSpace(game.Status) ? "scheduled" : game.Status;
                    game.WinnerTeamId ??= "";
                    game.LoserTeamId ??= "";
                    PlayoffService.NormalizePlayoffGame(game);
                }
            }
        }

        league.PlayoffBracket.LeagueChampionshipRound.Round ??= "";
        league.PlayoffBracket.LeagueChampionshipRound.Status = string.IsNullOrWhiteSpace(league.PlayoffBracket.LeagueChampionshipRound.Status)
            ? "scheduled"
            : league.PlayoffBracket.LeagueChampionshipRound.Status;
        league.PlayoffBracket.LeagueChampionshipRound.Games ??= new List<PlayoffGame>();
        foreach (var game in league.PlayoffBracket.LeagueChampionshipRound.Games)
        {
            if (game == null)
                continue;

            game.GameId ??= "";
            game.Round ??= "";
            game.RoundLabel ??= "";
            game.Conference ??= "";
            game.Phase ??= "";
            game.GameType ??= "";
            game.HomeTeamId ??= "";
            game.AwayTeamId ??= "";
            game.HomeTeamName ??= "";
            game.AwayTeamName ??= "";
            game.Status = string.IsNullOrWhiteSpace(game.Status) ? "scheduled" : game.Status;
            game.WinnerTeamId ??= "";
            game.LoserTeamId ??= "";
            PlayoffService.NormalizePlayoffGame(game);
        }

        league.PlayoffBracket.LeagueChampionRecord.ChampionTeamId ??= "";
        league.PlayoffBracket.LeagueChampionRecord.ChampionTeamName ??= "";
        league.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamId ??= "";
        league.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamName ??= "";
        league.PlayoffBracket.LeagueChampionRecord.ChampionshipHomeTeamId ??= "";
        league.PlayoffBracket.LeagueChampionRecord.ChampionshipAwayTeamId ??= "";
        league.PlayoffBracket.LeagueChampionRecord.CompletedPhaseLabel ??= "";

        foreach (var season in league.HistoricalSeasons)
        {
            if (season == null)
                continue;

            season.CompletedPhaseLabel ??= "";
            season.ChampionTeamId ??= "";
            season.ChampionTeamName ??= "";
            season.RunnerUpTeamId ??= "";
            season.RunnerUpTeamName ??= "";
            season.ChampionshipGameLabel ??= "";
            season.GeneratedAtLabel ??= "";
            season.TeamRecords ??= new List<SeasonTeamRecord>();
            season.PlayoffSeeds ??= new List<SeasonPlayoffSeedRecord>();
            season.PlayoffResults ??= new List<SeasonPlayoffResultRecord>();

            foreach (var teamRecord in season.TeamRecords)
            {
                if (teamRecord == null)
                    continue;

                teamRecord.TeamId ??= "";
                teamRecord.TeamName ??= "";
                teamRecord.Abbreviation ??= "";
                teamRecord.Conference ??= "";
                teamRecord.Division ??= "";
            }

            foreach (var seedRecord in season.PlayoffSeeds)
            {
                if (seedRecord == null)
                    continue;

                seedRecord.Conference ??= "";
                seedRecord.TeamId ??= "";
                seedRecord.TeamName ??= "";
                seedRecord.Division ??= "";
            }

            foreach (var playoffResult in season.PlayoffResults)
            {
                if (playoffResult == null)
                    continue;

                playoffResult.Round ??= "";
                playoffResult.Conference ??= "";
                playoffResult.HomeTeamId ??= "";
                playoffResult.HomeTeamName ??= "";
                playoffResult.AwayTeamId ??= "";
                playoffResult.AwayTeamName ??= "";
                playoffResult.WinnerTeamId ??= "";
                playoffResult.WinnerTeamName ??= "";
                playoffResult.LoserTeamId ??= "";
                playoffResult.LoserTeamName ??= "";
            }
        }

        foreach (var retirementSeason in league.RetirementHistory)
        {
            if (retirementSeason == null)
                continue;

            retirementSeason.ProcessedPhase ??= "";
            retirementSeason.Players ??= new List<PlayerRetirementRecord>();
            retirementSeason.RetiredCount = Math.Max(retirementSeason.RetiredCount, retirementSeason.Players.Count(record => record != null));

            foreach (var retirement in retirementSeason.Players)
            {
                if (retirement == null)
                    continue;

                retirement.PlayerId ??= "";
                retirement.PlayerName ??= "";
                retirement.TeamId ??= "";
                retirement.TeamName ??= "";
                retirement.Position ??= "";
                retirement.ReasonLabel ??= "";
                retirement.RetiredDuringPhase ??= "";
                if (retirement.SeasonYear <= 0)
                    retirement.SeasonYear = retirementSeason.SeasonYear;
            }
        }

        foreach (var team in league.Teams)
        {
            if (team == null)
                continue;

            team.TeamId ??= "";
            team.Name ??= "";
            team.Abbreviation ??= "";
            team.Division ??= "";
            team.Conference ??= "";
            team.Roster ??= new List<PlayerState>();
            team.DepthChart = team.DepthChart == null
                ? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, List<string>>(team.DepthChart, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in new List<string>(team.DepthChart.Keys))
                team.DepthChart[pair] ??= new List<string>();

            foreach (var player in team.Roster)
            {
                if (player == null)
                    continue;

                player.PlayerId ??= "";
                player.Name ??= "";
                player.Position ??= "";
                player.Status = string.IsNullOrWhiteSpace(player.Status) ? "Active" : player.Status;
                player.Injury ??= "";
                player.Morale = Math.Clamp(player.Morale, 0, 100);
                player.MoraleTrend = string.IsNullOrWhiteSpace(player.MoraleTrend) ? "Stable" : player.MoraleTrend;
                player.Contract ??= new PlayerContractState();
                player.Contract.ContractType = string.IsNullOrWhiteSpace(player.Contract.ContractType) ? "Standard" : player.Contract.ContractType;
            }
        }

        foreach (var player in league.FreeAgents)
        {
            if (player == null)
                continue;

            player.PlayerId ??= "";
            player.Name ??= "";
            player.Position ??= "";
            player.Status = string.IsNullOrWhiteSpace(player.Status) ? "Free Agent" : player.Status;
            player.Injury ??= "";
            player.Morale = Math.Clamp(player.Morale, 0, 100);
            player.MoraleTrend = string.IsNullOrWhiteSpace(player.MoraleTrend) ? "Stable" : player.MoraleTrend;
            player.Contract ??= new PlayerContractState { ContractType = "Free Agent" };
            player.Contract.ContractType = string.IsNullOrWhiteSpace(player.Contract.ContractType) ? "Free Agent" : player.Contract.ContractType;
        }

        foreach (var prospect in league.CollegeProspects)
        {
            if (prospect == null)
                continue;
            prospect.ProspectId ??= "";
            prospect.Name ??= "";
            prospect.Position ??= "";
            prospect.College ??= "";
            prospect.DraftedByTeamId ??= "";
        }
        league.Draft.Picks ??= new List<DraftPickState>();

        if (isLegacySave)
            ContractService.MigrateLegacyContracts(league);

        foreach (var game in league.Schedule)
        {
            if (game == null)
                continue;

            game.GameId ??= "";
            game.GameType = string.IsNullOrWhiteSpace(game.GameType)
                ? ScheduleService.InferGameTypeFromAbsoluteWeek(game.AbsoluteWeek > 0 ? game.AbsoluteWeek : game.Week)
                : game.GameType;
            game.Phase ??= "";
            game.HomeTeamId ??= "";
            game.AwayTeamId ??= "";
            game.Status = string.IsNullOrWhiteSpace(game.Status) ? "upcoming" : game.Status;
            game.Winner ??= "";
            ScheduleService.NormalizeScheduledGame(game);
            if (isLegacySave)
            {
                game.Phase = ScheduleService.GetPhaseForGameType(game.GameType);
                game.PhaseWeek = ScheduleService.GetDisplayWeek(game.GameType, game.AbsoluteWeek);
                game.WeekLabel = ScheduleService.BuildGameWeekLabel(game.GameType, game.AbsoluteWeek, game.PhaseWeek);
            }
        }

        foreach (var result in league.Results)
        {
            if (result == null)
                continue;

            result.GameId ??= "";
            result.GameType = string.IsNullOrWhiteSpace(result.GameType)
                ? ScheduleService.InferGameTypeFromAbsoluteWeek(result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week)
                : result.GameType;
            result.Phase ??= "";
            result.HomeTeamId ??= "";
            result.AwayTeamId ??= "";
            result.HomeTeam ??= "";
            result.AwayTeam ??= "";
            result.Winner ??= "";
            result.Summary ??= "";
            result.BoxScore ??= new BoxScoreState();
            result.BoxScore.Final ??= "";
            result.BoxScore.TeamStats ??= new Dictionary<string, int>();
            ScheduleService.NormalizeResult(result);
            if (isLegacySave)
            {
                result.Phase = ScheduleService.GetPhaseForGameType(result.GameType);
                result.PhaseWeek = ScheduleService.GetDisplayWeek(result.GameType, result.AbsoluteWeek);
                result.WeekLabel = ScheduleService.BuildGameWeekLabel(result.GameType, result.AbsoluteWeek, result.PhaseWeek);
            }
        }

        if (string.IsNullOrWhiteSpace(league.UserTeamId) || GameCoreStateHelper.ResolveTeam(league, league.UserTeamId) == null)
            league.UserTeamId = league.Teams.Count > 0 ? league.Teams[0].TeamId : "";

        if (league.Teams.Count == 0)
            throw new InvalidDataException("Save file does not contain any teams.");

        if (ShouldRegenerateLegacySchedule(league))
            league.Schedule = LeagueBootstrapService.BuildDeterministicSchedule(league.Teams);

        ScheduleService.NormalizeCalendar(league.Calendar);
        league.SaveVersion = LeagueState.CurrentSaveVersion;

        var context = new GameCoreContext { ActiveLeague = league };
        new ScheduleService(context).RefreshStatuses(league);
        if (ScheduleService.IsSeasonArchivePhase(league.Calendar?.Phase))
            new SeasonHistoryService(context).EnsureSeasonHistorySnapshot(league, out _);
    }

    private static bool ShouldRegenerateLegacySchedule(LeagueState league)
    {
        if (league == null)
            return false;

        if (league.SaveVersion >= LeagueState.CurrentSaveVersion)
            return false;

        if (league.Results != null && league.Results.Count > 0)
            return false;

        if (league.Teams == null || league.Teams.Count != 4)
            return false;

        return league.Schedule == null || league.Schedule.Count < LeagueBootstrapService.ExpectedScheduleGameCount;
    }
}
