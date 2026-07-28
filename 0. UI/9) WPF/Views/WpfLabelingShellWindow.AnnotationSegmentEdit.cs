using MvcVisionSystem._1._Core;
using OpenVisionLab.ImageCanvas.Canvas;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Selected segment editing is isolated from brush stroke commits because it mutates existing objects directly.
        private bool TryBeginSelectedSegmentEdit(CanvasImagePointEventArgs e)
        {
            if (e == null || e.Button != CanvasPointerButton.Left || activeImageSize.IsEmpty)
            {
                return false;
            }

            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item)
                || item.Source != WpfObjectReviewSource.ManualSegment
                || item.Index < 0
                || item.Index >= manualSegments.Count)
            {
                return false;
            }

            LabelingSegmentationObject segment = manualSegments[item.Index];
            if (segment == null || !CanEditManualSegment(segment))
            {
                return false;
            }

            int pointIndex = -1;
            if (segment.IsRasterMask)
            {
                if (!IsMaskPixelHit(segment, e.ImagePoint))
                {
                    return false;
                }
            }
            else
            {
                pointIndex = WpfPolygonAnnotationService.FindNearestPointIndex(segment, e.ImagePoint, maxDistancePixels: 8);
                if (pointIndex < 0 && !WpfPolygonAnnotationService.IsPointInsidePolygon(segment, e.ImagePoint))
                {
                    return false;
                }
            }

            WpfObjectSessionState sessionState = objectSessionStateService.GetManualSegmentState(segment);
            if (sessionState.IsPinned && (segment.IsRasterMask || pointIndex < 0))
            {
                SetYoloCommandStatus(
                    "\uC774\uB3D9 \uACE0\uC815\uB41C \uAC1D\uCCB4\uC785\uB2C8\uB2E4. \uACE0\uC815\uC744 \uD574\uC81C\uD558\uBA74 \uC804\uCCB4 \uC704\uCE58\uB97C \uC62E\uAE38 \uC218 \uC788\uC2B5\uB2C8\uB2E4.",
                    isBusy: false);
                return false;
            }

            activeSegmentDragIndex = item.Index;
            activePolygonPointDragIndex = pointIndex;
            lastSegmentDragPoint = e.ImagePoint;
            activeSegmentDragChanged = false;
            activeSegmentDragSnapshot = CaptureAnnotationHistory(segment.IsRasterMask
                ? "Move mask"
                : pointIndex >= 0 ? "Move polygon point" : "Move polygon");
            RefreshPolygonOverlays();
            SetYoloCommandStatus(segment.IsRasterMask
                ? "Mask selected: drag to move it."
                : pointIndex >= 0
                    ? $"Polygon point {pointIndex + 1} selected: drag to move it."
                    : "Polygon selected: drag inside to move it.",
                isBusy: false);
            return true;
        }

        private bool TryMoveSelectedSegmentEdit(CanvasImagePointEventArgs e)
        {
            if (e == null
                || e.Button != CanvasPointerButton.Left
                || activeSegmentDragIndex < 0
                || activeSegmentDragIndex >= manualSegments.Count
                || !lastSegmentDragPoint.HasValue)
            {
                return false;
            }

            LabelingSegmentationObject segment = manualSegments[activeSegmentDragIndex];
            if (segment == null)
            {
                return false;
            }

            bool changed;
            if (segment.IsRasterMask)
            {
                System.Drawing.Point previous = lastSegmentDragPoint.Value;
                changed = maskAnnotationService.TryMoveRasterMask(
                    segment,
                    e.ImagePoint.X - previous.X,
                    e.ImagePoint.Y - previous.Y,
                    activeImageSize,
                    out _);
            }
            else
            {
                System.Drawing.Point previous = lastSegmentDragPoint.Value;
                changed = activePolygonPointDragIndex >= 0
                    ? WpfPolygonAnnotationService.TryMovePoint(
                        segment,
                        activePolygonPointDragIndex,
                        e.ImagePoint,
                        activeImageSize,
                        out _)
                    : WpfPolygonAnnotationService.TryMovePolygon(
                        segment,
                        e.ImagePoint.X - previous.X,
                        e.ImagePoint.Y - previous.Y,
                        activeImageSize,
                        out _);
            }

            if (!changed)
            {
                return true;
            }

            lastSegmentDragPoint = e.ImagePoint;
            activeSegmentDragChanged = true;
            RefreshPolygonOverlays();
            return true;
        }

        private void CompleteSelectedSegmentEdit()
        {
            bool changed = activeSegmentDragChanged;
            bool movedPoint = activePolygonPointDragIndex >= 0;
            if (activeSegmentDragSnapshot != null && activeSegmentDragChanged)
            {
                PushAnnotationHistorySnapshot(activeSegmentDragSnapshot);
                AppendLog(movedPoint
                    ? "Polygon point moved."
                    : "Mask or polygon moved.");
            }

            activeSegmentDragIndex = -1;
            activePolygonPointDragIndex = -1;
            lastSegmentDragPoint = null;
            activeSegmentDragSnapshot = null;
            activeSegmentDragChanged = false;
            RefreshPolygonOverlays();
            if (changed)
            {
                MarkAnnotationsDirty(movedPoint
                    ? "Move polygon point"
                    : "Move polygon");
                RefreshObjectList();
                RefreshActiveImageQueueStatus(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            }
        }

        private static bool IsMaskPixelHit(LabelingSegmentationObject segment, System.Drawing.Point imagePoint)
        {
            if (segment?.IsRasterMask != true || segment.Bounds.IsEmpty || !segment.Bounds.Contains(imagePoint))
            {
                return false;
            }

            int index = (imagePoint.Y * segment.MaskSize.Width) + imagePoint.X;
            return index >= 0 && index < segment.MaskData.Length && segment.MaskData[index] != 0;
        }

    }
}
