// ============================================================
//  FuzzyEngine.cs
//  Core fuzzy logic system adapted for Biodome Ares-1.
//
//  Inputs:  Effective temperature (°F) and humidity (%)
//           after control modifiers are applied.
//
//  Output:  SystemStress (0.0–1.0)
//           0.0 = perfectly nominal
//           1.0 = critical — meters drain at max rate
//
//  The four controls (fans, shield, coolant, power) reduce
//  the effective inputs before they reach this engine.
//  The engine itself never knows about controls — it only
//  sees the resulting environmental conditions.
// ============================================================

using System;
using System.Collections.Generic;

namespace BiodomeAres1.Engine
{
    // ── Membership Functions ─────────────────────────────────
 
    /// <summary>
    /// Temperature membership functions (°F).
    /// Ranges tuned for Mars Biodome habitation thresholds.
    /// </summary>
    public static class TemperatureMembership
    {
        // Cold: fully cold below 50°F, fades out by 65°F
        public static double Cold(double t) => MembershipFunction.Trapezoid(t, 0, 0, 50, 65);
 
        // Warm: comfortable band 65–80°F
        public static double Warm(double t) => MembershipFunction.Trapezoid(t, 55, 65, 80, 90);
 
        // Hot: danger starts at 80°F, fully hot above 95°F
        public static double Hot(double t) => MembershipFunction.Trapezoid(t, 80, 95, 130, 130);
    }

    /// <summary>
    /// Humidity membership functions (%).
    /// </summary>
    public static class HumidityMembership
    {
        // Low: dry conditions below 20%, fades by 35%
        public static double Low(double h) => MembershipFunction.Trapezoid(h, 0, 0, 20, 35);
 
        // Moderate: comfortable 35–60%
        public static double Moderate(double h)  => MembershipFunction.Trapezoid(h, 25, 35, 60, 70);
 
        // High: humid stress above 60%, fully high above 75%
        public static double High(double h) => MembershipFunction.Trapezoid(h, 60, 75, 100, 100);
    }

    // ── Shared Trapezoid Helper ──────────────────────────────
    // Ramps up from a→b, flat b→c, ramps down c→d
    // Returns 0.0 outside [a,d], 1.0 inside [b,c]
    internal static class MembershipFunction
    {
        public static double Trapezoid(double x, double a, double b, double c, double d)
        {
            if (x <= a || x >= d) return 0.0;
            if (x >= b && x <= c) return 1.0;
            if (x <  b) return (x - a) / (b - a);
                        return (d - x) / (d - c);
        }
    }

    // ── Output Centroids ────────────────────────────────────
    // Stress levels mapped to crisp centroid values (0.0–1.0)
    // Used in weighted-average defuzzification
    internal static class StressCentroids
    {
        public const double Minimal  = 0.05;
        public const double Low      = 0.25;
        public const double Moderate = 0.55;
        public const double High     = 0.80;
        public const double Critical = 0.97;
    }

    // ── Fired Rule Record ────────────────────────────────────
    /// <summary>
    /// Describes a rule that fired during evaluation.
    /// Used by the renderer to show fuzzy reasoning to the player.
    /// </summary>
    public record FiredRule(string Description, double Strength);
    
    // ── Fuzzy Inference Result ───────────────────────────────
    /// <summary>
    /// Full output of one fuzzy evaluation pass.
    /// </summary>
    public record FuzzyResult(
        double SystemStress,           // 0.0–1.0 defuzzified output
        double TempCold,               // membership degrees (for display)
        double TempWarm,
        double TempHot,
        double HumLow,
        double HumModerate,
        double HumHigh,
        IReadOnlyList<FiredRule> FiredRules
    );

    // ── Fuzzy Engine ─────────────────────────────────────────
    /// <summary>
    /// Mamdani fuzzy inference engine.
    /// Five rules covering the full input space.
    ///
    /// R1: Hot  AND High     → Critical stress
    /// R2: Hot  AND Moderate → High stress
    /// R3: Warm AND High     → Moderate stress
    /// R4: Warm AND Moderate → Low stress
    /// R5: Cold OR  Low      → Minimal stress
    /// </summary>
 
    public static class FuzzyEngine
    {
        public static FuzzyResult Evaluate(double effectiveTempF, double effectiveHumidityPct)
        {
            // ── Fuzzify ──────────────────────────────────────
            double tCold = TemperatureMembership.Cold(effectiveTempF);
            double tWarm = TemperatureMembership.Warm(effectiveTempF);
            double tHot  = TemperatureMembership.Hot(effectiveTempF);
 
            double hLow  = HumidityMembership.Low(effectiveHumidityPct);
            double hMod  = HumidityMembership.Moderate(effectiveHumidityPct);
            double hHigh = HumidityMembership.High(effectiveHumidityPct);

            // ── Fire Rules ───────────────────────────────────
            // AND = min, OR = max  (Mamdani standard)
            double r1 = Math.Min(tHot,  hHigh);   // → Critical
            double r2 = Math.Min(tHot,  hMod);    // → High
            double r3 = Math.Min(tWarm, hHigh);   // → Moderate
            double r4 = Math.Min(tWarm, hMod);    // → Low
            double r5 = Math.Max(tCold, hLow);    // → Minimal

            // ── Build fired rules list ────────────────────────
            var rules = new List<FiredRule>();
            if (r1 > 0.01) rules.Add(new FiredRule("Hot AND High Humidity  → Critical", r1));
            if (r2 > 0.01) rules.Add(new FiredRule("Hot AND Moderate Hum  → High",      r2));
            if (r3 > 0.01) rules.Add(new FiredRule("Warm AND High Humidity → Moderate", r3));
            if (r4 > 0.01) rules.Add(new FiredRule("Warm AND Moderate Hum → Low",       r4));
            if (r5 > 0.01) rules.Add(new FiredRule("Cold OR Low Humidity  → Minimal",   r5));

            // ── Defuzzify: centroid (weighted average) ────────
            double weighted_average =
                r1 * StressCentroids.Critical +
                r2 * StressCentroids.High     +
                r3 * StressCentroids.Moderate +
                r4 * StressCentroids.Low      +
                r5 * StressCentroids.Minimal;
            
            double denominator = r1 + r2 + r3 + r4 + r5;
            double stress = denominator < 0.0001 ? 0.0 : weighted_average / denominator;

            return new FuzzyResult(
                SystemStress : stress,
                TempCold     : tCold,
                TempWarm     : tWarm,
                TempHot      : tHot,
                HumLow       : hLow,
                HumModerate  : hMod,
                HumHigh      : hHigh,
                FiredRules   : rules.AsReadOnly()
            );      
        }
    }
}
