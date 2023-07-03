using System;
using System.Windows.Forms;
using System.Reflection;
using Keys = System.Windows.Forms.Keys;
using RJCodeUI_M1.RJForms;
using System.Text;
using Lib.Common;
using System.Linq;
using System.Drawing;
using static MvcVisionSystem.CSystem;
using MvcVisionSystem.Yolo;
using Sunny.UI;
using System.Collections.Generic;

namespace MvcVisionSystem
{
    public partial class FormVision_ClassMenu : RJChildForm
    {
        // UpdateControl delegate의 이벤트를 선언합니다.
        public event UpdateControl OnButtonClicked;

        public FormVision_ClassMenu()
        {
            InitializeComponent();                                    
            this.TopLevel = true;
            this.TopMost = true;

            ShowImagesList();
        }

        private void ShowImagesList()
        {
            dgvImagesList.Rows.Clear();
            for (int i = 0; i < CGlobal.Inst.Data.ClassNamedList.Count; i++)
            {
                object[] row = new object[] { (i + 1), CGlobal.Inst.Data.ClassNamedList[i].Text };
                dgvImagesList.Rows.Add(row);
            }

            // 버튼이 클릭되면 OnButtonClicked 이벤트를 발생시킵니다.
            OnButtonClicked?.Invoke();
        }

        private void FormSettings_Camera_Load(object sender, EventArgs e)
        {
            InitEvent();                        
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            Keys key = keyData & ~(Keys.Shift | Keys.Control);

            switch (key)
            {
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return true;
                case Keys.Enter:
                    btnNewCreate_Click(null, null);
                    return true;                               
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private bool InitEvent()
        {
            try
            {
                this.KeyPreview = true;
                this.KeyDown += Form_KeyDown;
                CLOG.NORMAL( $"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL( $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }

        private void btnNewCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();            
        }

        private void btnNewCreate_Click(object sender, EventArgs e)
        {
            string names = txtNames.Text;

            // 랜덤 색상 생성
            Random rand = new Random();
            //Color randomColor = Color.FromArgb(128, rand.Next(256), rand.Next(256), rand.Next(256));
            Color randomColor = Color.FromArgb(128, System.Drawing.Color.Green.R, Color.Green.G, Color.Green.B);

            // Text와 Color 둘 다 중복되지 않는지 확인
            if (!CGlobal.Inst.Data.ClassNamedList.Any(x => x.Text == names))
            {
                CGlobal.Inst.Data.ClassNamedList.Add(new Yolo.CClassItem() { Text = names, DrawColor = randomColor });
                CGlobal.Inst.Data.SaveConfig(CGlobal.Inst.Recipe.Name);

                string trainPath = Application.StartupPath + "\\train";
                string valPath = Application.StartupPath + "\\valid";
                List<string> textList = CGlobal.Inst.Data.ClassNamedList.Select(item => item.Text).ToList();

                CYolov5.CreateYaml(trainPath, valPath, textList, Application.StartupPath + "\\data.yaml");
            }

            ShowImagesList();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {            
             DataGridViewRow row = dgvImagesList.SelectedRows[0]; //선택된 Row 값 가져옴.
            string targetText = row.Cells[1].Value.ToString(); // row의 컬럼
            CGlobal.Inst.Data.ClassNamedList.RemoveAll(x => x.Text == targetText);
            CGlobal.Inst.Data.SaveConfig(CGlobal.Inst.Recipe.Name);

            string trainPath = Application.StartupPath + "\\train";
            string valPath = Application.StartupPath + "\\valid";
            List<string> textList = CGlobal.Inst.Data.ClassNamedList.Select(item => item.Text).ToList();

            CYolov5.CreateYaml(trainPath, valPath, textList, Application.StartupPath + "\\data.yaml");

            ShowImagesList();
        }

        private void dgvImagesList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {            
            string targetText = dgvImagesList[1, e.RowIndex].Value.ToString();
            txtNames.Text = targetText;
        }

        private void dgvImagesList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string targetText = dgvImagesList[1, e.RowIndex].Value.ToString();
            txtNames.Text = targetText;
        }
    }
 }

