// ============================================================
//  GameLoop.cs
//  The beating heart of Biodome Ares-1.
//
//  Two threads run concurrently:
//    1. TICK THREAD  — advances game time, processes events,
//                      runs fuzzy engine, drains/recovers meters
//    2. INPUT THREAD — listens for single keypresses with no
//                      blocking, routes them to the state machine
//
//  The stasis system pauses the tick thread without freezing
//  input — the player can still navigate while time is stopped.
//
//  The renderer is called from the tick thread after every
//  state change, and from the input thread after every
//  player action. Both paths produce a fresh GameSnapshot.
// ============================================================

using System;
using System.Threading;
using BiodomeAres1.Config;
using BiodomeAres1.Engine;
using BiodomeAres1.Rendering;

namespace BiodomeAres1.Engine
{
 public class GameLoop
    {
        // ── Dependencies ─────────────────────────────────────
        private readonly GameConfig  _config;
        private readonly GameState   _state;
        private readonly IRenderer   _renderer;

        // ── Threading ────────────────────────────────────────
        // A single lock object guards all state mutations.
        // Both threads acquire this before touching _state.
        private readonly object _stateLock = new object();

        private Thread? _tickThread;
        private Thread? _inputThread;

        // Signals both threads to stop cleanly
        private volatile bool _shutdown = false;

        // ── Tick timing ──────────────────────────────────────
        // Tracked with DateTime so we can report seconds remaining
        // to the renderer accurately even mid-tick.
        private DateTime _tickStartTime;
        private double   _tickSecondsRemaining;

        // ── Trend tracking ───────────────────────────────────
        // Previous meter values — compared each tick to set arrows
        private double _prevCrewHealth;
        private double _prevEcoIntegrity;
        private double _prevDomeIntegrity;
        private int    _crewTrend;    // -1 drain, 0 stable, +1 recovering
        private int    _ecoTrend;
        private int    _domeTrend;

        // ── Event system ─────────────────────────────────────
        private EventSystem? _eventSystem;

        // ── Control panel focus ──────────────────────────────
        // Which control is highlighted in the control panel view.
        // 'F' fans, 'D' dome shield, 'W' water, 'P' power, null = none
        private char? _controlPanelFocus = null;

        // ── Stasis token just-used flash ─────────────────────
        // True for one render pass after a token is consumed
        private bool _stasisJustActivated = false;

        // ── Constructor ──────────────────────────────────────
        public GameLoop(GameConfig config, GameState state, IRenderer renderer)
        {
            _config   = config;
            _state    = state;
            _renderer = renderer;
        }

        // ════════════════════════════════════════════════════
        //  PUBLIC ENTRY POINT
        // ════════════════════════════════════════════════════
 
        /// <summary>
        /// Starts the game loop. Blocks until the player quits.
        /// Called this from Program.cs
        /// </summary>
        public void Run()
        {
            _renderer.Initialise();

            // Show main menu and block until player starts
            _state.CurrentView = ViewState.MainMenu;
            RenderCurrent();
            WaitForMenuSelection();

            // Main game lifecycle — runs levels until game over or quit
            while (!_shutdown)
            {
                RunLevelIntro();
                if (_shutdown) break;

                RunLevel();
                if (_shutdown) break;

                // Check win/loss after level ends
                if (_state.Meters.AnyMeterDead)
                {
                    HandleGameOver("A critical system has failed.");
                    break;
                }

                // Advance to next level or show victory
                if (_state.CurrentLevelIndex < GameConfig.Levels.Length - 1)
                {
                    HandleLevelComplete();
                    _state.CurrentLevelIndex++;
                    PrepareLevel(_state.CurrentLevelIndex);
                }
                else
                {
                    HandleVictory();
                    break;
                }
            }

             _renderer.Teardown();
        }

        // ════════════════════════════════════════════════════
        //  LEVEL LIFECYCLE
        // ════════════════════════════════════════════════════
 
        private void PrepareLevel(int levelIndex)
        {
            lock (_stateLock)
            {
                var level = GameConfig.Levels[levelIndex]; 
                
                _state.CurrentLevelIndex = levelIndex;
                _state.CurrentTick       = 0;
                _state.ActiveEvent       = null;
                _state.LastAction        = null;
                _state.LastFuzzyResult   = null;

                // Meters carry over — do NOT reset between levels
                // Coolant carries over (by design — resource consequence)
 
                // Reset controls to safe defaults for new level
                _state.Controls = new ControlState
                {
                    Fans    = FanSetting.Low,
                    Shield  = ShieldSetting.Partial,
                    Coolant = CoolantSetting.Off,
                };

                // Base environment for this level
                // Level 2 and 3 start hotter to reflect escalation
                _state.RawTemperatureF = levelIndex switch
                {
                    0 => 72.0,
                    1 => 80.0,
                    2 => 88.0,
                    _ => 72.0,
                };
                _state.RawHumidityPct = levelIndex switch
                {
                    0 => 45.0,
                    1 => 55.0,
                    2 => 65.0,
                    _ => 45.0,
                };

                // Award stasis tokens from previous level completion
                // (already added in HandleLevelComplete before PrepareLevel)
 
                // Initialise event system for this level
                _eventSystem = new EventSystem(levelIndex);
 
                // Initialise trend tracking
                _prevCrewHealth    = _state.Meters.CrewHealth;
                _prevEcoIntegrity  = _state.Meters.EcosystemIntegrity;
                _prevDomeIntegrity = _state.Meters.DomeIntegrity;
                _crewTrend = _ecoTrend = _domeTrend = 0;
 
                _state.CurrentView = ViewState.LevelIntro;
            }   
        }

        private void RunLevelIntro()
        {
            lock (_stateLock)
            {
                _state.CurrentView = ViewState.LevelIntro;   
            }
              
            RenderCurrent();
 
            // Display intro for 3 seconds then auto-advance,
            // or immediately on any keypress
            WaitForKeyOrTimeout(3000);
        }

        private void RunLevel()
        {   
            lock (_stateLock)
            {
                _state.CurrentView = ViewState.MainView;
            }
            
            // Start input thread
            _inputThread = new Thread(InputLoop)
            {
                IsBackground = true,
                Name = "InputThread"
            };
            _inputThread.Start();

            // Run tick loop on the calling thread
            TickLoop();

            // Signal input thread to stop and wait for it
            _shutdown = _shutdown; // volatile read/write fence
            _inputThread.Join(500);
        }

        // ════════════════════════════════════════════════════
        //  TICK LOOP  (runs on main thread during a level)
        // ════════════════════════════════════════════════════

        private void TickLoop()
        {
            while (!_shutdown)
            {
                // ── Start a new tick ─────────────────────────
                _tickStartTime = DateTime.UtcNow;
                int tickDurationMs = _config.TickSpeedSeconds * 1000;   

                // ── Wait for tick duration ────────────────────
                // Check every 100ms so we can update the timer display
                // and respond to stasis activation promptly.
                while (!_shutdown)
                {
                    Thread.Sleep(100);
 
                    double elapsed =  (DateTime.UtcNow - _tickStartTime).TotalSeconds;
                    double remaining = _config.TickSpeedSeconds - elapsed;

                    lock (_stateLock)
                    {
                        // Stasis pauses the tick — extend start time
                        // so the tick resumes with full time remaining
                        if (_state.StasisActive)
                        {
                            _tickStartTime = DateTime.UtcNow - TimeSpan.FromSeconds(elapsed);
                            _tickSecondsRemaining = _config.TickSpeedSeconds - elapsed;
                            continue;   
                        } 

                        _tickSecondsRemaining = Math.Max(0, remaining);
                    } 

                    if (remaining <= 0) break;

                    // Re-render timer every 100ms so the countdown
                    // feels live without a full state recalculation
                    RenderCurrent();
                }

                if (_shutdown) break;
 
                // ── Process the tick ─────────────────────────
                bool levelDone  = false;
                bool gameOver   = false;
                string? reason  = null;

                lock (_stateLock)
                {
                    ProcessTick();
                    
                    // Clear just-changed marker after one full tick
                    if (_state.LastAction?.JustChanged == true)
                    {
                        _state.LastAction = _state.LastAction with { JustChanged = false };   
                    }

                    // Check win/loss conditions
                    if (_state.Meters.AnyMeterDead)
                    {
                        gameOver = true;
                        reason   = GetGameOverReason();
                    }
                    else if (_state.CurrentTick >= _state.CurrentLevel.TotalTicks)
                    {
                        // Check win floor
                        bool passed = _state.CurrentLevel.WinMeterFloor == 0
                            ? !_state.Meters.AnyMeterDead
                            : _state.Meters.AllMetersAbove(_state.CurrentLevel.WinMeterFloor);
 
                        levelDone = passed;
                        gameOver  = !passed;
                        
                        if (gameOver)
                            reason = "Conditions exceeded survivable limits.";
                    }
                        
                }

                RenderCurrent();
 
                if (gameOver)
                {
                    HandleGameOver(reason ?? "Unknown failure.");
                    _shutdown = true;
                    return;
                }
                if (levelDone) return;
            }
        }

        // ════════════════════════════════════════════════════
        //  PROCESS TICK  (called inside lock)
        // ════════════════════════════════════════════════════
 
        private void ProcessTick()
        {
            _state.CurrentTick++;
 
            // ── 1. Update weather events ──────────────────────
            _state.ActiveEvent = _eventSystem!.ProcessTick(_state.CurrentTick, _state.ActiveEvent);

            // ── 2. Drift raw environment ──────────────────────
            // Each level slowly drifts toward more hostile conditions
            // even without events — the world is always getting worse
            double tempDrift = _state.CurrentLevelIndex switch
            {
                0 => 0.3,   // gentle drift in Level 1
                1 => 0.6,   // noticeable in Level 2
                2 => 1.0,   // relentless in Level 3
                _ => 0.3,
            };

            double humDrift = _state.CurrentLevelIndex switch
            {
                0 => 0.2,
                1 => 0.4,
                2 => 0.7,
                _ => 0.2,
            };

             _state.RawTemperatureF += tempDrift;
            _state.RawHumidityPct =  Math.Min(100, _state.RawHumidityPct + humDrift);

            // ── 3. Validate power budget ──────────────────────
            // If over budget (e.g. event reduced budget mid-level),
            // auto-reduce fans first, then shield, then coolant
            EnforcePowerBudget();

            // ── 4. Drain coolant reserve ──────────────────────
            if (_state.Controls.Coolant == CoolantSetting.Misting)
            {
                _state.CoolantReserve = Math.Max(0, _state.CoolantReserve - GameConfig.CoolantMistingCost);   
            }
                
            else if (_state.Controls.Coolant == CoolantSetting.Active)
            {
                _state.CoolantReserve = Math.Max(0, _state.CoolantReserve - GameConfig.CoolantActiveCost);   
            }
                
            else
            {
                // Passive refill when coolant is off
                _state.CoolantReserve = Math.Min(GameConfig.CoolantStartReserve, _state.CoolantReserve + GameConfig.CoolantRechargePerTick);   
            }

            // If coolant is empty, force it off
            if (_state.CoolantReserve <= 0 && _state.Controls.Coolant != CoolantSetting.Off)
            {
                _state.Controls.Coolant = CoolantSetting.Off;
                _state.LastAction = new PlayerAction("⚠ Coolant exhausted — system offline.", false);
            }

            // ── 5. Run fuzzy engine ───────────────────────────
            _state.LastFuzzyResult = FuzzyEngine.Evaluate(_state.EffectiveTemperatureF, _state.EffectiveHumidityPct);

            double stress = _state.LastFuzzyResult.SystemStress;

            // ── 6. Update meters ──────────────────────────────
            _prevCrewHealth    = _state.Meters.CrewHealth;
            _prevEcoIntegrity  = _state.Meters.EcosystemIntegrity;
            _prevDomeIntegrity = _state.Meters.DomeIntegrity;

            ApplyMeterChanges(stress);

            // Update trend arrows
            _crewTrend  = Trend(_state.Meters.CrewHealth,        _prevCrewHealth);
            _ecoTrend   = Trend(_state.Meters.EcosystemIntegrity,_prevEcoIntegrity);
            _domeTrend  = Trend(_state.Meters.DomeIntegrity,     _prevDomeIntegrity);

            // ── 7. Award stasis token for excellent performance ──
            if (_state.Meters.AllMetersAbove(GameConfig.StasisEarnThreshold) && _state.StasisTokens < GameConfig.MaxStasisTokens)
            {
                _state.StasisTokens++;
                _state.LastAction = new PlayerAction(
                    "✓ All systems green — Stasis Token earned.", false);
            }
        }

        // ── Meter drain / recovery ───────────────────────────
        private void ApplyMeterChanges(double stress)
        {
            // Crew and Eco drain proportional to stress
            double crewChange = stress > 0.2
                ? -(stress * GameConfig.CrewDrainRate)
                : +(GameConfig.CrewRecoveryRate * (1.0 - stress * 5));

            double ecoChange = stress > 0.15
                ? -(stress * GameConfig.EcoDrainRate)
                : +(GameConfig.EcoRecoveryRate * (1.0 - stress * 5));

             // Dome only drains above its stress threshold
            double domeChange = stress > GameConfig.DomeStressThreshold
                ? -(stress * GameConfig.DomeDrainRate)
                : 0.0;   // dome does not self-recover
            
            _state.Meters.CrewHealth = Math.Clamp(
                _state.Meters.CrewHealth + crewChange,
                GameConfig.MeterMin, GameConfig.MeterMax);
            
            _state.Meters.EcosystemIntegrity = Math.Clamp(
                _state.Meters.EcosystemIntegrity + ecoChange,
                GameConfig.MeterMin, GameConfig.MeterMax);
 
            _state.Meters.DomeIntegrity = Math.Clamp(
                _state.Meters.DomeIntegrity + domeChange,
                GameConfig.MeterMin, GameConfig.MeterMax);
        }

        // ── Power budget enforcement ─────────────────────────
        private void EnforcePowerBudget()
        {
            // Auto-reduce settings if over budget
            // Priority: reduce Coolant first (most expensive),
            // then Fans, then Shield — preserves minimum protection
            while (_state.PowerOverBudget)
            {
                if (_state.Controls.Coolant == CoolantSetting.Active) { _state.Controls.Coolant = CoolantSetting.Misting; continue; }
                
                if (_state.Controls.Coolant == CoolantSetting.Misting) { _state.Controls.Coolant = CoolantSetting.Off; continue; }
                
                if (_state.Controls.Fans == FanSetting.Maximum) { _state.Controls.Fans = FanSetting.High; continue; }
                
                if (_state.Controls.Fans == FanSetting.High) { _state.Controls.Fans = FanSetting.Medium; continue; }
                
                if (_state.Controls.Fans == FanSetting.Medium) { _state.Controls.Fans = FanSetting.Low; continue; }
                
                if (_state.Controls.Shield == ShieldSetting.Full) { _state.Controls.Shield = ShieldSetting.Partial; continue; }
                
                if (_state.Controls.Fans == FanSetting.Low) { _state.Controls.Fans = FanSetting.Off; continue; }
                
                break; // nothing left to reduce   
            }   
        }

        // ── Trend helper ─────────────────────────────────────
        private static int Trend(double current, double previous)
        {
             double delta = current - previous;
            if (delta >  0.1) return  1;
            if (delta < -0.1) return -1;
            return 0;
        }

        // ── Game over reason ─────────────────────────────────
        private string GetGameOverReason()
        {
            if (_state.Meters.CrewHealth <= 0){ return "Crew health has reached zero. All hands lost."; }
                
            if (_state.Meters.EcosystemIntegrity <= 0) { return "Ecosystem collapsed. Oxygen production offline."; }
                
            if (_state.Meters.DomeIntegrity <= 0) { return "Dome integrity failed. Hull breach detected."; }
               
            return "Unknown system failure.";
        }

        // ════════════════════════════════════════════════════
        //  INPUT LOOP  (runs on background thread)
        // ════════════════════════════════════════════════════
 
        private void InputLoop()
        {
            while (!_shutdown)
            {
                 // Non-blocking check — avoids locking the thread
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }
 
                var key = Console.ReadKey(intercept: true);
                HandleInput(key);
            }
        }

        private void HandleInput(ConsoleKeyInfo key)
        {
            char ch = char.ToUpper(key.Key switch
            {
                ConsoleKey.F => 'F',
                ConsoleKey.D => 'D',
                ConsoleKey.W => 'W',
                ConsoleKey.P => 'P',
                ConsoleKey.S => 'S',
                ConsoleKey.C => 'C',
                ConsoleKey.Q => 'Q',
                _            => (char)0,
            });

            // Also handle character key directly
            if (ch == 0) { ch = char.ToUpper(key.KeyChar);}
                
            lock (_stateLock)
            {
                switch (_state.CurrentView)
                {
                    case ViewState.MainView:
                        HandleMainViewInput(ch, key);
                        break;

                    case ViewState.ControlPanelView:
                        HandleControlPanelInput(ch, key);
                        break;
                }
            }

             RenderCurrent();
        }

        // ── Main view input ───────────────────────────────────
        private void HandleMainViewInput(char ch, ConsoleKeyInfo key)
        {
            switch (ch)
            {
                case 'C':
                    // Open control panel
                    _state.CurrentView     = ViewState.ControlPanelView;
                    _controlPanelFocus     = null;
                    break;
 
                case 'S':
                    // Activate / deactivate stasis
                    TryToggleStasis();
                    break;
 
                case 'Q':
                    _shutdown = true;
                    break;
            }
        }

        // ── Control panel input ───────────────────────────────
        private void HandleControlPanelInput(char ch, ConsoleKeyInfo key)
        {
            // If no control is focused, route to selection
            if (_controlPanelFocus == null)
            {
                switch (ch)
                {
                    case 'F': _controlPanelFocus = 'F'; break;
                    case 'D': _controlPanelFocus = 'D'; break;
                    case 'W': _controlPanelFocus = 'W'; break;
                    case 'P': _controlPanelFocus = 'P'; break;
                    case 'S': TryToggleStasis();        break;
                    case 'C':
                        // Return to main view
                        _state.CurrentView = ViewState.MainView;
                        _controlPanelFocus = null;
                        break;
                    case 'Q': _shutdown = true;         break;
                }
                return;
            }

            // A control is focused — handle adjustment or confirmation
            if (key.Key == ConsoleKey.Add || key.KeyChar == '+' || key.Key == ConsoleKey.OemPlus)
            {
                AdjustControl(_controlPanelFocus.Value, +1);
                return;
            }

            if (key.Key == ConsoleKey.Subtract || key.KeyChar == '-' || key.Key == ConsoleKey.OemMinus)
            {
                AdjustControl(_controlPanelFocus.Value, -1);
                return;
            }

            // Pressing the same letter again confirms and deselects
            if (ch == _controlPanelFocus.Value)
            {
                _controlPanelFocus = null;
                return;
            }

            // Pressing a different letter switches focus
            switch (ch)
            {
                case 'F': _controlPanelFocus = 'F'; break;
                case 'D': _controlPanelFocus = 'D'; break;
                case 'W': _controlPanelFocus = 'W'; break;
                case 'P': _controlPanelFocus = 'P'; break;
                case 'C':
                    _state.CurrentView = ViewState.MainView;
                    _controlPanelFocus = null;
                    break;
            }
        }

        // ── Control adjustment ────────────────────────────────
        private void AdjustControl(char control, int direction)
        {
            string actionText = "";
            bool changed = false;

            switch (control)
            {
                case 'F':
                {
                    var current = (int)_state.Controls.Fans;
                    var next    = Math.Clamp(current + direction, 0, (int)FanSetting.Maximum);
                    var newSetting = (FanSetting)next;
 
                    // Preview power cost — block if over budget
                    var preview = new ControlState
                    {
                        Fans    = newSetting,
                        Shield  = _state.Controls.Shield,
                        Coolant = _state.Controls.Coolant,
                    };
                    if (preview.TotalPowerUsed > _state.EffectivePowerBudget && direction > 0)
                    {
                        actionText = "⚠ Insufficient power for Fans increase.";
                    }
                    else if (newSetting != _state.Controls.Fans)
                    {
                        _state.Controls.Fans = newSetting;
                        actionText = $"Fans set to {newSetting}.";
                        changed    = true;
                    }
                    break;
                }
                case 'D':
                {
                    var current = (int)_state.Controls.Shield;
                    var next    = Math.Clamp(current + direction, 0, (int)ShieldSetting.Full);
                    var newSetting = (ShieldSetting)next;
 
                    var preview = new ControlState
                    {
                        Fans    = _state.Controls.Fans,
                        Shield  = newSetting,
                        Coolant = _state.Controls.Coolant,
                    };
                    if (preview.TotalPowerUsed > _state.EffectivePowerBudget && direction > 0)
                    {
                        actionText = "⚠ Insufficient power for Shield increase.";
                    }
                    else if (newSetting != _state.Controls.Shield)
                    {
                        _state.Controls.Shield = newSetting;
                        actionText = $"Shield set to {newSetting}.";
                        changed    = true;
                    }
                    break;
                }
                case 'W':
                {
                    var current = (int)_state.Controls.Coolant;
                    var next    = Math.Clamp(current + direction, 0,
                                      (int)CoolantSetting.Active);
                    var newSetting = (CoolantSetting)next;
 
                    // Block if coolant reserve is empty
                    if (newSetting != CoolantSetting.Off && _state.CoolantReserve <= 0)
                    {
                        actionText = "⚠ Coolant reserve empty.";
                        break;
                    }
 
                    var preview = new ControlState
                    {
                        Fans    = _state.Controls.Fans,
                        Shield  = _state.Controls.Shield,
                        Coolant = newSetting,
                    };
                    if (preview.TotalPowerUsed > _state.EffectivePowerBudget && direction > 0)
                    {
                        actionText = "⚠ Insufficient power for Coolant increase.";
                    }
                    else if (newSetting != _state.Controls.Coolant)
                    {
                        _state.Controls.Coolant = newSetting;
                        actionText = $"Coolant set to {newSetting}.";
                        changed    = true;
                    }
                    break;
                }
                case 'P':
                {
                    // Power warning threshold adjustment
                    int next = Math.Clamp(_state.PowerWarningThreshold + direction, 1, _state.EffectivePowerBudget);
                    if (next != _state.PowerWarningThreshold)
                    {
                        _state.PowerWarningThreshold = next;
                        actionText = $"Power warning set to {next} ⚡.";
                        changed    = true;
                    }
                    break;
                }
            }

            if (actionText != "") { _state.LastAction = new PlayerAction(actionText, changed); }       
        }

        // ── Stasis toggle ─────────────────────────────────────
        private void TryToggleStasis()
        {
            if (_state.StasisActive)
            {
                // Deactivate — resume tick
                _state.StasisActive = false;
                _state.LastAction   = new PlayerAction("Stasis released. Time resumes.", false);
            }
            else if (_state.StasisTokens > 0)
            {
                // Activate — consume one token
                _state.StasisTokens--;
                _state.StasisActive       = true;
                _stasisJustActivated      = true;
                _state.LastAction         = new PlayerAction($"⏸ Stasis Token activated. " + $"{_state.StasisTokens} remaining.", false);
            }
            else
            {
                _state.LastAction = new PlayerAction("⚠ No Stasis Tokens remaining.", false);
            }
        }

        // ════════════════════════════════════════════════════
        //  OUTCOME HANDLERS
        // ════════════════════════════════════════════════════
 
        private void HandleLevelComplete()
        {
            lock (_stateLock)
            {
                // Award stasis tokens for level completion
                int award = _state.CurrentLevel.StasisTokenAward;
                _state.StasisTokens = Math.Min(GameConfig.MaxStasisTokens, _state.StasisTokens + award);
 
                _state.CurrentView = ViewState.LevelComplete;
            }
            RenderCurrent();
            WaitForKeyOrTimeout(5000);
        }

        private void HandleGameOver(string reason)
        {
            lock (_stateLock) { _state.CurrentView = ViewState.GameOver; }
                
 
            // Store reason for renderer via snapshot
            _gameOverReason = reason;
            RenderCurrent();
            WaitForKeyOrTimeout(8000);
        }

        private void HandleVictory()
        {
            lock (_stateLock) { _state.CurrentView = ViewState.LevelComplete; }
                
 
            _gameOverReason = "MISSION COMPLETE — Ares-1 survived.";
            RenderCurrent();
            WaitForKeyOrTimeout(8000);
        }

        // Stored separately so HandleGameOver can pass it to snapshot
        private string _gameOverReason = "";

        // ════════════════════════════════════════════════════
        //  MENU / WAITING HELPERS
        // ════════════════════════════════════════════════════
 
        private void WaitForMenuSelection()
        {
            while (!_shutdown)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }
                var key = Console.ReadKey(intercept: true);
                char ch = char.ToUpper(key.KeyChar);

                switch (ch)
                {
                    case 'N':
                        // New game — prepare level 0
                        PrepareLevel(0);
                        return;
 
                    case 'S':
                        ShowSettingsMenu();
                        break;
 
                    case 'Q':
                        _shutdown = true;
                        return;
                }
 
                RenderCurrent();
            }
        }

        private void ShowSettingsMenu()
        {
            lock (_stateLock){ _state.CurrentView = ViewState.SettingsMenu; }
                
            RenderCurrent();

            while (!_shutdown)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }
 
                var key = Console.ReadKey(intercept: true);

                lock (_stateLock)
                {
                    // Preset selections
                    if (key.KeyChar == '1') _config.TickSpeedSeconds = 3;
                    if (key.KeyChar == '2') _config.TickSpeedSeconds = 8;
                    if (key.KeyChar == '3') _config.TickSpeedSeconds = 15;
 
                    // Custom adjustment with + / -
                    if (key.KeyChar == '+' || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus)
                        _config.TickSpeedSeconds =  Math.Min(60, _config.TickSpeedSeconds + 1);
 
                    if (key.KeyChar == '-' || key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus)
                        _config.TickSpeedSeconds = Math.Max(1, _config.TickSpeedSeconds - 1);
                }

                // Escape or Enter returns to menu
                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter)
                {
                    lock (_stateLock){ _state.CurrentView = ViewState.MainMenu; }
                        
                    RenderCurrent();
                    return;
                }
 
                RenderCurrent();
            }
        }

        /// <summary>
        /// Blocks for up to timeoutMs, returning early on any keypress.
        /// Used for intros, level complete screens, and game over.
        /// </summary>
        private void WaitForKeyOrTimeout(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!_shutdown && DateTime.UtcNow < deadline)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(intercept: true);
                    return;
                }
                Thread.Sleep(50);
            }
        }

        // ════════════════════════════════════════════════════
        //  RENDER HELPER
        // ════════════════════════════════════════════════════
 
        /// <summary>
        /// Builds a GameSnapshot from current state and calls Render.
        /// Always called outside the state lock to avoid deadlock —
        /// rendering is read-only and can happen on either thread.
        /// </summary>
        private void RenderCurrent()
        {
            GameSnapshot snapshot;
 
            lock (_stateLock)
            {
                snapshot = GameSnapshot.From(
                    state:                _state,
                    config:               _config,
                    tickSecondsRemaining: _tickSecondsRemaining,
                    crewTrend:            _crewTrend,
                    ecoTrend:             _ecoTrend,
                    domeTrend:            _domeTrend,
                    controlPanelFocus:    _controlPanelFocus,
                    isGameOver:           _state.CurrentView == ViewState.GameOver,
                    isLevelComplete:      _state.CurrentView == ViewState.LevelComplete,
                    gameOverReason:       _gameOverReason,
                    sensorGlitch:         _state.ActiveEvent?.SensorGlitch ?? false
                );
            }

            // Render outside the lock — renderer is pure display,
            // it receives a snapshot and cannot mutate game state
            _renderer.Render(snapshot);
        }
    }   
}