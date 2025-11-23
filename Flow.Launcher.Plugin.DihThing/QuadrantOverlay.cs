using System;
using System.Drawing;
using System.Windows.Forms;

namespace Flow.Launcher.Plugin.DihThing
{
    /// <summary>
    /// Displays a temporary overlay to highlight a screen quadrant.
    /// </summary>
    public class QuadrantOverlay : Form
    {
        public QuadrantOverlay(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Red;
            Opacity = 0.1;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
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
