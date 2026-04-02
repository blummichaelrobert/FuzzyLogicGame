// ============================================================
//  MainView.cs
//  Draws the primary gameplay screen.
//
//  Layout (rows 0–29):
//    0      — top border
//    1      — title bar  (level name | tick | stasis tokens)
//    2      — sub-border
//    3–11   — dome graphic (left) + environment panel (right)
//    12     — mid-border
//    13–15  — three meters (Crew, Eco, Dome)
//    16     — mid-border
//    17–18  — event log (event + last player action)
//    19     — bottom border
//    20     — hotkey bar
// ============================================================

using System;
using BiodomeAres1.Engine;
using static BiodomeAres1.Rendering.ConsoleRenderer;

namespace BiodomeAres1.Rendering
{
    public static class MainView
    {
         private const int SplitCol    = 36;    // column where dome | env splits
        private const int StartRow    = 0;
 
        public static void Draw(GameSnapshot snap)
        {
            DrawHeader(snap);
            DrawDomeAndEnvironment(snap);
            DrawMeters(snap);
            DrawEventLog(snap);
            DrawHotkeyBar(snap);
        }

        // ── Row 0–2: Header ──────────────────────────────────
        private static void DrawHeader(GameSnapshot snap)
        {
            HRule(0, 'T');
 
            string stasis = snap.StasisActive
                ? $"⏸ STASIS"
                : $"🧊 x{snap.StasisTokens}";
 
            string tickTimer = snap.StasisActive
                ? "[ STASIS ACTIVE ]"
                : $"⏱ {snap.TickSecondsRemaining:F0}s";
 
            string title =
                $"  BIODOME ARES-1  │  " +
                $"LVL {snap.LevelNumber}: {snap.LevelName.ToUpper(),-8}  │  " +
                $"Tick {snap.CurrentTick,2}/{snap.TotalTicks}  │  " +
                $"{tickTimer,-18}  {stasis}";
 
            FullRow(1, title, snap.StasisActive
                ? ConsoleColor.Cyan
                : ConsoleColor.White);
 
            HRule(2, 'M');
        }

        // ── Rows 3–11: Dome graphic + Environment ────────────
        private static void DrawDomeAndEnvironment(GameSnapshot snap)
        {
            var domeLines = DomeGraphic.Generate(snap);
 
            // Environment values — apply sensor glitch flicker
            double dispTemp = snap.SensorGlitch && Flicker()
                ? snap.RawTemperatureF + Random(-8, 8)
                : snap.EffectiveTemperatureF;
 
            double dispHum = snap.SensorGlitch && Flicker()
                ? snap.RawHumidityPct + Random(-10, 10)
                : snap.EffectiveHumidityPct;
 
            double stress = snap.FuzzyResult?.SystemStress ?? 0.0;
 
            string tempBar  = Bar(Math.Clamp(dispTemp, 0, 130), 130, 12);
            string humBar   = Bar(Math.Clamp(dispHum,  0, 100), 100, 12);
            string stressBar= Bar(stress, 1.0, 12);
 
            ConsoleColor tempCol   = dispTemp > 95  ? ConsoleColor.Red
                                   : dispTemp > 80  ? ConsoleColor.Yellow
                                   : ConsoleColor.Green;
            ConsoleColor humCol    = dispHum  > 75  ? ConsoleColor.Red
                                   : dispHum  > 55  ? ConsoleColor.Yellow
                                   : ConsoleColor.Green;
            ConsoleColor stressCol = stress   > 0.7 ? ConsoleColor.Red
                                   : stress   > 0.4 ? ConsoleColor.Yellow
                                   : ConsoleColor.Green;
 
            string glitch = snap.SensorGlitch ? " ?" : "  ";
 
            // 9 dome lines paired with environment rows
            string[] envLines =
            {
                $"ENVIRONMENT{glitch}",
                $"Temp:   {dispTemp,5:F1}°F {tempBar}",
                $"Humid:  {dispHum,5:F1}%  {humBar}",
                $"Stress: {stress,5:F2}    {stressBar}",
                "",
                $"CONTROLS  [{snap.TotalPowerUsed}/{snap.PowerBudget}⚡]",
                $"Fans:   {snap.Fans,-9} {snap.FanPowerCost}⚡",
                $"Shield: {snap.Shield,-9} {snap.ShieldPowerCost}⚡",
                $"Coolant:{snap.Coolant,-9} {snap.CoolantPowerCost}⚡",
            };
 
            ConsoleColor[] envColours =
            {
                ConsoleColor.Yellow,
                tempCol,
                humCol,
                stressCol,
                ConsoleColor.White,
                ConsoleColor.Yellow,
                ConsoleColor.White,
                ConsoleColor.White,
                ConsoleColor.White,
            };
 
            for (int i = 0; i < DomeGraphic.DomeHeight; i++)
            {
                int row = 3 + i;
                string dome = i < domeLines.Length
                    ? domeLines[i]
                    : new string(' ', DomeGraphic.DomeWidth);
 
                // Dome graphic — cyan tint, cracking goes red
                ConsoleColor domeCol = snap.DomeIntegrity < 30
                    ? ConsoleColor.Red
                    : snap.DomeIntegrity < 50
                        ? ConsoleColor.Yellow
                        : ConsoleColor.DarkCyan;
 
                Write(row, 1,        dome,          domeCol,
                    padToWidth: SplitCol - 1);
                Write(row, SplitCol, " │ ",         ConsoleColor.DarkCyan);
 
                string env = i < envLines.Length ? envLines[i] : "";
                Write(row, SplitCol + 3, env,       envColours[i],
                    padToWidth: TotalWidth - SplitCol - 4);
 
                Write(row, TotalWidth - 1, "│",     ConsoleColor.DarkCyan);
            }
 
            HRule(12, 'M');
        }

        // ── Rows 13–15: Three meters ─────────────────────────
        private static void DrawMeters(GameSnapshot snap)
        {
            DrawMeter(13,
                "👥 Crew",
                snap.CrewHealth,
                snap.CrewTrend);
 
            DrawMeter(14,
                "🌿 Eco ",
                snap.EcosystemIntegrity,
                snap.EcoTrend);
 
            DrawMeter(15,
                "🏗 Dome",
                snap.DomeIntegrity,
                snap.DomeTrend);
 
            HRule(16, 'M');
        }

        private static void DrawMeter(int row, string label, double value, int trend)
        {
            string bar     = Bar(value, 100, 22);
            string pct     = $"{value,5:F1}%";
            string arrow   = TrendArrow(trend);
            ConsoleColor c = MeterColour(value);
            ConsoleColor ac= TrendColour(trend);
 
            string content = $"{label}  {bar}  {pct}  ";
 
            Write(row, 0,   "│ ",    ConsoleColor.DarkCyan);
            Write(row, 2,   label,   ConsoleColor.White);
            Write(row, 10,  "  ",    ConsoleColor.White);
            Write(row, 12,  bar,     c);
            Write(row, 37,  "  ",    ConsoleColor.White);
            Write(row, 39,  pct,     c);
            Write(row, 45,  "  ",    ConsoleColor.White);
            Write(row, 47,  arrow,   ac);
            Write(row, 48,
                new string(' ', TotalWidth - 50),
                ConsoleColor.White);
            Write(row, TotalWidth - 2, " │", ConsoleColor.DarkCyan);
        }

        // ── Rows 17–18: Event log ────────────────────────────
        private static void DrawEventLog(GameSnapshot snap)
        {
            // Row 17 — active weather event
            string eventLine = snap.EventName != null
                ? $"⚡ {snap.EventName} — {snap.EventFlavor}"
                : "  Systems nominal.";
 
            ConsoleColor eventCol = snap.EventName != null
                ? ConsoleColor.Yellow
                : ConsoleColor.DarkGray;
 
            FullRow(17, eventLine, eventCol);
 
            // Row 18 — last player action (with ←★ marker)
            string actionLine = "";
            ConsoleColor actionCol = ConsoleColor.White;
 
            if (snap.LastActionText != null)
            {
                actionLine = snap.LastActionJustChanged
                    ? $"←★ {snap.LastActionText}"
                    : $"   {snap.LastActionText}";
 
                actionCol = snap.LastActionText.StartsWith("⚠")
                    ? ConsoleColor.Red
                    : snap.LastActionJustChanged
                        ? ConsoleColor.Cyan
                        : ConsoleColor.DarkGray;
            }
 
            FullRow(18, actionLine, actionCol);
 
            HRule(19, 'B');
        }

        // ── Row 20: Hotkey bar ───────────────────────────────
        private static void DrawHotkeyBar(GameSnapshot snap)
        {
            string stasisLabel = snap.StasisTokens > 0
                ? $"[S]tasis({snap.StasisTokens})"
                : "[S]tasis(0)";
 
            string bar =
                $"  [C]ontrols  {stasisLabel}  [Q]uit";
 
            Write(20, 0, bar.PadRight(TotalWidth),
                ConsoleColor.DarkGray);
        }

        // ── Helpers ──────────────────────────────────────────
        private static readonly Random _rng = new Random();
 
        private static bool Flicker() => _rng.NextDouble() > 0.5;
 
        private static double Random(double min, double max)
            => min + _rng.NextDouble() * (max - min);
    }
}