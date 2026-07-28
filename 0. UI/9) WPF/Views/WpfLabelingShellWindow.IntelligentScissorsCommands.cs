using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.ViewModels;
using System;
using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteBeginIntelligentScissorsCommand()
        {
            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                const string imageError = "\uACBD\uACC4\uB97C \uBD84\uC11D\uD560 \uC774\uBBF8\uC9C0\uB97C \uBA3C\uC800 \uC5EC\uC138\uC694.";
                SetYoloCommandStatus(imageError, isBusy: false);
                AppendLog(imageError);
                return;
            }

            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected)
                || selected.Source != WpfObjectReviewSource.ManualSegment
                || selected.Index < 0
                || selected.Index >= manualSegments.Count
                || manualSegments[selected.Index]?.IsRasterMask != false)
            {
                const string selectionError = "\uACBD\uACC4\uB97C \uB2E4\uC2DC \uACC4\uC0B0\uD560 \uC218\uB3D9 \uD3F4\uB9AC\uACE4\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
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

            CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
            CancelPendingPolygonVertexEdit(updateStatus: false);
            CancelPendingSegmentationSplit(updateStatus: false);
            CancelPendingSegmentationHoleEdit(updateStatus: false);
            SelectAnnotationTool(WpfAnnotationTool.Select);
            pendingIntelligentScissorsSourceIndex = selected.Index;
            pendingIntelligentScissorsSource = manualSegments[selected.Index];
            pendingIntelligentScissorsPlan = null;
            ObjectReviewViewModel?.SetIntelligentScissorsState(
                pending: true,
                hasPreview: false,
                statusText: "\uACBD\uACC4 \uCD94\uC885: \uB2E4\uC2DC \uACC4\uC0B0\uD560 \uD3F4\uB9AC\uACE4 \uBAA8\uC11C\uB9AC \uADFC\uCC98\uB97C \uD074\uB9AD\uD558\uC138\uC694. \uC6B0\uD074\uB9AD\uC740 \uCDE8\uC18C\uC785\uB2C8\uB2E4.");
            MainCanvasViewModel.IsImagePointInputMode = true;
            MainCanvasViewModel.ImageViewer.SetViewMode(CanvasInteractionMode.None);
            RefreshPolygonOverlays();
            const string status = "\uACBD\uACC4 \uCD94\uC885: \uD3F4\uB9AC\uACE4 \uBAA8\uC11C\uB9AC\uB97C \uD074\uB9AD\uD558\uBA74 \uC774\uBBF8\uC9C0 \uACBD\uACC4 \uACBD\uB85C\uB97C \uBBF8\uB9AC\uBCF4\uAE30\uD569\uB2C8\uB2E4.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryHandlePendingIntelligentScissors(CanvasImagePointEventArgs e)
        {
            if (pendingIntelligentScissorsSource == null)
            {
                return false;
            }

            if (e?.Button == CanvasPointerButton.Right)
            {
                CancelPendingIntelligentScissors(updateStatus: true);
                return true;
            }

            if (e?.Button != CanvasPointerButton.Left)
            {
                return true;
            }

            if (!TryResolvePendingIntelligentScissorsSource(out int sourceIndex, out LabelingSegmentationObject source))
            {
                CancelPendingIntelligentScissors(updateStatus: false);
                const string staleSelectionError = "\uC120\uD0DD\uD55C \uAC1D\uCCB4\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uACBD\uACC4 \uCD94\uC885\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(staleSelectionError, isBusy: false);
                AppendLog(staleSelectionError);
                return true;
            }

            int hitTolerance = WpfPolygonAnnotationService.ResolveImageHitTolerance(
                MainCanvasViewModel?.ImageViewer?.ZoomScale ?? 1F);
            if (!intelligentScissorsService.TryBuildPlan(
                activeImageBitmap,
                source,
                e.ImagePoint,
                activeImageSize,
                hitTolerance,
                out WpfIntelligentScissorsPlan plan,
                out string error))
            {
                pendingIntelligentScissorsPlan = null;
                ObjectReviewViewModel?.SetIntelligentScissorsState(
                    pending: true,
                    hasPreview: false,
                    statusText: error);
                RefreshPolygonOverlays();
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"Intelligent scissors preview skipped: {error}");
                return true;
            }

            pendingIntelligentScissorsPlan = plan;
            string previewStatus = FormattableString.Invariant(
                $"\uACBD\uACC4 \uBBF8\uB9AC\uBCF4\uAE30: {plan.PathPoints.Count}\uAC1C \uACBD\uB85C\uC810 / {plan.Elapsed.TotalMilliseconds:0.0} ms. \uCE94\uBC84\uC2A4\uB97C \uD655\uC778\uD55C \uD6C4 \uBBF8\uB9AC\uBCF4\uAE30 \uC801\uC6A9\uC744 \uB204\uB974\uC138\uC694.");
            ObjectReviewViewModel?.SetIntelligentScissorsState(
                pending: true,
                hasPreview: true,
                statusText: previewStatus);
            RefreshPolygonOverlays();
            SetYoloCommandStatus(previewStatus, isBusy: false);
            AppendLog($"Intelligent scissors preview: segment {sourceIndex + 1} / edge {plan.EdgeIndex + 1} / {plan.PathPoints.Count} points / {plan.Elapsed.TotalMilliseconds:0.0} ms");
            return true;
        }

        private void ExecuteApplyIntelligentScissorsCommand()
        {
            WpfIntelligentScissorsPlan plan = pendingIntelligentScissorsPlan;
            if (plan == null
                || !TryResolvePendingIntelligentScissorsSource(out int sourceIndex, out LabelingSegmentationObject source))
            {
                const string previewError = "\uC801\uC6A9\uD560 \uACBD\uACC4 \uBBF8\uB9AC\uBCF4\uAE30\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(previewError, isBusy: false);
                AppendLog(previewError);
                return;
            }

            var selected = WpfObjectReviewItemRef.ManualSegment(sourceIndex);
            if (!CanMutateSelectedObject(selected, requireVisible: true, out string stateError))
            {
                CancelPendingIntelligentScissors(updateStatus: false);
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return;
            }

            const string actionName = "\uD3F4\uB9AC\uACE4 \uACBD\uACC4 \uCD94\uC885";
            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory(actionName);
            if (!intelligentScissorsService.TryApplyPlan(
                source,
                plan,
                activeImageSize,
                out Rectangle _,
                out string error))
            {
                CancelPendingIntelligentScissors(updateStatus: false);
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"{actionName} skipped: {error}");
                return;
            }

            PushAnnotationHistorySnapshot(beforeChange);
            CancelPendingIntelligentScissors(updateStatus: false);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(sourceIndex));
            MarkAnnotationsDirty(actionName);
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            string status = $"{actionName}: {source.Points.Count}\uAC1C \uC815\uC810 / \uBBF8\uB9AC\uBCF4\uAE30 \uACBD\uB85C \uC801\uC6A9";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteCancelIntelligentScissorsCommand()
            => CancelPendingIntelligentScissors(updateStatus: true);

        private bool TryResolvePendingIntelligentScissorsSource(
            out int sourceIndex,
            out LabelingSegmentationObject source)
        {
            sourceIndex = pendingIntelligentScissorsSourceIndex;
            source = pendingIntelligentScissorsSource;
            return source?.IsRasterMask == false
                && sourceIndex >= 0
                && sourceIndex < manualSegments.Count
                && ReferenceEquals(manualSegments[sourceIndex], source);
        }

        private void CancelPendingIntelligentScissors(bool updateStatus)
        {
            bool wasPending = pendingIntelligentScissorsSource != null;
            pendingIntelligentScissorsSource = null;
            pendingIntelligentScissorsSourceIndex = -1;
            pendingIntelligentScissorsPlan = null;
            ObjectReviewViewModel?.SetIntelligentScissorsState(
                pending: false,
                hasPreview: false);
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
                const string status = "\uACBD\uACC4 \uCD94\uC885 \uBBF8\uB9AC\uBCF4\uAE30\uB97C \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }
    }
}
