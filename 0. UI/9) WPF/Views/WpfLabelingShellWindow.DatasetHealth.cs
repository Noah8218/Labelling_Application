using System;
using System.Windows;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private WpfDatasetHealthWindow datasetHealthWindow;

        private void ExecuteOpenDatasetHealthCommand()
        {
            if (datasetHealthWindow == null)
            {
                var viewModel = new WpfDatasetHealthViewModel(global.Data);
                viewModel.ConfigureVisualQaOpen(ExecuteOpenDatasetHealthImageInEditor);
                datasetHealthWindow = new WpfDatasetHealthWindow(viewModel)
                {
                    Owner = this
                };
                datasetHealthWindow.Closed += DatasetHealthWindow_Closed;
                datasetHealthWindow.ApplyThemeFrom(this);
                datasetHealthWindow.Show();
            }
            else
            {
                datasetHealthWindow.ViewModel?.Refresh(global.Data);
                datasetHealthWindow.ApplyThemeFrom(this);
                if (datasetHealthWindow.WindowState == WindowState.Minimized)
                {
                    datasetHealthWindow.WindowState = WindowState.Normal;
                }
            }

            datasetHealthWindow.Activate();
        }

        private void ExecuteOpenDatasetHealthImageInEditor(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            EnterLabelingWorkbenchStartView();
            if (!TryLoadImage(
                imagePath,
                populateQueue: false,
                refreshQueueDetails: false,
                refreshActiveStatus: true,
                appendLoadLog: true))
            {
                return;
            }

            datasetHealthWindow?.Close();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            AppendLog($"Dataset Health 시각 QA에서 편집기로 이동: {imagePath}");
        }

        private void DatasetHealthWindow_Closed(object sender, EventArgs e)
        {
            if (datasetHealthWindow != null)
            {
                datasetHealthWindow.Closed -= DatasetHealthWindow_Closed;
                datasetHealthWindow = null;
            }
        }

        private void CloseDatasetHealthWindow()
        {
            datasetHealthWindow?.Close();
        }

        private void RefreshDatasetHealthWindowTheme()
        {
            datasetHealthWindow?.ApplyThemeFrom(this);
        }
    }
}
