using MahApps.Metro.IconPacks;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.DrawObject;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Views;
using OpenVisionLab.ImageCanvas.ViewModels;
using OpenVisionLab.Mvvm;
using OpenVisionLab.Mvvm.Behaviors;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using CvMat = OpenCvSharp.Mat;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingSize = System.Drawing.Size;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfUiApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme;
using WpfUiApplicationThemeManager = Wpf.Ui.Appearance.ApplicationThemeManager;
using WpfUiFluentWindow = Wpf.Ui.Controls.FluentWindow;
using WpfUiWindowBackdropType = Wpf.Ui.Controls.WindowBackdropType;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Project settings helpers are shared by detection, training, and save flows.
        private string BuildLabelPathSummary()
        {
            LabelingImageSnapshot activeImage = global.ImageWorkspace.CaptureSnapshot();
            IReadOnlyList<string> labelPaths = YoloAnnotationService.GetTargetLabelPaths(activeImage.ImageName, global.Data);
            return labelPaths.Count == 0
                ? "라벨 경로: 확인 안 됨"
                : $"라벨: {labelPaths[0]}";
        }

        private void EnsureProjectSettings()
        {
            global.Data.ProjectSettings ??= new LabelingProjectSettings();
            PythonModelRuntimePathResolver.ApplyDefaults(global.Data.ProjectSettings);
        }

        private void ApplyProjectDatasetPurposeToWorkflow()
        {
            EnsureProjectSettings();
            CanvasPanelViewModel?.RestoreSmartMaskAutoContourMode(
                global.Data.ProjectSettings.SmartMaskAutoContourEnabled);
            RestoreBoxDrawingMethodFromProject();
            LearningWorkflowViewModel?.ApplyDatasetPurpose(global.Data.ProjectSettings.DatasetPurpose);
            RefreshCanvasAnnotationToolScope();
            ApplyAnnotationToolSelection(LearningWorkflowViewModel?.SelectedTool);
            RefreshCanvasWorkflowContext();
            RefreshAnnotationVisibilityForDatasetPurpose();
            // Purpose synchronization is also called while the shell is being
            // constructed.  Keep that lifecycle path presentation-only; the
            // explicit recipe-apply flow refreshes readiness after the new
            // project has been committed, while a fresh shell retains the
            // dashboard/checklist "before check" state.
            RefreshYoloTrainingStepCompletion();
        }

        private void ApplyWorkflowDatasetPurposeToProjectSettings()
        {
            EnsureProjectSettings();
            global.Data.ProjectSettings.DatasetPurpose = LearningWorkflowViewModel?.GetSelectedDatasetPurpose()
                ?? global.Data.ProjectSettings.DatasetPurpose;
        }

        private void ApplyDatasetPurposeToCurrentProject(LabelingDatasetPurpose purpose)
        {
            ApplyDatasetPurposeToCurrentProjectCore(purpose, persistAfterBindingsSettle: false);
        }

        private void ApplyPersistedDatasetPurposeToCurrentProject(LabelingDatasetPurpose purpose)
        {
            ApplyDatasetPurposeToCurrentProjectCore(purpose, persistAfterBindingsSettle: true);
        }

        private void ApplyDatasetPurposeToCurrentProjectCore(
            LabelingDatasetPurpose purpose,
            bool persistAfterBindingsSettle)
        {
            SynchronizeDatasetPurposeToCurrentProject(purpose);
            string recipeName = global.Recipe.Name;
            var recipeSessionCancellationToken = projectRecipeSessionCts.Token;

            // The Recipe session commits Data before it publishes the selected
            // Recipe identity, but the previous ListBox selection can still
            // raise a queued WPF SelectionChanged callback afterwards. Apply
            // the same canonical recipe purpose once bindings have settled so
            // a previous recipe cannot repaint the new recipe as
            // segmentation/anomaly by mistake.
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    if (recipeSessionCancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (GetCurrentDatasetPurpose() != purpose)
                    {
                        SynchronizeDatasetPurposeToCurrentProject(purpose);
                    }

                    if (persistAfterBindingsSettle
                        && string.Equals(global.Recipe.Name, recipeName, StringComparison.Ordinal)
                        && !string.IsNullOrWhiteSpace(recipeName))
                    {
                        RecipeConfigurationSaveResult saveResult = global.Data.SaveConfig(recipeName);
                        if (!saveResult.IsSuccess)
                        {
                            AppendLog($"\uB370\uC774\uD130\uC14B \uC6A9\uB3C4 \uC800\uC7A5 \uC2E4\uD328: {saveResult.ErrorMessage}");
                        }
                    }
                }));
        }

        private void SynchronizeDatasetPurposeToCurrentProject(LabelingDatasetPurpose purpose)
        {
            CancelFourPointBoxDraft(updateStatus: false);
            EnsureProjectSettings();
            global.Data.ProjectSettings.DatasetPurpose = purpose;
            CanvasPanelViewModel?.RestoreSmartMaskAutoContourMode(
                global.Data.ProjectSettings.SmartMaskAutoContourEnabled);
            CanvasPanelViewModel?.RestoreBoxDrawingMethod(global.Data.ProjectSettings.BoxDrawingMethod);
            LearningWorkflowViewModel?.ApplyDatasetPurpose(purpose);

            // Recipe creation can run while the dataset-purpose ListBox still
            // holds the previous recipe's selection. Reconcile the view adapter
            // with the ViewModel before its SelectionChanged command can write
            // that stale purpose back into the newly created recipe.
            if (DatasetPurposeListBox != null)
            {
                BindingOperations.GetBindingExpression(
                    DatasetPurposeListBox,
                    System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.UpdateTarget();
            }

            RefreshCanvasAnnotationToolScope();
            ApplyAnnotationToolSelection(LearningWorkflowViewModel?.SelectedTool);
            RefreshCanvasWorkflowContext();
            RefreshAnnotationVisibilityForDatasetPurpose();
            RefreshShellDatasetContext();
        }
    }
}
