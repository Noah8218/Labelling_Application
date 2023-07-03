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

namespace MvcVisionSystem
{
    public partial class FormImageList : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        private CGlobal Global = CGlobal.Inst;
        private int PanelCount = 0;
        public FormImageList()
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
            dgvImagesList.Update();

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
            ShowImageDgv(new List<string>());
        }

        private void OnCamUpdate(object sender, EventArgs e)
        {
            this.UIThreadBeginInvoke(() =>
            {
                //tbExposure.Text = Global.Device.CAMERAS[CDisplayManager.CameraIndex].Property.EXPOSURETIME_US.ToString();
                //tbGain.Text = Global.Device.CAMERAS[CDisplayManager.CameraIndex].Property.GAIN.ToString();
            });
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            Lib.Common.CUtil.LoadFolderPath(out string folderPath);
            List<string> imagePaths =  GetImageFiles(folderPath);
            ShowImageDgv(imagePaths);
        }

        private void ShowImageDgv(List<string> imagePaths)
        {
            // DataGridView 설정
            dgvImagesList.Columns.Clear();  // 기존의 모든 컬럼을 제거
            DataGridViewImageColumn imgColumn = new DataGridViewImageColumn();  // 이미지 컬럼을 추가
            imgColumn.HeaderText = "이미지";
            imgColumn.Name = "이미지";
            imgColumn.ImageLayout = DataGridViewImageCellLayout.Stretch;
            dgvImagesList.Columns.Add("No", "No");
            dgvImagesList.Columns.Add(imgColumn);
            dgvImagesList.Columns.Add("파일 경로", "파일 경로");
            dgvImagesList.RowTemplate.Height = 100;  // 적절한 높이를 설정하세요


            foreach (var path in imagePaths)
            {
                // 파일 경로에서 이미지를 로드
                Image image = Image.FromFile(path);
                string fileName = Path.GetFileName(path);                
                // DataGridView에 이미지와 경로를 추가
                int index = dgvImagesList.Rows.Count + 1;
                object[] row = new object[] { index.ToString(), image, fileName };
                dgvImagesList.Rows.Add(row);
            }

            dgvImagesList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvImagesList.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvImagesList.Columns[0].Width = 40;
        }

        public  List<string> GetImageFiles(string folderPath)
        {
            string[] SUPPORTED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            // 파일 목록을 가져올 리스트를 생성
            List<string> imageFiles = new List<string>();

            // 폴더가 존재하는지 확인
            if (Directory.Exists(folderPath))
            {
                // 해당 폴더에 있는 모든 파일들을 가져온다
                string[] files = Directory.GetFiles(folderPath);

                // 각 파일에 대해
                foreach (var file in files)
                {
                    // 파일 확장자를 가져온다
                    var extension = Path.GetExtension(file).ToLower();

                    // 이미지 파일이면 리스트에 추가한다
                    if (Array.IndexOf(SUPPORTED_EXTENSIONS, extension) > -1)
                    {
                        imageFiles.Add(file);
                    }
                }
            }
            else { Console.WriteLine("Directory does not exist."); }            
            return imageFiles;
        }

        private void uiSplitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void dgvImagesList_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 선택한 셀의 위치를 확인
                int columnIndex = e.ColumnIndex;
                int rowIndex = e.RowIndex;

                // 올바른 위치에서 더블 클릭했는지 확인
                if (columnIndex >= 0 && rowIndex >= 0)
                {
                    if(dgvImagesList.Columns[columnIndex].Name == "이미지")
                    {
                        // 선택한 셀의 이미지 경로를 가져옵니다.
                        Image image = (Image)dgvImagesList.Rows[rowIndex].Cells[columnIndex].Value;
                        string fileName = (string)dgvImagesList.Rows[rowIndex].Cells[columnIndex + 1].Value;
                        CGlobal.Inst.Data.LastSelectImageName = fileName;
                        CDisplayManager.ImageSrc = Lib.Common.CImageConverter.ToMat((Bitmap)image);                        
                        CDisplayManager.CreateLayerDisplay((Bitmap)image, "Main", true);
                        CDisplayManager.ZoomToFit("Main");
                    }                    
                }


            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                CCommon.ShowMessageBox("EXCEPTION", $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }
    }
}
