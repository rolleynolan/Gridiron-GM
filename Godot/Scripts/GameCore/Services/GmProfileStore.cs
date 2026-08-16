using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class GmProfileStore
{
    private static readonly string ProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridironGM", "gm_profiles_v1.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public IReadOnlyList<GmProfile> LoadAll()
    {
        try { return File.Exists(ProfilePath) ? JsonSerializer.Deserialize<List<GmProfile>>(File.ReadAllText(ProfilePath), JsonOptions) ?? new List<GmProfile>() : new List<GmProfile>(); }
        catch (Exception) { return new List<GmProfile>(); }
    }
    public void Save(GmProfile profile)
    {
        profile.Validate();
        var profiles = LoadAll().ToList();
        var index = profiles.FindIndex(item => string.Equals(item.Id, profile.Id, StringComparison.Ordinal));
        if (index >= 0) profiles[index] = profile; else profiles.Add(profile);
        Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath)!);
        File.WriteAllText(ProfilePath, JsonSerializer.Serialize(profiles, JsonOptions));
    }
}
