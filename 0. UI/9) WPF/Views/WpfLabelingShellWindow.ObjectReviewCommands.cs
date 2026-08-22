using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using MvcVisionSystem.Yolo;
using OpenVisionLab.Mvvm;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Object commands mutate one selected object at a time; incremental delete avoids forcing a full canvas/list rebuild.
        private void ExecuteObjectSelectionChangedCommand(object selectedItem)
        {
            if (ObjectReviewViewModel?.IsSelectionNotificationSuppressed == true)
            {
                UpdateObjectReviewActionState();
                return;
            }

            if (pendingSegmentationSplitOrientation.HasValue
                && (selectedItem is not WpfObjectReviewListItem selectedRow
                    || !selectedRow.IsManualSegment
                    || selectedRow.SourceIndex != pendingSegmentationSplitSourceIndex))
            {
                CancelPendingSegmentationSplit(updateStatus: false);
            }

            if (pendingSegmentationHoleEditMode.HasValue
                && (selectedItem is not WpfObjectReviewListItem selectedHoleRow
                    || !selectedHoleRow.IsManualSegment
                    || selectedHoleRow.SourceIndex != pendingSegmentationHoleSourceIndex))
            {
                CancelPendingSegmentationHoleEdit(updateStatus: false);
            }

            if (pendingPolygonVertexEditMode.HasValue
                && (selectedItem is not WpfObjectReviewListItem selectedVertexRow
                    || !selectedVertexRow.IsManualPolygon
                    || selectedVertexRow.SourceIndex != pendingPolygonVertexSourceIndex))
            {
                CancelPendingPolygonVertexEdit(updateStatus: false);
            }

            if (pendingIntelligentScissorsSource != null
                && (selectedItem is not WpfObjectReviewListItem selectedScissorsRow
                    || !selectedScissorsRow.IsManualPolygon
                    || selectedScissorsRow.SourceIndex != pendingIntelligentScissorsSourceIndex))
            {
                CancelPendingIntelligentScissors(updateStatus: false);
            }

            if (pendingSegmentationRemoveUnderlyingPlan != null
                && (selectedItem is not WpfObjectReviewListItem selectedRemoveRow
                    || !selectedRemoveRow.IsManualSegment
                    || selectedRemoveRow.SourceIndex != pendingSegmentationRemoveUnderlyingPlan.SelectedIndex))
            {
                CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
            }

            SyncObjectClassEditorToSelection();
            UpdateObjectReviewActionState();
            bool isManualSegmentSelected = ObjectReviewViewModel?.IsSelectedSource(WpfObjectReviewSource.ManualSegment) == true;
            bool canEditSelectedSegment = isManualSegmentSelected
                && selectedItem is WpfObjectReviewListItem selectedStateRow
                && !selectedStateRow.IsHidden
                && !selectedStateRow.IsLocked;
            if (activeAnnotationTool == WpfAnnotationTool.Select)
            {
                MainCanvasViewModel.IsImagePointInputMode = canEditSelectedSegment;
            }

            if (ObjectReviewViewModel?.IsSelectedSource(WpfObjectReviewSource.ManualRoi) != true)
            {
                MainCanvasViewModel.ClearRoiSelection();
            }

            RefreshPolygonOverlays();
        }

        private void ExecuteApplyObjectClassCommand()
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                return;
            }

            if (!CanMutateSelectedObject(item, requireVisible: false, out string stateError))
            {
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return;
            }

            string className = WpfObjectReviewEditService.NormalizeClassName(ObjectReviewViewModel?.SelectedClassName);
            CClassItem classItem = EnsureClassItem(className);
            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory("Change object class");
            if (!WpfObjectReviewEditService.TryApplyClass(
                item,
                manualRois,
                manualRoiClassNames,
                manualSegments,
                candidateReviewState.MutableConfirmedCandidates,
                className,
                out string appliedClassName,
                classItem))
            {
                return;
            }

            PushAnnotationHistorySnapshot(beforeChange);
            if (item.Source == WpfObjectReviewSource.ManualRoi)
            {
                ApplyManualRoiOverlayColor(item.Index, refreshImmediately: true);
            }
            else if (item.Source == WpfObjectReviewSource.ConfirmedAi)
            {
                RedrawReviewRois();
            }
            else
            {
                MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
                RefreshPolygonOverlays();
            }

            RefreshObjectList();
            MarkAnnotationsDirty($"\uAC1D\uCCB4 \uD074\uB798\uC2A4 \uBCC0\uACBD: {appliedClassName}");

            AppendLog($"Changed object class: {appliedClassName}");
        }

        private void ExecuteDeleteObjectCommand()
        {
            DeleteSelectedObject();
        }

        private void ExecuteMergeSelectedSegmentsCommand()
        {
            IReadOnlyList<int> selectedIndices = ObjectReviewViewModel?
                .GetMergeSelectedManualSegmentIndices()
                ?? Array.Empty<int>();
            if (!segmentationMergeService.TryMerge(
                manualSegments,
                selectedIndices,
                activeImageSize,
                out WpfSegmentationMergeResult mergeResult,
                out string error))
            {
                SetYoloCommandStatus(error, isBusy: false);
                AppendLog($"Segment merge skipped: {error}");
                ObjectReviewViewModel?.RefreshActionState();
                return;
            }

            CancelObjectGroupSelection(updateStatus: false);
            List<WpfPersistentObjectMetadata> sourceMetadata = mergeResult.SourceIndices
                .Select(index => objectMetadataStateService.GetManualSegmentMetadata(manualSegments[index]))
                .ToList();
            string inheritedGroupId = sourceMetadata.Count > 0
                && sourceMetadata.All(metadata =>
                    !string.IsNullOrWhiteSpace(metadata.GroupId)
                    && string.Equals(metadata.GroupId, sourceMetadata[0].GroupId, StringComparison.Ordinal))
                ? sourceMetadata[0].GroupId
                : string.Empty;
            WpfAnnotationHistorySnapshot beforeChange = CaptureAnnotationHistory("\uC138\uADF8\uBA3C\uD2B8 \uBCD1\uD569");
            foreach (int index in mergeResult.SourceIndices.OrderByDescending(index => index))
            {
                manualSegments.RemoveAt(index);
            }

            int insertIndex = Math.Max(0, Math.Min(mergeResult.InsertIndex, manualSegments.Count));
            manualSegments.Insert(insertIndex, mergeResult.MergedSegment);
            objectMetadataStateService.SetManualSegmentGroupId(
                mergeResult.MergedSegment,
                inheritedGroupId);
            objectMetadataStateService.DissolveInvalidGroups(manualRois.Count, manualSegments);
            PushAnnotationHistorySnapshot(beforeChange);
            MainCanvasViewModel?.ClearMaskStrokePreview(refresh: false, clearTexture: true);
            RefreshPolygonOverlays();
            RefreshObjectListWithSelection(WpfObjectReviewItemRef.ManualSegment(insertIndex));
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);

            string status = FormattableString.Invariant(
                $"\uC138\uADF8\uBA3C\uD2B8 \uBCD1\uD569: {mergeResult.SourceIndices.Count}\uAC1C \u2192 1\uAC1C / {mergeResult.MergedSegment.ClassName}");
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteObjectPreviewKeyDownCommand(KeyInputCommandArgs e)
        {
            if (e == null || (e.Key != Key.Delete && e.Key != Key.Back))
            {
                return;
            }

            e.Handled = DeleteSelectedObject();
        }

        private bool DeleteSelectedObject()
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                return false;
            }

            if (!CanMutateSelectedObject(item, requireVisible: false, out string stateError))
            {
                SetYoloCommandStatus(stateError, isBusy: false);
                AppendLog(stateError);
                return false;
            }

            int selectedObjectRowIndex = GetSelectedObjectReviewRowIndex();
            string manualOverlayId = item.Source == WpfObjectReviewSource.ManualRoi
                ? GetManualRoiOverlayId(item.Index)
                : string.Empty;
            string removedText = ObjectReviewViewModel?.SelectedObject?.DisplayText
                ?? "object";
            LabelingSegmentationObject deletedSegment = item.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count
                ? manualSegments[item.Index]
                : null;
            WpfAnnotationHistorySnapshot beforeChange = item.Source == WpfObjectReviewSource.ManualRoi
                ? CaptureManualRoiHistory("\uB77C\uBCA8 \uC0AD\uC81C")
                : CaptureAnnotationHistory("\uB77C\uBCA8 \uC0AD\uC81C");
            CancelObjectGroupSelection(updateStatus: false);
            if (!WpfObjectReviewEditService.TryDelete(
                item,
                manualRois,
                manualRoiClassNames,
                manualSegments,
                candidateReviewState.MutableConfirmedCandidates))
            {
                UpdateObjectReviewActionState();
                return false;
            }

            if (item.Source == WpfObjectReviewSource.ManualRoi)
            {
                objectSessionStateService.ShiftRoiStatesAfterRemoval(item.Index);
                objectMetadataStateService.ShiftRoiMetadataAfterRemoval(item.Index);
            }
            else if (item.Source == WpfObjectReviewSource.ManualSegment)
            {
                objectSessionStateService.RemoveManualSegment(deletedSegment);
                objectMetadataStateService.RemoveManualSegment(deletedSegment);
            }
            objectMetadataStateService.DissolveInvalidGroups(manualRois.Count, manualSegments);

            PushAnnotationHistorySnapshot(beforeChange);
            if (item.Source == WpfObjectReviewSource.ManualRoi)
            {
                RemoveAtIfPresent(manualRoiShapeKinds, item.Index);
                RemoveAtIfPresent(manualRoiOverlayIds, item.Index);
                if (!RemoveCanvasRoiOverlayById(manualOverlayId))
                {
                    RedrawReviewRois();
                }

                ClearCanvasRoiSelectionAfterDelete(manualOverlayId);
            }
            else if (item.Source == WpfObjectReviewSource.ManualSegment)
            {
                RefreshPolygonOverlays();
            }
            else
            {
                RedrawReviewRois();
            }

            RefreshObjectReviewAfterDelete(item.Source, selectedObjectRowIndex);
            MarkAnnotationsDirty($"\uB77C\uBCA8 \uC0AD\uC81C: {removedText}");
            QueueActiveImageQueueStatusRefresh(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            AppendLog($"Removed object from review: {removedText}");
            return true;
        }

        private bool TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item)
        {
            if (ObjectReviewViewModel == null)
            {
                item = null;
                return false;
            }

            return ObjectReviewViewModel.TryResolveSelectedItem(
                manualRoiOverlayIds,
                manualRois.Count,
                out item);
        }

        private int GetSelectedObjectReviewRowIndex()
            => ObjectReviewViewModel?.GetSelectedRowIndex() ?? -1;

        private bool RemoveCanvasRoiOverlayById(string overlayId)
        {
            if (string.IsNullOrWhiteSpace(overlayId) || MainCanvasViewModel?.ImageViewer == null)
            {
                return false;
            }

            var overlayItem = MainCanvasViewModel.ImageViewer.GetCanvasOverlayManager().GetOverlayByUniqueId(overlayId);
            string groupName = overlayItem?.Parent?.GroupType
                ?? overlayItem?.Shape?.GroupType
                ?? MainCanvasViewModel.ImageViewer.GetCanvasOverlayManager().LastGroupType
                ?? string.Empty;
            OpenVisionLab.ImageCanvas.OpenGLRendering.OpenGlOverlayExtensions.DeleteOverlay(
                MainCanvasViewModel.ImageViewer,
                overlayId,
                groupName,
                refreshImmediately: false);
            return overlayItem != null;
        }

        private void ClearCanvasRoiSelectionAfterDelete(string overlayId)
        {
            if (MainCanvasViewModel == null)
            {
                return;
            }

            if (!MainCanvasViewModel.ClearDeletedRoiSelection(overlayId, refreshImmediately: false))
            {
                MainCanvasViewModel.ClearRoiSelection(refreshImmediately: false);
            }
        }

        private void RefreshObjectReviewAfterDelete(WpfObjectReviewSource deletedSource, int deletedObjectRowIndex)
        {
            int objectCount = manualRois.Count + GetVisibleManualSegmentCount() + confirmedDetectionCandidates.Count;
            WpfObjectReviewDeleteRefreshPlan plan = objectReviewPresentationService.BuildDeleteRefreshPlan(
                deletedSource,
                objectCount,
                ObjectReviewFullRefreshDeleteLimit,
                deletedObjectRowIndex,
                ObjectReviewViewModel?.Objects?.Count ?? 0);
            if (!plan.UseIncremental)
            {
                RefreshObjectList();
                return;
            }

            using (ObjectReviewViewModel.SuppressSelectionNotifications())
            {
                if (!ObjectReviewViewModel.TryRemoveObject(
                    deletedObjectRowIndex,
                    plan.Summary,
                    plan.SelectedRowIndex))
                {
                    RefreshObjectList();
                    return;
                }
            }

            SyncObjectClassEditorToSelection();
            UpdateObjectReviewActionState();
        }
    }
}
