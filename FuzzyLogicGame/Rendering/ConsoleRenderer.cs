// ============================================================
//  ConsoleRenderer.cs
//  Implements IRenderer for the console.
//  Coordinates all drawing — delegates to MainView and
//  ControlPanelView for layout, owns all low-level
//  cursor-positioning and colour primitives.
//
//  Cursor-positioned rendering strategy:
//    - Console is cleared ONCE on Initialise()
//    - Every Render() call moves the cursor to specific
//      positions and overwrites only what changed
//    - No full Console.Clear() during gameplay — eliminates
//      flicker and gives a "live panel" feel
// ============================================================

using System;
using BiodomeAres1.Engine;
 
namespace BiodomeAres1.Rendering
{
    public class ConsoleRenderer : IRenderer
    {
        // ── Layout constants ─────────────────────────────────
        // All views share these so rows and columns stay aligned
        public const int TotalWidth      = 77;   // inner content width
        public const int TotalHeight     = 30;   // console window height
        public const int BorderLeft      = 0;    // outer left edge column
 
        // ── IRenderer ────────────────────────────────────────
        public void Initialise()
        {
            Console.CursorVisible  = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
 
            try
            {
                Console.SetWindowSize(Math.Max(Console.WindowWidth,  TotalWidth  + 2), Math.Max(Console.WindowHeight, TotalHeight + 4));
            }
            catch { /* Some terminals don't support resize — ignore */ }
 
            Console.Clear();
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void Render(GameSnapshot snapshot)
        {
            switch (snapshot.CurrentView)
            {
                case ViewState.MainMenu:
                    MainMenuView.Draw(snapshot);
                    break;
 
                case ViewState.SettingsMenu:
                    SettingsView.Draw(snapshot);
                    break;
 
                case ViewState.LevelIntro:
                    LevelIntroView.Draw(snapshot);
                    break;
 
                case ViewState.MainView:
                    MainView.Draw(snapshot);
                    break;
 
                case ViewState.ControlPanelView:
                    ControlPanelView.Draw(snapshot);
                    break;
 
                case ViewState.LevelComplete:
                    OutcomeView.DrawLevelComplete(snapshot);
                    break;
 
                case ViewState.GameOver:
                    OutcomeView.DrawGameOver(snapshot);
                    break;
            }
        }

        public void Teardown()
        {
            Console.SetCursorPosition(0, TotalHeight + 2);
            Console.CursorVisible  = true;
            Console.ResetColor();
            Console.WriteLine();
        }

        // ════════════════════════════════════════════════════
        //  SHARED DRAWING PRIMITIVES
        //  Static helpers used by all view classes.
        //  All coordinates are absolute console row/column.
        // ════════════════════════════════════════════════════
 
        /// <summary>
        /// Write text at an exact console position, then reset colour.
        /// Pads with spaces to 'width' to overwrite any previous content.
        /// </summary>
        public static void Write(int row, int col, string text, ConsoleColor fg = ConsoleColor.White, 
                                 ConsoleColor bg = ConsoleColor.Black, int padToWidth = 0)
        {
            Console.SetCursorPosition(col, row);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
 
            if (padToWidth > 0 && text.Length < padToWidth)
                text = text.PadRight(padToWidth);
 
            Console.Write(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Draw a full-width horizontal box border row.
        /// kind: 'T' = top, 'B' = bottom, 'M' = mid divider
        /// </summary>
        public static void HRule(int row, char kind, int col = 0, int width = TotalWidth, ConsoleColor fg = ConsoleColor.DarkCyan)
        {
            char left, fill, right;
            (left, fill, right) = kind switch
            {
                'T' => ('╔', '═', '╗'),
                'B' => ('╚', '═', '╝'),
                'M' => ('╠', '═', '╣'),
                _ =>   ('├', '─', '┤'),
            };
 
            string line = left + new string(fill, width - 2) + right;
 
            Write(row, col, line, fg);
        }

        /// <summary>
        /// Draw a mid-divider that has a vertical split at splitCol.
        /// Used for the two-column control panel divider rows.
        /// </summary>
        public static void HRuleSplit(int row, int splitCol, char leftJoin, char rightJoin, int col = 0, 
                                      int width = TotalWidth, ConsoleColor fg = ConsoleColor.DarkCyan)
        {
            char[] line = new char[width];
            line[0]         = '╠';
            line[width - 1] = '╣';
            for (int i = 1; i < width - 1; i++)
                line[i] = '═';
            line[splitCol - col] = '╦'; // will be overwritten per usage
            // Use the caller-supplied join characters
            line[0]              = leftJoin;
            line[splitCol - col] = '╪'; // cross joint
            line[width - 1]      = rightJoin;
 
            Write(row, col, new string(line), fg);
        }

        /// <summary>
        /// Draw one row of a two-column layout.
        /// Draws left border │, left content, centre │, right content, right │
        /// </summary>
        public static void TwoColRow(int row, string left, string right, int splitCol = 38, int totalWidth  = TotalWidth,
            ConsoleColor leftFg  = ConsoleColor.White, ConsoleColor rightFg = ConsoleColor.White)
        {
            int leftWidth  = splitCol - 3;       // inner left column width
            int rightWidth = totalWidth - splitCol - 2; // inner right column width
 
            // Truncate if over — avoids wrapping
            if (left.Length  > leftWidth)  left  = left[..leftWidth];
            if (right.Length > rightWidth) right = right[..rightWidth];
 
            string leftPad  = left.PadRight(leftWidth);
            string rightPad = right.PadRight(rightWidth);
 
            Write(row, 0,          "│ ", ConsoleColor.DarkCyan);
            Write(row, 2,          leftPad,  leftFg);
            Write(row, splitCol,   " │ ", ConsoleColor.DarkCyan);
            Write(row, splitCol+3, rightPad, rightFg);
            Write(row, totalWidth - 1, "│", ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// Draw a single-column full-width bordered row.
        /// </summary>
        public static void FullRow(int row, string content, ConsoleColor fg = ConsoleColor.White, int totalWidth  = TotalWidth)
        {
            int innerWidth = totalWidth - 4;
            if (content.Length > innerWidth)
                content = content[..innerWidth];
 
            string padded = content.PadRight(innerWidth);
            Write(row, 0,              "│ ",   ConsoleColor.DarkCyan);
            Write(row, 2,              padded, fg);
            Write(row, totalWidth - 2, " │",   ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// Draw an ASCII progress bar.
        /// Returns the rendered string — does not write to console.
        /// </summary>
        public static string Bar(double value, double max, int barWidth = 20, char filled = '█', char empty = '░')
        {
            int filledCount = (int)Math.Round((value / max) * barWidth);
            filledCount = Math.Clamp(filledCount, 0, barWidth);
            return "["
                + new string(filled, filledCount)
                + new string(empty,  barWidth - filledCount)
                + "]";
        }

        /// <summary>
        /// Colour for a meter value — green/yellow/red threshold.
        /// </summary>
        public static ConsoleColor MeterColour(double value)
        {
            if (value >= 60) return ConsoleColor.Green;
            if (value >= 30) return ConsoleColor.Yellow;
            return ConsoleColor.Red;
        }
 
        /// <summary>
        /// Trend arrow character.
        /// </summary>
        public static string TrendArrow(int trend) => trend switch
        {
            1  => "↑",
            -1 => "↓",
            _  => "→",
        };
 
        /// <summary>
        /// Trend colour.
        /// </summary>
        public static ConsoleColor TrendColour(int trend) => trend switch
        {
            1  => ConsoleColor.Green,
            -1 => ConsoleColor.Red,
            _  => ConsoleColor.DarkGray,
        };

         /// <summary>
        /// Clears a rectangle of the console to black/spaces.
        /// Used to wipe areas before redrawing.
        /// </summary>
        public static void ClearRegion(int startRow, int endRow,
            int startCol = 0, int width = TotalWidth)
        {
            string blank = new string(' ', width);
            for (int r = startRow; r <= endRow; r++)
            {
                Console.SetCursorPosition(startCol, r);
                Console.Write(blank);
            }
        }
    }
}