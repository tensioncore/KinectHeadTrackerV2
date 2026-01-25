using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;

namespace KinectHeadtracker
{
    public partial class MainForm : Form
    {
        private readonly object _frameLock = new object();
        private Image _currentFrame = null;
        private bool _suspendVideo = false;
        private Microsoft.Win32.PowerModeChangedEventHandler _powerModeHandler;

        // NOTE: v2.1 moves away from hardcoded transport config.
        // Keep defaults here only as fallback safety if UI/settings are missing.
        private const int DEFAULT_UDP_PORT = 5550;
        private const string DEFAULT_UDP_IP = "127.0.0.1";

        private KinectFaceTracker tracker;
        private DateTime lastDataReceived;

        private volatile bool _closing = false;

        // Engine state
        private bool _engineRunning = false;

        // UDP streaming state
        private bool _udpStreaming = false;

        // UI wiring / click guards
        private bool _uiWired = false;
        private bool _udpClickInProgress = false;
        private bool _engineClickInProgress = false;

        // ----------------------------
        // TSP / NetIsolate+ style theme
        // (UI polish only — no behavior changes)
        // ----------------------------
        private Color _pillBg = Color.FromArgb(28, 28, 28);
        private Color _pillBorder = Color.FromArgb(75, 75, 75);
        private Color _pillFg = Color.FromArgb(180, 180, 180);
        private readonly Color _inputBg = Color.FromArgb(24, 24, 24);
        private readonly Color _inputBorder = Color.FromArgb(70, 70, 70);
        private readonly Color _accent = Color.FromArgb(0, 170, 255);
        private readonly Color _headerBg = Color.FromArgb(18, 17, 16);
        private readonly Color _rootBg = Color.FromArgb(10, 9, 8);
        private readonly Color _panelBg = Color.FromArgb(16, 15, 14);
        private readonly Color _textHi = Color.FromArgb(232, 232, 232);
        private readonly Color _textLo = Color.FromArgb(190, 190, 190);
        private readonly Color _textDisabled = Color.FromArgb(120, 120, 120); // readable disabled text
        private readonly Color _btnBg = Color.FromArgb(40, 40, 40);
        private readonly Color _btnBorder = Color.FromArgb(70, 70, 70);
        private readonly Color _btnHover = Color.FromArgb(55, 55, 55);
        private readonly Color _btnDown = Color.FromArgb(30, 30, 30);
        private readonly ToolTip _startupHintTip = new ToolTip();
        // ----------------------------
        // Borderless / rounded window (Win32)
        // ----------------------------
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int HTCAPTION = 0x2;

        private const int WINDOW_RADIUS = 12; // tweak if you want tighter/rounder
        private readonly Color _cornerKey = Color.Fuchsia; // transparency key (never used in UI)

        // ----------------------------
        // v2.1 Settings (Phase 1 already added these)
        // ----------------------------
        // If your Phase 1 uses different field names, keep the logic the same and rename fields only.
        private AppSettings _settings;
        // ----------------------------
        // Startup options (v2.1 Final)
        // ----------------------------
        private bool _initializingUi = false;
        private DateTime _nextAutoEngineAttempt = DateTime.MinValue;
        private bool _autoStartEngineArmed = false;
        private bool _autoStartUdpArmed = false;
        public MainForm()
        {
            InitializeComponent();

            // v2.1 settings init (Phase 1)
            try
            {
                _settings = SettingsStore.Load();
            }
            catch
            {
                _settings = new AppSettings();
            }

            this.Resize += (s, e) =>
            {
                _suspendVideo = (this.WindowState == FormWindowState.Minimized);
            };

            this.VisibleChanged += (s, e) =>
            {
                if (!this.Visible) _suspendVideo = true;
            };

            _powerModeHandler = (s, e) =>
            {
                if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
                {
                    _suspendVideo = true;
                }
                else if (e.Mode == Microsoft.Win32.PowerModes.Resume)
                {
                    try
                    {
                        if (IsDisposed) return;
                        if (!IsHandleCreated) return;
                        BeginInvoke(new Action(() =>
                        {
                            ResumeLiveVideoAfterDelay(1500);
                        }));
                    }
                    catch { }
                }
            };
            Microsoft.Win32.SystemEvents.PowerModeChanged += _powerModeHandler;

            picLiveVideo.Paint += PicLiveVideo_Paint;
            picLiveVideo.Image = null;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;

            this.BackColor = _cornerKey;
            this.TransparencyKey = _cornerKey;
            this.DoubleBuffered = true;

            ApplyRoundedCorners();
            ApplyTspTheme();

            lastDataReceived = DateTime.Now;

            tracker = new KinectFaceTracker();
            tracker.Subscribe(Tracker_OnReceiveData);

            try { if (btnUdpToggle != null) btnUdpToggle.Visible = true; } catch { }

            WireUiOnce();
            WireHeaderDragging();
            // v2.1: apply settings to UI (Phase 1)
            _initializingUi = true;
            ApplySettingsToUi();
            _initializingUi = false;

            RefreshUiState();
        }
        // Formats numeric output cleanly:
        // - Up to 3 decimals (no trailing zeros)
        // - Removes "-0" / "-0.000" jitter
        private static string F3(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            if (Math.Abs(v) < 0.0005) v = 0; // kills "-0.000"
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }


        // Creates a fully-owned, fully-decoded bitmap copy that is safe to draw with GDI+.
        // IMPORTANT: tracker.GetImage() may return a reused/owned image. We must treat it as borrowed and never dispose it.
        private static Bitmap CreateOwnedFrame(Image src)
        {
            if (src == null) return null;

            try
            {
                if (src is Bitmap b)
                {
                    // Force a deep copy to a stable 32bpp format
                    var rect = new Rectangle(0, 0, b.Width, b.Height);
                    return b.Clone(rect, PixelFormat.Format32bppPArgb);
                }

                // For non-bitmap Images, materialize into a Bitmap first, then deep-clone.
                using (var tmp = new Bitmap(src))
                {
                    var rect = new Rectangle(0, 0, tmp.Width, tmp.Height);
                    return tmp.Clone(rect, PixelFormat.Format32bppPArgb);
                }
            }
            catch
            {
                return null;
            }
        }

        private void StyleDataValueLabel(Label lbl)
        {
            if (lbl == null) return;

            // “Smoother” than Courier/Consolas while still readable
            lbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);

            // Make the numbers line up nicely
            lbl.AutoSize = false;
            lbl.Width = 130;                   // adjust if you want tighter
            lbl.TextAlign = ContentAlignment.MiddleRight;
        }

        private void StyleTextBox(TextBox tb)
        {
            if (tb == null) return;

            tb.BackColor = _inputBg;
            tb.ForeColor = _textHi;
            tb.BorderStyle = BorderStyle.FixedSingle;

            // Keep readability when disabled
            tb.EnabledChanged -= Input_EnabledChanged;
            tb.EnabledChanged += Input_EnabledChanged;

            // Optional: no ugly blue selection
            // tb.HideSelection = false;
        }

        private void StyleNumericUpDown(NumericUpDown nud)
        {
            if (nud == null) return;

            nud.BackColor = _inputBg;
            nud.ForeColor = _textHi;

            // NumericUpDown is a composite control:
            // Controls[0] is the inner TextBox (where the value is typed)
            if (nud.Controls.Count > 0 && nud.Controls[0] is TextBox inner)
            {
                inner.BackColor = _inputBg;
                inner.ForeColor = _textHi;
                inner.BorderStyle = BorderStyle.None; // nud draws its own border-ish
            }

            nud.EnabledChanged -= Input_EnabledChanged;
            nud.EnabledChanged += Input_EnabledChanged;
        }

        private void Input_EnabledChanged(object sender, EventArgs e)
        {
            // Keep disabled inputs readable but clearly disabled
            try
            {
                if (sender is TextBox tb)
                {
                    tb.ForeColor = tb.Enabled ? _textHi : _textDisabled;
                    tb.BackColor = tb.Enabled ? _inputBg : Color.FromArgb(18, 18, 18);
                }
                else if (sender is NumericUpDown nud)
                {
                    nud.ForeColor = nud.Enabled ? _textHi : _textDisabled;
                    nud.BackColor = nud.Enabled ? _inputBg : Color.FromArgb(18, 18, 18);

                    if (nud.Controls.Count > 0 && nud.Controls[0] is TextBox inner)
                    {
                        inner.ForeColor = nud.Enabled ? _textHi : _textDisabled;
                        inner.BackColor = nud.Enabled ? _inputBg : Color.FromArgb(18, 18, 18);
                    }
                }
            }
            catch { }
        }

        private void ResumeLiveVideoAfterDelay(int delayMs)
        {
            try
            {
                if (_closing) return;

                _suspendVideo = true;
                ClearLiveImage();

                var t = new Timer();
                t.Interval = Math.Max(50, delayMs);
                t.Tick += (s2, e2) =>
                {
                    try { t.Stop(); } catch { }
                    try { t.Dispose(); } catch { }

                    if (_closing) return;
                    _suspendVideo = false;
                    try { picLiveVideo?.Invalidate(); } catch { }
                };
                t.Start();
            }
            catch
            {
                // If anything goes wrong here, fail open (don't wedge video forever)
                try { _suspendVideo = false; } catch { }
            }
        }
        private void PicLiveVideo_Paint(object sender, PaintEventArgs e)
{
    if (_closing) return;
    if (_suspendVideo) return;

    Bitmap frameCopy = null;

    try
    {
        lock (_frameLock)
        {
            if (_currentFrame == null) return;

            if (_currentFrame is Bitmap bmp)
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                frameCopy = bmp.Clone(rect, PixelFormat.Format32bppPArgb);
            }
            else
            {
                using (var tmp = new Bitmap(_currentFrame))
                {
                    var rect = new Rectangle(0, 0, tmp.Width, tmp.Height);
                    frameCopy = tmp.Clone(rect, PixelFormat.Format32bppPArgb);
                }
            }
        }

        if (frameCopy == null) return;

        e.Graphics.Clear(picLiveVideo.BackColor);
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        float boxW = picLiveVideo.Width;
        float boxH = picLiveVideo.Height;
        float imgW = frameCopy.Width;
        float imgH = frameCopy.Height;

        if (boxW <= 0 || boxH <= 0) return;
        if (imgW <= 0 || imgH <= 0) return;

        // aspect-fit ("Zoom" behavior)
        float scale = Math.Min(boxW / imgW, boxH / imgH);
        float drawW = imgW * scale;
        float drawH = imgH * scale;

        float x = (boxW - drawW) / 2f;
        float y = (boxH - drawH) / 2f;

        // If anything goes non-finite, bail (prevents rare GDI+ native crashes).
        if (float.IsNaN(drawW) || float.IsNaN(drawH) || float.IsInfinity(drawW) || float.IsInfinity(drawH)) return;
        if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y)) return;

        e.Graphics.DrawImage(frameCopy, x, y, drawW, drawH);
    }
    finally
    {
        try { frameCopy?.Dispose(); } catch { }
    }
}

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == WM_SYSCOMMAND)
                {
                    // Monitor power event (common when displays turn off/on without full system sleep)
                    if (((int)m.WParam & 0xFFF0) == SC_MONITORPOWER)
                    {
                        int state = m.LParam.ToInt32();

                        // state: -1 = on, 1 = low power, 2 = off
                        if (state == 1 || state == 2)
                        {
                            _suspendVideo = true;
                            try { ClearLiveImage(); } catch { }
                        }
                        else if (state == -1)
                        {
                            // Give the display stack a moment to settle before resuming GDI+ drawing
                            if (!_closing && cbxLiveVideo != null && cbxLiveVideo.Checked)
                                ResumeLiveVideoAfterDelay(250);
                        }
                    }
                }
            }
            catch { }

            base.WndProc(ref m);
        }




        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            try
            {
                int d = WINDOW_RADIUS * 2;
                IntPtr hrgn = CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, d, d);
                this.Region = Region.FromHrgn(hrgn);
                DeleteObject(hrgn);

                if (pnlHeader != null) pnlHeader.BackColor = _headerBg;
                if (pnlLeft != null) pnlLeft.BackColor = _panelBg;
                if (pnlRight != null) pnlRight.BackColor = _panelBg;
            }
            catch { }
        }

        private void WireHeaderDragging()
        {
            try
            {
                if (pnlHeader != null) pnlHeader.MouseDown += Header_MouseDown;
                if (lblTitle != null) lblTitle.MouseDown += Header_MouseDown;
                if (picLogo != null) picLogo.MouseDown += Header_MouseDown;
                if (lblStatusPill != null) lblStatusPill.MouseDown += Header_MouseDown;
            }
            catch { }
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            try
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
            catch { }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Settings load already happened in constructor.
            // Arm auto-start options at startup.
            try
            {
                _autoStartEngineArmed = (_settings != null && _settings.AutoStartEngineEnabled);
                _autoStartUdpArmed = (_settings != null && _settings.AutoStartStreamingEnabled);
            }
            catch { }
            // Ensure Run-at-Startup task matches the saved setting
            try { SyncRunAtStartup(false); } catch { }
            // If the user wants auto-start tracking, attempt immediately (and keep retrying on timer tick)
            if (_autoStartEngineArmed)
            {
                _nextAutoEngineAttempt = DateTime.MinValue;
                TryAutoStartIfNeeded();
            }
            // Keep UI state consistent
            RefreshUiState();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            _suspendVideo = true;

            try { timer1.Stop(); } catch { }
            try { timer2.Stop(); } catch { }
            try { timer1.Enabled = false; } catch { }
            try { timer2.Enabled = false; } catch { }

            try { if (picLiveVideo != null) picLiveVideo.Paint -= PicLiveVideo_Paint; } catch { }

            try
            {
                if (_powerModeHandler != null)
                    Microsoft.Win32.SystemEvents.PowerModeChanged -= _powerModeHandler;
            }
            catch { }

            // v2.1: capture latest UI values before saving
            try
            {
                PullUiToSettings();
                SettingsStore.Save(_settings);
            }
            catch { }

            try { tracker.Unsubscribe(Tracker_OnReceiveData); } catch { }
            try { StopEngine(); } catch { }

            try { ClearLiveImage(); } catch { }
            try { if (picLiveVideo != null) picLiveVideo.Image = null; } catch { }
        }

        // ----------------------------
        // Phase 1 Settings glue (minimal)
        // ----------------------------
        private void ApplySettingsToUi()
        {
            try
            {
                if (_settings == null) _settings = new AppSettings();

                // UDP
                try
                {
                    if (txtTargetIp != null)
                        txtTargetIp.Text = string.IsNullOrWhiteSpace(_settings.UdpTargetIp) ? DEFAULT_UDP_IP : _settings.UdpTargetIp;
                }
                catch { }

                try
                {
                    if (numUdpPort != null)
                    {
                        int p = _settings.UdpPort;
                        if (p <= 0) p = DEFAULT_UDP_PORT;
                        if (p < (int)numUdpPort.Minimum) p = (int)numUdpPort.Minimum;
                        if (p > (int)numUdpPort.Maximum) p = (int)numUdpPort.Maximum;
                        numUdpPort.Value = p;
                    }
                }
                catch { }

                // Live video
                try
                {
                    if (cbxLiveVideo != null)
                        cbxLiveVideo.Checked = _settings.LiveVideoEnabled;
                }
                catch { }

                // Window position (if you’re persisting this already in Phase 1)
                try
                {
                    if (_settings.WindowX != 0 || _settings.WindowY != 0)
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(_settings.WindowX, _settings.WindowY);
                    }
                }
                catch { }

                // Startup section checkboxes exist but behavior wiring is Phase 4; still persist visuals
                try { if (cbxRunAtStartup != null) cbxRunAtStartup.Checked = _settings.RunAtStartupEnabled; } catch { }
                try { if (cbxAutoStartEngine != null) cbxAutoStartEngine.Checked = _settings.AutoStartEngineEnabled; } catch { }
                try { if (cbxAutoStartUdp != null) cbxAutoStartUdp.Checked = _settings.AutoStartStreamingEnabled; } catch { }
            }
            catch { }
        }

        private void PullUiToSettings()
        {
            if (_settings == null) _settings = new AppSettings();

            // UDP
            try
            {
                if (txtTargetIp != null)
                    _settings.UdpTargetIp = (txtTargetIp.Text ?? "").Trim();
            }
            catch { }

            try
            {
                if (numUdpPort != null)
                    _settings.UdpPort = (int)numUdpPort.Value;
            }
            catch { }

            // Live video
            try
            {
                if (cbxLiveVideo != null)
                    _settings.LiveVideoEnabled = cbxLiveVideo.Checked;
            }
            catch { }

            // Window position
            try
            {
                _settings.WindowX = this.Location.X;
                _settings.WindowY = this.Location.Y;
            }
            catch { }

            // Startup checkboxes (behavior later; persist now)
            try { if (cbxRunAtStartup != null) _settings.RunAtStartupEnabled = cbxRunAtStartup.Checked; } catch { }
            try { if (cbxAutoStartEngine != null) _settings.AutoStartEngineEnabled = cbxAutoStartEngine.Checked; } catch { }
            try { if (cbxAutoStartUdp != null) _settings.AutoStartStreamingEnabled = cbxAutoStartUdp.Checked; } catch { }
        }

        private void SaveSettingsBestEffort()
{
    // Prevent accidental overwrites while we are applying settings to the UI.
    if (_closing) return;
    if (_initializingUi) return;

    try
    {
        PullUiToSettings();
        SettingsStore.Save(_settings);
    }
    catch { }
}
        // ----------------------------
        // UI wiring (single source)
        // ----------------------------
        private void WireUiOnce()
        {
            if (_uiWired) return;
            _uiWired = true;

            try
            {
                if (btnStartStop != null)
                {
                    btnStartStop.Click -= BtnStartStop_Click;
                    btnStartStop.Click += BtnStartStop_Click;
                }
            }
            catch { }

            try
            {
                if (btnUdpToggle != null)
                {
                    btnUdpToggle.Click -= BtnUdpToggle_Click;
                    btnUdpToggle.Click += BtnUdpToggle_Click;
                }
            }
            catch { }

            try
            {
                if (btnClose != null)
                {
                    btnClose.Click -= BtnClose_Click;
                    btnClose.Click += BtnClose_Click;
                }
            }
            catch { }

            try
            {
                if (btnMin != null)
                {
                    btnMin.Click -= BtnMin_Click;
                    btnMin.Click += BtnMin_Click;
                }
            }
            catch { }

            // NEW: save UDP settings when user edits them (no behavior change mid-stream)
            try
            {
                if (txtTargetIp != null)
                {
                    txtTargetIp.Leave -= TxtUdpTargetIp_Leave;
                    txtTargetIp.Leave += TxtUdpTargetIp_Leave;
                }
            }
            catch { }

            try
            {
                if (numUdpPort != null)
                {
                    numUdpPort.ValueChanged -= NumUdpPort_ValueChanged;
                    numUdpPort.ValueChanged += NumUdpPort_ValueChanged;
                }
            }
            catch { }

            // v2.1 Final: settings toggles
            try
            {
                if (cbxRunAtStartup != null)
                {
                    cbxRunAtStartup.CheckedChanged -= CbxRunAtStartup_CheckedChanged;
                    cbxRunAtStartup.CheckedChanged += CbxRunAtStartup_CheckedChanged;
                }
            }
            catch { }

            try
            {
                if (cbxAutoStartEngine != null)
                {
                    cbxAutoStartEngine.CheckedChanged -= CbxAutoStartEngine_CheckedChanged;
                    cbxAutoStartEngine.CheckedChanged += CbxAutoStartEngine_CheckedChanged;
                }
            }
            catch { }

            try
            {
                if (cbxAutoStartUdp != null)
                {
                    cbxAutoStartUdp.CheckedChanged -= CbxAutoStartUdp_CheckedChanged;
                    cbxAutoStartUdp.CheckedChanged += CbxAutoStartUdp_CheckedChanged;
                }
            }
            catch { }
        }
        private void TxtUdpTargetIp_Leave(object sender, EventArgs e)
        {
            SaveSettingsBestEffort();
        }

        private void NumUdpPort_ValueChanged(object sender, EventArgs e)
        {
            SaveSettingsBestEffort();
        }
        private void CbxRunAtStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializingUi) return;
            SaveSettingsBestEffort();
            try { SyncRunAtStartup(true); } catch { }
        }

        private void CbxAutoStartEngine_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializingUi) return;

            SaveSettingsBestEffort();

            _autoStartEngineArmed = (cbxAutoStartEngine != null && cbxAutoStartEngine.Checked);

            if (_autoStartEngineArmed)
            {
                _nextAutoEngineAttempt = DateTime.MinValue;
                TryAutoStartIfNeeded();
            }
        }

        private void CbxAutoStartUdp_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializingUi) return;

            SaveSettingsBestEffort();

            _autoStartUdpArmed = (cbxAutoStartUdp != null && cbxAutoStartUdp.Checked);

            // If we're already receiving data, we can start immediately. Otherwise, next frame will trigger it.
            if (_autoStartUdpArmed)
            {
                TryAutoStartUdpFromData();
            }
        }
        private void ShowStartupHintOnly()
        {
            if (_closing) return;

            try
            {
                if (cbxRunAtStartup != null)
                {
                    _startupHintTip.Show(
                        "Could not update startup task. Try running as Administrator.",
                        cbxRunAtStartup,
                        cbxRunAtStartup.Width / 2,
                        cbxRunAtStartup.Height / 2,
                        2500
                    );
                }
            }
            catch { }
        }
        private void ShowStartupHintAndRevert(bool desiredStateThatFailed)
        {
            if (_closing) return;

            // non-modal one-line hint
            try
            {
                if (cbxRunAtStartup != null)
                {
                    _startupHintTip.Show(
                        "Could not update startup task. Try running as Administrator.",
                        cbxRunAtStartup,
                        cbxRunAtStartup.Width / 2,
                        cbxRunAtStartup.Height / 2,
                        4000
                    );
                }
            }
            catch { }

            // Revert checkbox + persisted setting
            try
            {
                if (_settings == null) return;

                bool revertTo = !desiredStateThatFailed;

                _initializingUi = true;
                try
                {
                    if (cbxRunAtStartup != null)
                        cbxRunAtStartup.Checked = revertTo;

                    _settings.RunAtStartupEnabled = revertTo;
                    SettingsStore.Save(_settings);
                }
                finally
                {
                    _initializingUi = false;
                }
            }
            catch { }
        }
        private void SyncRunAtStartup(bool revertOnFailure)
        {
            try
            {
                if (_settings == null) return;

                var exePath = Application.ExecutablePath;
                bool desired = _settings.RunAtStartupEnabled;

                bool ok = desired
                    ? StartupManager.Ensure(exePath)
                    : StartupManager.Remove();

                if (!ok)
                {
                    if (revertOnFailure) ShowStartupHintAndRevert(desired);
                    else ShowStartupHintOnly();
                }
            }
            catch
            {
                try
                {
                    if (revertOnFailure)
                    {
                        bool desired = (_settings != null && _settings.RunAtStartupEnabled);
                        ShowStartupHintAndRevert(desired);
                    }
                    else
                    {
                        ShowStartupHintOnly();
                    }
                }
                catch { }
            }
        }
        private void TryAutoStartIfNeeded()
        {
            try
            {
                if (_closing) return;
                if (_settings == null) return;

                if (!_engineRunning && _autoStartEngineArmed && _settings.AutoStartEngineEnabled)
                {
                    if (DateTime.Now >= _nextAutoEngineAttempt)
                    {
                        _nextAutoEngineAttempt = DateTime.Now.AddSeconds(2);
                        StartEngine();
                    }
                }
            }
            catch { }
        }

        private void TryAutoStartUdpFromData()
        {
            try
            {
                if (_closing) return;
                if (_settings == null) return;
                if (!_engineRunning) return;
                if (_udpStreaming) return;

                if (!_autoStartUdpArmed) return;
                if (!_settings.AutoStartStreamingEnabled) return;

                // Only auto-start when we have recent tracking frames (Kinect actually alive)
                if ((DateTime.Now - lastDataReceived).TotalSeconds > 2) return;

                StartUdpStream();
                _autoStartUdpArmed = false; // one-shot unless user re-arms
            }
            catch { }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            try { this.Close(); } catch { }
        }

        private void BtnMin_Click(object sender, EventArgs e)
        {
            try { this.WindowState = FormWindowState.Minimized; } catch { }
        }

        // ----------------------------
        // Engine lifecycle
        // ----------------------------
        private void StartEngine()
        {
            if (_closing) return;
            if (_engineRunning) return;

            try
            {
                _engineRunning = tracker.StartWithCallback();
            }
            catch
            {
                _engineRunning = false;
            }

            RefreshUiState();
        }

        private void StopEngine()
        {
            try { StopUdpStream(); } catch { }

            try { tracker.Stop(); } catch { }

            _engineRunning = false;

            RefreshUiState();
        }

        // ----------------------------
        // UDP streaming toggle (v2.1: uses latched UI/settings)
        // ----------------------------
        private void StartUdpStream()
        {
            if (_closing) return;
            if (!_engineRunning) return;

            // Latch settings at stream start (no hot-swap mid-stream)
            string ip = DEFAULT_UDP_IP;
            int port = DEFAULT_UDP_PORT;

            try
            {
                if (txtTargetIp != null)
                {
                    var t = (txtTargetIp.Text ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(t)) ip = t;
                }
            }
            catch { }

            try
            {
                if (numUdpPort != null)
                    port = (int)numUdpPort.Value;
            }
            catch { }

            // Persist chosen values (Phase 2 expects these saved even if stream fails)
            try
            {
                if (_settings != null)
                {
                    _settings.UdpTargetIp = ip;
                    _settings.UdpPort = port;
                    SettingsStore.Save(_settings);
                }
            }
            catch { }

            try
            {
                // IMPORTANT: requires C++/CLI wrapper overload StartStreaming(int port, string ip)
                // If wrapper isn't patched yet, this will not compile — patch is trivial and we’ll do it next.
                if (tracker.StartStreaming(port, ip))
                    _udpStreaming = true;
                else
                    _udpStreaming = false;
            }
            catch
            {
                _udpStreaming = false;
            }

            RefreshUiState();
        }

        private void StopUdpStream()
        {
            try { tracker.StopStreaming(); } catch { }
            _udpStreaming = false;
            RefreshUiState();
        }

        // ----------------------------
        // Button handlers (guarded)
        // ----------------------------
        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_engineClickInProgress) return;
            _engineClickInProgress = true;

            try
            {
                if (_engineRunning)
                {
                    // User explicitly stopped the engine: disarm auto-start so we don't fight them.
                    _autoStartEngineArmed = false;
                    _autoStartUdpArmed = false;

                    StopEngine();
                }
                else
                {
                    StartEngine();
                }
            }
            finally
            {
                _engineClickInProgress = false;
                RefreshUiState();
            }
        }

        private void BtnUdpToggle_Click(object sender, EventArgs e)
        {
            if (_udpClickInProgress) return;
            _udpClickInProgress = true;

            try
            {
                // User explicitly toggled UDP: disarm auto-start so we don't fight them.
                _autoStartUdpArmed = false;

                if (_udpStreaming) StopUdpStream();
                else StartUdpStream();
            }
            finally
            {
                _udpClickInProgress = false;
                RefreshUiState();
            }
        }
        // ----------------------------
        // Existing controls
        // ----------------------------
        private void BtnCameraUp_Click(object sender, EventArgs e)
        {
            try { tracker?.TiltCamera(5); } catch { }
        }

        private void BtnCameraDown_Click(object sender, EventArgs e)
        {
            try { tracker?.TiltCamera(-5); } catch { }
        }

        private void cbxLiveVideo_CheckedChanged(object sender, EventArgs e)
        {
            timer2.Enabled = cbxLiveVideo.Checked;
            if (!cbxLiveVideo.Checked) ClearLiveImage();

            // Persist UI setting
            SaveSettingsBestEffort();
        }

        private void timer2_Tick(object sender, EventArgs e)
{
    if (_closing) return;

    if (!cbxLiveVideo.Checked || _suspendVideo)
    {
        ClearLiveImage();
        return;
    }

    Image borrowed = null;
    Bitmap owned = null;

    try
    {
        borrowed = tracker?.GetImage();
        if (borrowed == null) return;

        // Make our own safe copy. Do NOT dispose 'borrowed' (tracker may reuse it).
        owned = CreateOwnedFrame(borrowed);
        if (owned == null) return;

        Image old = null;
        lock (_frameLock)
        {
            old = _currentFrame;
            _currentFrame = owned; // now owned by us
        }

        // Dispose the previous owned frame (safe; it's ours).
        try { old?.Dispose(); } catch { }

        try { picLiveVideo.Invalidate(); } catch { }
    }
    catch
    {
        // If anything fails, dispose the owned copy we made.
        try { owned?.Dispose(); } catch { }
    }
}

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (_closing) return;

            TryAutoStartIfNeeded();
            UpdateStatusPill();
        }

        private void Tracker_OnReceiveData(double x, double y, double z, double yaw, double pitch, double roll)
        {
            if (_closing) return;
            if (IsDisposed) return;
            if (!IsHandleCreated) return;

            lastDataReceived = DateTime.Now;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (_closing || IsDisposed) return;

                    lblX.Text = F3(x);
                    lblY.Text = F3(y);
                    lblZ.Text = F3(z);
                    lblYaw.Text = F3(yaw);
                    lblPitch.Text = F3(pitch);
                    lblRoll.Text = F3(roll);

                    UpdateStatusPill();
                    TryAutoStartUdpFromData();
                }));
            }
            catch { }
        }

        private void ClearLiveImage()
        {
            try
            {
                Image old = null;

                lock (_frameLock)
                {
                    old = _currentFrame;
                    _currentFrame = null;

                    if (picLiveVideo != null) picLiveVideo.Image = null;
                }

                try { old?.Dispose(); } catch { }

                if (!_closing)
                {
                    try { picLiveVideo?.Invalidate(); } catch { }
                }
            }
            catch { }
        }

        // ----------------------------
        // UI polish: apply NetIsolate+ inspired styling safely
        // ----------------------------
        private void ApplyTspTheme()
        {
            try
            {
                this.DoubleBuffered = true;

                this.BackColor = _rootBg;
                this.ForeColor = _textLo;

                if (pnlHeader != null)
                {
                    pnlHeader.BackColor = _headerBg;
                    pnlHeader.Paint -= pnlHeader_Paint;
                    pnlHeader.Paint += pnlHeader_Paint;
                }

                if (lblTitle != null)
                {
                    lblTitle.ForeColor = _textHi;
                    lblTitle.Font = new Font("Segoe UI", 22f, FontStyle.Bold, GraphicsUnit.Point);
                }

                if (lblStatusPill != null)
                {
                    lblStatusPill.BackColor = Color.Transparent;
                    lblStatusPill.BorderStyle = BorderStyle.None;
                    lblStatusPill.Font = new Font("Courier New", 9f, FontStyle.Regular, GraphicsUnit.Point);
                    lblStatusPill.Paint -= lblStatusPill_Paint;
                    lblStatusPill.Paint += lblStatusPill_Paint;
                    lblStatusPill.Resize -= lblStatusPill_Resize;
                    lblStatusPill.Resize += lblStatusPill_Resize;
                }

                if (pnlLeft != null) pnlLeft.BackColor = _panelBg;
                if (pnlRight != null) pnlRight.BackColor = _panelBg;
                
                // Raw data labels: aligned + smooth font
                StyleDataValueLabel(lblX);
                StyleDataValueLabel(lblY);
                StyleDataValueLabel(lblZ);
                StyleDataValueLabel(lblYaw);
                StyleDataValueLabel(lblPitch);
                StyleDataValueLabel(lblRoll);


                StyleButton(btnCameraUp, isPrimary: false);
                StyleButton(btnCameraDown, isPrimary: false);
                StyleButton(btnStartStop, isPrimary: true);
                StyleButton(btnUdpToggle, isPrimary: false);

                StyleSectionHeader(lblUdpHeader);
                StyleSectionHeader(lblStartupHeader);

                StyleCheckbox(cbxRunAtStartup);
                StyleCheckbox(cbxAutoStartEngine);
                StyleCheckbox(cbxAutoStartUdp);
                StyleCheckbox(cbxLiveVideo);
                StyleTextBox(txtTargetIp);
                StyleNumericUpDown(numUdpPort);

                btnStartStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(100,100,100);

                try
                {
                    if (btnClose != null)
                    {
                        btnClose.FlatStyle = FlatStyle.Flat;
                        btnClose.FlatAppearance.BorderSize = 0;
                        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 20, 20);
                        btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(55, 25, 25);
                        btnClose.BackColor = Color.Transparent;
                        btnClose.ForeColor = Color.FromArgb(220, 220, 220);
                        btnClose.Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
                    }
                }
                catch { }

                try
                {
                    if (btnMin != null)
                    {
                        btnMin.FlatStyle = FlatStyle.Flat;
                        btnMin.FlatAppearance.BorderSize = 0;
                        btnMin.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 28, 28);
                        btnMin.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 20, 20);
                        btnMin.BackColor = Color.Transparent;
                        btnMin.ForeColor = Color.FromArgb(220, 220, 220);
                        btnMin.Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
                        btnMin.TextAlign = ContentAlignment.MiddleCenter;
                    }
                }
                catch { }

                if (picLiveVideo != null)
                {
                    picLiveVideo.BackColor = Color.FromArgb(20, 19, 18);
                }
            }
            catch
            {
                // UI-only; never allow theme application to affect app behavior
            }
        }

        private void StyleSectionHeader(Label lbl)
        {
            if (lbl == null) return;

            try
            {
                lbl.ForeColor = _textHi;
                lbl.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
            }
            catch { }
        }

        private void StyleCheckbox(CheckBox cb)
        {
            if (cb == null) return;

            try
            {
                cb.ForeColor = cb.Enabled ? _textLo : _textDisabled;
                cb.BackColor = Color.Transparent;

                cb.EnabledChanged -= Checkbox_EnabledChanged;
                cb.EnabledChanged += Checkbox_EnabledChanged;
            }
            catch { }
        }

        private void Checkbox_EnabledChanged(object sender, EventArgs e)
        {
            try
            {
                var cb = sender as CheckBox;
                if (cb == null) return;
                cb.ForeColor = cb.Enabled ? _textLo : _textDisabled;
            }
            catch { }
        }
        private void StyleButton(Button b, bool isPrimary)
        {
            if (b == null) return;

            b.BackColor = _btnBg;
            b.ForeColor = _textHi;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = _btnBorder;
            b.FlatAppearance.BorderSize = 1;

            b.FlatAppearance.MouseOverBackColor = _btnHover;
            b.FlatAppearance.MouseDownBackColor = _btnDown;

            if (isPrimary)
                b.FlatAppearance.BorderColor = Color.FromArgb(95, 95, 95);
        }

        private void lblStatusPill_Resize(object sender, EventArgs e)
        {
            try { lblStatusPill?.Invalidate(); } catch { }
        }

        private void pnlHeader_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var pen = new Pen(Color.FromArgb(35, 35, 35), 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }

                using (var accent = new SolidBrush(Color.FromArgb(50, _accent)))
                {
                    e.Graphics.FillRectangle(accent, 0, pnlHeader.Height - 3, 120, 3);
                }
            }
            catch { }
        }

        private void lblStatusPill_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                var r = new Rectangle(0, 0, lblStatusPill.Width - 1, lblStatusPill.Height - 1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int radius = 10;
                using (var path = RoundedRect(r, radius))
                using (var bg = new SolidBrush(_pillBg))
                using (var border = new Pen(_pillBorder, 1))
                {
                    e.Graphics.FillPath(bg, path);
                    e.Graphics.DrawPath(border, path);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    lblStatusPill.Text ?? "",
                    lblStatusPill.Font,
                    r,
                    _pillFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            catch { }
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ----------------------------
        // UI helpers (single source)
        // ----------------------------
        private void RefreshUiState()
        {
            UpdateEngineButton();
            UpdateUdpButton();
            UpdateStatusPill();

            try
            {
                if (cbxRunAtStartup != null) cbxRunAtStartup.ForeColor = cbxRunAtStartup.Enabled ? _textLo : _textDisabled;
                if (cbxAutoStartEngine != null) cbxAutoStartEngine.ForeColor = cbxAutoStartEngine.Enabled ? _textLo : _textDisabled;
                if (cbxAutoStartUdp != null) cbxAutoStartUdp.ForeColor = cbxAutoStartUdp.Enabled ? _textLo : _textDisabled;
                if (cbxLiveVideo != null) cbxLiveVideo.ForeColor = cbxLiveVideo.Enabled ? _textLo : _textDisabled;
            }
            catch { }
        }

        private void UpdateEngineButton()
        {
            try
            {
                if (btnStartStop == null) return;
                btnStartStop.Text = _engineRunning ? "Stop Head Tracking" : "Start Head Tracking";
            }
            catch { }
        }

        private void UpdateUdpButton()
        {
            try
            {
                if (btnUdpToggle == null) return;

                bool canUse = _engineRunning && !_udpClickInProgress;

                btnUdpToggle.Enabled = canUse;
                btnUdpToggle.Text = _udpStreaming ? "Stop UDP Stream" : "Start UDP Stream";
            }
            catch { }
        }

        private void UpdateStatusPill()
        {
            try
            {
                if (lblStatusPill == null) return;

                if (!_engineRunning)
                {
                    // If there's no Kinect attached, show a clearer state than "Stopped".
                    bool kinectConnected = true;
                    try
                    {
                        if (tracker != null)
                            kinectConnected = tracker.IsKinectConnected();
                    }
                    catch
                    {
                        kinectConnected = true; // fail open: don't claim disconnected on unexpected errors
                    }

                    if (!kinectConnected)
                    {
                        lblStatusPill.Text = "DISCONNECTED";
                        _pillFg = Color.FromArgb(230, 150, 150);
                        _pillBg = Color.FromArgb(32, 22, 22);
                        _pillBorder = Color.FromArgb(110, 70, 70);
                    }
                    else
                    {
                        lblStatusPill.Text = "● STOPPED";
                        _pillFg = Color.FromArgb(180, 180, 180);
                        _pillBg = Color.FromArgb(28, 28, 28);
                        _pillBorder = Color.FromArgb(75, 75, 75);
                    }
                    lblStatusPill.Invalidate();
                    return;
                }

                bool isWaiting = ((DateTime.Now - lastDataReceived).TotalSeconds > 5);

                if (isWaiting)
                {
                    lblStatusPill.Text = "● WAITING";
                    _pillFg = Color.FromArgb(230, 205, 140);
                    _pillBg = Color.FromArgb(32, 30, 22);
                    _pillBorder = Color.FromArgb(110, 95, 60);
                }
                else
                {
                    lblStatusPill.Text = "● RUNNING";
                    _pillFg = Color.FromArgb(150, 230, 150);
                    _pillBg = Color.FromArgb(22, 32, 22);
                    _pillBorder = Color.FromArgb(70, 110, 70);
                }

                lblStatusPill.Invalidate();
            }
            catch { }
        }
    }
}