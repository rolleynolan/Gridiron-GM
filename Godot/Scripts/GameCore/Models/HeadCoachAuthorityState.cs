using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class HeadCoachAuthorityState
{
    public List<string> ControlledDomains { get; set; } = new();
}
