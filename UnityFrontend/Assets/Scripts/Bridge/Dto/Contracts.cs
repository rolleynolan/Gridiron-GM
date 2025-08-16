using System;
using System.Collections.Generic;
using Gridiron.Core;

namespace Gridiron.Bridge.Dto
{
    [Serializable]
    public class ContractYearTerm
    {
        public int Year;
        public long Base;
        public long SigningProrated;
        public long RosterBonus;
        public long WorkoutBonus;
        public long GuaranteedBase;
    }

    [Serializable]
    public class Guarantee
    {
        public string Type;
        public int ThroughYear;
    }

    [Serializable]
    public class Incentive
    {
        public string Type;
        public long Amount;
        public string Metric;
        public string Threshold;
    }

    [Serializable]
    public class ContractDTO
    {
        public string ApiVersion;
        public int StartYear;
        public int EndYear;
        public List<ContractYearTerm> Terms;
        public List<Guarantee> Guarantees;
        public List<Incentive> Incentives;
        public Dictionary<string, bool> Flags;
        public List<string> Notes;
        public Dictionary<string, object> Extra = new Dictionary<string, object>();

        public static ContractDTO FromJson(string json)
        {
            var dict = MiniJson.Deserialize(json) as Dictionary<string, object>;
            var dto = new ContractDTO();
            if (dict == null) return dto;

            dto.ApiVersion = GetString(dict, "apiVersion");
            dto.StartYear = GetInt(dict, "startYear");
            dto.EndYear = GetInt(dict, "endYear");
            dto.Terms = ParseTerms(dict);
            dto.Guarantees = ParseGuarantees(dict);
            dto.Incentives = ParseIncentives(dict);
            dto.Flags = ParseFlags(dict);
            dto.Notes = ParseNotes(dict);

            var known = new HashSet<string> { "apiVersion", "startYear", "endYear", "terms", "guarantees", "incentives", "flags", "notes" };
            foreach (var kv in dict)
            {
                if (!known.Contains(kv.Key))
                {
                    dto.Extra[kv.Key] = kv.Value;
                }
            }
            return dto;
        }

        private static List<ContractYearTerm> ParseTerms(Dictionary<string, object> dict)
        {
            var list = new List<ContractYearTerm>();
            if (dict.TryGetValue("terms", out var obj) && obj is List<object> arr)
            {
                foreach (var item in arr)
                {
                    var d = item as Dictionary<string, object>;
                    if (d == null) continue;
                    var term = new ContractYearTerm
                    {
                        Year = GetInt(d, "year"),
                        Base = GetLong(d, "base"),
                        SigningProrated = GetLong(d, "signingProrated"),
                        RosterBonus = GetLong(d, "rosterBonus"),
                        WorkoutBonus = GetLong(d, "workoutBonus"),
                        GuaranteedBase = GetLong(d, "guaranteedBase")
                    };
                    list.Add(term);
                }
            }
            return list;
        }

        private static List<Guarantee> ParseGuarantees(Dictionary<string, object> dict)
        {
            var list = new List<Guarantee>();
            if (dict.TryGetValue("guarantees", out var obj) && obj is List<object> arr)
            {
                foreach (var item in arr)
                {
                    var d = item as Dictionary<string, object>;
                    if (d == null) continue;
                    list.Add(new Guarantee
                    {
                        Type = GetString(d, "type"),
                        ThroughYear = GetInt(d, "throughYear")
                    });
                }
            }
            return list;
        }

        private static List<Incentive> ParseIncentives(Dictionary<string, object> dict)
        {
            var list = new List<Incentive>();
            if (dict.TryGetValue("incentives", out var obj) && obj is List<object> arr)
            {
                foreach (var item in arr)
                {
                    var d = item as Dictionary<string, object>;
                    if (d == null) continue;
                    list.Add(new Incentive
                    {
                        Type = GetString(d, "type"),
                        Amount = GetLong(d, "amount"),
                        Metric = GetString(d, "metric"),
                        Threshold = GetString(d, "threshold")
                    });
                }
            }
            return list;
        }

        private static Dictionary<string, bool> ParseFlags(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, bool>();
            if (dict.TryGetValue("flags", out var obj) && obj is Dictionary<string, object> d)
            {
                foreach (var kv in d)
                {
                    result[kv.Key] = Convert.ToBoolean(kv.Value);
                }
            }
            return result;
        }

        private static List<string> ParseNotes(Dictionary<string, object> dict)
        {
            var list = new List<string>();
            if (dict.TryGetValue("notes", out var obj) && obj is List<object> arr)
            {
                foreach (var item in arr)
                {
                    if (item is string s)
                    {
                        list.Add(s);
                    }
                }
            }
            return list;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var obj)) return obj as string;
            return null;
        }

        private static int GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var obj))
            {
                return Convert.ToInt32(obj);
            }
            return 0;
        }

        private static long GetLong(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var obj))
            {
                return Convert.ToInt64(obj);
            }
            return 0L;
        }
    }
}
