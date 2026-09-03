using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;

namespace MvcVisionSystem
{
    // WPF owns anomaly review policy and classification mapping while the Core
    // service remains the single persistence authority for anomaly-review-status.json.
    public sealed class WpfAnomalyImageReviewWorkflowService
    {
        private readonly AnomalyImageReviewStatusService reviewStatus;

        public WpfAnomalyImageReviewWorkflowService(AnomalyImageReviewStatusService reviewStatus)
        {
            this.reviewStatus = reviewStatus ?? throw new ArgumentNullException(nameof(reviewStatus));
        }

        public IReadOnlyList<AnomalyImageReviewStatus> GetItems()
        {
            return reviewStatus.GetItems();
        }

        public void SetImages(IEnumerable<string> imagePaths)
        {
            reviewStatus.SetImages(imagePaths);
        }

        public void LoadReviewStatus(LabelingProjectData data, IEnumerable<string> imagePaths)
        {
            reviewStatus.LoadReviewStatus(data, imagePaths);
        }

        public AnomalyImageReviewSummary LoadPersistedSummary(LabelingProjectData data, int totalImageCount = 0)
        {
            return AnomalyImageReviewStatusService.LoadPersistedSummary(data, totalImageCount);
        }

        public void SaveReviewStatus(LabelingProjectData data)
        {
            reviewStatus.SaveReviewStatus(data);
        }

        public AnomalyImageReviewStatus GetOrCreate(string imagePath)
        {
            return reviewStatus.GetOrCreate(imagePath);
        }

        public AnomalyImageReviewFolderImportResult PreviewUnreviewedStatesFromParentFolders()
        {
            return reviewStatus.PreviewUnreviewedStatesFromParentFolders();
        }

        public AnomalyImageReviewFolderImportResult ImportUnreviewedStatesFromParentFolders()
        {
            return reviewStatus.ImportUnreviewedStatesFromParentFolders();
        }

        public bool TryFindNextUnreviewed(
            IReadOnlyList<string> orderedImagePaths,
            string currentImagePath,
            out string nextImagePath)
        {
            return reviewStatus.TryFindNextUnreviewed(orderedImagePaths, currentImagePath, out nextImagePath);
        }

        public WpfAnomalyImageReviewResult ApplyReviewState(
            WpfAnomalyImageReviewRequest request,
            LabelingProjectData data)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImagePath))
            {
                return WpfAnomalyImageReviewResult.NotApplicable();
            }

            AnomalyImageReviewStatus status = ApplyReviewStateCore(request);
            if (request.SaveReviewStatus)
            {
                reviewStatus.SaveReviewStatus(data);
            }

            return WpfAnomalyImageReviewResult.Applied(status);
        }

        public WpfAnomalyClassificationResult ApplyClassification(
            WpfAnomalyClassificationRequest request,
            LabelingProjectData data)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImagePath))
            {
                return WpfAnomalyClassificationResult.NotApplicable();
            }

            AnomalyClassificationDecision decision = AnomalyClassificationDecisionService.Build(
                request.Candidates,
                request.Options?.ToDecisionOptions());
            if (!decision.IsMapped)
            {
                return WpfAnomalyClassificationResult.Unmapped(decision);
            }

            AnomalyImageReviewStatus status = ApplyReviewStateCore(new WpfAnomalyImageReviewRequest(
                request.ImagePath,
                request.ImageName,
                decision.ReviewState,
                request.SaveReviewStatus));
            if (request.SaveReviewStatus)
            {
                reviewStatus.SaveReviewStatus(data);
            }

            return WpfAnomalyClassificationResult.Mapped(decision, status);
        }

        private AnomalyImageReviewStatus ApplyReviewStateCore(WpfAnomalyImageReviewRequest request)
        {
            return request.State switch
            {
                AnomalyImageReviewState.Normal => reviewStatus.MarkNormal(request.ImagePath, request.ImageName),
                AnomalyImageReviewState.Abnormal => reviewStatus.MarkAbnormal(request.ImagePath, request.ImageName),
                _ => reviewStatus.ClearReviewState(request.ImagePath, request.ImageName)
            };
        }
    }

    public sealed class WpfAnomalyImageReviewRequest
    {
        public WpfAnomalyImageReviewRequest(
            string imagePath,
            string imageName,
            AnomalyImageReviewState state,
            bool saveReviewStatus)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageName = imageName ?? string.Empty;
            State = state;
            SaveReviewStatus = saveReviewStatus;
        }

        public string ImagePath { get; }

        public string ImageName { get; }

        public AnomalyImageReviewState State { get; }

        public bool SaveReviewStatus { get; }
    }

    public sealed class WpfAnomalyClassificationRequest
    {
        public WpfAnomalyClassificationRequest(
            string imagePath,
            string imageName,
            IReadOnlyList<YoloWorkerSmokeCandidate> candidates,
            WpfAnomalyClassificationOptionsSnapshot options,
            bool saveReviewStatus)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageName = imageName ?? string.Empty;
            Candidates = candidates ?? Array.Empty<YoloWorkerSmokeCandidate>();
            Options = options;
            SaveReviewStatus = saveReviewStatus;
        }

        public string ImagePath { get; }

        public string ImageName { get; }

        public IReadOnlyList<YoloWorkerSmokeCandidate> Candidates { get; }

        public WpfAnomalyClassificationOptionsSnapshot Options { get; }

        public bool SaveReviewStatus { get; }
    }

    public sealed class WpfAnomalyClassificationOptionsSnapshot
    {
        public WpfAnomalyClassificationOptionsSnapshot(
            IEnumerable<string> normalClassNames,
            IEnumerable<string> abnormalClassNames,
            double minimumConfidence)
        {
            NormalClassNames = new List<string>(normalClassNames ?? Array.Empty<string>());
            AbnormalClassNames = new List<string>(abnormalClassNames ?? Array.Empty<string>());
            MinimumConfidence = minimumConfidence;
        }

        public IReadOnlyList<string> NormalClassNames { get; }

        public IReadOnlyList<string> AbnormalClassNames { get; }

        public double MinimumConfidence { get; }

        public static WpfAnomalyClassificationOptionsSnapshot From(AnomalyClassificationDecisionOptions options)
        {
            return new WpfAnomalyClassificationOptionsSnapshot(
                options?.NormalClassNames,
                options?.AbnormalClassNames,
                options?.MinimumConfidence ?? 0D);
        }

        internal AnomalyClassificationDecisionOptions ToDecisionOptions()
        {
            return new AnomalyClassificationDecisionOptions
            {
                NormalClassNames = NormalClassNames,
                AbnormalClassNames = AbnormalClassNames,
                MinimumConfidence = MinimumConfidence
            };
        }
    }

    public sealed class WpfAnomalyImageReviewResult
    {
        private WpfAnomalyImageReviewResult(bool isApplicable, AnomalyImageReviewStatus status)
        {
            IsApplicable = isApplicable;
            Status = status;
        }

        public bool IsApplicable { get; }

        public AnomalyImageReviewStatus Status { get; }

        public static WpfAnomalyImageReviewResult NotApplicable()
        {
            return new WpfAnomalyImageReviewResult(false, null);
        }

        public static WpfAnomalyImageReviewResult Applied(AnomalyImageReviewStatus status)
        {
            return new WpfAnomalyImageReviewResult(true, status);
        }
    }

    public sealed class WpfAnomalyClassificationResult
    {
        private WpfAnomalyClassificationResult(
            bool isApplicable,
            bool isMapped,
            AnomalyClassificationDecision decision,
            AnomalyImageReviewStatus status)
        {
            IsApplicable = isApplicable;
            IsMapped = isMapped;
            Decision = decision;
            Status = status;
        }

        public bool IsApplicable { get; }

        public bool IsMapped { get; }

        public AnomalyClassificationDecision Decision { get; }

        public AnomalyImageReviewStatus Status { get; }

        public static WpfAnomalyClassificationResult NotApplicable()
        {
            return new WpfAnomalyClassificationResult(false, false, null, null);
        }

        public static WpfAnomalyClassificationResult Unmapped(AnomalyClassificationDecision decision)
        {
            return new WpfAnomalyClassificationResult(true, false, decision, null);
        }

        public static WpfAnomalyClassificationResult Mapped(
            AnomalyClassificationDecision decision,
            AnomalyImageReviewStatus status)
        {
            return new WpfAnomalyClassificationResult(true, true, decision, status);
        }
    }
}
