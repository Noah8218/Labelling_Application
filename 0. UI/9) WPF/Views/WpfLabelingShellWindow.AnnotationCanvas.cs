using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.ImageCanvas.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Class-aware drawing policy stays in the shell because the canvas library should not know labeling class names.
        private bool ShouldDrawOverExistingRoiForCurrentClass(CanvasRect<float> roiRect)
        {
            if (roiRect == null || string.IsNullOrWhiteSpace(roiRect.UniqueId))
            {
                return false;
            }

            string currentClass = ClassCatalogService.NormalizeClassName(GetSelectedClassName());
            if (string.IsNullOrWhiteSpace(currentClass))
            {
                return false;
            }

            int index = FindManualRoiIndexByOverlayId(roiRect.UniqueId);
            if (index < 0 || index >= manualRoiClassNames.Count)
            {
                return false;
            }

            string existingClass = ClassCatalogService.NormalizeClassName(manualRoiClassNames[index]);
            return !string.IsNullOrWhiteSpace(existingClass)
                && !string.Equals(currentClass, existingClass, StringComparison.OrdinalIgnoreCase);
        }

        private DrawingColor GetClassDrawColor(string className)
        {
            LabelClass classItem = EnsureClassItem(FirstNonEmpty(className, "Defect"));
            return classItem?.DrawColor ?? DrawingColor.FromArgb(34, 197, 94);
        }

        private DrawingColor GetManualRoiDrawColor(int index)
            => GetClassDrawColor(GetManualRoiClassName(index));

        private string ResolveNewManualRoiClassName(CanvasRect<float> roiRect)
        {
            string copiedClassName = ClassCatalogService.NormalizeClassName(roiRect?.UserTag);
            return FirstNonEmpty(copiedClassName, GetSelectedClassName(), "Defect");
        }

        private void ApplyManualRoiOverlayColor(int index, bool refreshImmediately = false)
        {
            if (index < 0 || index >= manualRois.Count)
            {
                return;
            }

            string className = GetManualRoiClassName(index);
            MainCanvasViewModel?.SetRoiOverlayUserTag(
                GetManualRoiOverlayId(index),
                className);
            MainCanvasViewModel?.SetRoiOverlayColor(
                GetManualRoiOverlayId(index),
                GetClassDrawColor(className),
                refreshImmediately);
        }

        // Canvas annotation synchronization stays separate from tool input handling so ROI/overlay model mutations are easy to audit.
        private void MainCanvasViewModel_RoiAdded(object sender, OpenVisionLab.ImageCanvas.Model.RoiChangedEventArgs e)
        {
            if (isApplicationCloseApproved || e?.RoiRect == null || activeImageSize.IsEmpty)
            {
                return;
            }

            DrawingRectangle bounds = ConvertCanvasRectToImageBounds(e.RoiRect);
            if (bounds.IsEmpty)
            {
                return;
            }

            string overlayId = e.RoiRect.UniqueId ?? string.Empty;
            int existingIndex = FindManualRoiIndexByOverlayId(overlayId);
            bool addedNewManualRoi = existingIndex < 0;
            if (existingIndex >= 0)
            {
                if (manualRois[existingIndex] != bounds || GetManualRoiShapeKind(existingIndex) != e.RoiRect.ShapeKind)
                {
                    RegisterAnnotationHistoryBeforeChange("박스 수정");
                }

                manualRois[existingIndex] = bounds;
                manualRoiShapeKinds[existingIndex] = e.RoiRect.ShapeKind;
                e.RoiRect.UserTag = GetManualRoiClassName(existingIndex);
                ApplyManualRoiOverlayColor(existingIndex);
            }
            else
            {
                RegisterAnnotationHistoryBeforeChange("박스 추가");
                string className = ResolveNewManualRoiClassName(e.RoiRect);
                e.RoiRect.UserTag = className;
                manualRois.Add(bounds);
                manualRoiClassNames.Add(className);
                manualRoiShapeKinds.Add(e.RoiRect.ShapeKind);
                manualRoiOverlayIds.Add(overlayId);
                ApplyManualRoiOverlayColor(manualRois.Count - 1);
            }

            RefreshObjectListWithSelection(CreateManualRoiSelection(e.RoiRect));
            ShowSavedLabelsWorkflowView();
            string shapeName = FormatManualRoiShapeName(e.RoiRect.ShapeKind);
            SetModelStatus($"라벨 추가: {shapeName} {WpfCandidateReviewPresenter.FormatBoundsCompact(bounds)}");
            AppendLog($"라벨 추가({shapeName}): {bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}");
            RefreshSmartMaskCommandState();
            if (addedNewManualRoi)
            {
                TryStartAutoSmartMaskForNewRoi(e.RoiRect);
            }
        }

        private void MainCanvasViewModel_RoiEditingCompleted(object sender, OpenVisionLab.ImageCanvas.Model.RoiChangedEventArgs e)
        {
            if (isApplicationCloseApproved || e?.RoiRect == null || activeImageSize.IsEmpty)
            {
                return;
            }

            UpdateManualRoiFromCanvasRect(e.RoiRect);
        }

        private void MainCanvasViewModel_RoiMouseUp(object sender, OpenVisionLab.ImageCanvas.Model.RoiChangedEventArgs e)
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            WpfObjectReviewItemRef selectedManualRoi = null;
            bool updatedSingleObjectRow = false;
            if (e?.RoiRect != null)
            {
                UpdateManualRoiFromCanvasRect(e.RoiRect);
                selectedManualRoi = CreateManualRoiSelection(e.RoiRect);
                updatedSingleObjectRow = selectedManualRoi != null
                    && TryRefreshManualRoiObjectReviewRow(selectedManualRoi.Index, select: true);
            }

            activeRoiEditHistoryOverlayId = string.Empty;
            if (!updatedSingleObjectRow)
            {
                RefreshObjectListWithSelection(selectedManualRoi);
            }
        }


        private void MainCanvasViewModel_RemoveRoiRequested(object sender, CanvasRect<float> rect)
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            int index = FindManualRoiIndexByOverlayId(rect?.UniqueId);
            if (index < 0)
            {
                return;
            }

            PushAnnotationHistorySnapshot(CaptureManualRoiHistory("박스 삭제"));
            manualRois.RemoveAt(index);
            RemoveAtIfPresent(manualRoiClassNames, index);
            RemoveAtIfPresent(manualRoiShapeKinds, index);
            RemoveAtIfPresent(manualRoiOverlayIds, index);
            // Canvas ViewModel owns the OpenGL overlay removal after this event; the shell only updates model/review state here.
            RefreshObjectReviewAfterDelete(WpfObjectReviewSource.ManualRoi, index);
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            RefreshSmartMaskCommandState();
        }

        // ROI metadata helpers stay beside the Canvas event adapter because
        // overlay IDs, shape kinds, and review-row references form one state
        // boundary for manual ROI edits.
        private void UpdateManualRoiFromCanvasRect(CanvasRect<float> rect)
        {
            int index = FindManualRoiIndexByOverlayId(rect?.UniqueId);
            if (index < 0)
            {
                return;
            }

            DrawingRectangle bounds = ConvertCanvasRectToImageBounds(rect);
            if (bounds.IsEmpty)
            {
                return;
            }

            if (manualRois[index] != bounds || GetManualRoiShapeKind(index) != rect.ShapeKind)
            {
                RegisterRoiEditHistoryBeforeChange(rect.UniqueId, "박스 수정");
            }

            manualRois[index] = bounds;
            manualRoiShapeKinds[index] = rect.ShapeKind;
        }

        private WpfObjectReviewItemRef CreateManualRoiSelection(CanvasRect<float> rect)
        {
            int index = FindManualRoiIndexByOverlayId(rect?.UniqueId);
            return index >= 0 ? WpfObjectReviewItemRef.Manual(index, rect?.UniqueId) : null;
        }

        private DrawingRectangle ConvertCanvasRectToImageBounds(CanvasRect<float> rect)
        {
            if (rect == null || rect.IsEmpty() || activeImageSize.IsEmpty)
            {
                return DrawingRectangle.Empty;
            }

            var raw = new DrawingRectangle(
                (int)Math.Round(rect.Left),
                (int)Math.Round(activeImageSize.Height - rect.Top),
                (int)Math.Round(rect.Width),
                (int)Math.Round(rect.Height));

            return DrawingRectangle.Intersect(
                raw,
                new DrawingRectangle(0, 0, activeImageSize.Width, activeImageSize.Height));
        }

        private int FindManualRoiIndexByOverlayId(string overlayId)
            => WpfObjectReviewSelectionService.FindManualRoiIndexByOverlayId(manualRoiOverlayIds, overlayId);

        private CanvasRoiShapeKind GetManualRoiShapeKind(int index)
            => WpfObjectReviewPresentationService.GetManualRoiShapeKind(manualRoiShapeKinds, index);

        private string GetManualRoiOverlayId(int index)
            => WpfObjectReviewSelectionService.GetManualRoiOverlayId(manualRoiOverlayIds, index);

        private void EnsureManualRoiMetadataCount()
        {
            while (manualRoiShapeKinds.Count < manualRois.Count)
            {
                manualRoiShapeKinds.Add(CanvasRoiShapeKind.Rectangle);
            }

            while (manualRoiOverlayIds.Count < manualRois.Count)
            {
                manualRoiOverlayIds.Add(string.Empty);
            }
        }

        private static void RemoveAtIfPresent<T>(IList<T> items, int index)
        {
            if (items != null && index >= 0 && index < items.Count)
            {
                items.RemoveAt(index);
            }
        }

        private static string FormatManualRoiShapeName(CanvasRoiShapeKind shapeKind)
            => WpfObjectReviewPresentationService.FormatManualRoiShapeName(shapeKind);



    }
}
