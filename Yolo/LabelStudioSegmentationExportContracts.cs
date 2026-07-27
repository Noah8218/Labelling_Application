using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class LabelStudioSegmentationExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int TaskCount { get; set; }

        public int ReviewedTaskCount { get; set; }

        public int ResultCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();
    }

    public sealed class LabelStudioSegmentationTask
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("data")]
        public LabelStudioSegmentationTaskData Data { get; set; } = new LabelStudioSegmentationTaskData();

        [JsonProperty("annotations")]
        public List<LabelStudioSegmentationAnnotation> Annotations { get; } = new List<LabelStudioSegmentationAnnotation>();
    }

    public sealed class LabelStudioSegmentationTaskData
    {
        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("split")]
        public string Split { get; set; } = string.Empty;
    }

    public sealed class LabelStudioSegmentationAnnotation
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("result")]
        public List<LabelStudioSegmentationResult> Result { get; set; } = new List<LabelStudioSegmentationResult>();

        [JsonProperty("was_cancelled")]
        public bool WasCancelled { get; set; }

        [JsonProperty("ground_truth")]
        public bool GroundTruth { get; set; }

        [JsonProperty("lead_time")]
        public double LeadTime { get; set; }
    }

    public sealed class LabelStudioSegmentationResult
    {
        [JsonProperty("from_name")]
        public string FromName { get; set; } = string.Empty;

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("to_name")]
        public string ToName { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("value")]
        public LabelStudioSegmentationValue Value { get; set; } = new LabelStudioSegmentationValue();

        [JsonProperty("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonProperty("image_rotation")]
        public int ImageRotation { get; set; }

        [JsonProperty("original_width")]
        public int OriginalWidth { get; set; }

        [JsonProperty("original_height")]
        public int OriginalHeight { get; set; }
    }

    public sealed class LabelStudioSegmentationValue
    {
        [JsonProperty("points")]
        public List<double[]> Points { get; set; } = new List<double[]>();

        [JsonProperty("polygonlabels")]
        public string[] PolygonLabels { get; set; } = Array.Empty<string>();
    }
}
