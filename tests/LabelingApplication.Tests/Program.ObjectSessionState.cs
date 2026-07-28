using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using System.Drawing;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class ObjectSessionStateTests
{
    internal static void TestObjectSessionStateWorkflow()
    {
        TestStateServiceIdentityAndLifetime();
        TestLockedMaskPredicate();
        TestWindowCommandsAndPresentation();
    }

    private static void TestStateServiceIdentityAndLifetime()
    {
        var service = new WpfObjectSessionStateService();
        WpfObjectSessionState roi = service.ToggleManualRoiState(2, WpfObjectSessionStateKind.Hidden);
        AssertTrue(roi.IsHidden, "ROI hidden state should toggle on");
        service.ToggleManualRoiState(4, WpfObjectSessionStateKind.Pinned);
        service.ShiftRoiStatesAfterRemoval(1);
        AssertTrue(service.GetManualRoiState(1).IsHidden, "ROI states after a deletion should shift with their rows");
        AssertTrue(service.GetManualRoiState(3).IsPinned, "later ROI movement pin should shift after deletion");

        LabelingSegmentationObject original = Segment("stable-object");
        AssertTrue(
            service.ToggleManualSegmentState(original, WpfObjectSessionStateKind.Locked).IsLocked,
            "segment lock should toggle on");
        LabelingSegmentationObject restored = Segment("stable-object");
        AssertTrue(
            service.GetManualSegmentState(restored).IsLocked,
            "canonical object identity should retain session state across history clones");

        service.Clear();
        AssertTrue(service.GetManualRoiState(1).IsDefault, "image-session reset should clear ROI state");
        AssertTrue(service.GetManualSegmentState(restored).IsDefault, "image-session reset should clear segment state");
    }

    private static void TestLockedMaskPredicate()
    {
        var maskClass = new CClassItem { Text = "Mask", DrawColor = Color.LimeGreen };
        var locked = Raster("locked", maskClass, new Rectangle(5, 5, 8, 8));
        var editable = Raster("editable", maskClass, new Rectangle(5, 5, 8, 8));
        var segments = new List<LabelingSegmentationObject> { locked, editable };
        byte[] lockedBefore = locked.MaskData.ToArray();
        var service = new WpfMaskAnnotationService();

        AssertTrue(
            service.Erase(
                segments,
                new[] { new Point(8, 8) },
                2,
                locked.MaskSize,
                out _,
                out IReadOnlyList<LabelingSegmentationObject> changed,
                segment => !ReferenceEquals(segment, locked)),
            "eraser should still edit an unlocked overlapping mask");
        AssertTrue(changed.All(segment => !ReferenceEquals(segment, locked)),
            "locked mask should not be returned as changed");
        AssertTrue(locked.MaskData.SequenceEqual(lockedBefore),
            "locked mask pixels should remain unchanged");
    }

    private static void TestWindowCommandsAndPresentation()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        var data = new CData();
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.DeepSkyBlue });
        data.ClassNamedList.Add(new CClassItem { Text = "Mask", DrawColor = Color.LimeGreen });
        CGlobal.Inst.Data = data;

        var imageSize = new Size(40, 40);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<Rectangle> rois = GetPrivateField<List<Rectangle>>(window, "manualRois");
            List<string> roiClasses = GetPrivateField<List<string>>(window, "manualRoiClassNames");
            List<string> roiOverlayIds = GetPrivateField<List<string>>(window, "manualRoiOverlayIds");
            rois.Add(new Rectangle(2, 2, 10, 10));
            roiClasses.Add("Defect");
            roiOverlayIds.Add(string.Empty);

            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            LabelingSegmentationObject polygon = Segment("polygon");
            segments.Add(polygon);

            InvokePrivate(window, "RedrawReviewRois");
            SelectSegment(window, 0);
            AssertTrue(
                window.FindName("ToggleObjectHiddenButton") is System.Windows.Controls.Button
                    && window.FindName("ToggleObjectLockedButton") is System.Windows.Controls.Button
                    && window.FindName("ToggleObjectPinnedButton") is System.Windows.Controls.Button
                    && window.FindName("ObjectQualityReviewExpander") is System.Windows.Controls.Expander
                    && window.FindName("SegmentationAdvancedEditExpander") is System.Windows.Controls.Expander,
                "Object Review should expose compact state controls and contextual quality/segment editors");
            AssertTrue(
                window.FindName("ObjectQualityReviewExpander") is System.Windows.Controls.Expander qualityExpander
                    && !qualityExpander.IsExpanded,
                "image-level quality review should be collapsed so object rows remain the primary surface");
            AssertTrue(window.ObjectReviewViewModel.IsObjectSessionStateEnabled,
                "manual segment should enable session-state controls");
            AssertTrue(window.ObjectReviewViewModel.IsSegmentContextVisible
                && !window.ObjectReviewViewModel.IsSegmentAdvancedEditorOpen,
                "segment-only structural commands should be available through a collapsed contextual editor");
            AssertEqual(0, GetHistoryCount(window));
            AssertTrue(!window.StatusBarViewModel.IsAnnotationDirty,
                "session state should start outside label dirty state");

            window.ObjectReviewViewModel.ToggleObjectPinnedCommand.Execute(null);
            WpfObjectReviewListItem selected = window.ObjectReviewViewModel.SelectedObject;
            AssertTrue(selected.IsPinned && selected.StateBadgeText.Contains("\uC774\uB3D9 \uACE0\uC815", StringComparison.Ordinal),
                "pinned row should describe the movement-only constraint");
            AssertTrue(
                window.MainCanvasViewModel.PolygonOverlays.Any(overlay =>
                    overlay.Label.StartsWith("SEG", StringComparison.Ordinal)
                    && overlay.Color.ToArgb() == polygon.Color.ToArgb()),
                "movement pin should preserve the class color and ordinary segment label");
            AssertTrue(window.ObjectReviewViewModel.IsDeleteEnabled
                && window.ObjectReviewViewModel.IsApplyClassEnabled
                && window.ObjectReviewViewModel.IsSplitEnabled,
                "movement pin should continue to allow delete, class, and structural commands");
            AssertTrue(!InvokePrivateResult<bool>(
                    window,
                    "TryBeginSelectedSegmentEdit",
                    new OpenVisionLab.ImageCanvas.Canvas.CanvasImagePointEventArgs(
                        OpenVisionLab.ImageCanvas.Canvas.CanvasPointerButton.Left,
                        1,
                        0,
                        0,
                        new Point(20, 20),
                        PointF.Empty)),
                "movement pin should reject whole-polygon translation");
            AssertTrue(InvokePrivateResult<bool>(
                    window,
                    "TryBeginSelectedSegmentEdit",
                    new OpenVisionLab.ImageCanvas.Canvas.CanvasImagePointEventArgs(
                        OpenVisionLab.ImageCanvas.Canvas.CanvasPointerButton.Left,
                        1,
                        0,
                        0,
                        new Point(10, 10),
                        PointF.Empty)),
                "movement pin should still allow polygon vertex editing");
            InvokePrivate(window, "CompleteSelectedSegmentEdit");

            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.SelectedObject.IsLocked,
                "lock should remain visible in the rebuilt row");
            AssertTrue(!window.ObjectReviewViewModel.IsDeleteEnabled
                && !window.ObjectReviewViewModel.IsApplyClassEnabled
                && !window.ObjectReviewViewModel.IsSplitEnabled,
                "locked object should disable mutation commands");
            AssertTrue(!InvokePrivateResult<bool>(window, "DeleteSelectedObject"),
                "direct delete invocation should reject a locked object");
            AssertEqual(1, segments.Count);
            AssertTrue(!InvokePrivateResult<bool>(window, "TryDuplicateManualSegment", 0),
                "direct duplicate invocation should reject a locked object");

            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.SelectedObject.IsHidden,
                "hidden state should remain visible in Object Review");
            AssertTrue(window.MainCanvasViewModel.PolygonOverlays.All(overlay =>
                    !overlay.Label.Contains("polygon", StringComparison.OrdinalIgnoreCase)),
                "hidden polygon should be removed from canvas overlays");
            AssertTrue(!window.ObjectReviewViewModel.IsSplitEnabled,
                "hidden geometry should not expose structural edit commands");

            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);
            AssertTrue(window.MainCanvasViewModel.PolygonOverlays.Any(overlay =>
                    overlay.Label.StartsWith("SEG", StringComparison.Ordinal)),
                "showing an object should restore its ordinary class-colored overlay");

            SelectRoi(window);
            AssertTrue(!window.ObjectReviewViewModel.IsSegmentContextVisible
                && !window.ObjectReviewViewModel.IsSegmentAdvancedEditorOpen,
                "ROI selection should remove the segmentation-only editor from the active context");
            window.ObjectReviewViewModel.ToggleObjectPinnedCommand.Execute(null);
            string overlayId = GetPrivateField<List<string>>(window, "manualRoiOverlayIds")[0];
            var roiOverlay = window.MainCanvasViewModel.ImageViewer
                .GetCanvasOverlayManager()
                .GetOverlayByUniqueId(overlayId);
            AssertTrue(roiOverlay?.IsMoveLock == true && roiOverlay.IsControlLock == false,
                "pinned manual ROI should block movement without blocking resize, copy, or delete");
            AssertTrue(
                InvokePrivateResult<bool>(
                    window.MainCanvasViewModel,
                    "IsRoiMoveLocked",
                    (CanvasRect<float>)roiOverlay.Shape),
                "ROI mouse-move path should resolve the selected movement pin");
            AssertTrue(window.ObjectReviewViewModel.IsDeleteEnabled
                && window.ObjectReviewViewModel.IsApplyClassEnabled,
                "pinned ROI should keep delete and class commands enabled");
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);
            overlayId = GetPrivateField<List<string>>(window, "manualRoiOverlayIds")[0];
            roiOverlay = window.MainCanvasViewModel.ImageViewer
                .GetCanvasOverlayManager()
                .GetOverlayByUniqueId(overlayId);
            AssertTrue(roiOverlay?.IsControlLock == true && roiOverlay.IsMoveLock,
                "locked manual ROI should use the existing canvas control-lock contract");
            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);
            AssertEqual(string.Empty, GetPrivateField<List<string>>(window, "manualRoiOverlayIds")[0]);
            AssertTrue(window.ObjectReviewViewModel.SelectedObject.IsHidden,
                "hidden ROI should remain selectable from Object Review");

            AssertEqual(0, GetHistoryCount(window));
            AssertTrue(!window.StatusBarViewModel.IsAnnotationDirty,
                "hide/lock/pin should not create label history or dirty canonical data");
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
        }
    }

    private static void SelectSegment(WpfLabelingShellWindow window, int sourceIndex)
    {
        InvokePrivate(window, "RefreshObjectList");
        WpfObjectReviewListItem row = window.ObjectReviewViewModel.Objects
            .Single(item => item.IsManualSegment && item.SourceIndex == sourceIndex);
        window.ObjectReviewViewModel.SelectedObject = row;
        window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(row);
    }

    private static void SelectRoi(WpfLabelingShellWindow window)
    {
        InvokePrivate(window, "RefreshObjectList");
        WpfObjectReviewListItem row = window.ObjectReviewViewModel.Objects
            .Single(item => item.SourceKey == WpfObjectReviewSource.ManualRoi.ToString());
        window.ObjectReviewViewModel.SelectedObject = row;
        window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(row);
    }

    private static int GetHistoryCount(WpfLabelingShellWindow window)
        => GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count;

    private static LabelingSegmentationObject Segment(string objectId)
        => new LabelingSegmentationObject(
            new[]
            {
                new Point(10, 10),
                new Point(28, 10),
                new Point(28, 28),
                new Point(10, 28)
            },
            new CClassItem { Text = "Defect", DrawColor = Color.DeepSkyBlue })
        {
            ClassName = "Defect",
            ObjectId = objectId,
            ZOrder = 0,
            LastStructuralOperation = "Original"
        };

    private static LabelingSegmentationObject Raster(
        string objectId,
        CClassItem classItem,
        Rectangle bounds)
    {
        var size = new Size(24, 24);
        var mask = new byte[size.Width * size.Height];
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                mask[(y * size.Width) + x] = 1;
            }
        }

        return new LabelingSegmentationObject(Array.Empty<Point>(), classItem)
        {
            ClassName = classItem.Text,
            ObjectId = objectId,
            MaskData = mask,
            MaskSize = size,
            MaskBounds = bounds,
            RenderVersion = 1
        };
    }
}
