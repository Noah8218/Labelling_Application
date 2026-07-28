using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.ViewModels;
using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteBeginInsertPolygonVertexCommand()
            => BeginPendingPolygonVertexEdit(WpfPolygonVertexEditMode.Insert);

        private void ExecuteBeginDeletePolygonVertexCommand()
            => BeginPendingPolygonVertexEdit(WpfPolygonVertexEditMode.Delete);

        private void ExecuteCancelPolygonVertexEditCommand()
            => CancelPendingPolygonVertexEdit(updateStatus: true);

        private void BeginPendingPolygonVertexEdit(WpfPolygonVertexEditMode mode)
        {
            CancelPendingIntelligentScissors(updateStatus: false);
            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            if (smartMaskPromptSession.HasSession || isCreatingSmartMask)
            {
                const string smartMaskError = "\uC2A4\uB9C8\uD2B8 \uB9C8\uC2A4\uD06C \uD6C4\uBCF4\uB97C \uD655\uC815\uD558\uAC70\uB098 \uCDE8\uC18C\uD55C \uB4A4 \uD3F4\uB9AC\uACE4 \uC815\uC810\uC744 \uD3B8\uC9D1\uD558\uC138\uC694.";
                SetYoloCommandStatus(smartMaskError, isBusy: false);
                AppendLog(smartMaskError);
                return;
            }

            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected)
                || selected.Source != WpfObjectReviewSource.ManualSegment
                || selected.Index < 0
                || selected.Index >= manualSegments.Count
                || manualSegments[selected.Index]?.IsRasterMask != false
                || activeImageSize.IsEmpty)
            {
                const string selectionError = "\uC815\uC810\uC744 \uD3B8\uC9D1\uD560 \uC218\uB3D9 \uD3F4\uB9AC\uACE4 \uAC1D\uCCB4\uB97C \uD558\uB098 \uC120\uD0DD\uD558\uC138\uC694.";
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
            pendingPolygonVertexSourceIndex = selected.Index;
            pendingPolygonVertexSource = manualSegments[selected.Index];
            pendingPolygonVertexEditMode = mode;
            ObjectReviewViewModel?.SetVertexEditPending(mode);
            MainCanvasViewModel.IsImagePointInputMode = true;
            MainCanvasViewModel.ImageViewer.SetViewMode(CanvasInteractionMode.None);
            RefreshPolygonOverlays();

            string status = mode == WpfPolygonVertexEditMode.Insert
                ? "\uC815\uC810 \uCD94\uAC00: \uC120\uD0DD\uD55C \uD3F4\uB9AC\uACE4 \uBAA8\uC11C\uB9AC \uADFC\uCC98\uB97C \uD074\uB9AD\uD558\uC138\uC694. \uC6B0\uD074\uB9AD\uC740 \uCDE8\uC18C\uC785\uB2C8\uB2E4."
                : "\uC815\uC810 \uC0AD\uC81C: \uC0AD\uC81C\uD560 \uC815\uC810 \uADFC\uCC98\uB97C \uD074\uB9AD\uD558\uC138\uC694. \uC6B0\uD074\uB9AD\uC740 \uCDE8\uC18C\uC785\uB2C8\uB2E4.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryApplyPendingPolygonVertexEdit(CanvasImagePointEventArgs e)
        {
            if (!pendingPolygonVertexEditMode.HasValue)
            {
                return false;
            }

            if (e?.Button == CanvasPointerButton.Right)
            {
                CancelPendingPolygonVertexEdit(updateStatus: true);
                return true;
            }

            if (e?.Button != CanvasPointerButton.Left)
            {
                return true;
            }

            if (!TryResolvePendingPolygonVertexSource(out int sourceIndex, out LabelingSegmentationObject source))
            {
                CancelPendingPolygonVertexEdit(updateStatus: false);
                const string staleSelectionError = "\uC120\uD0DD\uD55C \uAC1D\uCCB4\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uC815\uC810 \uD3B8\uC9D1\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(staleSelectionError, isBusy: false);
                AppendLog(staleSelectionError);
                return true;
            }

            int hitTolerance = WpfPolygonAnnotationService.ResolveImageHitTolerance(
                MainCanvasViewModel?.ImageViewer?.ZoomScale ?? 1F);
            WpfPolygonVertexEditMode mode = pendingPolygonVertexEditMode.Value;
            string actionName = mode == WpfPolygonVertexEditMode.Insert
                ? "\uD3F4\uB9AC\uACE4 \uC815\uC810 \uCD94\uAC00"
                : "\uD3F4\uB9AC\uACE4 \uC815\uC810 \uC0AD\uC81C";
            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory(actionName);
            bool changed = mode == WpfPolygonVertexEditMode.Insert
                ? WpfPolygonAnnotationService.TryInsertPoint(
                    source,
                    e.ImagePoint,
                    activeImageSize,
                    hitTolerance,
                    out int _,
                    out Rectangle _,
                    out string error)
                : WpfPolygonAnnotationService.TryDeletePoint(
                    source,
                    e.ImagePoint,
                    activeImageSize,
                    hitTolerance,
                    out int _,
                    out Rectangle _,
                    out error);
            if (!changed)
            {
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"{actionName} skipped: {error}");
                return true;
            }

            PushAnnotationHistorySnapshot(beforeChange);
            CancelPendingPolygonVertexEdit(updateStatus: false);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(sourceIndex));
            MarkAnnotationsDirty(actionName);
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            string status = $"{actionName}: {source.Points.Count}\uAC1C \uC815\uC810";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
            return true;
        }

        private bool TryResolvePendingPolygonVertexSource(
            out int sourceIndex,
            out LabelingSegmentationObject source)
        {
            sourceIndex = pendingPolygonVertexSourceIndex;
            source = pendingPolygonVertexSource;
            return source?.IsRasterMask == false
                && sourceIndex >= 0
                && sourceIndex < manualSegments.Count
                && ReferenceEquals(manualSegments[sourceIndex], source);
        }

        private void CancelPendingPolygonVertexEdit(bool updateStatus)
        {
            bool wasPending = pendingPolygonVertexEditMode.HasValue;
            pendingPolygonVertexSource = null;
            pendingPolygonVertexSourceIndex = -1;
            pendingPolygonVertexEditMode = null;
            ObjectReviewViewModel?.SetVertexEditPending(null);
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
                const string status = "\uD3F4\uB9AC\uACE4 \uC815\uC810 \uD3B8\uC9D1\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }
    }
}
