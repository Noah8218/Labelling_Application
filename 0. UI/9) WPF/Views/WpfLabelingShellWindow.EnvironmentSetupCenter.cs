using MvcVisionSystem._1._Core;
using System;
using System.Windows;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private WpfEnvironmentSetupCenterWindow environmentSetupCenterWindow;

        private void ExecuteOpenEnvironmentSetupCenterCommand()
        {
            HeaderToolsPopup.IsOpen = false;
            if (environmentSetupCenterWindow == null)
            {
                var viewModel = new WpfEnvironmentSetupCenterViewModel(
                    RuntimeDiagnosticsViewModel.DiagnosticsService,
                    CaptureEnvironmentSetupPythonSettings,
                    ExecuteOpenModelSettingsFromEnvironmentSetupCenter);
                environmentSetupCenterWindow = new WpfEnvironmentSetupCenterWindow(viewModel)
                {
                    Owner = this
                };
                environmentSetupCenterWindow.Closed += EnvironmentSetupCenterWindow_Closed;
                environmentSetupCenterWindow.ApplyThemeFrom(this);
                environmentSetupCenterWindow.Show();
            }
            else
            {
                environmentSetupCenterWindow.ViewModel?.Refresh();
                environmentSetupCenterWindow.ApplyThemeFrom(this);
                if (environmentSetupCenterWindow.WindowState == WindowState.Minimized)
                {
                    environmentSetupCenterWindow.WindowState = WindowState.Normal;
                }
            }

            environmentSetupCenterWindow.Activate();
        }

        private PythonModelSettings CaptureEnvironmentSetupPythonSettings()
            => global.Data.ProjectSettings?.PythonModel ?? new PythonModelSettings();

        private void ExecuteOpenModelSettingsFromEnvironmentSetupCenter()
        {
            environmentSetupCenterWindow?.Close();
            FocusYoloModelSettingsTab();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }

        private void EnvironmentSetupCenterWindow_Closed(object sender, EventArgs e)
        {
            if (environmentSetupCenterWindow != null)
            {
                environmentSetupCenterWindow.Closed -= EnvironmentSetupCenterWindow_Closed;
                environmentSetupCenterWindow = null;
            }
        }

        private void CloseEnvironmentSetupCenterWindow()
        {
            environmentSetupCenterWindow?.Close();
        }

        private void RefreshEnvironmentSetupCenterWindowTheme()
        {
            environmentSetupCenterWindow?.ApplyThemeFrom(this);
        }
    }
}
