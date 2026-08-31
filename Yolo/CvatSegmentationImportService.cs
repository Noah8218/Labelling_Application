using MvcVisionSystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace MvcVisionSystem.Yolo
{
    public static class CvatSegmentationImportService
    {
        public static CvatSegmentationImportResult ImportArchive(
            LabelingProjectData data,
            string archivePath,
            string targetSplit = YoloDatasetSplitService.TrainMode)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(archivePath))
            {
                throw new ArgumentException("CVAT segmentation archive path is required.", nameof(archivePath));
            }

            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException("CVAT segmentation archive was not found.", archivePath);
            }

            string split = YoloDatasetSplitService.NormalizeStandardSplit(targetSplit);
            if (string.IsNullOrWhiteSpace(split))
            {
                throw new ArgumentException("Target split must be train, valid, or test.", nameof(targetSplit));
            }

            data.NormalizeOutputPaths();
            data.EnsureYoloOutputDirectories();

            var result = new CvatSegmentationImportResult
            {
                ArchivePath = archivePath,
                TargetSplit = split
            };

            string imageDirectory = Path.Combine(data.OutputRootPath, "data", split, "images");
            Directory.CreateDirectory(imageDirectory);

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                ZipArchiveEntry annotationEntry = archive.GetEntry("annotations.xml");
                if (annotationEntry == null)
                {
                    throw new InvalidDataException("CVAT archive does not contain annotations.xml.");
                }

                XDocument document;
                using (Stream annotationStream = annotationEntry.Open())
                {
                    document = XDocument.Load(annotationStream);
                }

                foreach (XElement imageElement in document.Root?.Elements("image") ?? Enumerable.Empty<XElement>())
                {
                    if (!TryImportImage(data, archive, imageElement, imageDirectory, result))
                    {
                        result.SkippedImageCount++;
                    }
                }
            }

            result.CategoryCount = data.ClassNamedList?.Count(item => !string.IsNullOrWhiteSpace(item?.Text)) ?? 0;
            data.SaveYoloDataYaml();
            return result;
        }

        private static bool TryImportImage(
            LabelingProjectData data,
            ZipArchive archive,
            XElement imageElement,
            string imageDirectory,
            CvatSegmentationImportResult result)
        {
            string archiveName = imageElement?.Attribute("name")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(archiveName)
                || !TryReadInt(imageElement, "width", out int width)
                || !TryReadInt(imageElement, "height", out int height)
                || width <= 0
                || height <= 0)
            {
                return false;
            }

            ZipArchiveEntry imageEntry = archive.GetEntry($"images/{archiveName.Replace('\\', '/')}");
            if (imageEntry == null)
            {
                return false;
            }

            string fileName = Path.GetFileName(archiveName.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string targetImagePath = Path.Combine(imageDirectory, fileName);
            imageEntry.ExtractToFile(targetImagePath, overwrite: true);

            using Image image = Image.FromFile(targetImagePath);
            Dictionary<string, List<LabelingSegmentationObject>> segmentsByClass = BuildSegments(
                data,
                imageElement,
                new Size(width, height),
                result);

            if (segmentsByClass.Count > 0)
            {
                SaveSegmentationArtifacts(data, result.TargetSplit, fileName, image, segmentsByClass);
                result.ImportedSegmentFileCount++;
                result.ImportedPolygonCount += segmentsByClass.Values.Sum(list => list.Count);
            }

            result.ImportedImageCount++;
            result.ImportedImagePaths.Add(targetImagePath);
            return true;
        }

        private static Dictionary<string, List<LabelingSegmentationObject>> BuildSegments(
            LabelingProjectData data,
            XElement imageElement,
            Size imageSize,
            CvatSegmentationImportResult result)
        {
            var segmentsByClass = new Dictionary<string, List<LabelingSegmentationObject>>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement polygonElement in imageElement.Elements("polygon"))
            {
                if (!TryBuildSegment(data, polygonElement, imageSize, out string className, out LabelingSegmentationObject segment))
                {
                    result.SkippedPolygonCount++;
                    continue;
                }

                if (!segmentsByClass.TryGetValue(className, out List<LabelingSegmentationObject> segments))
                {
                    segments = new List<LabelingSegmentationObject>();
                    segmentsByClass.Add(className, segments);
                }

                segments.Add(segment);
            }

            return segmentsByClass;
        }

        private static bool TryBuildSegment(
            LabelingProjectData data,
            XElement polygonElement,
            Size imageSize,
            out string className,
            out LabelingSegmentationObject segment)
        {
            className = ClassCatalogService.NormalizeClassName(polygonElement?.Attribute("label")?.Value);
            segment = null;
            if (string.IsNullOrWhiteSpace(className))
            {
                return false;
            }

            List<Point> points = ParsePoints(polygonElement?.Attribute("points")?.Value, imageSize);
            if (points.Count < 3)
            {
                return false;
            }

            int classIndex = ClassCatalogService.FindOrAddClass(data, className);
            if (classIndex < 0)
            {
                return false;
            }

            segment = new LabelingSegmentationObject(points, data.ClassNamedList[classIndex]);
            return true;
        }

        private static List<Point> ParsePoints(string value, Size imageSize)
        {
            var points = new List<Point>();
            foreach (string pair in (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split(',');
                if (parts.Length < 2
                    || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                    || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    continue;
                }

                points.Add(new Point((int)Math.Round(x), (int)Math.Round(y)));
            }

            return SegmentationGeometry.NormalizePolygon(points, imageSize, minimumDistance: 1);
        }

        private static void SaveSegmentationArtifacts(
            LabelingProjectData data,
            string targetSplit,
            string fileName,
            Image image,
            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> segmentsByClass)
        {
            int originalValidationPercent = data.ProjectSettings.YoloDataset.ValidationPercent;
            int originalTestPercent = data.ProjectSettings.YoloDataset.TestPercent;
            try
            {
                if (string.Equals(targetSplit, YoloDatasetSplitService.ValidMode, StringComparison.OrdinalIgnoreCase))
                {
                    data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                    data.ProjectSettings.YoloDataset.TestPercent = 0;
                }
                else if (string.Equals(targetSplit, YoloDatasetSplitService.TestMode, StringComparison.OrdinalIgnoreCase))
                {
                    data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                    data.ProjectSettings.YoloDataset.TestPercent = 100;
                }
                else
                {
                    data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                    data.ProjectSettings.YoloDataset.TestPercent = 0;
                }

                YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                    fileName,
                    image,
                    segmentsByClass,
                    data.ClassNamedList,
                    data);
            }
            finally
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = originalValidationPercent;
                data.ProjectSettings.YoloDataset.TestPercent = originalTestPercent;
            }
        }

        private static bool TryReadInt(XElement element, string attributeName, out int value)
        {
            value = 0;
            return int.TryParse(element?.Attribute(attributeName)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

    }

}
