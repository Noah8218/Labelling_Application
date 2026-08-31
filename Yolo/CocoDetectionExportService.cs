using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace MvcVisionSystem.Yolo
{
    public static class CocoDetectionExportService
    {
        private static readonly string[] ImageExtensions = { ".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };

        public static CocoDetectionExportResult ExportDataset(
            LabelingProjectData data,
            string outputPath,
            IEnumerable<string> splits = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("COCO export output path is required.", nameof(outputPath));
            }

            CocoDetectionExportResult result = BuildDataset(data, splits, out CocoDetectionDataset dataset);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(dataset, Formatting.Indented), new UTF8Encoding(false));
            result.OutputPath = outputPath;
            return result;
        }

        public static CocoDetectionExportResult BuildDataset(
            LabelingProjectData data,
            IEnumerable<string> splits,
            out CocoDetectionDataset dataset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.NormalizeOutputPaths();
            var result = new CocoDetectionExportResult();
            dataset = new CocoDetectionDataset();

            IReadOnlyList<string> normalizedSplits = NormalizeSplits(splits);
            result.Splits.AddRange(normalizedSplits);

            for (int classIndex = 0; classIndex < (data.ClassNamedList?.Count ?? 0); classIndex++)
            {
                string className = data.ClassNamedList[classIndex]?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(className))
                {
                    continue;
                }

                dataset.Categories.Add(new CocoDetectionCategory
                {
                    Id = classIndex + 1,
                    Name = className.Trim(),
                    SuperCategory = "object"
                });
            }

            int nextImageId = 1;
            int nextAnnotationId = 1;
            foreach (string split in normalizedSplits)
            {
                string imageDirectory = Path.Combine(data.OutputRootPath, "data", split, "images");
                string labelDirectory = Path.Combine(data.OutputRootPath, "data", split, "labels");
                foreach (string imagePath in EnumerateImageFiles(imageDirectory))
                {
                    using Image image = Image.FromFile(imagePath);
                    int imageId = nextImageId++;
                    dataset.Images.Add(new CocoDetectionImage
                    {
                        Id = imageId,
                        FileName = FormatRelativePath(data.OutputRootPath, imagePath),
                        Width = image.Width,
                        Height = image.Height
                    });

                    string labelPath = Path.Combine(labelDirectory, $"{Path.GetFileNameWithoutExtension(imagePath)}.txt");
                    if (!File.Exists(labelPath))
                    {
                        continue;
                    }

                    foreach (string line in File.ReadLines(labelPath))
                    {
                        if (!YoloAnnotationService.TryParseYoloLine(line, image.Size, out int classIndex, out Rectangle bounds)
                            || classIndex < 0
                            || classIndex >= (data.ClassNamedList?.Count ?? 0)
                            || string.IsNullOrWhiteSpace(data.ClassNamedList[classIndex]?.Text))
                        {
                            result.SkippedAnnotationCount++;
                            continue;
                        }

                        dataset.Annotations.Add(new CocoDetectionAnnotation
                        {
                            Id = nextAnnotationId++,
                            ImageId = imageId,
                            CategoryId = classIndex + 1,
                            BBox = new[] { (double)bounds.X, bounds.Y, bounds.Width, bounds.Height },
                            Area = bounds.Width * bounds.Height,
                            IsCrowd = 0
                        });
                    }
                }
            }

            result.ImageCount = dataset.Images.Count;
            result.AnnotationCount = dataset.Annotations.Count;
            result.CategoryCount = dataset.Categories.Count;
            return result;
        }

        private static IReadOnlyList<string> NormalizeSplits(IEnumerable<string> splits)
        {
            var result = new List<string>();
            foreach (string split in splits ?? YoloDatasetSplitService.StandardModes)
            {
                string normalized = YoloDatasetSplitService.NormalizeStandardSplit(split);
                if (!string.IsNullOrWhiteSpace(normalized)
                    && !result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(normalized);
                }
            }

            return result.Count > 0 ? result : YoloDatasetSplitService.StandardModes.ToList();
        }

        private static IEnumerable<string> EnumerateImageFiles(string imageDirectory)
        {
            if (string.IsNullOrWhiteSpace(imageDirectory) || !Directory.Exists(imageDirectory))
            {
                yield break;
            }

            foreach (string imagePath in Directory.EnumerateFiles(imageDirectory)
                .Where(path => ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return imagePath;
            }
        }

        private static string FormatRelativePath(string root, string path)
        {
            string relative = Path.GetRelativePath(root, path);
            return relative.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        }
    }

}
