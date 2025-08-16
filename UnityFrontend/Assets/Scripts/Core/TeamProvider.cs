using System;
using System.Collections.Generic;
using System.IO;

namespace Gridiron.Core
{
    public interface ITeamProvider
    {
        IEnumerable<string> GetAllTeamAbbrs();
    }

    public class TeamProvider : ITeamProvider
    {
        public IEnumerable<string> GetAllTeamAbbrs()
        {
            var results = new List<string>();
            var path = GGPaths.Streaming(GGConventions.TeamsJsonFile);
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var data = MiniJson.Deserialize(json) as List<object>;
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict != null && dict.TryGetValue("abbr", out var abbrObj))
                            {
                                var abbr = abbrObj as string;
                                if (!string.IsNullOrEmpty(abbr))
                                {
                                    results.Add(abbr);
                                }
                            }
                        }
                    }
                }
                else
                {
                    GGLog.Warn("Teams file missing: " + path);
                }
            }
            catch (Exception ex)
            {
                GGLog.Error("TeamProvider error: " + ex.Message);
            }
            return results;
        }
    }
}
