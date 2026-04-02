// ============================================================
//  MenuViews.cs
//  All non-gameplay screens:
//    MainMenuView   — title screen
//    SettingsView   — tick speed configuration
//    LevelIntroView — level flavour text before play begins
//    OutcomeView    — level complete and game over screens
// ============================================================
 
using System;
using BiodomeAres1.Engine;
using static BiodomeAres1.Rendering.ConsoleRenderer;

namespace BiodomeAres1.Rendering
{
    // ════════════════════════════════════════════════════════
    //  MAIN MENU
    // ════════════════════════════════════════════════════════
    public static class MainMenuView
    {
        public static void Draw(GameSnapshot snap)
        {
            ClearRegion(0, 24);
            HRule(0, 'T');
 
            FullRow(1,  "");
            FullRow(2,  "  ██████╗ ██╗ ██████╗ ██████╗  ██████╗ ███╗   ███╗███████╗",
                ConsoleColor.Cyan);
            FullRow(3,  "  ██╔══██╗██║██╔═══██╗██╔══██╗██╔═══██╗████╗ ████║██╔════╝",
                ConsoleColor.Cyan);
            FullRow(4,  "  ██████╔╝██║██║   ██║██║  ██║██║   ██║██╔████╔██║█████╗  ",
                ConsoleColor.Cyan);
            FullRow(5,  "  ██╔══██╗██║██║   ██║██║  ██║██║   ██║██║╚██╔╝██║██╔══╝  ",
                ConsoleColor.DarkCyan);
            FullRow(6,  "  ██████╔╝██║╚██████╔╝██████╔╝╚██████╔╝██║ ╚═╝ ██║███████╗",
                ConsoleColor.DarkCyan);
            FullRow(7,  "  ╚═════╝ ╚═╝ ╚═════╝ ╚═════╝  ╚═════╝ ╚═╝     ╚═╝╚══════╝",
                ConsoleColor.DarkCyan);
            FullRow(8,  "");
            FullRow(9,  "               A R E S - 1   S T A T I O N",
                ConsoleColor.Yellow);
            FullRow(10, "                  M A R S   S U R V I V A L",
                ConsoleColor.DarkYellow);
            FullRow(11, "");
            FullRow(12, "  ─────────────────────────────────────────────────────",
                ConsoleColor.DarkGray);
            FullRow(13, "");
            FullRow(14, "  You are the Life Systems Officer of Biodome Ares-1.",
                ConsoleColor.White);
            FullRow(15, "  Keep the crew alive. Protect the ecosystem.",
                ConsoleColor.White);
            FullRow(16, "  Do not let the dome fail.",
                ConsoleColor.White);
            FullRow(17, "");
            FullRow(18, "  Mars does not care about you.",
                ConsoleColor.DarkGray);
            FullRow(19, "");
            FullRow(20, "  ─────────────────────────────────────────────────────",
                ConsoleColor.DarkGray);
            FullRow(21, "");
            FullRow(22, "  [N] New Game       [S] Settings       [Q] Quit",
                ConsoleColor.Yellow);
            FullRow(23, "");
 
            HRule(24, 'B');
        }
    }

    // ════════════════════════════════════════════════════════
    //  SETTINGS MENU
    // ════════════════════════════════════════════════════════
    public static class SettingsView
    {
        public static void Draw(GameSnapshot snap)
        {
            ClearRegion(0, 20);
            HRule(0, 'T');
 
            FullRow(1, "  ⚙  SETTINGS",  ConsoleColor.Yellow);
            HRule(2, 'M');
            FullRow(3, "");
            FullRow(4, "  TICK SPEED  —  How long each game tick lasts",
                ConsoleColor.White);
            FullRow(5, "");
            FullRow(6, $"  Current: {snap.TickSpeedSeconds} seconds per tick",
                ConsoleColor.Cyan);
            FullRow(7, "");
 
            // Visual tick speed slider
            string slider = TickSlider(snap.TickSpeedSeconds);
            FullRow(8, $"  {slider}", ConsoleColor.White);
            FullRow(9, "  1s                                              60s",
                ConsoleColor.DarkGray);
            FullRow(10, "");
            FullRow(11, "  PRESETS:", ConsoleColor.Yellow);
            FullRow(12,
                $"  [1] Tester   —  3s   (brutal, for development)",
                snap.TickSpeedSeconds == 3
                    ? ConsoleColor.Cyan : ConsoleColor.White);
            FullRow(13,
                $"  [2] Standard —  8s   (intended experience)",
                snap.TickSpeedSeconds == 8
                    ? ConsoleColor.Cyan : ConsoleColor.White);
            FullRow(14,
                $"  [3] Relaxed  — 15s   (more thinking time)",
                snap.TickSpeedSeconds == 15
                    ? ConsoleColor.Cyan : ConsoleColor.White);
            FullRow(15, "");
            FullRow(16, "  [+] / [-] to set a custom value (1–60 seconds)",
                ConsoleColor.DarkGray);
            FullRow(17, "");
            FullRow(18, "  [Enter] or [Esc] to return to main menu",
                ConsoleColor.DarkGray);
            FullRow(19, "");
 
            HRule(20, 'B');
        }

        private static string TickSlider(int seconds)
        {
            // Map 1–60 seconds onto a 50-character bar
            int pos = (int)Math.Round((seconds - 1) / 59.0 * 48);
            pos = Math.Clamp(pos, 0, 48);
            char[] bar = new char[50];
            for (int i = 0; i < 50; i++) bar[i] = '─';
            bar[0]  = '[';
            bar[49] = ']';
            bar[1 + pos] = '●';
            return new string(bar);
        }
    }

    // ════════════════════════════════════════════════════════
    //  LEVEL INTRO
    // ════════════════════════════════════════════════════════
    public static class LevelIntroView
    {
        public static void Draw(GameSnapshot snap)
        {
            ClearRegion(0, 20);
            HRule(0, 'T');
 
            FullRow(1, "");
            FullRow(2, $"  LEVEL {snap.LevelNumber}  —  {snap.LevelName.ToUpper()}",
                ConsoleColor.Yellow);
            FullRow(3, $"  {snap.LevelSubtitle}",
                ConsoleColor.DarkYellow);
            FullRow(4, "");
 
            HRule(5, 'M');
            FullRow(6, "");
 
            // Word-wrap the intro text at ~70 chars
            var lines = WordWrap(snap.LevelName == "Crisis"
                ? "ALERT: " + snap.LevelSubtitle
                : snap.LevelSubtitle, 70);
 
            // Display the full intro flavor from snapshot
            // (We use LevelSubtitle here; GameLoop sets the flavor text
            //  on the state which flows through as LevelSubtitle)
            FullRow(7,  $"  \"{GetIntroText(snap.LevelNumber, 0)}\"",
                ConsoleColor.White);
            FullRow(8,  $"  {GetIntroText(snap.LevelNumber, 1)}",
                ConsoleColor.DarkGray);
            FullRow(9,  "");
 
            HRule(10, 'M');
            FullRow(11, "");
            FullRow(12, $"  Duration:      {snap.TotalTicks} ticks",
                ConsoleColor.White);
            FullRow(13, $"  Power budget:  {snap.PowerBudget} ⚡",
                ConsoleColor.White);
            FullRow(14, $"  Stasis tokens: {snap.StasisTokens} 🧊",
                ConsoleColor.White);
            FullRow(15, "");
 
            FullRow(16, LevelObjective(snap.LevelNumber),
                ConsoleColor.Yellow);
            FullRow(17, "");
 
            HRule(18, 'M');
            FullRow(19, "  Press any key to begin — or wait 3 seconds.",
                ConsoleColor.DarkGray);
            HRule(20, 'B');
        }

        private static string GetIntroText(int level, int line)
        {
            return (level, line) switch
            {
                (1, 0) => "Ares-1, all systems nominal. Light solar activity detected.",
                (1, 1) => "Begin environmental monitoring. This is a calibration exercise.",
                (2, 0) => "Warning: elevated particulate activity detected on scanners.",
                (2, 1) => "Brace for reduced solar input and pressure variance.",
                (3, 0) => "ALERT: Catastrophic weather event. Multiple failures imminent.",
                (3, 1) => "This is not a drill. Decide what you are willing to lose.",
                _      => "",
            };
        }
 
        private static string LevelObjective(int level) => level switch
        {
            1 => "  Objective: Keep all meters above 80% for 10 ticks.",
            2 => "  Objective: Keep all meters above 50% for 15 ticks.",
            3 => "  Objective: Survive 20 ticks. Any meter above 0%.",
            _ => "",
        };

         private static string[] WordWrap(string text, int width)
        {
            // Simple word-wrap — not used yet but available for richer intros
            var words  = text.Split(' ');
            var lines  = new System.Collections.Generic.List<string>();
            var current= "";
            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > width)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = current.Length == 0 ? word : current + " " + word;
                }
            }
            if (current.Length > 0) lines.Add(current);
            return lines.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════
    //  OUTCOME SCREENS  (Level Complete + Game Over)
    // ════════════════════════════════════════════════════════
    public static class OutcomeView
    {
        public static void DrawLevelComplete(GameSnapshot snap)
        {
            ClearRegion(0, 20);
            HRule(0, 'T');
 
            bool finalVictory = snap.LevelNumber == 3;
 
            FullRow(1, "");
            FullRow(2,
                finalVictory
                    ? "  ★  MISSION COMPLETE  ★"
                    : $"  ✓  LEVEL {snap.LevelNumber} COMPLETE",
                ConsoleColor.Green);
            FullRow(3, "");
 
            HRule(4, 'M');
            FullRow(5, "");
 
            FullRow(6, finalVictory
                ? "  Ares-1 has survived. The dome stands. Well done, Officer."
                : "  Systems stable. Proceeding to next phase.",
                ConsoleColor.White);
            FullRow(7, "");
 
            FullRow(8,  $"  👥 Crew Health:        {snap.CrewHealth:F1}%",
                MeterColour(snap.CrewHealth));
            FullRow(9,  $"  🌿 Ecosystem:          {snap.EcosystemIntegrity:F1}%",
                MeterColour(snap.EcosystemIntegrity));
            FullRow(10, $"  🏗 Dome Integrity:     {snap.DomeIntegrity:F1}%",
                MeterColour(snap.DomeIntegrity));
            FullRow(11, "");
 
            if (!finalVictory)
            {
                int award = BiodomeAres1.Config.GameConfig.Levels[snap.LevelNumber - 1]
                    .StasisTokenAward;
                FullRow(12,
                    $"  🧊 Stasis Tokens awarded: +{award}  " +
                    $"(now holding {snap.StasisTokens})",
                    ConsoleColor.Cyan);
                FullRow(13, "");
                FullRow(14, "  Coolant reserves carry over to the next level.",
                    ConsoleColor.DarkGray);
            }
 
            FullRow(15, "");
            HRule(16, 'M');
            FullRow(17,
                finalVictory
                    ? "  Press any key to return to the main menu."
                    : "  Press any key to continue to the next level.",
                ConsoleColor.DarkGray);
            HRule(18, 'B');
        }

        public static void DrawGameOver(GameSnapshot snap)
        {
            ClearRegion(0, 20);
            HRule(0, 'T');
 
            FullRow(1, "");
            FullRow(2, "  ✗  MISSION FAILED",  ConsoleColor.Red);
            FullRow(3, "");
 
            HRule(4, 'M');
            FullRow(5, "");
 
            FullRow(6, $"  {snap.GameOverReason}", ConsoleColor.Red);
            FullRow(7, "");
 
            FullRow(8,  $"  👥 Crew Health:        {snap.CrewHealth:F1}%",
                MeterColour(snap.CrewHealth));
            FullRow(9,  $"  🌿 Ecosystem:          {snap.EcosystemIntegrity:F1}%",
                MeterColour(snap.EcosystemIntegrity));
            FullRow(10, $"  🏗 Dome Integrity:     {snap.DomeIntegrity:F1}%",
                MeterColour(snap.DomeIntegrity));
            FullRow(11, "");
 
            FullRow(12,
                $"  Survived to tick {snap.CurrentTick} of {snap.TotalTicks}" +
                $"  (Level {snap.LevelNumber}: {snap.LevelName})",
                ConsoleColor.DarkGray);
            FullRow(13, "");
 
            // Fuzzy debrief — show which rules were firing at failure
            if (snap.FuzzyResult != null && snap.FuzzyResult.FiredRules.Count > 0)
            {
                FullRow(14, "  System stress analysis:", ConsoleColor.DarkYellow);
                int r = 15;
                foreach (var rule in snap.FuzzyResult.FiredRules)
                {
                    if (r > 17) break;
                    FullRow(r++,
                        $"  [{rule.Strength:F2}] {rule.Description}",
                        ConsoleColor.DarkGray);
                }
            }
 
            FullRow(18, "");
            HRule(19, 'M');
            FullRow(20, "  Press any key to return to the main menu.",
                ConsoleColor.DarkGray);
            HRule(21, 'B');
        }
    }
}