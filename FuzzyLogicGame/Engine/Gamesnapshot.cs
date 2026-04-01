// ============================================================
//  GameSnapshot.cs
//  An immutable point-in-time copy of GameState.
//  This is the data contract between the engine and renderer.
//
//  The renderer only ever sees a GameSnapshot — never the
//  live GameState. This means:
//    - No renderer can accidentally mutate game state
//    - The snapshot is safe to pass across threads
//    - When a GUI layer replaces the console renderer,
//      it receives the same snapshot — nothing else changes
// ============================================================

using BiodomeAres1.Config;

namespace BiodomeAres1.Engine
{
    /// <summary>
    /// Immutable snapshot of everything the renderer needs to draw.
    /// Created by GameLoop at the start of each render pass.
    /// </summary>
    public record GameSnapshot
    {
        // ── View ─────────────────────────────────────────────
        public ViewState CurrentView       { get; init; }

        // ── Level ────────────────────────────────────────────
        public int    LevelNumber          { get; init; }
        public string LevelName            { get; init; } = "";
        public string LevelSubtitle        { get; init; } = "";
        public int    CurrentTick          { get; init; }
        public int    TotalTicks           { get; init; }
        public int    TickSpeedSeconds     { get; init; }
        public double TickSecondsRemaining { get; init; }

        // ── Environment (raw — shown with flicker if sensor glitch) ──
        public double RawTemperatureF      { get; init; }
        public double RawHumidityPct       { get; init; }
 
        // ── Environment (effective — what fuzzy engine used) ─
        public double EffectiveTemperatureF  { get; init; }
        public double EffectiveHumidityPct   { get; init; }
 
        // ── Fuzzy result ─────────────────────────────────────
        public FuzzyResult? FuzzyResult    { get; init; }
 
        // ── Meters ───────────────────────────────────────────
        public double CrewHealth           { get; init; }
        public double EcosystemIntegrity   { get; init; }
        public double DomeIntegrity        { get; init; }

         // Trend indicators (-1 draining, 0 stable, +1 recovering)
        public int CrewTrend               { get; init; }
        public int EcoTrend                { get; init; }
        public int DomeTrend               { get; init; }
 
        // ── Controls ─────────────────────────────────────────
        public FanSetting     Fans         { get; init; }
        public ShieldSetting  Shield       { get; init; }
        public CoolantSetting Coolant      { get; init; }
 
        public int FanPowerCost            { get; init; }
        public int ShieldPowerCost         { get; init; }
        public int CoolantPowerCost        { get; init; }
        public int TotalPowerUsed          { get; init; }
        public int PowerBudget             { get; init; }
        public int PowerRemaining          { get; init; }
        public bool PowerOverBudget        { get; init; }
        public int PowerWarningThreshold   { get; init; }
 
        // ── Resources ────────────────────────────────────────
        public double CoolantReserve       { get; init; }

        // ── Stasis ───────────────────────────────────────────
        public bool StasisActive           { get; init; }
        public int  StasisTokens           { get; init; }
 
        // ── Active Event ─────────────────────────────────────
        public string? EventName           { get; init; }
        public string? EventFlavor         { get; init; }
        public int?    EventTicksRemaining { get; init; }
        public bool    SensorGlitch        { get; init; }
 
        // ── Player feedback ───────────────────────────────────
        public string? LastActionText      { get; init; }
        public bool    LastActionJustChanged { get; init; }
 
        // ── Control Panel focus ──────────────────────────────
        // Which control is currently selected in the panel view
        public char? ControlPanelFocus     { get; init; }

        // ── Game outcome ─────────────────────────────────────
        public bool IsGameOver             { get; init; }
        public bool IsLevelComplete        { get; init; }
        public string? GameOverReason      { get; init; }

         // ── Factory method ───────────────────────────────────
        /// <summary>
        /// Creates a snapshot from live GameState.
        /// Call this at the start of each render pass.
        /// </summary>
        public static GameSnapshot From(
            GameState state,
            GameConfig config,
            double tickSecondsRemaining,
            int crewTrend, int ecoTrend, int domeTrend,
            char? controlPanelFocus = null,
            bool isGameOver = false,
            bool isLevelComplete = false,
            string? gameOverReason = null,
            bool sensorGlitch = false)
        {
            return new GameSnapshot
            {
                CurrentView             = state.CurrentView,
                LevelNumber             = state.CurrentLevel.LevelNumber,
                LevelName               = state.CurrentLevel.Name,
                LevelSubtitle           = state.CurrentLevel.Subtitle,
                CurrentTick             = state.CurrentTick,
                TotalTicks              = state.CurrentLevel.TotalTicks,
                TickSpeedSeconds        = config.TickSpeedSeconds,
                TickSecondsRemaining    = tickSecondsRemaining,
 
                RawTemperatureF         = state.RawTemperatureF,
                RawHumidityPct          = state.RawHumidityPct,
                EffectiveTemperatureF   = state.EffectiveTemperatureF,
                EffectiveHumidityPct    = state.EffectiveHumidityPct,
 
                FuzzyResult             = state.LastFuzzyResult,
 
                CrewHealth              = state.Meters.CrewHealth,
                EcosystemIntegrity      = state.Meters.EcosystemIntegrity,
                DomeIntegrity           = state.Meters.DomeIntegrity,
                CrewTrend               = crewTrend,
                EcoTrend                = ecoTrend,
                DomeTrend               = domeTrend,
 
                Fans                    = state.Controls.Fans,
                Shield                  = state.Controls.Shield,
                Coolant                 = state.Controls.Coolant,
                FanPowerCost            = state.Controls.FanPowerCost,
                ShieldPowerCost         = state.Controls.ShieldPowerCost,
                CoolantPowerCost        = state.Controls.CoolantPowerCost,
                TotalPowerUsed          = state.Controls.TotalPowerUsed,
                PowerBudget             = state.EffectivePowerBudget,
                PowerRemaining          = state.PowerRemaining,
                PowerOverBudget         = state.PowerOverBudget,
                PowerWarningThreshold   = state.PowerWarningThreshold,
 
                CoolantReserve          = state.CoolantReserve,
 
                StasisActive            = state.StasisActive,
                StasisTokens            = state.StasisTokens,
 
                EventName               = state.ActiveEvent?.Name,
                EventFlavor             = state.ActiveEvent?.FlavorText,
                EventTicksRemaining     = state.ActiveEvent?.TicksRemaining,
                SensorGlitch            = sensorGlitch,
 
                LastActionText          = state.LastAction?.Description,
                LastActionJustChanged   = state.LastAction?.JustChanged ?? false,
 
                ControlPanelFocus       = controlPanelFocus,
 
                IsGameOver              = isGameOver,
                IsLevelComplete         = isLevelComplete,
                GameOverReason          = gameOverReason,
            };
        }
    }
}