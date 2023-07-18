using Accord.Math;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._2._Common;
using Lib.Common;
using Lib.OpenCV;
using Lib.OpenCV.Blob;
using Lib.OpenCV.Result;
using Lib.OpenCV.Tool;
using OpenCvSharp;
using RJCodeUI_M1.RJControls;
using Sunny.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using MvcVisionSystem.Yolo;
using System.Web.UI.WebControls;

namespace MvcVisionSystem
{
    public partial class FormClassList : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        private CGlobal Global = CGlobal.Inst;
        private int PanelCount = 0;
        public FormClassList()
        {
            InitializeComponent();

            CloseButton = false;
            CloseButtonVisible = false;

            
            Global.System.OnDataUpdated += System_OnDataUpdated;
        }

        private void System_OnDataUpdated()
        {
            ShowClassItems();
        }

        public void ShowClassItems()
        {
            dgvImagesList.Rows.Clear();
            foreach (var item in CDisplayManager.Displays[DEFINE.Main].viewer._RoiDic)
            {
                for(int i =0; i < item.Value.Count;i++)
                {
                    int index = dgvImagesList.Rows.Count + 1;
                    object[] row = new object[] { index.ToString(), item.Value[i].cClassItem.Text, item.Value[i].Roi.ToString() };
                    dgvImagesList.Rows.Add(row);
                }                
            }
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

        private void Form_Load(object sender, EventArgs e)
        {            
            CDisplayManager.EventUpdateCam += OnCamUpdate;
            //ShowImageDgv(new List<string>());
        }

        private void OnCamUpdate(object sender, EventArgs e)
        {
            this.UIThreadBeginInvoke(() =>
            {
                //tbExposure.Text = Global.Device.CAMERAS[CDisplayManager.CameraIndex].Property.EXPOSURETIME_US.ToString();
                //tbGain.Text = Global.Device.CAMERAS[CDisplayManager.CameraIndex].Property.GAIN.ToString();
            });
        }

    }
}
