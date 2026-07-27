using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class CvatImageTaskArchiveExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public int BoxCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();

        public List<string> ArchiveEntryNames { get; } = new List<string>();
    }

    public sealed class CvatSegmentationArchiveExportResult
    {
        public string OutputPath { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public int PolygonCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedAnnotationCount { get; set; }

        public List<string> Splits { get; } = new List<string>();

        public List<string> ArchiveEntryNames { get; } = new List<string>();
    }
}
