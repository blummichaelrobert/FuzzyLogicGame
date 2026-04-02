// ============================================================
//  EventSystem.cs
//  Defines all weather events and controls when they trigger.
//
//  Each level has a scheduled sequence of events.
//  Events modify raw temperature, humidity, and power budget
//  before the fuzzy engine sees them.
// ============================================================

using System;
using System.Collections.Generic;
using BiodomeAres1.Config;

namespace BiodomeAres1.Engine
{
    // ── Event Definition ─────────────────────────────────────
    /// <summary>
    /// A blueprint for a weather event — not the live instance.
    /// </summary>
    public record EventDefinition(
        string Name,
        string FlavorText,
        int    DurationTicks,
        double TempModifier,      // °F added to raw temp
        double HumidityModifier,  // % added to raw humidity
        double PowerModifier,     // multiplier on power budget (1.0 = no change)
        bool   SensorGlitch       // whether readings flicker
    );

    // ── Event Schedule Entry ─────────────────────────────────
    /// <summary>
    /// When to trigger an event within a level (by tick number).
    /// </summary>
    public record ScheduledEvent(int TriggerAtTick, EventDefinition Event);

    // ── Event Library ────────────────────────────────────────
    public static class EventLibrary
    {
        public static readonly EventDefinition SolarWarmFront = new(
            Name:             "Solar Warm Front",
            FlavorText:       "Solar irradiance increasing. " +
                              "Thermal load rising gradually.",
            DurationTicks:    4,
            TempModifier:     12.0,
            HumidityModifier: 5.0,
            PowerModifier:    1.0,
            SensorGlitch:     false
        );

        public static readonly EventDefinition DustStorm = new(
            Name:             "Dust Storm",
            FlavorText:       "Particulate density rising. " +
                              "Solar panel efficiency reduced.",
            DurationTicks:    5,
            TempModifier:     8.0,
            HumidityModifier: 10.0,
            PowerModifier:    0.75,   // storm cuts power budget by 25%
            SensorGlitch:     false
        );

        public static readonly EventDefinition HumiditySpike = new(
            Name:             "Humidity Spike",
            FlavorText:       "Atmospheric moisture surge detected. " +
                              "The air thickens.",
            DurationTicks:    3,
            TempModifier:     2.0,
            HumidityModifier: 25.0,
            PowerModifier:    1.0,
            SensorGlitch:     false
        );

         public static readonly EventDefinition FalseCalm = new(
            Name:             "False Calm",
            FlavorText:       "Readings normalising... " +
                              "Do not be deceived.",
            DurationTicks:    2,
            TempModifier:     -5.0,   // brief temperature drop — lures player
            HumidityModifier: -5.0,
            PowerModifier:    1.0,
            SensorGlitch:     false
        );
 
        public static readonly EventDefinition ExtremeHeatwave = new(
            Name:             "Extreme Heatwave",
            FlavorText:       "Catastrophic thermal event. " +
                              "All cooling systems to maximum.",
            DurationTicks:    6,
            TempModifier:     28.0,
            HumidityModifier: 15.0,
            PowerModifier:    1.0,
            SensorGlitch:     false
        );

        public static readonly EventDefinition PerfectStorm = new(
            Name:             "The Perfect Storm",
            FlavorText:       "All systems critical. " +
                              "What do you sacrifice?",
            DurationTicks:    5,
            TempModifier:     22.0,
            HumidityModifier: 22.0,
            PowerModifier:    0.65,
            SensorGlitch:     false
        );

        public static readonly EventDefinition SensorDegradation = new(
            Name:             "Sensor Degradation",
            FlavorText:       "Instrument array compromised. " +
                              "Readings unreliable.",
            DurationTicks:    4,
            TempModifier:     5.0,
            HumidityModifier: 8.0,
            PowerModifier:    1.0,
            SensorGlitch:     true   // readings flicker on display
        );
    }
    
    // ── Level Schedules ──────────────────────────────────────
    public static class EventSchedules
    {
        /// <summary>
        /// Returns the event schedule for a given level index (0-based).
        /// </summary>
        
        public static IReadOnlyList<ScheduledEvent> ForLevel(int levelIndex)
        {
             return levelIndex switch
            {
                // Level 1 — Calm: one gentle warm front mid-level
                0 => new List<ScheduledEvent>
                {
                    new(TriggerAtTick: 4, EventLibrary.SolarWarmFront),
                },
 
                // Level 2 — Storm: dust storm then humidity spike overlap
                1 => new List<ScheduledEvent>
                {
                    new(TriggerAtTick: 4,  EventLibrary.DustStorm),
                    new(TriggerAtTick: 8,  EventLibrary.HumiditySpike),
                    new(TriggerAtTick: 11, EventLibrary.FalseCalm),
                },
 
                // Level 3 — Crisis: compound events, sensor failure
                2 => new List<ScheduledEvent>
                {
                    new(TriggerAtTick: 2,  EventLibrary.DustStorm),
                    new(TriggerAtTick: 5,  EventLibrary.ExtremeHeatwave),
                    new(TriggerAtTick: 10, EventLibrary.SensorDegradation),
                    new(TriggerAtTick: 14, EventLibrary.FalseCalm),
                    new(TriggerAtTick: 16, EventLibrary.PerfectStorm),
                },
 
                _ => new List<ScheduledEvent>(),
            };
        }
    }

    // ── Event System ─────────────────────────────────────────
    /// <summary>
    /// Manages the active event lifecycle each tick.
    /// Called by GameLoop.ProcessTick().
    /// </summary>
    public class EventSystem
    {
        private readonly IReadOnlyList<ScheduledEvent> _schedule;
        private readonly HashSet<int> _triggeredTicks = new();

        public EventSystem(int levelIndex)
        {
            _schedule = EventSchedules.ForLevel(levelIndex);
        }

        /// <summary>
        /// Called each tick. May start a new event, tick down an
        /// existing one, or clear it if it has expired.
        /// Returns the active event after processing (or null).
        /// </summary>
        public ActiveEvent? ProcessTick(int currentTick, ActiveEvent? currentEvent)
        {
            // ── Check for a new scheduled event ──────────────
            foreach (var scheduled in _schedule)
            {
                if (scheduled.TriggerAtTick == currentTick && !_triggeredTicks.Contains(currentTick))
                {
                    _triggeredTicks.Add(currentTick);
                    var def = scheduled.Event;
                    return new ActiveEvent
                    {
                        Name              = def.Name,
                        FlavorText        = def.FlavorText,
                        TicksRemaining    = def.DurationTicks,
                        TempModifier      = def.TempModifier,
                        HumidityModifier  = def.HumidityModifier,
                        PowerModifier     = def.PowerModifier,
                        SensorGlitch      = def.SensorGlitch,
                    };
                }
            }

            // ── Tick down existing event ──────────────────────
            if (currentEvent != null)
            {
                currentEvent.TicksRemaining--;
                if (currentEvent.TicksRemaining <= 0)
                    return null;   // event expired
                return currentEvent;
            }
 
            return null;
        }
    }

}