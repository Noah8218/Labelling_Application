using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using MvcVisionSystem.Yolo;
using OpenVisionLab.Mvvm;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void RefreshObjectList()
        {
            RefreshObjectListViewModel(null);
        }

        private void RefreshObjectListWithSelection(WpfObjectReviewItemRef preferredSelection)
        {
            RefreshObjectListViewModel(preferredSelection);
        }

        private void RefreshObjectListViewModel(WpfObjectReviewItemRef preferredSelection)
        {
            WpfObjectReviewItemRef previousSelection = null;
            TryGetSelectedObjectReviewItem(out previousSelection);

            WpfObjectReviewListPresentation presentation = objectReviewPresentationService.BuildListPresentation(
                manualRois,
                manualRoiClassNames,
                manualRoiShapeKinds,
                manualRoiOverlayIds,
                GetVisibleManualSegments(),
                confirmedDetectionCandidates,
                preferredSelection,
                previousSelection,
                candidate => WpfCandidateReviewPresentationService.ClipCandidateBounds(candidate, activeImageSize),
                candidate =>
                {
                    DrawingRectangle bounds = WpfCandidateReviewPresentationService.ClipCandidateBounds(candidate, activeImageSize);
                    return WpfCandidateReviewPresenter.BuildDetail(
                        candidate,
                        bounds,
                        GetCandidateOverlapInfo(bounds),
                        GetMinimumDetectionConfidence());
                });

            ApplyObjectSessionStates(presentation.Rows);
            ApplyObjectPersistentMetadata(presentation.Rows);
            SetObjectReviewObjects(presentation.Rows, presentation.Summary, presentation.SelectedItem);
            UpdateObjectReviewActionState();
        }

        private WpfObjectReviewListItem BuildManualRoiObjectReviewItem(int index)
        {
            WpfObjectReviewListItem row = objectReviewPresentationService.BuildManualRoiItem(
                manualRois,
                manualRoiClassNames,
                manualRoiShapeKinds,
                manualRoiOverlayIds,
                index);
            row?.ApplySessionState(objectSessionStateService.GetManualRoiState(index));
            row?.ApplyPersistentMetadata(objectMetadataStateService.GetManualRoiMetadata(index));
            return row;
        }

        private bool TryRefreshManualRoiObjectReviewRow(int manualRoiIndex, bool select)
        {
            WpfObjectReviewListItem row = BuildManualRoiObjectReviewItem(manualRoiIndex);
            if (row == null
                || !WpfObjectReviewSelectionService.CanReplaceManualRoiRow(
                    ObjectReviewViewModel?.Objects,
                    manualRoiIndex,
                    manualRois.Count))
            {
                return false;
            }

            bool replaced;
            using (ObjectReviewViewModel.SuppressSelectionNotifications())
            {
                replaced = ObjectReviewViewModel.TryReplaceObject(
                    manualRoiIndex,
                    row,
                    select);
            }

            SyncObjectClassEditorToSelection();
            UpdateObjectReviewActionState();
            return replaced;
        }

        private void SetObjectReviewObjects(
            IEnumerable<WpfObjectReviewListItem> rows,
            string summary,
            WpfObjectReviewItemRef selectedItem)
        {
            // Rebuilding the side list temporarily clears WPF SelectedItem. During ROI click/drag
            // that transient null must not clear the active canvas ROI handles.
            using (ObjectReviewViewModel.SuppressSelectionNotifications())
            {
                ObjectReviewViewModel.SetObjects(
                    rows,
                    summary,
                    selectedItem?.Source.ToString() ?? string.Empty,
                    selectedItem?.Index ?? -1);
            }

            SyncObjectClassEditorToSelection();
        }

        private string GetManualRoiClassName(int index)
            => WpfObjectReviewPresentationService.GetManualRoiClassName(manualRoiClassNames, index);

        private WpfObjectReviewListItem BuildManualSegmentObjectReviewItem(int manualSegmentIndex)
        {
            if (!IsSegmentationDatasetPurposeActive())
            {
                return null;
            }

            return BuildManualSegmentObjectReviewItemCore(manualSegmentIndex);
        }

        private WpfObjectReviewListItem BuildManualSegmentObjectReviewItemCore(int manualSegmentIndex)
        {
            WpfObjectReviewListItem row = objectReviewPresentationService.BuildManualSegmentItem(
                manualRois.Count,
                manualSegments,
                manualSegmentIndex);
            row?.ApplySessionState(GetManualSegmentSessionState(manualSegmentIndex));
            if (manualSegmentIndex >= 0 && manualSegmentIndex < manualSegments.Count)
            {
                row?.ApplyPersistentMetadata(
                    objectMetadataStateService.GetManualSegmentMetadata(manualSegments[manualSegmentIndex]));
            }
            return row;
        }

        private void ApplyObjectSessionStates(IEnumerable<WpfObjectReviewListItem> rows)
        {
            foreach (WpfObjectReviewListItem row in rows ?? Enumerable.Empty<WpfObjectReviewListItem>())
            {
                row?.ApplySessionState(row.Payload is WpfObjectReviewItemRef item
                    ? GetObjectSessionState(item)
                    : WpfObjectSessionState.Default);
            }
        }

        private bool TryRefreshManualSegmentObjectReviewRow(int manualSegmentIndex, string summary, bool select)
        {
            WpfObjectReviewListItem row = BuildManualSegmentObjectReviewItem(manualSegmentIndex);
            int objectRowIndex = manualRois.Count + manualSegmentIndex;
            if (row == null || ObjectReviewViewModel == null || objectRowIndex < 0)
            {
                return false;
            }

            bool updated;
            using (ObjectReviewViewModel.SuppressSelectionNotifications())
            {
                updated = ObjectReviewViewModel.TryUpsertObject(
                    objectRowIndex,
                    row,
                    summary,
                    select);
            }

            SyncObjectClassEditorToSelection();
            UpdateObjectReviewActionState();
            return updated;
        }

        // Class-editor synchronization stays beside object-list construction because both
        // paths operate on the selected object-review row and its class catalog.
        private void UpdateObjectReviewActionState()
        {
            ObjectReviewViewModel?.RefreshActionState();
        }

        private void SyncObjectClassEditorToSelection()
        {
            if (ObjectReviewViewModel == null)
            {
                return;
            }

            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                ObjectReviewViewModel.SelectedClassName = string.Empty;
                return;
            }

            string className = WpfObjectReviewEditService.GetClassName(
                item,
                manualRoiClassNames,
                manualSegments,
                confirmedDetectionCandidates);
            ObjectReviewViewModel.SetSelectedObjectClass(GetClassNames(), className);
        }

        private void RefreshObjectClassOptions(string selectedName = "")
        {
            if (ObjectReviewViewModel == null)
            {
                return;
            }

            string viewModelSelection = string.IsNullOrWhiteSpace(selectedName)
                ? ObjectReviewViewModel.SelectedClassName
                : selectedName;
            ObjectReviewViewModel.SetClassNames(GetClassNames(), viewModelSelection);
        }

        private IReadOnlyList<string> GetClassNames()
        {
            if (global.Data.ClassNamedList == null
                || !global.Data.ClassNamedList.Any(item => item != null && !string.IsNullOrWhiteSpace(item.Text)))
            {
                EnsureClassItem("Defect");
            }

            return global.Data.ClassNamedList
                .Where(ClassCatalogService.IsActiveClass)
                .Select(item => item.Text)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


    }
}
