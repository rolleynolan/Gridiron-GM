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
        WriteIndented = false,
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

            // Write and flush a sibling before atomic replacement; a failed write preserves the prior save.
            var temporaryPath = absolutePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(stream, context.ActiveLeague, JsonOptions);
                    stream.Flush(flushToDisk: true);
                }
                if (File.Exists(absolutePath)) File.Replace(temporaryPath, absolutePath, null);
                else File.Move(temporaryPath, absolutePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }

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
        league.AvailableCoaches ??= new List<CoachState>();
        if (isLegacySave && league.AvailableCoaches.Count == 0)
            league.AvailableCoaches = LeagueBootstrapService.CreateStaffMarket(league.FranchiseMetadata?.World?.Seed ?? WorldDefinition.StandardSeed);
        league.Waivers ??= new List<WaiverClaimState>();
        league.CollegeProspects ??= new List<CollegeProspectState>();
        league.CollegeUniverse ??= new CollegeUniverseState();
        league.CollegeSeasonArchives ??= new List<CollegeSeasonArchiveRecord>();
        league.Draft ??= new DraftState();
        league.HistoricalDrafts ??= new List<DraftState>();
        NormalizeDraftPickOwnership(league.Draft);
        foreach (var historicalDraft in league.HistoricalDrafts)
            NormalizeDraftPickOwnership(historicalDraft);
        league.Schedule ??= new List<ScheduledGame>();
        league.Results ??= new List<GameResult>();
        league.ActiveLiveGameSession ??= new LiveGameSessionState();
        league.ActiveLiveGameSession.GameId ??= "";
        league.ActiveLiveGameSession.PendingResult ??= new GameResult();
        league.ActiveLiveGameSession.PlayedEvents ??= new List<GamePlayEventState>();
        league.ActiveLiveGameSession.Adjustments ??= new List<LiveGameAdjustmentState>();
        league.TradeMarket ??= new TradeMarketState();
        league.TradeMarket.Phase ??= "";
        league.TradeMarket.SubmittedDate ??= "";
        league.TradeMarket.RequestedPosition ??= "";
        league.TradeMarket.OfferedPlayerIds ??= new List<string>();
        league.TradeMarket.OfferedPickOverallNumbers ??= new List<int>();
        league.TradeMarket.Offers ??= new List<TradeMarketOfferState>();
        foreach (var offer in league.TradeMarket.Offers)
        {
            if (offer == null) continue;
            offer.OfferId ??= ""; offer.PartnerTeamId ??= ""; offer.Rationale ??= ""; offer.Status ??= "open";
            offer.PartnerPlayerIds ??= new List<string>(); offer.PartnerPickOverallNumbers ??= new List<int>();
        }
        league.RookieMinicamp ??= new RookieMinicampState();
        league.RookieMinicamp.InvitedPlayerIds ??= new List<string>();
        league.ActiveLiveGameSession.PendingResult.BoxScore ??= new BoxScoreState();
        league.ActiveLiveGameSession.PendingResult.BoxScore.Final ??= "";
        league.ActiveLiveGameSession.PendingResult.BoxScore.TeamStats ??= new Dictionary<string, int>();
        league.ActiveLiveGameSession.PendingResult.BoxScore.PlayerStats ??= new List<PlayerGameStats>();
        league.ActiveLiveGameSession.PendingResult.BoxScore.PlayByPlay ??= new List<GamePlayEventState>();
        var live = league.ActiveLiveGameSession;
        if (live.PendingResult.ProGame != null)
        {
            if (live.PendingResult.ProGame.RulesVersion != ProGameState.LegacyRulesVersion
                && live.PendingResult.ProGame.RulesVersion != ProGameState.ClockRulesVersion)
                throw new InvalidDataException("This live game's simulation rules are not supported by this build.");
            live.NextEventIndex = live.PendingResult.BoxScore.PlayByPlay.Count;
            live.PendingResult.ProGame.PendingDecision ??= new ProGameDecision();
            live.PendingResult.ProGame.InjuredPlayerIds ??= new List<string>();
            live.PendingResult.ProGame.OvertimePossessions ??= new List<string>();
            live.PendingResult.ProGame.Drives ??= new List<GameDriveState>();
        }
        if (live.Active && league.Results.Any(r => r.GameId == live.GameId))
        {
            live.Active = false; live.Completed = true; live.IsPaused = true;
            live.PendingResult = league.Results.First(r => r.GameId == live.GameId);
        }
        league.ActiveLiveGameSession.NextEventIndex = Math.Clamp(
            league.ActiveLiveGameSession.NextEventIndex,
            0,
            league.ActiveLiveGameSession.PendingResult.BoxScore.PlayByPlay.Count);
        foreach (var play in league.ActiveLiveGameSession.PlayedEvents.Concat(league.ActiveLiveGameSession.PendingResult.BoxScore.PlayByPlay))
        {
            if (play == null)
                continue;
            play.PossessionTeamId ??= "";
            play.Description ??= "";
        }
        foreach (var adjustment in league.ActiveLiveGameSession.Adjustments)
        {
            if (adjustment == null)
                continue;
            adjustment.TeamId ??= "";
            adjustment.Position ??= "";
            adjustment.PlayerId ??= "";
            adjustment.TargetPlayerId ??= "";
            adjustment.Action ??= "";
        }
        league.PlayoffBracket ??= new PlayoffBracket();
        league.HistoricalSeasons ??= new List<SeasonHistoryRecord>();
        league.RetirementHistory ??= new List<SeasonRetirementRecord>();
        league.Transactions ??= new List<TransactionRecord>();
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
            team.InjuredReserve ??= new List<PlayerState>();
            team.PracticeSquad ??= new List<PlayerState>();
            team.Coaches ??= new List<CoachState>();
            team.DepthChart ??= new Dictionary<string, List<string>>();
            team.DepthChartLockedPositions ??= new List<string>();
            team.DepthChartLockedPositions = team.DepthChartLockedPositions
                .Where(position => !string.IsNullOrWhiteSpace(position) && team.DepthChart.ContainsKey(position))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            team.FranchiseTagPlayerId ??= "";
            if (team.FranchiseTagSeason != league.SeasonYear)
            {
                team.FranchiseTagSeason = 0;
                team.FranchiseTagPlayerId = "";
            }
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
            season.Awards ??= new List<SeasonAwardRecord>();

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
            foreach (var award in season.Awards)
            {
                if (award == null) continue;
                award.AwardName ??= ""; award.PlayerId ??= ""; award.PlayerName ??= ""; award.TeamId ??= ""; award.TeamName ??= ""; award.Position ??= ""; award.Summary ??= "";
            }
            SeasonAwardsService.EnsureAwards(league, season);
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
                retirement.CurrentSeasonStats ??= new PlayerSeasonStats();
                retirement.CareerStats ??= new List<PlayerSeasonStats>();
                NormalizePlayerStatistics(new PlayerState { SeasonStats = retirement.CurrentSeasonStats, CareerStats = retirement.CareerStats });
                if (retirement.SeasonYear <= 0)
                    retirement.SeasonYear = retirementSeason.SeasonYear;
            }
        }

        foreach (var transaction in league.Transactions)
        {
            if (transaction == null)
                continue;

            transaction.TransactionId ??= "";
            transaction.DateLabel ??= "";
            transaction.Phase ??= "";
            transaction.Type ??= "";
            transaction.TeamId ??= "";
            transaction.TeamName ??= "";
            transaction.PlayerId ??= "";
            transaction.PlayerName ??= "";
            transaction.StaffId ??= "";
            transaction.StaffName ??= "";
            transaction.Details ??= "";
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
            team.InjuredReserve ??= new List<PlayerState>();
            team.PracticeSquad ??= new List<PlayerState>();
            team.Coaches ??= new List<CoachState>();
            team.TrainingCamp ??= new TrainingCampState();
            team.TrainingCamp.FocusPosition ??= "";
            team.TrainingCamp.FocusPlayerId ??= "";
            team.TrainingCamp.FocusPlayerName ??= "";
            team.TrainingCamp.Summary ??= "";
            team.TrainingCamp.Report ??= new TrainingCampReportState();
            team.TrainingCamp.Report.Summary ??= "";
            team.TrainingCamp.Report.RecommendedFocusPosition ??= "";
            team.TrainingCamp.Report.Positions ??= new List<TrainingCampPositionReport>();
            team.TrainingCamp.UserAdjustedPositions ??= new List<string>();
            team.TrainingCamp.PositionBattles ??= new List<PositionBattleOutcome>();
            foreach (var report in team.TrainingCamp.Report.Positions)
            {
                if (report == null)
                    continue;
                report.Position ??= "";
                report.Recommendation ??= "";
            }
            team.DepthChart = team.DepthChart == null
                ? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, List<string>>(team.DepthChart, StringComparer.OrdinalIgnoreCase);

            foreach (var coach in team.Coaches.Where(coach => coach != null))
                NormalizeCoach(coach, coach.Role);

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
                NormalizePlayerInjury(player);
                player.Fatigue = Math.Clamp(player.Fatigue, 0, 100);
                player.Morale = Math.Clamp(player.Morale, 0, 100);
                player.MoraleTrend = string.IsNullOrWhiteSpace(player.MoraleTrend) ? "Stable" : player.MoraleTrend;
                player.Trait ??= "";
                player.Contract ??= new PlayerContractState();
                player.Contract.ContractType = string.IsNullOrWhiteSpace(player.Contract.ContractType) ? "Standard" : player.Contract.ContractType;
                NormalizePlayerStatistics(player);
            }

            NormalizeReservePlayers(team.InjuredReserve, "IR");
            NormalizeReservePlayers(team.PracticeSquad, "Practice Squad");
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
            NormalizePlayerInjury(player);
            player.Fatigue = Math.Clamp(player.Fatigue, 0, 100);
            NormalizePlayerInjury(player);
            player.Morale = Math.Clamp(player.Morale, 0, 100);
            player.MoraleTrend = string.IsNullOrWhiteSpace(player.MoraleTrend) ? "Stable" : player.MoraleTrend;
            player.Trait ??= "";
            player.Contract ??= new PlayerContractState { ContractType = "Free Agent" };
            player.Contract.ContractType = string.IsNullOrWhiteSpace(player.Contract.ContractType) ? "Free Agent" : player.Contract.ContractType;
            NormalizePlayerStatistics(player);
        }

        foreach (var coach in league.AvailableCoaches.Where(coach => coach != null))
            NormalizeCoach(coach, "Available Staff");

        foreach (var waiver in league.Waivers)
        {
            if (waiver == null)
                continue;
            waiver.Player ??= new PlayerState();
            waiver.WaivedByTeamId ??= "";
            waiver.PendingClaimTeamId ??= "";
            waiver.ConditionalReleasePlayerId ??= "";
            waiver.DeclinedTeamIds ??= new List<string>();
            waiver.Claims ??= new List<WaiverClaimEntryState>();
            foreach (var claim in waiver.Claims)
            {
                if (claim == null) continue;
                claim.TeamId ??= "";
                claim.ConditionalReleasePlayerId ??= "";
            }
            waiver.Player.PlayerId ??= "";
            waiver.Player.Name ??= "";
            waiver.Player.Position ??= "";
            waiver.Player.Contract ??= new PlayerContractState();
            NormalizePlayerInjury(waiver.Player);
            NormalizePlayerStatistics(waiver.Player);
        }

        foreach (var prospect in league.CollegeProspects)
        {
            if (prospect == null)
                continue;
            prospect.ProspectId ??= "";
            prospect.Name ??= "";
            prospect.Position ??= "";
            prospect.College ??= "";
            prospect.CollegeTeamId ??= "";
            prospect.CollegePlayerId ??= "";
            prospect.DeclarationStatus = string.IsNullOrWhiteSpace(prospect.DeclarationStatus) ? "Declared" : prospect.DeclarationStatus;
            prospect.DeclarationRationale ??= "";
            prospect.DraftStock = string.IsNullOrWhiteSpace(prospect.DraftStock) ? "Season outlook pending" : prospect.DraftStock;
            prospect.CollegeCareerStats ??= new List<CollegePlayerSeasonStats>();
            NormalizeCollegeStatistics(prospect.CollegeCareerStats);
            prospect.DraftedByTeamId ??= "";
        }
        NormalizeCollegeUniverse(league);
        foreach (var archive in league.CollegeSeasonArchives.Where(archive => archive != null))
        {
            archive.PostseasonRuleVersion ??= ""; archive.ChampionTeamId ??= ""; archive.ChampionTeamName ??= ""; archive.TeamRecords ??= new List<CollegeTeamSeasonRecord>(); archive.Awards ??= new List<CollegeSeasonAwardRecord>(); archive.PostseasonGames ??= new List<CollegePostseasonGame>();
            archive.TeamRecords = archive.TeamRecords.Where(record => record != null).ToList();
            foreach (var record in archive.TeamRecords)
            {
                record.SeasonYear = record.SeasonYear <= 0 ? archive.SeasonYear : record.SeasonYear;
                record.TeamId ??= ""; record.TeamName ??= ""; record.Conference ??= "";
                record.Wins = Math.Max(0, record.Wins); record.Losses = Math.Max(0, record.Losses); record.FinalRanking = Math.Max(0, record.FinalRanking);
            }
        }
        ProspectEvaluationService.EnsureEvaluations(league);
        league.Draft.Picks ??= new List<DraftPickState>();
        league.Draft.RecapEntries ??= new List<DraftClassRecapEntry>();
        league.Draft.UserBoardProspectIds ??= new List<string>();
        league.Draft.UserBoardProspectIds = league.Draft.UserBoardProspectIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToList();
        league.Draft.UserBoardNotes ??= new Dictionary<string, string>();
        league.Draft.UserBoardTags ??= new Dictionary<string, string>();
        league.Draft.UserBoardTiers ??= new Dictionary<string, string>();
        foreach (var pick in league.Draft.Picks)
        {
            if (pick == null)
                continue;

            pick.TeamId ??= "";
            pick.ProspectId ??= "";
            pick.PlayerId ??= "";
        }
        foreach (var historicalDraft in league.HistoricalDrafts)
        {
            if (historicalDraft == null)
                continue;

            historicalDraft.Picks ??= new List<DraftPickState>();
            historicalDraft.RecapEntries ??= new List<DraftClassRecapEntry>();
            historicalDraft.UserBoardProspectIds ??= new List<string>();
            historicalDraft.UserBoardNotes ??= new Dictionary<string, string>();
            historicalDraft.UserBoardTags ??= new Dictionary<string, string>();
            historicalDraft.UserBoardTiers ??= new Dictionary<string, string>();
            foreach (var pick in historicalDraft.Picks)
            {
                if (pick == null)
                    continue;

                pick.TeamId ??= "";
                pick.ProspectId ??= "";
                pick.PlayerId ??= "";
            }
        }

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
            result.BoxScore.PlayerStats ??= new List<PlayerGameStats>();
            result.BoxScore.PlayByPlay ??= new List<GamePlayEventState>();
            foreach (var stat in result.BoxScore.PlayerStats)
            {
                if (stat == null)
                    continue;
                stat.PlayerId ??= "";
                stat.PlayerName ??= "";
                stat.TeamId ??= "";
                stat.Position ??= "";
            }
            foreach (var play in result.BoxScore.PlayByPlay)
            {
                if (play == null)
                    continue;
                play.PossessionTeamId ??= "";
                play.Description ??= "";
            }
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

    private static void NormalizeCollegeUniverse(LeagueState league)
    {
        var createdFromLegacySave = league.CollegeUniverse.Teams == null || league.CollegeUniverse.Teams.Count == 0;
        if (createdFromLegacySave)
            league.CollegeUniverse = CollegeUniverseService.CreateInitial(league);

        var universe = league.CollegeUniverse;
        universe.SeasonYear = universe.SeasonYear <= 0 ? league.SeasonYear : universe.SeasonYear;
        universe.Teams ??= new List<CollegeTeamState>();
        universe.Players ??= new List<CollegePlayerState>();
        universe.Schedule ??= new List<CollegeScheduledGame>();
        universe.Results ??= new List<CollegeGameResult>();
        universe.Postseason ??= new CollegePostseasonState();
        universe.Postseason.RuleVersion ??= "";
        universe.Postseason.Games ??= new List<CollegePostseasonGame>();
        foreach (var game in universe.Postseason.Games.Where(game => game != null))
        {
            game.Label ??= "";
            game.Stage ??= "";
            game.HomeTeamId ??= "";
            game.AwayTeamId ??= "";
            game.WinnerTeamId ??= "";
        }
        universe.Awards ??= new List<CollegeSeasonAwardRecord>();
        universe.Transfers ??= new List<CollegeTransferRecord>();
        universe.RecruitingClass ??= new List<CollegeRecruitingRecord>();
        universe.CoachingChanges ??= new List<CollegeCoachChangeRecord>();
        universe.Transfers = universe.Transfers.Where(record => record != null).ToList();
        foreach (var transfer in universe.Transfers)
            NormalizeCollegeTransfer(transfer, universe.SeasonYear);
        universe.RecruitingClass = universe.RecruitingClass.Where(record => record != null).ToList();
        foreach (var recruit in universe.RecruitingClass)
        {
            recruit.SeasonYear = recruit.SeasonYear <= 0 ? universe.SeasonYear : recruit.SeasonYear;
            recruit.PlayerId ??= "";
            recruit.PlayerName ??= "";
            recruit.Position ??= "";
            recruit.TeamId ??= "";
            recruit.PublicTier ??= "";
            recruit.Summary ??= "";
        }
        universe.CoachingChanges = universe.CoachingChanges.Where(record => record != null).ToList();
        foreach (var change in universe.CoachingChanges)
        {
            change.SeasonYear = change.SeasonYear <= 0 ? universe.SeasonYear : change.SeasonYear;
            change.TeamId ??= "";
            change.PreviousCoachName ??= "";
            change.NewCoachName ??= "";
            change.Reason ??= "";
        }
        GeneratedNamePools collegeCoachNames = null;
        foreach (var team in universe.Teams.Where(team => team != null))
        {
            team.TeamId ??= ""; team.Name ??= ""; team.Abbreviation ??= ""; team.Conference ??= "";
            team.Wins = Math.Max(0, team.Wins); team.Losses = Math.Max(0, team.Losses); team.Ranking = Math.Max(0, team.Ranking);
            if (team.HeadCoach == null || string.IsNullOrWhiteSpace(team.HeadCoach.CoachId) || string.IsNullOrWhiteSpace(team.HeadCoach.Name))
            {
                collegeCoachNames ??= NamePoolService.Load();
                team.HeadCoach = CollegeCoachCarouselService.CreateCoach(league, team, collegeCoachNames, 0);
            }
            team.HeadCoach.CoachId ??= "";
            team.HeadCoach.Name ??= "";
            team.HeadCoach.Age = Math.Clamp(team.HeadCoach.Age <= 0 ? 45 : team.HeadCoach.Age, 30, 80);
            team.HeadCoach.ProgramRating = Math.Clamp(team.HeadCoach.ProgramRating <= 0 ? 65 : team.HeadCoach.ProgramRating, 40, 99);
            team.HeadCoach.RecruitingRating = Math.Clamp(team.HeadCoach.RecruitingRating <= 0 ? 65 : team.HeadCoach.RecruitingRating, 40, 99);
            team.HeadCoach.HiredSeasonYear = team.HeadCoach.HiredSeasonYear <= 0 ? universe.SeasonYear : team.HeadCoach.HiredSeasonYear;
            team.HeadCoach.SeasonsAtProgram = Math.Max(1, team.HeadCoach.SeasonsAtProgram);
        }
        foreach (var player in universe.Players.Where(player => player != null))
        {
            player.PlayerId ??= ""; player.Name ??= ""; player.TeamId ??= ""; player.Position ??= "";
            player.ClassYear = Math.Clamp(player.ClassYear, 1, 4);
            player.CollegeYear = player.CollegeYear <= 0 ? player.ClassYear : Math.Clamp(player.CollegeYear, 1, 5);
            player.PlayableSeasonsUsed = player.PlayableSeasonsUsed <= 0 && !player.IsRedshirted
                ? player.ClassYear
                : Math.Clamp(player.PlayableSeasonsUsed, 0, 4);
            if (player.IsRedshirted)
            {
                player.GamesPlayed = 0;
                player.PassingYards = 0;
                player.RushingYards = 0;
                player.ReceivingYards = 0;
                player.Touchdowns = 0;
            }
            player.GamesPlayed = Math.Max(0, player.GamesPlayed);
            player.DraftDecision = string.IsNullOrWhiteSpace(player.DraftDecision) ? (player.DraftEligible ? "Declared" : "Pending") : player.DraftDecision;
            player.DraftDecisionReason ??= "";
            player.DraftStock = string.IsNullOrWhiteSpace(player.DraftStock) ? "Undeclared" : player.DraftStock;
            player.CareerStats ??= new List<CollegePlayerSeasonStats>();
            player.CareerStats = player.CareerStats.Where(record => record != null).ToList();
            foreach (var stats in player.CareerStats)
            {
                stats.SeasonYear = Math.Max(0, stats.SeasonYear); stats.TeamId ??= "";
                stats.GamesPlayed = Math.Max(0, stats.GamesPlayed); stats.PassingYards = Math.Max(0, stats.PassingYards);
                stats.RushingYards = Math.Max(0, stats.RushingYards); stats.ReceivingYards = Math.Max(0, stats.ReceivingYards); stats.Touchdowns = Math.Max(0, stats.Touchdowns);
            }
            player.DevelopmentHistory ??= new List<CollegePlayerDevelopmentRecord>();
            player.DevelopmentHistory = player.DevelopmentHistory.Where(record => record != null).ToList();
            foreach (var record in player.DevelopmentHistory)
            {
                record.SeasonYear = Math.Max(0, record.SeasonYear);
                record.OverallBefore = Math.Clamp(record.OverallBefore, 40, 99);
                record.OverallAfter = Math.Clamp(record.OverallAfter, 40, 99);
                record.Reason ??= "";
            }
            player.TransferHistory ??= new List<CollegeTransferRecord>();
            player.TransferHistory = player.TransferHistory.Where(record => record != null).ToList();
            foreach (var transfer in player.TransferHistory)
                NormalizeCollegeTransfer(transfer, universe.SeasonYear);
            player.RecruitingSummary ??= "";
            player.CurrentInjury ??= new CollegePlayerInjuryState();
            player.CurrentInjury.Name ??= "";
            player.CurrentInjury.WeeksRemaining = Math.Max(0, player.CurrentInjury.WeeksRemaining);
            player.CurrentInjury.OccurredInWeek = Math.Max(0, player.CurrentInjury.OccurredInWeek);
            player.CurrentInjury.GameId ??= "";
            player.InjuryHistory ??= new List<CollegePlayerInjuryRecord>();
            player.InjuryHistory = player.InjuryHistory.Where(record => record != null).ToList();
            foreach (var injury in player.InjuryHistory)
            {
                injury.SeasonYear = Math.Max(0, injury.SeasonYear);
                injury.Name ??= "";
                injury.WeeksOut = Math.Max(0, injury.WeeksOut);
                injury.OccurredInWeek = Math.Max(0, injury.OccurredInWeek);
                injury.RecoveredInWeek = Math.Max(0, injury.RecoveredInWeek);
                injury.GameId ??= "";
            }
        }
        foreach (var game in universe.Schedule.Where(game => game != null))
        {
            game.GameId ??= ""; game.HomeTeamId ??= ""; game.AwayTeamId ??= "";
            game.Status = string.IsNullOrWhiteSpace(game.Status) ? "upcoming" : game.Status;
        }
        foreach (var result in universe.Results.Where(result => result != null))
        {
            result.GameId ??= ""; result.HomeTeamId ??= ""; result.AwayTeamId ??= ""; result.WinnerTeamId ??= "";
            result.PlayerStats ??= new List<CollegeGamePlayerStatLine>();
            result.PlayerStats = result.PlayerStats.Where(line => line != null).ToList();
            foreach (var line in result.PlayerStats)
            {
                line.PlayerId ??= "";
                line.PlayerName ??= "";
                line.TeamId ??= "";
                line.Position ??= "";
                line.PassingYards = Math.Max(0, line.PassingYards);
                line.RushingYards = Math.Max(0, line.RushingYards);
                line.ReceivingYards = Math.Max(0, line.ReceivingYards);
                line.Touchdowns = Math.Max(0, line.Touchdowns);
            }
        }
        foreach (var game in universe.Postseason.Games.Where(game => game != null))
        {
            game.Label ??= ""; game.HomeTeamId ??= ""; game.AwayTeamId ??= ""; game.WinnerTeamId ??= "";
        }
        foreach (var award in universe.Awards.Where(award => award != null))
        {
            award.AwardName ??= ""; award.PlayerId ??= ""; award.PlayerName ??= ""; award.TeamId ??= "";
            award.TeamName ??= ""; award.Position ??= ""; award.Summary ??= "";
        }
        CollegeAwardsService.EnsureAwards(universe);
        if (createdFromLegacySave)
            new CollegeUniverseService(new GameCoreContext { ActiveLeague = league }).AdvanceToProWeek(Math.Max(0, league.Calendar?.AbsoluteWeek ?? 0));
    }

    private static void NormalizeReservePlayers(IEnumerable<PlayerState> players, string status)
    {
        foreach (var player in players ?? Enumerable.Empty<PlayerState>())
        {
            if (player == null)
                continue;

            player.PlayerId ??= "";
            player.Name ??= "";
            player.Position ??= "";
            player.Status = status;
            player.Injury ??= "";
            player.Fatigue = Math.Clamp(player.Fatigue, 0, 100);
            player.Morale = Math.Clamp(player.Morale, 0, 100);
            player.MoraleTrend = string.IsNullOrWhiteSpace(player.MoraleTrend) ? "Stable" : player.MoraleTrend;
            player.Trait ??= "";
            player.Contract ??= new PlayerContractState();
            player.Contract.ContractType = string.IsNullOrWhiteSpace(player.Contract.ContractType) ? "Standard" : player.Contract.ContractType;
            NormalizePlayerStatistics(player);
        }
    }

    private static void NormalizeCollegeTransfer(CollegeTransferRecord transfer, int fallbackSeasonYear)
    {
        transfer.SeasonYear = transfer.SeasonYear <= 0 ? fallbackSeasonYear : transfer.SeasonYear;
        transfer.PlayerId ??= "";
        transfer.PlayerName ??= "";
        transfer.Position ??= "";
        transfer.FromTeamId ??= "";
        transfer.ToTeamId ??= "";
        transfer.Reason ??= "";
    }

    private static void NormalizeCoach(CoachState coach, string fallbackRole)
    {
        coach.CoachId ??= "";
        coach.Name ??= "";
        coach.Role = string.IsNullOrWhiteSpace(coach.Role) ? fallbackRole : coach.Role;
        coach.Overall = Math.Clamp(coach.Overall, 1, 99);
        coach.Age = Math.Clamp(coach.Age, 20, 90);
        coach.TenureStartSeason = Math.Max(0, coach.TenureStartSeason);
        HeadCoachAuthorityService.Normalize(coach);
    }

    private static void NormalizeDraftPickOwnership(DraftState draft)
    {
        if (draft == null)
            return;

        draft.Picks ??= new List<DraftPickState>();
        foreach (var pick in draft.Picks.Where(pick => pick != null))
        {
            pick.TeamId ??= "";
            pick.OriginalTeamId = string.IsNullOrWhiteSpace(pick.OriginalTeamId) ? pick.TeamId : pick.OriginalTeamId;
            pick.ProspectId ??= "";
            pick.PlayerId ??= "";
        }
    }

    private static void NormalizePlayerStatistics(PlayerState player)
    {
        player.Potential = PlayerDevelopmentService.ResolvePotential(player);
        player.College ??= "";
        player.CollegePlayerId ??= "";
        player.CollegeCareerStats ??= new List<CollegePlayerSeasonStats>();
        NormalizeCollegeStatistics(player.CollegeCareerStats);
        player.SeasonStats ??= new PlayerSeasonStats();
        player.CareerStats ??= new List<PlayerSeasonStats>();
        player.CareerStats = player.CareerStats.Where(stat => stat != null).ToList();
        player.DevelopmentHistory ??= new List<PlayerDevelopmentRecord>();
        player.DevelopmentHistory = player.DevelopmentHistory.Where(record => record != null).ToList();
        foreach (var record in player.DevelopmentHistory) { record.SeasonYear = Math.Max(0, record.SeasonYear); record.OverallBefore = Math.Clamp(record.OverallBefore, 40, 99); record.OverallAfter = Math.Clamp(record.OverallAfter, 40, 99); record.Note ??= ""; }
    }

    private static void NormalizeCollegeStatistics(List<CollegePlayerSeasonStats> records)
    {
        records.RemoveAll(record => record == null);
        foreach (var stats in records)
        {
            stats.SeasonYear = Math.Max(0, stats.SeasonYear); stats.TeamId ??= "";
            stats.GamesPlayed = Math.Max(0, stats.GamesPlayed); stats.PassingYards = Math.Max(0, stats.PassingYards);
            stats.RushingYards = Math.Max(0, stats.RushingYards); stats.ReceivingYards = Math.Max(0, stats.ReceivingYards); stats.Touchdowns = Math.Max(0, stats.Touchdowns);
        }
    }

    private static void NormalizePlayerInjury(PlayerState player)
    {
        player.CurrentInjury ??= new PlayerInjuryState();
        player.InjuryHistory ??= new List<PlayerInjuryRecord>();
        player.InjuryHistory = player.InjuryHistory.Where(record => record != null).ToList();
        player.CurrentInjury.Name ??= "";
        player.CurrentInjury.OccurredOn ??= "";
        player.CurrentInjury.GameId ??= "";
        player.CurrentInjury.DaysRemaining = Math.Max(0, player.CurrentInjury.DaysRemaining);
        if (!player.CurrentInjury.IsActive && !string.IsNullOrWhiteSpace(player.Injury))
        {
            player.CurrentInjury.Name = player.Injury;
            player.CurrentInjury.DaysRemaining = 1;
        }
        if (player.CurrentInjury.IsActive)
        {
            player.Injury = player.CurrentInjury.Name;
            if (string.Equals(player.Status, "Active", StringComparison.OrdinalIgnoreCase))
                player.Status = "Injured";
        }
        else if (string.Equals(player.Status, "Injured", StringComparison.OrdinalIgnoreCase))
        {
            player.Status = "Active";
            player.Injury = "";
        }
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
