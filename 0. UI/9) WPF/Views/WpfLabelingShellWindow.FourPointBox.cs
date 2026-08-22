using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.OpenGLRendering;
using System;
using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private readonly WpfFourPointBoxService fourPointBoxService = new WpfFourPointBoxService();

        private bool IsFourPointBoxInputActive()
            => activeAnnotationTool == WpfAnnotationTool.Rectangle
                && CanvasPanelViewModel?.SelectedBoxDrawingMethod?.Method
                    == LabelingBoxDrawingMethod.FourPointExtreme;

        private void ExecuteSetBoxDrawingMethod(LabelingBoxDrawingMethod method)
        {
            LabelingBoxDrawingMethod normalized = Enum.IsDefined(typeof(LabelingBoxDrawingMethod), method)
                ? method
                : LabelingBoxDrawingMethod.TwoPointDrag;
            CancelFourPointBoxDraft(updateStatus: false);
            EnsureProjectSettings();
            global.Data.ProjectSettings.BoxDrawingMethod = normalized;
            CanvasPanelViewModel?.RestoreBoxDrawingMethod(normalized);

            string recipeName = GetCurrentRecipeName();
            if (!string.IsNullOrWhiteSpace(recipeName))
            {
                try
                {
                    global.Data.SaveConfig(recipeName, refreshDatasetVersion: false);
                }
                catch (Exception error)
                {
                    AppendLog("\uBC15\uC2A4 \uC785\uB825 \uBC29\uC2DD \uC800\uC7A5 \uC2E4\uD328: " + error.Message);
                }
            }

            ApplyRectangleDrawingInputMode();
            string methodName = normalized == LabelingBoxDrawingMethod.FourPointExtreme
                ? "4\uC810 \uADF9\uC810"
                : "2\uC810 \uB4DC\uB798\uADF8";
            SetYoloCommandStatus($"\uBC15\uC2A4 \uC785\uB825: {methodName}", isBusy: false);
            AppendLog($"\uBC15\uC2A4 \uC785\uB825 \uBC29\uC2DD: {methodName}");
        }

        private void RestoreBoxDrawingMethodFromProject()
        {
            EnsureProjectSettings();
            CanvasPanelViewModel?.RestoreBoxDrawingMethod(global.Data.ProjectSettings.BoxDrawingMethod);
            CancelFourPointBoxDraft(updateStatus: false);
            ApplyRectangleDrawingInputMode();
        }

        private void ApplyRectangleDrawingInputMode()
        {
            if (MainCanvasViewModel == null || activeAnnotationTool != WpfAnnotationTool.Rectangle)
            {
                return;
            }

            if (IsFourPointBoxInputActive())
            {
                MainCanvasViewModel.IsTeachingMode = false;
                MainCanvasViewModel.IsImagePointInputMode = true;
                MainCanvasViewModel.ImageViewer.SetViewMode(CanvasInteractionMode.None);
                CanvasPanelViewModel?.SetFourPointBoxProgress(fourPointBoxService.PointCount);
                RefreshCanvasWorkflowContext();
                return;
            }

            MainCanvasViewModel.IsImagePointInputMode = false;
            MainCanvasViewModel.IsTeachingMode = true;
            CanvasPanelViewModel?.SetFourPointBoxProgress(0);
            RefreshCanvasWorkflowContext();
        }

        private bool TryHandleFourPointBoxInput(CanvasImagePointEventArgs e)
        {
            if (!IsFourPointBoxInputActive() || e == null)
            {
                return false;
            }

            if (e.Button == CanvasPointerButton.Right)
            {
                CancelFourPointBoxDraft(updateStatus: true);
                return true;
            }

            if (e.Button != CanvasPointerButton.Left)
            {
                return true;
            }

            WpfFourPointBoxInputResult result = fourPointBoxService.TryAddPoint(
                e.ImagePoint,
                activeImageSize,
                out Rectangle completedBounds,
                out string message);
            CanvasPanelViewModel?.SetFourPointBoxProgress(fourPointBoxService.PointCount);
            RefreshPolygonOverlays();
            SetYoloCommandStatus(message, isBusy: false);
            if (result != WpfFourPointBoxInputResult.Completed)
            {
                return true;
            }

            string className = FirstNonEmpty(GetSelectedClassName(), "Defect");
            if (MainCanvasViewModel.AddCompletedImageRectangle(completedBounds, className) == null)
            {
                SetYoloCommandStatus("\uBC15\uC2A4 \uC624\uBC84\uB808\uC774\uB97C \uCD94\uAC00\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", isBusy: false);
                return true;
            }

            CanvasPanelViewModel?.SetFourPointBoxProgress(0);
            RefreshPolygonOverlays();
            return true;
        }

        private bool RemoveLastFourPointBoxPoint()
        {
            if (!IsFourPointBoxInputActive() || !fourPointBoxService.RemoveLastPoint())
            {
                return false;
            }

            CanvasPanelViewModel?.SetFourPointBoxProgress(fourPointBoxService.PointCount);
            RefreshPolygonOverlays();
            SetYoloCommandStatus(
                fourPointBoxService.PointCount == 0
                    ? "4\uC810 \uADF9\uC810 \uC785\uB825\uC744 \uB2E4\uC2DC \uC2DC\uC791\uD558\uC138\uC694."
                    : fourPointBoxService.BuildProgressText(),
                isBusy: false);
            return true;
        }

        private bool CancelFourPointBoxDraft(bool updateStatus)
        {
            bool canceled = fourPointBoxService.Reset();
            CanvasPanelViewModel?.SetFourPointBoxProgress(0);
            if (canceled)
            {
                RefreshPolygonOverlays();
                if (updateStatus)
                {
                    SetYoloCommandStatus(
                        "4\uC810 \uADF9\uC810 \uCD08\uC548\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.",
                        isBusy: false);
                }
            }

            return canceled;
        }
    }
}
