using System;
using System.Collections.Generic;
using System.Linq;
using OpenVisionLab.Wpf.MessageDialogs;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void RestoreObjectMetadataTagsFromProject()
        {
            EnsureProjectSettings();
            ObjectReviewViewModel?.SetMetadataTagDefinitions(
                global.Data.ProjectSettings.ObjectReviewTags);
        }

        private void LoadObjectMetadataForActiveImage(string imagePath)
        {
            RestoreObjectMetadataTagsFromProject();
            WpfObjectMetadataLoadResult result = objectMetadataPersistenceService.LoadForImage(
                imagePath,
                manualRois,
                manualRoiClassNames,
                manualSegments,
                objectMetadataStateService,
                global.Data);
            if (!string.IsNullOrWhiteSpace(result.StatusText))
            {
                AppendLog(result.IsCompatible
                    ? result.StatusText
                    : $"\uAC1D\uCCB4 \uBA54\uD0C0\uB370\uC774\uD130 \uBB34\uC2DC: {result.StatusText}");
            }
        }

        private bool TrySaveCurrentObjectMetadata()
        {
            try
            {
                objectMetadataPersistenceService.Save(
                    global.Data.LastSelectImageName,
                    manualRois,
                    manualRoiClassNames,
                    manualSegments,
                    objectMetadataStateService,
                    global.Data);
                return true;
            }
            catch (Exception ex)
            {
                string status = $"\uAC1D\uCCB4 \uBA54\uD0C0\uB370\uC774\uD130 \uC800\uC7A5 \uC2E4\uD328: {ex.Message}";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
                return false;
            }
        }

        private void ExecuteTogglePersistentOccludedCommand()
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item)
                || !TryTogglePersistentOccluded(item, out WpfPersistentObjectMetadata metadata))
            {
                SetYoloCommandStatus(
                    "\uC218\uB3D9 \uBC15\uC2A4 \uB610\uB294 \uC138\uADF8\uBA3C\uD2B8\uB97C \uC120\uD0DD\uD558\uC138\uC694.",
                    isBusy: false);
                return;
            }

            RefreshObjectListWithSelection(item);
            MarkAnnotationsDirty(
                metadata.IsOccluded
                    ? "\uAC1D\uCCB4 \uAC00\uB9BC \uBA54\uD0C0\uB370\uC774\uD130 \uC124\uC815"
                    : "\uAC1D\uCCB4 \uAC00\uB9BC \uBA54\uD0C0\uB370\uC774\uD130 \uD574\uC81C");
            string status = metadata.IsOccluded
                ? "\uAC00\uB9BC \uAC1D\uCCB4\uB85C \uD45C\uC2DC\uD588\uC2B5\uB2C8\uB2E4. \uB77C\uBCA8 \uC800\uC7A5 \uC2DC \uBA54\uD0C0\uB370\uC774\uD130\uC5D0 \uBC18\uC601\uB429\uB2C8\uB2E4."
                : "\uAC00\uB9BC \uD45C\uC2DC\uB97C \uD574\uC81C\uD588\uC2B5\uB2C8\uB2E4. \uB77C\uBCA8 \uC800\uC7A5 \uC2DC \uBC18\uC601\uB429\uB2C8\uB2E4.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteTogglePersistentTagCommand(string requestedTag)
        {
            string tag = WpfObjectMetadataStateService.NormalizeTag(requestedTag);
            if (string.IsNullOrWhiteSpace(tag)
                || !TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                SetYoloCommandStatus(
                    "\uC218\uB3D9 \uBC15\uC2A4/\uC138\uADF8\uBA3C\uD2B8\uB97C \uC120\uD0DD\uD558\uACE0 \uD0DC\uADF8\uB97C \uC785\uB825\uD558\uC138\uC694.",
                    isBusy: false);
                return;
            }

            WpfPersistentObjectMetadata current = GetObjectPersistentMetadata(item);
            bool alreadyApplied = current.Tags.Any(value =>
                string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
            if (!alreadyApplied && !TryEnsureRecipeMetadataTag(tag))
            {
                return;
            }

            if (!TryTogglePersistentTag(item, tag, out WpfPersistentObjectMetadata metadata))
            {
                SetYoloCommandStatus(
                    "\uC218\uB3D9 \uBC15\uC2A4 \uB610\uB294 \uC138\uADF8\uBA3C\uD2B8\uC5D0\uB9CC \uD0DC\uADF8\uB97C \uC801\uC6A9\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.",
                    isBusy: false);
                return;
            }

            bool isApplied = metadata.Tags.Any(value =>
                string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
            RefreshObjectListWithSelection(item);
            MarkAnnotationsDirty(
                isApplied
                    ? $"\uAC1D\uCCB4 \uD0DC\uADF8 \uC124\uC815: {tag}"
                    : $"\uAC1D\uCCB4 \uD0DC\uADF8 \uD574\uC81C: {tag}");
            string status = isApplied
                ? $"\uD0DC\uADF8 \uC801\uC6A9: {tag} \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694"
                : $"\uD0DC\uADF8 \uD574\uC81C: {tag} \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteResetRecipeMetadataTagsCommand()
        {
            EnsureProjectSettings();
            if (global.Data.ProjectSettings.ObjectReviewTags.Count == 0)
            {
                SetYoloCommandStatus(
                    "\uD604\uC7AC Recipe \uD0DC\uADF8 \uBAA9\uB85D\uC774 \uC774\uBBF8 \uBE44\uC5B4 \uC788\uC2B5\uB2C8\uB2E4.",
                    isBusy: false);
                return;
            }

            List<string> previousTags = global.Data.ProjectSettings.ObjectReviewTags.ToList();
            global.Data.ProjectSettings.ObjectReviewTags.Clear();
            if (!TryPersistRecipeMetadataTagDefinitions())
            {
                global.Data.ProjectSettings.ObjectReviewTags = previousTags;
                return;
            }

            ObjectReviewViewModel?.SetMetadataTagDefinitions(Array.Empty<string>());
            const string status =
                "Recipe \uD0DC\uADF8 \uBAA9\uB85D\uC744 \uAE30\uBCF8\uAC12(\uBE48 \uBAA9\uB85D)\uC73C\uB85C \uB418\uB3CC\uB838\uC2B5\uB2C8\uB2E4. \uAE30\uC874 \uAC1D\uCCB4\uC5D0 \uC800\uC7A5\uB41C \uD0DC\uADF8\uB294 \uC0AD\uC81C\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteBeginObjectGroupSelectionCommand()
        {
            objectGroupSelectionService.Begin();
            ObjectReviewViewModel?.SetGroupSelectionMode(true);
            const string status = "\uADF8\uB8F9 \uAD6C\uC131 \uC120\uD0DD \uC2DC\uC791: \uBAA9\uB85D\uC5D0\uC11C \uBBF8\uADF8\uB8F9 \uC800\uC7A5 \uAC1D\uCCB4\uB97C 2\uAC1C \uC774\uC0C1 \uC120\uD0DD\uD558\uC138\uC694.";
            ObjectReviewViewModel?.RefreshGroupSelectionPresentation(status);
            SetYoloCommandStatus(status, isBusy: false);
        }

        private void ExecuteCancelObjectGroupSelectionCommand()
            => CancelObjectGroupSelection(updateStatus: true);

        private void CancelObjectGroupSelection(bool updateStatus)
        {
            bool wasActive = objectGroupSelectionService.IsActive;
            objectGroupSelectionService.Cancel();
            ObjectReviewViewModel?.SetGroupSelectionMode(false);
            if (wasActive && updateStatus)
            {
                const string status = "\uADF8\uB8F9 \uAD6C\uC131 \uC120\uD0DD\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
            }
        }

        private void ExecuteObjectGroupSelectionChangedCommand(object value)
        {
            if (value is not WpfObjectReviewListItem row
                || row.Payload is not WpfObjectReviewItemRef item)
            {
                return;
            }

            if (!objectGroupSelectionService.SetSelected(
                item,
                row.IsGroupSelected,
                out string error))
            {
                row.IsGroupSelected = false;
                ObjectReviewViewModel?.RefreshGroupSelectionPresentation(error);
                SetYoloCommandStatus(error, isBusy: false);
                return;
            }

            ObjectReviewViewModel?.RefreshGroupSelectionPresentation();
        }

        private void ExecuteCreateObjectGroupCommand()
        {
            if (!objectGroupSelectionService.TryCreatePlan(
                GetObjectPersistentMetadata,
                out WpfObjectReviewGroupCreatePlan plan,
                out string error))
            {
                ObjectReviewViewModel?.RefreshGroupSelectionPresentation(error);
                SetYoloCommandStatus(error, isBusy: false);
                return;
            }

            foreach (WpfObjectReviewItemRef member in plan.Members)
            {
                if (!TrySetObjectGroupId(member, plan.GroupId))
                {
                    foreach (WpfObjectReviewItemRef rollback in plan.Members)
                    {
                        TrySetObjectGroupId(rollback, string.Empty);
                    }
                    const string applyError = "\uADF8\uB8F9 \uAD6C\uC131\uC6D0 \uC801\uC6A9 \uC911 \uAC1D\uCCB4\uAC00 \uBCC0\uACBD\uB418\uC5B4 \uC804\uCCB4 \uC791\uC5C5\uC744 \uB418\uB3CC\uB838\uC2B5\uB2C8\uB2E4.";
                    ObjectReviewViewModel?.RefreshGroupSelectionPresentation(applyError);
                    SetYoloCommandStatus(applyError, isBusy: false);
                    return;
                }
            }

            WpfObjectReviewItemRef selection = plan.Members.FirstOrDefault();
            CancelObjectGroupSelection(updateStatus: false);
            RefreshObjectListWithSelection(selection);
            MarkAnnotationsDirty($"\uAC80\uC218 \uADF8\uB8F9 \uC0DD\uC131: {plan.Members.Count}\uAC1C");
            string status = $"\uAC80\uC218 \uADF8\uB8F9 \uC0DD\uC131: {plan.Members.Count}\uAC1C \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteRemoveSelectedObjectFromGroupCommand()
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                return;
            }

            string groupId = GetObjectPersistentMetadata(item).GroupId;
            if (string.IsNullOrWhiteSpace(groupId) || !TrySetObjectGroupId(item, string.Empty))
            {
                SetYoloCommandStatus("\uC120\uD0DD\uD55C \uADF8\uB8F9 \uAD6C\uC131\uC6D0\uC744 \uD655\uC778\uD558\uC138\uC694.", isBusy: false);
                return;
            }

            int dissolved = objectMetadataStateService.DissolveInvalidGroups(
                manualRois.Count,
                manualSegments);
            RefreshObjectListWithSelection(item);
            MarkAnnotationsDirty("\uAC80\uC218 \uADF8\uB8F9 \uAD6C\uC131\uC6D0 \uC81C\uAC70");
            string status = dissolved > 0
                ? "\uADF8\uB8F9\uC5D0\uC11C \uC81C\uAC70\uD588\uACE0 1\uAC1C\uB9CC \uB0A8\uC740 \uADF8\uB8F9\uC740 \uC790\uB3D9 \uD574\uC81C\uD588\uC2B5\uB2C8\uB2E4. \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694"
                : "\uC120\uD0DD \uAC1D\uCCB4\uB97C \uADF8\uB8F9\uC5D0\uC11C \uC81C\uAC70\uD588\uC2B5\uB2C8\uB2E4. \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteDissolveSelectedObjectGroupCommand()
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected))
            {
                return;
            }

            string groupId = GetObjectPersistentMetadata(selected).GroupId;
            IReadOnlyList<WpfObjectReviewItemRef> members = GetObjectGroupMembers(groupId);
            if (members.Count < 2)
            {
                SetYoloCommandStatus("\uD574\uC81C\uD560 \uAC80\uC218 \uADF8\uB8F9\uC744 \uC120\uD0DD\uD558\uC138\uC694.", isBusy: false);
                return;
            }

            WpfMessageDialogResult result = WpfMessageDialog.Confirm(
                this,
                "\uAC80\uC218 \uADF8\uB8F9 \uD574\uC81C",
                $"\uAD6C\uC131\uC6D0 {members.Count}\uAC1C\uC758 \uADF8\uB8F9 \uAD00\uACC4\uB97C \uD574\uC81C\uD569\uB2C8\uB2E4. \uAC1D\uCCB4\uC640 \uB77C\uBCA8\uC740 \uC0AD\uC81C\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.",
                "\uADF8\uB8F9 \uD574\uC81C",
                "\uCDE8\uC18C");
            if (result != WpfMessageDialogResult.Yes)
            {
                return;
            }

            foreach (WpfObjectReviewItemRef member in members)
            {
                TrySetObjectGroupId(member, string.Empty);
            }
            RefreshObjectListWithSelection(selected);
            MarkAnnotationsDirty($"\uAC80\uC218 \uADF8\uB8F9 \uD574\uC81C: {members.Count}\uAC1C");
            string status = $"\uAC80\uC218 \uADF8\uB8F9 \uD574\uC81C: {members.Count}\uAC1C \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteToggleObjectGroupOccludedCommand()
        {
            if (!TryResolveSelectedGroupMembers(out WpfObjectReviewItemRef selected, out IReadOnlyList<WpfObjectReviewItemRef> members))
            {
                return;
            }

            bool apply = members.Any(member => !GetObjectPersistentMetadata(member).IsOccluded);
            foreach (WpfObjectReviewItemRef member in members)
            {
                TrySetObjectOccluded(member, apply);
            }
            RefreshObjectListWithSelection(selected);
            MarkAnnotationsDirty(apply ? "\uADF8\uB8F9 \uAC00\uB9BC \uC801\uC6A9" : "\uADF8\uB8F9 \uAC00\uB9BC \uD574\uC81C");
            string status = $"\uADF8\uB8F9 {members.Count}\uAC1C \uAC1D\uCCB4 \uAC00\uB9BC {(apply ? "\uC801\uC6A9" : "\uD574\uC81C")} \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private void ExecuteToggleObjectGroupTagCommand(string requestedTag)
        {
            string tag = WpfObjectMetadataStateService.NormalizeTag(requestedTag);
            if (string.IsNullOrWhiteSpace(tag)
                || !TryResolveSelectedGroupMembers(out WpfObjectReviewItemRef selected, out IReadOnlyList<WpfObjectReviewItemRef> members))
            {
                SetYoloCommandStatus("\uADF8\uB8F9\uACFC Recipe \uD0DC\uADF8\uB97C \uC120\uD0DD\uD558\uC138\uC694.", isBusy: false);
                return;
            }

            bool apply = members.Any(member => !GetObjectPersistentMetadata(member).Tags.Any(value =>
                string.Equals(value, tag, StringComparison.OrdinalIgnoreCase)));
            if (apply && !TryEnsureRecipeMetadataTag(tag))
            {
                return;
            }

            foreach (WpfObjectReviewItemRef member in members)
            {
                TrySetObjectTag(member, tag, apply);
            }
            RefreshObjectListWithSelection(selected);
            MarkAnnotationsDirty(apply ? $"\uADF8\uB8F9 \uD0DC\uADF8 \uC801\uC6A9: {tag}" : $"\uADF8\uB8F9 \uD0DC\uADF8 \uD574\uC81C: {tag}");
            string status = $"\uADF8\uB8F9 {members.Count}\uAC1C \uAC1D\uCCB4 \uD0DC\uADF8 {(apply ? "\uC801\uC6A9" : "\uD574\uC81C")}: {tag} \u00B7 \uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryResolveSelectedGroupMembers(
            out WpfObjectReviewItemRef selected,
            out IReadOnlyList<WpfObjectReviewItemRef> members)
        {
            members = Array.Empty<WpfObjectReviewItemRef>();
            if (!TryGetSelectedObjectReviewItem(out selected))
            {
                return false;
            }

            members = GetObjectGroupMembers(GetObjectPersistentMetadata(selected).GroupId);
            if (members.Count >= 2)
            {
                return true;
            }

            SetYoloCommandStatus("\uADF8\uB8F9 \uAD6C\uC131\uC6D0\uC744 \uC120\uD0DD\uD558\uC138\uC694.", isBusy: false);
            return false;
        }

        private IReadOnlyList<WpfObjectReviewItemRef> GetObjectGroupMembers(string groupId)
        {
            string normalized = WpfObjectMetadataStateService.NormalizeGroupId(groupId);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<WpfObjectReviewItemRef>();
            }

            return (ObjectReviewViewModel?.Objects ?? Enumerable.Empty<WpfObjectReviewListItem>())
                .Where(row => row?.Payload is WpfObjectReviewItemRef
                    && string.Equals(row.GroupId, normalized, StringComparison.Ordinal))
                .Select(row => (WpfObjectReviewItemRef)row.Payload)
                .ToList();
        }

        private bool TrySetObjectGroupId(WpfObjectReviewItemRef item, string groupId)
        {
            if (item?.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                objectMetadataStateService.SetManualRoiGroupId(item.Index, groupId);
                return true;
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                objectMetadataStateService.SetManualSegmentGroupId(manualSegments[item.Index], groupId);
                return true;
            }

            return false;
        }

        private bool TrySetObjectOccluded(WpfObjectReviewItemRef item, bool isOccluded)
        {
            if (item?.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                objectMetadataStateService.SetManualRoiOccluded(item.Index, isOccluded);
                return true;
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                objectMetadataStateService.SetManualSegmentOccluded(manualSegments[item.Index], isOccluded);
                return true;
            }

            return false;
        }

        private bool TrySetObjectTag(WpfObjectReviewItemRef item, string tag, bool isApplied)
        {
            if (item?.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                objectMetadataStateService.SetManualRoiTag(item.Index, tag, isApplied);
                return true;
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                objectMetadataStateService.SetManualSegmentTag(manualSegments[item.Index], tag, isApplied);
                return true;
            }

            return false;
        }

        private bool TryEnsureRecipeMetadataTag(string tag)
        {
            EnsureProjectSettings();
            List<string> tags = global.Data.ProjectSettings.ObjectReviewTags;
            if (tags.Any(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (tags.Count >= WpfObjectMetadataStateService.MaximumTagCount)
            {
                SetYoloCommandStatus(
                    $"\uD604\uC7AC Recipe\uC5D0\uB294 \uD0DC\uADF8\uB97C {WpfObjectMetadataStateService.MaximumTagCount}\uAC1C\uAE4C\uC9C0 \uC815\uC758\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.",
                    isBusy: false);
                return false;
            }

            tags.Add(tag);
            global.Data.ProjectSettings.EnsureDefaults();
            if (!TryPersistRecipeMetadataTagDefinitions())
            {
                global.Data.ProjectSettings.ObjectReviewTags.RemoveAll(value =>
                    string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
                return false;
            }

            ObjectReviewViewModel?.SetMetadataTagDefinitions(
                global.Data.ProjectSettings.ObjectReviewTags);
            return true;
        }

        private bool TryPersistRecipeMetadataTagDefinitions()
        {
            string recipeName = GetCurrentRecipeName();
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                SetYoloCommandStatus(
                    "Recipe\uB97C \uC801\uC6A9\uD55C \uD6C4 \uAC1D\uCCB4 \uD0DC\uADF8 \uBAA9\uB85D\uC744 \uC800\uC7A5\uD558\uC138\uC694.",
                    isBusy: false);
                return false;
            }

            try
            {
                projectRecipeSessionService.Save(global.Data, recipeName);
                return true;
            }
            catch (Exception ex)
            {
                string status = $"Recipe \uD0DC\uADF8 \uBAA9\uB85D \uC800\uC7A5 \uC2E4\uD328: {ex.Message}";
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
                return false;
            }
        }

        private bool TryTogglePersistentOccluded(
            WpfObjectReviewItemRef item,
            out WpfPersistentObjectMetadata metadata)
        {
            metadata = WpfPersistentObjectMetadata.Default;
            if (item?.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                metadata = objectMetadataStateService.ToggleManualRoiOccluded(item.Index);
                return true;
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                metadata = objectMetadataStateService.ToggleManualSegmentOccluded(
                    manualSegments[item.Index]);
                return true;
            }

            return false;
        }

        private bool TryTogglePersistentTag(
            WpfObjectReviewItemRef item,
            string tag,
            out WpfPersistentObjectMetadata metadata)
        {
            metadata = WpfPersistentObjectMetadata.Default;
            if (item?.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                metadata = objectMetadataStateService.ToggleManualRoiTag(item.Index, tag);
                return true;
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                metadata = objectMetadataStateService.ToggleManualSegmentTag(
                    manualSegments[item.Index],
                    tag);
                return true;
            }

            return false;
        }

        private WpfPersistentObjectMetadata GetObjectPersistentMetadata(WpfObjectReviewItemRef item)
        {
            if (item?.Source == WpfObjectReviewSource.ManualRoi)
            {
                return objectMetadataStateService.GetManualRoiMetadata(item.Index);
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                return objectMetadataStateService.GetManualSegmentMetadata(
                    manualSegments[item.Index]);
            }

            return WpfPersistentObjectMetadata.Default;
        }

        private void ApplyObjectPersistentMetadata(IEnumerable<WpfObjectReviewListItem> rows)
        {
            foreach (WpfObjectReviewListItem row in rows ?? Enumerable.Empty<WpfObjectReviewListItem>())
            {
                row?.ApplyPersistentMetadata(row.Payload is WpfObjectReviewItemRef item
                    ? GetObjectPersistentMetadata(item)
                    : WpfPersistentObjectMetadata.Default);
            }
        }
    }
}
