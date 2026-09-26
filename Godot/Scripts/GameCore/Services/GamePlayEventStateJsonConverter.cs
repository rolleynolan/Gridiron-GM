using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

/// <summary>Compact current play logs while accepting the verbose schema written by saves 33-37.</summary>
public sealed class GamePlayEventStateJsonConverter : JsonConverter<GamePlayEventState>
{
    public override GamePlayEventState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var result = new GamePlayEventState();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            switch (property.Name)
            {
                case "s": case "Sequence": result.Sequence = property.Value.GetInt32(); break;
                case "q": case "Quarter": result.Quarter = property.Value.GetInt32(); break;
                case "c": case "ClockSeconds": result.ClockSeconds = property.Value.GetInt32(); break;
                case "p": case "PossessionTeamId": result.PossessionTeamId = Text(property.Value); break;
                case "d": case "Down": result.Down = property.Value.GetInt32(); break;
                case "n": case "Distance": result.Distance = property.Value.GetInt32(); break;
                case "y": case "YardLine": result.YardLine = property.Value.GetInt32(); break;
                case "g": case "YardsGained": result.YardsGained = property.Value.GetInt32(); break;
                case "x": case "Description": result.Description = Text(property.Value); break;
                case "h": case "HomeScore": result.HomeScore = property.Value.GetInt32(); break;
                case "a": case "AwayScore": result.AwayScore = property.Value.GetInt32(); break;
                case "b": ApplyFlags(result, property.Value.GetInt32()); break;
                case "IsScoringPlay": result.IsScoringPlay = property.Value.GetBoolean(); break;
                case "IsTurnover": result.IsTurnover = property.Value.GetBoolean(); break;
                case "IsInjury": result.IsInjury = property.Value.GetBoolean(); break;
                case "IsFinal": result.IsFinal = property.Value.GetBoolean(); break;
                case "IsFirstDown": result.IsFirstDown = property.Value.GetBoolean(); break;
                case "t": case "PlayType": result.PlayType = Text(property.Value); break;
                case "o": case "Outcome": result.Outcome = Text(property.Value); break;
                case "f": case "OffensiveTeamId": result.OffensiveTeamId = Text(property.Value); break;
                case "r": case "ScoringTeamId": result.ScoringTeamId = Text(property.Value); break;
                case "z": case "Points": result.Points = property.Value.GetInt32(); break;
                case "v": case "DriveNumber": result.DriveNumber = property.Value.GetInt32(); break;
                case "Y": case "StartYardLine": result.StartYardLine = property.Value.GetInt32(); break;
                case "D": case "StartDown": result.StartDown = property.Value.GetInt32(); break;
                case "N": case "StartDistance": result.StartDistance = property.Value.GetInt32(); break;
                case "C": case "StartClockSeconds": result.StartClockSeconds = property.Value.GetInt32(); break;
                case "e": case "ElapsedSeconds": result.ElapsedSeconds = property.Value.GetInt32(); break;
                case "u": case "OffensiveCall": result.OffensiveCall = Text(property.Value); break;
                case "w": case "DefensiveCall": result.DefensiveCall = Text(property.Value); break;
                case "m": case "ManagementCall": result.ManagementCall = Text(property.Value); break;
                case "k": case "SpecialTeamsCall": result.SpecialTeamsCall = Text(property.Value); break;
                case "T": case "TimeoutTeamId": result.TimeoutTeamId = Text(property.Value); break;
                case "I": case "InjuredPlayerId": result.InjuredPlayerId = Text(property.Value); break;
                case "i": case "Injury": result.Injury = property.Value.Deserialize<PlayerInjuryState>(options); break;
                case "P": case "ParticipantIds": result.ParticipantIds = property.Value.Deserialize<List<string>>(options) ?? new(); break;
                case "S": result.StatChanges = ReadStats(property.Value); break;
                case "StatChanges": result.StatChanges = property.Value.Deserialize<List<PlayerGameStats>>(options) ?? new(); break;
            }
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, GamePlayEventState value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        Number(writer, "s", value.Sequence); Number(writer, "q", value.Quarter); Number(writer, "c", value.ClockSeconds);
        String(writer, "p", value.PossessionTeamId); Number(writer, "d", value.Down); Number(writer, "n", value.Distance);
        Number(writer, "y", value.YardLine); Number(writer, "g", value.YardsGained, writeZero: false); String(writer, "x", value.Description);
        Number(writer, "h", value.HomeScore, writeZero: false); Number(writer, "a", value.AwayScore, writeZero: false);
        var flags = (value.IsScoringPlay ? 1 : 0) | (value.IsTurnover ? 2 : 0) | (value.IsInjury ? 4 : 0) | (value.IsFinal ? 8 : 0) | (value.IsFirstDown ? 16 : 0);
        Number(writer, "b", flags, writeZero: false);
        String(writer, "t", value.PlayType); String(writer, "o", value.Outcome); String(writer, "f", value.OffensiveTeamId); String(writer, "r", value.ScoringTeamId);
        Number(writer, "z", value.Points, false); Number(writer, "v", value.DriveNumber, false);
        Number(writer, "Y", value.StartYardLine, false); Number(writer, "D", value.StartDown, false); Number(writer, "N", value.StartDistance, false);
        Number(writer, "C", value.StartClockSeconds, false); Number(writer, "e", value.ElapsedSeconds, false);
        String(writer, "u", value.OffensiveCall); String(writer, "w", value.DefensiveCall); String(writer, "m", value.ManagementCall); String(writer, "k", value.SpecialTeamsCall);
        String(writer, "T", value.TimeoutTeamId); String(writer, "I", value.InjuredPlayerId);
        if (value.Injury != null) { writer.WritePropertyName("i"); JsonSerializer.Serialize(writer, value.Injury, options); }
        if (value.ParticipantIds?.Count > 0) { writer.WritePropertyName("P"); JsonSerializer.Serialize(writer, value.ParticipantIds, options); }
        if (value.StatChanges?.Count > 0)
        {
            writer.WritePropertyName("S"); writer.WriteStartArray();
            foreach (var stats in value.StatChanges) WriteStats(writer, stats);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private static void ApplyFlags(GamePlayEventState value, int flags)
    {
        value.IsScoringPlay = (flags & 1) != 0; value.IsTurnover = (flags & 2) != 0; value.IsInjury = (flags & 4) != 0;
        value.IsFinal = (flags & 8) != 0; value.IsFirstDown = (flags & 16) != 0;
    }

    private static string Text(JsonElement value) => value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? "";
    private static void String(Utf8JsonWriter writer, string name, string value) { if (!string.IsNullOrEmpty(value)) writer.WriteString(name, value); }
    private static void Number(Utf8JsonWriter writer, string name, int value, bool writeZero = true) { if (writeZero || value != 0) writer.WriteNumber(name, value); }

    private static List<PlayerGameStats> ReadStats(JsonElement array)
    {
        var results = new List<PlayerGameStats>();
        foreach (var element in array.EnumerateArray())
        {
            var stats = new PlayerGameStats();
            foreach (var property in element.EnumerateObject())
            {
                var number = property.Value.ValueKind == JsonValueKind.Number ? property.Value.GetInt32() : 0;
                switch (property.Name)
                {
                    case "i": stats.PlayerId = Text(property.Value); break; case "n": stats.PlayerName = Text(property.Value); break;
                    case "t": stats.TeamId = Text(property.Value); break; case "p": stats.Position = Text(property.Value); break;
                    case "py": stats.PassingYards = number; break; case "pt": stats.PassingTouchdowns = number; break;
                    case "ry": stats.RushingYards = number; break; case "rt": stats.RushingTouchdowns = number; break;
                    case "cy": stats.ReceivingYards = number; break; case "ct": stats.ReceivingTouchdowns = number; break;
                    case "tk": stats.Tackles = number; break; case "sk": stats.Sacks = number; break; case "di": stats.Interceptions = number; break;
                    case "sn": stats.Snaps = number; break; case "pa": stats.PassAttempts = number; break; case "co": stats.Completions = number; break;
                    case "it": stats.InterceptionsThrown = number; break; case "st": stats.SacksTaken = number; break; case "sl": stats.SackYardsLost = number; break;
                    case "ra": stats.RushAttempts = number; break; case "re": stats.Receptions = number; break; case "fl": stats.FumblesLost = number; break;
                    case "fa": stats.FieldGoalAttempts = number; break; case "fm": stats.FieldGoalsMade = number; break;
                    case "ea": stats.ExtraPointAttempts = number; break; case "em": stats.ExtraPointsMade = number; break; case "2p": stats.TwoPointConversions = number; break;
                    case "pu": stats.Punts = number; break; case "uy": stats.PuntYards = number; break;
                }
            }
            results.Add(stats);
        }
        return results;
    }

    private static void WriteStats(Utf8JsonWriter writer, PlayerGameStats value)
    {
        writer.WriteStartObject();
        String(writer, "i", value.PlayerId); String(writer, "n", value.PlayerName); String(writer, "t", value.TeamId); String(writer, "p", value.Position);
        Number(writer, "py", value.PassingYards, false); Number(writer, "pt", value.PassingTouchdowns, false); Number(writer, "ry", value.RushingYards, false); Number(writer, "rt", value.RushingTouchdowns, false);
        Number(writer, "cy", value.ReceivingYards, false); Number(writer, "ct", value.ReceivingTouchdowns, false); Number(writer, "tk", value.Tackles, false); Number(writer, "sk", value.Sacks, false); Number(writer, "di", value.Interceptions, false);
        Number(writer, "sn", value.Snaps, false); Number(writer, "pa", value.PassAttempts, false); Number(writer, "co", value.Completions, false); Number(writer, "it", value.InterceptionsThrown, false);
        Number(writer, "st", value.SacksTaken, false); Number(writer, "sl", value.SackYardsLost, false); Number(writer, "ra", value.RushAttempts, false); Number(writer, "re", value.Receptions, false); Number(writer, "fl", value.FumblesLost, false);
        Number(writer, "fa", value.FieldGoalAttempts, false); Number(writer, "fm", value.FieldGoalsMade, false); Number(writer, "ea", value.ExtraPointAttempts, false); Number(writer, "em", value.ExtraPointsMade, false);
        Number(writer, "2p", value.TwoPointConversions, false); Number(writer, "pu", value.Punts, false); Number(writer, "uy", value.PuntYards, false);
        writer.WriteEndObject();
    }
}
