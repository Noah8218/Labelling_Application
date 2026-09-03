using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MvcVisionSystem
{
    // The service owns the background, file-backed catalog calculation. The
    // Shell still owns cancellation/version guards, UI-thread application,
    // observable rows, and current-image transitions.
    public sealed class WpfImageQueueCatalogLoadService
    {
        private readonly WpfImageQueueSelectionService selectionService;

        public WpfImageQueueCatalogLoadService(WpfImageQueueSelectionService selectionService)
        {
            this.selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        }

        public WpfImageQueueCatalogLoadResult Build(
            string imageRoot,
            LabelingProjectData data,
            bool isAnomalyPurpose,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<string> imagePaths = selectionService.EnumerateImageFiles(imageRoot, cancellationToken);
            if (isAnomalyPurpose)
            {
                imagePaths = selectionService.InterleaveTopLevelFolderImages(
                    imageRoot,
                    imagePaths,
                    cancellationToken);
            }

            IReadOnlyList<WpfImageQueueCatalogEntry> catalogEntries = selectionService.CreateCatalogEntries(
                imagePaths,
                cancellationToken);

            var reviewStatus = new YoloImageReviewStatusService();
            var reviewWorkflow = new WpfImageQualityReviewWorkflowService(reviewStatus);
            reviewWorkflow.SetImages(imagePaths);
            reviewWorkflow.LoadReviewStatus(data, imagePaths);

            var anomalyReviewStatus = new AnomalyImageReviewStatusService();
            var anomalyReviewWorkflow = new WpfAnomalyImageReviewWorkflowService(anomalyReviewStatus);
            anomalyReviewWorkflow.SetImages(imagePaths);
            anomalyReviewWorkflow.LoadReviewStatus(data, imagePaths);
            AnomalyImageReviewFolderImportResult anomalyFolderStateSuggestion = isAnomalyPurpose
                ? anomalyReviewWorkflow.PreviewUnreviewedStatesFromParentFolders()
                : null;
            cancellationToken.ThrowIfCancellationRequested();

            return new WpfImageQueueCatalogLoadResult(
                imagePaths,
                catalogEntries,
                reviewStatus,
                anomalyReviewStatus,
                reviewWorkflow,
                anomalyReviewWorkflow,
                anomalyFolderStateSuggestion);
        }
    }

    public sealed class WpfImageQueueCatalogLoadResult
    {
        public WpfImageQueueCatalogLoadResult(
            IReadOnlyList<string> imagePaths,
            IReadOnlyList<WpfImageQueueCatalogEntry> catalogEntries,
            YoloImageReviewStatusService reviewStatus,
            AnomalyImageReviewStatusService anomalyReviewStatus,
            WpfImageQualityReviewWorkflowService reviewWorkflow,
            WpfAnomalyImageReviewWorkflowService anomalyReviewWorkflow,
            AnomalyImageReviewFolderImportResult anomalyFolderStateSuggestion)
        {
            ImagePaths = imagePaths ?? Array.Empty<string>();
            CatalogEntries = catalogEntries ?? Array.Empty<WpfImageQueueCatalogEntry>();
            ReviewStatus = reviewStatus ?? new YoloImageReviewStatusService();
            AnomalyReviewStatus = anomalyReviewStatus ?? new AnomalyImageReviewStatusService();
            ReviewWorkflow = reviewWorkflow ?? new WpfImageQualityReviewWorkflowService(ReviewStatus);
            AnomalyReviewWorkflow = anomalyReviewWorkflow ?? new WpfAnomalyImageReviewWorkflowService(AnomalyReviewStatus);
            AnomalyFolderStateSuggestion = anomalyFolderStateSuggestion;
        }

        public IReadOnlyList<string> ImagePaths { get; }

        public IReadOnlyList<WpfImageQueueCatalogEntry> CatalogEntries { get; }

        public YoloImageReviewStatusService ReviewStatus { get; }

        public AnomalyImageReviewStatusService AnomalyReviewStatus { get; }

        public WpfImageQualityReviewWorkflowService ReviewWorkflow { get; }

        public WpfAnomalyImageReviewWorkflowService AnomalyReviewWorkflow { get; }

        public AnomalyImageReviewFolderImportResult AnomalyFolderStateSuggestion { get; }
    }
}
