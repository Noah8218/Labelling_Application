using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
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
            Func<ImageSource> previewFactory)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageName = Path.GetFileName(ImagePath);
            SplitText = splitText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            ObjectCountText = objectCount > 0 ? $"객체 {objectCount}" : "객체 없음";
            IsProblem = isProblem;
            this.previewFactory = previewFactory;
        }

        public string ImagePath { get; }
        public string ImageName { get; }
        public string SplitText { get; }
        public string StatusText { get; }
        public string DetailText { get; }
        public string ObjectCountText { get; }
        public bool IsProblem { get; }

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
            int problemCount,
            bool isTruncated)
        {
            Items = items ?? Array.Empty<WpfDatasetVisualQaItem>();
            ScannedImageCount = Math.Max(0, scannedImageCount);
            ProblemCount = Math.Max(0, problemCount);
            IsTruncated = isTruncated;
        }

        public IReadOnlyList<WpfDatasetVisualQaItem> Items { get; }
        public int ScannedImageCount { get; }
        public int ProblemCount { get; }
        public bool IsTruncated { get; }
    }
}
