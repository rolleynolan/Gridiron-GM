using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GridironGM.GameCore.Services;

public sealed class GeneratedNamePools
{
    public IReadOnlyList<string> MaleFirstNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FemaleFirstNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LastNames { get; init; } = Array.Empty<string>();
}

public static class NamePoolService
{
    public static GeneratedNamePools Load(string teamSeedPath = null)
    {
        var directory = string.IsNullOrWhiteSpace(teamSeedPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "Assets", "data_seed")
            : Path.GetDirectoryName(Path.GetFullPath(teamSeedPath));

        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Unable to locate the name pool directory.");

        return new GeneratedNamePools
        {
            MaleFirstNames = ReadPool(Path.Combine(directory, "male_first_names.txt"), "male first names"),
            FemaleFirstNames = ReadPool(Path.Combine(directory, "female_first_names.txt"), "female first names"),
            LastNames = ReadPool(Path.Combine(directory, "last_names.txt"), "last names"),
        };
    }

    private static IReadOnlyList<string> ReadPool(string path, string label)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing {label} pool at {path}.", path);

        var entries = File.ReadAllText(path)
            .Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (entries.Count == 0)
            throw new InvalidDataException($"The {label} pool is empty.");

        return entries;
    }
}
