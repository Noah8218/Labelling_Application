using Cyotek.Windows.Forms;

namespace MvcVisionSystem
{
    partial class FormImageList
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormImageList));
            this.timePixelData = new System.Windows.Forms.Timer(this.components);
            this.rjPanel1 = new RJCodeUI_M1.RJControls.RJPanel();
            this.dgvImagesList = new RJCodeUI_M1.RJControls.RJDataGridView();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnOpenFileList = new RJCodeUI_M1.RJControls.RJButton();
            this.uiSplitContainer1 = new Sunny.UI.UISplitContainer();
            this.rjPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvImagesList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).BeginInit();
            this.uiSplitContainer1.Panel1.SuspendLayout();
            this.uiSplitContainer1.Panel2.SuspendLayout();
            this.uiSplitContainer1.SuspendLayout();
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
            this.rjPanel1.Controls.Add(this.uiSplitContainer1);
            this.rjPanel1.Customizable = false;
            this.rjPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rjPanel1.Location = new System.Drawing.Point(0, 0);
            this.rjPanel1.Name = "rjPanel1";
            this.rjPanel1.Size = new System.Drawing.Size(534, 513);
            this.rjPanel1.TabIndex = 2144;
            // 
            // dgvImagesList
            // 
            this.dgvImagesList.AllowUserToAddRows = false;
            this.dgvImagesList.AllowUserToDeleteRows = false;
            this.dgvImagesList.AllowUserToResizeRows = false;
            this.dgvImagesList.AlternatingRowsColor = System.Drawing.Color.Empty;
            this.dgvImagesList.AlternatingRowsColorApply = false;
            this.dgvImagesList.BackgroundColor = System.Drawing.Color.White;
            this.dgvImagesList.BorderRadius = 13;
            this.dgvImagesList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvImagesList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvImagesList.ColumnHeaderColor = System.Drawing.Color.MediumPurple;
            this.dgvImagesList.ColumnHeaderFont = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvImagesList.ColumnHeaderHeight = 40;
            this.dgvImagesList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.MediumPurple;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvImagesList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvImagesList.ColumnHeadersHeight = 40;
            this.dgvImagesList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvImagesList.ColumnHeaderTextColor = System.Drawing.Color.White;
            this.dgvImagesList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column2,
            this.Column1});
            this.dgvImagesList.ColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgvImagesList.Customizable = false;
            this.dgvImagesList.DgvBackColor = System.Drawing.Color.White;
            this.dgvImagesList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvImagesList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvImagesList.EnableHeadersVisualStyles = false;
            this.dgvImagesList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.dgvImagesList.Location = new System.Drawing.Point(0, 0);
            this.dgvImagesList.Name = "dgvImagesList";
            this.dgvImagesList.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.dgvImagesList.RowHeaderColor = System.Drawing.Color.WhiteSmoke;
            this.dgvImagesList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvImagesList.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvImagesList.RowHeadersVisible = false;
            this.dgvImagesList.RowHeadersWidth = 30;
            this.dgvImagesList.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvImagesList.RowHeight = 40;
            this.dgvImagesList.RowsColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.Gray;
            this.dgvImagesList.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvImagesList.RowsTextColor = System.Drawing.Color.Gray;
            this.dgvImagesList.RowTemplate.Height = 40;
            this.dgvImagesList.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            this.dgvImagesList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvImagesList.SelectionTextColor = System.Drawing.Color.Gray;
            this.dgvImagesList.Size = new System.Drawing.Size(534, 407);
            this.dgvImagesList.TabIndex = 2155;
            this.dgvImagesList.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvImagesList_CellContentDoubleClick);
            // 
            // Column2
            // 
            this.Column2.HeaderText = "이미지";
            this.Column2.Name = "Column2";
            this.Column2.Width = 225;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "파일 경로";
            this.Column1.Name = "Column1";
            this.Column1.Width = 225;
            // 
            // btnOpenFileList
            // 
            this.btnOpenFileList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenFileList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(146)))), ((int)(((byte)(246)))));
            this.btnOpenFileList.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(146)))), ((int)(((byte)(246)))));
            this.btnOpenFileList.BorderRadius = 10;
            this.btnOpenFileList.BorderSize = 3;
            this.btnOpenFileList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOpenFileList.Design = RJCodeUI_M1.RJControls.ButtonDesign.Normal;
            this.btnOpenFileList.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.btnOpenFileList.FlatAppearance.BorderSize = 0;
            this.btnOpenFileList.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(84)))), ((int)(((byte)(137)))), ((int)(((byte)(231)))));
            this.btnOpenFileList.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(79)))), ((int)(((byte)(128)))), ((int)(((byte)(216)))));
            this.btnOpenFileList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFileList.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenFileList.ForeColor = System.Drawing.Color.White;
            this.btnOpenFileList.IconChar = FontAwesome.Sharp.IconChar.FileImport;
            this.btnOpenFileList.IconColor = System.Drawing.Color.White;
            this.btnOpenFileList.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnOpenFileList.IconSize = 50;
            this.btnOpenFileList.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnOpenFileList.Location = new System.Drawing.Point(372, 3);
            this.btnOpenFileList.Name = "btnOpenFileList";
            this.btnOpenFileList.Size = new System.Drawing.Size(159, 84);
            this.btnOpenFileList.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnOpenFileList.TabIndex = 2154;
            this.btnOpenFileList.Text = "이미지 폴더 열기";
            this.btnOpenFileList.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnOpenFileList.UseVisualStyleBackColor = false;
            this.btnOpenFileList.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // uiSplitContainer1
            // 
            this.uiSplitContainer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.uiSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiSplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.uiSplitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.uiSplitContainer1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiSplitContainer1.Name = "uiSplitContainer1";
            this.uiSplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // uiSplitContainer1.Panel1
            // 
            this.uiSplitContainer1.Panel1.Controls.Add(this.btnOpenFileList);
            this.uiSplitContainer1.Panel1MinSize = 0;
            // 
            // uiSplitContainer1.Panel2
            // 
            this.uiSplitContainer1.Panel2.Controls.Add(this.dgvImagesList);
            this.uiSplitContainer1.Panel2MinSize = 0;
            this.uiSplitContainer1.Size = new System.Drawing.Size(534, 513);
            this.uiSplitContainer1.SplitterDistance = 95;
            this.uiSplitContainer1.SplitterWidth = 11;
            this.uiSplitContainer1.TabIndex = 2156;
            // 
            // FormImageList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(534, 513);
            this.Controls.Add(this.rjPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FormImageList";
            this.Text = "이미지 리스트";
            this.Load += new System.EventHandler(this.Form_Load);
            this.VisibleChanged += new System.EventHandler(this.Form_VisibleChanged);
            this.rjPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvImagesList)).EndInit();
            this.uiSplitContainer1.Panel1.ResumeLayout(false);
            this.uiSplitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).EndInit();
            this.uiSplitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timePixelData;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel1;
        private RJCodeUI_M1.RJControls.RJButton btnOpenFileList;
        private RJCodeUI_M1.RJControls.RJDataGridView dgvImagesList;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private Sunny.UI.UISplitContainer uiSplitContainer1;
    }
}