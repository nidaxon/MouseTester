using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace MousePollingTester
{
    public class MainForm : Form
    {
        // Import Raw Input API from Windows to get hardware-level polling rates
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        const int WM_INPUT = 0x00FF;
        
        // UI Elements
        private Label lblCurrent;
        private Label lblAverage;
        private Label lblMax;
        private Label lblInstructions;
        private Button btnReset;
        
        // Tracking Variables
        private Stopwatch stopwatch = new Stopwatch();
        private Queue<double> timestamps = new Queue<double>();
        private double lastEventTime = 0;
        private double maxHz = 0;
        private double lastCalculatedHz = 0;
        private double sumHz = 0;
        private int validSamples = 0;
        private Timer displayTimer;

        public MainForm()
        {
            // Build the Window
            this.Text = "Raw Input Mouse Polling Tester";
            this.Size = new Size(450, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            Font mainFont = new Font("Segoe UI", 20, FontStyle.Bold);
            Font subFont = new Font("Segoe UI", 12, FontStyle.Regular);

            lblInstructions = new Label { Text = "Move your mouse rapidly in circles", Top = 20, Left = 0, Width = 450, TextAlign = ContentAlignment.MiddleCenter, Font = subFont, ForeColor = Color.LightGray };
            lblCurrent = new Label { Text = "Current: 0 Hz", Top = 70, Left = 0, Width = 450, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = mainFont };
            lblAverage = new Label { Text = "Average: 0 Hz", Top = 125, Left = 0, Width = 450, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = mainFont, ForeColor = Color.DeepSkyBlue };
            lblMax = new Label { Text = "Maximum: 0 Hz", Top = 180, Left = 0, Width = 450, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = mainFont, ForeColor = Color.LimeGreen };
            
            btnReset = new Button { Text = "Reset", Top = 235, Left = 175, Width = 100, Height = 35, Font = subFont, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), Cursor = Cursors.Hand };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) => ResetStats();

            this.Controls.Add(lblInstructions);
            this.Controls.Add(lblCurrent);
            this.Controls.Add(lblAverage);
            this.Controls.Add(lblMax);
            this.Controls.Add(btnReset);

            // Register Mouse for Raw Input
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic Desktop Controls
            rid[0].usUsage = 0x02;     // Mouse
            rid[0].dwFlags = 0;        
            rid[0].hwndTarget = this.Handle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register Raw Input API.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            stopwatch.Start();
            
            // Timer to update the visual display smoothly (every 100ms)
            displayTimer = new Timer();
            displayTimer.Interval = 100; 
            displayTimer.Tick += DisplayTimer_Tick;
            displayTimer.Start();
        }

        private void ResetStats()
        {
            maxHz = 0;
            lastCalculatedHz = 0;
            sumHz = 0;
            validSamples = 0;
            timestamps.Clear();
            
            lblCurrent.Text = "Current: 0 Hz";
            lblAverage.Text = "Average: 0 Hz";
            lblMax.Text = "Maximum: 0 Hz";
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            double now = stopwatch.Elapsed.TotalSeconds;
            
            // If the mouse hasn't moved for 100ms, drop current Hz to 0
            if (now - lastEventTime > 0.1)
            {
                lastCalculatedHz = 0;
            }

            lblCurrent.Text = "Current: " + Math.Round(lastCalculatedHz) + " Hz";
            
            // Only update the average while the mouse is actively moving
            if (lastCalculatedHz > 0)
            {
                sumHz += lastCalculatedHz;
                validSamples++;
                double averageHz = sumHz / validSamples;
                lblAverage.Text = "Average: " + Math.Round(averageHz) + " Hz";
            }
            
            lblMax.Text = "Maximum: " + Math.Round(maxHz) + " Hz";
        }

        // Intercept Windows System Messages to catch Raw Input
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                double now = stopwatch.Elapsed.TotalSeconds;
                lastEventTime = now;
                timestamps.Enqueue(now);
                
                // Keep only mouse events from the last 50ms (rolling window)
                while (timestamps.Count > 0 && now - timestamps.Peek() > 0.05)
                {
                    timestamps.Dequeue();
                }

                // Need at least 3 events to calculate a reliable time delta
                if (timestamps.Count >= 3)
                {
                    double elapsed = now - timestamps.Peek();
                    if (elapsed > 0)
                    {
                        double currentHz = (timestamps.Count - 1) / elapsed;
                        
                        // Ignore system stutter anomalies (>15000Hz is physically impossible right now)
                        if (currentHz < 15000)
                        {
                            lastCalculatedHz = currentHz;
                            if (currentHz > maxHz) 
                            {
                                maxHz = currentHz;
                            }
                        }
                    }
                }
            }
            base.WndProc(ref m);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}