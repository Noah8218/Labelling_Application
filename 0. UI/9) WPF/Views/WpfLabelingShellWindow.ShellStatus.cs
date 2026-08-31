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
            int remainingCount = Math.Max(0, totalCount - completedCount);
            string stageText = ResolveWorkflowStageText(totalCount);
            string progressText = totalCount > 0
                ? $"진행: {completedCount}/{totalCount} 완료 · {remainingCount} 남음"
                : "진행: 이미지 없음";
            string nextActionText = ResolveWorkflowNextActionText(totalCount, remainingCount);
            StatusBarViewModel.SetWorkflowStatus(stageText, progressText, nextActionText);
        }

        private string ResolveWorkflowStageText(int totalCount)
        {
            if (currentWorkflowMode == WorkflowMode.Inference)
            {
                return pendingDetectionCandidates.Count > 0
                    ? "단계: AI 후보 검토"
                    : "단계: AI 후보 대기";
            }

            if (!string.IsNullOrWhiteSpace(annotationDirtyReason))
            {
                return "단계: 저장 필요";
            }

            if (lastYoloTrainingReadinessReport?.IsReady == true)
            {
                return "단계: 학습 준비";
            }

            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                return totalCount > 0
                    ? "단계: 이미지 선택"
                    : "단계: 데이터셋 준비";
            }

            return activeImageBitmap != null && !activeImageSize.IsEmpty
                ? "단계: 라벨링"
                : "단계: 준비";
        }

        private string ResolveWorkflowNextActionText(int totalCount, int remainingCount)
        {
            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                return totalCount > 0
                    ? "다음: 이미지 선택"
                    : "다음: 데이터셋 시작";
            }

            if (pendingDetectionCandidates.Count > 0)
            {
                return "다음: AI 후보 확정/스킵";
            }

            if (!string.IsNullOrWhiteSpace(annotationDirtyReason))
            {
                return "다음: 저장";
            }

            if (totalCount > 0 && remainingCount > 0)
            {
                return "다음: 다음 미완료 이미지";
            }

            if (totalCount > 0)
            {
                return lastYoloTrainingReadinessReport?.IsReady == true
                    ? "다음: 학습 시작"
                    : "다음: 데이터셋 점검";
            }

            return "다음: 이미지 폴더";
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
