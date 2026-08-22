namespace MvcVisionSystem.Yolo
{
    public sealed class AnomalyClassificationDatasetExportResult
    {
        public string DatasetRootPath { get; set; } = string.Empty;

        public int NormalImageCount { get; set; }

        public int AbnormalImageCount { get; set; }

        public int SkippedImageCount { get; set; }

        public int TotalExportedImageCount => NormalImageCount + AbnormalImageCount;
    }
}
