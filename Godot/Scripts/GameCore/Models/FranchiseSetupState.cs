using System;

namespace GridironGM.GameCore.Models;

public enum RosterSource { Standard, Generated }

public sealed class GmAttributes
{
    public const int Minimum = 20;
    public const int Maximum = 80;
    public const int MaximumTotal = 220;
    public int Negotiation { get; set; } = 50;
    public int PlayerManagement { get; set; } = 50;
    public int ScoutingJudgment { get; set; } = 50;
    public int Leadership { get; set; } = 50;
    public int Total => Negotiation + PlayerManagement + ScoutingJudgment + Leadership;
    public void Validate()
    {
        ValidateRating(Negotiation, nameof(Negotiation)); ValidateRating(PlayerManagement, nameof(PlayerManagement));
        ValidateRating(ScoutingJudgment, nameof(ScoutingJudgment)); ValidateRating(Leadership, nameof(Leadership));
        if (Total > MaximumTotal) throw new ArgumentException($"Attributes may not exceed {MaximumTotal} points.");
    }
    private static void ValidateRating(int value, string name)
    {
        if (value < Minimum || value > Maximum) throw new ArgumentOutOfRangeException(name, $"{name} must be between {Minimum} and {Maximum}.");
    }
}

public sealed class CharacterDesign
{
    public string Pronouns { get; set; } = "They/Them";
    public string HairStyle { get; set; } = "Short";
    public string HairColor { get; set; } = "Brown";
    public string SkinTone { get; set; } = "Medium";
    public string Outfit { get; set; } = "Team Polo";
}

public sealed class GmProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "User GM";
    public GmAttributes Attributes { get; set; } = new();
    public CharacterDesign Appearance { get; set; } = new();
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("GM name is required.");
        Attributes ??= new GmAttributes(); Appearance ??= new CharacterDesign(); Attributes.Validate();
    }
    public GmProfile Snapshot() => new() { Id = Id, Name = Name, Attributes = new GmAttributes { Negotiation = Attributes.Negotiation, PlayerManagement = Attributes.PlayerManagement, ScoutingJudgment = Attributes.ScoutingJudgment, Leadership = Attributes.Leadership }, Appearance = new CharacterDesign { Pronouns = Appearance.Pronouns, HairStyle = Appearance.HairStyle, HairColor = Appearance.HairColor, SkinTone = Appearance.SkinTone, Outfit = Appearance.Outfit } };
}

public sealed class WorldDefinition
{
    public const ulong StandardSeed = 0x4752494449524F4EUL;
    public const int GeneratorVersion = 1;
    public RosterSource Source { get; set; } = RosterSource.Standard;
    public ulong Seed { get; set; } = StandardSeed;
    public int Version { get; set; } = GeneratorVersion;
    public static WorldDefinition Standard() => new();
    public static WorldDefinition Generated(ulong seed) => new() { Source = RosterSource.Generated, Seed = seed };
}

public sealed class FranchiseMetadata
{
    public int SchemaVersion { get; set; } = 2;
    public WorldDefinition World { get; set; } = WorldDefinition.Standard();
    public GmProfile GmProfileSnapshot { get; set; } = new();
}
