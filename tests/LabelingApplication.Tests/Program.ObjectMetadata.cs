using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

internal static partial class Program
{
    internal static void TestObjectMetadataReviewWorkflow()
    {
        TestObjectMetadataStateAndReviewFilters();
        TestObjectMetadataPersistenceRoundTrip();
        TestObjectMetadataRecipeTagsRoundTrip();
        TestObjectMetadataPanelDeclaresConsumerControls();
    }

    internal static void TestObjectGroupReviewWorkflow()
    {
        TestObjectGroupSelectionAndFilters();
        TestObjectGroupPersistenceAndLegacyCompatibility();
        TestObjectGroupMutationContractsAreWired();
        TestObjectGroupPanelDeclaresConsumerControls();
    }

    private static void TestObjectMetadataStateAndReviewFilters()
    {
        var state = new WpfObjectMetadataStateService();
        AssertTrue(state.GetManualRoiMetadata(0).IsDefault, "new box metadata should be empty");
        AssertTrue(state.ToggleManualRoiOccluded(1).IsOccluded, "occluded toggle should target one box");
        AssertTrue(
            state.ToggleManualRoiTag(1, "Review").Tags.Contains("Review"),
            "tag toggle should add a normalized tag");
        state.ShiftRoiMetadataAfterRemoval(0);
        AssertTrue(state.GetManualRoiMetadata(0).IsOccluded, "box metadata should shift with row deletion");

        var first = new WpfObjectReviewListItem(
            "1. Defect",
            string.Empty,
            WpfObjectReviewSource.ManualRoi.ToString(),
            0,
            WpfObjectReviewItemRef.Manual(0));
        first.ApplyPersistentMetadata(new WpfPersistentObjectMetadata(false, new[] { "Review" }));
        var second = new WpfObjectReviewListItem(
            "2. Defect",
            string.Empty,
            WpfObjectReviewSource.ManualRoi.ToString(),
            1,
            WpfObjectReviewItemRef.Manual(1));
        second.ApplyPersistentMetadata(new WpfPersistentObjectMetadata(true, new[] { "Review", "Boundary" }));

        var viewModel = new WpfObjectReviewPanelViewModel();
        viewModel.SetMetadataTagDefinitions(new[] { "Review", "Boundary" });
        viewModel.SetObjects(new[] { first, second }, "2 objects");
        AssertEqual(2, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        viewModel.SelectedMetadataTagFilter = "Review";
        AssertEqual(2, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        viewModel.ToggleOccludedFilterCommand.Execute(null);
        AssertEqual(1, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        AssertTrue(second.IsMetadataFilterMatch, "combined tag and occluded filters should keep the matching row");
        viewModel.ResetMetadataFilterCommand.Execute(null);
        AssertEqual(2, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        AssertEqual(WpfObjectReviewPanelViewModel.AllMetadataTagsFilter, viewModel.SelectedMetadataTagFilter);
    }

    private static void TestObjectMetadataPersistenceRoundTrip()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            "object-metadata-" + Guid.NewGuid().ToString("N"));
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;

            string[] modes = { "train", "valid", "test" };
            foreach (string mode in modes)
            {
                string labelDirectory = Path.Combine(root, "data", mode, "labels");
                Directory.CreateDirectory(labelDirectory);
                File.WriteAllText(Path.Combine(labelDirectory, "sample.txt"), "unchanged-label");
            }

            var rois = new List<Rectangle>
            {
                new Rectangle(10, 12, 30, 24),
                new Rectangle(10, 12, 30, 24)
            };
            var classNames = new List<string> { "Defect", "Defect" };
            var segment = new LabelingSegmentationObject
            {
                ObjectId = "segment-001",
                ClassName = "Defect",
                Points = new List<Point>
                {
                    new Point(1, 1),
                    new Point(8, 1),
                    new Point(8, 8)
                }
            };
            var segments = new List<LabelingSegmentationObject> { segment };
            var state = new WpfObjectMetadataStateService();
            state.ToggleManualRoiOccluded(1);
            state.ToggleManualRoiTag(1, "Review");
            state.ToggleManualSegmentTag(segment, "Boundary");

            var persistence = new WpfObjectMetadataPersistenceService();
            IReadOnlyList<string> written = persistence.Save(
                "sample.png",
                rois,
                classNames,
                segments,
                state,
                data);
            AssertEqual(1, written.Count);
            AssertTrue(File.Exists(written[0]), "object metadata sidecar should be written in the selected split");
            string json = File.ReadAllText(written[0]);
            AssertTrue(json.Contains("\"Occurrence\": 1", StringComparison.Ordinal), "duplicate boxes should persist their occurrence");
            AssertTrue(json.Contains("\"Version\": 2", StringComparison.Ordinal), "object metadata should write the current v2 schema");

            foreach (string mode in modes)
            {
                AssertEqual(
                    "unchanged-label",
                    File.ReadAllText(Path.Combine(root, "data", mode, "labels", "sample.txt")));
            }

            string imagePath = Path.Combine(root, "data", "train", "images", "sample.png");
            var restoredSegment = new LabelingSegmentationObject
            {
                ObjectId = "segment-001",
                ClassName = "Defect"
            };
            var restoredState = new WpfObjectMetadataStateService();
            WpfObjectMetadataLoadResult load = persistence.LoadForImage(
                imagePath,
                rois,
                classNames,
                new[] { restoredSegment },
                restoredState,
                data);
            AssertTrue(load.IsCompatible, "current metadata schema should load");
            AssertEqual(2, load.LoadedCount);
            AssertTrue(restoredState.GetManualRoiMetadata(0).IsDefault, "first duplicate box should stay untagged");
            AssertTrue(restoredState.GetManualRoiMetadata(1).IsOccluded, "second duplicate box should restore occluded");
            AssertTrue(
                restoredState.GetManualRoiMetadata(1).Tags.Contains("Review"),
                "box tag should survive reopen");
            AssertTrue(
                restoredState.GetManualSegmentMetadata(restoredSegment).Tags.Contains("Boundary"),
                "segment tag should reconnect by ObjectId");

            persistence.Save(
                "sample.png",
                rois,
                classNames,
                new[] { restoredSegment },
                new WpfObjectMetadataStateService(),
                data);
            AssertTrue(!File.Exists(written[0]), "saving default metadata should remove the stale sidecar");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void TestObjectGroupSelectionAndFilters()
    {
        var state = new WpfObjectMetadataStateService();
        var segment = new LabelingSegmentationObject
        {
            ObjectId = "group-segment",
            ClassName = "Defect"
        };
        var selection = new WpfObjectReviewGroupSelectionService();
        selection.Begin();
        AssertTrue(
            selection.SetSelected(WpfObjectReviewItemRef.Manual(0), true, out string firstError),
            firstError);
        AssertTrue(
            selection.SetSelected(WpfObjectReviewItemRef.ManualSegment(0), true, out string secondError),
            secondError);
        AssertEqual(2, selection.SelectedCount);

        WpfPersistentObjectMetadata Resolve(WpfObjectReviewItemRef item)
            => item.Source == WpfObjectReviewSource.ManualRoi
                ? state.GetManualRoiMetadata(item.Index)
                : state.GetManualSegmentMetadata(segment);

        AssertTrue(
            selection.TryCreatePlan(Resolve, out WpfObjectReviewGroupCreatePlan plan, out string planError),
            planError);
        AssertEqual(32, plan.GroupId.Length);
        AssertEqual(2, plan.Members.Count);
        state.SetManualRoiGroupId(0, plan.GroupId);
        state.SetManualSegmentGroupId(segment, plan.GroupId);
        AssertEqual(plan.GroupId, state.GetManualRoiMetadata(0).GroupId);
        AssertEqual(plan.GroupId, state.GetManualSegmentMetadata(segment).GroupId);

        selection.Begin();
        selection.SetSelected(WpfObjectReviewItemRef.Manual(0), true, out _);
        selection.SetSelected(WpfObjectReviewItemRef.Manual(1), true, out _);
        AssertTrue(
            !selection.TryCreatePlan(Resolve, out _, out string alreadyGroupedError)
            && alreadyGroupedError.Contains("\uC774\uBBF8 \uADF8\uB8F9", StringComparison.Ordinal),
            "an object already in a group must not join another group");

        var first = new WpfObjectReviewListItem(
            "1. Defect",
            string.Empty,
            WpfObjectReviewSource.ManualRoi.ToString(),
            0,
            WpfObjectReviewItemRef.Manual(0));
        first.ApplyPersistentMetadata(new WpfPersistentObjectMetadata(false, Array.Empty<string>(), plan.GroupId));
        var second = new WpfObjectReviewListItem(
            "2. Defect",
            string.Empty,
            WpfObjectReviewSource.ManualSegment.ToString(),
            0,
            WpfObjectReviewItemRef.ManualSegment(0));
        second.ApplyPersistentMetadata(new WpfPersistentObjectMetadata(true, new[] { "Review" }, plan.GroupId));
        var ungrouped = new WpfObjectReviewListItem(
            "3. Defect",
            string.Empty,
            WpfObjectReviewSource.ManualRoi.ToString(),
            1,
            WpfObjectReviewItemRef.Manual(1));

        var viewModel = new WpfObjectReviewPanelViewModel();
        viewModel.SetObjects(new[] { first, second, ungrouped }, "3 objects");
        string groupFilter = viewModel.GroupFilters.Single(value =>
            value.StartsWith("\uADF8\uB8F9 1 ", StringComparison.Ordinal));
        viewModel.SelectedGroupFilter = groupFilter;
        AssertEqual(2, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        viewModel.SelectedGroupFilter = WpfObjectReviewPanelViewModel.UngroupedFilter;
        AssertEqual(1, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        AssertTrue(ungrouped.IsMetadataFilterMatch, "ungrouped filter should keep only ungrouped rows");
        viewModel.SelectedGroupFilter = WpfObjectReviewPanelViewModel.AllGroupsFilter;
        AssertEqual(3, viewModel.Objects.Count(item => item.IsMetadataFilterMatch));
        viewModel.SelectedObject = ungrouped;
        AssertTrue(viewModel.IsGroupStartVisible, "an ungrouped saved object should show only the group-start action");
        AssertTrue(
            !viewModel.IsGroupSelectionActionsVisible && !viewModel.IsSelectedGroupActionsVisible,
            "an ungrouped saved object should hide selection-mode and existing-group actions");
        viewModel.SetGroupSelectionMode(true);
        AssertTrue(
            !viewModel.IsGroupStartVisible
            && viewModel.IsGroupSelectionActionsVisible
            && !viewModel.IsSelectedGroupActionsVisible,
            "group selection mode should show only selection status and create/cancel actions");
        ungrouped.IsGroupSelected = true;
        viewModel.RefreshGroupSelectionPresentation();
        AssertTrue(
            viewModel.GroupSelectionStatusText.Contains("\uBC15\uC2A4 1\uAC1C", StringComparison.Ordinal)
            && viewModel.GroupSelectionStatusText.Contains("\uD074\uB798\uC2A4 Defect", StringComparison.Ordinal),
            "pending group preview should identify selected object types and classes");
        viewModel.SetGroupSelectionMode(false);
        viewModel.SelectedObject = first;
        AssertTrue(
            !viewModel.IsGroupStartVisible
            && !viewModel.IsGroupSelectionActionsVisible
            && viewModel.IsSelectedGroupActionsVisible,
            "a grouped saved object should show only its current-group actions");

        state.SetManualRoiGroupId(0, string.Empty);
        AssertEqual(1, state.DissolveInvalidGroups(2, new[] { segment }));
        AssertTrue(
            state.GetManualSegmentMetadata(segment).IsDefault,
            "a one-member remainder should dissolve automatically");
    }

    private static void TestObjectGroupPersistenceAndLegacyCompatibility()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            "object-group-" + Guid.NewGuid().ToString("N"));
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            var rois = new List<Rectangle>
            {
                new Rectangle(10, 12, 30, 24)
            };
            var classes = new List<string> { "Defect" };
            var segment = new LabelingSegmentationObject
            {
                ObjectId = "group-segment-001",
                ClassName = "Defect"
            };
            string groupId = Guid.NewGuid().ToString("N");
            var state = new WpfObjectMetadataStateService();
            state.SetManualRoiGroupId(0, groupId);
            state.SetManualSegmentGroupId(segment, groupId);

            var persistence = new WpfObjectMetadataPersistenceService();
            string path = persistence.Save(
                "sample.png",
                rois,
                classes,
                new[] { segment },
                state,
                data).Single();
            string json = File.ReadAllText(path);
            AssertTrue(json.Contains("\"Version\": 2", StringComparison.Ordinal), "group sidecar should use schema v2");
            AssertTrue(json.Contains(groupId, StringComparison.Ordinal), "both group members should persist the stable group id");

            string imagePath = Path.Combine(root, "data", "train", "images", "sample.png");
            var restoredSegment = new LabelingSegmentationObject
            {
                ObjectId = "group-segment-001",
                ClassName = "Defect"
            };
            var restored = new WpfObjectMetadataStateService();
            WpfObjectMetadataLoadResult load = persistence.LoadForImage(
                imagePath,
                rois,
                classes,
                new[] { restoredSegment },
                restored,
                data);
            AssertTrue(load.IsCompatible, "v2 group sidecar should reopen");
            AssertEqual(groupId, restored.GetManualRoiMetadata(0).GroupId);
            AssertEqual(groupId, restored.GetManualSegmentMetadata(restoredSegment).GroupId);

            File.WriteAllText(
                path,
                """
                {
                  "Version": 1,
                  "ImageName": "sample.png",
                  "Objects": [
                    {
                      "Kind": "Box",
                      "ObjectId": "",
                      "ClassName": "Defect",
                      "X": 10,
                      "Y": 12,
                      "Width": 30,
                      "Height": 24,
                      "Occurrence": 0,
                      "IsOccluded": true,
                      "Tags": [ "Legacy" ]
                    }
                  ]
                }
                """);
            var legacy = new WpfObjectMetadataStateService();
            WpfObjectMetadataLoadResult legacyLoad = persistence.LoadForImage(
                imagePath,
                rois,
                classes,
                Array.Empty<LabelingSegmentationObject>(),
                legacy,
                data);
            AssertTrue(legacyLoad.IsCompatible, "schema v1 should remain readable");
            AssertTrue(legacy.GetManualRoiMetadata(0).IsOccluded, "v1 metadata should retain its prior fields");
            AssertTrue(legacy.GetManualRoiMetadata(0).Tags.Contains("Legacy"), "v1 tags should remain readable");
            AssertTrue(string.IsNullOrEmpty(legacy.GetManualRoiMetadata(0).GroupId), "v1 metadata should load ungrouped");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void TestObjectGroupMutationContractsAreWired()
    {
        string root = FindRepositoryRoot();
        string objectCommands = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.ObjectReviewCommands.cs"));
        string splitCommands = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.SegmentationSplitCommands.cs"));
        string imageLoading = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.ImageLoading.cs"));

        AssertTrue(
            objectCommands.Contains("sourceMetadata.All", StringComparison.Ordinal)
            && objectCommands.Contains("SetManualSegmentGroupId", StringComparison.Ordinal),
            "merge should inherit a group only when every source has the same nonempty group");
        AssertTrue(
            splitCommands.Contains("GetManualSegmentMetadata(source)", StringComparison.Ordinal)
            && splitCommands.Contains("foreach (LabelingSegmentationObject segment in splitResult.Segments)", StringComparison.Ordinal),
            "split outputs should inherit the source group");
        AssertTrue(
            objectCommands.Contains("DissolveInvalidGroups", StringComparison.Ordinal),
            "delete and merge should dissolve invalid singleton groups");
        AssertTrue(
            imageLoading.Contains("CancelObjectGroupSelection(updateStatus: false)", StringComparison.Ordinal),
            "image transitions should cancel pending group selection");
    }

    private static void TestObjectGroupPanelDeclaresConsumerControls()
    {
        var panel = new WpfObjectReviewPanel
        {
            DataContext = new WpfObjectReviewPanelViewModel()
        };
        AssertTrue(panel.GroupSelectionBeginButton != null, "Object Review should start dedicated group selection");
        AssertTrue(panel.GroupCreateButton != null, "Object Review should explicitly create the selected group");
        AssertTrue(panel.GroupSelectionCancelButton != null, "group selection should have an explicit cancel");
        AssertTrue(panel.GroupMemberRemoveButton != null, "one object should be removable from its group");
        AssertTrue(panel.GroupDissolveButton != null, "a group should expose explicit dissolve");
        AssertTrue(panel.GroupOccludedButton != null, "group-level occluded editing should be available");
        AssertTrue(panel.GroupTagButton != null, "group-level Recipe tag editing should be available");
        AssertTrue(panel.GroupFilterBox != null, "Object Review should filter by group");
    }

    private static void TestObjectMetadataRecipeTagsRoundTrip()
    {
        string recipeName = "codex_object_metadata_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        string outputRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            recipeName);
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.ObjectReviewTags = new List<string>
            {
                "Review",
                "review",
                new string('x', 40)
            };
            data.ProjectSettings.EnsureDefaults();
            AssertEqual(2, data.ProjectSettings.ObjectReviewTags.Count);
            AssertEqual(32, data.ProjectSettings.ObjectReviewTags[1].Length);
            data.SaveConfig(recipeName, refreshDatasetVersion: false);

            CData loaded = new CData().LoadConfig(recipeName);
            AssertTrue(
                loaded.ProjectSettings.ObjectReviewTags.Contains("Review"),
                "Recipe tag definitions should survive save and reopen");
            AssertEqual(2, loaded.ProjectSettings.ObjectReviewTags.Count);
            AssertEqual(0, new LabelingProjectSettings().ObjectReviewTags.Count);
        }
        finally
        {
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static void TestObjectMetadataPanelDeclaresConsumerControls()
    {
        var panel = new WpfObjectReviewPanel
        {
            DataContext = new WpfObjectReviewPanelViewModel()
        };
        AssertTrue(panel.ObjectMetadataEditor != null, "Object Review should expose the metadata editor");
        AssertTrue(panel.PersistentOccludedButton != null, "Object Review should expose occluded metadata");
        AssertTrue(panel.MetadataTagBox?.IsEditable == true, "Recipe tag input should accept a new tag");
        AssertTrue(panel.PersistentTagButton != null, "Object Review should expose tag apply/remove");
        AssertTrue(panel.OccludedFilterButton != null, "Object Review should consume occluded metadata as a filter");
        AssertTrue(panel.MetadataTagFilterBox != null, "Object Review should consume tags as a filter");
        AssertTrue(panel.MetadataFilterResetButton != null, "metadata filters should have an explicit reset");
        AssertTrue(panel.RecipeMetadataTagsResetButton != null, "Recipe tag definitions should have an explicit reset");
    }
}
