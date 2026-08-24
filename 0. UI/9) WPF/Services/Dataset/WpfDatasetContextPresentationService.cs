using System.Globalization;
using System.IO;
using MvcVisionSystem.Yolo;
using OpenVisionLab;

namespace MvcVisionSystem
{
    public sealed class WpfDatasetContextPresentation
    {
        public WpfDatasetContextPresentation(
            string datasetName,
            string purposeText,
            string storagePathText,
            string imageRootText,
            string sourceText,
            string combinedPathText,
            string tooltip)
        {
            DatasetName = datasetName ?? string.Empty;
            PurposeText = purposeText ?? string.Empty;
            StoragePathText = storagePathText ?? string.Empty;
            ImageRootText = imageRootText ?? string.Empty;
            SourceText = sourceText ?? string.Empty;
            CombinedPathText = combinedPathText ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
        }

        public string DatasetName { get; }

        public string PurposeText { get; }

        public string StoragePathText { get; }

        public string ImageRootText { get; }

        public string SourceText { get; }

        public string CombinedPathText { get; }

        public string Tooltip { get; }
    }

    public static class WpfDatasetContextPresentationService
    {
        public static string BuildDatasetName(string recipeName, string outputRootPath)
        {
            if (!string.IsNullOrWhiteSpace(recipeName))
            {
                return recipeName.Trim();
            }

            string outputRootName = GetPathLeafName(outputRootPath);
            return string.IsNullOrWhiteSpace(outputRootName)
                ? T("WpfShell.Dataset.Unselected")
                : outputRootName;
        }

        public static string FormatPurposeName(LabelingDatasetPurpose purpose)
        {
            return purpose switch
            {
                LabelingDatasetPurpose.Segmentation => T("WpfShell.Dataset.Purpose.Segmentation"),
                LabelingDatasetPurpose.AnomalyDetection => T("WpfShell.Dataset.Purpose.AnomalyDetection"),
                _ => T("WpfShell.Dataset.Purpose.ObjectDetection")
            };
        }

        public static WpfDatasetContextPresentation Build(
            string datasetName,
            string purposeText,
            string outputRootPath,
            string imageRootPath,
            int classCount = 0)
        {
            string normalizedName = string.IsNullOrWhiteSpace(datasetName)
                ? T("WpfShell.Dataset.Unselected")
                : datasetName.Trim();
            string normalizedPurpose = string.IsNullOrWhiteSpace(purposeText)
                ? T("WpfShell.Dataset.Purpose.Unselected")
                : purposeText.Trim();
            string outputText = string.IsNullOrWhiteSpace(outputRootPath)
                ? T("WpfShell.Dataset.Storage.Empty")
                : ShortenPath(outputRootPath.Trim());
            string imageText = string.IsNullOrWhiteSpace(imageRootPath)
                ? T("WpfShell.Dataset.Images.Empty")
                : ShortenPath(imageRootPath.Trim());

            string storagePathText = Format("WpfShell.Dataset.Storage.Summary", outputText);
            string imageRootText = Format("WpfShell.Dataset.Images.Summary", imageText);
            string classSourceText = classCount > 0
                ? Format("WpfShell.Dataset.Source.Classes", classCount)
                : T("WpfShell.Dataset.Source.ClassesMissing");
            string labelSourceText = string.IsNullOrWhiteSpace(outputRootPath)
                ? T("WpfShell.Dataset.Source.LabelsMissing")
                : T("WpfShell.Dataset.Source.LabelsReady");
            string sourceText = $"{classSourceText} / {labelSourceText}";
            string combinedPathText = $"{storagePathText}  /  {imageRootText}  /  {sourceText}";
            string tooltip =
                Format("WpfShell.Dataset.Tooltip.Dataset", normalizedName) + "\n" +
                Format("WpfShell.Dataset.Tooltip.Purpose", normalizedPurpose) + "\n" +
                Format("WpfShell.Dataset.Tooltip.Storage", outputRootPath) + "\n" +
                Format("WpfShell.Dataset.Tooltip.Images", imageRootPath) + "\n" +
                T("WpfShell.Dataset.Tooltip.Basis") + "\n" +
                T("WpfShell.Dataset.Tooltip.Classes") + "\n" +
                T("WpfShell.Dataset.Tooltip.Labels") + "\n" +
                T("WpfShell.Dataset.Tooltip.ChangeImages");

            return new WpfDatasetContextPresentation(
                normalizedName,
                normalizedPurpose,
                storagePathText,
                imageRootText,
                sourceText,
                combinedPathText,
                tooltip);
        }

        private static string ShortenPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length <= 54)
            {
                return path ?? string.Empty;
            }

            string normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fileName = Path.GetFileName(normalizedPath);
            string root = Path.GetPathRoot(path) ?? string.Empty;
            return string.IsNullOrWhiteSpace(fileName)
                ? path
                : $"{root}...\\{fileName}";
        }

        private static string GetPathLeafName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);

        private static string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, T(key), arguments ?? System.Array.Empty<object>());
        }
    }
}
