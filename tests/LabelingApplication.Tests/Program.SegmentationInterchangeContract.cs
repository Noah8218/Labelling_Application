using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationInterchangeContractTests
{
    internal static void TestPreservationAndLossContract()
    {
        TestCapabilityMatrixIsComplete();
        TestStructuralFixtureReportsFormatLosses();
        TestAnnotationAuditDetectsCurrentRasterAndHoleSemantics();
        TestExportResultsSurfaceHoleLoss();
        TestCanonicalV3IdentityRoundTripAndLegacyCompatibility();
    }

    private static void TestCapabilityMatrixIsComplete()
    {
        foreach (SegmentationInterchangeTarget target in Enum.GetValues<SegmentationInterchangeTarget>())
        {
            IReadOnlyList<SegmentationInterchangeCapability> capabilities =
                SegmentationInterchangeContractService.GetCapabilities(target);
            AssertTrue(
                Enum.GetValues<SegmentationSemantic>().Length == capabilities.Count,
                $"{target} capability count");
            AssertTrue(
                capabilities.Count == capabilities.Select(capability => capability.Semantic).Distinct().Count(),
                $"{target} unique semantic count");
        }

        AssertLevel(
            SegmentationInterchangeTarget.CanonicalSegmentJson,
            SegmentationSemantic.Holes,
            SegmentationPreservationLevel.Preserved);
        AssertLevel(
            SegmentationInterchangeTarget.CanonicalSegmentJson,
            SegmentationSemantic.InstanceGrouping,
            SegmentationPreservationLevel.Preserved);
        AssertLevel(
            SegmentationInterchangeTarget.RasterMaskPng,
            SegmentationSemantic.RasterPixels,
            SegmentationPreservationLevel.Preserved);
        AssertLevel(
            SegmentationInterchangeTarget.YoloSegmentation,
            SegmentationSemantic.Holes,
            SegmentationPreservationLevel.Lost);
        AssertLevel(
            SegmentationInterchangeTarget.CocoPolygonSegmentation,
            SegmentationSemantic.Holes,
            SegmentationPreservationLevel.Lost);
        AssertLevel(
            SegmentationInterchangeTarget.CvatPolygon,
            SegmentationSemantic.ZOrder,
            SegmentationPreservationLevel.Lost);
    }

    private static void TestStructuralFixtureReportsFormatLosses()
    {
        SegmentationInterchangeProfile profile =
            SegmentationInterchangeProfile.CreateStructuralMaskFixture();
        SegmentationInterchangeAuditResult canonical = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.CanonicalSegmentJson,
            profile);
        SegmentationInterchangeAuditResult mask = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.RasterMaskPng,
            profile);
        SegmentationInterchangeAuditResult yolo = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.YoloSegmentation,
            profile);
        SegmentationInterchangeAuditResult coco = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.CocoPolygonSegmentation,
            profile);
        SegmentationInterchangeAuditResult cvat = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.CvatPolygon,
            profile);

        AssertTrue(!canonical.IsLossless, "canonical JSON alone should warn that raster pixels need the sibling PNG");
        AssertTrue(
            !canonical.Warnings.Any(warning => warning.StartsWith("InstanceGrouping:", StringComparison.Ordinal)),
            "canonical v3 should preserve instance grouping");
        AssertWarning(mask, SegmentationSemantic.PolygonGeometry, SegmentationPreservationLevel.Conditional);
        AssertWarning(mask, SegmentationSemantic.InstanceGrouping, SegmentationPreservationLevel.Lost);
        AssertWarning(yolo, SegmentationSemantic.Holes, SegmentationPreservationLevel.Lost);
        AssertWarning(coco, SegmentationSemantic.Holes, SegmentationPreservationLevel.Lost);
        AssertWarning(cvat, SegmentationSemantic.ZOrder, SegmentationPreservationLevel.Lost);
        AssertWarning(cvat, SegmentationSemantic.RemoveUnderlyingProvenance, SegmentationPreservationLevel.Lost);
    }

    private static void TestAnnotationAuditDetectsCurrentRasterAndHoleSemantics()
    {
        var annotation = new SegmentationAnnotationFile
        {
            Version = 2,
            ImageName = "fixture.png",
            ImageWidth = 64,
            ImageHeight = 64,
            Polygons = new List<SegmentationPolygonRecord>
            {
                new SegmentationPolygonRecord
                {
                    ClassIndex = 0,
                    ClassName = "Defect",
                    GeometryType = "RasterMask",
                    Points = RectanglePoints(8, 8, 48, 48),
                    Cutouts = new List<List<SegmentationPointRecord>>
                    {
                        RectanglePoints(20, 20, 32, 32)
                    }
                }
            }
        };

        SegmentationInterchangeProfile profile = SegmentationInterchangeProfile.FromAnnotation(annotation);
        AssertTrue(profile.RequiredSemantics.Contains(SegmentationSemantic.ClassIdentity), "annotation class semantic");
        AssertTrue(profile.RequiredSemantics.Contains(SegmentationSemantic.PolygonGeometry), "annotation polygon semantic");
        AssertTrue(profile.RequiredSemantics.Contains(SegmentationSemantic.RasterPixels), "annotation raster semantic");
        AssertTrue(profile.RequiredSemantics.Contains(SegmentationSemantic.Holes), "annotation hole semantic");
        AssertTrue(!profile.RequiredSemantics.Contains(SegmentationSemantic.InstanceGrouping), "v1/v2 cannot infer instance grouping");

        SegmentationInterchangeAuditResult yolo = SegmentationInterchangeContractService.Audit(
            SegmentationInterchangeTarget.YoloSegmentation,
            profile);
        AssertWarning(yolo, SegmentationSemantic.RasterPixels, SegmentationPreservationLevel.Conditional);
        AssertWarning(yolo, SegmentationSemantic.Holes, SegmentationPreservationLevel.Lost);
    }

    private static void TestExportResultsSurfaceHoleLoss()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });
            data.EnsureYoloOutputDirectories();

            var polygon = new LabelingSegmentationObject(
                new[]
                {
                    new Point(8, 8),
                    new Point(48, 8),
                    new Point(48, 48),
                    new Point(8, 48)
                },
                data.ClassNamedList[0])
            {
                CutoutPolygons = new List<List<Point>>
                {
                    new List<Point>
                    {
                        new Point(20, 20),
                        new Point(32, 20),
                        new Point(32, 32),
                        new Point(20, 32)
                    }
                }
            };
            var segments = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject> { polygon }
            };

            string imagePath = Path.Combine(data.TrainImagesPath, "fixture.png");
            using (Bitmap image = CreateSolidBitmap(64, 64, Color.Black))
            {
                image.Save(imagePath);
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                    "fixture.png",
                    image,
                    segments,
                    data.ClassNamedList,
                    data);
            }

            CocoSegmentationExportResult coco = CocoSegmentationExportService.ExportDataset(
                data,
                Path.Combine(root, "exports", "coco.json"),
                new[] { YoloDatasetSplitService.TrainMode });
            CvatSegmentationArchiveExportResult cvat = CvatSegmentationArchiveExportService.ExportDataset(
                data,
                Path.Combine(root, "exports", "cvat.zip"),
                new[] { YoloDatasetSplitService.TrainMode });
            YoloSegmentationTrainingLabelExportResult yolo = YoloSegmentationTrainingLabelService.Export(data);

            AssertTrue(coco.Warnings.Any(warning => warning.StartsWith("Holes: Lost", StringComparison.Ordinal)), "COCO hole-loss warning");
            AssertTrue(cvat.Warnings.Any(warning => warning.StartsWith("Holes: Lost", StringComparison.Ordinal)), "CVAT hole-loss warning");
            AssertTrue(yolo.Warnings.Any(warning => warning.StartsWith("Holes: Lost", StringComparison.Ordinal)), "YOLO hole-loss warning");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static void TestCanonicalV3IdentityRoundTripAndLegacyCompatibility()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "source"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });

            Size imageSize = new Size(40, 40);
            var maskData = new byte[imageSize.Width * imageSize.Height];
            FillMaskRectangle(maskData, imageSize, new Rectangle(2, 2, 6, 6));
            FillMaskRectangle(maskData, imageSize, new Rectangle(24, 24, 7, 7));
            var source = new LabelingSegmentationObject
            {
                ClassName = "Defect",
                ClassItem = data.ClassNamedList[0],
                ObjectId = "fixture-object-001",
                ComponentIndex = -1,
                ZOrder = 7,
                LastStructuralOperation = "RemoveUnderlying",
                MaskData = maskData,
                MaskSize = imageSize
            };
            var segments = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject> { source }
            };

            using var image = CreateSolidBitmap(imageSize.Width, imageSize.Height, Color.Black);
            YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                "identity.png",
                image,
                segments,
                data.ClassNamedList,
                data);

            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "identity.json");
            string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "identity.png");
            SegmentationAnnotationFile saved =
                JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
            AssertEqual(3, saved.Version);
            AssertEqual(2, saved.Polygons.Count);
            AssertTrue(saved.Polygons.All(record => record.ObjectId == "fixture-object-001"), "v3 object id");
            AssertTrue(
                saved.Polygons.Select(record => record.ComponentIndex).OrderBy(index => index).SequenceEqual(new[] { 0, 1 }),
                "v3 component indices");
            AssertTrue(saved.Polygons.All(record => record.ZOrder == 7), "v3 z-order");
            AssertTrue(
                saved.Polygons.All(record => record.LastStructuralOperation == "RemoveUnderlying"),
                "v3 structural operation provenance");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    segmentPath,
                    maskPath,
                    data.ClassNamedList,
                    imageSize);
            AssertEqual(2, loaded["Defect"].Count);
            AssertTrue(loaded["Defect"].All(segment => segment.ObjectId == "fixture-object-001"), "loaded object id");
            AssertTrue(
                loaded["Defect"].Select(segment => segment.ComponentIndex).OrderBy(index => index).SequenceEqual(new[] { 0, 1 }),
                "loaded component indices");
            AssertTrue(loaded["Defect"].All(segment => segment.ZOrder == 7), "loaded z-order");
            AssertTrue(
                loaded["Defect"].All(segment => segment.LastStructuralOperation == "RemoveUnderlying"),
                "loaded provenance");

            var reopenedByClass = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = loaded["Defect"].ToList()
            };
            var roundTripData = new CData();
            roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip"));
            roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
            roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });
            YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                "identity.png",
                image,
                reopenedByClass,
                roundTripData.ClassNamedList,
                roundTripData);
            string roundTripPath = Path.Combine(roundTripData.OutputRootPath, "data", "train", "segments", "identity.json");
            SegmentationAnnotationFile roundTrip =
                JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(roundTripPath));
            AssertEqual(3, roundTrip.Version);
            AssertTrue(roundTrip.Polygons.All(record => record.ObjectId == "fixture-object-001"), "round-trip object id");
            AssertTrue(
                roundTrip.Polygons.Select(record => record.ComponentIndex).OrderBy(index => index).SequenceEqual(new[] { 0, 1 }),
                "round-trip component indices");

            SegmentationInterchangeProfile v3Profile = SegmentationInterchangeProfile.FromAnnotation(saved);
            AssertTrue(v3Profile.RequiredSemantics.Contains(SegmentationSemantic.InstanceGrouping), "v3 grouping semantic");
            AssertTrue(v3Profile.RequiredSemantics.Contains(SegmentationSemantic.ZOrder), "v3 z-order semantic");
            AssertTrue(
                v3Profile.RequiredSemantics.Contains(SegmentationSemantic.RemoveUnderlyingProvenance),
                "v3 operation provenance semantic");
            SegmentationInterchangeAuditResult canonicalAudit = SegmentationInterchangeContractService.Audit(
                SegmentationInterchangeTarget.CanonicalSegmentJson,
                v3Profile);
            AssertTrue(
                !canonicalAudit.Warnings.Any(warning =>
                    warning.StartsWith("InstanceGrouping:", StringComparison.Ordinal)
                    || warning.StartsWith("ZOrder:", StringComparison.Ordinal)
                    || warning.StartsWith("RemoveUnderlyingProvenance:", StringComparison.Ordinal)),
                "canonical v3 metadata should be preserved");

            string legacyPath = Path.Combine(root, "legacy-v2.json");
            var legacy = new SegmentationAnnotationFile
            {
                Version = 2,
                ImageName = "legacy.png",
                ImageWidth = imageSize.Width,
                ImageHeight = imageSize.Height,
                Polygons = new List<SegmentationPolygonRecord>
                {
                    new SegmentationPolygonRecord
                    {
                        ClassIndex = 0,
                        ClassName = "Defect",
                        GeometryType = "Polygon",
                        Points = RectanglePoints(5, 5, 15, 15)
                    }
                }
            };
            File.WriteAllText(legacyPath, JsonConvert.SerializeObject(legacy, Formatting.Indented));
            LabelingSegmentationObject legacyLoaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    legacyPath,
                    data.ClassNamedList,
                    imageSize)["Defect"].Single();
            AssertEqual(new Rectangle(5, 5, 11, 11), legacyLoaded.Bounds);
            AssertEqual("legacy-000000", legacyLoaded.ObjectId);
            AssertEqual(0, legacyLoaded.ComponentIndex);
            AssertEqual(0, legacyLoaded.ZOrder);
            AssertEqual(string.Empty, legacyLoaded.LastStructuralOperation);

            LabelingSegmentationObject historyClone = WpfAnnotationHistoryService.CloneSegment(source);
            AssertEqual(source.ObjectId, historyClone.ObjectId);
            AssertEqual(source.ZOrder, historyClone.ZOrder);
            LabelingSegmentationObject duplicate = WpfAnnotationProductivityService.CreateOffsetSegment(
                source,
                imageSize,
                new WpfMaskAnnotationService());
            AssertEqual(string.Empty, duplicate.ObjectId);
            AssertEqual(-1, duplicate.ComponentIndex);
            AssertEqual(string.Empty, duplicate.LastStructuralOperation);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static void FillMaskRectangle(byte[] maskData, Size maskSize, Rectangle bounds)
    {
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                maskData[(y * maskSize.Width) + x] = 255;
            }
        }
    }

    private static List<SegmentationPointRecord> RectanglePoints(int left, int top, int right, int bottom)
    {
        return new List<SegmentationPointRecord>
        {
            new SegmentationPointRecord { X = left, Y = top },
            new SegmentationPointRecord { X = right, Y = top },
            new SegmentationPointRecord { X = right, Y = bottom },
            new SegmentationPointRecord { X = left, Y = bottom }
        };
    }

    private static void AssertLevel(
        SegmentationInterchangeTarget target,
        SegmentationSemantic semantic,
        SegmentationPreservationLevel expected)
    {
        SegmentationInterchangeCapability capability =
            SegmentationInterchangeContractService.GetCapabilities(target)
                .Single(item => item.Semantic == semantic);
        AssertTrue(capability.Level == expected, $"{target} {semantic}");
    }

    private static void AssertWarning(
        SegmentationInterchangeAuditResult audit,
        SegmentationSemantic semantic,
        SegmentationPreservationLevel level)
    {
        AssertTrue(
            audit.Warnings.Any(warning => warning.StartsWith($"{semantic}: {level}", StringComparison.Ordinal)),
            $"{audit.Target} should warn {semantic} as {level}");
    }
}
