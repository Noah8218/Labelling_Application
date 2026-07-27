namespace MvcVisionSystem.Yolo
{
    /// <summary>
    /// Exports a selected segmentation adapter's raw raster predictions without touching the recipe or its U-Net export.
    /// </summary>
    public sealed class SegmentationPredictionExportRequest
    {
        public string AdapterKey { get; set; } = string.Empty;

        public string Engine { get; set; } = string.Empty;

        public string PythonExecutablePath { get; set; } = string.Empty;

        public string ScriptPath { get; set; } = string.Empty;

        public string WeightsPath { get; set; } = string.Empty;

        public string DatasetExportRootPath { get; set; } = string.Empty;

        public string Split { get; set; } = "test";

        public string OutputRootPath { get; set; } = string.Empty;

        public int ImageSize { get; set; } = 320;

        public double Confidence { get; set; } = 0.25D;

        public string Device { get; set; } = "cpu";
    }

    public sealed class SegmentationPredictionExportResult
    {
        public bool Succeeded { get; internal set; }

        public string PredictionManifestPath { get; internal set; } = string.Empty;

        public string Output { get; internal set; } = string.Empty;

        public string Error { get; internal set; } = string.Empty;
    }
}
