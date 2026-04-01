// ============================================================
//  GameState.cs
//  The single source of truth for all live game data.
//  Only the GameLoop writes to this — everything else reads
//  from a GameSnapshot (immutable copy).
// ============================================================

using BiodomeAres1.Config;

namespace BiodomeAres1.Engine
{
    // ── View State ───────────────────────────────────────────
    /// <summary>
    /// Which screen the player is currently on.
    /// The renderer switches layout based on this value.
    /// </summary>
    
    public enum ViewState
    {
        MainView,
        ControlPanelView,
        MainMenu,
        SettingsMenu,
        LevelIntro,
        LevelComplete,
        GameOver,
    }

    // ── Control Settings ─────────────────────────────────────
 
    public enum FanSetting     { Off, Low, Medium, High, Maximum }
    public enum ShieldSetting  { Retracted, Partial, Full }
    public enum CoolantSetting { Off, Misting, Active }

    /// <summary>
    /// All four control states and their derived power cost.
    /// </summary>
    public class ControlState
    {
        public FanSetting     Fans    { get; set; } = FanSetting.Low;
        public ShieldSetting  Shield  { get; set; } = ShieldSetting.Partial;
        public CoolantSetting Coolant { get; set; } = CoolantSetting.Off;

        // Power costs per setting
        public int FanPowerCost => Fans switch
        {
            FanSetting.Off     => 0,
            FanSetting.Low     => 1,
            FanSetting.Medium  => 2,
            FanSetting.High    => 3,
            FanSetting.Maximum => 4,
            _                  => 0,
        };

        public int ShieldPowerCost => Shield switch
        {
            ShieldSetting.Retracted => 0,
            ShieldSetting.Partial   => 1,
            ShieldSetting.Full      => 2,
            _                       => 0,
        };

        public int CoolantPowerCost => Coolant switch
        {
            CoolantSetting.Off     => 0,
            CoolantSetting.Misting => 1,
            CoolantSetting.Active  => 3,
            _                      => 0,
        };

        public int TotalPowerUsed => FanPowerCost + ShieldPowerCost + CoolantPowerCost;

        // ── Environmental modifiers ──────────────────────────
        // How much each control reduces effective temp / humidity
        // These feed into the fuzzy engine as input offsets
 
        public double TempReductionF => FanTempReduction + ShieldTempReduction + CoolantTempReduction;
 
        public double HumidityReductionPct => FanHumidityReduction + CoolantHumidityReduction;

        // Humidity increase from coolant misting
        public double HumidityIncreasePct => Coolant == CoolantSetting.Misting ? 5.0 : 0.0;
 
        // Net humidity effect (reduction minus any increase)
        public double NetHumidityEffectPct => HumidityReductionPct - HumidityIncreasePct;

        private double FanTempReduction => Fans switch
        {
            FanSetting.Off     => 0.0,
            FanSetting.Low     => 2.0,
            FanSetting.Medium  => 5.0,
            FanSetting.High    => 9.0,
            FanSetting.Maximum => 14.0,
            _                  => 0.0,
        };
 
        private double FanHumidityReduction => Fans switch
        {
            FanSetting.Off     => 0.0,
            FanSetting.Low     => 1.0,
            FanSetting.Medium  => 3.0,
            FanSetting.High    => 6.0,
            FanSetting.Maximum => 10.0,
            _                  => 0.0,
        };
 
        private double ShieldTempReduction => Shield switch
        {
            ShieldSetting.Retracted => 0.0,
            ShieldSetting.Partial   => 4.0,
            ShieldSetting.Full      => 8.0,
            _                       => 0.0,
        };
 
        private double CoolantTempReduction => Coolant switch
        {
            CoolantSetting.Off     => 0.0,
            CoolantSetting.Misting => 6.0,
            CoolantSetting.Active  => 15.0,
            _                      => 0.0,
        };
 
        private double CoolantHumidityReduction => Coolant switch
        {
            CoolantSetting.Off    => 0.0,
            CoolantSetting.Active => 8.0,   // active cooling dehumidifies
            _                     => 0.0,
        };
 
        // Solar generation efficiency affected by full shield
        public double SolarEfficiency => Shield switch
        {
            ShieldSetting.Retracted => 1.00,
            ShieldSetting.Partial   => 0.85,
            ShieldSetting.Full      => 0.60,
            _                       => 1.00,
        };
        
    }
    
    // ── Meter State ──────────────────────────────────────────
    public class MeterState
    {
        public double CrewHealth          { get; set; } = GameConfig.MeterStartValue;
        public double EcosystemIntegrity  { get; set; } = GameConfig.MeterStartValue;
        public double DomeIntegrity       { get; set; } = GameConfig.MeterStartValue;
 
        public bool AnyMeterDead =>
            CrewHealth         <= GameConfig.MeterMin ||
            EcosystemIntegrity <= GameConfig.MeterMin ||
            DomeIntegrity      <= GameConfig.MeterMin;
 
        public bool AllMetersAbove(double threshold) =>
            CrewHealth         > threshold &&
            EcosystemIntegrity > threshold &&
            DomeIntegrity      > threshold;
    }

    // ── Active Weather Event ─────────────────────────────────
    public class ActiveEvent
    {
        public string Name        { get; set; } = "";
        public string FlavorText  { get; set; } = "";
        public int    TicksRemaining { get; set; }
        public double TempModifier   { get; set; }   // raw temp shift
        public double HumidityModifier { get; set; } // raw humidity shift
        public double PowerModifier    { get; set; } // budget multiplier
        public bool   SensorGlitch     { get; set; } // flickers readings
    }

    // ── Last Player Action ───────────────────────────────────
    // Shown in the event log as confirmation feedback
    public record PlayerAction(string Description, bool JustChanged);

    // ── Full Game State ──────────────────────────────────────
    /// <summary>
    /// All mutable game data. Only GameLoop modifies this directly.
    /// </summary>
    
    public class GameState
    {
         // ── Session ──────────────────────────────────────────
        public ViewState  CurrentView       { get; set; } = ViewState.MainMenu;
        public int        CurrentLevelIndex { get; set; } = 0;
        public int        CurrentTick       { get; set; } = 0;
        public bool       IsRunning         { get; set; } = false;
        public bool       StasisActive      { get; set; } = false;
        public int        StasisTokens      { get; set; } = GameConfig.StartingStasisTokens;

        // ── Environment ──────────────────────────────────────
        // Raw conditions before controls are applied
        public double RawTemperatureF   { get; set; } = 72.0;
        public double RawHumidityPct    { get; set; } = 45.0;

        // Effective conditions after controls — what fuzzy engine sees
        public double EffectiveTemperatureF => Math.Max(0, RawTemperatureF - Controls.TempReductionF + (ActiveEvent?.TempModifier ?? 0));

        public double EffectiveHumidityPct => Math.Clamp(RawHumidityPct - Controls.NetHumidityEffectPct + (ActiveEvent?.HumidityModifier ?? 0), 0, 100);

        // ── Controls ─────────────────────────────────────────
        public ControlState Controls { get; set; } = new ControlState();

        // ── Meters ───────────────────────────────────────────
        public MeterState Meters { get; set; } = new MeterState();
 
        // ── Resources ────────────────────────────────────────
        public double CoolantReserve { get; set; } = GameConfig.CoolantStartReserve;

        // ── Events ───────────────────────────────────────────
        public ActiveEvent? ActiveEvent { get; set; } = null;

        // ── Last fuzzy result (for display) ──────────────────
        public FuzzyResult? LastFuzzyResult { get; set; } = null;
 
        // ── Player feedback ───────────────────────────────────
        public PlayerAction? LastAction { get; set; } = null;

        // ── Power ─────────────────────────────────────────────
        public int PowerWarningThreshold { get; set; } = GameConfig.DefaultPowerWarningThreshold;
 
        public LevelConfig CurrentLevel => GameConfig.Levels[CurrentLevelIndex];
 
        public int EffectivePowerBudget => (int)(CurrentLevel.PowerBudget * (ActiveEvent?.PowerModifier ?? 1.0));
 
        public int PowerRemaining => EffectivePowerBudget - Controls.TotalPowerUsed;
 
        public bool PowerOverBudget => Controls.TotalPowerUsed > EffectivePowerBudget;
    }
}