using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class CocoDetectionExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public int AnnotationCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();
    }

    public sealed class CocoDetectionDataset
    {
        [JsonProperty("info")]
        public CocoDetectionInfo Info { get; set; } = new CocoDetectionInfo();

        [JsonProperty("licenses")]
        public List<object> Licenses { get; } = new List<object>();

        [JsonProperty("images")]
        public List<CocoDetectionImage> Images { get; } = new List<CocoDetectionImage>();

        [JsonProperty("annotations")]
        public List<CocoDetectionAnnotation> Annotations { get; } = new List<CocoDetectionAnnotation>();

        [JsonProperty("categories")]
        public List<CocoDetectionCategory> Categories { get; } = new List<CocoDetectionCategory>();
    }

    public sealed class CocoDetectionInfo
    {
        [JsonProperty("description")]
        public string Description { get; set; } = "OpenVisionLab Labeling Studio COCO detection export";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
    }

    public sealed class CocoDetectionImage
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public sealed class CocoDetectionAnnotation
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("image_id")]
        public int ImageId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("bbox")]
        public double[] BBox { get; set; } = Array.Empty<double>();

        [JsonProperty("area")]
        public double Area { get; set; }

        [JsonProperty("iscrowd")]
        public int IsCrowd { get; set; }
    }

    public sealed class CocoDetectionCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("supercategory")]
        public string SuperCategory { get; set; } = string.Empty;
    }
}
