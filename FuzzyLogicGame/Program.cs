

#region ORIGINAL IDEA
// ============================================================
//  FuzzyFanSystem.cs  —  Fuzzy Logic Fan Speed Controller
//  Survival Game Foundation
// ============================================================
//
//  FUZZY SETS
//  ──────────
//  Temperature (°F):  Cold | Warm | Hot
//  Humidity (%):      Low  | Moderate | High
//  Fan Speed (%):     Slow | Medium | Fast | VeryFast
//
//  RULES
//  ─────
//  R1: IF Temp is Hot      AND Humidity is High     → VeryFast
//  R2: IF Temp is Hot      AND Humidity is Moderate → Fast
//  R3: IF Temp is Warm     AND Humidity is High     → Medium
//  R4: IF Temp is Cold     OR  Humidity is Low      → Slow
//
// ============================================================

// using System;
// using System.Collections.Generic;
 
// namespace FuzzyFanSystem
// {
//     // ── 1. MEMBERSHIP FUNCTIONS ─────────────────────────────────
//     // Each method returns a degree of membership [0.0 – 1.0]
//     // using trapezoidal / triangular shapes for smooth transitions.
 
//     static class Temperature
//     {
//         // Cold: fully cold below 50°F, fades out by 65°F
//         public static double Cold(double t)   => MembershipFunction.Trapezoid(t, 0, 0, 50, 65);
 
//         // Warm: peaks between 65–80°F
//         public static double Warm(double t)   => MembershipFunction.Trapezoid(t, 55, 65, 80, 90);
 
//         // Hot: starts rising at 80°F, fully hot above 95°F
//         public static double Hot(double t)    => MembershipFunction.Trapezoid(t, 80, 95, 130, 130);
//     }

//     static class Humidity
//     {
//         // Low: fully low below 20%, fades out by 35%
//         public static double Low(double h)      => MembershipFunction.Trapezoid(h, 0, 0, 20, 35);
 
//         // Moderate: peaks between 35–60%
//         public static double Moderate(double h) => MembershipFunction.Trapezoid(h, 25, 35, 60, 70);
 
//         // High: starts rising at 60%, fully high above 75%
//         public static double High(double h)     => MembershipFunction.Trapezoid(h, 60, 75, 100, 100);
//     }

//     // Output fuzzy sets — represented as crisp centroid values
//     // used during defuzzification (weighted average).
//     static class FanSpeed
//     {
//         public const double Slow     = 10.0;   // ~10% fan speed
//         public const double Medium   = 40.0;   // ~40% fan speed
//         public const double Fast     = 70.0;   // ~70% fan speed
//         public const double VeryFast = 95.0;   // ~95% fan speed
//     }

//     // ── 2. SHARED TRAPEZOID HELPER ───────────────────────────────
//     // Shape:  0 before a, ramps up a→b, flat b→c, ramps down c→d, 0 after d
//     static class MembershipFunction
//     {
//         public static double Trapezoid(double x, double a, double b, double c, double d)
//         {
//             if (x <= a || x >= d) return 0.0;
//             if (x >= b && x <= c) return 1.0;
//             if (x < b)  return (x - a) / (b - a);
//                         return (d - x) / (d - c);
//         }
//     }

//     // Alias so Temperature/Humidity can call the helper cleanly
//     // static partial class Temperature
//     // {
//     //     static double Trapezoid(double x, double a, double b, double c, double d)
//     //         => MembershipFunction.Trapezoid(x, a, b, c, d);
//     // }
//     // static partial class Humidity
//     // {
//     //     static double Trapezoid(double x, double a, double b, double c, double d)
//     //         => MembershipFunction.Trapezoid(x, a, b, c, d);
//     // }

//     // ── 3. FUZZY INFERENCE ENGINE ────────────────────────────────
//     static class FuzzyEngine
//     {
//         /// <summary>
//         /// Evaluates all 4 rules and returns a defuzzified fan speed (0–100%).
//         /// Uses Mamdani min-inference + centroid defuzzification.
//         /// </summary>
//         public static double Evaluate(double tempF, double humidityPct,
//                                       out List<(string rule, double strength)> firedRules)
//         {
//             firedRules = new List<(string, double)>();
 
//             // ── Fuzzify inputs ───────────────────────────────────
//             double muTempCold = Temperature.Cold(tempF);
//             double muTempWarm = Temperature.Warm(tempF);
//             double muTempHot  = Temperature.Hot(tempF);
 
//             double muHumLow  = Humidity.Low(humidityPct);
//             double muHumMod  = Humidity.Moderate(humidityPct);
//             double muHumHigh = Humidity.High(humidityPct);
 
//             // ── Fire rules (AND = min, OR = max) ─────────────────
//             double r1 = Math.Min(muTempHot,  muHumHigh);   // Hot AND High     → VeryFast
//             double r2 = Math.Min(muTempHot,  muHumMod);    // Hot AND Moderate → Fast
//             double r3 = Math.Min(muTempWarm, muHumHigh);   // Warm AND High    → Medium
//             double r4 = Math.Max(muTempCold, muHumLow);    // Cold OR Low      → Slow
 
//             if (r1 > 0) firedRules.Add(("Hot AND High → VeryFast", r1));
//             if (r2 > 0) firedRules.Add(("Hot AND Moderate → Fast", r2));
//             if (r3 > 0) firedRules.Add(("Warm AND High → Medium",  r3));
//             if (r4 > 0) firedRules.Add(("Cold OR Low → Slow",      r4));
 
//             // ── Defuzzify: weighted average (centroid method) ─────
//             double numerator =
//                 r1 * FanSpeed.VeryFast +
//                 r2 * FanSpeed.Fast     +
//                 r3 * FanSpeed.Medium   +
//                 r4 * FanSpeed.Slow;
 
//             double denominator = r1 + r2 + r3 + r4;
 
//             return denominator == 0 ? 0 : numerator / denominator;
//         }
//     }

//     // ── 4. DISPLAY HELPERS ───────────────────────────────────────
//     static class Display
//     {
//         public static void PrintHeader()
//         {
//             Console.ForegroundColor = ConsoleColor.Cyan;
//             Console.WriteLine("╔══════════════════════════════════════════════╗");
//             Console.WriteLine("║     🌡  FUZZY FAN SURVIVAL SYSTEM  💨        ║");
//             Console.WriteLine("╚══════════════════════════════════════════════╝");
//             Console.ResetColor();
//         }

//         public static void PrintMemberships(double tempF, double humidityPct)
//         {
//             Console.ForegroundColor = ConsoleColor.Yellow;
//             Console.WriteLine("\n── Membership Degrees ──────────────────────────");
//             Console.ResetColor();

//             Console.WriteLine($"  Temperature ({tempF}°F):");
//             PrintBar("    Cold  ", Temperature.Cold(tempF));
//             PrintBar("    Warm  ", Temperature.Warm(tempF));
//             PrintBar("    Hot   ", Temperature.Hot(tempF));

//             Console.WriteLine($"  Humidity ({humidityPct}%):");
//             PrintBar("    Low   ", Humidity.Low(humidityPct));
//             PrintBar("    Mod   ", Humidity.Moderate(humidityPct));
//             PrintBar("    High  ", Humidity.High(humidityPct));
//         }

//         public static void PrintRules(List<(string rule, double strength)> rules)
//         {
//             Console.ForegroundColor = ConsoleColor.Yellow;
//             Console.WriteLine("\n── Fired Rules ─────────────────────────────────");
//             Console.ResetColor();

//             if (rules.Count == 0)
//             {
//                 Console.WriteLine("  (no rules fired)");
//                 return;
//             }
//             foreach (var (rule, strength) in rules)
//             {
//                 Console.Write($"  [{strength:F2}] ");
//                 Console.ForegroundColor = ConsoleColor.White;
//                 Console.WriteLine(rule);
//                 Console.ResetColor();
//             }
//         }
        
//         public static void PrintFanSpeed(double speed)
//         {
//             Console.ForegroundColor = ConsoleColor.Yellow;
//             Console.WriteLine("\n── Fan Speed Output ────────────────────────────");
//             Console.ResetColor();

//             string label = speed switch
//             {
//                 >= 85 => "🔴 VERY FAST — Danger Zone!",
//                 >= 60 => "🟠 FAST — Stay alert",
//                 >= 30 => "🟡 MEDIUM — Comfortable",
//                 _     => "🟢 SLOW — All good",
//             };

//             PrintBar("  Speed", speed / 100.0);
//             Console.ForegroundColor = ConsoleColor.White;
//             Console.WriteLine($"\n  {label}  ({speed:F1}%)");
//             Console.ResetColor();
//         }

//         public static void PrintSurvivalStatus(double speed)
//         {
//             Console.ForegroundColor = ConsoleColor.Magenta;
//             Console.WriteLine("\n── Survival Status ─────────────────────────────");
//             Console.ResetColor();

//             string status = speed switch
//             {
//                 >= 85 => "⚠  CRITICAL: Heat stress imminent! Find shelter!",
//                 >= 60 => "!  WARNING:  Conditions are tough. Stay hydrated.",
//                 >= 30 => "~  MODERATE: Manageable but keep an eye on it.",
//                 _     => "✓  SAFE:     Conditions are fine. Carry on.",
//             };
//             Console.WriteLine($"  {status}");
//         }

//         static void PrintBar(string label, double value)
//         {
//             int filled = (int)(value * 20);
//             string bar = "[" + new string('█', filled) + new string('░', 20 - filled) + "]";
//             Console.WriteLine($"{label,-10} {bar} {value:F2}");
//         }
//     }

//     // ── 5. MAIN PROGRAM ──────────────────────────────────────────
//     class Program
//     {
//         static void Main(string[] args)
//         {
//             Display.PrintHeader();

//             while (true)
//             {
//                 Console.WriteLine();
//                 double temp     = ReadDouble("Enter temperature (°F, 0–130): ", 0, 130);
//                 double humidity = ReadDouble("Enter humidity   (%,  0–100): ", 0, 100);

//                 double fanSpeed = FuzzyEngine.Evaluate(temp, humidity,
//                                     out var firedRules);

//                 Display.PrintMemberships(temp, humidity);
//                 Display.PrintRules(firedRules);
//                 Display.PrintFanSpeed(fanSpeed);
//                 Display.PrintSurvivalStatus(fanSpeed);

//                 Console.ForegroundColor = ConsoleColor.DarkGray;
//                 Console.WriteLine("\n────────────────────────────────────────────────");
//                 Console.Write("  Try another reading? (y/n): ");
//                 Console.ResetColor();

//                 string again = Console.ReadLine()?.Trim().ToLower();
//                 if (again != "y") break;
//             }

//             Console.ForegroundColor = ConsoleColor.Cyan;
//             Console.WriteLine("\n  Stay cool out there. 👋");
//             Console.ResetColor();
//         }

//         static double ReadDouble(string prompt, double min, double max)
//         {
//             while (true)
//             {
//                 Console.Write(prompt);
//                 if (double.TryParse(Console.ReadLine(), out double val)
//                     && val >= min && val <= max)
//                     return val;
 
//                 Console.ForegroundColor = ConsoleColor.Red;
//                 Console.WriteLine($"  Please enter a number between {min} and {max}.");
//                 Console.ResetColor();
//             }
//         }
//     }
// }
#endregion

// ============================================================
//  Program.cs
//  Entry point. Wires config, state, renderer, and game loop.
//
//  To swap in a GUI renderer later:
//    Replace ConsoleRenderer with your GuiRenderer here.
//    Nothing else changes.
// ============================================================
 
using BiodomeAres1.Config;
using BiodomeAres1.Engine;
using BiodomeAres1.Rendering;

// ── Wire dependencies ────────────────────────────────────────
var config   = new GameConfig();    // all tunable constants
var state    = new GameState();     // live mutable game data
var renderer = new ConsoleRenderer();  // swap for ConsoleRenderer in Layer 3
 
var loop = new GameLoop(config, state, renderer);
 
// ── Start ────────────────────────────────────────────────────
loop.Run();
