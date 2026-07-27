using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.ViewModels;
using System;
using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteBeginVerticalSegmentationSplitCommand()
            => BeginPendingSegmentationSplit(WpfSegmentationSplitOrientation.Vertical);

        private void ExecuteBeginHorizontalSegmentationSplitCommand()
            => BeginPendingSegmentationSplit(WpfSegmentationSplitOrientation.Horizontal);

        private void ExecuteCancelSegmentationSplitCommand()
            => CancelPendingSegmentationSplit(updateStatus: true);

        private void BeginPendingSegmentationSplit(WpfSegmentationSplitOrientation orientation)
        {
            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            if (smartMaskPromptSession.HasSession || isCreatingSmartMask)
            {
                const string smartMaskError = "\uC2A4\uB9C8\uD2B8 \uB9C8\uC2A4\uD06C \uD6C4\uBCF4\uB97C \uD655\uC815\uD558\uAC70\uB098 \uCDE8\uC18C\uD55C \uB4A4 \uC808\uB2E8\uD558\uC138\uC694.";
                SetYoloCommandStatus(smartMaskError, isBusy: false);
                AppendLog(smartMaskError);
                return;
            }

            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected)
                || selected.Source != WpfObjectReviewSource.ManualSegment
                || selected.Index < 0
                || selected.Index >= manualSegments.Count
                || activeImageSize.IsEmpty)
            {
                const string selectionError = "\uC808\uB2E8\uD560 \uD3F4\uB9AC\uACE4 \uB610\uB294 \uB9C8\uC2A4\uD06C \uAC1D\uCCB4\uB97C \uD558\uB098 \uC120\uD0DD\uD558\uC138\uC694.";
                SetYoloCommandStatus(selectionError, isBusy: false);
                AppendLog(selectionError);
                return;
            }

            SelectAnnotationTool(WpfAnnotationTool.Select);
            pendingSegmentationSplitSourceIndex = selected.Index;
            pendingSegmentationSplitSource = manualSegments[selected.Index];
            pendingSegmentationSplitOrientation = orientation;
            ObjectReviewViewModel?.SetSplitPending(orientation);
            MainCanvasViewModel.IsImagePointInputMode = true;
            MainCanvasViewModel.ImageViewer.SetViewMode(CanvasInteractionMode.None);

            string direction = orientation == WpfSegmentationSplitOrientation.Vertical
                ? "\uC138\uB85C"
                : "\uAC00\uB85C";
            string status = $"{direction} \uC808\uB2E8 \uC704\uCE58 \uC120\uD0DD: \uCE94\uBC84\uC2A4\uC5D0\uC11C \uAC1D\uCCB4 \uC548\uCABD\uC744 \uD074\uB9AD\uD558\uC138\uC694. \uC6B0\uD074\uB9AD\uC740 \uCDE8\uC18C\uC785\uB2C8\uB2E4.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryApplyPendingSegmentationSplit(CanvasImagePointEventArgs e)
        {
            if (!pendingSegmentationSplitOrientation.HasValue)
            {
                return false;
            }

            if (e?.Button == CanvasPointerButton.Right)
            {
                CancelPendingSegmentationSplit(updateStatus: true);
                return true;
            }

            if (e?.Button != CanvasPointerButton.Left)
            {
                return true;
            }

            int sourceIndex = pendingSegmentationSplitSourceIndex;
            LabelingSegmentationObject source = pendingSegmentationSplitSource;
            if (source == null
                || sourceIndex < 0
                || sourceIndex >= manualSegments.Count
                || !ReferenceEquals(manualSegments[sourceIndex], source))
            {
                CancelPendingSegmentationSplit(updateStatus: false);
                const string staleSelectionError = "\uC120\uD0DD\uD55C \uAC1D\uCCB4\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uC808\uB2E8\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4. \uAC1D\uCCB4\uB97C \uB2E4\uC2DC \uC120\uD0DD\uD558\uC138\uC694.";
                SetYoloCommandStatus(staleSelectionError, isBusy: false);
                AppendLog(staleSelectionError);
                return true;
            }

            WpfSegmentationSplitOrientation orientation = pendingSegmentationSplitOrientation.Value;
            int coordinate = orientation == WpfSegmentationSplitOrientation.Vertical
                ? e.ImagePoint.X
                : e.ImagePoint.Y;
            if (!segmentationSplitService.TrySplit(
                source,
                orientation,
                coordinate,
                activeImageSize,
                out WpfSegmentationSplitResult splitResult,
                out string error))
            {
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"Segment split skipped: {error}");
                return true;
            }

            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory("\uC138\uADF8\uBA3C\uD2B8 \uC808\uB2E8");
            manualSegments.RemoveAt(sourceIndex);
            manualSegments.InsertRange(sourceIndex, splitResult.Segments);
            PushAnnotationHistorySnapshot(beforeChange);
            CancelPendingSegmentationSplit(updateStatus: false);
            MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(sourceIndex));
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);

            string direction = orientation == WpfSegmentationSplitOrientation.Vertical
                ? "\uC138\uB85C"
                : "\uAC00\uB85C";
            string status = FormattableString.Invariant(
                $"\uC138\uADF8\uBA3C\uD2B8 {direction} \uC808\uB2E8: 1\uAC1C \u2192 {splitResult.Segments.Count}\uAC1C / \uC88C\uD45C {coordinate}");
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
            return true;
        }

        private void CancelPendingSegmentationSplit(bool updateStatus)
        {
            bool wasPending = pendingSegmentationSplitOrientation.HasValue;
            pendingSegmentationSplitSource = null;
            pendingSegmentationSplitSourceIndex = -1;
            pendingSegmentationSplitOrientation = null;
            ObjectReviewViewModel?.SetSplitPending(null);
            if (MainCanvasViewModel != null)
            {
                MainCanvasViewModel.IsImagePointInputMode =
                    activeAnnotationTool == WpfAnnotationTool.Polygon
                    || activeAnnotationTool == WpfAnnotationTool.Brush
                    || activeAnnotationTool == WpfAnnotationTool.Eraser
                    || (activeAnnotationTool == WpfAnnotationTool.Select
                        && ObjectReviewViewModel?.IsSelectedSource(WpfObjectReviewSource.ManualSegment) == true);
            }

            if (wasPending && updateStatus)
            {
                const string status = "\uC138\uADF8\uBA3C\uD2B8 \uC808\uB2E8 \uC704\uCE58 \uC120\uD0DD\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }
    }
}
