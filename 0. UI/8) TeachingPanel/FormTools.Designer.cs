using Cyotek.Windows.Forms;

namespace MvcVisionSystem
{
    partial class FormTools
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTools));
            this.timePixelData = new System.Windows.Forms.Timer(this.components);
            this.rjPanel1 = new RJCodeUI_M1.RJControls.RJPanel();
            this.btnSaveParameter = new RJCodeUI_M1.RJControls.RJButton();
            this.btnTrainROI = new RJCodeUI_M1.RJControls.RJButton();
            this.btnMultiRoi = new RJCodeUI_M1.RJControls.RJButton();
            this.btnROI = new RJCodeUI_M1.RJControls.RJButton();
            this.btnDrag = new RJCodeUI_M1.RJControls.RJButton();
            this.rjPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timePixelData
            // 
            this.timePixelData.Enabled = true;
            this.timePixelData.Interval = 10;
            // 
            // rjPanel1
            // 
            this.rjPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjPanel1.BorderRadius = 5;
            this.rjPanel1.Controls.Add(this.btnSaveParameter);
            this.rjPanel1.Controls.Add(this.btnTrainROI);
            this.rjPanel1.Controls.Add(this.btnMultiRoi);
            this.rjPanel1.Controls.Add(this.btnROI);
            this.rjPanel1.Controls.Add(this.btnDrag);
            this.rjPanel1.Customizable = false;
            this.rjPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rjPanel1.Location = new System.Drawing.Point(0, 0);
            this.rjPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.rjPanel1.Name = "rjPanel1";
            this.rjPanel1.Size = new System.Drawing.Size(67, 513);
            this.rjPanel1.TabIndex = 2144;
            // 
            // btnSaveParameter
            // 
            this.btnSaveParameter.BackColor = System.Drawing.Color.White;
            this.btnSaveParameter.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSaveParameter.BorderRadius = 15;
            this.btnSaveParameter.BorderSize = 3;
            this.btnSaveParameter.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveParameter.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnSaveParameter.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSaveParameter.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnSaveParameter.FlatAppearance.BorderSize = 3;
            this.btnSaveParameter.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.btnSaveParameter.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnSaveParameter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveParameter.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveParameter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSaveParameter.IconChar = FontAwesome.Sharp.IconChar.Save;
            this.btnSaveParameter.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSaveParameter.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnSaveParameter.IconSize = 45;
            this.btnSaveParameter.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnSaveParameter.Location = new System.Drawing.Point(0, 208);
            this.btnSaveParameter.Name = "btnSaveParameter";
            this.btnSaveParameter.Size = new System.Drawing.Size(67, 52);
            this.btnSaveParameter.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.btnSaveParameter.TabIndex = 2156;
            this.btnSaveParameter.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnSaveParameter.UseVisualStyleBackColor = false;
            // 
            // btnTrainROI
            // 
            this.btnTrainROI.BackColor = System.Drawing.Color.Transparent;
            this.btnTrainROI.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnTrainROI.BorderRadius = 15;
            this.btnTrainROI.BorderSize = 3;
            this.btnTrainROI.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTrainROI.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnTrainROI.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnTrainROI.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnTrainROI.FlatAppearance.BorderSize = 3;
            this.btnTrainROI.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.btnTrainROI.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnTrainROI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTrainROI.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTrainROI.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnTrainROI.IconChar = FontAwesome.Sharp.IconChar.FileImage;
            this.btnTrainROI.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnTrainROI.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnTrainROI.IconSize = 45;
            this.btnTrainROI.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnTrainROI.Location = new System.Drawing.Point(0, 156);
            this.btnTrainROI.Name = "btnTrainROI";
            this.btnTrainROI.Size = new System.Drawing.Size(67, 52);
            this.btnTrainROI.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.btnTrainROI.TabIndex = 2155;
            this.btnTrainROI.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnTrainROI.UseVisualStyleBackColor = false;
            // 
            // btnMultiRoi
            // 
            this.btnMultiRoi.BackColor = System.Drawing.Color.Transparent;
            this.btnMultiRoi.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnMultiRoi.BorderRadius = 15;
            this.btnMultiRoi.BorderSize = 3;
            this.btnMultiRoi.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMultiRoi.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnMultiRoi.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnMultiRoi.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnMultiRoi.FlatAppearance.BorderSize = 3;
            this.btnMultiRoi.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.btnMultiRoi.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnMultiRoi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMultiRoi.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMultiRoi.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnMultiRoi.IconChar = FontAwesome.Sharp.IconChar.ObjectUngroup;
            this.btnMultiRoi.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnMultiRoi.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnMultiRoi.IconSize = 45;
            this.btnMultiRoi.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnMultiRoi.Location = new System.Drawing.Point(0, 104);
            this.btnMultiRoi.Name = "btnMultiRoi";
            this.btnMultiRoi.Size = new System.Drawing.Size(67, 52);
            this.btnMultiRoi.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.btnMultiRoi.TabIndex = 2154;
            this.btnMultiRoi.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnMultiRoi.UseVisualStyleBackColor = false;
            // 
            // btnROI
            // 
            this.btnROI.BackColor = System.Drawing.Color.Transparent;
            this.btnROI.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnROI.BorderRadius = 15;
            this.btnROI.BorderSize = 3;
            this.btnROI.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnROI.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnROI.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnROI.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnROI.FlatAppearance.BorderSize = 3;
            this.btnROI.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.btnROI.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnROI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnROI.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnROI.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnROI.IconChar = FontAwesome.Sharp.IconChar.VectorSquare;
            this.btnROI.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnROI.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnROI.IconSize = 45;
            this.btnROI.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnROI.Location = new System.Drawing.Point(0, 52);
            this.btnROI.Name = "btnROI";
            this.btnROI.Size = new System.Drawing.Size(67, 52);
            this.btnROI.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.btnROI.TabIndex = 2153;
            this.btnROI.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnROI.UseVisualStyleBackColor = false;
            // 
            // btnDrag
            // 
            this.btnDrag.BackColor = System.Drawing.Color.Transparent;
            this.btnDrag.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnDrag.BorderRadius = 15;
            this.btnDrag.BorderSize = 3;
            this.btnDrag.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDrag.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnDrag.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDrag.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnDrag.FlatAppearance.BorderSize = 3;
            this.btnDrag.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.btnDrag.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnDrag.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDrag.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDrag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnDrag.IconChar = FontAwesome.Sharp.IconChar.MousePointer;
            this.btnDrag.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnDrag.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnDrag.IconSize = 45;
            this.btnDrag.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnDrag.Location = new System.Drawing.Point(0, 0);
            this.btnDrag.Name = "btnDrag";
            this.btnDrag.Size = new System.Drawing.Size(67, 52);
            this.btnDrag.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.btnDrag.TabIndex = 2152;
            this.btnDrag.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDrag.UseVisualStyleBackColor = false;
            // 
            // FormTools
            // 
            this.AutoHidePortion = 0.1D;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(67, 513);
            this.Controls.Add(this.rjPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormTools";
            this.Text = "Tools";
            this.Load += new System.EventHandler(this.Form_Load);
            this.VisibleChanged += new System.EventHandler(this.Form_VisibleChanged);
            this.rjPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timePixelData;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel1;
        private RJCodeUI_M1.RJControls.RJButton btnSaveParameter;
        private RJCodeUI_M1.RJControls.RJButton btnTrainROI;
        private RJCodeUI_M1.RJControls.RJButton btnMultiRoi;
        private RJCodeUI_M1.RJControls.RJButton btnROI;
        private RJCodeUI_M1.RJControls.RJButton btnDrag;
    }
}