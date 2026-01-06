namespace KinectHeadtracker
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnCameraUp = new System.Windows.Forms.Button();
            this.btnCameraDown = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.picLiveVideo = new System.Windows.Forms.PictureBox();
            this.cbxLiveVideo = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblX = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblZ = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblYaw = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lblPitch = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.lblRoll = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);

            // NEW: layout panels + status + start/stop + UDP toggle
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblStatusPill = new System.Windows.Forms.Label();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.btnUdpToggle = new System.Windows.Forms.Button();
            // NEW: right panel sub-layout containers (UI only)
            this.pnlUdpSettings = new System.Windows.Forms.Panel();
            this.pnlStartupSettings = new System.Windows.Forms.Panel();

            // NEW: custom close button (borderless window)
            this.btnClose = new System.Windows.Forms.Button();

            // NEW: custom minimize button (borderless window)
            this.btnMin = new System.Windows.Forms.Button();

            // v2.1: UDP config controls (left panel)
            this.lblUdpHeader = new System.Windows.Forms.Label();
            this.lblTargetIp = new System.Windows.Forms.Label();
            this.txtTargetIp = new System.Windows.Forms.TextBox();
            this.lblUdpPort = new System.Windows.Forms.Label();
            this.numUdpPort = new System.Windows.Forms.NumericUpDown();

            // v2.1: Startup options (right panel)
            this.lblStartupHeader = new System.Windows.Forms.Label();
            this.cbxRunAtStartup = new System.Windows.Forms.CheckBox();
            this.cbxAutoStartEngine = new System.Windows.Forms.CheckBox();
            this.cbxAutoStartUdp = new System.Windows.Forms.CheckBox();
            this.lblStreamHeader = new System.Windows.Forms.Label();


            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLiveVideo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpPort)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.SuspendLayout();

            // 
            // btnCameraUp
            // 
            this.btnCameraUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCameraUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCameraUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.btnCameraUp.Location = new System.Drawing.Point(12, 108);
            this.btnCameraUp.Name = "btnCameraUp";
            this.btnCameraUp.Size = new System.Drawing.Size(196, 28);
            this.btnCameraUp.TabIndex = 0;
            this.btnCameraUp.Text = "Kinect move up";
            this.btnCameraUp.UseVisualStyleBackColor = false;
            this.btnCameraUp.Click += new System.EventHandler(this.BtnCameraUp_Click);

            // 
            // btnCameraDown
            // 
            this.btnCameraDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCameraDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCameraDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.btnCameraDown.Location = new System.Drawing.Point(12, 142);
            this.btnCameraDown.Name = "btnCameraDown";
            this.btnCameraDown.Size = new System.Drawing.Size(196, 28);
            this.btnCameraDown.TabIndex = 1;
            this.btnCameraDown.Text = "Kinect move down";
            this.btnCameraDown.UseVisualStyleBackColor = false;
            this.btnCameraDown.Click += new System.EventHandler(this.BtnCameraDown_Click);

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Calibri", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(82, 26);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(490, 32);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "Kinect Head Tracker v2.1";

            // 
            // picLogo
            // 
            this.picLogo.Image = global::KinectHeadtracker.Properties.Resources.Kinect;
            this.picLogo.Location = new System.Drawing.Point(12, 12);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(64, 64);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 3;
            this.picLogo.TabStop = false;

            // 
            // picLiveVideo
            // 
            this.picLiveVideo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(19)))), ((int)(((byte)(18)))));
            this.picLiveVideo.Location = new System.Drawing.Point(12, 140);
            this.picLiveVideo.Name = "picLiveVideo";
            this.picLiveVideo.Size = new System.Drawing.Size(296, 199);
            this.picLiveVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLiveVideo.TabIndex = 4;
            this.picLiveVideo.TabStop = false;
            
            // 
            // cbxLiveVideo
            // 
            this.cbxLiveVideo.AutoSize = true;
            this.cbxLiveVideo.Location = new System.Drawing.Point(12, 114);
            this.cbxLiveVideo.Name = "cbxLiveVideo";
            this.cbxLiveVideo.Size = new System.Drawing.Size(106, 17);
            this.cbxLiveVideo.TabIndex = 3;
            this.cbxLiveVideo.Text = "Show Live Video";
            this.cbxLiveVideo.UseVisualStyleBackColor = true;
            this.cbxLiveVideo.CheckedChanged += new System.EventHandler(this.cbxLiveVideo_CheckedChanged);

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 226);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "X:";

            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(72, 226);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(13, 13);
            this.lblX.TabIndex = 11;
            this.lblX.Text = "0";

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 246);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(17, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Y:";

            // 
            // lblY
            // 
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(72, 246);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(13, 13);
            this.lblY.TabIndex = 13;
            this.lblY.Text = "0";

            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 266);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(17, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Z:";

            // 
            // lblZ
            // 
            this.lblZ.AutoSize = true;
            this.lblZ.Location = new System.Drawing.Point(72, 266);
            this.lblZ.Name = "lblZ";
            this.lblZ.Size = new System.Drawing.Size(13, 13);
            this.lblZ.TabIndex = 15;
            this.lblZ.Text = "0";

            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 286);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(31, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "Yaw:";

            // 
            // lblYaw
            // 
            this.lblYaw.AutoSize = true;
            this.lblYaw.Location = new System.Drawing.Point(72, 286);
            this.lblYaw.Name = "lblYaw";
            this.lblYaw.Size = new System.Drawing.Size(13, 13);
            this.lblYaw.TabIndex = 17;
            this.lblYaw.Text = "0";

            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 306);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "Pitch:";

            // 
            // lblPitch
            // 
            this.lblPitch.AutoSize = true;
            this.lblPitch.Location = new System.Drawing.Point(72, 306);
            this.lblPitch.Name = "lblPitch";
            this.lblPitch.Size = new System.Drawing.Size(13, 13);
            this.lblPitch.TabIndex = 19;
            this.lblPitch.Text = "0";

            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(12, 326);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(28, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Roll:";

            // 
            // lblRoll
            // 
            this.lblRoll.AutoSize = true;
            this.lblRoll.Location = new System.Drawing.Point(72, 326);
            this.lblRoll.Name = "lblRoll";
            this.lblRoll.Size = new System.Drawing.Size(13, 13);
            this.lblRoll.TabIndex = 21;
            this.lblRoll.Text = "0";

            // 
            // timer1
            // 
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);

            // 
            // timer2
            // 
            this.timer2.Interval = 50;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);

            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(17)))), ((int)(((byte)(16)))));
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(580, 88);
            this.pnlHeader.TabIndex = 100;

            // 
            // lblStatusPill
            // 
            this.lblStatusPill.BackColor = System.Drawing.Color.Transparent;
            this.lblStatusPill.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lblStatusPill.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular);
            this.lblStatusPill.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblStatusPill.Location = new System.Drawing.Point(455, 40);
            this.lblStatusPill.Name = "lblStatusPill";
            this.lblStatusPill.Size = new System.Drawing.Size(90, 24);
            this.lblStatusPill.TabIndex = 101;
            this.lblStatusPill.Text = "● STOPPED";
            this.lblStatusPill.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            //  btnClose (NEW) // 
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.Location = new System.Drawing.Point(548, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(30, 30);
            this.btnClose.TabIndex = 199;
            this.btnClose.Text = "✕";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));

            // btnMin (NEW) // 
            this.btnMin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMin.FlatAppearance.BorderSize = 0;
            this.btnMin.Location = new System.Drawing.Point(520, 8); // adjust if you like
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(24, 24);
            this.btnMin.TabIndex = 198;
            this.btnMin.Text = "–";
            this.btnMin.UseVisualStyleBackColor = true;
            this.btnMin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // Add header children
            this.pnlHeader.Controls.Add(this.picLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblStatusPill);
            this.pnlHeader.Controls.Add(this.btnClose);
            this.pnlHeader.Controls.Add(this.btnMin);

            // 
            // pnlLeft
            // 
            this.pnlLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(15)))), ((int)(((byte)(14)))));
            this.pnlLeft.Location = new System.Drawing.Point(12, 98);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Size = new System.Drawing.Size(220, 351);
            this.pnlLeft.TabIndex = 102;

            // 
            // pnlRight
            // 
            this.pnlRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(15)))), ((int)(((byte)(14)))));
            this.pnlRight.Location = new System.Drawing.Point(248, 98);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(320, 351);
            this.pnlRight.TabIndex = 103;
            // 
            // pnlUdpSettings (NEW)
            // 
            this.pnlUdpSettings.BackColor = System.Drawing.Color.Transparent;
            this.pnlUdpSettings.Location = new System.Drawing.Point(12, 12);
            this.pnlUdpSettings.Name = "pnlUdpSettings";
            this.pnlUdpSettings.Size = new System.Drawing.Size(296, 76);
            this.pnlUdpSettings.TabIndex = 200;

            // 
            // pnlStartupSettings (NEW)
            // 
            this.pnlStartupSettings.BackColor = System.Drawing.Color.Transparent;
            this.pnlStartupSettings.Location = new System.Drawing.Point(12, 104);
            this.pnlStartupSettings.Name = "pnlStartupSettings";
            this.pnlStartupSettings.Size = new System.Drawing.Size(296, 70);
            this.pnlStartupSettings.TabIndex = 202;

            // 
            // btnStartStop
            // 
            this.btnStartStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.btnStartStop.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnStartStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartStop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnStartStop.Location = new System.Drawing.Point(12, 176);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(196, 30);
            this.btnStartStop.TabIndex = 5;
            this.btnStartStop.Text = "Stop Head Tracking";
            this.btnStartStop.UseVisualStyleBackColor = false;

            // 
            // btnUdpToggle  (Right panel near video)
            // 
            this.btnUdpToggle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnUdpToggle.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnUdpToggle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUdpToggle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnUdpToggle.Location = new System.Drawing.Point(150, 84);
            this.btnUdpToggle.Name = "btnUdpToggle";
            this.btnUdpToggle.Size = new System.Drawing.Size(150, 28);
            this.btnUdpToggle.TabIndex = 4;
            this.btnUdpToggle.Text = "Start UDP Stream";
            this.btnUdpToggle.UseVisualStyleBackColor = false;
            // NOTE: Click handler is wired in MainForm.cs constructor

            // ----------------------------
            // v2.1: UDP config controls (LEFT PANEL)
            // ----------------------------

            // lblUdpHeader
            this.lblUdpHeader.AutoSize = true;
            this.lblUdpHeader.Location = new System.Drawing.Point(12, 8);
            this.lblUdpHeader.Name = "lblUdpHeader";
            this.lblUdpHeader.Size = new System.Drawing.Size(85, 13);
            this.lblUdpHeader.TabIndex = 30;
            this.lblUdpHeader.Text = "UDP Transport";

            // lblTargetIp
            this.lblTargetIp.AutoSize = true;
            this.lblTargetIp.Location = new System.Drawing.Point(12, 32);
            this.lblTargetIp.Name = "lblTargetIp";
            this.lblTargetIp.Size = new System.Drawing.Size(95, 24);
            this.lblTargetIp.TabIndex = 31;
            this.lblTargetIp.Text = "Target IP:";

            // txtTargetIp
            this.txtTargetIp.Location = new System.Drawing.Point(150, 30);
            this.txtTargetIp.Name = "txtTargetIp";
            this.txtTargetIp.Size = new System.Drawing.Size(150, 24);
            this.txtTargetIp.TabIndex = 32;
            this.txtTargetIp.Text = "127.0.0.1";

            // lblUdpPort
            this.lblUdpPort.AutoSize = true;
            this.lblUdpPort.Location = new System.Drawing.Point(12, 56);
            this.lblUdpPort.Name = "lblUdpPort";
            this.lblUdpPort.Size = new System.Drawing.Size(95, 24);
            this.lblUdpPort.TabIndex = 33;
            this.lblUdpPort.Text = "UDP Port:";

            // numUdpPort
            this.numUdpPort.Location = new System.Drawing.Point(150, 54);
            this.numUdpPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numUdpPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numUdpPort.Name = "numUdpPort";
            this.numUdpPort.Size = new System.Drawing.Size(150, 24);
            this.numUdpPort.TabIndex = 34;
            this.numUdpPort.Value = new decimal(new int[] { 5550, 0, 0, 0 });

            // ----------------------------
            // v2.1: Startup options (RIGHT PANEL)
            // ----------------------------

            // lblStartupHeader
            this.lblStartupHeader.AutoSize = true;
            this.lblStartupHeader.Location = new System.Drawing.Point(12, 8);
            this.lblStartupHeader.Name = "lblStartupHeader";
            this.lblStartupHeader.Size = new System.Drawing.Size(52, 13);
            this.lblStartupHeader.TabIndex = 30;
            this.lblStartupHeader.Text = "Startup";

            // cbxRunAtStartup
            this.cbxRunAtStartup.AutoSize = true;
            this.cbxRunAtStartup.Location = new System.Drawing.Point(12, 30);
            this.cbxRunAtStartup.Name = "cbxRunAtStartup";
            this.cbxRunAtStartup.Size = new System.Drawing.Size(97, 17);
            this.cbxRunAtStartup.TabIndex = 61;
            this.cbxRunAtStartup.Text = "Run at startup";
            this.cbxRunAtStartup.UseVisualStyleBackColor = true;

            // cbxAutoStartEngine
            this.cbxAutoStartEngine.AutoSize = true;
            this.cbxAutoStartEngine.Location = new System.Drawing.Point(12, 74);
            this.cbxAutoStartEngine.Name = "cbxAutoStartEngine";
            this.cbxAutoStartEngine.Size = new System.Drawing.Size(104, 17);
            this.cbxAutoStartEngine.TabIndex = 63;
            this.cbxAutoStartEngine.Text = "Auto-start engine";
            this.cbxAutoStartEngine.UseVisualStyleBackColor = true;

            // cbxAutoStartUdp
            this.cbxAutoStartUdp.AutoSize = true;
            this.cbxAutoStartUdp.Location = new System.Drawing.Point(12, 52);
            this.cbxAutoStartUdp.Name = "cbxAutoStartUdp";
            this.cbxAutoStartUdp.Size = new System.Drawing.Size(132, 17);
            this.cbxAutoStartUdp.TabIndex = 62;
            this.cbxAutoStartUdp.Text = "Auto-start UDP stream";
            this.cbxAutoStartUdp.UseVisualStyleBackColor = true;

            // Add left panel children
            this.pnlLeft.Controls.Add(this.btnCameraUp);
            this.pnlLeft.Controls.Add(this.btnCameraDown);
            this.pnlLeft.Controls.Add(this.btnStartStop);

            this.pnlLeft.Controls.Add(this.label1);
            this.pnlLeft.Controls.Add(this.lblX);
            this.pnlLeft.Controls.Add(this.label3);
            this.pnlLeft.Controls.Add(this.lblY);
            this.pnlLeft.Controls.Add(this.label5);
            this.pnlLeft.Controls.Add(this.lblZ);
            this.pnlLeft.Controls.Add(this.label7);
            this.pnlLeft.Controls.Add(this.lblYaw);
            this.pnlLeft.Controls.Add(this.label9);
            this.pnlLeft.Controls.Add(this.lblPitch);
            this.pnlLeft.Controls.Add(this.label11);
            this.pnlLeft.Controls.Add(this.lblRoll);
            this.pnlLeft.Controls.Add(this.lblStartupHeader);
            this.pnlLeft.Controls.Add(this.cbxRunAtStartup);
            this.pnlLeft.Controls.Add(this.cbxAutoStartEngine);
            this.pnlLeft.Controls.Add(this.cbxAutoStartUdp);

            // Add UDP settings container children
            this.pnlUdpSettings.Controls.Add(this.lblUdpHeader);
            this.pnlUdpSettings.Controls.Add(this.lblTargetIp);
            this.pnlUdpSettings.Controls.Add(this.txtTargetIp);
            this.pnlUdpSettings.Controls.Add(this.lblUdpPort);
            this.pnlUdpSettings.Controls.Add(this.numUdpPort);

            // Add right panel children (transport + streaming + video)
            this.pnlRight.Controls.Add(this.lblUdpHeader);
            this.pnlRight.Controls.Add(this.lblTargetIp);
            this.pnlRight.Controls.Add(this.txtTargetIp);
            this.pnlRight.Controls.Add(this.lblUdpPort);
            this.pnlRight.Controls.Add(this.numUdpPort);

            this.pnlRight.Controls.Add(this.cbxLiveVideo);
            this.pnlRight.Controls.Add(this.btnUdpToggle);

            // Add video LAST? (No — add it FIRST or send it back)
            this.pnlRight.Controls.Add(this.picLiveVideo);
            this.picLiveVideo.SendToBack();
            
            // lblStreamHeader
            this.lblStreamHeader.AutoSize = true;
            this.lblStreamHeader.Location = new System.Drawing.Point(12, 178);
            this.lblStreamHeader.Name = "lblStreamHeader";
            this.lblStreamHeader.Size = new System.Drawing.Size(57, 13);
            this.lblStreamHeader.TabIndex = 210;
            this.lblStreamHeader.Text = "Streaming";

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(9)))), ((int)(((byte)(8)))));
            this.ClientSize = new System.Drawing.Size(580, 461);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlRight);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(580, 461);
            this.MinimumSize = new System.Drawing.Size(580, 461);
            this.Name = "MainForm";
            this.Text = "Kinect Head Tracker v2.1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);

            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLiveVideo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpPort)).EndInit();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlLeft.ResumeLayout(false);
            this.pnlLeft.PerformLayout();
            this.pnlRight.ResumeLayout(false);
            this.pnlRight.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnCameraUp;
        private System.Windows.Forms.Button btnCameraDown;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.PictureBox picLiveVideo;
        private System.Windows.Forms.CheckBox cbxLiveVideo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblZ;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblYaw;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblRoll;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;

        // NEW: structured layout
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblStatusPill;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Button btnStartStop;

        // NEW: UDP toggle button (single button)
        private System.Windows.Forms.Button btnUdpToggle;

        // NEW: close/min buttons for borderless form
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnMin;

        // v2.1: UDP transport config (left panel)
        private System.Windows.Forms.Label lblUdpHeader;
        private System.Windows.Forms.Label lblTargetIp;
        private System.Windows.Forms.TextBox txtTargetIp;
        private System.Windows.Forms.Label lblUdpPort;
        private System.Windows.Forms.NumericUpDown numUdpPort;

        // v2.1: Startup options (right panel)
        private System.Windows.Forms.Label lblStartupHeader;
        private System.Windows.Forms.CheckBox cbxRunAtStartup;
        private System.Windows.Forms.CheckBox cbxAutoStartEngine;
        private System.Windows.Forms.CheckBox cbxAutoStartUdp;
        // NEW: right panel sub-layout containers
        private System.Windows.Forms.Panel pnlUdpSettings;
        private System.Windows.Forms.Panel pnlStartupSettings;
        private System.Windows.Forms.Label lblStreamHeader;
    }
}
