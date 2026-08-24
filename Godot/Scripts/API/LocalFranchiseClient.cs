using Godot;
using GridironGM.Domain;
using GridironGM.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GridironGM.Client.Api
{
    public partial class LocalFranchiseClient : Node, IBackendClient
    {
        private const string SavePath = "user://franchise_v1.json";
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        private readonly GmProfileStore _profileStore = new();
        private FranchiseState _state;

        public override void _Ready()
        {
            _state = Load();
            EnsureState();
        }

        public Task<(int status, string body)> GetAsync(string path)
        {
            EnsureState();
            return Task.FromResult(DispatchGet(path));
        }

        public Task<(int status, string body)> PostAsync(string path, string jsonBody = "{}")
        {
            EnsureState();
            return Task.FromResult(DispatchPost(path, jsonBody));
        }

        private (int, string) DispatchGet(string path)
        {
            var uri = new Uri("http://local" + path);
            var route = uri.AbsolutePath;
            if (TryHandleTeamGet(uri, out var teamResult))
                return teamResult;
            if (TryHandleGameGet(uri, out var gameResult))
                return gameResult;

            return route switch
            {
                "/health" => Ok(new { ok = true, runtime = "native_csharp", save_name = "franchise_v1.json" }),
                "/state_summary" => Ok(StateSummary()),
                "/dashboard_state" => Ok(DashboardState()),
                "/league_history" => Ok(new { ok = true, history = _state.SeasonHistory }),
                "/gm_profile" => Ok(GmProfilePayload()),
                "/gm_profiles" => Ok(new { profiles = _profileStore.LoadAll() }),
                "/standings" => Ok(new { ok = true, standings = Standings() }),
                "/results" => Ok(Results(QueryValue(uri, "week_key"))),
                "/team_schedule" => Ok(TeamSchedule(QueryValue(uri, "team_id"))),
                "/team_roster" => Ok(BuildRosterPayload(_state.UserTeamId)),
                "/team_depth_chart" => Ok(BuildDepthChartPayload(_state.UserTeamId)),
                "/injury_report" => Ok(BuildInjuryReportPayload(QueryValue(uri, "team_id"))),
                "/game_result" => GameResult(QueryValue(uri, "game_id")),
                _ => Error(404, "not_found")
            };
        }

        private (int, string) DispatchPost(string path, string jsonBody)
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody);
            var data = document.RootElement;
            switch (path)
            {
                case "/new_game":
                    _state = FranchiseState.CreateNew(ResolveWorldDefinition(data), ResolveProfile(data));
                    Save();
                    return Ok(new { ok = true, metadata = _state.Metadata });
                case "/gm_profiles":
                    try
                    {
                        var profile = CreateOrUpdateProfile(data);
                        _profileStore.Save(profile);
                        return Ok(new { ok = true, profile });
                    }
                    catch (ArgumentException exception)
                    {
                        return Error(400, exception.Message);
                    }
                case "/reset_save":
                    _state = FranchiseState.CreateNew();
                    Save();
                    return Ok(StateSummary());
                case "/set_user_team":
                    var teamId = StringValue(data, "team_id");
                    if (!_state.Teams.Any(team => team.Id == teamId))
                        return Error(400, "team_not_found");
                    _state.UserTeamId = teamId;
                    Save();
                    return Ok(new { ok = true, team_id = teamId });
                case "/continue":
                    return ContinueSimulation(data);
                case "/advance_day":
                    return AdvanceOneDay();
                case "/sim_until":
                    return ContinueSimulation(data);
                case "/simulate_user_game":
                    return SimulateSelectedGame(StringValue(data, "game_id"));
                case "/auto_fill_depth_chart":
                    return AutoFillDepthChart(StringValue(data, "team_id"));
                case "/update_depth_chart":
                    return UpdateDepthChart(data);
                case "/inbox/mark_read":
                    return AcknowledgeNotification(StringValue(data, "message_id"));
                default:
                    return Error(404, "not_found");
            }
        }

        private bool TryHandleTeamGet(Uri uri, out (int, string) result)
        {
            result = default;
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3 || !string.Equals(segments[0], "team", StringComparison.OrdinalIgnoreCase))
                return false;

            var teamId = segments[1];
            if (!_state.Teams.Any(team => team.Id == teamId))
            {
                result = Error(404, "team_not_found");
                return true;
            }

            if (string.Equals(segments[2], "roster", StringComparison.OrdinalIgnoreCase))
            {
                result = Ok(BuildRosterPayload(teamId));
                return true;
            }

            if (string.Equals(segments[2], "depth_chart", StringComparison.OrdinalIgnoreCase))
            {
                result = Ok(BuildDepthChartPayload(teamId));
                return true;
            }

            return false;
        }

        private bool TryHandleGameGet(Uri uri, out (int, string) result)
        {
            result = default;
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 || !string.Equals(segments[0], "game", StringComparison.OrdinalIgnoreCase))
                return false;

            result = GameResult(segments[1]);
            return true;
        }

        private object StateSummary() => new
        {
            milestones = LeagueSliceFactory.GetLeagueCalendarMilestones(_state.SeasonYear)
                .Select(SerializeMilestone),
            today_milestones = LeagueSliceFactory.GetLeagueCalendarMilestonesForDate(_state.SeasonYear, _state.CurrentDate)
                .Where(milestone => !string.Equals(milestone.Id, "new_league_year", StringComparison.OrdinalIgnoreCase))
                .Select(SerializeMilestone),
            next_milestone = SerializeMilestoneOrNull(
                LeagueSliceFactory.GetNextLeagueCalendarMilestone(_state.SeasonYear, _state.CurrentDate)),
            calendar = new
            {
                season_year = _state.SeasonYear,
                current_date = _state.CurrentDate.ToString("yyyy-MM-dd"),
                day_of_week = _state.CurrentDate.DayOfWeek.ToString(),
                week_label = CurrentWeekLabel(),
                football_week = _state.CurrentWeek,
                phase = CurrentPhaseLabel()
            },
            user_team_id = _state.UserTeamId,
            franchise = new
            {
                roster_source = _state.Metadata.World.Source.ToString().ToLowerInvariant(),
                world_seed = _state.Metadata.World.Seed,
                generator_version = _state.Metadata.World.GeneratorVersion
            },
            league = new
            {
                teams = _state.Teams.Select(team => new
                {
                    id = team.Id,
                    abbreviation = team.Abbreviation,
                    city = team.City,
                    team_name = team.Name,
                    conference = team.Conference,
                    division = team.Division
                })
            },
            history = new
            {
                completed_seasons = _state.SeasonHistory.Count,
                latest_champion = _state.SeasonHistory
                    .OrderByDescending(item => item.SeasonYear)
                    .Select(item => item.ChampionDisplayName)
                    .FirstOrDefault() ?? ""
            }
        };

        private object GmProfilePayload()
        {
            var profile = _state.Metadata.GmProfileSnapshot;
            var attributes = profile.Attributes;
            return new
            {
                gm = new
                {
                    id = profile.Id,
                    name = profile.Name,
                    current_role = "General Manager",
                    current_team_id = _state.UserTeamId,
                    reputation = 50,
                    job_security = 70,
                    attributes = new
                    {
                        negotiation = attributes.Negotiation,
                        player_management = attributes.PlayerManagement,
                        scouting_judgment = attributes.ScoutingJudgment,
                        leadership = attributes.Leadership,
                        contract_attractiveness_modifier = attributes.ContractAttractivenessModifier,
                        retention_happiness_modifier = attributes.RetentionHappinessModifier,
                        scouting_uncertainty_modifier = attributes.ScoutingUncertaintyModifier,
                        culture_modifier = attributes.CultureModifier
                    }
                }
            };
        }

        private object DashboardState()
        {
            var team = Team(_state.UserTeamId);
            var roster = TeamPlayers(team.Id);
            var rosterStatus = LeagueSliceFactory.EvaluateRoster(roster);
            var depthChartIssues = LeagueSliceFactory.ValidateDepthChart(FindDepthChart(team.Id), roster);
            var next = GetNextUserGame();
            var recent = _state.Games
                .Where(game => game.Completed && (game.HomeTeamId == team.Id || game.AwayTeamId == team.Id))
                .OrderByDescending(game => game.Week)
                .Take(3)
                .Select(CompactGame);
            var actionItems = BuildDashboardActionItems(team.Id, rosterStatus, depthChartIssues, GetPendingUserGameForDate(_state.CurrentDate));

            return new
            {
                ok = true,
                dashboard = new
                {
                    team = new
                    {
                        id = team.Id,
                        name = $"{team.City} {team.Name}",
                        abbreviation = team.Abbreviation,
                        conference = team.Conference,
                        division = team.Division,
                        record = Record(team.Id)
                    },
                    calendar = new
                    {
                        year = _state.SeasonYear,
                        week = Math.Min(_state.CurrentWeek, _state.MaxWeek),
                        week_label = CurrentWeekLabel(),
                        phase = CurrentPhaseLabel(),
                        current_date = _state.CurrentDate.ToString("yyyy-MM-dd"),
                        day_of_week = _state.CurrentDate.DayOfWeek.ToString()
                    },
                    next_game = next == null ? null : GameForTeam(next, team.Id),
                    team_status = new
                    {
                        roster_size = rosterStatus.RosterSize,
                        injuries = rosterStatus.InjuredCount,
                        cap_room = 0
                    },
                    action_items = actionItems,
                    recent_results = recent
                }
            };
        }

        private object Results(string weekKey)
        {
            var availableWeeks = _state.Games
                .Select(game => game.Week)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            var fallbackWeek = _state.Games
                .Where(game => game.Completed)
                .Select(game => game.Week)
                .DefaultIfEmpty(Math.Max(1, _state.CurrentWeek - 1))
                .Max();
            var week = int.TryParse(weekKey, out var parsed) && availableWeeks.Contains(parsed)
                ? parsed
                : fallbackWeek;
            return new
            {
                week,
                week_key = week.ToString(),
                games = _state.Games.Where(game => game.Week == week).Select(CompactGame),
                available_week_keys = availableWeeks.Select(value => value.ToString()),
                available_week_labels = availableWeeks.Select(LeagueSliceFactory.DescribeWeek),
                completed_week_keys = _state.Games.Where(game => game.Completed).Select(game => game.Week.ToString()).Distinct()
            };
        }

        private object TeamSchedule(string teamId)
        {
            teamId = NormalizeTeamId(teamId);
            var teams = LeagueTeamDefinitions();
            var snapshots = SeasonSnapshots();
            var standings = RegularSeasonStandings();
            var standingLookup = standings.ToDictionary(standing => standing.TeamId, StringComparer.Ordinal);
            var seedLookup = LeagueSliceFactory.BuildConferencePlayoffSeedLookup(teams, standings, snapshots);
            var divisionRanks = LeagueSliceFactory.BuildDivisionRanks(teams, standings, snapshots);
            var conferenceRanks = LeagueSliceFactory.BuildConferenceRanks(teams, standings, snapshots);
            var raceStatuses = LeagueSliceFactory.BuildPlayoffRaceStatuses(teams, standings, snapshots);
            return new
            {
                ok = true,
                schedule = _state.Games
                    .Where(game => game.HomeTeamId == teamId || game.AwayTeamId == teamId)
                    .OrderBy(game => game.Week)
                    .Select(game => GameForTeam(game, teamId, standingLookup, seedLookup, divisionRanks, conferenceRanks, raceStatuses))
            };
        }

        private object BuildRosterPayload(string teamId)
        {
            teamId = NormalizeTeamId(teamId);
            var team = Team(teamId);
            var players = TeamPlayers(teamId);
            var rosterStatus = LeagueSliceFactory.EvaluateRoster(players);
            var orderedPlayers = players
                .OrderBy(player => BucketOrder(player.RosterBucket))
                .ThenBy(player => LeagueSliceFactory.PositionSortOrder(player.Position))
                .ThenByDescending(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new
            {
                ok = true,
                team = TeamSummary(team),
                roster_status = new
                {
                    is_valid = rosterStatus.IsValid,
                    roster_size = rosterStatus.RosterSize,
                    roster_limit = rosterStatus.RosterLimit,
                    required_cuts = rosterStatus.RequiredCuts,
                    injured_count = rosterStatus.InjuredCount,
                    issues = rosterStatus.Issues
                },
                position_counts = rosterStatus.PositionCounts.Select(count => new { position = count.Position, count = count.Count }),
                players = orderedPlayers.Select(PlayerPayload),
                roster = orderedPlayers.Select(PlayerPayload),
                ir_list = orderedPlayers.Where(player => player.OnInjuredReserve).Select(PlayerPayload),
                practice_squad = orderedPlayers.Where(player => string.Equals(player.RosterBucket, "practice_squad", StringComparison.OrdinalIgnoreCase)).Select(PlayerPayload)
            };
        }

        private object BuildDepthChartPayload(string teamId)
        {
            teamId = NormalizeTeamId(teamId);
            var team = Team(teamId);
            var players = TeamPlayers(teamId);
            var depthChart = FindDepthChart(teamId);
            var issues = LeagueSliceFactory.ValidateDepthChart(depthChart, players);

            return new
            {
                ok = true,
                team = TeamSummary(team),
                depth_chart_status = new
                {
                    is_valid = issues.Count == 0,
                    issues
                },
                positions = depthChart.Positions
                    .OrderBy(position => LeagueSliceFactory.PositionSortOrder(position.Position))
                    .Select(position => new
                    {
                        position = position.Position,
                        required_starters = position.RequiredStarters,
                        players = position.PlayerIds
                            .Select(id => players.FirstOrDefault(player => player.Id == id))
                            .Where(player => player != null)
                            .Select((player, index) => new
                            {
                                player_id = player.Id,
                                name = player.Name,
                                overall = player.Overall,
                                role = index < position.RequiredStarters ? "Starter" : index == position.RequiredStarters ? "Top Backup" : "Depth",
                                status = player.OnInjuredReserve ? "ir" : "active",
                                injury = InjuryDisplay(player)
                            })
                    })
            };
        }

        private object BuildInjuryReportPayload(string teamId)
        {
            teamId = NormalizeTeamId(teamId);
            var entries = TeamPlayers(teamId)
                .Where(player => player.Injury != null && !player.Injury.IsHealthy)
                .OrderByDescending(player => player.OnInjuredReserve)
                .ThenBy(player => LeagueSliceFactory.PositionSortOrder(player.Position))
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => new
                {
                    player_id = player.Id,
                    name = player.Name,
                    position = player.Position,
                    injury_status = player.OnInjuredReserve ? "IR" : Humanize(player.Injury.Status),
                    injury_name = string.IsNullOrWhiteSpace(player.Injury.Name) ? "Soreness" : player.Injury.Name,
                    injury_end_date = player.Injury.ReturnLabel,
                    days_remaining = player.Injury.DaysRemaining,
                    on_injured_reserve = player.OnInjuredReserve,
                    ir = player.OnInjuredReserve
                });
            return new { ok = true, entries };
        }

        private (int, string) GameResult(string gameId)
        {
            var game = _state.Games.FirstOrDefault(value => value.Id == gameId);
            return game == null ? Error(404, "game_not_found") : Ok(new { ok = true, game = CompactGame(game), box_score = BuildBoxScore(game) });
        }

        private object Standings()
        {
            var teams = LeagueTeamDefinitions();
            var snapshots = SeasonSnapshots();
            var standings = RegularSeasonStandings();
            var divisionRanks = LeagueSliceFactory.BuildDivisionRanks(teams, standings, snapshots);
            var conferenceRanks = LeagueSliceFactory.BuildConferenceRanks(teams, standings, snapshots);
            var seedLookup = LeagueSliceFactory.BuildConferencePlayoffSeedLookup(teams, standings, snapshots);
            var raceStatuses = LeagueSliceFactory.BuildPlayoffRaceStatuses(teams, standings, snapshots);
            var rows = standings.Select((standing, index) =>
                {
                    var team = Team(standing.TeamId);
                    var divisionRank = divisionRanks.TryGetValue(team.Id, out var resolvedDivisionRank) ? resolvedDivisionRank : 0;
                    var conferenceRank = conferenceRanks.TryGetValue(team.Id, out var resolvedConferenceRank) ? resolvedConferenceRank : 0;
                    var playoffSeed = seedLookup.TryGetValue(team.Id, out var resolvedSeed) ? resolvedSeed : 0;
                    var raceStatus = raceStatuses.TryGetValue(team.Id, out var resolvedRaceStatus)
                        ? resolvedRaceStatus
                        : new PlayoffRaceStatus();
                    return new
                    {
                        rank = index + 1,
                        team_id = team.Id,
                        team = new { id = team.Id, abbreviation = team.Abbreviation, city = team.City, team_name = team.Name },
                        abbreviation = team.Abbreviation,
                        conference = team.Conference,
                        division = team.Division,
                        wins = standing.Wins,
                        losses = standing.Losses,
                        ties = standing.Ties,
                        record = $"{standing.Wins}-{standing.Losses}",
                        points_for = standing.PointsFor,
                        points_against = standing.PointsAgainst,
                        win_pct = standing.WinPct,
                        conference_rank = conferenceRank,
                        division_rank = divisionRank,
                        playoff_seed = playoffSeed > 0 ? playoffSeed : (int?)null,
                        is_division_leader = divisionRank == 1,
                        is_wild_card = playoffSeed >= 5,
                        is_in_hunt = raceStatus.InHunt,
                        is_clinched_division = raceStatus.ClinchedDivision,
                        is_clinched_playoff = raceStatus.ClinchedPlayoff,
                        is_eliminated = raceStatus.Eliminated,
                        remaining_games = raceStatus.RemainingGames,
                        max_wins = raceStatus.MaxWins,
                        status_label = raceStatus.StatusLabel,
                        spot_label = raceStatus.SpotLabel
                    };
                })
                .ToList();

            var divisions = rows
                .GroupBy(row => $"{row.conference}:{row.division}")
                .Select(group => new
                {
                    conference = group.First().conference,
                    division = group.First().division,
                    rows = group.OrderBy(row => row.division_rank == 0 ? int.MaxValue : row.division_rank)
                        .ThenBy(row => row.conference_rank == 0 ? int.MaxValue : row.conference_rank)
                        .ToList()
                })
                .OrderBy(group => group.conference, StringComparer.Ordinal)
                .ThenBy(group => DivisionSortOrder(group.division))
                .ToList();

            return new
            {
                rows,
                divisions
            };
        }

        private object GameForTeam(Game game, string teamId)
        {
            var teams = LeagueTeamDefinitions();
            var snapshots = SeasonSnapshots();
            var standings = RegularSeasonStandings();
            return GameForTeam(
                game,
                teamId,
                standings.ToDictionary(standing => standing.TeamId, StringComparer.Ordinal),
                LeagueSliceFactory.BuildConferencePlayoffSeedLookup(teams, standings, snapshots),
                LeagueSliceFactory.BuildDivisionRanks(teams, standings, snapshots),
                LeagueSliceFactory.BuildConferenceRanks(teams, standings, snapshots),
                LeagueSliceFactory.BuildPlayoffRaceStatuses(teams, standings, snapshots));
        }

        private object GameForTeam(
            Game game,
            string teamId,
            IReadOnlyDictionary<string, TeamStanding> standingLookup,
            IReadOnlyDictionary<string, int> seedLookup,
            IReadOnlyDictionary<string, int> divisionRanks,
            IReadOnlyDictionary<string, int> conferenceRanks,
            IReadOnlyDictionary<string, PlayoffRaceStatus> raceStatuses)
        {
            var home = Team(game.HomeTeamId);
            var away = Team(game.AwayTeamId);
            var isHome = game.HomeTeamId == teamId;
            var opponent = isHome ? away : home;
            var opponentStanding = standingLookup != null && standingLookup.TryGetValue(opponent.Id, out var resolvedStanding)
                ? resolvedStanding
                : new TeamStanding { TeamId = opponent.Id };
            var opponentSeed = seedLookup != null && seedLookup.TryGetValue(opponent.Id, out var resolvedSeed) ? resolvedSeed : 0;
            var opponentDivisionRank = divisionRanks != null && divisionRanks.TryGetValue(opponent.Id, out var resolvedDivisionRank) ? resolvedDivisionRank : 0;
            var opponentConferenceRank = conferenceRanks != null && conferenceRanks.TryGetValue(opponent.Id, out var resolvedConferenceRank) ? resolvedConferenceRank : 0;
            var opponentRaceStatus = raceStatuses != null && raceStatuses.TryGetValue(opponent.Id, out var resolvedRaceStatus)
                ? resolvedRaceStatus
                : new PlayoffRaceStatus();
            var status = game.Completed
                ? "final"
                : IsPendingUserGameForDate(game, _state.CurrentDate)
                    ? "game_day"
                    : "upcoming";
            return new
            {
                game_id = game.Id,
                week = game.Week,
                week_label = LeagueSliceFactory.DescribeWeek(game.Week),
                game_type = NormalizeGameType(game),
                home_away = isHome ? "home" : "away",
                opponent = $"{opponent.City} {opponent.Name}",
                opponent_abbreviation = opponent.Abbreviation,
                opponent_id = opponent.Id,
                home_team = home.Abbreviation,
                away_team = away.Abbreviation,
                home_team_id = home.Id,
                away_team_id = away.Id,
                home_score = game.HomeScore,
                away_score = game.AwayScore,
                status,
                opponent_record = $"{opponentStanding.Wins}-{opponentStanding.Losses}-{opponentStanding.Ties}",
                opponent_playoff_seed = opponentSeed > 0 ? opponentSeed : (int?)null,
                opponent_division_rank = opponentDivisionRank,
                opponent_conference_rank = opponentConferenceRank,
                opponent_status_label = opponentRaceStatus.StatusLabel,
                opponent_spot_label = opponentRaceStatus.SpotLabel
            };
        }

        private object CompactGame(Game game)
        {
            var home = Team(game.HomeTeamId);
            var away = Team(game.AwayTeamId);
            return new
            {
                game_id = game.Id,
                week = game.Week,
                week_label = LeagueSliceFactory.DescribeWeek(game.Week),
                game_type = NormalizeGameType(game),
                home_team = new { id = home.Id, abbreviation = home.Abbreviation, city = home.City, team_name = home.Name },
                away_team = new { id = away.Id, abbreviation = away.Abbreviation, city = away.City, team_name = away.Name },
                home_team_id = home.Id,
                away_team_id = away.Id,
                home_score = game.HomeScore,
                away_score = game.AwayScore,
                status = game.Completed ? "final" : IsPendingUserGameForDate(game, _state.CurrentDate) ? "game_day" : "upcoming",
                winner = game.Completed ? (game.HomeScore > game.AwayScore ? home.Abbreviation : away.Abbreviation) : "",
                box_score = BuildBoxScore(game)
            };
        }

        private object BuildBoxScore(Game game)
        {
            var home = Team(game.HomeTeamId);
            var away = Team(game.AwayTeamId);
            return new
            {
                home_team = new { id = home.Id, abbreviation = home.Abbreviation, city = home.City, team_name = home.Name },
                away_team = new { id = away.Id, abbreviation = away.Abbreviation, city = away.City, team_name = away.Name },
                home_score = game.HomeScore,
                away_score = game.AwayScore,
                final = new { home = game.HomeScore, away = game.AwayScore },
                quarter_scores = new { home = game.HomeQuarterScores, away = game.AwayQuarterScores },
                team_stats = new { home = game.HomeTeamStats, away = game.AwayTeamStats },
                leaders = new { home = game.HomeLeaders, away = game.AwayLeaders }
            };
        }

        private (int, string) AdvanceOneDay()
        {
            SyncCurrentWeekFromDate();
            ProcessCalendarMilestonesForCurrentDate();
            if (TryStartNextSeason())
            {
                Save();
                return Ok(new { ok = true, result = BuildAdvanceResult(0, 0, "new_league_year_started") });
            }

            var simulated = 0;
            if (IsCurrentWeekGameDay(_state.CurrentDate))
            {
                simulated += SimulateAndFinalizeWeek(_state.CurrentWeek);
                ClearContinueStopState();
            }

            _state.CurrentDate = _state.CurrentDate.Date.AddDays(1);
            SyncCurrentWeekFromDate();
            ProcessCalendarMilestonesForCurrentDate();

            var stopReason = TryStartNextSeason() ? "new_league_year_started" : "day_complete";
            Save();
            return Ok(new { ok = true, result = BuildAdvanceResult(1, simulated, stopReason) });
        }

        private (int, string) ContinueSimulation(JsonElement data)
        {
            SyncCurrentWeekFromDate();
            var maxDays = Math.Max(1, IntValue(data, "max_days", 14));
            var targetType = StringValue(data, "target_type");
            var targetWeek = IntValue(data, "target_week", _state.CurrentWeek);
            var targetMilestoneId = StringValue(data, "target_milestone_id");
            var targetMilestoneDate = ResolveMilestoneDate(targetType, targetWeek, targetMilestoneId);

            if (!string.Equals(targetType, "offseason_start", StringComparison.OrdinalIgnoreCase) && TryStartNextSeason())
            {
                Save();
                return Ok(BuildSimUntilResult(0, "new_league_year_started"));
            }

            var daysAdvanced = 0;
            var totalGames = 0;
            var stopReason = "";
            while (daysAdvanced < maxDays)
            {
                SyncCurrentWeekFromDate();

                if (string.Equals(targetType, "playoffs_start", StringComparison.OrdinalIgnoreCase)
                    && _state.CurrentWeek >= LeagueSliceFactory.WildCardWeek)
                {
                    stopReason = "season_phase_changed";
                    break;
                }

                if (string.Equals(targetType, "regular_season_week", StringComparison.OrdinalIgnoreCase)
                    && _state.CurrentWeek > Math.Min(targetWeek, _state.MaxWeek))
                    break;

                if (targetMilestoneDate.HasValue && _state.CurrentDate.Date >= targetMilestoneDate.Value.Date)
                    break;

                if (IsPendingUserGameForDate(_state.CurrentDate))
                {
                    if (!HasShownContinueStopToday("game_day"))
                    {
                        MarkContinueStopShown("game_day");
                        stopReason = "game_day";
                        break;
                    }
                }

                if (ProcessCalendarMilestonesForCurrentDate() > 0 && !HasShownContinueStopToday("calendar_event"))
                {
                    MarkContinueStopShown("calendar_event");
                    stopReason = "calendar_event";
                    break;
                }

                if (IsCurrentWeekGameDay(_state.CurrentDate))
                {
                    totalGames += SimulateAndFinalizeWeek(_state.CurrentWeek);
                    ClearContinueStopState();
                    if (string.Equals(targetType, "offseason_start", StringComparison.OrdinalIgnoreCase) && _state.SeasonArchived)
                    {
                        stopReason = "season_phase_changed";
                        break;
                    }
                }

                _state.CurrentDate = _state.CurrentDate.Date.AddDays(1);
                daysAdvanced++;
                SyncCurrentWeekFromDate();

                if (TryStartNextSeason())
                {
                    stopReason = "new_league_year_started";
                    break;
                }
            }

            Save();
            return Ok(BuildSimUntilResult(totalGames, string.IsNullOrWhiteSpace(stopReason)
                ? (daysAdvanced >= maxDays ? "max_days_reached" : "target_reached")
                : stopReason));
        }

        private (int, string) SimulateSelectedGame(string gameId)
        {
            var game = _state.Games.FirstOrDefault(value => value.Id == gameId);
            if (game == null)
                return Error(404, "game_not_found");
            SimulateGame(game);
            if (IsCurrentWeekGameDay(_state.CurrentDate))
            {
                foreach (var remaining in _state.Games.Where(value => value.Week == game.Week && !value.Completed).ToList())
                    SimulateGame(remaining);
                FinalizeWeekIfReady(game.Week);
                ClearContinueStopState();
            }
            Save();
            return Ok(new { ok = true, game = CompactGame(game), box_score = BuildBoxScore(game) });
        }

        private (int, string) AutoFillDepthChart(string teamId)
        {
            teamId = NormalizeTeamId(teamId);
            var players = TeamPlayers(teamId);
            var depthChart = FindDepthChart(teamId);
            LeagueSliceFactory.AutoFillDepthChart(depthChart, players);
            AddNotification(new FranchiseNotification
            {
                Id = CreateNotificationId("depth_chart_notice", $"{teamId}:autofill:{_state.CurrentWeek}"),
                TeamId = teamId,
                Type = "depth_chart_notice",
                Title = "Depth chart auto-filled",
                Description = $"{FormatTeamLabel(teamId)} rebuilt the depth chart from current healthy active players.",
                Severity = "info",
                PrimaryAction = "Review Depth Chart"
            });
            Save();
            return Ok(new { ok = true, message = "Depth chart auto-filled.", depth_chart = BuildDepthChartPayload(teamId) });
        }

        private (int, string) UpdateDepthChart(JsonElement data)
        {
            var teamId = NormalizeTeamId(StringValue(data, "team_id"));
            var position = StringValue(data, "position");
            var playerId = StringValue(data, "player_id");
            var action = StringValue(data, "action");
            var depthChart = FindDepthChart(teamId);
            var updated = LeagueSliceFactory.ApplyDepthChartAction(depthChart, TeamPlayers(teamId), position, playerId, action);
            if (!updated)
                return Error(400, "Unable to update depth chart.");

            AddNotification(new FranchiseNotification
            {
                Id = CreateNotificationId("depth_chart_notice", $"{teamId}:{position}:{playerId}:{action}:{_state.CurrentWeek}"),
                TeamId = teamId,
                Type = "depth_chart_notice",
                Title = $"{position} updated",
                Description = $"{FormatTeamLabel(teamId)} depth chart changed at {position}.",
                Severity = "info",
                PrimaryAction = "Review Depth Chart"
            });
            Save();
            return Ok(new { ok = true, message = "Depth chart updated.", depth_chart = BuildDepthChartPayload(teamId) });
        }

        private int SimulateWeek(int week) => _state.Games.Where(game => game.Week == week && !game.Completed).Select(SimulateGame).Count();

        private Game SimulateGame(Game game)
        {
            if (game.Completed)
                return game;

            var seed = unchecked((uint)_state.Metadata.World.Seed) + (uint)(game.Week * 7919) + StableSeed.Hash32(game.Id);
            var homeContext = BuildGameTeamContext(game.HomeTeamId, seed ^ 0x13579BDFu);
            var awayContext = BuildGameTeamContext(game.AwayTeamId, seed ^ 0x2468ACE0u);
            var homeStrength = homeContext.Readiness.EffectiveStrength + 2;
            var awayStrength = awayContext.Readiness.EffectiveStrength;
            var homeScore = 13 + (int)(StableSeed.Next(ref seed) % 25) + homeStrength / 8;
            var awayScore = 10 + (int)(StableSeed.Next(ref seed) % 25) + awayStrength / 8;
            homeScore -= Math.Min(5, homeContext.Readiness.MissingStarters);
            awayScore -= Math.Min(5, awayContext.Readiness.MissingStarters);
            homeScore -= Math.Min(4, homeContext.Readiness.UnavailablePlayers / 4);
            awayScore -= Math.Min(4, awayContext.Readiness.UnavailablePlayers / 4);
            homeScore = Math.Max(6, homeScore);
            awayScore = Math.Max(6, awayScore);
            if (homeScore == awayScore)
                homeScore++;

            game.HomeQuarterScores = SplitScore(homeScore, ref seed);
            game.AwayQuarterScores = SplitScore(awayScore, ref seed);
            game.HomeScore = game.HomeQuarterScores.Sum();
            game.AwayScore = game.AwayQuarterScores.Sum();
            game.HomeTeamStats = BuildTeamStats(game.HomeScore, homeStrength, homeContext, ref seed);
            game.AwayTeamStats = BuildTeamStats(game.AwayScore, awayStrength, awayContext, ref seed);
            game.HomeLeaders = BuildTeamLeaders(homeContext, ref seed);
            game.AwayLeaders = BuildTeamLeaders(awayContext, ref seed);
            LeagueSliceFactory.ApplyPostGameInjuries(homeContext.Readiness.AvailableByPosition.SelectMany(entry => entry.Value).DistinctBy(player => player.Id), seed ^ 0xA5A5A5A5u);
            LeagueSliceFactory.ApplyPostGameInjuries(awayContext.Readiness.AvailableByPosition.SelectMany(entry => entry.Value).DistinctBy(player => player.Id), seed ^ 0x5A5A5A5Au);
            RepairDepthChart(game.HomeTeamId);
            RepairDepthChart(game.AwayTeamId);
            game.Completed = true;
            return game;
        }

        private static List<int> SplitScore(int total, ref uint seed)
        {
            var quarters = new List<int> { 0, 0, 0, 0 };
            var remaining = total;
            for (var index = 0; index < quarters.Count - 1; index++)
            {
                if (remaining <= 0)
                    break;
                var maxForQuarter = Math.Max(0, remaining - (quarters.Count - index - 1));
                var value = maxForQuarter == 0 ? 0 : (int)(StableSeed.Next(ref seed) % (uint)(maxForQuarter + 1));
                quarters[index] = value;
                remaining -= value;
            }
            quarters[^1] = remaining;
            return quarters;
        }

        private static Dictionary<string, int> BuildTeamStats(int score, int strength, GameTeamContext context, ref uint seed)
        {
            var quarterbackBoost = context.Quarterback?.Overall ?? 60;
            var runnerBoost = context.RunningBack?.Overall ?? 60;
            var receiverBoost = context.Receiver?.Overall ?? 60;
            var rushYards = 60 + (int)(StableSeed.Next(ref seed) % 80) + strength / 5 + runnerBoost / 5;
            var passYards = 105 + (int)(StableSeed.Next(ref seed) % 160) + strength / 4 + quarterbackBoost / 6 + receiverBoost / 8;
            return new Dictionary<string, int>
            {
                ["total_yards"] = rushYards + passYards,
                ["rush_yards"] = rushYards,
                ["pass_yards"] = passYards,
                ["turnovers"] = Math.Max(0, (int)(StableSeed.Next(ref seed) % 3) + (context.Readiness.MissingStarters > 0 ? 1 : 0) - (strength >= 80 ? 1 : 0)),
                ["first_downs"] = 12 + score / 3
            };
        }

        private TeamLeaderSet BuildTeamLeaders(GameTeamContext context, ref uint seed)
        {
            var qb = context.Quarterback ?? CreateFallbackLeader("Quarterback");
            var rb = context.RunningBack ?? CreateFallbackLeader("Running Back");
            var wr = context.Receiver ?? CreateFallbackLeader("Receiver");
            return new TeamLeaderSet
            {
                Passing = new PassingLeader
                {
                    Name = qb.Name,
                    Completions = 17 + (int)(StableSeed.Next(ref seed) % 10),
                    Attempts = 25 + (int)(StableSeed.Next(ref seed) % 12),
                    Yards = 185 + (int)(StableSeed.Next(ref seed) % 120),
                    Touchdowns = (int)(StableSeed.Next(ref seed) % 4)
                },
                Rushing = new RushingLeader
                {
                    Name = rb.Name,
                    Carries = 12 + (int)(StableSeed.Next(ref seed) % 10),
                    Yards = 48 + (int)(StableSeed.Next(ref seed) % 80),
                    Touchdowns = (int)(StableSeed.Next(ref seed) % 3)
                },
                Receiving = new ReceivingLeader
                {
                    Name = wr.Name,
                    Receptions = 4 + (int)(StableSeed.Next(ref seed) % 6),
                    Yards = 55 + (int)(StableSeed.Next(ref seed) % 95),
                    Touchdowns = (int)(StableSeed.Next(ref seed) % 3)
                }
            };
        }

        private GameTeamContext BuildGameTeamContext(string teamId, uint matchupSeed)
        {
            var team = Team(teamId);
            var roster = TeamPlayers(teamId);
            var depthChart = FindDepthChart(teamId);
            var readiness = LeagueSliceFactory.EvaluateTeamReadiness(team.Strength, roster, depthChart, matchupSeed);
            return new GameTeamContext
            {
                Team = team,
                Readiness = readiness,
                Quarterback = PrimaryContributor(readiness, "QB"),
                RunningBack = PrimaryContributor(readiness, "RB"),
                Receiver = PrimaryContributor(readiness, "WR")
            };
        }

        private static Player PrimaryContributor(TeamReadiness readiness, string position)
        {
            if (readiness?.AvailableByPosition == null)
                return null;
            return readiness.AvailableByPosition.TryGetValue(position, out var players)
                ? players.FirstOrDefault()
                : null;
        }

        private static Player CreateFallbackLeader(string position) => new() { Name = position, Position = position, Overall = 60 };

        private Team Team(string id) => _state.Teams.First(team => team.Id == id);
        private List<Player> TeamPlayers(string teamId) => _state.Players.Where(player => player.TeamId == teamId).ToList();
        private TeamDepthChart FindDepthChart(string teamId) => _state.DepthCharts.First(chart => chart.TeamId == teamId);
        private void RepairDepthChart(string teamId)
        {
            var notices = LeagueSliceFactory.AutoRepairDepthChart(FindDepthChart(teamId), TeamPlayers(teamId), FormatTeamLabel(teamId));
            foreach (var notice in notices)
            {
                AddNotification(new FranchiseNotification
                {
                    Id = CreateNotificationId(notice.Type, $"{notice.TeamId}:{notice.Title}:{notice.Description}:{_state.CurrentWeek}"),
                    TeamId = notice.TeamId,
                    Type = notice.Type,
                    Title = notice.Title,
                    Description = notice.Description,
                    Severity = notice.Severity,
                    PrimaryAction = string.IsNullOrWhiteSpace(notice.PrimaryAction) ? "Review Depth Chart" : notice.PrimaryAction
                });
            }
        }
        private void RepairAllDepthCharts()
        {
            foreach (var team in _state.Teams)
                RepairDepthChart(team.Id);
        }
        private List<TeamStanding> RegularSeasonStandings()
            => LeagueSliceFactory.BuildStandings(_state.Teams.Select(team => team.Id), SeasonSnapshots());
        private TeamStanding RegularSeasonStanding(string teamId)
            => RegularSeasonStandings().FirstOrDefault(item => item.TeamId == teamId) ?? new TeamStanding { TeamId = teamId };
        private List<LeagueTeamDefinition> LeagueTeamDefinitions()
            => _state.Teams.Select(team => new LeagueTeamDefinition
            {
                Id = team.Id,
                City = team.City,
                Name = team.Name,
                Abbreviation = team.Abbreviation,
                Conference = team.Conference,
                Division = team.Division,
                Strength = team.Strength
            }).ToList();
        private static string StandingsStatusLabel(int playoffSeed, int divisionRank, int conferenceRank)
        {
            if (playoffSeed > 0)
                return playoffSeed <= 4 ? "Division Leader" : "Wild Card";
            if (divisionRank == 1)
                return "Division Leader";
            if (conferenceRank > 0 && conferenceRank <= 10)
                return "In Hunt";
            return "";
        }
        private string Record(string teamId)
        {
            var standing = RegularSeasonStanding(teamId);
            return $"{standing.Wins}-{standing.Losses}";
        }
        private List<SeasonGameSnapshot> SeasonSnapshots()
            => _state.Games.Select(game => new SeasonGameSnapshot
            {
                Id = game.Id,
                Week = game.Week,
                GameType = NormalizeGameType(game),
                HomeTeamId = game.HomeTeamId,
                AwayTeamId = game.AwayTeamId,
                HomeScore = game.HomeScore,
                AwayScore = game.AwayScore,
                Completed = game.Completed,
                HomeSeed = game.HomeSeed,
                AwaySeed = game.AwaySeed
            }).ToList();
        private static string NormalizeGameType(Game game)
            => string.IsNullOrWhiteSpace(game?.GameType) ? LeagueSliceFactory.RegularSeasonGameType : game.GameType;
        private void SyncCurrentWeekFromDate()
        {
            _state.CurrentWeek = LeagueSliceFactory.GetFootballWeekForDate(_state.SeasonYear, _state.CurrentDate, _state.MaxWeek);
        }

        private string CurrentWeekLabel()
            => _state.CurrentDate.Date > GameDayDate(LeagueSliceFactory.ChampionshipWeek).Date
                ? CurrentPhaseLabel()
                : _state.CurrentWeek > _state.MaxWeek
                    ? CurrentPhaseLabel()
                    : LeagueSliceFactory.DescribeWeek(Math.Min(_state.CurrentWeek, _state.MaxWeek));

        private string CurrentPhaseLabel()
            => LeagueSliceFactory.DescribeCalendarPhase(_state.SeasonYear, _state.CurrentDate, SeasonSnapshots());

        private DateTime GameDayDate(int week) => LeagueSliceFactory.GetGameDayDate(_state.SeasonYear, week);

        private static object SerializeMilestone(LeagueCalendarMilestone milestone) => new
        {
            id = milestone.Id,
            label = milestone.Label,
            date = milestone.Date.ToString("yyyy-MM-dd"),
            day_of_week = milestone.Date.DayOfWeek.ToString(),
            phase = milestone.Phase
        };

        private static object SerializeMilestoneOrNull(LeagueCalendarMilestone milestone)
            => milestone == null ? null : SerializeMilestone(milestone);

        private DateTime? ResolveMilestoneDate(string targetType, int targetWeek, string targetMilestoneId)
        {
            if (string.Equals(targetType, "regular_season_week", StringComparison.OrdinalIgnoreCase))
                return LeagueSliceFactory.GetWeekStartDate(_state.SeasonYear, Math.Min(targetWeek, _state.MaxWeek));

            if (string.IsNullOrWhiteSpace(targetMilestoneId))
                return targetType switch
                {
                    "playoffs_start" => LeagueSliceFactory.GetWeekStartDate(_state.SeasonYear, LeagueSliceFactory.WildCardWeek),
                    "offseason_start" => LeagueSliceFactory.GetOffseasonOpenDate(_state.SeasonYear),
                    _ => null
                };

            return LeagueSliceFactory.GetLeagueCalendarMilestones(_state.SeasonYear)
                .FirstOrDefault(milestone => string.Equals(milestone.Id, targetMilestoneId, StringComparison.OrdinalIgnoreCase))
                ?.Date;
        }

        private int ProcessCalendarMilestonesForCurrentDate()
        {
            var created = 0;
            foreach (var milestone in LeagueSliceFactory.GetLeagueCalendarMilestonesForDate(_state.SeasonYear, _state.CurrentDate))
            {
                if (string.Equals(milestone.Id, "next_season_opens", StringComparison.OrdinalIgnoreCase))
                    continue;

                var milestoneKey = $"{_state.SeasonYear}:{milestone.Id}";
                if (_state.ProcessedCalendarMilestones.Any(item => string.Equals(item, milestoneKey, StringComparison.OrdinalIgnoreCase)))
                    continue;

                _state.ProcessedCalendarMilestones.Add(milestoneKey);
                AddNotification(new FranchiseNotification
                {
                    Id = CreateNotificationId("league_calendar", milestoneKey),
                    DedupKey = $"league_calendar:{milestoneKey}",
                    TeamId = _state.UserTeamId,
                    Type = "league_calendar",
                    Title = milestone.Label,
                    Description = BuildCalendarMilestoneDescription(milestone),
                    Severity = CalendarMilestoneSeverity(milestone),
                    PrimaryAction = CalendarMilestonePrimaryAction(milestone),
                    CreatedWeek = _state.CurrentWeek
                });
                created++;
            }

            return created;
        }

        private static string BuildCalendarMilestoneDescription(LeagueCalendarMilestone milestone)
            => milestone.Id switch
            {
                "regular_season_week_1" => "Opening week is on the calendar today. Review the schedule, lineup, and early standings context.",
                "regular_season_week_5" or "regular_season_week_9" or "regular_season_week_13"
                    => $"{milestone.Label} begins today. Review standings, injuries, and the next stretch of games.",
                "playoffs_start" => "The postseason begins this week. Review the bracket, seeding, and your next matchup.",
                "championship_game" => "Championship day has arrived. The title game will decide the league champion.",
                "offseason_opens" => "The season has moved into the offseason. Review the league summary, roster, and upcoming deadlines.",
                "retirement_decisions" => "Retirement decisions begin today. Veteran roster turnover will become part of the offseason loop here.",
                "new_league_year" => "The new league year bookkeeping date has arrived. This is the foundation point for offseason roster and contract resets.",
                "free_agency_opens" => "Free agency opens today. Contract negotiation and market activity will plug into this window.",
                "draft_prep_opens" => "Draft preparation opens today. Board building, scouting review, and prospect planning belong in this window.",
                "draft_week" => "Draft week is here. The offseason draft flow will attach to this league date.",
                _ => $"{milestone.Label} is on today's league calendar."
            };

        private static string CalendarMilestoneSeverity(LeagueCalendarMilestone milestone)
            => milestone.Id switch
            {
                "playoffs_start" or "championship_game" => "warning",
                _ => "info"
            };

        private static string CalendarMilestonePrimaryAction(LeagueCalendarMilestone milestone)
            => milestone.Id switch
            {
                "playoffs_start" or "championship_game" => "Review League",
                _ => "Review Schedule"
            };

        private bool IsCurrentWeekGameDay(DateTime date)
            => _state.CurrentWeek >= 1
                && _state.CurrentWeek <= _state.MaxWeek
                && GameDayDate(_state.CurrentWeek).Date == date.Date;

        private bool IsPendingUserGameForDate(DateTime date)
            => GetPendingUserGameForDate(date) != null;

        private bool IsPendingUserGameForDate(Game game, DateTime date)
            => game != null
                && !game.Completed
                && (game.HomeTeamId == _state.UserTeamId || game.AwayTeamId == _state.UserTeamId)
                && GameDayDate(game.Week).Date == date.Date;

        private Game GetPendingUserGameForDate(DateTime date)
            => _state.Games
                .Where(game => IsPendingUserGameForDate(game, date))
                .OrderBy(game => game.Week)
                .FirstOrDefault();

        private Game GetNextUserGame()
            => _state.Games
                .Where(game => !game.Completed && (game.HomeTeamId == _state.UserTeamId || game.AwayTeamId == _state.UserTeamId))
                .OrderBy(game => GameDayDate(game.Week))
                .ThenBy(game => game.Week)
                .FirstOrDefault();

        private bool HasShownContinueStopToday(string reason)
            => !string.IsNullOrWhiteSpace(reason)
                && string.Equals(_state.LastContinueStopReason, reason, StringComparison.OrdinalIgnoreCase)
                && _state.LastContinueStopDate.HasValue
                && _state.LastContinueStopDate.Value.Date == _state.CurrentDate.Date;

        private void MarkContinueStopShown(string reason)
        {
            _state.LastContinueStopReason = reason;
            _state.LastContinueStopDate = _state.CurrentDate.Date;
        }

        private void ClearContinueStopState()
        {
            _state.LastContinueStopReason = "";
            _state.LastContinueStopDate = null;
        }

        private int SimulateAndFinalizeWeek(int week)
        {
            var simulated = SimulateWeek(week);
            FinalizeWeekIfReady(week);
            return simulated;
        }

        private bool FinalizeWeekIfReady(int week)
        {
            if (_state.FinalizedWeeks.Contains(week) || _state.Games.Any(game => game.Week == week && !game.Completed))
                return false;

            AddRecoveryNotifications(LeagueSliceFactory.AdvanceWeeklyRecovery(_state.Players), week + 1);
            RepairAllDepthCharts();
            EnsureSeasonProgression();
            _state.FinalizedWeeks.Add(week);
            TryArchiveCompletedSeason();
            return true;
        }

        private object BuildAdvanceResult(int daysAdvanced, int gamesSimulated, string stopReason) => new
        {
            ok = true,
            days_advanced = daysAdvanced,
            stop_reason = stopReason,
            games_simulated = gamesSimulated,
            season_year = _state.SeasonYear,
            current_date = _state.CurrentDate.ToString("yyyy-MM-dd"),
            day_of_week = _state.CurrentDate.DayOfWeek.ToString(),
            phase = CurrentPhaseLabel()
        };

        private (int, string) BuildSimUntilResult(int gamesSimulated, string stopReason)
            => Ok(new
            {
                ok = true,
                games_simulated = gamesSimulated,
                stop_reason = stopReason,
                season_year = _state.SeasonYear,
                current_date = _state.CurrentDate.ToString("yyyy-MM-dd"),
                day_of_week = _state.CurrentDate.DayOfWeek.ToString(),
                phase = CurrentPhaseLabel(),
                stopped_at = new
                {
                    week = _state.CurrentWeek,
                    week_label = CurrentWeekLabel()
                }
            });

        private bool TryArchiveCompletedSeason()
        {
            if (_state.SeasonArchived)
                return false;

            var archive = LeagueSliceFactory.BuildSeasonArchive(_state.SeasonYear, LeagueTeamDefinitions(), RegularSeasonStandings(), SeasonSnapshots());
            if (archive == null)
                return false;

            _state.SeasonHistory.RemoveAll(item => item.SeasonYear == archive.SeasonYear);
            _state.SeasonHistory.Add(archive);
            _state.SeasonHistory = _state.SeasonHistory
                .OrderByDescending(item => item.SeasonYear)
                .ToList();
            _state.SeasonArchived = true;

            AddNotification(new FranchiseNotification
            {
                Id = CreateNotificationId("season_complete", $"{archive.SeasonYear}:{archive.ChampionTeamId}:{archive.RunnerUpTeamId}"),
                DedupKey = $"season_complete:{archive.SeasonYear}",
                TeamId = archive.ChampionTeamId,
                Type = "season_complete",
                Title = $"{archive.SeasonYear} season complete",
                Description = $"{archive.ChampionDisplayName} beat {archive.RunnerUpDisplayName} {archive.ChampionScore}-{archive.RunnerUpScore} in the championship.",
                Severity = "info",
                PrimaryAction = "Review League",
                CreatedWeek = _state.CurrentWeek
            });
            return true;
        }

        private bool TryStartNextSeason()
        {
            if (!_state.SeasonArchived || _state.CurrentDate.Date < LeagueSliceFactory.GetLeagueYearStartDate(_state.SeasonYear + 1).Date)
                return false;

            var previousSeason = _state.SeasonHistory
                .OrderByDescending(item => item.SeasonYear)
                .FirstOrDefault();
            _state.SeasonYear++;
            _state.CurrentDate = LeagueSliceFactory.GetLeagueYearStartDate(_state.SeasonYear);
            _state.CurrentWeek = 1;
            _state.MaxWeek = LeagueSliceFactory.MaxSeasonWeek;
            _state.Games = LeagueSliceFactory.CreatePrototypeRegularSeasonSchedule(LeagueTeamDefinitions(), _state.SeasonYear)
                .Select(game => new Game
                {
                    Id = game.Id,
                    Week = game.Week,
                    GameType = game.GameType,
                    HomeTeamId = game.HomeTeamId,
                    AwayTeamId = game.AwayTeamId
                })
                .ToList();
            _state.SeasonArchived = false;
            _state.FinalizedWeeks.Clear();
            _state.ProcessedCalendarMilestones.Clear();
            HealPlayersForNewSeason();
            _state.Notifications.Clear();
            ClearContinueStopState();
            RepairAllDepthCharts();
            AddNotification(new FranchiseNotification
            {
                Id = CreateNotificationId("season_reset", $"{_state.SeasonYear}:{_state.UserTeamId}"),
                DedupKey = $"season_reset:{_state.SeasonYear}",
                TeamId = _state.UserTeamId,
                Type = "season_phase_changed",
                Title = $"{_state.SeasonYear} season ready",
                Description = previousSeason == null
                    ? "A new regular-season schedule is in place and your roster is reset for the new year."
                    : $"{_state.SeasonYear} has started. Reigning champion: {previousSeason.ChampionDisplayName}.",
                Severity = "info",
                PrimaryAction = "Review Schedule",
                CreatedWeek = _state.CurrentWeek
            });
            return true;
        }

        private void HealPlayersForNewSeason()
        {
            foreach (var player in _state.Players)
            {
                player.OnInjuredReserve = false;
                if (string.Equals(player.RosterBucket, "injured_reserve", StringComparison.OrdinalIgnoreCase))
                    player.RosterBucket = "active";
                player.Injury = new PlayerInjury();
            }
        }

        private bool EnsureSeasonProgression()
        {
            var changed = false;
            var standings = RegularSeasonStandings();
            var teams = LeagueTeamDefinitions();
            if (!_state.Games.Any(game => string.Equals(NormalizeGameType(game), LeagueSliceFactory.PlayoffWildCardGameType, StringComparison.OrdinalIgnoreCase))
                && _state.Games.Where(game => LeagueSliceFactory.IsRegularSeasonGame(NormalizeGameType(game))).All(game => game.Completed))
            {
                foreach (var conference in teams.Select(team => team.Conference).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var seeds = LeagueSliceFactory.SelectConferencePlayoffSeeds(teams, standings, SeasonSnapshots(), conference);
                    foreach (var snapshot in LeagueSliceFactory.CreateWildCardGames(seeds, conference))
                    {
                        _state.Games.Add(new Game
                        {
                            Id = snapshot.Id,
                            Week = snapshot.Week,
                            GameType = snapshot.GameType,
                            HomeTeamId = snapshot.HomeTeamId,
                            AwayTeamId = snapshot.AwayTeamId,
                            HomeSeed = snapshot.HomeSeed,
                            AwaySeed = snapshot.AwaySeed
                        });
                        changed = true;
                    }
                }
            }

            if (!_state.Games.Any(game => string.Equals(NormalizeGameType(game), LeagueSliceFactory.PlayoffDivisionalGameType, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var conference in teams.Select(team => team.Conference).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var seeds = LeagueSliceFactory.SelectConferencePlayoffSeeds(teams, standings, SeasonSnapshots(), conference);
                    foreach (var snapshot in LeagueSliceFactory.CreateDivisionalGames(seeds, SeasonSnapshots(), conference))
                    {
                        _state.Games.Add(new Game
                        {
                            Id = snapshot.Id,
                            Week = snapshot.Week,
                            GameType = snapshot.GameType,
                            HomeTeamId = snapshot.HomeTeamId,
                            AwayTeamId = snapshot.AwayTeamId,
                            HomeSeed = snapshot.HomeSeed,
                            AwaySeed = snapshot.AwaySeed
                        });
                        changed = true;
                    }
                }
            }

            if (!_state.Games.Any(game => string.Equals(NormalizeGameType(game), LeagueSliceFactory.PlayoffConferenceChampionshipGameType, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var conference in teams.Select(team => team.Conference).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var conferenceTitleGame = LeagueSliceFactory.CreateConferenceChampionshipGame(SeasonSnapshots(), conference);
                    if (conferenceTitleGame == null)
                        continue;

                    _state.Games.Add(new Game
                    {
                        Id = conferenceTitleGame.Id,
                        Week = conferenceTitleGame.Week,
                        GameType = conferenceTitleGame.GameType,
                        HomeTeamId = conferenceTitleGame.HomeTeamId,
                        AwayTeamId = conferenceTitleGame.AwayTeamId,
                        HomeSeed = conferenceTitleGame.HomeSeed,
                        AwaySeed = conferenceTitleGame.AwaySeed
                    });
                    changed = true;
                }
            }

            if (!_state.Games.Any(game => string.Equals(NormalizeGameType(game), LeagueSliceFactory.ChampionshipGameType, StringComparison.OrdinalIgnoreCase)))
            {
                var championship = LeagueSliceFactory.CreateChampionshipGame(SeasonSnapshots());
                if (championship != null)
                {
                    _state.Games.Add(new Game
                    {
                        Id = championship.Id,
                        Week = championship.Week,
                        GameType = championship.GameType,
                        HomeTeamId = championship.HomeTeamId,
                        AwayTeamId = championship.AwayTeamId,
                        HomeSeed = championship.HomeSeed,
                        AwaySeed = championship.AwaySeed
                    });
                    changed = true;
                }
            }

            return changed;
        }
        private (int, string) Ok(object value) => (200, JsonSerializer.Serialize(value, _jsonOptions));
        private (int, string) Error(int status, string error) => (status, JsonSerializer.Serialize(new { ok = false, error }, _jsonOptions));
        private void Save() => File.WriteAllText(ProjectSettings.GlobalizePath(SavePath), JsonSerializer.Serialize(_state, _jsonOptions));

        private FranchiseState Load()
        {
            var path = ProjectSettings.GlobalizePath(SavePath);
            try
            {
                return File.Exists(path) ? JsonSerializer.Deserialize<FranchiseState>(File.ReadAllText(path)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EnsureState()
        {
            var migrated = false;
            if (_state == null || _state.Teams == null || _state.Teams.Count == 0 || _state.Games == null)
            {
                _state = Load() ?? FranchiseState.CreateNew();
                migrated = true;
            }

            _state.Metadata ??= new FranchiseMetadata();
            _state.Notifications ??= new List<FranchiseNotification>();
            _state.SeasonHistory ??= new List<SeasonArchiveSummary>();
            _state.FinalizedWeeks ??= new List<int>();
            _state.ProcessedCalendarMilestones ??= new List<string>();
            _state.Metadata.Validate();
            _state.EnsureTeamData();
            if (_state.CurrentDate == default)
            {
                var week = Math.Clamp(_state.CurrentWeek, 1, _state.MaxWeek);
                _state.CurrentDate = LeagueSliceFactory.GetWeekStartDate(_state.SeasonYear, week);
                migrated = true;
            }
            SyncCurrentWeekFromDate();
            var defaultTeamsById = LeagueSliceFactory.CreateDefaultLeagueTeams()
                .ToDictionary(team => team.Id, StringComparer.Ordinal);
            foreach (var team in _state.Teams)
            {
                if (!defaultTeamsById.TryGetValue(team.Id, out var definition))
                    continue;
                if (string.IsNullOrWhiteSpace(team.Conference))
                {
                    team.Conference = definition.Conference;
                    migrated = true;
                }
                if (string.IsNullOrWhiteSpace(team.Division))
                {
                    team.Division = definition.Division;
                    migrated = true;
                }
            }
            if (_state.MaxWeek < LeagueSliceFactory.MaxSeasonWeek)
            {
                _state.MaxWeek = LeagueSliceFactory.MaxSeasonWeek;
                migrated = true;
            }
            foreach (var game in _state.Games)
            {
                if (string.IsNullOrWhiteSpace(game.GameType))
                {
                    game.GameType = LeagueSliceFactory.RegularSeasonGameType;
                    migrated = true;
                }
            }
            if (EnsureSeasonProgression())
                migrated = true;
            if (TryArchiveCompletedSeason())
                migrated = true;
            if (_profileStore.Find(_state.Metadata.GmProfileSnapshot.Id) == null)
            {
                _profileStore.Save(_state.Metadata.GmProfileSnapshot);
                migrated = true;
            }
            if (ProcessCalendarMilestonesForCurrentDate() > 0)
                migrated = true;

            if (migrated)
                Save();
        }

        private GmProfile ResolveProfile(JsonElement data)
        {
            var profileId = StringValue(data, "gm_profile_id");
            return string.IsNullOrWhiteSpace(profileId)
                ? _profileStore.GetOrCreateDefault()
                : _profileStore.Find(profileId) ?? _profileStore.GetOrCreateDefault();
        }

        private static WorldDefinition ResolveWorldDefinition(JsonElement data)
        {
            var source = StringValue(data, "roster_source");
            if (!string.Equals(source, "generated", StringComparison.OrdinalIgnoreCase))
                return WorldDefinition.Standard();

            var seed = ULongValue(data, "world_seed", 0);
            if (seed == 0)
                seed = unchecked((ulong)DateTime.UtcNow.Ticks ^ (ulong)Guid.NewGuid().GetHashCode());
            return WorldDefinition.Generated(seed);
        }

        private GmProfile CreateOrUpdateProfile(JsonElement data)
        {
            var profileId = StringValue(data, "gm_profile_id");
            var profile = string.IsNullOrWhiteSpace(profileId)
                ? new GmProfile()
                : _profileStore.Find(profileId) ?? new GmProfile { Id = profileId };

            profile.Name = StringValue(data, "name");
            profile.Attributes = new GmAttributes
            {
                Negotiation = IntValue(data, "negotiation", 50),
                PlayerManagement = IntValue(data, "player_management", 50),
                ScoutingJudgment = IntValue(data, "scouting_judgment", 50),
                Leadership = IntValue(data, "leadership", 50)
            };
            profile.Appearance = new CharacterDesign
            {
                Pronouns = StringValue(data, "pronouns"),
                HairStyle = StringValue(data, "hair_style"),
                HairColor = StringValue(data, "hair_color"),
                SkinTone = StringValue(data, "skin_tone"),
                Outfit = StringValue(data, "outfit")
            };
            return profile;
        }

        private object TeamSummary(Team team) => new
        {
            team_id = team.Id,
            abbreviation = team.Abbreviation,
            city = team.City,
            name = team.Name,
            conference = team.Conference,
            division = team.Division
        };

        private object PlayerPayload(Player player) => new
        {
            player_id = player.Id,
            name = player.Name,
            position = player.Position,
            age = player.Age,
            overall = player.Overall,
            pot = player.Potential,
            potential = player.Potential,
            pot_rating = player.Potential,
            jersey_number = player.JerseyNumber,
            roster_bucket = player.RosterBucket,
            on_injured_reserve = player.OnInjuredReserve,
            ir = player.OnInjuredReserve,
            status = player.OnInjuredReserve ? "ir" : "active",
            injury_status = player.Injury == null ? "healthy" : player.Injury.Status,
            injury = InjuryDisplay(player),
            injury_name = player.Injury?.Name ?? "",
            injury_end_date = player.Injury?.ReturnLabel ?? "",
            days_remaining = player.Injury?.DaysRemaining ?? 0,
            confidence = player.ScoutConfidence,
            scout_confidence = player.ScoutConfidence,
            scout_summary = player.ScoutSummary,
            scout_report = player.ScoutReport,
            tags = player.Tags
        };

        private static string StringValue(JsonElement data, string property) => data.TryGetProperty(property, out var value) ? value.GetString() ?? "" : "";
        private static int IntValue(JsonElement data, string property, int fallback) => data.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : fallback;
        private static ulong ULongValue(JsonElement data, string property, ulong fallback) => data.TryGetProperty(property, out var value) && value.TryGetUInt64(out var result) ? result : fallback;

        private static string QueryValue(Uri uri, string key)
        {
            var prefix = Uri.EscapeDataString(key) + "=";
            foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (item.StartsWith(prefix, StringComparison.Ordinal))
                    return Uri.UnescapeDataString(item.Substring(prefix.Length));
            }
            return "";
        }

        private string NormalizeTeamId(string teamId) => string.IsNullOrWhiteSpace(teamId) ? _state.UserTeamId : teamId;
        private string FormatTeamLabel(string teamId)
        {
            var team = _state.Teams.FirstOrDefault(value => value.Id == teamId);
            return team == null ? "Team" : $"{team.Abbreviation} {team.City} {team.Name}".Trim();
        }

        private void AddNotification(FranchiseNotification notification)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Id))
                return;
            notification.CreatedWeek = notification.CreatedWeek <= 0 ? _state.CurrentWeek : notification.CreatedWeek;
            _state.Notifications.RemoveAll(item =>
                string.Equals(item.Id, notification.Id, StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(notification.DedupKey)
                    && string.Equals(item.DedupKey, notification.DedupKey, StringComparison.Ordinal)));
            _state.Notifications.Add(notification);
            _state.Notifications = _state.Notifications
                .OrderByDescending(NotificationSeverityRank)
                .ThenByDescending(item => item.CreatedWeek)
                .ThenByDescending(item => item.Id, StringComparer.Ordinal)
                .Take(12)
                .ToList();
        }

        private (int, string) AcknowledgeNotification(string messageId)
        {
            if (!string.IsNullOrWhiteSpace(messageId))
                _state.Notifications.RemoveAll(item => string.Equals(item.Id, messageId, StringComparison.OrdinalIgnoreCase));
            Save();
            return Ok(new { ok = true });
        }

        private object[] BuildDashboardActionItems(string userTeamId, RosterValidationResult rosterStatus, List<string> depthChartIssues, Game nextGame)
        {
            var items = new List<object>();
            if (nextGame != null)
            {
                items.Add(new
                {
                    id = $"game-{nextGame.Id}",
                    type = "game_day",
                    title = "Game Day",
                    description = $"{LeagueSliceFactory.DescribeWeek(nextGame.Week)} matchup is ready.",
                    severity = "info",
                    primary_action = "View Matchup",
                    game_id = nextGame.Id
                });
            }

            if (rosterStatus != null && !rosterStatus.IsValid)
            {
                items.Add(new
                {
                    id = CreateNotificationId("roster_invalid", $"{userTeamId}:roster:{string.Join("|", rosterStatus.Issues)}"),
                    type = "roster_invalid",
                    title = "Roster needs attention",
                    description = rosterStatus.Issues.FirstOrDefault() ?? "Roster validation failed.",
                    severity = "danger",
                    primary_action = "Open Roster"
                });
            }

            if (depthChartIssues != null && depthChartIssues.Count > 0)
            {
                items.Add(new
                {
                    id = CreateNotificationId("depth_chart_invalid", $"{userTeamId}:depth:{string.Join("|", depthChartIssues)}"),
                    type = "depth_chart_invalid",
                    title = "Depth chart needs attention",
                    description = depthChartIssues.First(),
                    severity = "danger",
                    primary_action = "Open Depth Chart"
                });
            }

            items.AddRange(_state.Notifications
                .OrderByDescending(NotificationSeverityRank)
                .ThenByDescending(item => item.CreatedWeek)
                .Select(notification => new
            {
                id = notification.Id,
                type = notification.Type,
                title = notification.Title,
                description = notification.Description,
                severity = notification.Severity,
                primary_action = notification.PrimaryAction,
                requires_ack = true
            }));
            return items.ToArray();
        }

        private static string CreateNotificationId(string prefix, string key)
            => $"{prefix}:{StableSeed.Hash32(key ?? string.Empty):x8}";

        private void AddRecoveryNotifications(List<PlayerRecoveryNotice> notices, int recoveryWeek)
        {
            if (notices == null || notices.Count == 0)
                return;

            foreach (var notice in notices)
            {
                AddNotification(new FranchiseNotification
                {
                    Id = CreateNotificationId("roster_notice", $"{notice.TeamId}:{notice.PlayerId}:recovered:{recoveryWeek}"),
                    DedupKey = $"recovered:{notice.TeamId}:{notice.PlayerId}",
                    TeamId = notice.TeamId,
                    Type = "roster_notice",
                    Title = "Player returned",
                    Description = $"{FormatTeamLabel(notice.TeamId)} activated {notice.PlayerName} at {notice.Position}.",
                    Severity = "info",
                    PrimaryAction = "Open Roster",
                    CreatedWeek = recoveryWeek
                });
            }
        }

        private static int NotificationSeverityRank(FranchiseNotification notification)
        {
            var severity = notification?.Severity ?? "info";
            return severity switch
            {
                "danger" => 3,
                "warning" => 2,
                _ => 1
            };
        }

        private static int DivisionSortOrder(string division) => division switch
        {
            "East" => 0,
            "North" => 1,
            "South" => 2,
            "West" => 3,
            _ => 4
        };

        private static int BucketOrder(string bucket) => bucket switch
        {
            "active" => 0,
            "injured_reserve" => 1,
            "practice_squad" => 2,
            _ => 3
        };

        private static string InjuryDisplay(Player player)
        {
            if (player?.Injury == null || player.Injury.IsHealthy)
                return "Healthy";
            return player.OnInjuredReserve ? "IR" : Humanize(player.Injury.Status);
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Healthy";
            var normalized = value.Replace('_', ' ').Trim().ToLowerInvariant();
            return char.ToUpperInvariant(normalized[0]) + normalized[1..];
        }
    }

    public class FranchiseState
    {
        public int SeasonYear { get; set; } = 2026;
        public DateTime CurrentDate { get; set; } = LeagueSliceFactory.GetLeagueYearStartDate(2026);
        public int CurrentWeek { get; set; } = 1;
        public int MaxWeek { get; set; } = LeagueSliceFactory.MaxSeasonWeek;
        public int Seed { get; set; } = 20260814;
        public string UserTeamId { get; set; } = "lake";
        public FranchiseMetadata Metadata { get; set; } = new();
        public List<Team> Teams { get; set; } = new();
        public List<Game> Games { get; set; } = new();
        public List<Player> Players { get; set; } = new();
        public List<TeamDepthChart> DepthCharts { get; set; } = new();
        public List<FranchiseNotification> Notifications { get; set; } = new();
        public bool SeasonArchived { get; set; }
        public List<SeasonArchiveSummary> SeasonHistory { get; set; } = new();
        public List<int> FinalizedWeeks { get; set; } = new();
        public List<string> ProcessedCalendarMilestones { get; set; } = new();
        public DateTime? LastContinueStopDate { get; set; }
        public string LastContinueStopReason { get; set; } = "";

        public static FranchiseState CreateNew(WorldDefinition world = null, GmProfile profile = null)
        {
            world ??= WorldDefinition.Standard();
            profile ??= new GmProfile();
            world.Validate();
            profile.Validate();
            var state = new FranchiseState
            {
                Seed = unchecked((int)world.Seed),
                CurrentDate = LeagueSliceFactory.GetLeagueYearStartDate(2026),
                Metadata = new FranchiseMetadata
                {
                    World = new WorldDefinition { Source = world.Source, Seed = world.Seed, GeneratorVersion = world.GeneratorVersion },
                    GmProfileSnapshot = profile.Snapshot()
                }
            };
            state.Teams = LeagueSliceFactory.CreateDefaultLeagueTeams()
                .Select(team => new Team
                {
                    Id = team.Id,
                    City = team.City,
                    Name = team.Name,
                    Abbreviation = team.Abbreviation,
                    Conference = team.Conference,
                    Division = team.Division,
                    Strength = team.Strength
                })
                .ToList();
            var ids = state.Teams.Select(team => team.Id).ToArray();
            var strengthSeed = unchecked((uint)world.Seed);
            foreach (var team in state.Teams)
                team.Strength += (int)(StableSeed.Next(ref strengthSeed) % 7) - 3;
            state.Games = LeagueSliceFactory.CreatePrototypeRegularSeasonSchedule(
                    state.Teams.Select(team => new LeagueTeamDefinition
                    {
                        Id = team.Id,
                        City = team.City,
                        Name = team.Name,
                        Abbreviation = team.Abbreviation,
                        Conference = team.Conference,
                        Division = team.Division,
                        Strength = team.Strength
                    }),
                    state.SeasonYear)
                .Select(game => new Game
                {
                    Id = game.Id,
                    Week = game.Week,
                    GameType = game.GameType,
                    HomeTeamId = game.HomeTeamId,
                    AwayTeamId = game.AwayTeamId
                })
                .ToList();

            state.EnsureTeamData();
            return state;
        }

        public void EnsureTeamData()
        {
            Players ??= new List<Player>();
            DepthCharts ??= new List<TeamDepthChart>();
            Notifications ??= new List<FranchiseNotification>();
            SeasonHistory ??= new List<SeasonArchiveSummary>();
            FinalizedWeeks ??= new List<int>();
            ProcessedCalendarMilestones ??= new List<string>();
            foreach (var team in Teams)
            {
                if (!Players.Any(player => player.TeamId == team.Id))
                    Players.AddRange(LeagueSliceFactory.CreatePlayersForTeam(team.Id, Metadata.World.Seed ^ (ulong)StableSeed.Hash32(team.Id)));
                if (!DepthCharts.Any(chart => chart.TeamId == team.Id))
                    DepthCharts.Add(LeagueSliceFactory.CreateDepthChart(team.Id, Players.Where(player => player.TeamId == team.Id)));
            }
        }
    }

    public class Team
    {
        public string Id { get; set; } = "";
        public string City { get; set; } = "";
        public string Name { get; set; } = "";
        public string Abbreviation { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
        public int Strength { get; set; }
    }

    public class Game
    {
        public string Id { get; set; } = "";
        public int Week { get; set; }
        public string GameType { get; set; } = LeagueSliceFactory.RegularSeasonGameType;
        public string HomeTeamId { get; set; } = "";
        public string AwayTeamId { get; set; } = "";
        public int HomeSeed { get; set; }
        public int AwaySeed { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool Completed { get; set; }
        public List<int> HomeQuarterScores { get; set; } = new();
        public List<int> AwayQuarterScores { get; set; } = new();
        public Dictionary<string, int> HomeTeamStats { get; set; } = new();
        public Dictionary<string, int> AwayTeamStats { get; set; } = new();
        public TeamLeaderSet HomeLeaders { get; set; } = new();
        public TeamLeaderSet AwayLeaders { get; set; } = new();
    }

    public class TeamLeaderSet
    {
        public PassingLeader Passing { get; set; } = new();
        public RushingLeader Rushing { get; set; } = new();
        public ReceivingLeader Receiving { get; set; } = new();
    }

    public class PassingLeader
    {
        public string Name { get; set; } = "";
        public int Completions { get; set; }
        public int Attempts { get; set; }
        public int Yards { get; set; }
        public int Touchdowns { get; set; }
    }

    public class RushingLeader
    {
        public string Name { get; set; } = "";
        public int Carries { get; set; }
        public int Yards { get; set; }
        public int Touchdowns { get; set; }
    }

    public class ReceivingLeader
    {
        public string Name { get; set; } = "";
        public int Receptions { get; set; }
        public int Yards { get; set; }
        public int Touchdowns { get; set; }
    }

    public class FranchiseNotification
    {
        public string Id { get; set; } = "";
        public string DedupKey { get; set; } = "";
        public string TeamId { get; set; } = "";
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "info";
        public string PrimaryAction { get; set; } = "";
        public int CreatedWeek { get; set; }
    }

    public class GameTeamContext
    {
        public Team Team { get; set; } = new();
        public TeamReadiness Readiness { get; set; } = new();
        public Player Quarterback { get; set; }
        public Player RunningBack { get; set; }
        public Player Receiver { get; set; }
    }
}
