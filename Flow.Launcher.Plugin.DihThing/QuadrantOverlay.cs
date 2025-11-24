using System;
using System.Drawing;
using System.Windows.Forms;

namespace Flow.Launcher.Plugin.DihThing
{
    /// <summary>
    /// Displays a temporary overlay to highlight one or more screen areas.
    /// </summary>
    public class QuadrantOverlay : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int RGN_OR = 2;

        public QuadrantOverlay(System.Collections.Generic.IEnumerable<Rectangle> rects)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Red;
            Opacity = 0.3; // Increased opacity slightly for better visibility of small text
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            // Calculate the union of all rectangles to set the form bounds
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool hasRects = false;

            // We need to store rects to process them for region creation relative to the form
            var rectList = new System.Collections.Generic.List<Rectangle>();

            foreach (var rect in rects)
            {
                hasRects = true;
                if (rect.X < minX) minX = rect.X;
                if (rect.Y < minY) minY = rect.Y;
                if (rect.Right > maxX) maxX = rect.Right;
                if (rect.Bottom > maxY) maxY = rect.Bottom;
                rectList.Add(rect);
            }

            if (hasRects)
            {
                // Set the form's bounds to cover all areas
                Bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);

                // Create the initial empty region
                IntPtr hRgnCombined = CreateRectRgn(0, 0, 0, 0);

                foreach (var rect in rectList)
                {
                    // Create a region for this rectangle, relative to the form's top-left
                    // The form is at (minX, minY), so we subtract that from the rect's coordinates
                    IntPtr hRgnRect = CreateRectRgn(
                        rect.X - minX,
                        rect.Y - minY,
                        rect.Right - minX,
                        rect.Bottom - minY
                    );

                    // Combine it with the accumulated region
                    CombineRgn(hRgnCombined, hRgnCombined, hRgnRect, RGN_OR);

                    // Clean up the temporary rect region
                    DeleteObject(hRgnRect);
                }

                // Set the window region
                // SetWindowRgn takes ownership of the region handle, so we don't delete hRgnCombined
                SetWindowRgn(Handle, hRgnCombined, true);
            }
            else
            {
                // Fallback if no rects provided
                Bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            }
        }

        public QuadrantOverlay(Rectangle bounds) : this(new[] { bounds })
        {
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000; // WS_EX_LAYERED
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        /// <summary>
        /// Shows the overlay for a specified duration.
        /// </summary>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void ShowTemporarily(int durationMs)
        {
            Show();
            var timer = new System.Windows.Forms.Timer { Interval = durationMs };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Close();
                Dispose();
            };
            timer.Start();
        }
    }
}
