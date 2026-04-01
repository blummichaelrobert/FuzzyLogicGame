// ============================================================
//  GameConfig.cs
//  All tunable game constants in one place.
//  Tick speed, level definitions, meter thresholds,
//  power budgets — change here, felt everywhere.
// ============================================================

namespace BiodomeAres1.Config
{
    public class GameConfig
    {
        // ── Tick Speed ───────────────────────────────────────
        // How many seconds between each game tick.
        // Change this to tune difficulty during testing.
        // Valid range: 1–60 seconds.
        public int TickSpeedSeconds { get; set; } = 8;

        // Preset named speeds for the settings menu
        public static readonly (string Label, int Seconds)[] TickPresets =
        {
            ("Tester",   3),
            ("Standard", 8),
            ("Relaxed",  15),
            ("Custom",   0),   // 0 = use custom value
        };

        // ── Level Definitions ────────────────────────────────
        public static readonly LevelConfig[] Levels =
        {
            new LevelConfig
            {
                LevelNumber      = 1,
                Name             = "Calm",
                Subtitle         = "System Calibration",
                TotalTicks       = 10,
                PowerBudget      = 10,
                WinMeterFloor    = 80,   // all meters must stay above 80%
                FlavorIntro      = "Ares-1, all systems nominal. " +
                                   "Light solar activity detected. " +
                                   "Begin environmental monitoring.",
                StasisTokenAward = 1,    // tokens awarded on level completion
            },
            new LevelConfig
            {
                LevelNumber      = 2,
                Name             = "Storm",
                Subtitle         = "Dust on the Horizon",
                TotalTicks       = 15,
                PowerBudget      = 8,
                WinMeterFloor    = 50,
                FlavorIntro      = "Warning: meteorological systems detecting " +
                                   "elevated particulate activity. " +
                                   "Brace for reduced solar input.",
                StasisTokenAward = 2,
            },
            new LevelConfig
            {
                LevelNumber      = 3,
                Name             = "Crisis",
                Subtitle         = "The Perfect Storm",
                TotalTicks       = 20,
                PowerBudget      = 6,
                WinMeterFloor    = 0,    // just survive — any meter above 0
                FlavorIntro      = "ALERT: Catastrophic weather event detected. " +
                                   "Multiple system failures imminent. " +
                                   "This is not a drill.",
                StasisTokenAward = 0,
            },
        };

        // ── Stasis Tokens ────────────────────────────────────
        public const int MaxStasisTokens       = 3;
        public const int StartingStasisTokens  = 1;
 
        // Earn a token for keeping all meters above this % for a full tick
        public const double StasisEarnThreshold = 85.0;

        // ── Meter Settings ───────────────────────────────────
        public const double MeterStartValue    = 100.0;
        public const double MeterMin           = 0.0;
        public const double MeterMax           = 100.0;
 
        // Drain rates per tick (percentage points lost)
        // Scaled by system stress level (0.0–1.0) from fuzzy engine
        public const double CrewDrainRate       = 8.0;   // per tick at max stress
        public const double EcoDrainRate        = 12.0;  // eco is more sensitive
        public const double DomeDrainRate       = 5.0;   // dome only at high stress
 
        // Dome only starts draining above this stress level
        public const double DomeStressThreshold = 0.65;
 
        // Recovery rate per tick when stress is low (< 0.2)
        public const double CrewRecoveryRate    = 1.5;
        public const double EcoRecoveryRate     = 2.5;
        // Dome does not self-recover
 
        // ── Power Settings ───────────────────────────────────
        public const int DefaultPowerWarningThreshold = 7;
 
        // ── Coolant Settings ─────────────────────────────────
        public const double CoolantStartReserve    = 100.0;  // %
        public const double CoolantMistingCost     = 3.0;    // % per tick
        public const double CoolantActiveCost      = 7.0;    // % per tick
        public const double CoolantRechargePerTick = 1.5;    // slow passive refill
 
        // ── Console Rendering Constants ──────────────────────
        // These belong here so renderer and engine agree on dimensions
        public const int ConsoleWidth       = 75;
        public const int ControlPanelDividerColumn = 36;  // unbroken │ column
    }

    // ── Level Configuration ───────────────────────────────────
    public class LevelConfig
    {
        public int    LevelNumber      { get; init; }
        public string Name             { get; init; } = "";
        public string Subtitle         { get; init; } = "";
        public int    TotalTicks       { get; init; }
        public int    PowerBudget      { get; init; }
        public double WinMeterFloor    { get; init; }
        public string FlavorIntro      { get; init; } = "";
        public int    StasisTokenAward { get; init; }
    }
}