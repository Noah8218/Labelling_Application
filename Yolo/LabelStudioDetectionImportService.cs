using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public static class LabelStudioDetectionImportService
    {
        public static LabelStudioDetectionImportResult ImportTasks(
            LabelingProjectData data,
            string taskJsonPath,
            string imageRoot,
            string targetSplit = YoloDatasetSplitService.TrainMode)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(taskJsonPath))
            {
                throw new ArgumentException("Label Studio task JSON path is required.", nameof(taskJsonPath));
            }

            if (!File.Exists(taskJsonPath))
            {
                throw new FileNotFoundException("Label Studio task JSON was not found.", taskJsonPath);
            }

            string split = YoloDatasetSplitService.NormalizeStandardSplit(targetSplit);
            if (string.IsNullOrWhiteSpace(split))
            {
                throw new ArgumentException("Target split must be train, valid, or test.", nameof(targetSplit));
            }

            List<LabelStudioDetectionTask> tasks = JsonConvert.DeserializeObject<List<LabelStudioDetectionTask>>(File.ReadAllText(taskJsonPath))
                ?? new List<LabelStudioDetectionTask>();

            data.NormalizeOutputPaths();
            data.EnsureYoloOutputDirectories();

            var result = new LabelStudioDetectionImportResult
            {
                TaskJsonPath = taskJsonPath,
                ImageRoot = YoloDatasetImportPathService.ResolveImageRoot(taskJsonPath, imageRoot),
                TargetSplit = split
            };

            string imageDirectory = Path.Combine(data.OutputRootPath, "data", split, "images");
            string labelDirectory = Path.Combine(data.OutputRootPath, "data", split, "labels");
            Directory.CreateDirectory(imageDirectory);
            Directory.CreateDirectory(labelDirectory);

            foreach (LabelStudioDetectionTask task in tasks)
            {
                if (!TryImportTask(data, task, imageDirectory, labelDirectory, result))
                {
                    result.SkippedTaskCount++;
                }
            }

            result.CategoryCount = data.ClassNamedList?.Count(item => !string.IsNullOrWhiteSpace(item?.Text)) ?? 0;
            data.SaveYoloDataYaml();
            return result;
        }

        private static bool TryImportTask(
            LabelingProjectData data,
            LabelStudioDetectionTask task,
            string imageDirectory,
            string labelDirectory,
            LabelStudioDetectionImportResult result)
        {
            string imageValue = task?.Data?.Image ?? string.Empty;
            if (string.IsNullOrWhiteSpace(imageValue))
            {
                return false;
            }

            string sourcePath = YoloDatasetImportPathService.ResolveSourceImagePath(result.ImageRoot, imageValue);
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            string fileName = YoloDatasetImportPathService.GetFileName(imageValue);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string targetImagePath = Path.Combine(imageDirectory, fileName);
            File.Copy(sourcePath, targetImagePath, overwrite: true);

            Size fallbackImageSize = ResolveImageSize(sourcePath);
            List<string> labelLines = BuildLabelLines(data, task, fallbackImageSize, result);
            string labelPath = Path.Combine(labelDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}.txt");
            File.WriteAllLines(labelPath, labelLines);

            result.ImportedTaskCount++;
            result.LabelFileCount++;
            result.ImportedResultCount += labelLines.Count;
            result.ImportedImagePaths.Add(targetImagePath);
            return true;
        }

        private static List<string> BuildLabelLines(
            LabelingProjectData data,
            LabelStudioDetectionTask task,
            Size fallbackImageSize,
            LabelStudioDetectionImportResult result)
        {
            var lines = new List<string>();
            IEnumerable<LabelStudioDetectionResult> results = (task?.Annotations ?? new List<LabelStudioDetectionAnnotation>())
                .SelectMany(annotation => annotation?.Result ?? new List<LabelStudioDetectionResult>());

            foreach (LabelStudioDetectionResult item in results)
            {
                if (!TryBuildLabelLine(data, item, fallbackImageSize, out string line))
                {
                    result.SkippedResultCount++;
                    continue;
                }

                lines.Add(line);
            }

            return lines;
        }

        private static bool TryBuildLabelLine(
            LabelingProjectData data,
            LabelStudioDetectionResult item,
            Size fallbackImageSize,
            out string line)
        {
            line = string.Empty;
            if (item == null
                || !string.Equals(item.Type, "rectanglelabels", StringComparison.OrdinalIgnoreCase)
                || item.Value?.RectangleLabels == null
                || item.Value.RectangleLabels.Length == 0
                || Math.Abs(item.ImageRotation) > double.Epsilon
                || Math.Abs(item.Value.Rotation) > double.Epsilon)
            {
                return false;
            }

            string className = ClassCatalogService.NormalizeClassName(item.Value.RectangleLabels[0]);
            int classIndex = ClassCatalogService.FindOrAddClass(data, className);
            Size imageSize = item.OriginalWidth > 0 && item.OriginalHeight > 0
                ? new Size(item.OriginalWidth, item.OriginalHeight)
                : fallbackImageSize;
            if (classIndex < 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
            {
                return false;
            }

            int left = (int)Math.Round(item.Value.X / 100D * imageSize.Width);
            int top = (int)Math.Round(item.Value.Y / 100D * imageSize.Height);
            int width = (int)Math.Round(item.Value.Width / 100D * imageSize.Width);
            int height = (int)Math.Round(item.Value.Height / 100D * imageSize.Height);
            Rectangle rectangle = Rectangle.Intersect(
                new Rectangle(left, top, width, height),
                new Rectangle(Point.Empty, imageSize));
            if (rectangle.IsEmpty)
            {
                return false;
            }

            line = YoloAnnotationService.TryCreateYoloLine(classIndex, rectangle, imageSize);
            return !string.IsNullOrWhiteSpace(line);
        }

        private static Size ResolveImageSize(string sourcePath)
        {
            using Image image = Image.FromFile(sourcePath);
            return image.Size;
        }

    }

}
