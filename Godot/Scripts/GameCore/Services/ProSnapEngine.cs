using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

/// <summary>One authoritative play per Step. No global RNG, league mutation, fabricated final score, or future plays.</summary>
public sealed class ProSnapEngine
{
    private readonly LeagueState _league;
    private readonly GameResult _result;
    private readonly ProGameState _state;
    private readonly TeamState _home;
    private readonly TeamState _away;
    private readonly Dictionary<string, PlayerState> _players;
    private static readonly (string Role, int Count)[] OffenseRoles = { ("QB", 1), ("RB", 1), ("WR", 3), ("TE", 1), ("LT", 1), ("LG", 1), ("C", 1), ("RG", 1), ("RT", 1) };
    private static readonly (string Role, int Count)[] DefenseRoles = { ("DE", 2), ("DT", 2), ("OLB", 2), ("MLB", 1), ("CB", 2), ("S", 2) };

    public ProSnapEngine(LeagueState league, GameResult result)
    {
        _league = league;
        _result = result;
        _state = result.ProGame ?? throw new ArgumentException("This game does not contain snap state.");
        if (_state.RulesVersion != ProGameState.LegacyRulesVersion && _state.RulesVersion != ProGameState.ClockRulesVersion)
            throw new InvalidOperationException("Unsupported live-game rules version.");
        _home = GameCoreStateHelper.ResolveTeam(league, result.HomeTeamId) ?? throw new ArgumentException("Home team not found.");
        _away = GameCoreStateHelper.ResolveTeam(league, result.AwayTeamId) ?? throw new ArgumentException("Away team not found.");
        _players = _home.Roster.Concat(_away.Roster).ToDictionary(p => p.PlayerId, StringComparer.OrdinalIgnoreCase);
    }

    public static GameResult Create(LeagueState league, string gameId, string homeId, string awayId, int absoluteWeek,
        int phaseWeek, string phase, string gameType, string weekLabel, int homeFieldBonus, bool requireWinner)
    {
        var home = GameCoreStateHelper.ResolveTeam(league, homeId);
        var away = GameCoreStateHelper.ResolveTeam(league, awayId);
        if (home == null || away == null || homeId == awayId) throw new ArgumentException("Two distinct existing teams are required.");
        ulong seed = league.FranchiseMetadata?.World?.Seed ?? WorldDefinition.StandardSeed;
        unchecked
        {
            foreach (var character in $"{league.SeasonYear}:{gameId}:{homeId}:{awayId}") seed = (seed ^ character) * 1099511628211UL;
        }
        var result = new GameResult
        {
            GameId = gameId, Week = absoluteWeek, AbsoluteWeek = absoluteWeek, PhaseWeek = phaseWeek,
            Phase = phase, GameType = gameType, WeekLabel = weekLabel, HomeTeamId = homeId, AwayTeamId = awayId,
            HomeTeam = home.Abbreviation, AwayTeam = away.Abbreviation,
            ProGame = new ProGameState { RulesVersion = ProGameState.ClockRulesVersion, RandomState = seed, RequireWinner = requireWinner, HomeFieldBonus = homeFieldBonus },
        };
        ProGameStatistics.Initialize(result.BoxScore);
        var engine = new ProSnapEngine(league, result);
        result.ProGame.OpeningReceiverId = engine.Roll(2) == 0 ? homeId : awayId;
        result.ProGame.PossessionTeamId = engine.Other(result.ProGame.OpeningReceiverId);
        return result;
    }

    public GamePlayEventState Step()
    {
        if (_state.Completed) return null;
        if (_state.Phase == "scrimmage" && _state.ClockSeconds > 0)
        {
            var offense = Team(_state.PossessionTeamId);
            if (ProClockManagementService.Enabled(_state) && _state.PendingDecision.Offense is "Kneel" or "Spike" && First(offense, "QB") == null)
                throw new InvalidOperationException("An available quarterback is required for a kneel or spike.");
            if (First(offense, "QB") == null && First(offense, "RB") == null)
                throw new InvalidOperationException($"{offense.Abbreviation} has no available quarterback or running back. Restore legal personnel before resuming.");
        }
        var play = new GamePlayEventState
        {
            Sequence = _result.BoxScore.PlayByPlay.Count + 1, Quarter = _state.Quarter,
            StartClockSeconds = _state.ClockSeconds, StartDown = _state.Down, StartDistance = _state.Distance,
            StartYardLine = _state.YardLine, OffensiveTeamId = _state.PossessionTeamId, DriveNumber = _state.CurrentDrive,
        };
        if (_state.ClockSeconds == 0 && _state.Phase != "try") EndPeriod(play);
        else if (ProClockManagementService.Enabled(_state) && ProClockManagementService.WarningDue(_state)) TwoMinuteWarning(play);
        else if (ProClockManagementService.Enabled(_state) && TryTimeout(play)) { }
        else if (_state.Phase == "kickoff") Kickoff(play);
        else if (_state.Phase == "try") TryAfterTouchdown(play);
        else Scrimmage(play);
        if (play.PlayType == "timeout" || play.Outcome == "two_minute_warning") _state.PendingDecision.Timeout = "";
        else _state.PendingDecision = new ProGameDecision();
        play.ClockSeconds = play.Quarter == _state.Quarter ? _state.ClockSeconds : 0;
        play.ElapsedSeconds = Math.Max(0, play.StartClockSeconds - play.ClockSeconds);
        play.HomeScore = _result.HomeScore; play.AwayScore = _result.AwayScore;
        play.PossessionTeamId = _state.PossessionTeamId;
        play.YardLine = _state.YardLine; play.Down = _state.Down; play.Distance = _state.Distance;
        play.IsScoringPlay = play.Points > 0;
        play.IsFinal = _state.Completed;
        _result.BoxScore.PlayByPlay.Add(play);
        ProGameStatistics.Apply(_result.BoxScore, play, _result.HomeTeamId);
        _result.BoxScore.Final = $"{_result.HomeTeam} {_result.HomeScore}, {_result.AwayTeam} {_result.AwayScore}";
        if (_state.Completed)
        {
            EndDrive("end of game", play.Sequence);
            _result.Winner = _result.HomeScore == _result.AwayScore ? "" : _result.HomeScore > _result.AwayScore ? _home.Name : _away.Name;
            var loser = _result.HomeScore > _result.AwayScore ? _away.Name : _home.Name;
            _result.Summary = _result.HomeScore == _result.AwayScore
                ? $"{_home.Name} and {_away.Name} tied {_result.HomeScore}-{_result.AwayScore}."
                : $"{_result.Winner} defeated {loser}, {Math.Max(_result.HomeScore, _result.AwayScore)}-{Math.Min(_result.HomeScore, _result.AwayScore)}.";
            play.Description += $" Final: {_result.BoxScore.Final}.";
        }
        return play;
    }

    public void Finish()
    {
        var guard = 0;
        while (!_state.Completed && guard++ < 20000) Step();
        if (!_state.Completed) throw new InvalidOperationException("Game exceeded the simulation event limit.");
    }

    private string Other(string teamId) => teamId == _home.TeamId ? _away.TeamId : _home.TeamId;
    private TeamState Team(string teamId) => teamId == _home.TeamId ? _home : _away;
    // SplitMix64: explicit algorithm and saved cursor, stable across runtimes and save/load.
    private int Roll(int maximum)
    {
        unchecked
        {
            var value = _state.RandomState += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return (int)((value ^ (value >> 31)) % (uint)maximum);
        }
    }
    private bool Chance(int percent) => Roll(10000) < Math.Clamp(percent, 0, 100) * 100;

    private List<PlayerState> Role(TeamState team, string role, int count)
    {
        bool Available(PlayerState p) => PlayerInjuryService.IsAvailableForGame(p) && !_state.InjuredPlayerIds.Contains(p.PlayerId);
        var chosen = new List<PlayerState>(count);
        if (team.DepthChart.TryGetValue(role, out var order))
            foreach (var id in order)
                if (_players.TryGetValue(id, out var p) && team.Roster.Contains(p) && p.Position == role && Available(p) && !chosen.Contains(p))
                {
                    chosen.Add(p);
                    if (chosen.Count == count) return chosen;
                }
        // Legacy saves without complete charts append legal same-position depth deterministically.
        foreach (var p in team.Roster.Where(p => p.Position == role && Available(p)).OrderBy(p => p.PlayerId, StringComparer.Ordinal))
            if (!chosen.Contains(p)) { chosen.Add(p); if (chosen.Count == count) break; }
        return chosen;
    }
    private PlayerState First(TeamState team, string role) => Role(team, role, 1).FirstOrDefault();
    private List<PlayerState> Unit(TeamState team, (string Role, int Count)[] roles)
    {
        var players = new List<PlayerState>(11);
        foreach (var (role, count) in roles) players.AddRange(Role(team, role, count));
        return players;
    }
    private int Ability(PlayerState player)
    {
        if (player == null) return 25;
        var snaps = _result.BoxScore.PlayerStats.FirstOrDefault(s => s.PlayerId == player.PlayerId)?.Snaps ?? 0;
        return Math.Clamp(player.Overall - player.Fatigue / 8 - snaps / 20, 1, 100);
    }
    private int Strength(List<PlayerState> players) => players.Count == 0 ? 25 : (int)players.Average(Ability);
    private int Staff(TeamState team, string role) => Math.Clamp(((team.Coaches.FirstOrDefault(c => c.Role == role)?.Overall ?? 55) - 70) / 15, -1, 1);
    private PlayerGameStats Line(GamePlayEventState play, PlayerState player, TeamState team)
    {
        if (player == null) return null;
        Participate(play, player, team);
        var line = play.StatChanges.FirstOrDefault(s => s.PlayerId == player.PlayerId);
        if (line != null) return line;
        line = new PlayerGameStats { PlayerId = player.PlayerId, PlayerName = player.Name, TeamId = team.TeamId, Position = player.Position };
        play.StatChanges.Add(line);
        return line;
    }
    private void Participate(GamePlayEventState play, PlayerState player, TeamState team)
    {
        if (!play.ParticipantIds.Contains(player.PlayerId)) play.ParticipantIds.Add(player.PlayerId);
        if (!_result.BoxScore.PlayerStats.Any(s => s.PlayerId == player.PlayerId))
            _result.BoxScore.PlayerStats.Add(new PlayerGameStats { PlayerId = player.PlayerId, PlayerName = player.Name, TeamId = team.TeamId, Position = player.Position });
    }

    private void BeginDrive(string teamId, int yardLine, int sequence)
    {
        _state.PossessionTeamId = teamId; _state.YardLine = Math.Clamp(yardLine, 1, 99);
        _state.Down = 1; _state.Distance = Math.Min(10, 100 - _state.YardLine);
        _state.ClockRunning = false; _state.Phase = "scrimmage";
        _state.CurrentDrive = _state.Drives.Count + 1;
        _state.Drives.Add(new GameDriveState { Number = _state.CurrentDrive, TeamId = teamId, StartSequence = sequence, StartYardLine = _state.YardLine });
    }
    private void EndDrive(string outcome, int sequence)
    {
        var drive = _state.Drives.LastOrDefault();
        if (drive == null || drive.EndSequence != 0) return;
        drive.EndSequence = sequence; drive.Outcome = outcome;
        if (_state.Quarter >= 5 && !_state.OvertimePossessions.Contains(drive.TeamId)) _state.OvertimePossessions.Add(drive.TeamId);
    }
    private void CheckOvertime()
    {
        if (_state.Quarter >= 5 && _state.OvertimePossessions.Count == 2 && _result.HomeScore != _result.AwayScore) _state.Completed = true;
    }
    private void Score(GamePlayEventState play, string teamId, int points)
    {
        play.ScoringTeamId = teamId; play.Points = points;
        if (teamId == _home.TeamId) _result.HomeScore += points; else _result.AwayScore += points;
    }
    private void Kickoff(GamePlayEventState play)
    {
        var kicking = Team(_state.PossessionTeamId);
        var kicker = First(kicking, "K");
        play.PlayType = "kickoff";
        var call = _state.PendingDecision.SpecialTeams;
        if (string.IsNullOrEmpty(call)) call = Chance(55) ? "Deep kick" : "Return kick";
        play.SpecialTeamsCall = call;
        Line(play, kicker, kicking);
        var receiver = Other(kicking.TeamId);
        var spot = call == "Deep kick" ? 35 : 20 + Roll(21);
        if (call == "Return kick") _state.ClockSeconds = Math.Max(0, _state.ClockSeconds - 6);
        play.Outcome = call == "Deep kick" ? "touchback" : "kick_return";
        play.Description = $"{kicking.Abbreviation} kicks off. {Team(receiver).Abbreviation} starts at its {spot}.";
        BeginDrive(receiver, spot, play.Sequence);
        play.DriveNumber = _state.CurrentDrive;
    }

    private void Scrimmage(GamePlayEventState play)
    {
        var offense = Team(_state.PossessionTeamId);
        var defense = Team(Other(offense.TeamId));
        var ownScore = offense == _home ? _result.HomeScore : _result.AwayScore;
        var opposingScore = offense == _home ? _result.AwayScore : _result.HomeScore;
        var tempo = _state.PendingDecision.Tempo;
        var clockRules = ProClockManagementService.Enabled(_state);
        if (string.IsNullOrEmpty(tempo)) tempo = clockRules ? ProClockManagementService.TempoRecommendation(_result)
            : _state.ClockSeconds < 120 && _state.Quarter % 2 == 0 ? ownScore <= opposingScore ? "Hurry" : "Chew" : "Normal";
        var clockCall = _state.PendingDecision.Offense;
        if (clockRules && string.IsNullOrEmpty(clockCall) && First(offense, "QB") != null)
            clockCall = ProClockManagementService.OffenseRecommendation(_result);
        play.ManagementCall = tempo;
        var runoff = clockRules ? ProClockManagementService.Runoff(_state, tempo, clockCall)
            : !_state.ClockRunning ? 0 : tempo == "Hurry" ? 7 : tempo == "Chew" ? 38 : 24;
        if (clockRules && ProClockManagementService.HasWarning(_state) && _state.LastWarningQuarter != _state.Quarter
            && _state.ClockSeconds > 120 && _state.ClockSeconds - runoff <= 120)
        {
            _state.ClockSeconds = 120; TwoMinuteWarning(play); return;
        }
        if (runoff >= _state.ClockSeconds)
        {
            _state.ClockSeconds = 0; play.PlayType = "clock"; play.Outcome = "clock_expired"; play.Description = "The clock runs out before the next snap."; return;
        }
        var fourth = _state.PendingDecision.FourthDown;
        if (string.IsNullOrEmpty(fourth)) fourth = clockRules ? ProClockManagementService.DownRecommendation(_result)
            : (_state.YardLine > 50 && _state.Distance <= 2) || (ownScore < opposingScore && _state.Quarter >= 4 && play.StartClockSeconds < 150) ? "Go" : "Kick";
        if (clockRules && fourth != "Kick" && clockCall is "Kneel" or "Spike")
        {
            _state.ClockSeconds = Math.Max(0, _state.ClockSeconds - runoff - (clockCall == "Spike" ? 1 : 2));
            ResolveClockPlay(play, offense, clockCall); return;
        }
        _state.ClockSeconds = Math.Max(0, _state.ClockSeconds - runoff - 5 - Roll(5));
        if ((_state.Down == 4 || clockRules) && fourth == "Kick")
        {
            play.ManagementCall += "; Kick";
            var special = _state.PendingDecision.SpecialTeams;
            if (string.IsNullOrEmpty(special)) special = _state.YardLine >= 60 ? "Field goal" : "Punt";
            var specialist = First(offense, special == "Punt" ? "P" : "K");
            if (specialist != null) { KickFromScrimmage(play, offense, specialist, special); return; }
            // With no eligible specialist the offense must attempt the down; never activate an unavailable kicker.
        }
        if (_state.Down == 4) play.ManagementCall += "; Go";
        var attackers = Unit(offense, OffenseRoles); var defenders = Unit(defense, DefenseRoles);
        var qb = attackers.FirstOrDefault(p => p.Position == "QB");
        var runner = attackers.FirstOrDefault(p => p.Position == "RB") ?? qb;
        var targets = attackers.Where(p => p.Position is "WR" or "TE").ToList();
        if (runner == null) throw new InvalidOperationException($"{offense.Abbreviation} has no available quarterback or running back.");
        foreach (var player in attackers) Participate(play, player, offense);
        foreach (var player in defenders) Participate(play, player, defense);
        var defender = defenders.Count == 0 ? null : defenders[Roll(defenders.Count)];
        var defenseCall = _state.PendingDecision.Defense;
        if (string.IsNullOrEmpty(defenseCall)) defenseCall = _state.Distance >= 7 ? "Pass focus" : "Balanced";
        var offenseCall = _state.PendingDecision.Offense;
        if (string.IsNullOrEmpty(offenseCall)) offenseCall = Chance(_state.Distance >= 7 ? 62 : 42) ? "Pass" : "Run";
        if (qb == null || targets.Count == 0) offenseCall = "Run";
        play.OffensiveCall = offenseCall; play.DefensiveCall = defenseCall;
        var edge = Strength(attackers) - Strength(defenders) + Staff(offense, "Offensive Coordinator") - Staff(defense, "Defensive Coordinator") + (offense == _home ? _state.HomeFieldBonus : 0);
        PlayerState carrier;
        if (offenseCall == "Pass")
        {
            play.PlayType = "pass";
            var qbLine = Line(play, qb, offense);
            var receiver = targets[Roll(targets.Count)]; carrier = qb;
            if (Chance(7 - edge / 10 + (defenseCall == "Blitz" ? 5 : 0)))
            {
                play.Outcome = "sack"; play.YardsGained = -3 - Roll(7); qbLine.SacksTaken++;
                if (defender != null) Line(play, defender, defense).Sacks++;
            }
            else
            {
                qbLine.PassAttempts++;
                if (Chance(2 - edge / 30 + (defenseCall == "Pass focus" ? 1 : 0)) && defender != null)
                {
                    play.Outcome = "interception"; play.IsTurnover = true; qbLine.InterceptionsThrown++;
                    Line(play, defender, defense).Interceptions++;
                    var spot = Math.Min(100, _state.YardLine + 5 + Roll(26));
                    play.Description = $"{qb.Name} is intercepted by {defender.Name}.";
                    EndDrive("interception", play.Sequence); CheckOvertime();
                    if (!_state.Completed) BeginDrive(defense.TeamId, spot >= 100 ? 20 : 100 - spot, play.Sequence + 1);
                    MaybeInjury(play); return;
                }
                if (!Chance(65 + edge / 3 + (Ability(qb) - 70) / 4 - (defenseCall == "Pass focus" ? 7 : 0)))
                {
                    play.Outcome = "incomplete"; play.Description = $"{qb.Name}'s pass is incomplete."; _state.ClockRunning = false;
                    NextDown(play, defense.TeamId); MaybeInjury(play); return;
                }
                play.Outcome = "completion"; qbLine.Completions++;
                carrier = receiver; Line(play, receiver, offense).Receptions++;
                play.YardsGained = -2 + Roll(23) + edge / 10 + (defenseCall == "Blitz" ? 2 : 0);
                if (Chance(7)) play.YardsGained += 15 + Roll(25);
            }
        }
        else
        {
            play.PlayType = "run"; play.Outcome = "rush"; carrier = runner;
            Line(play, runner, offense).RushAttempts++;
            play.YardsGained = -3 + Roll(15) + edge / 12 + (defenseCall == "Run focus" ? -2 : defenseCall == "Pass focus" ? 1 : 0);
            if (Chance(6)) play.YardsGained += 12 + Roll(20);
        }
        play.YardsGained = Math.Clamp(play.YardsGained, -_state.YardLine, 100 - _state.YardLine);
        if (play.Outcome == "sack") Line(play, qb, offense).SackYardsLost = -play.YardsGained;
        else if (play.PlayType == "run") Line(play, carrier, offense).RushingYards = play.YardsGained;
        else { Line(play, qb, offense).PassingYards = play.YardsGained; Line(play, carrier, offense).ReceivingYards = play.YardsGained; }
        var end = _state.YardLine + play.YardsGained;
        _state.ClockRunning = true;
        if (end >= 100)
        {
            play.Outcome = "touchdown"; play.IsFirstDown = true; Score(play, offense.TeamId, 6);
            if (play.PlayType == "run") Line(play, carrier, offense).RushingTouchdowns++;
            else { Line(play, qb, offense).PassingTouchdowns++; Line(play, carrier, offense).ReceivingTouchdowns++; }
            play.Description = $"Touchdown: {carrier.Name}, {play.YardsGained}-yard {(play.PlayType == "run" ? "run" : "reception")}.";
            _state.YardLine = 98; _state.Down = 1; _state.Distance = 2; _state.Phase = "try"; _state.ClockRunning = false;
            // A winning second-possession/sudden-death TD needs no try.
            if (_state.Quarter >= 5 && _state.OvertimePossessions.Contains(defense.TeamId)
                && (offense == _home ? _result.HomeScore > _result.AwayScore : _result.AwayScore > _result.HomeScore))
            { EndDrive("touchdown", play.Sequence); _state.Completed = true; }
        }
        else if (end <= 0)
        {
            play.Outcome = "safety"; Score(play, defense.TeamId, 2);
            play.Description = $"Safety: {carrier.Name} is stopped in the end zone.";
            EndDrive("safety", play.Sequence);
            if (_state.Quarter >= 5) _state.Completed = true;
            _state.Phase = "kickoff"; _state.YardLine = 20; _state.Down = 1; _state.Distance = 10; _state.ClockRunning = false;
        }
        else
        {
            if (defender != null) Line(play, defender, defense).Tackles++;
            _state.YardLine = end;
            if (Chance(1))
            {
                play.Outcome = "fumble"; play.IsTurnover = true; Line(play, carrier, offense).FumblesLost++;
                play.Description = $"{carrier.Name} gains {play.YardsGained} yards and loses a fumble.";
                EndDrive("fumble", play.Sequence); CheckOvertime();
                if (!_state.Completed) BeginDrive(defense.TeamId, 100 - end, play.Sequence + 1);
            }
            else
            {
                play.Description = play.Outcome == "sack" ? $"{qb.Name} is sacked for {-play.YardsGained} yards." : $"{carrier.Name}: {play.YardsGained}-yard {(play.PlayType == "run" ? "run" : "reception")}.";
                if (play.YardsGained >= play.StartDistance)
                {
                    play.IsFirstDown = true; _state.Down = 1; _state.Distance = Math.Min(10, 100 - end);
                }
                else { _state.Distance = Math.Min(100 - end, play.StartDistance - play.YardsGained); NextDown(play, defense.TeamId); }
            }
        }
        MaybeInjury(play);
    }

    private bool TryTimeout(GamePlayEventState play)
    {
        var teamId = ProClockManagementService.TimeoutRecommendation(_league, _result);
        if (string.IsNullOrEmpty(teamId)) return false;
        if (teamId == _home.TeamId) _state.HomeTimeouts--; else _state.AwayTimeouts--;
        _state.ClockRunning = false;
        play.PlayType = "timeout"; play.Outcome = "team_timeout"; play.TimeoutTeamId = teamId;
        play.ManagementCall = "Use timeout";
        play.Description = $"{Team(teamId).Abbreviation} calls timeout; {ProClockManagementService.Timeouts(_result, teamId)} remaining.";
        return true;
    }

    private void TwoMinuteWarning(GamePlayEventState play)
    {
        _state.LastWarningQuarter = _state.Quarter; _state.ClockRunning = false;
        play.PlayType = "clock"; play.Outcome = "two_minute_warning"; play.Description = "Two-minute warning.";
    }

    private void ResolveClockPlay(GamePlayEventState play, TeamState offense, string call)
    {
        var qb = First(offense, "QB"); var defenseId = Other(offense.TeamId);
        foreach (var player in Unit(offense, OffenseRoles)) Participate(play, player, offense);
        foreach (var player in Unit(Team(defenseId), DefenseRoles)) Participate(play, player, Team(defenseId));
        var line = Line(play, qb, offense); play.OffensiveCall = call;
        if (call == "Spike")
        {
            play.PlayType = "pass"; play.Outcome = "spike"; line.PassAttempts++;
            play.Description = $"{qb.Name} spikes the ball to stop the clock."; _state.ClockRunning = false;
            NextDown(play, defenseId); return;
        }
        play.PlayType = "run"; play.Outcome = "kneel"; play.YardsGained = -1;
        line.RushAttempts++; line.RushingYards = -1; _state.ClockRunning = true;
        play.Description = $"{qb.Name} kneels for a loss of one yard.";
        _state.YardLine--;
        if (_state.YardLine == 0)
        {
            play.Outcome = "safety"; Score(play, defenseId, 2); EndDrive("safety", play.Sequence);
            play.Description += " Safety.";
            if (_state.Quarter >= 5) _state.Completed = true;
            _state.Phase = "kickoff"; _state.YardLine = 20; _state.Down = 1; _state.Distance = 10; _state.ClockRunning = false;
        }
        else
        {
            _state.Distance = Math.Min(100 - _state.YardLine, play.StartDistance + 1);
            NextDown(play, defenseId);
        }
    }

    private void NextDown(GamePlayEventState play, string defenseId)
    {
        if (_state.Down < 4) { _state.Down++; return; }
        play.IsTurnover = true; play.Description += " Turnover on downs.";
        EndDrive("downs", play.Sequence); CheckOvertime();
        if (!_state.Completed) BeginDrive(defenseId, 100 - _state.YardLine, play.Sequence + 1);
    }
    private void KickFromScrimmage(GamePlayEventState play, TeamState offense, PlayerState specialist, string call)
    {
        play.SpecialTeamsCall = call; _state.ClockRunning = false;
        var line = Line(play, specialist, offense);
        var opponent = Other(offense.TeamId);
        if (call == "Punt")
        {
            play.PlayType = "punt"; play.Outcome = "punt";
            var distance = 33 + Roll(22) + (Ability(specialist) - 70) / 8;
            var landing = _state.YardLine + distance;
            line.Punts++; line.PuntYards = Math.Min(distance, 100 - _state.YardLine);
            var spot = landing >= 100 ? 20 : Math.Clamp(100 - landing + Roll(9), 1, 99);
            play.Description = $"{specialist.Name} punts {line.PuntYards} yards. {Team(opponent).Abbreviation} takes over at its {spot}.";
            EndDrive("punt", play.Sequence); CheckOvertime();
            if (!_state.Completed) BeginDrive(opponent, spot, play.Sequence + 1);
        }
        else
        {
            play.PlayType = "field_goal"; line.FieldGoalAttempts++;
            var distance = 117 - _state.YardLine;
            var made = Chance(Math.Clamp(98 - Math.Max(0, distance - 30) * 2 + (Ability(specialist) - 70) / 3, 2, 98));
            play.Outcome = made ? "field_goal_made" : "field_goal_missed";
            play.Description = $"{specialist.Name}'s {distance}-yard field goal is {(made ? "good" : "no good")}.";
            if (made) { line.FieldGoalsMade++; Score(play, offense.TeamId, 3); }
            EndDrive(play.Outcome, play.Sequence); CheckOvertime();
            if (!_state.Completed)
            {
                if (made) { _state.Phase = "kickoff"; _state.YardLine = 35; _state.Down = 1; _state.Distance = 10; }
                else BeginDrive(opponent, Math.Max(20, 107 - _state.YardLine), play.Sequence + 1);
            }
        }
    }
    private void TryAfterTouchdown(GamePlayEventState play)
    {
        var team = Team(_state.PossessionTeamId);
        var kicker = First(team, "K");
        var call = _state.PendingDecision.SpecialTeams;
        if (string.IsNullOrEmpty(call))
        {
            var deficit = team == _home ? _result.AwayScore - _result.HomeScore : _result.HomeScore - _result.AwayScore;
            call = kicker == null || (_state.Quarter >= 4 && deficit == 2) ? "Two point" : "Extra point";
        }
        play.PlayType = "try"; play.SpecialTeamsCall = call;
        if (call == "Extra point" && kicker != null)
        {
            var line = Line(play, kicker, team); line.ExtraPointAttempts++;
            var made = Chance(94 + (Ability(kicker) - 70) / 5);
            if (made) { line.ExtraPointsMade++; Score(play, team.TeamId, 1); }
            play.Outcome = made ? "extra_point_made" : "extra_point_missed";
            play.Description = $"{kicker.Name}'s extra point is {(made ? "good" : "no good")}.";
        }
        else
        {
            var runner = First(team, "RB") ?? First(team, "QB");
            var made = runner != null && Chance(45 + (Ability(runner) - 70) / 3);
            if (runner != null) { var line = Line(play, runner, team); if (made) line.TwoPointConversions++; }
            if (made) Score(play, team.TeamId, 2);
            play.Outcome = made ? "two_point_made" : "two_point_failed";
            play.Description = made ? "The two-point try succeeds." : "The two-point try fails.";
        }
        EndDrive("touchdown", play.Sequence); CheckOvertime();
        _state.Phase = "kickoff"; _state.YardLine = 35; _state.Down = 1; _state.Distance = 10; _state.ClockRunning = false;
    }
    private void EndPeriod(GamePlayEventState play)
    {
        play.PlayType = "period"; play.Outcome = "end_period";
        play.Description = $"End of period {_state.Quarter}.";
        if (_state.Quarter == 4)
        {
            EndDrive("regulation ended", play.Sequence);
            if (_result.HomeScore != _result.AwayScore || (_result.GameType == "preseason" && !_state.RequireWinner)) { _state.Completed = true; return; }
            _state.Quarter = 5; _state.ClockSeconds = _state.RequireWinner ? 900 : 600;
            if (ProClockManagementService.Enabled(_state)) _state.HomeTimeouts = _state.AwayTimeouts = _state.RequireWinner ? 3 : 2;
            _state.PossessionTeamId = Roll(2) == 0 ? _home.TeamId : _away.TeamId;
            _state.Phase = "kickoff"; _state.YardLine = 35; _state.Down = 1; _state.Distance = 10; _state.ClockRunning = false;
            play.Description += " Overtime begins; both teams have an initial possession opportunity, subject to the game clock.";
            return;
        }
        if (_state.Quarter >= 5 && !_state.RequireWinner) { _state.Completed = true; return; }
        if (_state.Quarter == 2)
        {
            if (ProClockManagementService.Enabled(_state)) _state.HomeTimeouts = _state.AwayTimeouts = 3;
            EndDrive("halftime", play.Sequence);
            _state.PossessionTeamId = _state.OpeningReceiverId; _state.Phase = "kickoff";
            _state.YardLine = 35; _state.Down = 1; _state.Distance = 10;
            play.Description = "Halftime. The other team receives the second-half kickoff.";
        }
        if (ProClockManagementService.Enabled(_state) && _state.Quarter >= 6 && _state.Quarter % 2 == 0)
            _state.HomeTimeouts = _state.AwayTimeouts = 3;
        _state.Quarter++; _state.ClockSeconds = 900; _state.ClockRunning = false;
    }
    private void MaybeInjury(GamePlayEventState play)
    {
        // Bounded first slice: at most one new game injury, using the existing medical model on commit.
        if (_state.InjuredPlayerIds.Count > 0 || play.ParticipantIds.Count == 0 || Roll(1000) >= 2) return;
        var player = _players[play.ParticipantIds[Roll(play.ParticipantIds.Count)]];
        _state.InjuredPlayerIds.Add(player.PlayerId);
        play.IsInjury = true; play.InjuredPlayerId = player.PlayerId;
        play.Injury = new PlayerInjuryState { Name = "Ankle sprain", DaysRemaining = 3 + Roll(12), GameId = _result.GameId, OccurredOn = _league.Calendar.CurrentDate };
        play.Description += $" {player.Name} leaves with an ankle sprain.";
    }
}
