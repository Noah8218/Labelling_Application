using System;
using System.IO;

namespace MvcVisionSystem
{
    public enum AnomalyImageReviewState
    {
        Unreviewed,
        Normal,
        Abnormal
    }

    public sealed class AnomalyImageReviewStatus
    {
        internal AnomalyImageReviewStatus(string imagePath)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageName = Path.GetFileNameWithoutExtension(ImagePath);
            ReviewState = AnomalyImageReviewState.Unreviewed;
        }

        public string ImagePath { get; }

        public string ImageName { get; }

        public AnomalyImageReviewState ReviewState { get; internal set; }

        public string ReviewStateName => ReviewState.ToString();

        public DateTime LastUpdatedUtc { get; internal set; }

        public bool IsReviewed => ReviewState == AnomalyImageReviewState.Normal
            || ReviewState == AnomalyImageReviewState.Abnormal;
    }

    public sealed class AnomalyImageReviewSummary
    {
        public int TotalImageCount { get; set; }

        public int ReviewedImageCount { get; set; }

        public int NormalImageCount { get; set; }

        public int AbnormalImageCount { get; set; }

        public int UnreviewedImageCount { get; set; }
    }

    public sealed class AnomalyImageReviewFolderImportResult
    {
        public int NormalImageCount { get; internal set; }

        public int AbnormalImageCount { get; internal set; }

        public int ExistingReviewCount { get; internal set; }

        public int UnmatchedImageCount { get; internal set; }

        public int ImportedImageCount => NormalImageCount + AbnormalImageCount;

        public bool HasChanges => ImportedImageCount > 0;
    }
}
