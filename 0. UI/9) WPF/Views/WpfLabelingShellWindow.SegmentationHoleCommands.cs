using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.ViewModels;
using System;
using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteBeginAddSegmentationHoleCommand()
            => BeginPendingSegmentationHoleEdit(WpfSegmentationHoleEditMode.Add);

        private void ExecuteBeginRemoveSegmentationHoleCommand()
            => BeginPendingSegmentationHoleEdit(WpfSegmentationHoleEditMode.Remove);

        private void ExecuteCancelSegmentationHoleEditCommand()
            => CancelPendingSegmentationHoleEdit(updateStatus: true);

        private void BeginPendingSegmentationHoleEdit(WpfSegmentationHoleEditMode mode)
        {
            CancelPendingIntelligentScissors(updateStatus: false);
            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            if (smartMaskPromptSession.HasSession || isCreatingSmartMask)
            {
                const string smartMaskError = "\uC2A4\uB9C8\uD2B8 \uB9C8\uC2A4\uD06C \uD6C4\uBCF4\uB97C \uD655\uC815\uD558\uAC70\uB098 \uCDE8\uC18C\uD55C \uB4A4 \uAD6C\uBA4D\uC744 \uD3B8\uC9D1\uD558\uC138\uC694.";
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
                const string selectionError = "\uAD6C\uBA4D\uC744 \uD3B8\uC9D1\uD560 \uD3F4\uB9AC\uACE4 \uB610\uB294 \uB9C8\uC2A4\uD06C \uAC1D\uCCB4\uB97C \uD558\uB098 \uC120\uD0DD\uD558\uC138\uC694.";
                SetYoloCommandStatus(selectionError, isBusy: false);
                AppendLog(selectionError);
                return;
            }

            if (!CanMutateSelectedObject(selected, requireVisible: true, out string stateError))
            {
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return;
            }

            SelectAnnotationTool(WpfAnnotationTool.Select);
            pendingSegmentationHoleSourceIndex = selected.Index;
            pendingSegmentationHoleSource = manualSegments[selected.Index];
            pendingSegmentationHoleEditMode = mode;
            holePolygonAnnotationService.Reset();
            ObjectReviewViewModel?.SetHoleEditPending(mode);
            MainCanvasViewModel.IsImagePointInputMode = true;
            MainCanvasViewModel.ImageViewer.SetViewMode(CanvasInteractionMode.None);
            RefreshPolygonOverlays();

            string status = mode == WpfSegmentationHoleEditMode.Add
                ? "\uAD6C\uBA4D \uADF8\uB9AC\uAE30: \uAC1D\uCCB4 \uC548\uCABD\uC5D0 \uC810\uC744 \uD074\uB9AD\uD558\uACE0 \uCCAB \uC810 \uB610\uB294 \uB354\uBE14\uD074\uB9AD\uC73C\uB85C \uC644\uB8CC\uD558\uC138\uC694."
                : "\uAD6C\uBA4D \uCC44\uC6B0\uAE30: \uC678\uBD80 \uBC30\uACBD\uACFC \uC5F0\uACB0\uB418\uC9C0 \uC54A\uC740 \uB0B4\uBD80 \uAD6C\uBA4D\uC744 \uD074\uB9AD\uD558\uC138\uC694.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryApplyPendingSegmentationHoleEdit(CanvasImagePointEventArgs e)
        {
            if (!pendingSegmentationHoleEditMode.HasValue)
            {
                return false;
            }

            if (e?.Button == CanvasPointerButton.Right)
            {
                CancelPendingSegmentationHoleEdit(updateStatus: true);
                return true;
            }

            if (e?.Button != CanvasPointerButton.Left)
            {
                return true;
            }

            if (!TryResolvePendingSegmentationHoleSource(out int sourceIndex, out LabelingSegmentationObject source))
            {
                CancelPendingSegmentationHoleEdit(updateStatus: false);
                const string staleSelectionError = "\uC120\uD0DD\uD55C \uAC1D\uCCB4\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uAD6C\uBA4D \uD3B8\uC9D1\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(staleSelectionError, isBusy: false);
                AppendLog(staleSelectionError);
                return true;
            }

            if (pendingSegmentationHoleEditMode == WpfSegmentationHoleEditMode.Remove)
            {
                if (!segmentationHoleService.TryRemoveHole(
                    source,
                    e.ImagePoint,
                    activeImageSize,
                    out LabelingSegmentationObject filled,
                    out string error))
                {
                    SetYoloCommandStatus(error, isBusy: false);
                    AppendLog($"Segment hole fill skipped: {error}");
                    return true;
                }

                ApplySegmentationHoleEdit(sourceIndex, filled, "\uB0B4\uBD80 \uAD6C\uBA4D \uCC44\uC6B0\uAE30");
                return true;
            }

            if (e.Clicks > 1 && holePolygonAnnotationService.Points.Count >= 3)
            {
                CompletePendingSegmentationHoleAddition(sourceIndex, source);
                return true;
            }

            if (!holePolygonAnnotationService.TryAddPoint(e.ImagePoint, activeImageSize, out bool closed))
            {
                return true;
            }

            RefreshPolygonOverlays();
            if (closed)
            {
                CompletePendingSegmentationHoleAddition(sourceIndex, source);
                return true;
            }

            SetYoloCommandStatus(
                $"\uAD6C\uBA4D \uB2E4\uAC01\uD615: {holePolygonAnnotationService.Points.Count}\uC810 / \uCCAB \uC810 \uB610\uB294 \uB354\uBE14\uD074\uB9AD\uC73C\uB85C \uC644\uB8CC",
                isBusy: false);
            return true;
        }

        private void CompletePendingSegmentationHoleAddition(
            int sourceIndex,
            LabelingSegmentationObject source)
        {
            if (!segmentationHoleService.TryAddHole(
                source,
                holePolygonAnnotationService.Points,
                activeImageSize,
                out LabelingSegmentationObject edited,
                out string error))
            {
                holePolygonAnnotationService.Reset();
                RefreshPolygonOverlays();
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"Segment hole add skipped: {error}");
                return;
            }

            ApplySegmentationHoleEdit(sourceIndex, edited, "\uB0B4\uBD80 \uAD6C\uBA4D \uCD94\uAC00");
        }

        private void ApplySegmentationHoleEdit(
            int sourceIndex,
            LabelingSegmentationObject edited,
            string actionName)
        {
            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory(actionName);
            manualSegments[sourceIndex] = edited;
            PushAnnotationHistorySnapshot(beforeChange);
            CancelPendingSegmentationHoleEdit(updateStatus: false);
            MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(sourceIndex));
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);

            string status = $"{actionName}: {edited.ClassName} / {edited.LastStructuralOperation}";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryResolvePendingSegmentationHoleSource(
            out int sourceIndex,
            out LabelingSegmentationObject source)
        {
            sourceIndex = pendingSegmentationHoleSourceIndex;
            source = pendingSegmentationHoleSource;
            return source != null
                && sourceIndex >= 0
                && sourceIndex < manualSegments.Count
                && ReferenceEquals(manualSegments[sourceIndex], source);
        }

        private void CancelPendingSegmentationHoleEdit(bool updateStatus)
        {
            bool wasPending = pendingSegmentationHoleEditMode.HasValue;
            pendingSegmentationHoleSource = null;
            pendingSegmentationHoleSourceIndex = -1;
            pendingSegmentationHoleEditMode = null;
            holePolygonAnnotationService.Reset();
            ObjectReviewViewModel?.SetHoleEditPending(null);
            if (MainCanvasViewModel != null)
            {
                MainCanvasViewModel.IsImagePointInputMode =
                    activeAnnotationTool == WpfAnnotationTool.Polygon
                    || activeAnnotationTool == WpfAnnotationTool.Brush
                    || activeAnnotationTool == WpfAnnotationTool.Eraser
                    || (activeAnnotationTool == WpfAnnotationTool.Select
                        && ObjectReviewViewModel?.IsSelectedSource(WpfObjectReviewSource.ManualSegment) == true);
            }

            RefreshPolygonOverlays();
            if (wasPending && updateStatus)
            {
                const string status = "\uB0B4\uBD80 \uAD6C\uBA4D \uD3B8\uC9D1\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }
    }
}
