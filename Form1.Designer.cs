namespace OMTplay
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            flowLayoutPanel1 = new FlowLayoutPanel();
            _buttonPanel = new Panel();
            label1 = new Label();
            radioButton4 = new RadioButton();
            radioButton3 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton1 = new RadioButton();
            _cbSources = new ComboBox();
            _lblTimestamp = new Label();
            _lblResolution = new Label();
            _lblConnected = new Label();
            _lblStatus = new Label();
            _btnRefresh = new Button();
            _btnConnect = new Button();
            _btnDisconnect = new Button();
            _btnFullscreen = new Button();
            _btnExit = new Button();
            _videoBox = new PictureBox();
            flowLayoutPanel1.SuspendLayout();
            _buttonPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_videoBox).BeginInit();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.BackColor = Color.Brown;
            flowLayoutPanel1.Controls.Add(_buttonPanel);
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1055, 73);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // _buttonPanel
            // 
            _buttonPanel.BackColor = Color.Silver;
            _buttonPanel.Controls.Add(label1);
            _buttonPanel.Controls.Add(radioButton4);
            _buttonPanel.Controls.Add(radioButton3);
            _buttonPanel.Controls.Add(radioButton2);
            _buttonPanel.Controls.Add(radioButton1);
            _buttonPanel.Controls.Add(_cbSources);
            _buttonPanel.Controls.Add(_lblTimestamp);
            _buttonPanel.Controls.Add(_lblResolution);
            _buttonPanel.Controls.Add(_lblConnected);
            _buttonPanel.Controls.Add(_lblStatus);
            _buttonPanel.Controls.Add(_btnRefresh);
            _buttonPanel.Controls.Add(_btnConnect);
            _buttonPanel.Controls.Add(_btnDisconnect);
            _buttonPanel.Controls.Add(_btnFullscreen);
            _buttonPanel.Controls.Add(_btnExit);
            _buttonPanel.Location = new Point(0, 0);
            _buttonPanel.Margin = new Padding(0);
            _buttonPanel.Name = "_buttonPanel";
            _buttonPanel.Size = new Size(1035, 73);
            _buttonPanel.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = SystemColors.ControlText;
            label1.Location = new Point(319, 4);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 14;
            label1.Text = "Audio:";
            // 
            // radioButton4
            // 
            radioButton4.AutoSize = true;
            radioButton4.Location = new Point(564, 4);
            radioButton4.Name = "radioButton4";
            radioButton4.Size = new Size(59, 19);
            radioButton4.TabIndex = 13;
            radioButton4.Text = "CH7/8";
            radioButton4.UseVisualStyleBackColor = true;
            radioButton4.CheckedChanged += radioButton4_CheckedChanged;
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.Location = new Point(495, 3);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(59, 19);
            radioButton3.TabIndex = 12;
            radioButton3.Text = "CH5/6";
            radioButton3.UseVisualStyleBackColor = true;
            radioButton3.CheckedChanged += radioButton3_CheckedChanged;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new Point(430, 3);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new Size(59, 19);
            radioButton2.TabIndex = 11;
            radioButton2.Text = "CH3/4";
            radioButton2.UseVisualStyleBackColor = true;
            radioButton2.CheckedChanged += radioButton2_CheckedChanged;
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Location = new Point(368, 3);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new Size(59, 19);
            radioButton1.TabIndex = 10;
            radioButton1.Text = "CH1/2";
            radioButton1.UseVisualStyleBackColor = true;
            radioButton1.CheckedChanged += radioButton1_CheckedChanged;
            // 
            // _cbSources
            // 
            _cbSources.FlatStyle = FlatStyle.Flat;
            _cbSources.FormattingEnabled = true;
            _cbSources.Location = new Point(365, 25);
            _cbSources.Name = "_cbSources";
            _cbSources.Size = new Size(255, 23);
            _cbSources.TabIndex = 9;
            _cbSources.Text = "select source";
            // 
            // _lblTimestamp
            // 
            _lblTimestamp.BackColor = SystemColors.Control;
            _lblTimestamp.Location = new Point(8, 52);
            _lblTimestamp.Name = "_lblTimestamp";
            _lblTimestamp.Size = new Size(273, 15);
            _lblTimestamp.TabIndex = 8;
            _lblTimestamp.Text = "Timestamp ";
            // 
            // _lblResolution
            // 
            _lblResolution.BackColor = SystemColors.Control;
            _lblResolution.Location = new Point(8, 29);
            _lblResolution.Name = "_lblResolution";
            _lblResolution.Size = new Size(273, 15);
            _lblResolution.TabIndex = 7;
            _lblResolution.Text = "Resolution";
            // 
            // _lblConnected
            // 
            _lblConnected.BackColor = SystemColors.Control;
            _lblConnected.ForeColor = Color.Red;
            _lblConnected.Location = new Point(8, 6);
            _lblConnected.Name = "_lblConnected";
            _lblConnected.Size = new Size(273, 15);
            _lblConnected.TabIndex = 6;
            _lblConnected.Text = "Disconnected";
            // 
            // _lblStatus
            // 
            _lblStatus.BackColor = Color.Silver;
            _lblStatus.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _lblStatus.Location = new Point(300, 55);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(467, 15);
            _lblStatus.TabIndex = 5;
            _lblStatus.Text = "Status:";
            _lblStatus.Visible = false;
            // 
            // _btnRefresh
            // 
            _btnRefresh.BackColor = SystemColors.Control;
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Location = new Point(300, 24);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.Size = new Size(61, 23);
            _btnRefresh.TabIndex = 4;
            _btnRefresh.Text = "refresh";
            _btnRefresh.UseVisualStyleBackColor = false;
            // 
            // _btnConnect
            // 
            _btnConnect.BackColor = Color.PowderBlue;
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.FlatStyle = FlatStyle.Flat;
            _btnConnect.Location = new Point(629, 17);
            _btnConnect.Name = "_btnConnect";
            _btnConnect.Size = new Size(75, 38);
            _btnConnect.TabIndex = 3;
            _btnConnect.Text = "Connect";
            _btnConnect.UseVisualStyleBackColor = false;
            // 
            // _btnDisconnect
            // 
            _btnDisconnect.BackColor = Color.LightCoral;
            _btnDisconnect.FlatAppearance.BorderSize = 0;
            _btnDisconnect.FlatStyle = FlatStyle.Flat;
            _btnDisconnect.Location = new Point(628, 17);
            _btnDisconnect.Name = "_btnDisconnect";
            _btnDisconnect.Size = new Size(75, 38);
            _btnDisconnect.TabIndex = 2;
            _btnDisconnect.Text = "Disconnect";
            _btnDisconnect.UseVisualStyleBackColor = false;
            // 
            // _btnFullscreen
            // 
            _btnFullscreen.BackColor = SystemColors.Control;
            _btnFullscreen.FlatAppearance.BorderSize = 0;
            _btnFullscreen.FlatStyle = FlatStyle.Flat;
            _btnFullscreen.Location = new Point(725, 16);
            _btnFullscreen.Name = "_btnFullscreen";
            _btnFullscreen.Size = new Size(195, 38);
            _btnFullscreen.TabIndex = 1;
            _btnFullscreen.Text = "Fullscreen / →Esc returns";
            _btnFullscreen.UseVisualStyleBackColor = false;
            // 
            // _btnExit
            // 
            _btnExit.BackColor = Color.FromArgb(192, 0, 0);
            _btnExit.FlatAppearance.BorderSize = 0;
            _btnExit.FlatStyle = FlatStyle.Flat;
            _btnExit.ForeColor = Color.White;
            _btnExit.Location = new Point(943, 16);
            _btnExit.Name = "_btnExit";
            _btnExit.Size = new Size(75, 38);
            _btnExit.TabIndex = 0;
            _btnExit.Text = "Exit";
            _btnExit.UseVisualStyleBackColor = false;
            // 
            // _videoBox
            // 
            _videoBox.Location = new Point(6, 79);
            _videoBox.Name = "_videoBox";
            _videoBox.Size = new Size(1004, 565);
            _videoBox.SizeMode = PictureBoxSizeMode.Zoom;
            _videoBox.TabIndex = 10;
            _videoBox.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1030, 689);
            Controls.Add(_videoBox);
            Controls.Add(flowLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1036, 695);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += Form1_Load;
            flowLayoutPanel1.ResumeLayout(false);
            _buttonPanel.ResumeLayout(false);
            _buttonPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_videoBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanel1;
        private Panel _buttonPanel;
        private ComboBox _cbSources;
        private Label _lblTimestamp;
        private Label _lblResolution;
        private Label _lblConnected;
        private Label _lblStatus;
        private Button _btnRefresh;
        private Button _btnConnect;
        private Button _btnDisconnect;
        private Button _btnFullscreen;
        private Button _btnExit;
        private PictureBox _videoBox;
        private RadioButton radioButton2;
        private RadioButton radioButton1;
        private RadioButton radioButton4;
        private RadioButton radioButton3;
        private Label label1;
    }
}
