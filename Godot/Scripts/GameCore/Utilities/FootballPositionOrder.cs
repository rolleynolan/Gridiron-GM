using System;
using System.Collections.Generic;

namespace GridironGM.GameCore.Utilities;

public static class FootballPositionOrder
{
    private static readonly Dictionary<string, int> PositionRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["QB"] = 0,
        ["RB"] = 1,
        ["FB"] = 2,
        ["WR"] = 3,
        ["TE"] = 4,
        ["LT"] = 5,
        ["LG"] = 6,
        ["C"] = 7,
        ["RG"] = 8,
        ["RT"] = 9,
        ["OL"] = 10,
        ["EDGE"] = 11,
        ["DE"] = 12,
        ["DT"] = 13,
        ["NT"] = 14,
        ["DL"] = 15,
        ["OLB"] = 16,
        ["ILB"] = 17,
        ["MLB"] = 18,
        ["LB"] = 19,
        ["CB"] = 20,
        ["FS"] = 21,
        ["SS"] = 22,
        ["S"] = 23,
        ["K"] = 24,
        ["P"] = 25,
        ["LS"] = 26,
    };

    public static int GetSortOrder(string position)
    {
        if (string.IsNullOrWhiteSpace(position))
            return int.MaxValue;

        return PositionRanks.TryGetValue(position.Trim(), out var rank)
            ? rank
            : int.MaxValue;
    }

    public static int Compare(string left, string right)
    {
        var leftRank = GetSortOrder(left);
        var rightRank = GetSortOrder(right);
        var rankCompare = leftRank.CompareTo(rightRank);
        if (rankCompare != 0)
            return rankCompare;

        return StringComparer.OrdinalIgnoreCase.Compare(
            NormalizeForComparison(left),
            NormalizeForComparison(right));
    }

    private static string NormalizeForComparison(string position)
        => string.IsNullOrWhiteSpace(position) ? "~" : position.Trim().ToUpperInvariant();
}
