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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormImageList));
            this.timePixelData = new System.Windows.Forms.Timer(this.components);
            this.rjPanel1 = new RJCodeUI_M1.RJControls.RJPanel();
            this.imageListView1 = new Manina.Windows.Forms.ImageListView();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.miniToolStrip = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.rendererToolStripLabel = new System.Windows.Forms.ToolStripLabel();
            this.renderertoolStripComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.colorToolStripComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.thumbnailsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.galleryToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.horizontalStripToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.verticalStripToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.clearThumbsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.x96ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x120ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x200ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.btnOpenFileList = new RJCodeUI_M1.RJControls.RJButton();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.rjPanel2 = new RJCodeUI_M1.RJControls.RJPanel();
            this.uiSplitContainer1 = new Sunny.UI.UISplitContainer();
            this.rjPanel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.rjPanel2.SuspendLayout();
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
            this.rjPanel1.Controls.Add(this.toolStripContainer1);
            this.rjPanel1.Customizable = false;
            this.rjPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rjPanel1.Location = new System.Drawing.Point(0, 0);
            this.rjPanel1.Name = "rjPanel1";
            this.rjPanel1.Size = new System.Drawing.Size(554, 513);
            this.rjPanel1.TabIndex = 2144;
            // 
            // imageListView1
            // 
            this.imageListView1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.imageListView1.CheckBoxAlignment = System.Drawing.ContentAlignment.TopLeft;
            this.imageListView1.Colors = new Manina.Windows.Forms.ImageListViewColor(resources.GetString("imageListView1.Colors"));
            this.imageListView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageListView1.Location = new System.Drawing.Point(0, 0);
            this.imageListView1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.imageListView1.Name = "imageListView1";
            this.imageListView1.PersistentCacheDirectory = "";
            this.imageListView1.PersistentCacheSize = ((long)(100));
            this.imageListView1.Size = new System.Drawing.Size(554, 412);
            this.imageListView1.TabIndex = 2138;
            this.imageListView1.UseWIC = true;
            this.imageListView1.ItemClick += new Manina.Windows.Forms.ItemClickEventHandler(this.imageListView1_ItemClick);
            // 
            // BottomToolStripPanel
            // 
            this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // miniToolStrip
            // 
            this.miniToolStrip.AccessibleName = "새 항목 선택";
            this.miniToolStrip.AccessibleRole = System.Windows.Forms.AccessibleRole.ButtonDropDown;
            this.miniToolStrip.AutoSize = false;
            this.miniToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.miniToolStrip.Location = new System.Drawing.Point(536, 23);
            this.miniToolStrip.Name = "miniToolStrip";
            this.miniToolStrip.Size = new System.Drawing.Size(534, 22);
            this.miniToolStrip.TabIndex = 0;
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(39, 17);
            this.StatusLabel.Text = "Ready";
            // 
            // TopToolStripPanel
            // 
            this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rendererToolStripLabel,
            this.renderertoolStripComboBox,
            this.colorToolStripComboBox,
            this.toolStripSeparator2,
            this.thumbnailsToolStripButton,
            this.galleryToolStripButton,
            this.horizontalStripToolStripButton,
            this.verticalStripToolStripButton,
            this.toolStripSeparator3,
            this.clearThumbsToolStripButton,
            this.toolStripDropDownButton1,
            this.toolStripSeparator4});
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(248, 25);
            this.toolStrip1.TabIndex = 1;
            // 
            // rendererToolStripLabel
            // 
            this.rendererToolStripLabel.Name = "rendererToolStripLabel";
            this.rendererToolStripLabel.Size = new System.Drawing.Size(57, 22);
            this.rendererToolStripLabel.Text = "Renderer:";
            this.rendererToolStripLabel.Visible = false;
            // 
            // renderertoolStripComboBox
            // 
            this.renderertoolStripComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.renderertoolStripComboBox.Name = "renderertoolStripComboBox";
            this.renderertoolStripComboBox.Size = new System.Drawing.Size(121, 25);
            this.renderertoolStripComboBox.Visible = false;
            // 
            // colorToolStripComboBox
            // 
            this.colorToolStripComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.colorToolStripComboBox.Name = "colorToolStripComboBox";
            this.colorToolStripComboBox.Size = new System.Drawing.Size(121, 25);
            this.colorToolStripComboBox.Visible = false;
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // thumbnailsToolStripButton
            // 
            this.thumbnailsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.thumbnailsToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("thumbnailsToolStripButton.Image")));
            this.thumbnailsToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.thumbnailsToolStripButton.Name = "thumbnailsToolStripButton";
            this.thumbnailsToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.thumbnailsToolStripButton.Text = "Thumbnails";
            this.thumbnailsToolStripButton.Click += new System.EventHandler(this.thumbnailsToolStripButton_Click);
            // 
            // galleryToolStripButton
            // 
            this.galleryToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.galleryToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("galleryToolStripButton.Image")));
            this.galleryToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.galleryToolStripButton.Name = "galleryToolStripButton";
            this.galleryToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.galleryToolStripButton.Text = "Gallery";
            this.galleryToolStripButton.Click += new System.EventHandler(this.galleryToolStripButton_Click);
            // 
            // horizontalStripToolStripButton
            // 
            this.horizontalStripToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.horizontalStripToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("horizontalStripToolStripButton.Image")));
            this.horizontalStripToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.horizontalStripToolStripButton.Name = "horizontalStripToolStripButton";
            this.horizontalStripToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.horizontalStripToolStripButton.Text = "Horizontal Strip";
            this.horizontalStripToolStripButton.Click += new System.EventHandler(this.horizontalStripToolStripButton_Click);
            // 
            // verticalStripToolStripButton
            // 
            this.verticalStripToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.verticalStripToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("verticalStripToolStripButton.Image")));
            this.verticalStripToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.verticalStripToolStripButton.Name = "verticalStripToolStripButton";
            this.verticalStripToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.verticalStripToolStripButton.Text = "Vertical Strip";
            this.verticalStripToolStripButton.Click += new System.EventHandler(this.verticalStripToolStripButton_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // clearThumbsToolStripButton
            // 
            this.clearThumbsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.clearThumbsToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("clearThumbsToolStripButton.Image")));
            this.clearThumbsToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.clearThumbsToolStripButton.Name = "clearThumbsToolStripButton";
            this.clearThumbsToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.clearThumbsToolStripButton.Text = "Clear Thumbnail Cache";
            this.clearThumbsToolStripButton.Click += new System.EventHandler(this.clearThumbsToolStripButton_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.x96ToolStripMenuItem,
            this.x120ToolStripMenuItem,
            this.x200ToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(103, 22);
            this.toolStripDropDownButton1.Text = "Thumbnail Size";
            // 
            // x96ToolStripMenuItem
            // 
            this.x96ToolStripMenuItem.Name = "x96ToolStripMenuItem";
            this.x96ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.x96ToolStripMenuItem.Text = "96x96";
            this.x96ToolStripMenuItem.Click += new System.EventHandler(this.x96ToolStripMenuItem_Click);
            // 
            // x120ToolStripMenuItem
            // 
            this.x120ToolStripMenuItem.Name = "x120ToolStripMenuItem";
            this.x120ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.x120ToolStripMenuItem.Text = "120x120";
            // 
            // x200ToolStripMenuItem
            // 
            this.x200ToolStripMenuItem.Name = "x200ToolStripMenuItem";
            this.x200ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.x200ToolStripMenuItem.Text = "200x200";
            this.x200ToolStripMenuItem.Click += new System.EventHandler(this.x200ToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // RightToolStripPanel
            // 
            this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ContentPanel.Size = new System.Drawing.Size(534, 488);
            // 
            // btnOpenFileList
            // 
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
            this.btnOpenFileList.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenFileList.Location = new System.Drawing.Point(355, 5);
            this.btnOpenFileList.Name = "btnOpenFileList";
            this.btnOpenFileList.Size = new System.Drawing.Size(196, 56);
            this.btnOpenFileList.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnOpenFileList.TabIndex = 2154;
            this.btnOpenFileList.Text = "이미지 폴더 열기";
            this.btnOpenFileList.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOpenFileList.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenFileList.UseVisualStyleBackColor = false;
            this.btnOpenFileList.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.StatusStrip);
            this.toolStripContainer1.BottomToolStripPanelVisible = false;
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.uiSplitContainer1);
            this.toolStripContainer1.ContentPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(554, 488);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.LeftToolStripPanelVisible = false;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.RightToolStripPanelVisible = false;
            this.toolStripContainer1.Size = new System.Drawing.Size(554, 513);
            this.toolStripContainer1.TabIndex = 2155;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // StatusStrip
            // 
            this.StatusStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 25);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(554, 22);
            this.StatusStrip.TabIndex = 0;
            // 
            // rjPanel2
            // 
            this.rjPanel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjPanel2.BorderRadius = 0;
            this.rjPanel2.Controls.Add(this.rjPanel1);
            this.rjPanel2.Customizable = false;
            this.rjPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rjPanel2.Location = new System.Drawing.Point(0, 0);
            this.rjPanel2.Name = "rjPanel2";
            this.rjPanel2.Size = new System.Drawing.Size(554, 513);
            this.rjPanel2.TabIndex = 2145;
            // 
            // uiSplitContainer1
            // 
            this.uiSplitContainer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.uiSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiSplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.uiSplitContainer1.MinimumSize = new System.Drawing.Size(20, 20);
            this.uiSplitContainer1.Name = "uiSplitContainer1";
            this.uiSplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // uiSplitContainer1.Panel1
            // 
            this.uiSplitContainer1.Panel1.Controls.Add(this.btnOpenFileList);
            // 
            // uiSplitContainer1.Panel2
            // 
            this.uiSplitContainer1.Panel2.Controls.Add(this.imageListView1);
            this.uiSplitContainer1.Size = new System.Drawing.Size(554, 488);
            this.uiSplitContainer1.SplitterDistance = 65;
            this.uiSplitContainer1.SplitterWidth = 11;
            this.uiSplitContainer1.TabIndex = 2155;
            // 
            // FormImageList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(554, 513);
            this.Controls.Add(this.rjPanel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FormImageList";
            this.Text = "이미지 리스트";
            this.Load += new System.EventHandler(this.Form_Load);
            this.VisibleChanged += new System.EventHandler(this.Form_VisibleChanged);
            this.rjPanel1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.rjPanel2.ResumeLayout(false);
            this.uiSplitContainer1.Panel1.ResumeLayout(false);
            this.uiSplitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).EndInit();
            this.uiSplitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timePixelData;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel1;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private RJCodeUI_M1.RJControls.RJButton btnOpenFileList;
        private Manina.Windows.Forms.ImageListView imageListView1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel rendererToolStripLabel;
        private System.Windows.Forms.ToolStripComboBox renderertoolStripComboBox;
        private System.Windows.Forms.ToolStripComboBox colorToolStripComboBox;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton thumbnailsToolStripButton;
        private System.Windows.Forms.ToolStripButton galleryToolStripButton;
        private System.Windows.Forms.ToolStripButton horizontalStripToolStripButton;
        private System.Windows.Forms.ToolStripButton verticalStripToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton clearThumbsToolStripButton;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem x96ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x120ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x200ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
        private System.Windows.Forms.StatusStrip miniToolStrip;
        private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
        private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
        private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
        private System.Windows.Forms.ToolStripContentPanel ContentPanel;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel2;
        private Sunny.UI.UISplitContainer uiSplitContainer1;
    }
}