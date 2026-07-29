using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace MvcVisionSystem
{
    public sealed class WpfDatasetVisualQaItem : WpfObservableViewModel
    {
        private readonly Func<ImageSource> previewFactory;
        private bool previewLoadAttempted;
        private ImageSource previewSource;

        internal WpfDatasetVisualQaItem(
            string imagePath,
            string splitText,
            string statusText,
            string detailText,
            int objectCount,
            bool isProblem,
            IEnumerable<int> classIndexes,
            Func<ImageSource> previewFactory)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageName = Path.GetFileName(ImagePath);
            SplitText = splitText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            ObjectCountText = objectCount > 0 ? $"객체 {objectCount}" : "객체 없음";
            IsProblem = isProblem;
            ClassIndexes = (classIndexes ?? Enumerable.Empty<int>())
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            this.previewFactory = previewFactory;
        }

        public string ImagePath { get; }
        public string ImageName { get; }
        public string SplitText { get; }
        public string StatusText { get; }
        public string DetailText { get; }
        public string ObjectCountText { get; }
        public bool IsProblem { get; }
        public IReadOnlyList<int> ClassIndexes { get; }

        public bool ContainsClassIndex(int classIndex)
            => classIndex >= 0 && ClassIndexes.Contains(classIndex);

        public ImageSource PreviewSource
        {
            get
            {
                if (!previewLoadAttempted)
                {
                    previewLoadAttempted = true;
                    previewSource = previewFactory?.Invoke();
                }

                return previewSource;
            }
        }

        public bool HasPreview => PreviewSource != null;
    }

    public sealed class WpfDatasetVisualQaCatalog
    {
        public WpfDatasetVisualQaCatalog(
            IReadOnlyList<WpfDatasetVisualQaItem> items,
            int scannedImageCount,
            int matchedImageCount,
            int problemCount,
            bool isTruncated)
        {
            Items = items ?? Array.Empty<WpfDatasetVisualQaItem>();
            ScannedImageCount = Math.Max(0, scannedImageCount);
            MatchedImageCount = Math.Max(0, matchedImageCount);
            ProblemCount = Math.Max(0, problemCount);
            IsTruncated = isTruncated;
        }

        public IReadOnlyList<WpfDatasetVisualQaItem> Items { get; }
        public int ScannedImageCount { get; }
        public int MatchedImageCount { get; }
        public int ProblemCount { get; }
        public bool IsTruncated { get; }
    }

    public sealed class WpfDatasetVisualQaClassFilterItem
    {
        public WpfDatasetVisualQaClassFilterItem(int? classIndex, string className)
        {
            ClassIndex = classIndex;
            ClassName = className?.Trim() ?? string.Empty;
            Text = classIndex.HasValue
                ? $"{classIndex.Value} · {ClassName}"
                : WpfDatasetHealthViewModel.AllVisualQaClasses;
        }

        public int? ClassIndex { get; }
        public string ClassName { get; }
        public string Text { get; }

        public bool HasSameIdentity(WpfDatasetVisualQaClassFilterItem other)
            => other != null
                && ClassIndex == other.ClassIndex
                && string.Equals(ClassName, other.ClassName, StringComparison.Ordinal);
    }
}
