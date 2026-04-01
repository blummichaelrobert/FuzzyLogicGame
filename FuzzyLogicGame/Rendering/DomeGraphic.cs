// ============================================================
//  DomeGraphic.cs
//  Generates the atmospheric ASCII dome art.
//  The dome changes appearance based on:
//    - Active weather event
//    - System stress level
//    - Dome integrity meter
//    - Stasis state
//
//  Returns a string[] of exactly DomeHeight lines,
//  each exactly DomeWidth characters wide.
//  The renderer writes them at a fixed position.
// ============================================================

using System;
using BiodomeAres1.Engine;

namespace BiodomeAres1.Rendering
{
    public static class DomeGraphic
    {
        public const int DomeWidth  = 34;
        public const int DomeHeight = 9;
 
        // ── Random for particle animation ────────────────────
        private static readonly Random _rng = new Random();

        public static string[] Generate(GameSnapshot snap)
        {
            double stress       = snap.FuzzyResult?.SystemStress ?? 0.0;
            double domeHealth   = snap.DomeIntegrity;
            bool   hasEvent     = snap.EventName != null;
            bool   stasis       = snap.StasisActive;
            bool   cracking     = domeHealth < 40.0;
            bool   critical     = domeHealth < 20.0;
 
            // Choose which frame generator to use
            if (stasis)          return DrawStasis();
            if (critical)        return DrawCritical(snap);
            if (cracking)        return DrawCracking(snap, stress);
            if (hasEvent)        return DrawStorm(snap, stress);
            if (stress > 0.5)    return DrawWarm(snap, stress);
                                 return DrawCalm(snap);
        }

        // ── CALM ─────────────────────────────────────────────
        // Clear sky, clean dome, faint stars
        private static string[] DrawCalm(GameSnapshot snap)
        {
            return new[]
            {
                Star("  .  *   .     *  .    *     . "),
                Star("*    .      *    .    *    .    "),
                "        ╔══════════════╗        ",
                "      ╔╝              ╚╗        ",
                "    ╔╝   ARES - 1      ╚╗      ",
                "   ║      NOMINAL        ║      ",
                "    ╚╗                 ╔╝       ",
                "      ╚═══════════════╝         ",
                "  ≈ ≈ ≈ ≈ ≈ M A R S ≈ ≈ ≈ ≈  ",
            };
        }

        // ── WARM / ELEVATED STRESS ───────────────────────────
        // Heat shimmer, no storm particles yet
        private static string[] DrawWarm(GameSnapshot snap, double stress)
        {
            string shimmer = stress > 0.7
                ? "~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~"
                : ".  ~  .  ~  .  ~  .  ~  .  ~ .";
 
            return new[]
            {
                $" {shimmer[..32]} ",
                "  .  ~  HEAT SHIMMER  ~  .  ~   ",
                "        ╔══════════════╗         ",
                "      ╔╝              ╚╗         ",
                "    ╔╝   ARES - 1      ╚╗       ",
                "   ║      ELEVATED       ║       ",
                "    ╚╗                 ╔╝        ",
                "      ╚═══════════════╝          ",
                "  ≈ ≈ ≈ ≈ ≈ M A R S ≈ ≈ ≈ ≈   ",
            };
        }

        // ── STORM ────────────────────────────────────────────
        // Directional particles, turbulent horizon
        private static string[] DrawStorm(GameSnapshot snap, double stress)
        {
            // Particle density increases with stress
            string p1 = Particles(stress, ">> ~*~ >> . ~*~ >> . ~*~  >> .");
            string p2 = Particles(stress, "~*~ >> . ~*~ >> ~*~ . >> ~*~  ");
            string p3 = Particles(stress, ">> . ~*~ >> ~*~ >> . ~*~ >>  .");
 
            bool heavy = stress > 0.75;
            string eventTag = snap.EventName != null
                ? TruncateCentre(snap.EventName, 14)
                : "  STORM  ";
 
            return new[]
            {
                $" {p1[..32]} ",
                $" {p2[..32]} ",
                $"  {p3[..8]}╔══════════════╗{p3[..8]}  ",
                $"  {p3[..6]}╔╝{Pad(14)}╚╗{p3[..6]}  ",
                $"  {p1[..4]}╔╝  ARES - 1    ╚╗{p1[..4]}  ",
                $"  {p2[..3]}║  {eventTag,-14}  ║{p2[..3]}  ",
                $"  {p1[..4]}╚╗               ╔╝{p1[..4]}  ",
                $"  {p3[..6]}╚═══════════════╝{p3[..6]}  ",
                heavy
                    ? "  ≋ ≋ ≋ ≋ DUST STORM ≋ ≋ ≋ ≋  "
                    : "  ≈ ≈ ≈ ≈ M A R S ≈ ≈ ≈ ≈ ≈  ",
            };
        }

        // ── CRACKING ─────────────────────────────────────────
        // Dome wall gaps appear as integrity drops
        private static string[] DrawCracking(GameSnapshot snap, double stress)
        {
            // The worse the integrity, the more gaps in the wall
            bool leftCrack  = snap.DomeIntegrity < 35;
            bool rightCrack = snap.DomeIntegrity < 30;
            bool topCrack   = snap.DomeIntegrity < 25;
 
            string top    = topCrack   ? "╔══!══════════!══╗" : "╔══════════════╗";
            string leftW  = leftCrack  ? " " : "║";
            string rightW = rightCrack ? " " : "║";
 
            return new[]
            {
                " %%>>%%  %%>>%% %%>>%% %%>>%%  ",
                " %>>  %%>>  %% >>%%  >>  %%>>% ",
                $"  %%>> {top} >>%%  ",
                $"  %>>╔╝{Pad(14)}╚╗>>%  ",
                $"  %% ╔╝  ARES - 1    ╚╗ %%  ",
                $"  >>  {leftW} !! BREACH RISK !! {rightW}  >>  ",
                $"  %% ╚╗               ╔╝ %%  ",
                "  %>% ╚═══════════════╝ %>%  ",
                "  ≋ ≋ ≋ INTEGRITY LOW ≋ ≋ ≋  ",
            };
        }

        // ── CRITICAL ─────────────────────────────────────────
        // Dome is nearly gone — dramatic fractures everywhere
        private static string[] DrawCritical(GameSnapshot snap)
        {
            return new[]
            {
                " %%%>>%%>>  %%%>>%% %%%>>%%>>  ",
                " %>>   _!_  %>>  >>   _!_  >>% ",
                " %%  ╔══!══════════!══╗  %%  ",
                " %>  ╔╝  !          !  ╚╗  >%  ",
                " %% ╔╝    ARES - 1     ╚╗ %%  ",
                " >> ║  !! CRITICAL !!    ║ >>  ",
                " %% ╚╗   !          !   ╔╝ %%  ",
                " >%  ╚══!═══════════!══╝  %>  ",
                " ≋≋≋≋≋≋ ** BREACH IMMINENT ** ≋≋",
            };
        }

        // ── STASIS ───────────────────────────────────────────
        // Time frozen — everything still, faint border glow
        private static string[] DrawStasis()
        {
            return new[]
            {
                "  ·  ·  ·  ·  ·  ·  ·  ·  ·   ",
                "  ·  ·  ·  ·  ·  ·  ·  ·  ·   ",
                "        ╔══════════════╗        ",
                "      ╔╝              ╚╗        ",
                "    ╔╝  [ S T A S I S ]  ╚╗    ",
                "   ║     T I M E  S T O P  ║   ",
                "    ╚╗                 ╔╝       ",
                "      ╚═══════════════╝         ",
                "  · · · · · P A U S E · · · ·  ",
            };
        }

        // ── Helpers ──────────────────────────────────────────
 
        // Randomly drop some characters from a particle string
        // based on stress — lower stress = sparser particles
        private static string Particles(double stress, string template)
        {
            if (stress > 0.8) return template;
            char[] chars = template.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] != ' ' && _rng.NextDouble() > stress)
                    chars[i] = ' ';
            return new string(chars);
        }

        // Scatter stars randomly in a string
        private static string Star(string template)
        {
            char[] chars = template.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] != ' ' && _rng.NextDouble() > 0.6)
                    chars[i] = ' ';
            return new string(chars);
        }

        private static string Pad(int n) => new string(' ', n);
 
        private static string TruncateCentre(string s, int width)
        {
            if (s.Length <= width) return s.PadLeft((width + s.Length) / 2)
                                           .PadRight(width);
            return s[..width];
        }
    }
}