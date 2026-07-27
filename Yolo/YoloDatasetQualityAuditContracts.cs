using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloDatasetQualityAuditReport
    {
        private readonly Dictionary<string, int> objectCountByClass = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public List<YoloDatasetQualityAuditSplitSummary> Splits { get; } = new List<YoloDatasetQualityAuditSplitSummary>();

        public int TotalImageCount => Splits.Sum(item => item.ImageCount);

        public int TotalLabelFileCount => Splits.Sum(item => item.LabelFileCount);

        public int TotalMissingLabelCount => Splits.Sum(item => item.MissingLabelCount);

        public int TotalEmptyLabelCount => Splits.Sum(item => item.EmptyLabelCount);

        public int TotalInvalidLabelLineCount => Splits.Sum(item => item.InvalidLabelLineCount);

        public int TotalObjectCount => Splits.Sum(item => item.ObjectCount);

        public IReadOnlyDictionary<string, int> ObjectCountByClass => objectCountByClass;

        public IReadOnlyList<string> SummaryLines
        {
            get
            {
                var lines = Splits
                    .Select(split => $"Dataset quality audit. Split:{split.Split}, Images:{split.ImageCount}, Labels:{split.LabelFileCount}, MissingLabels:{split.MissingLabelCount}, EmptyLabels:{split.EmptyLabelCount}, InvalidLabels:{split.InvalidLabelLineCount}, Objects:{split.ObjectCount}")
                    .ToList();

                foreach (KeyValuePair<string, int> item in objectCountByClass.OrderBy(item => item.Key))
                {
                    lines.Add($"Dataset quality class distribution. {item.Key}:{item.Value}");
                }

                return lines;
            }
        }

        internal void AddClassObject(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            objectCountByClass.TryGetValue(className, out int count);
            objectCountByClass[className] = count + 1;
        }
    }

    public sealed class YoloDatasetQualityAuditSplitSummary
    {
        private readonly Dictionary<string, int> objectCountByClass = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public string Split { get; set; } = string.Empty;

        public int ImageCount { get; internal set; }

        public int LabelFileCount { get; internal set; }

        public int MissingLabelCount { get; internal set; }

        public int EmptyLabelCount { get; internal set; }

        public int InvalidLabelLineCount { get; internal set; }

        public int ObjectCount { get; internal set; }

        public IReadOnlyDictionary<string, int> ObjectCountByClass => objectCountByClass;

        internal void AddClassObject(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            objectCountByClass.TryGetValue(className, out int count);
            objectCountByClass[className] = count + 1;
            ObjectCount++;
        }
    }
}
