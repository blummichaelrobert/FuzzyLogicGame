// ============================================================
//  ControlPanelView.cs
//  Draws the two-column control panel screen.
//  Accessed by pressing [C] from the main view.
//
//  Column split is at column 38 — unbroken │ every row.
//
//  Layout:
//    0      — top border
//    1–2    — header (level, stasis tokens, tick timer)
//    3      — column header divider  ╠═══╪═══╣
//    4–11   — LEFT: Fans   RIGHT: Shield
//    12     — mid divider            ╠═══╪═══╣
//    13–20  — LEFT: Coolant  RIGHT: Power
//    21     — bottom section divider
//    22–23  — event log
//    24     — bottom border
//    25     — hotkey bar
// ============================================================

using System;
using BiodomeAres1.Engine;
using static BiodomeAres1.Rendering.ConsoleRenderer;

namespace BiodomeAres1.Rendering
{
    public static class ControlPanelView
    {
        // The column of the unbroken vertical divider
        private const int Split     = 38;
        private const int W         = TotalWidth;
 
        public static void Draw(GameSnapshot snap)
        {
            DrawHeader(snap);
            DrawColumnHeaders();
            DrawFansAndShield(snap);
            DrawMidDivider();
            DrawCoolantAndPower(snap);
            DrawEventLog(snap);
            DrawHotkeyBar(snap);
        }

        // ── Rows 0–2: Header ─────────────────────────────────
        private static void DrawHeader(GameSnapshot snap)
        {
            HRule(0, 'T');
 
            string stasis = snap.StasisActive
                ? "⏸ STASIS ACTIVE"
                : $"🧊 Tokens: {snap.StasisTokens}";
 
            string timer = snap.StasisActive
                ? "[ PAUSED ]"
                : $"⏱ {snap.TickSecondsRemaining:F0}s";
 
            FullRow(1,
                $"  BIODOME ARES-1  ⚙ CONTROL PANEL" +
                $"          {timer,-12}  {stasis}",
                snap.StasisActive ? ConsoleColor.Cyan : ConsoleColor.White);
 
            FullRow(2,
                $"  LEVEL {snap.LevelNumber}: " +
                $"{snap.LevelName.ToUpper()}  —  " +
                $"Tick {snap.CurrentTick}/{snap.TotalTicks}  —  " +
                $"Power: {snap.TotalPowerUsed}/{snap.PowerBudget} ⚡",
                ConsoleColor.DarkGray);
 
            // Column header divider row — ╠═══╪═══╣
            DrawSplitDivider(3);
        }

         // ── Row 3: Column headers ─────────────────────────────
        private static void DrawColumnHeaders()
        {
            // Already drawn as part of DrawSplitDivider(3) in header
            // The actual section label rows start at 4
        }

        // ── Rows 4–11: Fans (left) | Shield (right) ──────────
        private static void DrawFansAndShield(GameSnapshot snap)
        {
            bool fansActive   = snap.ControlPanelFocus == 'F';
            bool shieldActive = snap.ControlPanelFocus == 'D';
 
            ConsoleColor fanHdr   = fansActive   ? ConsoleColor.Cyan : ConsoleColor.Yellow;
            ConsoleColor shieldHdr= shieldActive ? ConsoleColor.Cyan : ConsoleColor.Yellow;
 
            // Row 4 — section headers
            TwoColRow(4,
                "[F] VENTILATION FANS",
                "[D] SOLAR SHIELD / PANELS",
                Split, W, fanHdr, shieldHdr);
 
            // Row 5 — current setting + power
            TwoColRow(5,
                $"Current: {snap.Fans,-9} {snap.FanPowerCost}⚡",
                $"Current: {snap.Shield,-9} {snap.ShieldPowerCost}⚡",
                Split, W,
                fansActive   ? ConsoleColor.Cyan : ConsoleColor.White,
                shieldActive ? ConsoleColor.Cyan : ConsoleColor.White);
 
            // Row 6 — effect line 1
            TwoColRow(6,
                "Effect:  Temp↓↓",
                "Effect:  Temp↓  (humidity: none)",
                Split, W, ConsoleColor.Green, ConsoleColor.Green);
 
            // Row 7 — effect line 2 (extra space as requested)
            TwoColRow(7,
                "         Humidity↓↓",
                $"  {ShieldSettingBar(snap.Shield)}",
                Split, W, ConsoleColor.Green, ConsoleColor.DarkGray);
 
            // Row 8 — setting selector bar
            TwoColRow(8,
                FanSettingBar(snap.Fans),
                "",
                Split, W, ConsoleColor.White, ConsoleColor.White);
 
            // Row 9 — carets under current fan setting
            TwoColRow(9,
                FanCarets(snap.Fans),
                ShieldCarets(snap.Shield),
                Split, W, ConsoleColor.Cyan, ConsoleColor.Cyan);
 
            // Row 10 — instruction
            TwoColRow(10,
                fansActive
                    ? "▶ [+/-] adjust   [F] confirm"
                    : "  [F] select   [+/-] adjust",
                shieldActive
                    ? "▶ [+/-] adjust   [D] confirm"
                    : "  [D] select   [+/-] adjust",
                Split, W,
                fansActive   ? ConsoleColor.Cyan : ConsoleColor.DarkGray,
                shieldActive ? ConsoleColor.Cyan : ConsoleColor.DarkGray);
 
            // Row 11 — warning
            TwoColRow(11,
                "⚠ Max reduces filter life",
                "⚠ Full cuts solar generation",
                Split, W, ConsoleColor.DarkYellow, ConsoleColor.DarkYellow);
        }

        // ── Row 12: Mid divider ───────────────────────────────
        private static void DrawMidDivider()
        {
            DrawSplitDivider(12);
        }

        // ── Rows 13–20: Coolant (left) | Power (right) ───────
        private static void DrawCoolantAndPower(GameSnapshot snap)
        {
            bool coolantActive = snap.ControlPanelFocus == 'W';
            bool powerActive   = snap.ControlPanelFocus == 'P';
 
            ConsoleColor coolHdr  = coolantActive ? ConsoleColor.Cyan : ConsoleColor.Yellow;
            ConsoleColor powerHdr = powerActive   ? ConsoleColor.Cyan : ConsoleColor.Yellow;
 
            // Row 13 — section headers
            TwoColRow(13,
                "[W] WATER / COOLANT",
                "[P] POWER ALLOCATION",
                Split, W, coolHdr, powerHdr);
 
            // Row 14 — current + power cost
            TwoColRow(14,
                $"Current: {snap.Coolant,-9} {snap.CoolantPowerCost}⚡",
                $"Budget:  {snap.PowerBudget} ⚡ total",
                Split, W,
                coolantActive ? ConsoleColor.Cyan : ConsoleColor.White,
                powerActive   ? ConsoleColor.Cyan : ConsoleColor.White);
 
            // Row 15 — effect line 1
            TwoColRow(15,
                "Effect:  Temp↓↓↓",
                PowerBar(snap),
                Split, W, ConsoleColor.Green, PowerBarColour(snap));
 
            // Row 16 — effect line 2 (extra space as requested)
            TwoColRow(16,
                "         Humidity↑",
                $"Free:    {snap.PowerRemaining} ⚡",
                Split, W, ConsoleColor.DarkYellow,
                snap.PowerRemaining < 2
                    ? ConsoleColor.Red
                    : ConsoleColor.Green);
 
            // Row 17 — reserve bar / power auto-updates note
            string reserveBar = Bar(snap.CoolantReserve, 100, 14);
            ConsoleColor resCol = snap.CoolantReserve < 25
                ? ConsoleColor.Red
                : snap.CoolantReserve < 50
                    ? ConsoleColor.Yellow
                    : ConsoleColor.Cyan;
 
            TwoColRow(17,
                $"Reserve: {reserveBar} {snap.CoolantReserve:F0}%",
                "(auto-updates with controls)",
                Split, W, resCol, ConsoleColor.DarkGray);
 
            // Row 18 — setting selector bar
            TwoColRow(18,
                CoolantSettingBar(snap.Coolant),
                $"Warn at: {snap.PowerWarningThreshold} ⚡",
                Split, W, ConsoleColor.White,
                snap.TotalPowerUsed >= snap.PowerWarningThreshold
                    ? ConsoleColor.Red
                    : ConsoleColor.White);
 
            // Row 19 — carets + power warning adjust
            TwoColRow(19,
                CoolantCarets(snap.Coolant),
                "[+/-] adjust warning level",
                Split, W, ConsoleColor.Cyan, ConsoleColor.DarkGray);
 
            // Row 20 — instruction + warning
            TwoColRow(20,
                coolantActive
                    ? "▶ [+/-] adjust   [W] confirm"
                    : "  [W] select   [+/-] adjust",
                powerActive
                    ? "▶ [+/-] adjust   [P] confirm"
                    : "  [P] select   [+/-] adjust",
                Split, W,
                coolantActive ? ConsoleColor.Cyan : ConsoleColor.DarkGray,
                powerActive   ? ConsoleColor.Cyan : ConsoleColor.DarkGray);
 
            // Row 21 — warning
            TwoColRow(21,
                "⚠ Coolant reserve is finite",
                "",
                Split, W, ConsoleColor.DarkYellow, ConsoleColor.White);
 
            HRule(22, 'M');
        }

        // ── Rows 23–24: Event log ─────────────────────────────
        private static void DrawEventLog(GameSnapshot snap)
        {
            string eventLine = snap.EventName != null
                ? $"⚡ {snap.EventName} — {snap.EventFlavor}"
                : "  Systems nominal.";
 
            ConsoleColor eventCol = snap.EventName != null
                ? ConsoleColor.Yellow
                : ConsoleColor.DarkGray;
 
            FullRow(23, eventLine, eventCol);
 
            string actionLine = snap.LastActionText != null
                ? (snap.LastActionJustChanged
                    ? $"←★ {snap.LastActionText}"
                    : $"   {snap.LastActionText}")
                : "";
 
            ConsoleColor actionCol = snap.LastActionText?.StartsWith("⚠") == true
                ? ConsoleColor.Red
                : snap.LastActionJustChanged
                    ? ConsoleColor.Cyan
                    : ConsoleColor.DarkGray;
 
            FullRow(24, actionLine, actionCol);
 
            HRule(25, 'B');
        }

        // ── Row 26: Hotkey bar ────────────────────────────────
        private static void DrawHotkeyBar(GameSnapshot snap)
        {
            string focus = snap.ControlPanelFocus switch
            {
                'F' => "  Fans SELECTED — [+/-] adjust  [F] confirm  ",
                'D' => "  Shield SELECTED — [+/-] adjust  [D] confirm  ",
                'W' => "  Coolant SELECTED — [+/-] adjust  [W] confirm  ",
                'P' => "  Power Warning SELECTED — [+/-] adjust  [P] confirm  ",
                _   => "  [F]ans  [D]ome Shield  [W]ater  [S]tasis  [C] Main View  ",
            };
 
            Write(26, 0, focus.PadRight(TotalWidth), ConsoleColor.DarkGray);
        }

        // ════════════════════════════════════════════════════
        //  SETTING SELECTOR HELPERS
        // ════════════════════════════════════════════════════
 
        private static string FanSettingBar(FanSetting s)
            => $"Off─Low─Med─High─Max";
 
        private static string FanCarets(FanSetting s) => s switch
        {
            FanSetting.Off     => "^^^",
            FanSetting.Low     => "    ^^^",
            FanSetting.Medium  => "        ^^^",
            FanSetting.High    => "            ^^^^",
            FanSetting.Maximum => "                 ^^^",
            _                  => "",
        };

        private static string ShieldSettingBar(ShieldSetting s)
            => $"Retract─{(s == ShieldSetting.Partial ? "[Partial]" : "Partial")}─Full";
 
        private static string ShieldCarets(ShieldSetting s) => s switch
        {
            ShieldSetting.Retracted => "  Retract─Partial─Full",
            ShieldSetting.Partial   => "  Retract─^^^^^^^─Full",
            ShieldSetting.Full      => "  Retract─Partial─^^^^",
            _                       => "",
        };

        private static string CoolantSettingBar(CoolantSetting s)
            => $"Off─Misting─Active";
 
        private static string CoolantCarets(CoolantSetting s) => s switch
        {
            CoolantSetting.Off     => "^^^",
            CoolantSetting.Misting => "    ^^^^^^^",
            CoolantSetting.Active  => "            ^^^^^^",
            _                      => "",
        };

        // ── Power bar display ─────────────────────────────────
        private static string PowerBar(GameSnapshot snap)
        {
            string bar = Bar(snap.TotalPowerUsed, snap.PowerBudget, 14);
            return $"Used:    {bar} {snap.TotalPowerUsed}⚡";
        }
 
        private static ConsoleColor PowerBarColour(GameSnapshot snap)
        {
            if (snap.PowerOverBudget) return ConsoleColor.Red;
            if (snap.TotalPowerUsed >= snap.PowerWarningThreshold)
                return ConsoleColor.Yellow;
            return ConsoleColor.Green;
        }

        // ── Split divider row — unbroken │ at Split ──────────
        private static void DrawSplitDivider(int row)
        {
            // ╠════════════════════╪════════════════════════════╣
            char[] line = new char[W];
            line[0]     = '╠';
            line[W - 1] = '╣';
            for (int i = 1; i < W - 1; i++) line[i] = '═';
            line[Split] = '╪';
            Write(row, 0, new string(line), ConsoleColor.DarkCyan);
        }
    }
}