using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using RJCodeUI_M1.RJControls;
using System.Collections.Generic;
using Cyotek.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Threading.Tasks;
using static MvcVisionSystem.DEFINE;
using Lib.Common;
using RJCodeUI_M1.Settings;
using RJCodeUI_M1.RJForms;
using Lib.OpenCV;
using System.Linq;
using static MvcVisionSystem.CSystem;
using MvcVisionSystem.Yolo;
using System.IO;
using MvcVisionSystem._1._Core;
using static MvcVisionSystem._3._Device.TCP.CCommunicationLearning;

namespace MvcVisionSystem
{
    public partial class FormMetroFrame : Form
    {
        private CGlobal Global = CGlobal.Inst;
        private DockPanel OperDockPanel;

        private Dictionary<MAIN_DOCK_FORM, object> Forms = new Dictionary<MAIN_DOCK_FORM, object>();

        private List<FormLayerDisplay> Displays { get; set; } = new List<FormLayerDisplay>();

        #region MENU
        private FormTeachingVision FrmVision = null;
        private FormInit formInit = null;
        private FormVision_ClassMenu formVision_ClassMenu = null;
        #endregion

        public event UpdateControl OnChangeClassClicked;

        public FormMetroFrame(FormInit formInit)
        {            
            this.formInit = formInit;
            InitializeComponent();
            InitEvent();
            InitConfig();
            // 프로그램 Load때 마지막에 사용한 레시피를 Load
            if (Global.System.LastRecipe != "" && Global.System.LastRecipe != "\r\n\t") { Global.Recipe.Name = Global.System.LastRecipe; }
            InitDevice();

            //btnAuthoriztionName.Text = Global.System.Authorization.ToString();
        }

        private void FormMetroFrame_Load(object sender, EventArgs e)
        {
            InitUI();
            InitThread();
            InitIO();
            InitMenu();

            Global.System.Menu = CSystem.MENU.VISION;            
        }

        private void FormMetroFrame_Shown(object sender, EventArgs e) { formInit.Close = true; }

        #region INIT
        private bool InitCamera()
        {
            try
            {
                Bitmap DisconnectedImage = Properties.Resources.Disconnected;

                Global.System.Notice = "Initialize The Init Camera";
                for (int i = 0; i < Global.Device.CAMERA_COUNT; i++)
                {
                    ImageBox imageBox = new ImageBox();
                    Global.Device.CAMERAS[i].ImageManager.LoadImageBox(imageBox, false, true);
                    Global.Device.CAMERAS[i].ImageManager._Ib.Image = DisconnectedImage;
                    Global.Device.CAMERAS[i].ImageManager._Ib.ZoomToFit();
                    Global.Device.CAMERAS[i].EventGrabEnd += OnGrabEnd;
                    Global.Device.CAMERAS[i].ImageManager = Displays[i].viewer;
                }

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }
        
        private bool InitUI()
        {
            try
            {
                pnlTitleBar.BackColor = Color.FromArgb(64, 73, 108);
                pnStatusBar.BackColor = Color.FromArgb(64, 73, 108);
                //pnSideMenu.BackColor = Color.FromArgb(64, 73, 108);
                this.BackColor = Color.FromArgb(64, 73, 108);
                //btnAuthoriztion.BackColor = Color.FromArgb(64, 73, 108);
                btnScreenCapture.BackColor = Color.FromArgb(64, 73, 108);
                btnClassMenu.BackColor = Color.FromArgb(64, 73, 108);
                btnUserOptions.BackColor = Color.FromArgb(64, 73, 108);
                btnClassSave.BackColor = Color.FromArgb(64, 73, 108);
                btnClassTrain.BackColor = Color.FromArgb(64, 73, 108);
                btnClassInfer.BackColor = Color.FromArgb(64, 73, 108);

                this.Location = new System.Drawing.Point(0, 0);
                this.IsMdiContainer = true;

                CLOG.NORMAL("Start S/W");
                Global.System.IF_Handle = this.Handle;
                lbVersion.Text = $"VERSION : {CVersion.VERSION} - {CVersion.DATETIME_UPDATED} ({CVersion.MANAGER})";

                if (FrmVision == null) FrmVision = new FormTeachingVision();
                if (FrmVision != null && !FrmVision.Visible) FrmVision.Show();
                //btnMenuMain.BackColor = DEFINE.MOUSEHOVER_COLOR;
                //btnMenuMain.Tag = CSystem.MENU.MAIN.ToString();
                //btnMenuVision.Tag = CSystem.MENU.VISION.ToString();

                // LINQ를 사용하여 Text만 선택합니다.
                var textList = Global.Data.ClassNamedList.Select(x => x.Text).ToList();
                // ComboBox에 추가합니다.
                cbClassMenu.Items.AddRange(textList.ToArray());
                if (cbClassMenu.Items.Count != 0) { cbClassMenu.SelectedIndex = 0; }

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                System.Windows.Forms.MessageBox.Show(Desc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void ShowMainForms()
        {
            WeifenLuo.WinFormsUI.Docking.DockContent fr;
            foreach (var form in Forms)
            {
                //  DockState.DockLeft : 도킹 방향
                // CAM_TOP.Pane, DockAlignment.Bottom, 0.5 : 
                // CAM_TOP 폼에 도킹 
                // DockAlignment.Bottom => 폼의 하단부분에 도킹
                // 0.5 => 폼에 사이즈 비율 1:1비율임
                fr = (form.Value as WeifenLuo.WinFormsUI.Docking.DockContent);
                switch (form.Key)
                {
                    case MAIN_DOCK_FORM.MainSystem:
                        fr.Show(this.OperDockPanel, DockState.DockLeft);
                        fr.AutoHidePortion = 500;
                        break;
                    case MAIN_DOCK_FORM.Defect:
                        fr.Show(this.OperDockPanel, DockState.DockRight);
                        fr.AutoHidePortion = 500;
                        break;
                    case MAIN_DOCK_FORM.Graph:
                        fr.Show(this.OperDockPanel, DockState.DockLeft);
                        fr.AutoHidePortion = 500;
                        break;
                    case MAIN_DOCK_FORM.IO:
                        fr.Show(this.OperDockPanel, DockState.DockLeftAutoHide);
                        fr.AutoHidePortion = 1200;
                        break;
                    case MAIN_DOCK_FORM.PLC:
                        fr.Show(this.OperDockPanel, DockState.DockLeftAutoHide);
                        fr.AutoHidePortion = 1200;
                        break;
                    case MAIN_DOCK_FORM.CSV:
                        fr.Show(this.OperDockPanel, DockState.DockLeftAutoHide);
                        fr.AutoHidePortion = 700;
                        break;
                    case MAIN_DOCK_FORM.SEARCHDB:
                        fr.Show(this.OperDockPanel, DockState.DockLeftAutoHide);
                        fr.AutoHidePortion = 720;
                        break;
                    case MAIN_DOCK_FORM.LABEL:
                        fr.Show(this.OperDockPanel, DockState.DockRight);
                        fr.AutoHidePortion = 500;
                        break;
                    case MAIN_DOCK_FORM.LOG:
                        fr.Show(this.OperDockPanel, DockState.DockBottomAutoHide);
                        fr.DockState = DockState.DockBottom;
                        fr.AutoHidePortion = 400;
                        break;
                    case MAIN_DOCK_FORM.SUMMARY:
                        DockContent log = (Forms[MAIN_DOCK_FORM.LOG] as DockContent);
                        fr.Show(log.Pane, null);
                        break;
                    case MAIN_DOCK_FORM.BUTTON:
                        fr.Show(this.OperDockPanel, DockState.DockBottomAutoHide);
                        fr.DockState = DockState.DockBottom;
                        fr.AutoHidePortion = 180;
                        break;
                    case MAIN_DOCK_FORM.CAM_TOP:
                        fr.Show(this.OperDockPanel, DockState.Document);
                        break;
                    case MAIN_DOCK_FORM.CAM_BOTTOM:
                        DockContent CAM_TOP = (Forms[MAIN_DOCK_FORM.CAM_TOP] as DockContent);
                        fr.Show(CAM_TOP.Pane, DockAlignment.Bottom, 0.5);
                        break;
                }
            }
            // 텝 활성화
            fr = (Forms[MAIN_DOCK_FORM.MainSystem] as WeifenLuo.WinFormsUI.Docking.DockContent);
            fr.Activate();

            fr = (Forms[MAIN_DOCK_FORM.LOG] as WeifenLuo.WinFormsUI.Docking.DockContent);
            fr.Activate();
        }

        private bool InitIO()
        {
            try
            {
                Global.System.Notice = "Initialize The Init I/O";
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }
            return true;
        }
        private bool InitEvent()
        {
            try
            {
                Global.System.EventChangedMenu += OnChangedMenu;
                Global.System.EventChangedMode += OnChangedMode;
                Global.System.EventChangedAuthorization += OnChangedAuthorization;
                Global.Recipe.EventChagedRecipe += OnChangedRecipe;
                Global.Thread.CSeqVision.EventSeqComplete += OnInspResult;

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }
        private void OnInspResult(object sender, EventArgs e)
        {
            if (!(e is InspResultArgs args)) { return; }
            this.UIThreadBeginInvoke(() =>
            {
                if (Global.System.Menu == CSystem.MENU.MAIN)
                {

                    var task = Task.Run(() =>
                    {
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            Global.Device.CAMERAS[args.Index].ImageManager._Ib.Image = args.imageResult;
                            //Global.Device.CAMERAS[e.Index].ImageManager.ib.ZoomToFit();                            
                            //CLOG.NORMAL($"Inspection Tack Time : {e.tackTime}");
                        }));
                    });
                }
            });
        }

        private void OnChangedMode(object sender, EventArgs e)
        {
            this.UIThreadInvoke(() =>
            {
                switch (Global.System.Mode)
                {
                    case CSystem.MODE.AUTO:


                        break;
                    case CSystem.MODE.READY:

                        break;
                    case CSystem.MODE.ALARM:

                        break;
                }
            });
        }

        private bool InitConfig()
        {
            try
            {
                Global.System.Notice = "Initialize the Config";
                CUtil.InitDirectory("TEST");
                CUtil.InitDirectory("CAPTURE");
                CUtil.InitDirectory("CONFIG");
                CUtil.InitDirectory("CONFIG\\DEVICE");
                CUtil.InitDirectory("RECIPE");
                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }

        private bool InitMenu()
        {
            try
            {                
                FrmVision.TopLevel = false;
                this.pnMDI.Controls.Add(FrmVision);
                FrmVision.Dock = DockStyle.Fill;
                FrmVision.Visible = false;
                FrmVision.Show();

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }

        private bool InitDevice()
        {
            try
            {
                Global.Device = Global.Device.LoadConfig();
                Global.Device.Init();

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }


        private bool InitThread()
        {
            try
            {
                Global.Thread.Start();

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return false;
            }

            return true;
        }
        #endregion

        #region CALL BACK   
        private void OnGrabEnd(object sender, GrabEventArgs e)
        {
            this.UIThreadBeginInvoke(() =>
            {
                try
                {
                    if (Global.System.Menu == CSystem.MENU.MAIN)
                    {
                        if (Global.System.Mode == CSystem.MODE.AUTO)
                        {
                        }
                        else
                        {
                            if (!COpenCVHelper.IsImageEmpty(e.ImageGrab)) { Global.Device.CAMERAS[e.m_Index].ImageManager._Ib.Image = Lib.Common.CImageConverter.ToBitmap(e.ImageGrab); }
                        }
                        //if (!CUtil.IsImageEmpty(e.ImageGrab)) { Global.Device.CAMERAS[e.m_Index].ImageManager._Ib.Image = Lib.Common.CImageConverter.ToBitmap(e.ImageGrab); }
                        GC.Collect();
                    }
                }
                catch (Exception Desc)
                {
                    CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                }
            });
        }

        private void OnChangedRecipe(object sender, EventArgs e)
        {
            this.UIThreadInvoke(() =>
            {
                try
                {
                    //lbRecipeName.Text = Global.Recipe.ModelName;
                    //lbModelName.Text =  Global.Recipe.ModelNo;

                    CUtil.InitDirectory("RECIPE");
                    CUtil.InitDirectory($"RECIPE\\{Global.Recipe.Name}\\VISION");
                    CUtil.InitDirectory($"RECIPE\\{Global.Recipe.Name}\\DEVICE");
                    CUtil.InitDirectory($"RECIPE\\{Global.Recipe.Name}\\MOTION");
                    CUtil.InitDirectory($"RECIPE\\{Global.Recipe.Name}\\GRAPH");
                    CUtil.InitDirectory($"RECIPE\\{Global.Recipe.Name}\\PATTERN");
                }
                catch (Exception Desc)
                {
                    CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                }
            });
        }

        private void HideForm()
        {
            if (FrmVision != null && FrmVision.Visible) FrmVision.Hide();
            pnFormMain.Hide();
        }

        private void OnChangedMenu(object sender, EventArgs e)
        {
            this.UIThreadInvoke(() =>
            {
                switch (Global.System.Menu)
                {
                    case CSystem.MENU.MAIN:
                        HideForm();
                        pnFormMain.Show();
                        //btnMenuMain.BackColor = UIAppearance.StyleColor;
                        //btnMenuVision.BackColor = Color.FromArgb(64, 73, 108);
                        //btnMenuMotion.BackColor = DEFINE.BACK_COLOR;
                        break;
                    case CSystem.MENU.VISION:
                        HideForm();
                        if (FrmVision != null && !FrmVision.Visible) FrmVision.Show();
                        //btnMenuVision.BackColor = UIAppearance.StyleColor;
                        //btnMenuMain.BackColor = Color.FromArgb(64, 73, 108);
                        //btnMenuMotion.BackColor = DEFINE.BACK_COLOR;
                        break;
                    case CSystem.MENU.MOTION:
                        //btnMenuVision.BackColor = Color.FromArgb(64, 73, 108);
                        //btnMenuMain.BackColor = Color.FromArgb(64, 73, 108);
                        //btnMenuMotion.BackColor = DEFINE.MOUSEHOVER_COLOR;
                        HideForm();
                        break;
                }
            });
        }

        private void OnClickMenu(object sender, EventArgs e)
        {
            try
            {
                string strIndex = ((RJButton)sender).Tag.ToString();
                Global.System.Menu = CUtil.ParseEnum<CSystem.MENU>(strIndex);

                CLOG.NORMAL($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }

        private void OnChangedAuthorization(object sender, EventArgs e)
        {
            //btnAuthoriztionName.Text = Global.System.Authorization.ToString();
        }

        #endregion

        private void timerAlarm_Tick(object sender, EventArgs e)
        {
            if (CAlarm.Exists)
            {
                try
                {
                    if (CUtil.OpenCheckForm("FormAlarm"))
                    {
                        Global.System.AlarmWait.Reset();
                        Global.System.Mode = CSystem.MODE.ALARM;

                        string strCode = CAlarm.GetLastAlarm().Item1;
                        string strDesc = CAlarm.GetLastAlarm().Item2;
                        string strPos = CAlarm.GetLastAlarm().Item3;

                        FormAlarm.BUTTON_TYPE btnType = FormAlarm.BUTTON_TYPE.DEFAULT;

                        switch (strCode)
                        {
                            case "":
                                btnType = FormAlarm.BUTTON_TYPE.DEFAULT;
                                break;
                        }

                        FormAlarm frmAlarm = new FormAlarm(strCode, strDesc, strPos, btnType);

                        if (frmAlarm != null && !frmAlarm.IsDisposed)
                        {
                            if (frmAlarm.ShowDialog() == DialogResult.OK || frmAlarm.ShowDialog() == DialogResult.Cancel)
                            {
                                switch (frmAlarm.m_ebtnResult)
                                {
                                    case FormAlarm.BUTTON_RESULT.NONE:
                                    case FormAlarm.BUTTON_RESULT.SKIP:
                                        break;
                                    case FormAlarm.BUTTON_RESULT.RESET:
                                        break;
                                    case FormAlarm.BUTTON_RESULT.REJECT:
                                        break;
                                    case FormAlarm.BUTTON_RESULT.RETRY:
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception Desc)
                {
                    CCommon.ShowMessageBox("S/W BUG", "디버깅용, 담당자와 연락을 취해주세요.");
                    CLOG.ABNORMAL("S/W BUG, 담당자와 연락을 취해주세요.");
                }
            }
        }

        private void timerStatus_Tick(object sender, EventArgs e)
        {

        }

        private void btnScreenCapture_Click(object sender, EventArgs e)
        {
            try
            {
                int w = Screen.PrimaryScreen.Bounds.Width;
                int h = Screen.PrimaryScreen.Bounds.Height;

                System.Drawing.Size s = new System.Drawing.Size(w, h);
                Bitmap b = new Bitmap(w, h);
                Graphics g = Graphics.FromImage(b);

                g.CopyFromScreen(0, 0, 0, 0, s);

                string strSavePath = $"{System.Windows.Forms.Application.StartupPath}\\CAPTURE\\{this.Text}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.jpeg";

                b.Save(strSavePath);

                CLOG.NORMAL($"저장 경로 : {strSavePath}");
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }


        private void FormMetroFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void btnScreenCapture_MouseUp(object sender, MouseEventArgs e)
        {           
            if (e.Button == MouseButtons.Right)
            {
                Control control = (Control)sender;
                ddmCapture.ItemClicked += new ToolStripItemClickedEventHandler(CaptureClicked);
                ddmCapture.Show(control, 0, control.Height);
            }
        }

        private void Open_DropdownMenu(RJDropdownMenu dropdownMenu, object sender)
        {
            Control control = (Control)sender;

            switch (control.Text)
            {
                case "CAPTURE":
                    dropdownMenu.ItemClicked += new ToolStripItemClickedEventHandler(CaptureClicked);
                    dropdownMenu.Show(control, 0, control.Height);
                    break;
                case "Setting":
                    dropdownMenu.ItemClicked += new ToolStripItemClickedEventHandler(DeviceClicked);
                    //dropdownMenu.Show(control, control.Width, 0);
                    dropdownMenu.Show(control, dropdownMenu.Width - control.Width, control.Height);
                    //dropdownMenu.Show(control, (int)(control.Width / 2) * -1, 0);
                    break;
            }
        }

        private void CaptureClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "Show Folder":
                    Process.Start(System.Windows.Forms.Application.StartupPath + "\\CAPTURE");
                    break;
                default:
                    break;
            }
        }

        private void DeviceClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "CAMERA":
                    FormSetting_Camera FrmCam = new FormSetting_Camera();
                    FrmCam.StartPosition = FormStartPosition.CenterScreen;
                    FrmCam.TopMost = true;

                    if (!CUtil.OpenCheckForm(FrmCam)) return;
                    FrmCam.Show();
                    break;
                case "LIGHT":
                    FormSetting_llumination FrmIIIumunation = new FormSetting_llumination();
                    FrmIIIumunation.StartPosition = FormStartPosition.CenterScreen;

                    if (!CUtil.OpenCheckForm(FrmIIIumunation)) return;
                    FrmIIIumunation.Show();
                    break;
                case "UTIL":
                    FormSetting_UTIL formSettings_UTIL = new FormSetting_UTIL();
                    formSettings_UTIL.StartPosition = FormStartPosition.CenterScreen;
                    formSettings_UTIL.TopMost = true;

                    if (!CUtil.OpenCheckForm(formSettings_UTIL)) return;
                    formSettings_UTIL.Show();
                    break;
                default:
                    break;
            }
        }

        private void btnMenuOption_Click(object sender, EventArgs e)
        {
            Open_DropdownMenu(ddmDevice, sender);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            if (Global.System.Mode == CSystem.MODE.AUTO)
            {
                Global.System.Notice = "Can't Close the Program, because Current Mode is Auto";
                return;
            }
            else
            {
                if (CCommon.ShowdialogMessageBox("EXIT", "DO YOU WANT TO EXIT?", FormMessageBox.MESSAGEBOX_TYPE.Stop))
                {
                    Global.Data.SaveConfig(Global.Recipe.Name);

                    Global.Close();
                    this.Close();
                }
            }
        }

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnRestaurar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            //btnRestaurar.Visible = false;
            ///btnMaximizar.Visible = true;
        }

        private void btnMaximizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            //  btnMaximizar.Visible = false;
            //  btnRestaurar.Visible = true;
        }

        private void btnAuthoriztion_Click(object sender, EventArgs e)
        {
            FormAuthorization formAuthorization = new FormAuthorization();
            formAuthorization.TopLevel = true;
            formAuthorization.TopMost = true;
            formAuthorization.StartPosition = FormStartPosition.CenterParent;
            if (!CUtil.OpenCheckForm(formAuthorization)) return;
            formAuthorization.Show();
        }

        private void btnMenuModel_Click(object sender, EventArgs e)
        {
            FormRecipeManager FrmRecipe = new FormRecipeManager();
            FrmRecipe.TopLevel = true;
            FrmRecipe.TopMost = true;
            FrmRecipe.StartPosition = FormStartPosition.CenterParent;
            if (!CUtil.OpenCheckForm(FrmRecipe)) return;
            FrmRecipe.Show();
        }

        private void timerConnection_Tick(object sender, EventArgs e)
        {
            lbSelectImageName.Text = Global.Data.LastSelectImageName;
        }

        private void miSettings_Click(object sender, EventArgs e)
        {
            RJSettingsForm rJSettingsForm = new RJSettingsForm();
            rJSettingsForm.Show();
        }

        private void btnClassMenu_Click(object sender, EventArgs e)
        {
            formVision_ClassMenu = new FormVision_ClassMenu();
            formVision_ClassMenu.TopLevel = true;
            formVision_ClassMenu.TopMost = true;
            formVision_ClassMenu.StartPosition = FormStartPosition.CenterParent;
            if (!CUtil.OpenCheckForm(formVision_ClassMenu)) return;
            formVision_ClassMenu.OnButtonClicked += ClassMenuForm_OnButtonClicked;
            formVision_ClassMenu.Show();
        }

        private void ClassMenuForm_OnButtonClicked()
        {
            var textList = Global.Data.ClassNamedList.Select(x => x.Text).ToList();
            // ComboBox에 추가합니다.
            cbClassMenu.Items.Clear();
            cbClassMenu.Items.AddRange(textList.ToArray());
            if (cbClassMenu.Items.Count != 0) { cbClassMenu.SelectedIndex = 0; }

        }

        private void cbClassMenu_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            // 버튼이 클릭되면 OnButtonClicked 이벤트를 발생시킵니다.
            //OnChangeClassClicked?.Invoke();
            if (FrmVision == null) { return; }
            if (FrmVision.EventUpdateImageItem != null)
            {
                FrmVision.EventUpdateImageItem(null, new ClassItemEventArgs(Global.Data.ClassNamedList[cbClassMenu.SelectedIndex]));
            }
            if(FrmVision.EventUpdateClassItem != null)
            {
                FrmVision.EventUpdateClassItem(null, null);
            }
        }

        private void btnClassSave_Click(object sender, EventArgs e)
        {
            int imageW = CDisplayManager.Displays[DEFINE.Main].viewer._Ib.Image.Width;
            int imageH = CDisplayManager.Displays[DEFINE.Main].viewer._Ib.Image.Height;
            string imageFile = GetFileNameWithoutExtension(Global.Data.LastSelectImageName);
            foreach (var item in CDisplayManager.Displays[DEFINE.Main].viewer._RoisOb)
            {
                int index = CYolov5.GetComboBoxIndex(cbClassMenu, item.Title);
                float centerX = (float)(item.Roi.X + item.Roi.Width / 2) / imageW;
                float centerY = (float)(item.Roi.Y + item.Roi.Height / 2) / imageH;
                float width = (float)item.Roi.Width / imageW;
                float height = (float)item.Roi.Height / imageH;
                float[] floats = { index, centerX, centerY, width, height };
                CYolov5.CreateImageAndTxtFile(imageFile, (Bitmap)CDisplayManager.Displays[DEFINE.Main].viewer._Ib.Image, floats, Application.StartupPath + "\\DATA");
            }
        }

        public string GetFileNameWithoutExtension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        private void btnClassTrain_Click(object sender, EventArgs e)
        {
            Global.DeepLearning.Send(CommandLearning.StartTraining.ToString());
        }

        private void btnClassInfer_Click(object sender, EventArgs e)
        {
            Global.DeepLearning.Send(CommandLearning.StartDefect.ToString());
        }
    }
}
