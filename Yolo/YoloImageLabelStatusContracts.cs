using System;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloImageLabelStatus
    {
        public YoloImageLabelStatus(string labelPath, int objectCount, int invalidLineCount)
        {
            LabelPath = labelPath ?? string.Empty;
            ObjectCount = objectCount;
            InvalidLineCount = invalidLineCount;
        }

        public string LabelPath { get; }

        public int ObjectCount { get; }

        public int InvalidLineCount { get; }

        public bool HasLabelFile => !string.IsNullOrWhiteSpace(LabelPath);

        public bool HasObjects => ObjectCount > 0;

        public string Text
        {
            get
            {
                if (!HasLabelFile)
                {
                    return "No Label";
                }

                if (ObjectCount > 0)
                {
                    return InvalidLineCount > 0 ? $"Label {ObjectCount} / Invalid {InvalidLineCount}" : $"Label {ObjectCount}";
                }

                return InvalidLineCount > 0 ? $"Invalid {InvalidLineCount}" : "Empty Label";
            }
        }
    }
}
