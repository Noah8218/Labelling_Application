namespace MvcVisionSystem.Yolo
{
    public sealed class DatasetExportCapability
    {
        public string FormatKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public string DatasetPurpose { get; set; } = string.Empty;

        public bool IsImplemented { get; set; }

        public bool IsRecommendedNext { get; set; }

        public string RequirementSummary { get; set; } = string.Empty;

        public string VerificationSwitch { get; set; } = string.Empty;
    }
}
