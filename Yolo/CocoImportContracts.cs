using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class CocoDetectionImportResult
    {
        public string AnnotationPath { get; set; } = string.Empty;

        public string ImageRoot { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedImageCount { get; set; }

        public int LabelFileCount { get; set; }

        public int ImportedAnnotationCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedImageCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }

    public sealed class CocoSegmentationImportResult
    {
        public string AnnotationPath { get; set; } = string.Empty;

        public string ImageRoot { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedImageCount { get; set; }

        public int ImportedAnnotationCount { get; set; }

        public int ImportedSegmentFileCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedImageCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }
}
