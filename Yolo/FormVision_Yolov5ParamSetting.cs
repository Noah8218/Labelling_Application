using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lib.Common;
using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using RJCodeUI_M1.RJForms;
using RJCodeUI_M1.Settings;
using RJCodeUI_M1.Utils;
using static MvcVisionSystem._3._Device.TCP.CCommunicationLearning;

namespace RJCodeUI_M1.RJForms
{
    public partial class FormVision_Yolov5ParamSetting : RJChildForm
    {
        /// <summary>
        /// This class inherits from the <see cref="RJChildForm"/>  class
        /// </summary>
        /// 

        #region -> Constructor

        public FormVision_Yolov5ParamSetting()
        {
            //This form was built by the designer.
            InitializeComponent();
        }
        #endregion

        #region -> Event Methods

        private void lblRestartApp_Click(object sender, EventArgs e)
        {//Restart application

            Application.Restart();
            Environment.Exit(0);
        }
        #endregion

        private void FormVision_Yolov5ParamSetting_Load(object sender, EventArgs e)
        {
            tbImageSize.Text = CGlobal.Inst.Data.TranningParam.imageSize.ToString();
            tbbatch.Text = CGlobal.Inst.Data.TranningParam.batch.ToString();
            tbepoch.Text = CGlobal.Inst.Data.TranningParam.epoch.ToString();

            foreach (string item in Enum.GetNames(typeof(CYolov5TranningParam.Cfg)))
            {
                cbcfg.Items.Add(item);
            }

            string enumString = Enum.GetName(typeof(CYolov5TranningParam.Cfg), CGlobal.Inst.Data.TranningParam.cfg);

            for (int i = 0; i < cbcfg.Items.Count; i++)
            {
                if (cbcfg.Items[i].ToString() == enumString)
                {
                    cbcfg.SelectedIndex = i;
                    break;
                }
            }

            foreach (string item in Enum.GetNames(typeof(CYolov5TranningParam.Weight)))
            {
                cbweight.Items.Add(item);
            }

            enumString = Enum.GetName(typeof(CYolov5TranningParam.Weight), CGlobal.Inst.Data.TranningParam.weight);

            for (int i = 0; i < cbweight.Items.Count; i++)
            {
                if (cbweight.Items[i].ToString() == enumString)
                {
                    cbweight.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnApplyChanges_Click(object sender, EventArgs e)
        {
            try
            {
                int img = int.Parse(tbImageSize.Text);
                int batch = int.Parse(tbbatch.Text);
                int epoch = int.Parse(tbepoch.Text);
                var cfg = Lib.Common.CUtil.ParseEnum<CYolov5TranningParam.Cfg>(cbcfg.SelectedItem.ToString());
                var weight = Lib.Common.CUtil.ParseEnum<CYolov5TranningParam.Weight>(cbweight.SelectedItem.ToString());

                CGlobal.Inst.Data.TranningParam.imageSize = img;
                CGlobal.Inst.Data.TranningParam.batch = batch;
                CGlobal.Inst.Data.TranningParam.epoch = epoch;
                CGlobal.Inst.Data.TranningParam.cfg = cfg;
                CGlobal.Inst.Data.TranningParam.weight = weight;
                CGlobal.Inst.Data.SaveConfig(CGlobal.Inst.Recipe.Name);

                // 훈련 데이터를 같이 보내야함
                CGlobal.Inst.DeepLearning.SendTrainingData(CommandLearning.StartTraining.ToString(), img.ToString(), batch.ToString(), epoch.ToString(),
                    cfg.ToString()+".yaml", weight.ToString()+".pt");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");                
            }
        }

        private void rjButton1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
