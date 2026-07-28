using System;
using System.IO;
using System.Windows;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private readonly WpfFileDialogService datasetInterchangeFileDialogService =
            new WpfFileDialogService();
        private WpfDatasetInterchangeWindow datasetInterchangeWindow;

        private void ExecuteOpenDatasetInterchangeCommand()
        {
            if (datasetInterchangeWindow == null)
            {
                var viewModel = new WpfDatasetInterchangeViewModel(global.Data);
                viewModel.ConfigurePickers(
                    PickDatasetInterchangeSource,
                    PickDatasetInterchangeTarget,
                    PickDatasetInterchangeImageRoot);
                datasetInterchangeWindow = new WpfDatasetInterchangeWindow(viewModel)
                {
                    Owner = this
                };
                datasetInterchangeWindow.Closed += DatasetInterchangeWindow_Closed;
                datasetInterchangeWindow.ApplyThemeFrom(this);
                datasetInterchangeWindow.Show();
            }
            else
            {
                datasetInterchangeWindow.ViewModel?.Refresh(global.Data);
                datasetInterchangeWindow.ApplyThemeFrom(this);
                if (datasetInterchangeWindow.WindowState == WindowState.Minimized)
                {
                    datasetInterchangeWindow.WindowState = WindowState.Normal;
                }
            }

            datasetInterchangeWindow.Activate();
        }

        private string PickDatasetInterchangeSource(
            WpfDatasetInterchangeOption operation,
            string currentPath)
        {
            if (operation?.SourceIsDirectory == true)
            {
                return datasetInterchangeFileDialogService.TryPickFolder(
                    datasetInterchangeWindow,
                    "\uC678\uBD80 \uC5B4\uB178\uD14C\uC774\uC158 \uD3F4\uB354 \uC120\uD0DD",
                    currentPath,
                    out string folderPath)
                    ? folderPath
                    : currentPath;
            }

            string filter = operation?.Capability.FormatKey.Contains("cvat", StringComparison.Ordinal) == true
                ? "CVAT archive (*.zip)|*.zip|All files (*.*)|*.*"
                : "JSON annotation (*.json)|*.json|All files (*.*)|*.*";
            return datasetInterchangeFileDialogService.TryPickFile(
                datasetInterchangeWindow,
                "\uC678\uBD80 \uC5B4\uB178\uD14C\uC774\uC158 \uC120\uD0DD",
                filter,
                currentPath,
                out string filePath)
                ? filePath
                : currentPath;
        }

        private string PickDatasetInterchangeTarget(
            WpfDatasetInterchangeOption operation,
            string currentPath)
        {
            if (operation?.TargetIsDirectory == true)
            {
                return datasetInterchangeFileDialogService.TryPickFolder(
                    datasetInterchangeWindow,
                    "Pascal VOC \uB0B4\uBCF4\uB0B4\uAE30 \uD3F4\uB354 \uC120\uD0DD",
                    currentPath,
                    out string folderPath)
                    ? folderPath
                    : currentPath;
            }

            bool isArchive = operation?.Capability.FormatKey.Contains("archive", StringComparison.Ordinal) == true;
            string filter = isArchive
                ? "ZIP archive (*.zip)|*.zip"
                : "JSON file (*.json)|*.json";
            string extension = isArchive ? ".zip" : ".json";
            return datasetInterchangeFileDialogService.TryPickSaveFile(
                datasetInterchangeWindow,
                "\uB0B4\uBCF4\uB0B4\uAE30 \uB300\uC0C1 \uC120\uD0DD",
                filter,
                currentPath,
                extension,
                out string filePath)
                ? filePath
                : currentPath;
        }

        private string PickDatasetInterchangeImageRoot(string currentPath)
            => datasetInterchangeFileDialogService.TryPickFolder(
                datasetInterchangeWindow,
                "\uC6D0\uBCF8 \uC774\uBBF8\uC9C0 \uD3F4\uB354 \uC120\uD0DD",
                currentPath,
                out string folderPath)
                ? folderPath
                : currentPath;

        private void DatasetInterchangeWindow_Closed(object sender, EventArgs e)
        {
            if (datasetInterchangeWindow != null)
            {
                datasetInterchangeWindow.Closed -= DatasetInterchangeWindow_Closed;
                datasetInterchangeWindow = null;
            }
        }

        private void CloseDatasetInterchangeWindow()
        {
            datasetInterchangeWindow?.Close();
        }

        private void RefreshDatasetInterchangeWindowTheme()
        {
            datasetInterchangeWindow?.ApplyThemeFrom(this);
        }
    }
}
