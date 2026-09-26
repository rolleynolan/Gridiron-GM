using System;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class PlayerMedicalReportService
{
    private const string AutomaticTreatment = "Conservative rehabilitation and medical monitoring (staff selected)";

    public static PlayerMedicalReport Build(LeagueState league, TeamState team, PlayerState player, bool isOnInjuredReserve)
    {
        if (player == null)
            return new PlayerMedicalReport
            {
                Diagnosis = "Medical record unavailable",
                EvaluationStatus = "Evaluation unavailable",
                ClearanceStatus = "Clearance unavailable",
                EstimatedClearanceWindow = "Estimate unavailable",
                EstimateContext = "No player medical record is available.",
                Restrictions = "Availability cannot be verified.",
                ReportedOn = league?.Calendar?.CurrentDate ?? "",
            };

        var injury = player.CurrentInjury;
        var hasActiveInjury = injury?.IsActive == true || !string.IsNullOrWhiteSpace(player.Injury);
        var reportDate = league?.Calendar?.CurrentDate ?? "";
        if (!hasActiveInjury)
        {
            var clearance = isOnInjuredReserve
                ? "Medically cleared; roster activation required"
                : "Medically cleared";
            return new PlayerMedicalReport
            {
                Diagnosis = "No active injury",
                EvaluationStatus = "No evaluation pending",
                Treatment = "No active treatment",
                ClearanceStatus = clearance,
                EstimatedClearanceWindow = "Not applicable",
                EstimateContext = string.IsNullOrWhiteSpace(reportDate) ? "Current report date unavailable." : $"Status reviewed {reportDate}.",
                Restrictions = isOnInjuredReserve ? "Not game-eligible until activated from injured reserve." : "No medical game restriction.",
                ReportedOn = reportDate,
            };
        }

        var diagnosis = !string.IsNullOrWhiteSpace(injury?.Name)
            ? injury.Name
            : !string.IsNullOrWhiteSpace(player.Injury)
                ? player.Injury
                : "Under medical evaluation";
        var daysRemaining = injury?.DaysRemaining ?? 0;
        var asOf = default(DateTime);
        var hasDatedProjection = daysRemaining > 0 && PlayerInjuryService.TryDate(reportDate, out asOf);
        var estimate = "Pending medical evaluation";
        if (hasDatedProjection)
        {
            var expectedDays = (int)Math.Ceiling(daysRemaining / (double)PlayerInjuryService.GetDailyRecoveryRate(team));
            var uncertainty = Math.Max(1, (int)Math.Ceiling(expectedDays * 0.20));
            var earliest = asOf.AddDays(Math.Max(1, expectedDays - uncertainty));
            var latest = asOf.AddDays(expectedDays + uncertainty);
            estimate = $"{PlayerInjuryService.FormatDate(earliest)} to {PlayerInjuryService.FormatDate(latest)}";
        }

        return new PlayerMedicalReport
        {
            Diagnosis = diagnosis,
            EvaluationStatus = hasDatedProjection ? "Diagnosis recorded" : "Recovery timetable under evaluation",
            Treatment = AutomaticTreatment,
            ClearanceStatus = "Not medically cleared",
            EstimatedClearanceWindow = estimate,
            EstimateContext = hasDatedProjection
                ? $"Estimated medical-clearance window as of {reportDate}; not guaranteed and separate from performance readiness."
                : "Medical staff have not published a dated clearance range.",
            Restrictions = isOnInjuredReserve
                ? "Unavailable for games; injured-reserve activation is required after clearance."
                : "Unavailable for game selection until medically cleared.",
            ReportedOn = reportDate,
            HasActiveInjury = true,
        };
    }
}
