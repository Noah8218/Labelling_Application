using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloSegmentationTrainingLabelExportResult
    {
        public int ImageCount { get; set; }

        public int LabelFileCount { get; set; }

        public int PolygonCount { get; set; }

        public int TrainPolygonCount { get; set; }

        public int ValidPolygonCount { get; set; }

        public int TestPolygonCount { get; set; }

        public int BackgroundImageCount { get; set; }

        public int EmptyLabelFileCount { get; set; }

        public List<string> Errors { get; } = new List<string>();

        public bool IsReady => Errors.Count == 0 && LabelFileCount > 0 && PolygonCount > 0;
    }
}
