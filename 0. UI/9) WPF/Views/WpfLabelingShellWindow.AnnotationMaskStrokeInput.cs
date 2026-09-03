using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Stroke geometry is previewed by the GPU/FBO; status text is throttled so WPF
        // bindings do not compete with high-frequency brush MouseMove input.
        private static readonly long MaskStrokeStatusUpdateIntervalTicks = Math.Max(1L, Stopwatch.Frequency / 8);

        private void ApplyMaskAnnotationStroke(CanvasImagePointEventArgs e, bool resetStroke)
        {
            if (e == null || activeImageSize.IsEmpty)
            {
                return;
            }

            if (e.Button == CanvasPointerButton.Right)
            {
                CompleteMaskAnnotationStroke();
                CancelMaskStrokePreviewCommitSwap();
                lastMaskStrokePoint = null;
                MainCanvasViewModel?.ClearMaskStrokePreview();
                SetYoloCommandStatus("마스크 스트로크를 초기화했습니다. 다시 드래그해 편집을 이어가세요.", isBusy: false);
                return;
            }

            if (e.Button != CanvasPointerButton.Left)
            {
                return;
            }

            int radius = GetMaskBrushRadius();
            IReadOnlyList<System.Drawing.Point> centers = maskAnnotationService.BuildStrokeCenters(
                resetStroke ? null : lastMaskStrokePoint,
                e.ImagePoint,
                radius);
            lastMaskStrokePoint = e.ImagePoint;

            string actionName = activeAnnotationTool == WpfAnnotationTool.Brush ? "마스크 칠하기" : "마스크 지우기";
            if (resetStroke && activeMaskStrokeInProgress)
            {
                CompleteMaskAnnotationStroke();
            }

            if (!activeMaskStrokeInProgress)
            {
                CancelMaskStrokePreviewCommitSwap();
                // Match the old Viewer2D brush flow: MouseMove only feeds the GPU/FBO
                // edit preview, while MouseUp enqueues CPU MaskData/history work between strokes.
                activeMaskStrokeInProgress = true;
                activeMaskStrokeActionName = actionName;
                activeMaskStrokeSegmentIndices.Clear();
                activeMaskStrokeNeedsFullObjectRefresh = false;
                activeMaskStrokeCommitSession.Begin(
                    radius,
                    activeAnnotationTool,
                    FirstNonEmpty(GetSelectedClassName(), "Defect"));
                MainCanvasViewModel?.BeginMaskStrokePreview(
                    activeImageSize,
                    GetMaskStrokePreviewColor(activeAnnotationTool == WpfAnnotationTool.Eraser),
                    activeAnnotationTool == WpfAnnotationTool.Eraser);
            }

            IReadOnlyList<System.Drawing.Point> previewCenters = AppendMaskStrokeCommitCenters(centers);
            if (previewCenters.Count == 0)
            {
                return;
            }

            MainCanvasViewModel?.AddMaskStrokePreview(
                previewCenters,
                radius,
                GetMaskStrokePreviewColor(activeAnnotationTool == WpfAnnotationTool.Eraser),
                activeAnnotationTool == WpfAnnotationTool.Eraser);
            TryUpdateMaskStrokePreviewStatus(force: false);
        }

        private void TryUpdateMaskStrokePreviewStatus(bool force)
        {
            long now = Stopwatch.GetTimestamp();
            if (!force
                && lastMaskStrokeStatusUpdateTicks != 0
                && now - lastMaskStrokeStatusUpdateTicks < MaskStrokeStatusUpdateIntervalTicks)
            {
                return;
            }

            lastMaskStrokeStatusUpdateTicks = now;
            string action = activeAnnotationTool == WpfAnnotationTool.Brush ? "마스크 칠하기 미리보기" : "마스크 지우기 미리보기";
            SetModelStatus($"{action}: 스트로크 {activeMaskStrokeCommitSession.Count}점");
        }

        // Stroke preview colors, tool predicates, and object-row refresh are
        // part of the same interactive mask-input state as the pointer handler.
        private bool TryRefreshMaskStrokeObjectReviewRows()
            => TryRefreshMaskStrokeObjectReviewRows(activeMaskStrokeSegmentIndices, activeMaskStrokeNeedsFullObjectRefresh);

        private bool TryRefreshMaskStrokeObjectReviewRows(
            IEnumerable<int> segmentIndices,
            bool needsFullObjectRefresh)
        {
            IReadOnlyList<int> orderedSegmentIndices = (segmentIndices ?? Array.Empty<int>())
                .Distinct()
                .OrderBy(index => index)
                .ToList();
            if (needsFullObjectRefresh
                || orderedSegmentIndices.Count == 0
                || ObjectReviewViewModel == null)
            {
                return false;
            }

            string summary = WpfObjectReviewPresenter.BuildSummary(
                manualRois.Count + manualSegments.Count + confirmedDetectionCandidates.Count);
            bool selectChangedMask = orderedSegmentIndices.Count == 1
                && !activeMaskStrokeInProgress;
            foreach (int segmentIndex in orderedSegmentIndices)
            {
                if (!TryRefreshManualSegmentObjectReviewRow(segmentIndex, summary, selectChangedMask))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsMaskAnnotationToolActive()
            => maskEditStateService.IsMaskPaintTool(activeAnnotationTool);

        private bool ShouldSelectCommittedMaskAfterStroke()
            => !suppressMaskStrokeCommitSelection
                && maskEditStateService.ShouldSelectCommittedMask(activeAnnotationTool);

        private int GetMaskBrushRadius()
        {
            int brushSize = LearningWorkflowViewModel?.BrushSize ?? WpfMaskAnnotationService.DefaultBrushRadius * 2;
            return Math.Clamp((int)Math.Round(brushSize / 2D), 1, 128);
        }

        private System.Drawing.Color GetMaskCursorPreviewColor(bool isEraser)
        {
            if (isEraser)
            {
                return System.Drawing.Color.FromArgb(245, 158, 11);
            }

            string className = FirstNonEmpty(GetSelectedClassName(), "Defect");
            LabelClass existing = global.Data.ClassNamedList?
                .FirstOrDefault(item => string.Equals(item?.Text, className, StringComparison.OrdinalIgnoreCase));
            return existing?.DrawColor ?? System.Drawing.Color.FromArgb(44, 210, 110);
        }

        private System.Drawing.Color GetMaskStrokePreviewColor(bool isEraser)
        {
            System.Drawing.Color color = GetMaskCursorPreviewColor(isEraser);
            if (isEraser)
            {
                return color;
            }

            int alpha = (int)Math.Round(Math.Clamp(LearningWorkflowViewModel?.MaskOpacity ?? 0.66D, 0.1D, 1.0D) * 255D);
            return System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        // Brush-size commands update the same shared mask-input radius and
        // Canvas toolbar projection, so they stay with this interactive owner.
        private const int CanvasBrushSizeStep = 2;

        private void ExecuteDecreaseBrushSizeCommand()
            => AdjustBrushSize(-CanvasBrushSizeStep);

        private void ExecuteIncreaseBrushSizeCommand()
            => AdjustBrushSize(CanvasBrushSizeStep);

        private void AdjustBrushSize(int delta)
        {
            if (LearningWorkflowViewModel == null)
            {
                return;
            }

            LearningWorkflowViewModel.BrushSize = Math.Clamp(
                LearningWorkflowViewModel.BrushSize + delta,
                2,
                64);
            SyncCanvasBrushSizeFromWorkflow();
        }

        private void SyncCanvasBrushSizeFromWorkflow()
        {
            CanvasPanelViewModel?.SetBrushSize(LearningWorkflowViewModel?.BrushSize ?? 12);
        }

        private void LearningWorkflowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e?.PropertyName, nameof(WpfLearningWorkflowPanelViewModel.BrushSize), StringComparison.Ordinal))
            {
                SyncCanvasBrushSizeFromWorkflow();
            }
        }
    }
}
