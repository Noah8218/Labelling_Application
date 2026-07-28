using System;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecutePreviewSegmentationRemoveUnderlyingCommand()
        {
            CancelPendingIntelligentScissors(updateStatus: false);
            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            string error = "\uB4A4\uCABD \uAC1D\uCCB4\uC640 \uACB9\uCE68\uC744 \uBD84\uC11D\uD560 \uC138\uADF8\uBA3C\uD2B8\uB97C \uD558\uB098 \uC120\uD0DD\uD558\uC138\uC694.";
            WpfSegmentationRemoveUnderlyingPlan plan = null;
            bool hasSelection = TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected)
                && selected.Source == WpfObjectReviewSource.ManualSegment;
            if (!hasSelection
                || !segmentationRemoveUnderlyingService.TryAnalyze(
                    manualSegments,
                    selected.Index,
                    activeImageSize,
                    out plan,
                    out error))
            {
                CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
                SetYoloCommandStatus(error, isBusy: false);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendLog($"Remove-underlying analysis skipped: {error}");
                }

                return;
            }

            if (!CanMutateSelectedObject(selected, requireVisible: true, out string stateError))
            {
                CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return;
            }

            pendingSegmentationRemoveUnderlyingPlan = plan;
            string affected = string.Join(
                ", ",
                plan.Changes
                    .OrderBy(change => change.SourceIndex)
                    .Select(change => $"#{change.SourceIndex + 1}"));
            string warning = FormattableString.Invariant(
                $"\uC601\uD5A5 {plan.Changes.Count}\uAC1C({affected}) \u00B7 \uC81C\uAC70 {plan.RemovedPixelCount:N0}px \u00B7 \uC644\uC804 \uC0AD\uC81C {plan.RemovedObjectCount}\uAC1C. '\uD655\uC778 \uD6C4 \uC81C\uAC70'\uB97C \uB204\uB974\uBA74 \uB4A4\uCABD geometry\uAC00 \uBCC0\uACBD\uB429\uB2C8\uB2E4.");
            ObjectReviewViewModel?.SetRemoveUnderlyingPreview(true, warning);
            RefreshPolygonOverlays();
            SetYoloCommandStatus(warning, isBusy: false);
            AppendLog($"Remove-underlying preview: {warning}");
        }

        private void ExecuteApplySegmentationRemoveUnderlyingCommand()
        {
            WpfSegmentationRemoveUnderlyingPlan pending = pendingSegmentationRemoveUnderlyingPlan;
            if (pending == null)
            {
                return;
            }

            string error = string.Empty;
            WpfSegmentationRemoveUnderlyingPlan current = null;
            bool currentSelectionMatches = pending.SelectedIndex >= 0
                && pending.SelectedIndex < manualSegments.Count
                && ReferenceEquals(manualSegments[pending.SelectedIndex], pending.SelectedSource);
            bool currentAnalysisMatches = currentSelectionMatches
                && segmentationRemoveUnderlyingService.TryAnalyze(
                    manualSegments,
                    pending.SelectedIndex,
                    activeImageSize,
                    out current,
                    out error)
                && string.Equals(current.Signature, pending.Signature, StringComparison.Ordinal);
            if (!currentAnalysisMatches)
            {
                CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
                const string stale = "\uB77C\uBCA8 geometry\uB098 \uD45C\uC2DC \uC21C\uC11C\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uAE30\uC874 \uC601\uD5A5 \uBD84\uC11D\uC744 \uC801\uC6A9\uD558\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uACB9\uCE68\uC744 \uBD84\uC11D\uD558\uC138\uC694.";
                SetYoloCommandStatus(stale, isBusy: false);
                AppendLog($"Remove-underlying stale preview rejected: {FirstNonEmpty(error, "selection or geometry changed")}");
                return;
            }

            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory("\uB4A4\uCABD \uACB9\uCE68 \uC81C\uAC70");
            foreach (WpfSegmentationRemoveUnderlyingChange change in current.Changes
                .OrderByDescending(change => change.SourceIndex))
            {
                if (change.Replacement == null)
                {
                    manualSegments.RemoveAt(change.SourceIndex);
                }
                else
                {
                    manualSegments[change.SourceIndex] = change.Replacement;
                }
            }

            int selectedIndex = manualSegments.IndexOf(current.SelectedSource);
            PushAnnotationHistorySnapshot(beforeChange);
            CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
            MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(selectedIndex >= 0
                ? WpfObjectReviewItemRef.ManualSegment(selectedIndex)
                : null);
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);

            string status = FormattableString.Invariant(
                $"\uB4A4\uCABD \uACB9\uCE68 \uC81C\uAC70: \uC601\uD5A5 {current.Changes.Count}\uAC1C / {current.RemovedPixelCount:N0}px / \uC644\uC804 \uC0AD\uC81C {current.RemovedObjectCount}\uAC1C");
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteCancelSegmentationRemoveUnderlyingCommand()
            => CancelPendingSegmentationRemoveUnderlying(updateStatus: true);

        private void CancelPendingSegmentationRemoveUnderlying(bool updateStatus)
        {
            bool wasPending = pendingSegmentationRemoveUnderlyingPlan != null;
            pendingSegmentationRemoveUnderlyingPlan = null;
            ObjectReviewViewModel?.SetRemoveUnderlyingPreview(false);
            if (wasPending)
            {
                RefreshPolygonOverlays();
            }

            if (wasPending && updateStatus)
            {
                const string status = "\uB4A4\uCABD \uACB9\uCE68 \uC81C\uAC70 \uBBF8\uB9AC\uBCF4\uAE30\uB97C \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }

        private bool IsPendingRemoveUnderlyingAffectedIndex(int sourceIndex)
            => pendingSegmentationRemoveUnderlyingPlan?.Changes
                .Any(change => change.SourceIndex == sourceIndex) == true;
    }
}
