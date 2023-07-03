using MvcVisionSystem._1._Core;
using Lib.Common;
using Lib.OpenCV;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Media;

namespace MvcVisionSystem
{
    public partial class FormThreshold : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        private CGlobal Global = CGlobal.Inst;
        public FormThreshold()
        {           
            InitializeComponent();

            CloseButton = false;
            CloseButtonVisible = false;
        }
     
        // If the content won't display nicely, hide it
        private void ResizeEvent(object sender, EventArgs e)
        {
            this.Visible = this.Width > this.MinimumSize.Width && this.Height > this.MinimumSize.Height;
        }

        private bool ChangeSize = false;

        private void Form_VisibleChanged(object sender, EventArgs e)
        {
            if (!ChangeSize)
            {
                if (DockHandler.FloatPane == null) { return; }
                DockHandler.FloatPane.FloatWindow.Bounds = new Rectangle(DockHandler.FloatPane.FloatWindow.Bounds.X, DockHandler.FloatPane.FloatWindow.Bounds.Y, 800, 400);
                this.Refresh();
                ChangeSize = true;
            }
        }

        public void InitThresholdMenu()
        {
            cbThresholdMenu.Items.Add(ThresholdTypes.Binary);
            cbThresholdMenu.Items.Add(ThresholdTypes.BinaryInv);
            cbThresholdMenu.Items.Add(ThresholdTypes.Mask);
            cbThresholdMenu.Items.Add(ThresholdTypes.Otsu);
            cbThresholdMenu.Items.Add(ThresholdTypes.Tozero);
            cbThresholdMenu.Items.Add(ThresholdTypes.TozeroInv);
            cbThresholdMenu.Items.Add(ThresholdTypes.Triangle);
            cbThresholdMenu.Items.Add(ThresholdTypes.Trunc);
            cbThresholdMenu.SelectedIndex = 0;

            cbAdaptiveThresholdMenu.Items.Add(ThresholdTypes.Binary);
            cbAdaptiveThresholdMenu.Items.Add(ThresholdTypes.BinaryInv);
            cbAdaptiveThresholdMenu.SelectedIndex = 0;

            cbAdaptiveType.Items.Add(AdaptiveThresholdTypes.MeanC);
            cbAdaptiveType.Items.Add(AdaptiveThresholdTypes.GaussianC);
            cbAdaptiveType.SelectedIndex = 0;
        }

        private void Form_Load(object sender, EventArgs e)
        {
            InitThresholdMenu();
        }

        private void trbThreshold_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (COpenCVHelper.IsImageEmpty(CDisplayManager.ImageSrc)) return;

                using (Mat imageSrc = CDisplayManager.SelecteItem != DEFINE.Threshold ? CDisplayManager.ImageSrc.Clone() : Lib.Common.CImageConverter.ToMat((Bitmap)CDisplayManager.Displays[CDisplayManager.FindIndex("Main")].viewer._Ib.Image).Clone())
                {
                                  
                }
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL( $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }

        private void trbDoubleThresholdMax_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (COpenCVHelper.IsImageEmpty(CDisplayManager.ImageSrc)) return;

                int Min = trbDoubleThresholdMin.Value;
                int Max = trbDoubleThresholdMax.Value;

                using (Mat imageSrc = CDisplayManager.SelecteItem != DEFINE.Threshold ? CDisplayManager.ImageSrc.Clone() : Lib.Common.CImageConverter.ToMat((Bitmap)CDisplayManager.Displays[CDisplayManager.FindIndex("Main")].viewer._Ib.Image).Clone())
                {
         
                }
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL( $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }

        private void trbAdaptiveThreshold_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (COpenCVHelper.IsImageEmpty(CDisplayManager.ImageSrc)) return;

                if (tbBlockSize.Text == "") { tbBlockSize.Text = "25"; }
                if (tbWeight.Text == "") { tbWeight.Text = "5"; }

                int Block = int.Parse(tbBlockSize.Text);
                int Weight = int.Parse(tbWeight.Text);

                using (Mat imageSrc = CDisplayManager.SelecteItem != DEFINE.Threshold ? CDisplayManager.ImageSrc.Clone() : Lib.Common.CImageConverter.ToMat((Bitmap)CDisplayManager.Displays[CDisplayManager.FindIndex("Main")].viewer._Ib.Image).Clone())
                {
                    COpenCVHelper.SetImageChannel1(imageSrc);
                    
                
                }
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL( $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }
    }
}
