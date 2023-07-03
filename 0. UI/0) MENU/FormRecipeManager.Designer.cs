namespace MvcVisionSystem
{
    partial class FormRecipeManager
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvRecipeList = new RJCodeUI_M1.RJControls.RJDataGridView();
            this.lbSelectedModel = new RJCodeUI_M1.RJControls.RJLabel();
            this.lbLastUpdateTime = new RJCodeUI_M1.RJControls.RJLabel();
            this.lbModelNo = new RJCodeUI_M1.RJControls.RJLabel();
            this.lbModelName = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel7 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel6 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel5 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel4 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjPanel2 = new RJCodeUI_M1.RJControls.RJPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.btnSearch = new RJCodeUI_M1.RJControls.RJButton();
            this.tbSearch = new RJCodeUI_M1.RJControls.RJTextBox();
            this.pnRecipeNew = new System.Windows.Forms.Panel();
            this.cbRecipeRefList = new RJCodeUI_M1.RJControls.RJComboBox();
            this.rjLabel3 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel2 = new RJCodeUI_M1.RJControls.RJLabel();
            this.rjLabel1 = new RJCodeUI_M1.RJControls.RJLabel();
            this.lblTitle = new RJCodeUI_M1.RJControls.RJLabel();
            this.tbRecipeNewNo = new RJCodeUI_M1.RJControls.RJTextBox();
            this.tbRecipeNewName = new RJCodeUI_M1.RJControls.RJTextBox();
            this.btnNewRecipeCancel = new RJCodeUI_M1.RJControls.RJButton();
            this.btnNewRecipeCreate = new RJCodeUI_M1.RJControls.RJButton();
            this.rjPanel1 = new RJCodeUI_M1.RJControls.RJPanel();
            this.btnRecipeDel = new RJCodeUI_M1.RJControls.RJButton();
            this.btnRecipeNew = new RJCodeUI_M1.RJControls.RJButton();
            this.btnRecipeSelect = new RJCodeUI_M1.RJControls.RJButton();
            this.pnlClientArea.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecipeList)).BeginInit();
            this.rjPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.pnRecipeNew.SuspendLayout();
            this.rjPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlClientArea
            // 
            this.pnlClientArea.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.pnlClientArea.Controls.Add(this.splitContainer1);
            this.pnlClientArea.Location = new System.Drawing.Point(1, 41);
            this.pnlClientArea.Size = new System.Drawing.Size(958, 897);
            // 
            // dgvRecipeList
            // 
            this.dgvRecipeList.AllowUserToAddRows = false;
            this.dgvRecipeList.AllowUserToDeleteRows = false;
            this.dgvRecipeList.AllowUserToResizeRows = false;
            this.dgvRecipeList.AlternatingRowsColor = System.Drawing.Color.Empty;
            this.dgvRecipeList.AlternatingRowsColorApply = false;
            this.dgvRecipeList.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.dgvRecipeList.BorderRadius = 13;
            this.dgvRecipeList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvRecipeList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvRecipeList.ColumnHeaderColor = System.Drawing.Color.MediumPurple;
            this.dgvRecipeList.ColumnHeaderFont = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvRecipeList.ColumnHeaderHeight = 40;
            this.dgvRecipeList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.MediumPurple;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecipeList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvRecipeList.ColumnHeadersHeight = 40;
            this.dgvRecipeList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvRecipeList.ColumnHeaderTextColor = System.Drawing.Color.White;
            this.dgvRecipeList.ColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgvRecipeList.Customizable = false;
            this.dgvRecipeList.DgvBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.dgvRecipeList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRecipeList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvRecipeList.EnableHeadersVisualStyles = false;
            this.dgvRecipeList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.dgvRecipeList.Location = new System.Drawing.Point(0, 0);
            this.dgvRecipeList.Name = "dgvRecipeList";
            this.dgvRecipeList.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.dgvRecipeList.RowHeaderColor = System.Drawing.Color.WhiteSmoke;
            this.dgvRecipeList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRecipeList.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRecipeList.RowHeadersVisible = false;
            this.dgvRecipeList.RowHeadersWidth = 30;
            this.dgvRecipeList.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvRecipeList.RowHeight = 40;
            this.dgvRecipeList.RowsColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.Gray;
            this.dgvRecipeList.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvRecipeList.RowsTextColor = System.Drawing.Color.Gray;
            this.dgvRecipeList.RowTemplate.Height = 40;
            this.dgvRecipeList.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(213)))), ((int)(((byte)(199)))), ((int)(((byte)(241)))));
            this.dgvRecipeList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRecipeList.SelectionTextColor = System.Drawing.Color.Gray;
            this.dgvRecipeList.Size = new System.Drawing.Size(437, 829);
            this.dgvRecipeList.TabIndex = 2107;
            this.dgvRecipeList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvRecipeList_CellClick);
            this.dgvRecipeList.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvRecipeList_CellValueChanged);
            this.dgvRecipeList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgv_KeyDown);
            // 
            // lbSelectedModel
            // 
            this.lbSelectedModel.AutoSize = true;
            this.lbSelectedModel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.lbSelectedModel.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.lbSelectedModel.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.lbSelectedModel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.lbSelectedModel.LinkLabel = false;
            this.lbSelectedModel.Location = new System.Drawing.Point(190, 134);
            this.lbSelectedModel.Name = "lbSelectedModel";
            this.lbSelectedModel.Size = new System.Drawing.Size(14, 16);
            this.lbSelectedModel.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.lbSelectedModel.TabIndex = 2131;
            this.lbSelectedModel.Text = "-";
            // 
            // lbLastUpdateTime
            // 
            this.lbLastUpdateTime.AutoSize = true;
            this.lbLastUpdateTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.lbLastUpdateTime.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.lbLastUpdateTime.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.lbLastUpdateTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.lbLastUpdateTime.LinkLabel = false;
            this.lbLastUpdateTime.Location = new System.Drawing.Point(190, 93);
            this.lbLastUpdateTime.Name = "lbLastUpdateTime";
            this.lbLastUpdateTime.Size = new System.Drawing.Size(14, 16);
            this.lbLastUpdateTime.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.lbLastUpdateTime.TabIndex = 2130;
            this.lbLastUpdateTime.Text = "-";
            // 
            // lbModelNo
            // 
            this.lbModelNo.AutoSize = true;
            this.lbModelNo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.lbModelNo.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.lbModelNo.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.lbModelNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.lbModelNo.LinkLabel = false;
            this.lbModelNo.Location = new System.Drawing.Point(190, 51);
            this.lbModelNo.Name = "lbModelNo";
            this.lbModelNo.Size = new System.Drawing.Size(14, 16);
            this.lbModelNo.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.lbModelNo.TabIndex = 2129;
            this.lbModelNo.Text = "-";
            // 
            // lbModelName
            // 
            this.lbModelName.AutoSize = true;
            this.lbModelName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.lbModelName.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.lbModelName.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.lbModelName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.lbModelName.LinkLabel = false;
            this.lbModelName.Location = new System.Drawing.Point(190, 9);
            this.lbModelName.Name = "lbModelName";
            this.lbModelName.Size = new System.Drawing.Size(14, 16);
            this.lbModelName.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.lbModelName.TabIndex = 2128;
            this.lbModelName.Text = "-";
            // 
            // rjLabel7
            // 
            this.rjLabel7.AutoSize = true;
            this.rjLabel7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjLabel7.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel7.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel7.LinkLabel = false;
            this.rjLabel7.Location = new System.Drawing.Point(27, 134);
            this.rjLabel7.Name = "rjLabel7";
            this.rjLabel7.Size = new System.Drawing.Size(105, 16);
            this.rjLabel7.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel7.TabIndex = 2127;
            this.rjLabel7.Text = "Choice Model :";
            // 
            // rjLabel6
            // 
            this.rjLabel6.AutoSize = true;
            this.rjLabel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjLabel6.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel6.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel6.LinkLabel = false;
            this.rjLabel6.Location = new System.Drawing.Point(27, 93);
            this.rjLabel6.Name = "rjLabel6";
            this.rjLabel6.Size = new System.Drawing.Size(140, 16);
            this.rjLabel6.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel6.TabIndex = 2126;
            this.rjLabel6.Text = "Last modified date :";
            // 
            // rjLabel5
            // 
            this.rjLabel5.AutoSize = true;
            this.rjLabel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjLabel5.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel5.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel5.LinkLabel = false;
            this.rjLabel5.Location = new System.Drawing.Point(27, 51);
            this.rjLabel5.Name = "rjLabel5";
            this.rjLabel5.Size = new System.Drawing.Size(94, 16);
            this.rjLabel5.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel5.TabIndex = 2125;
            this.rjLabel5.Text = "Model Code :";
            // 
            // rjLabel4
            // 
            this.rjLabel4.AutoSize = true;
            this.rjLabel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjLabel4.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel4.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel4.LinkLabel = false;
            this.rjLabel4.Location = new System.Drawing.Point(27, 9);
            this.rjLabel4.Name = "rjLabel4";
            this.rjLabel4.Size = new System.Drawing.Size(97, 16);
            this.rjLabel4.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel4.TabIndex = 2124;
            this.rjLabel4.Text = "Model Name :";
            // 
            // rjPanel2
            // 
            this.rjPanel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjPanel2.BorderRadius = 0;
            this.rjPanel2.Controls.Add(this.rjLabel6);
            this.rjPanel2.Controls.Add(this.lbSelectedModel);
            this.rjPanel2.Controls.Add(this.rjLabel4);
            this.rjPanel2.Controls.Add(this.lbLastUpdateTime);
            this.rjPanel2.Controls.Add(this.rjLabel5);
            this.rjPanel2.Controls.Add(this.lbModelNo);
            this.rjPanel2.Controls.Add(this.lbModelName);
            this.rjPanel2.Controls.Add(this.rjLabel7);
            this.rjPanel2.Customizable = false;
            this.rjPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.rjPanel2.Location = new System.Drawing.Point(0, 0);
            this.rjPanel2.Name = "rjPanel2";
            this.rjPanel2.Size = new System.Drawing.Size(517, 167);
            this.rjPanel2.TabIndex = 2133;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pnRecipeNew);
            this.splitContainer1.Panel2.Controls.Add(this.rjPanel1);
            this.splitContainer1.Panel2.Controls.Add(this.rjPanel2);
            this.splitContainer1.Size = new System.Drawing.Size(958, 897);
            this.splitContainer1.SplitterDistance = 437;
            this.splitContainer1.TabIndex = 2134;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.btnSearch);
            this.splitContainer2.Panel1.Controls.Add(this.tbSearch);
            this.splitContainer2.Panel1MinSize = 0;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.dgvRecipeList);
            this.splitContainer2.Panel2MinSize = 0;
            this.splitContainer2.Size = new System.Drawing.Size(437, 897);
            this.splitContainer2.SplitterDistance = 67;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 2108;
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSearch.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSearch.BorderRadius = 0;
            this.btnSearch.BorderSize = 1;
            this.btnSearch.Design = RJCodeUI_M1.RJControls.ButtonDesign.IconButton;
            this.btnSearch.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnSearch.FlatAppearance.BorderSize = 0;
            this.btnSearch.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(91)))), ((int)(((byte)(199)))));
            this.btnSearch.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(85)))), ((int)(((byte)(186)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.IconChar = FontAwesome.Sharp.IconChar.Search;
            this.btnSearch.IconColor = System.Drawing.Color.White;
            this.btnSearch.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnSearch.IconSize = 24;
            this.btnSearch.Location = new System.Drawing.Point(387, 2);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(49, 31);
            this.btnSearch.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnSearch.TabIndex = 18;
            this.btnSearch.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnSearch.UseVisualStyleBackColor = false;
            // 
            // tbSearch
            // 
            this.tbSearch._Customizable = false;
            this.tbSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.tbSearch.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbSearch.BorderFocusColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(120)))), ((int)(((byte)(218)))));
            this.tbSearch.BorderRadius = 5;
            this.tbSearch.BorderSize = 1;
            this.tbSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbSearch.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbSearch.Location = new System.Drawing.Point(0, 0);
            this.tbSearch.MultiLine = false;
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Padding = new System.Windows.Forms.Padding(10, 7, 10, 7);
            this.tbSearch.PasswordChar = false;
            this.tbSearch.PlaceHolderColor = System.Drawing.Color.DarkGray;
            this.tbSearch.PlaceHolderText = "Search";
            this.tbSearch.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.tbSearch.Size = new System.Drawing.Size(437, 34);
            this.tbSearch.Style = RJCodeUI_M1.RJControls.TextBoxStyle.MatteBorder;
            this.tbSearch.TabIndex = 17;
            // 
            // pnRecipeNew
            // 
            this.pnRecipeNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.pnRecipeNew.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnRecipeNew.Controls.Add(this.cbRecipeRefList);
            this.pnRecipeNew.Controls.Add(this.rjLabel3);
            this.pnRecipeNew.Controls.Add(this.rjLabel2);
            this.pnRecipeNew.Controls.Add(this.rjLabel1);
            this.pnRecipeNew.Controls.Add(this.lblTitle);
            this.pnRecipeNew.Controls.Add(this.tbRecipeNewNo);
            this.pnRecipeNew.Controls.Add(this.tbRecipeNewName);
            this.pnRecipeNew.Controls.Add(this.btnNewRecipeCancel);
            this.pnRecipeNew.Controls.Add(this.btnNewRecipeCreate);
            this.pnRecipeNew.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnRecipeNew.Location = new System.Drawing.Point(0, 227);
            this.pnRecipeNew.Name = "pnRecipeNew";
            this.pnRecipeNew.Size = new System.Drawing.Size(517, 193);
            this.pnRecipeNew.TabIndex = 2134;
            this.pnRecipeNew.Visible = false;
            // 
            // cbRecipeRefList
            // 
            this.cbRecipeRefList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.None;
            this.cbRecipeRefList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.None;
            this.cbRecipeRefList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.cbRecipeRefList.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.cbRecipeRefList.BorderRadius = 0;
            this.cbRecipeRefList.BorderSize = 2;
            this.cbRecipeRefList.Customizable = true;
            this.cbRecipeRefList.DataSource = null;
            this.cbRecipeRefList.DropDownBackColor = System.Drawing.SystemColors.Window;
            this.cbRecipeRefList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRecipeRefList.DropDownTextColor = System.Drawing.SystemColors.WindowText;
            this.cbRecipeRefList.IconColor = System.Drawing.Color.CornflowerBlue;
            this.cbRecipeRefList.Location = new System.Drawing.Point(162, 119);
            this.cbRecipeRefList.Name = "cbRecipeRefList";
            this.cbRecipeRefList.Padding = new System.Windows.Forms.Padding(3);
            this.cbRecipeRefList.SelectedIndex = -1;
            this.cbRecipeRefList.Size = new System.Drawing.Size(324, 32);
            this.cbRecipeRefList.Style = RJCodeUI_M1.RJControls.ControlStyle.Glass;
            this.cbRecipeRefList.TabIndex = 2139;
            this.cbRecipeRefList.Texts = "";
            // 
            // rjLabel3
            // 
            this.rjLabel3.AutoSize = true;
            this.rjLabel3.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel3.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel3.LinkLabel = false;
            this.rjLabel3.Location = new System.Drawing.Point(28, 124);
            this.rjLabel3.Name = "rjLabel3";
            this.rjLabel3.Size = new System.Drawing.Size(94, 16);
            this.rjLabel3.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel3.TabIndex = 2113;
            this.rjLabel3.Text = "Model Copy :";
            // 
            // rjLabel2
            // 
            this.rjLabel2.AutoSize = true;
            this.rjLabel2.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel2.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel2.LinkLabel = false;
            this.rjLabel2.Location = new System.Drawing.Point(26, 88);
            this.rjLabel2.Name = "rjLabel2";
            this.rjLabel2.Size = new System.Drawing.Size(97, 16);
            this.rjLabel2.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel2.TabIndex = 2112;
            this.rjLabel2.Text = "Model Name :";
            // 
            // rjLabel1
            // 
            this.rjLabel1.AutoSize = true;
            this.rjLabel1.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.rjLabel1.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.rjLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.rjLabel1.LinkLabel = false;
            this.rjLabel1.Location = new System.Drawing.Point(26, 54);
            this.rjLabel1.Name = "rjLabel1";
            this.rjLabel1.Size = new System.Drawing.Size(94, 16);
            this.rjLabel1.Style = RJCodeUI_M1.RJControls.LabelStyle.Normal;
            this.rjLabel1.TabIndex = 2111;
            this.rjLabel1.Text = "Model Code :";
            // 
            // lblTitle
            // 
            this.lblTitle.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.lblTitle.Font = new System.Drawing.Font("Verdana", 14F);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.lblTitle.LinkLabel = false;
            this.lblTitle.Location = new System.Drawing.Point(116, 10);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(267, 23);
            this.lblTitle.Style = RJCodeUI_M1.RJControls.LabelStyle.Title;
            this.lblTitle.TabIndex = 2110;
            this.lblTitle.Text = "Add Model";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // tbRecipeNewNo
            // 
            this.tbRecipeNewNo._Customizable = false;
            this.tbRecipeNewNo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.tbRecipeNewNo.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbRecipeNewNo.BorderFocusColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(120)))), ((int)(((byte)(218)))));
            this.tbRecipeNewNo.BorderRadius = 10;
            this.tbRecipeNewNo.BorderSize = 1;
            this.tbRecipeNewNo.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.tbRecipeNewNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbRecipeNewNo.Location = new System.Drawing.Point(162, 47);
            this.tbRecipeNewNo.MultiLine = false;
            this.tbRecipeNewNo.Name = "tbRecipeNewNo";
            this.tbRecipeNewNo.Padding = new System.Windows.Forms.Padding(10, 7, 10, 7);
            this.tbRecipeNewNo.PasswordChar = false;
            this.tbRecipeNewNo.PlaceHolderColor = System.Drawing.Color.DarkGray;
            this.tbRecipeNewNo.PlaceHolderText = null;
            this.tbRecipeNewNo.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.tbRecipeNewNo.Size = new System.Drawing.Size(324, 31);
            this.tbRecipeNewNo.Style = RJCodeUI_M1.RJControls.TextBoxStyle.MatteBorder;
            this.tbRecipeNewNo.TabIndex = 2109;
            // 
            // tbRecipeNewName
            // 
            this.tbRecipeNewName._Customizable = false;
            this.tbRecipeNewName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.tbRecipeNewName.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbRecipeNewName.BorderFocusColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(120)))), ((int)(((byte)(218)))));
            this.tbRecipeNewName.BorderRadius = 10;
            this.tbRecipeNewName.BorderSize = 1;
            this.tbRecipeNewName.Font = new System.Drawing.Font("Verdana", 9.5F);
            this.tbRecipeNewName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(132)))), ((int)(((byte)(129)))), ((int)(((byte)(132)))));
            this.tbRecipeNewName.Location = new System.Drawing.Point(162, 82);
            this.tbRecipeNewName.MultiLine = false;
            this.tbRecipeNewName.Name = "tbRecipeNewName";
            this.tbRecipeNewName.Padding = new System.Windows.Forms.Padding(10, 7, 10, 7);
            this.tbRecipeNewName.PasswordChar = false;
            this.tbRecipeNewName.PlaceHolderColor = System.Drawing.Color.DarkGray;
            this.tbRecipeNewName.PlaceHolderText = null;
            this.tbRecipeNewName.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.tbRecipeNewName.Size = new System.Drawing.Size(324, 31);
            this.tbRecipeNewName.Style = RJCodeUI_M1.RJControls.TextBoxStyle.MatteBorder;
            this.tbRecipeNewName.TabIndex = 2108;
            // 
            // btnNewRecipeCancel
            // 
            this.btnNewRecipeCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNewRecipeCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(110)))), ((int)(((byte)(134)))));
            this.btnNewRecipeCancel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(110)))), ((int)(((byte)(134)))));
            this.btnNewRecipeCancel.BorderRadius = 10;
            this.btnNewRecipeCancel.BorderSize = 1;
            this.btnNewRecipeCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNewRecipeCancel.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnNewRecipeCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnNewRecipeCancel.FlatAppearance.BorderSize = 0;
            this.btnNewRecipeCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(97)))), ((int)(((byte)(103)))), ((int)(((byte)(125)))));
            this.btnNewRecipeCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(96)))), ((int)(((byte)(117)))));
            this.btnNewRecipeCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRecipeCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNewRecipeCancel.ForeColor = System.Drawing.Color.White;
            this.btnNewRecipeCancel.IconChar = FontAwesome.Sharp.IconChar.TimesCircle;
            this.btnNewRecipeCancel.IconColor = System.Drawing.Color.White;
            this.btnNewRecipeCancel.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnNewRecipeCancel.IconSize = 24;
            this.btnNewRecipeCancel.Location = new System.Drawing.Point(336, 153);
            this.btnNewRecipeCancel.Name = "btnNewRecipeCancel";
            this.btnNewRecipeCancel.Size = new System.Drawing.Size(164, 35);
            this.btnNewRecipeCancel.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnNewRecipeCancel.TabIndex = 2106;
            this.btnNewRecipeCancel.Text = "Cancel";
            this.btnNewRecipeCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnNewRecipeCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnNewRecipeCancel.UseVisualStyleBackColor = false;
            this.btnNewRecipeCancel.Click += new System.EventHandler(this.btnNewRecipeCancel_Click);
            // 
            // btnNewRecipeCreate
            // 
            this.btnNewRecipeCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNewRecipeCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.btnNewRecipeCreate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnNewRecipeCreate.BorderRadius = 10;
            this.btnNewRecipeCreate.BorderSize = 1;
            this.btnNewRecipeCreate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNewRecipeCreate.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnNewRecipeCreate.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.btnNewRecipeCreate.FlatAppearance.BorderSize = 0;
            this.btnNewRecipeCreate.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(149)))), ((int)(((byte)(106)))));
            this.btnNewRecipeCreate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(139)))), ((int)(((byte)(99)))));
            this.btnNewRecipeCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRecipeCreate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNewRecipeCreate.ForeColor = System.Drawing.Color.White;
            this.btnNewRecipeCreate.IconChar = FontAwesome.Sharp.IconChar.Plus;
            this.btnNewRecipeCreate.IconColor = System.Drawing.Color.White;
            this.btnNewRecipeCreate.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnNewRecipeCreate.IconSize = 24;
            this.btnNewRecipeCreate.Location = new System.Drawing.Point(180, 153);
            this.btnNewRecipeCreate.Name = "btnNewRecipeCreate";
            this.btnNewRecipeCreate.Size = new System.Drawing.Size(156, 35);
            this.btnNewRecipeCreate.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnNewRecipeCreate.TabIndex = 2106;
            this.btnNewRecipeCreate.Text = "Add";
            this.btnNewRecipeCreate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnNewRecipeCreate.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnNewRecipeCreate.UseVisualStyleBackColor = false;
            this.btnNewRecipeCreate.Click += new System.EventHandler(this.btnNewRecipeCreate_Click);
            // 
            // rjPanel1
            // 
            this.rjPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(252)))), ((int)(((byte)(253)))));
            this.rjPanel1.BorderRadius = 0;
            this.rjPanel1.Controls.Add(this.btnRecipeDel);
            this.rjPanel1.Controls.Add(this.btnRecipeNew);
            this.rjPanel1.Controls.Add(this.btnRecipeSelect);
            this.rjPanel1.Customizable = false;
            this.rjPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.rjPanel1.Location = new System.Drawing.Point(0, 167);
            this.rjPanel1.Name = "rjPanel1";
            this.rjPanel1.Size = new System.Drawing.Size(517, 60);
            this.rjPanel1.TabIndex = 2132;
            // 
            // btnRecipeDel
            // 
            this.btnRecipeDel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRecipeDel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnRecipeDel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnRecipeDel.BorderRadius = 10;
            this.btnRecipeDel.BorderSize = 1;
            this.btnRecipeDel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecipeDel.Design = RJCodeUI_M1.RJControls.ButtonDesign.Delete;
            this.btnRecipeDel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(79)))), ((int)(((byte)(82)))));
            this.btnRecipeDel.FlatAppearance.BorderSize = 0;
            this.btnRecipeDel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(74)))), ((int)(((byte)(77)))));
            this.btnRecipeDel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(69)))), ((int)(((byte)(72)))));
            this.btnRecipeDel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecipeDel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRecipeDel.ForeColor = System.Drawing.Color.White;
            this.btnRecipeDel.IconChar = FontAwesome.Sharp.IconChar.TrashAlt;
            this.btnRecipeDel.IconColor = System.Drawing.Color.White;
            this.btnRecipeDel.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnRecipeDel.IconSize = 24;
            this.btnRecipeDel.Location = new System.Drawing.Point(169, 14);
            this.btnRecipeDel.Name = "btnRecipeDel";
            this.btnRecipeDel.Size = new System.Drawing.Size(168, 35);
            this.btnRecipeDel.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnRecipeDel.TabIndex = 2122;
            this.btnRecipeDel.Text = "Delete";
            this.btnRecipeDel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRecipeDel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecipeDel.UseVisualStyleBackColor = false;
            this.btnRecipeDel.Click += new System.EventHandler(this.btnRecipeDel_Click);
            // 
            // btnRecipeNew
            // 
            this.btnRecipeNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRecipeNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.btnRecipeNew.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnRecipeNew.BorderRadius = 10;
            this.btnRecipeNew.BorderSize = 1;
            this.btnRecipeNew.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecipeNew.Design = RJCodeUI_M1.RJControls.ButtonDesign.Custom;
            this.btnRecipeNew.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(159)))), ((int)(((byte)(113)))));
            this.btnRecipeNew.FlatAppearance.BorderSize = 0;
            this.btnRecipeNew.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(149)))), ((int)(((byte)(106)))));
            this.btnRecipeNew.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(139)))), ((int)(((byte)(99)))));
            this.btnRecipeNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecipeNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRecipeNew.ForeColor = System.Drawing.Color.White;
            this.btnRecipeNew.IconChar = FontAwesome.Sharp.IconChar.Plus;
            this.btnRecipeNew.IconColor = System.Drawing.Color.White;
            this.btnRecipeNew.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnRecipeNew.IconSize = 24;
            this.btnRecipeNew.Location = new System.Drawing.Point(0, 14);
            this.btnRecipeNew.Name = "btnRecipeNew";
            this.btnRecipeNew.Size = new System.Drawing.Size(168, 35);
            this.btnRecipeNew.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnRecipeNew.TabIndex = 2121;
            this.btnRecipeNew.Text = "Add";
            this.btnRecipeNew.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRecipeNew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecipeNew.UseVisualStyleBackColor = false;
            this.btnRecipeNew.Click += new System.EventHandler(this.btnRecipeNew_Click);
            // 
            // btnRecipeSelect
            // 
            this.btnRecipeSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRecipeSelect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnRecipeSelect.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnRecipeSelect.BorderRadius = 10;
            this.btnRecipeSelect.BorderSize = 1;
            this.btnRecipeSelect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecipeSelect.Design = RJCodeUI_M1.RJControls.ButtonDesign.IconButton;
            this.btnRecipeSelect.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.btnRecipeSelect.FlatAppearance.BorderSize = 0;
            this.btnRecipeSelect.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(91)))), ((int)(((byte)(199)))));
            this.btnRecipeSelect.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(85)))), ((int)(((byte)(186)))));
            this.btnRecipeSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecipeSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRecipeSelect.ForeColor = System.Drawing.Color.White;
            this.btnRecipeSelect.IconChar = FontAwesome.Sharp.IconChar.BookMedical;
            this.btnRecipeSelect.IconColor = System.Drawing.Color.White;
            this.btnRecipeSelect.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnRecipeSelect.IconSize = 24;
            this.btnRecipeSelect.Location = new System.Drawing.Point(338, 14);
            this.btnRecipeSelect.Name = "btnRecipeSelect";
            this.btnRecipeSelect.Size = new System.Drawing.Size(167, 35);
            this.btnRecipeSelect.Style = RJCodeUI_M1.RJControls.ControlStyle.Solid;
            this.btnRecipeSelect.TabIndex = 2123;
            this.btnRecipeSelect.Text = "Select";
            this.btnRecipeSelect.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRecipeSelect.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecipeSelect.UseVisualStyleBackColor = false;
            this.btnRecipeSelect.Click += new System.EventHandler(this.btnRecipeSelect_Click);
            // 
            // FormRecipeManager
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(97)))), ((int)(((byte)(212)))));
            this.BorderSize = 1;
            this.Caption = "MODEL";
            this.ClientSize = new System.Drawing.Size(960, 939);
            this.MaximizeBox = false;
            this.Name = "FormRecipeManager";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Text = "MODEL";
            this.Load += new System.EventHandler(this.FormRecipe_Load);
            this.Resize += new System.EventHandler(this.FormRecipeManager_Resize);
            this.pnlClientArea.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecipeList)).EndInit();
            this.rjPanel2.ResumeLayout(false);
            this.rjPanel2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.pnRecipeNew.ResumeLayout(false);
            this.pnRecipeNew.PerformLayout();
            this.rjPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private RJCodeUI_M1.RJControls.RJDataGridView dgvRecipeList;
        private RJCodeUI_M1.RJControls.RJLabel lbSelectedModel;
        private RJCodeUI_M1.RJControls.RJLabel lbLastUpdateTime;
        private RJCodeUI_M1.RJControls.RJLabel lbModelNo;
        private RJCodeUI_M1.RJControls.RJLabel lbModelName;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel7;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel6;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel5;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel4;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel pnRecipeNew;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel3;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel2;
        private RJCodeUI_M1.RJControls.RJLabel rjLabel1;
        private RJCodeUI_M1.RJControls.RJLabel lblTitle;
        private RJCodeUI_M1.RJControls.RJTextBox tbRecipeNewNo;
        private RJCodeUI_M1.RJControls.RJTextBox tbRecipeNewName;
        private RJCodeUI_M1.RJControls.RJButton btnNewRecipeCancel;
        private RJCodeUI_M1.RJControls.RJButton btnNewRecipeCreate;
        private RJCodeUI_M1.RJControls.RJPanel rjPanel1;
        private RJCodeUI_M1.RJControls.RJButton btnRecipeDel;
        private RJCodeUI_M1.RJControls.RJButton btnRecipeNew;
        private RJCodeUI_M1.RJControls.RJButton btnRecipeSelect;
        private RJCodeUI_M1.RJControls.RJComboBox cbRecipeRefList;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private RJCodeUI_M1.RJControls.RJButton btnSearch;
        private RJCodeUI_M1.RJControls.RJTextBox tbSearch;
    }
}