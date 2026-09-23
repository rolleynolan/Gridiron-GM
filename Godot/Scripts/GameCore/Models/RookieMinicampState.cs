using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class RookieMinicampState
{
    public int SeasonYear { get; set; }
    public List<string> InvitedPlayerIds { get; set; } = new();
}
