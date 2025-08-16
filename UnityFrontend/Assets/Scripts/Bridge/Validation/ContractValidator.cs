using System;
using Gridiron.Bridge.Dto;

namespace Gridiron.Bridge.Validation
{
    public class GGDataException : Exception
    {
        public string Code { get; }

        public GGDataException(string code, string message) : base(message)
        {
            Code = code;
        }
    }

    public static class ContractValidator
    {
        public static void Validate(ContractDTO c)
        {
            if (c == null)
            {
                throw new GGDataException("GG2001", "Null contract payload");
            }

            if (c.ApiVersion != "gg.v1")
            {
                throw new GGDataException("GG2002", "Version " + c.ApiVersion + " not supported");
            }

            if (c.Terms == null || c.Terms.Count == 0)
            {
                throw new GGDataException("GG2003", "Missing terms[]");
            }

            foreach (var term in c.Terms)
            {
                if (term.Year < 2000 || term.Year > 2100)
                {
                    throw new GGDataException("GG2003", "Term year out of range");
                }

                if (term.Base < 0 || term.SigningProrated < 0 || term.RosterBonus < 0 || term.WorkoutBonus < 0 || term.GuaranteedBase < 0)
                {
                    throw new GGDataException("GG2003", "Negative amounts");
                }
            }
        }
    }
}
