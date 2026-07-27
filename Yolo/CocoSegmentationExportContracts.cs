using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class CocoSegmentationExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public int AnnotationCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();
    }

    public sealed class CocoSegmentationDataset
    {
        [JsonProperty("info")]
        public CocoSegmentationInfo Info { get; set; } = new CocoSegmentationInfo();

        [JsonProperty("licenses")]
        public List<object> Licenses { get; } = new List<object>();

        [JsonProperty("images")]
        public List<CocoSegmentationImage> Images { get; } = new List<CocoSegmentationImage>();

        [JsonProperty("annotations")]
        public List<CocoSegmentationAnnotation> Annotations { get; } = new List<CocoSegmentationAnnotation>();

        [JsonProperty("categories")]
        public List<CocoSegmentationCategory> Categories { get; } = new List<CocoSegmentationCategory>();
    }

    public sealed class CocoSegmentationInfo
    {
        [JsonProperty("description")]
        public string Description { get; set; } = "OpenVisionLab Labeling Studio COCO segmentation export";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
    }

    public sealed class CocoSegmentationImage
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

    public sealed class CocoSegmentationAnnotation
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("image_id")]
        public int ImageId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("segmentation")]
        public List<double[]> Segmentation { get; set; } = new List<double[]>();

        [JsonProperty("bbox")]
        public double[] BBox { get; set; } = Array.Empty<double>();

        [JsonProperty("area")]
        public double Area { get; set; }

        [JsonProperty("iscrowd")]
        public int IsCrowd { get; set; }
    }

    public sealed class CocoSegmentationCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("supercategory")]
        public string SuperCategory { get; set; } = string.Empty;
    }
}
