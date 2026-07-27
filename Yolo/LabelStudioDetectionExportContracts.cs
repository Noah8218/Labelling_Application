using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class LabelStudioDetectionExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int TaskCount { get; set; }

        public int ReviewedTaskCount { get; set; }

        public int ResultCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();
    }

    public sealed class LabelStudioDetectionTask
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("data")]
        public LabelStudioDetectionTaskData Data { get; set; } = new LabelStudioDetectionTaskData();

        [JsonProperty("annotations")]
        public List<LabelStudioDetectionAnnotation> Annotations { get; } = new List<LabelStudioDetectionAnnotation>();
    }

    public sealed class LabelStudioDetectionTaskData
    {
        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("split")]
        public string Split { get; set; } = string.Empty;
    }

    public sealed class LabelStudioDetectionAnnotation
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("result")]
        public List<LabelStudioDetectionResult> Result { get; set; } = new List<LabelStudioDetectionResult>();

        [JsonProperty("was_cancelled")]
        public bool WasCancelled { get; set; }

        [JsonProperty("ground_truth")]
        public bool GroundTruth { get; set; }

        [JsonProperty("lead_time")]
        public double LeadTime { get; set; }
    }

    public sealed class LabelStudioDetectionResult
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
        public LabelStudioDetectionValue Value { get; set; } = new LabelStudioDetectionValue();

        [JsonProperty("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonProperty("image_rotation")]
        public int ImageRotation { get; set; }

        [JsonProperty("original_width")]
        public int OriginalWidth { get; set; }

        [JsonProperty("original_height")]
        public int OriginalHeight { get; set; }
    }

    public sealed class LabelStudioDetectionValue
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("rotation")]
        public int Rotation { get; set; }

        [JsonProperty("rectanglelabels")]
        public string[] RectangleLabels { get; set; } = Array.Empty<string>();
    }
}
