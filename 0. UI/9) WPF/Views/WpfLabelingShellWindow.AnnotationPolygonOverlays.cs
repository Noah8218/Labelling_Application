using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.ImageCanvas.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Polygon completion and overlay refresh are grouped away from ROI rectangle synchronization.
        private void CompletePolygonAnnotation()
        {
            CClassItem classItem = EnsureClassItem(FirstNonEmpty(GetSelectedClassName(), "Defect"));
            if (!polygonAnnotationService.TryComplete(classItem, activeImageSize, out LabelingSegmentationObject annotation, out string message))
            {
                SetYoloCommandStatus(message, isBusy: false);
                return;
            }

            RegisterAnnotationHistoryBeforeChange("Add polygon");
            annotation.ZOrder = WpfSegmentationZOrderService.GetNextZOrder(manualSegments);
            manualSegments.Add(annotation);
            polygonAnnotationService.Reset();
            RefreshPolygonOverlays();
            RefreshObjectList();
            ShowSavedLabelsWorkflowView();
            SetModelStatus($"Polygon added: {annotation.ClassName} / {annotation.Points.Count} points");
            AppendLog($"Polygon added: {annotation.ClassName} / {annotation.Points.Count} points / {FormatSegmentBoundsCompact(annotation)}");
            RefreshActiveImageQueueStatus(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
        }

        private void RefreshPolygonOverlays()
        {
            if (MainCanvasViewModel == null)
            {
                return;
            }

            bool showSavedLabels = ShouldShowLabelOverlays();
            bool showSmartMaskPrompts = smartMaskPromptSession.HasSession;
            if ((!showSavedLabels && !showSmartMaskPrompts) || !IsSegmentationDatasetPurposeActive())
            {
                ClearSegmentationOverlays();
                return;
            }

            var overlays = new List<RoiImageCanvasPolygonOverlay>();
            var maskOverlays = new List<RoiImageCanvasMaskOverlay>();
            float maskOpacity = (float)(LearningWorkflowViewModel?.MaskOpacity ?? 0.66);
            WpfObjectReviewListItem selectedObject = ObjectReviewViewModel?.SelectedObject;
            string selectedSourceKey = selectedObject?.SourceKey ?? string.Empty;
            int selectedSourceIndex = selectedObject?.SourceIndex ?? -1;
            if (showSavedLabels)
            {
                IEnumerable<int> renderIndices = manualSegments
                    .Select((segment, index) => new { Segment = segment, Index = index })
                    .Where(item => item.Segment != null)
                    .OrderBy(item => item.Segment.ZOrder)
                    .ThenBy(item => item.Index)
                    .Select(item => item.Index);
                foreach (int i in renderIndices)
                {
                    LabelingSegmentationObject segment = manualSegments[i];
                    WpfObjectSessionState sessionState = GetManualSegmentSessionState(i);
                    if (sessionState.IsHidden)
                    {
                        continue;
                    }

                    string className = FirstNonEmpty(segment.ClassName, segment.ClassItem?.Text, "Defect");
                    bool isSegmentSelected = string.Equals(
                        selectedSourceKey,
                        WpfObjectReviewSource.ManualSegment.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                        && selectedSourceIndex == i;
                    if (segment.IsRasterMask)
                    {
                        if (TryBuildManualMaskOverlay(i, selectedSourceKey, selectedSourceIndex, maskOpacity, out RoiImageCanvasMaskOverlay maskOverlay))
                        {
                            maskOverlays.Add(maskOverlay);
                        }

                        continue;
                    }

                    if (segment.Points == null || segment.Points.Count == 0)
                    {
                        continue;
                    }

                    bool isRemoveUnderlyingPreview = IsPendingRemoveUnderlyingAffectedIndex(i);
                    System.Drawing.Color overlayColor = isRemoveUnderlyingPreview
                        ? System.Drawing.Color.Orange
                        : segment.Color;
                    overlays.Add(new RoiImageCanvasPolygonOverlay(
                        segment.Points,
                        isRemoveUnderlyingPreview
                            ? $"REMOVE PREVIEW {i + 1} {className}"
                            : $"SEG {i + 1} {className}",
                        overlayColor,
                        isClosed: true,
                        isDraft: false,
                        isSelected: isSegmentSelected,
                        selectedPointIndex: activeSegmentDragIndex == i ? activePolygonPointDragIndex : -1));
                }

                if (polygonAnnotationService.Points.Count > 0)
                {
                    overlays.Add(new RoiImageCanvasPolygonOverlay(
                        polygonAnnotationService.Points,
                        $"Draft {polygonAnnotationService.Points.Count}",
                        System.Drawing.Color.FromArgb(80, 180, 255),
                        polygonAnnotationService.IsClosed,
                        isDraft: true));
                }

                if (pendingSegmentationHoleEditMode == WpfSegmentationHoleEditMode.Add
                    && holePolygonAnnotationService.Points.Count > 0)
                {
                    overlays.Add(new RoiImageCanvasPolygonOverlay(
                        holePolygonAnnotationService.Points,
                        $"HOLE DRAFT {holePolygonAnnotationService.Points.Count}",
                        System.Drawing.Color.Orange,
                        holePolygonAnnotationService.IsClosed,
                        isDraft: true,
                        isSelected: true));
                }

                if (pendingIntelligentScissorsPlan?.PathPoints?.Count > 1)
                {
                    overlays.Add(new RoiImageCanvasPolygonOverlay(
                        pendingIntelligentScissorsPlan.PathPoints,
                        "EDGE PREVIEW",
                        System.Drawing.Color.Gold,
                        isClosed: false,
                        isDraft: true,
                        isSelected: true));
                }
            }

            AppendSmartMaskPromptOverlays(overlays);
            AppendPendingSmartMaskCandidateMask(maskOverlays, maskOpacity);
            MainCanvasViewModel.SetSegmentationOverlays(overlays, maskOverlays);
        }

        private void AppendPendingSmartMaskCandidateMask(
            List<RoiImageCanvasMaskOverlay> maskOverlays,
            float configuredOpacity)
        {
            if (!smartMaskPromptSession.HasSession || activeImageSize.IsEmpty)
            {
                return;
            }

            YoloWorkerSmokeCandidate candidate = GetSelectedCandidate();
            IReadOnlyList<System.Drawing.PointF> contour = GetCandidateContourPoints(candidate);
            if (candidate == null || contour.Count < 3)
            {
                return;
            }

            List<System.Drawing.Point> points = SegmentationGeometry.NormalizePolygon(
                contour.Select(point => new System.Drawing.Point(
                    Math.Clamp((int)Math.Round(point.X), 0, activeImageSize.Width - 1),
                    Math.Clamp((int)Math.Round(point.Y), 0, activeImageSize.Height - 1))),
                activeImageSize,
                minimumDistance: 1,
                simplificationTolerance: 0D);
            var source = new LabelingSegmentationObject
            {
                Points = points,
                ClassName = FirstNonEmpty(candidate.ClassName, "Defect")
            };
            if (!WpfSegmentationMaskGeometryService.TryRasterize(
                    source,
                    activeImageSize,
                    out byte[] maskData,
                    out DrawingRectangle maskBounds))
            {
                return;
            }

            int renderVersion = 17;
            foreach (System.Drawing.Point point in points)
            {
                renderVersion = unchecked((renderVersion * 31) + point.X);
                renderVersion = unchecked((renderVersion * 31) + point.Y);
            }

            float candidateOpacity = Math.Clamp(configuredOpacity * 0.62F, 0.34F, 0.46F);
            maskOverlays.Add(new RoiImageCanvasMaskOverlay(
                $"smart-mask-candidate:{candidate.Index}",
                maskData,
                activeImageSize,
                maskBounds,
                System.Drawing.Color.FromArgb(80, 180, 255),
                candidateOpacity,
                renderVersion,
                isSelected: false,
                label: string.Empty,
                showMarker: false));
        }

        private void AppendSmartMaskPromptOverlays(List<RoiImageCanvasPolygonOverlay> overlays)
        {
            if (!smartMaskPromptSession.HasSession)
            {
                return;
            }

            System.Drawing.Rectangle bounds = smartMaskPromptSession.PromptBounds;
            overlays.Add(new RoiImageCanvasPolygonOverlay(
                new[]
                {
                    new System.Drawing.Point(bounds.Left, bounds.Top),
                    new System.Drawing.Point(bounds.Right, bounds.Top),
                    new System.Drawing.Point(bounds.Right, bounds.Bottom),
                    new System.Drawing.Point(bounds.Left, bounds.Bottom)
                },
                "SMART MASK PROMPT",
                System.Drawing.Color.FromArgb(168, 85, 247),
                isClosed: true,
                isDraft: true));

            for (int index = 0; index < smartMaskPromptSession.Points.Count; index++)
            {
                WpfSmartMaskPromptPoint point = smartMaskPromptSession.Points[index];
                int radius = 5;
                overlays.Add(new RoiImageCanvasPolygonOverlay(
                    new[]
                    {
                        new System.Drawing.Point(point.Position.X, point.Position.Y - radius),
                        new System.Drawing.Point(point.Position.X + radius, point.Position.Y),
                        new System.Drawing.Point(point.Position.X, point.Position.Y + radius),
                        new System.Drawing.Point(point.Position.X - radius, point.Position.Y)
                    },
                    point.Kind == WpfSmartMaskPointKind.Positive ? $"+{index + 1}" : $"−{index + 1}",
                    point.Kind == WpfSmartMaskPointKind.Positive
                        ? System.Drawing.Color.LimeGreen
                        : System.Drawing.Color.Red,
                    isClosed: true,
                    isDraft: false,
                    isSelected: true));
            }
        }

        private void ClearSegmentationOverlays()
        {
            MainCanvasViewModel?.SetSegmentationOverlays(
                Array.Empty<RoiImageCanvasPolygonOverlay>(),
                Array.Empty<RoiImageCanvasMaskOverlay>());
        }
    }
}
