namespace MvcVisionSystem.Yolo
{
    public sealed class YoloImageQualityReviewReportExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int TotalImageCount { get; set; }

        public int UnreviewedCount { get; set; }

        public int NeedsFixCount { get; set; }

        public int ReviewedCount { get; set; }
    }
}
