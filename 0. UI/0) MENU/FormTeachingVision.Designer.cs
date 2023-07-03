namespace MvcVisionSystem
{
    partial class FormTeachingVision
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
            this.timerStatus = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.TeachingPanel = new System.Windows.Forms.Panel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timeragin = new System.Windows.Forms.Timer(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.miniToolStrip = new System.Windows.Forms.MenuStrip();
            this.rjButton3 = new RJCodeUI_M1.RJControls.RJButton();
            this.rjButton2 = new RJCodeUI_M1.RJControls.RJButton();
            this.rjButton1 = new RJCodeUI_M1.RJControls.RJButton();
            this.SuspendLayout();
            // 
            // timerStatus
            // 
            this.timerStatus.Enabled = true;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(1943, 425);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 46);
            this.label2.TabIndex = 1927;
            this.label2.Text = "00.000 mm";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TeachingPanel
            // 
            this.TeachingPanel.Location = new System.Drawing.Point(0, 1);
            this.TeachingPanel.Margin = new System.Windows.Forms.Padding(0);
            this.TeachingPanel.Name = "TeachingPanel";
            this.TeachingPanel.Size = new System.Drawing.Size(1922, 996);
            this.TeachingPanel.TabIndex = 1949;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timeragin
            // 
            this.timeragin.Interval = 300;
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 5000;
            this.toolTip1.InitialDelay = 100;
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ReshowDelay = 100;
            // 
            // miniToolStrip
            // 
            this.miniToolStrip.AccessibleName = "새 항목 선택";
            this.miniToolStrip.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
            this.miniToolStrip.AutoSize = false;
            this.miniToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.miniToolStrip.Location = new System.Drawing.Point(0, 0);
            this.miniToolStrip.Name = "miniToolStrip";
            this.miniToolStrip.Size = new System.Drawing.Size(707, 28);
            this.miniToolStrip.TabIndex = 2165;
            // 
            // rjButton3
            // 
            this.rjButton3.BackColor = System.Drawing.Color.White;
            this.rjButton3.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton3.BorderRadius = 15;
            this.rjButton3.BorderSize = 3;
            this.rjButton3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rjButton3.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.rjButton3.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.rjButton3.FlatAppearance.BorderSize = 3;
            this.rjButton3.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.rjButton3.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rjButton3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rjButton3.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rjButton3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton3.IconChar = FontAwesome.Sharp.IconChar.Youtube;
            this.rjButton3.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton3.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.rjButton3.IconSize = 80;
            this.rjButton3.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.rjButton3.Location = new System.Drawing.Point(208, 121);
            this.rjButton3.Name = "rjButton3";
            this.rjButton3.Size = new System.Drawing.Size(150, 112);
            this.rjButton3.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.rjButton3.TabIndex = 2138;
            this.rjButton3.Text = "LIVE";
            this.rjButton3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.rjButton3.UseVisualStyleBackColor = false;
            // 
            // rjButton2
            // 
            this.rjButton2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.rjButton2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.rjButton2.BorderRadius = 15;
            this.rjButton2.BorderSize = 3;
            this.rjButton2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rjButton2.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.rjButton2.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.rjButton2.FlatAppearance.BorderSize = 3;
            this.rjButton2.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.rjButton2.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rjButton2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rjButton2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rjButton2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.rjButton2.IconChar = FontAwesome.Sharp.IconChar.Check;
            this.rjButton2.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.rjButton2.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.rjButton2.IconSize = 24;
            this.rjButton2.Location = new System.Drawing.Point(396, 234);
            this.rjButton2.Name = "rjButton2";
            this.rjButton2.Size = new System.Drawing.Size(117, 96);
            this.rjButton2.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.rjButton2.TabIndex = 2139;
            this.rjButton2.Text = "EXCUTE";
            this.rjButton2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rjButton2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rjButton2.UseVisualStyleBackColor = false;
            // 
            // rjButton1
            // 
            this.rjButton1.BackColor = System.Drawing.Color.White;
            this.rjButton1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton1.BorderRadius = 15;
            this.rjButton1.BorderSize = 3;
            this.rjButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rjButton1.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.rjButton1.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.rjButton1.FlatAppearance.BorderSize = 3;
            this.rjButton1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.rjButton1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rjButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rjButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rjButton1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton1.IconChar = FontAwesome.Sharp.IconChar.Camera;
            this.rjButton1.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.rjButton1.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.rjButton1.IconSize = 80;
            this.rjButton1.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.rjButton1.Location = new System.Drawing.Point(52, 121);
            this.rjButton1.Name = "rjButton1";
            this.rjButton1.Size = new System.Drawing.Size(150, 112);
            this.rjButton1.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.rjButton1.TabIndex = 2137;
            this.rjButton1.Text = "GRAB";
            this.rjButton1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.rjButton1.UseVisualStyleBackColor = false;
            // 
            // FormTeachingVision
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1924, 996);
            this.ControlBox = false;
            this.Controls.Add(this.TeachingPanel);
            this.Controls.Add(this.label2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Movable = false;
            this.Name = "FormTeachingVision";
            this.Resizable = false;
            this.ShadowType = MetroFramework.Forms.MetroFormShadowType.None;
            this.ShowInTaskbar = false;
            this.Style = MetroFramework.MetroColorStyle.White;
            this.TransparencyKey = System.Drawing.Color.Empty;
            this.Load += new System.EventHandler(this.FormTeachingVision_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timerStatus;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.Panel TeachingPanel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timeragin;
        private System.Windows.Forms.ToolTip toolTip1;
        private RJCodeUI_M1.RJControls.RJButton rjButton3;
        private RJCodeUI_M1.RJControls.RJButton rjButton2;
        private RJCodeUI_M1.RJControls.RJButton rjButton1;
        private System.Windows.Forms.MenuStrip miniToolStrip;
    }
}
