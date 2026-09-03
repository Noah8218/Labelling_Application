using OpenVisionLab.Logging;
using System;
using System.Linq;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfUiApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme;
using WpfUiApplicationThemeManager = Wpf.Ui.Appearance.ApplicationThemeManager;
using WpfUiWindowBackdropType = Wpf.Ui.Controls.WindowBackdropType;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private static string FirstNonEmpty(params string[] values)
        {
            return values?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private void SetDatasetStatus(string text)
        {
            string normalized = text ?? string.Empty;
            StatusBarViewModel.SetDatasetStatus(normalized);
            RefreshShellDatasetContext();
            UpdateWorkflowProgressStatus();
        }

        private void SetPythonStatus(string text)
        {
            string normalized = text ?? string.Empty;
            StatusBarViewModel.SetPythonStatus(normalized);
        }

        private void UpdateWorkflowProgressStatus()
        {
            if (StatusBarViewModel == null)
            {
                return;
            }

            int totalCount = imageQueueItems?.Count ?? 0;
            int completedCount = imageQueueItems?.Count(WpfImageQueueFilterService.IsCompletedQueueItem) ?? 0;
            WpfShellWorkflowStatus status = WpfShellWorkflowStatusPresentationService.Build(
                new WpfShellWorkflowStatusContext(
                    isInferenceMode: currentWorkflowMode == WorkflowMode.Inference,
                    totalImageCount: totalCount,
                    completedImageCount: completedCount,
                    hasPendingCandidates: pendingDetectionCandidates.Count > 0,
                    hasUnsavedAnnotationChanges: !string.IsNullOrWhiteSpace(annotationDirtyReason),
                    isTrainingReady: lastYoloTrainingReadinessReport?.IsReady == true,
                    hasActiveImage: activeImageBitmap != null && !activeImageSize.IsEmpty));
            StatusBarViewModel.SetWorkflowStatus(
                status.StageText,
                status.ProgressText,
                status.NextActionText);
        }

        private void SetModelStatus(string text)
        {
            string normalized = text ?? string.Empty;
            StatusBarViewModel.SetModelStatus(normalized);
        }

        private void SetInspectionModelStatus(string text, string toolTip = null)
        {
            string normalized = string.IsNullOrWhiteSpace(text)
                ? "\uAC80\uC0AC \uBAA8\uB378: \uC5C6\uC74C"
                : text.Trim();
            string normalizedToolTip = string.IsNullOrWhiteSpace(toolTip)
                ? normalized
                : toolTip.Trim();
            StatusBarViewModel.SetInspectionModelStatus(normalized, normalizedToolTip);
        }

        private void ExecuteToggleThemeCommand()
        {
            ApplyTheme(ShellTheme.Dark);
            AppendLog("테마 고정: 다크");
        }

        private void ApplyTheme(ShellTheme theme)
        {
            // Theme selection is intentionally hidden for the focused workstation product.
            // Keep legacy callers safe by treating every request as the supported dark theme.
            theme = ShellTheme.Dark;
            currentTheme = theme;
            WpfUiApplicationThemeManager.Apply(WpfUiApplicationTheme.Dark, WpfUiWindowBackdropType.None, updateAccent: true);
            WpfUiApplicationThemeManager.Apply(this);

            // Theme resources stay centralized so split view partials do not introduce conflicting palette keys.
            SetThemeBrush("AppBackgroundBrush", "#0C0D0F");
            SetThemeBrush("FrameBrush", "#0A0B0D");
            SetThemeBrush("PanelBrush", "#171717");
            SetThemeBrush("PanelHeaderBrush", "#1F1F1F");
            SetThemeBrush("CanvasBrush", "#101820");
            SetThemeBrush("StatusBarBrush", "#0F1115");
            SetThemeBrush("BorderBrushDark", "#303030");
            SetThemeBrush("PrimaryTextBrush", "#F7F7F7");
            SetThemeBrush("SecondaryTextBrush", "#B7B7B7");
            SetThemeBrush("AccentBrush", "#3B82F6");
            SetThemeBrush("InfoBrush", "#3B82F6");
            SetThemeBrush("SuccessBrush", "#22C55E");
            SetThemeBrush("WarningBrush", "#F59E0B");
            SetThemeBrush("ErrorBrush", "#EF4444");
            SetThemeBrush("ToolbarButtonBrush", "#252525");
            SetThemeBrush("ToolbarButtonBorderBrush", "#3A3A3A");
            SetThemeBrush("ToolbarButtonHoverBrush", "#333333");
            SetThemeBrush("ToolbarButtonPressedBrush", "#1D1D1D");
            SetThemeBrush("ToolbarButtonDisabledBrush", "#20242A");
            SetThemeBrush("ToolbarButtonDisabledBorderBrush", "#2B3038");
            SetThemeBrush("DisabledTextBrush", "#69707A");
            SetThemeBrush("InputBrush", "#242424");
            SetThemeBrush("InputBorderBrush", "#3A3A3A");
            SetThemeBrush("GridLineBrush", "#2A2A2A");
            SetThemeBrush("GridHeaderBrush", "#202020");
            SetThemeBrush("RowHoverBrush", "#222A33");
            SetThemeBrush("SelectedRowBrush", "#26384F");
            SetThemeBrush("SelectedRowTextBrush", "#FFFFFF");
            SetThemeBrush("DetectionOverlayBackgroundBrush", "#F00B1320");
            SetThemeBrush("DetectionOverlayBorderBrush", "#5524D366");
            SetThemeBrush("DetectionOverlayTitleTextBrush", "#FFFFFF");
            SetThemeBrush("DetectionOverlaySummaryTextBrush", "#BEEBD0");
            SetThemeBrush("DetectionOverlaySelectedBackgroundBrush", "#1F24D366");
            SetThemeBrush("DetectionOverlaySelectedTextBrush", "#FFFFFF");
            SetThemeBrush("DetectionOverlayDetailTextBrush", "#C9D4E2");

            if (FindResource("AppBackgroundBrush") is MediaBrush backgroundBrush)
            {
                Background = backgroundBrush;
            }

            RefreshModelBenchmarkWindowTheme();
            RefreshDatasetHealthWindowTheme();
            RefreshDatasetInterchangeWindowTheme();
            RefreshEnvironmentSetupCenterWindowTheme();
            UpdateWorkflowModeUi();
            UpdateQueueQuickFilterButtons();
        }

        private void SetThemeBrush(string key, string color)
        {
            var brush = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString(color));
            Resources[key] = brush;
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Resources[key] = brush;
            }
        }

        private void AppendLog(string message)
        {
            ShellLogViewModel?.RecordLog(message);
            OVLog.Write(LogCategory.Main, LogLevel.Info, message);
        }
    }
}
