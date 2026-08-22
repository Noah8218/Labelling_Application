using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public enum SegmentationInterchangeTarget
    {
        CanonicalSegmentJson,
        RasterMaskPng,
        YoloSegmentation,
        CocoPolygonSegmentation,
        CvatPolygon
    }

    public enum SegmentationSemantic
    {
        ClassIdentity,
        PolygonGeometry,
        RasterPixels,
        Holes,
        MultipleComponents,
        InstanceGrouping,
        ZOrder,
        RemoveUnderlyingProvenance
    }

    public enum SegmentationPreservationLevel
    {
        Preserved,
        Conditional,
        Lost
    }

    public sealed class SegmentationInterchangeCapability
    {
        public SegmentationInterchangeCapability(
            SegmentationSemantic semantic,
            SegmentationPreservationLevel level,
            string explanation)
        {
            Semantic = semantic;
            Level = level;
            Explanation = explanation ?? string.Empty;
        }

        public SegmentationSemantic Semantic { get; }

        public SegmentationPreservationLevel Level { get; }

        public string Explanation { get; }
    }

    public sealed class SegmentationInterchangeProfile
    {
        private readonly HashSet<SegmentationSemantic> requiredSemantics = new HashSet<SegmentationSemantic>();

        public IReadOnlyCollection<SegmentationSemantic> RequiredSemantics => requiredSemantics;

        public SegmentationInterchangeProfile Require(params SegmentationSemantic[] semantics)
        {
            foreach (SegmentationSemantic semantic in semantics ?? Array.Empty<SegmentationSemantic>())
            {
                requiredSemantics.Add(semantic);
            }

            return this;
        }

        public static SegmentationInterchangeProfile FromAnnotation(SegmentationAnnotationFile annotation)
        {
            var profile = new SegmentationInterchangeProfile();
            IReadOnlyList<SegmentationPolygonRecord> records =
                annotation?.Polygons ?? (IReadOnlyList<SegmentationPolygonRecord>)Array.Empty<SegmentationPolygonRecord>();
            if (records.Count == 0)
            {
                return profile;
            }

            profile.Require(
                SegmentationSemantic.ClassIdentity,
                SegmentationSemantic.PolygonGeometry);
            if (records.Any(record =>
                string.Equals(record?.GeometryType, "RasterMask", StringComparison.OrdinalIgnoreCase)))
            {
                profile.Require(SegmentationSemantic.RasterPixels);
            }

            if (records.Any(record => record?.Cutouts?.Any(cutout => cutout?.Count >= 3) == true))
            {
                profile.Require(SegmentationSemantic.Holes);
            }

            if (annotation.Version >= 3)
            {
                bool hasGroupedComponents = records
                    .Where(record => !string.IsNullOrWhiteSpace(record?.ObjectId))
                    .GroupBy(record => record.ObjectId, StringComparer.Ordinal)
                    .Any(group => group.Count() > 1);
                if (hasGroupedComponents)
                {
                    profile.Require(
                        SegmentationSemantic.MultipleComponents,
                        SegmentationSemantic.InstanceGrouping);
                }

                if (records.Any(record => (record?.ZOrder ?? 0) != 0))
                {
                    profile.Require(SegmentationSemantic.ZOrder);
                }

                if (records.Any(record => !string.IsNullOrWhiteSpace(record?.LastStructuralOperation)))
                {
                    profile.Require(SegmentationSemantic.RemoveUnderlyingProvenance);
                }
            }

            // Version 1/2 has no object/component identity, z-order, or structural
            // operation provenance, so those requirements cannot be inferred.
            return profile;
        }

        public static SegmentationInterchangeProfile CreateStructuralMaskFixture()
        {
            return new SegmentationInterchangeProfile().Require(
                SegmentationSemantic.ClassIdentity,
                SegmentationSemantic.PolygonGeometry,
                SegmentationSemantic.RasterPixels,
                SegmentationSemantic.Holes,
                SegmentationSemantic.MultipleComponents,
                SegmentationSemantic.InstanceGrouping,
                SegmentationSemantic.ZOrder,
                SegmentationSemantic.RemoveUnderlyingProvenance);
        }
    }

    public sealed class SegmentationInterchangeAuditResult
    {
        public SegmentationInterchangeAuditResult(
            SegmentationInterchangeTarget target,
            IReadOnlyList<SegmentationInterchangeCapability> capabilities)
        {
            Target = target;
            Capabilities = capabilities ?? Array.Empty<SegmentationInterchangeCapability>();
            Warnings = Capabilities
                .Where(capability => capability.Level != SegmentationPreservationLevel.Preserved)
                .Select(capability =>
                    $"{capability.Semantic}: {capability.Level} - {capability.Explanation}")
                .ToList();
        }

        public SegmentationInterchangeTarget Target { get; }

        public IReadOnlyList<SegmentationInterchangeCapability> Capabilities { get; }

        public IReadOnlyList<string> Warnings { get; }

        public bool IsLossless => Warnings.Count == 0;
    }

    public static class SegmentationInterchangeContractService
    {
        private static readonly IReadOnlyDictionary<SegmentationInterchangeTarget, IReadOnlyList<SegmentationInterchangeCapability>> CapabilityMatrix =
            new Dictionary<SegmentationInterchangeTarget, IReadOnlyList<SegmentationInterchangeCapability>>
            {
                [SegmentationInterchangeTarget.CanonicalSegmentJson] = new[]
                {
                    Preserved(SegmentationSemantic.ClassIdentity, "Class index and class name are stored together."),
                    Preserved(SegmentationSemantic.PolygonGeometry, "Outer polygon vertices are stored in image-pixel coordinates."),
                    Conditional(SegmentationSemantic.RasterPixels, "Raster pixels require the sibling class-index mask PNG; JSON stores derived contours."),
                    Preserved(SegmentationSemantic.Holes, "Cutout polygons are stored explicitly in segment JSON version 2."),
                    Preserved(SegmentationSemantic.MultipleComponents, "Version 3 records a component index for every object component."),
                    Preserved(SegmentationSemantic.InstanceGrouping, "Version 3 groups component records with a persistent object id."),
                    Preserved(SegmentationSemantic.ZOrder, "Version 3 stores object z-order on every component record."),
                    Preserved(SegmentationSemantic.RemoveUnderlyingProvenance, "Version 3 stores the last structural operation provenance.")
                },
                [SegmentationInterchangeTarget.RasterMaskPng] = new[]
                {
                    Conditional(SegmentationSemantic.ClassIdentity, "Pixel values depend on the external ordered class catalog and support at most 255 classes."),
                    Conditional(SegmentationSemantic.PolygonGeometry, "Vertices are rasterized to image pixels and cannot be recovered exactly."),
                    Preserved(SegmentationSemantic.RasterPixels, "Class-index pixels are preserved at the source image resolution."),
                    Preserved(SegmentationSemantic.Holes, "Holes are preserved as background pixels."),
                    Preserved(SegmentationSemantic.MultipleComponents, "Disconnected pixel components remain present."),
                    Lost(SegmentationSemantic.InstanceGrouping, "Same-class instances share one class value and cannot be separated reliably."),
                    Lost(SegmentationSemantic.ZOrder, "Only the final composited class value remains."),
                    Lost(SegmentationSemantic.RemoveUnderlyingProvenance, "Only the final pixels remain; the destructive operation is not recorded.")
                },
                [SegmentationInterchangeTarget.YoloSegmentation] = new[]
                {
                    Conditional(SegmentationSemantic.ClassIdentity, "Class identity depends on the external ordered names list."),
                    Conditional(SegmentationSemantic.PolygonGeometry, "Coordinates are normalized and raster masks are contour-converted."),
                    Conditional(SegmentationSemantic.RasterPixels, "Raster pixels are converted to polygon contours."),
                    Lost(SegmentationSemantic.Holes, "YOLO polygon rows do not encode cutout rings."),
                    Conditional(SegmentationSemantic.MultipleComponents, "Components are emitted as independent rows."),
                    Lost(SegmentationSemantic.InstanceGrouping, "Independent rows do not retain one-object component grouping."),
                    Lost(SegmentationSemantic.ZOrder, "YOLO segmentation rows have no z-order."),
                    Lost(SegmentationSemantic.RemoveUnderlyingProvenance, "YOLO rows contain final geometry only.")
                },
                [SegmentationInterchangeTarget.CocoPolygonSegmentation] = new[]
                {
                    Preserved(SegmentationSemantic.ClassIdentity, "Category id and category name mapping are exported."),
                    Preserved(SegmentationSemantic.PolygonGeometry, "Polygon coordinates are exported without coordinate normalization."),
                    Conditional(SegmentationSemantic.RasterPixels, "The current exporter writes polygon segmentation, not lossless RLE."),
                    Lost(SegmentationSemantic.Holes, "The current polygon exporter does not encode cutout rings."),
                    Conditional(SegmentationSemantic.MultipleComponents, "Components are exported as separate annotations."),
                    Lost(SegmentationSemantic.InstanceGrouping, "Source component grouping is not represented by the current exporter."),
                    Lost(SegmentationSemantic.ZOrder, "COCO polygon annotations do not carry the editor z-order contract."),
                    Lost(SegmentationSemantic.RemoveUnderlyingProvenance, "COCO annotations contain final geometry only.")
                },
                [SegmentationInterchangeTarget.CvatPolygon] = new[]
                {
                    Preserved(SegmentationSemantic.ClassIdentity, "Polygon labels and task label definitions are exported."),
                    Preserved(SegmentationSemantic.PolygonGeometry, "Polygon coordinates are exported without coordinate normalization."),
                    Conditional(SegmentationSemantic.RasterPixels, "The current CVAT exporter writes polygons rather than CVAT mask RLE."),
                    Lost(SegmentationSemantic.Holes, "The current polygon exporter does not encode cutout rings."),
                    Conditional(SegmentationSemantic.MultipleComponents, "Components are exported as independent polygons."),
                    Lost(SegmentationSemantic.InstanceGrouping, "Independent polygons do not retain one-object component grouping."),
                    Lost(SegmentationSemantic.ZOrder, "The current exporter writes z_order=0 for every polygon."),
                    Lost(SegmentationSemantic.RemoveUnderlyingProvenance, "CVAT polygons contain final geometry only.")
                }
            };

        public static IReadOnlyList<SegmentationInterchangeCapability> GetCapabilities(
            SegmentationInterchangeTarget target)
        {
            return CapabilityMatrix.TryGetValue(target, out IReadOnlyList<SegmentationInterchangeCapability> capabilities)
                ? capabilities
                : Array.Empty<SegmentationInterchangeCapability>();
        }

        public static SegmentationInterchangeAuditResult Audit(
            SegmentationInterchangeTarget target,
            SegmentationInterchangeProfile profile)
        {
            HashSet<SegmentationSemantic> required = new HashSet<SegmentationSemantic>(
                profile?.RequiredSemantics ?? Array.Empty<SegmentationSemantic>());
            IReadOnlyList<SegmentationInterchangeCapability> capabilities = GetCapabilities(target)
                .Where(capability => required.Contains(capability.Semantic))
                .ToList();
            return new SegmentationInterchangeAuditResult(target, capabilities);
        }

        public static SegmentationInterchangeAuditResult AuditAnnotation(
            SegmentationInterchangeTarget target,
            SegmentationAnnotationFile annotation)
        {
            return Audit(target, SegmentationInterchangeProfile.FromAnnotation(annotation));
        }

        public static void AppendDistinctWarnings(
            ICollection<string> target,
            SegmentationInterchangeAuditResult audit)
        {
            if (target == null || audit == null)
            {
                return;
            }

            foreach (string warning in audit.Warnings)
            {
                if (!target.Contains(warning))
                {
                    target.Add(warning);
                }
            }
        }

        private static SegmentationInterchangeCapability Preserved(
            SegmentationSemantic semantic,
            string explanation)
            => new SegmentationInterchangeCapability(
                semantic,
                SegmentationPreservationLevel.Preserved,
                explanation);

        private static SegmentationInterchangeCapability Conditional(
            SegmentationSemantic semantic,
            string explanation)
            => new SegmentationInterchangeCapability(
                semantic,
                SegmentationPreservationLevel.Conditional,
                explanation);

        private static SegmentationInterchangeCapability Lost(
            SegmentationSemantic semantic,
            string explanation)
            => new SegmentationInterchangeCapability(
                semantic,
                SegmentationPreservationLevel.Lost,
                explanation);
    }
}
