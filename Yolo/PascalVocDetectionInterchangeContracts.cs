using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class PascalVocDetectionExportResult
    {
        public string OutputDirectory { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public int XmlFileCount { get; set; }

        public int ObjectCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();

        public List<string> OutputPaths { get; } = new List<string>();
    }

    public sealed class PascalVocDetectionImportResult
    {
        public string AnnotationDirectory { get; set; } = string.Empty;

        public string ImageRoot { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedImageCount { get; set; }

        public int LabelFileCount { get; set; }

        public int ImportedObjectCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedXmlCount { get; set; }

        public int SkippedObjectCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }
}
