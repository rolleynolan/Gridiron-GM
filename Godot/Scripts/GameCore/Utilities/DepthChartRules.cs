using System;
using System.Collections.Generic;

namespace GridironGM.GameCore.Utilities;

public static class DepthChartRules
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static readonly IReadOnlyDictionary<string, int> RequiredStartersByPosition =
        new Dictionary<string, int>(Comparer)
        {
            ["QB"] = 1,
            ["RB"] = 1,
            ["WR"] = 2,
            ["TE"] = 1,
            ["LT"] = 1,
            ["LG"] = 1,
            ["C"] = 1,
            ["RG"] = 1,
            ["RT"] = 1,
            ["EDGE"] = 2,
            ["DT"] = 2,
            ["LB"] = 2,
            ["CB"] = 2,
            ["S"] = 2,
            ["K"] = 1,
            ["P"] = 1,
        };

    public static int GetRequiredStarters(string position)
        => RequiredStartersByPosition.TryGetValue(position ?? "", out var value) ? value : 1;
}
