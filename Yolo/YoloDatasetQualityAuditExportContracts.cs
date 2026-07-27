namespace MvcVisionSystem.Yolo
{
    public sealed class YoloDatasetQualityAuditExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int LineCount { get; set; }

        public int MissingLabelCount { get; set; }

        public int InvalidLabelLineCount { get; set; }
    }
}
