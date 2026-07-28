using System;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteSendSegmentationToBackCommand()
            => MoveSelectedSegmentationZOrder(WpfSegmentationZOrderMove.SendToBack);

        private void ExecuteSendSegmentationBackwardCommand()
            => MoveSelectedSegmentationZOrder(WpfSegmentationZOrderMove.SendBackward);

        private void ExecuteBringSegmentationForwardCommand()
            => MoveSelectedSegmentationZOrder(WpfSegmentationZOrderMove.BringForward);

        private void ExecuteBringSegmentationToFrontCommand()
            => MoveSelectedSegmentationZOrder(WpfSegmentationZOrderMove.BringToFront);

        private void MoveSelectedSegmentationZOrder(WpfSegmentationZOrderMove move)
        {
            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            string error = string.Empty;
            WpfSegmentationZOrderResult result = null;
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected)
                || selected.Source != WpfObjectReviewSource.ManualSegment)
            {
                error = "\uC21C\uC11C\uB97C \uBCC0\uACBD\uD560 \uC138\uADF8\uBA3C\uD2B8\uB97C \uD558\uB098 \uC120\uD0DD\uD558\uC138\uC694.";
            }
            else
            {
                segmentationZOrderService.TryPlanMove(
                    manualSegments,
                    selected.Index,
                    move,
                    out result,
                    out error);
            }

            if (result == null)
            {
                SetYoloCommandStatus(error, isBusy: false);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendLog($"Segment z-order skipped: {error}");
                }

                ObjectReviewViewModel?.RefreshActionState();
                return;
            }

            if (!CanMutateSelectedObject(selected, requireVisible: true, out string stateError))
            {
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return;
            }

            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory("\uC138\uADF8\uBA3C\uD2B8 \uD45C\uC2DC \uC21C\uC11C \uBCC0\uACBD");
            var previousZOrders = manualSegments
                .Select(segment => (Segment: segment, ZOrder: segment.ZOrder))
                .ToList();
            manualSegments.Clear();
            manualSegments.AddRange(result.OrderedSegments);
            for (int index = 0; index < manualSegments.Count; index++)
            {
                LabelingSegmentationObject segment = manualSegments[index];
                if (segment == null)
                {
                    continue;
                }

                int previousZOrder = previousZOrders
                    .First(item => ReferenceEquals(item.Segment, segment))
                    .ZOrder;
                if (previousZOrder != index || index == result.SelectedIndex)
                {
                    segment.LastStructuralOperation = WpfSegmentationZOrderService.StructuralOperationName;
                }

                segment.ZOrder = index;
            }

            PushAnnotationHistorySnapshot(beforeChange);
            MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(result.SelectedIndex));
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);

            string action = FormatSegmentationZOrderMove(result.Move);
            string status = FormattableString.Invariant(
                $"{action}: {result.SelectedIndex + 1}/{manualSegments.Count} (\uC22B\uC790\uAC00 \uD074\uC218\uB85D \uC55E)");
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private static string FormatSegmentationZOrderMove(WpfSegmentationZOrderMove move)
            => move switch
            {
                WpfSegmentationZOrderMove.SendToBack => "\uB9E8 \uB4A4\uB85C",
                WpfSegmentationZOrderMove.SendBackward => "\uD55C \uCE78 \uB4A4\uB85C",
                WpfSegmentationZOrderMove.BringForward => "\uD55C \uCE78 \uC55E\uC73C\uB85C",
                WpfSegmentationZOrderMove.BringToFront => "\uB9E8 \uC55E\uC73C\uB85C",
                _ => "\uD45C\uC2DC \uC21C\uC11C \uBCC0\uACBD"
            };
    }
}
