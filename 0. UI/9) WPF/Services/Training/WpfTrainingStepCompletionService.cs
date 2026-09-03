using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem
{
    /// <summary>
    /// Builds the seven-step training guide projection from immutable workflow inputs.
    /// The service intentionally has no Shell, ViewModel, control, or file-system access.
    /// </summary>
    public static class WpfTrainingStepCompletionService
    {
        public static WpfTrainingStepCompletionSnapshot Build(
            YoloDatasetReadinessReport report,
            IEnumerable<WpfTrainingStepQueueState> queueItems,
            string activeImagePath,
            int classCount,
            int manualRoiCount,
            int confirmedCandidateCount,
            bool hasDatasetSetup,
            bool hasCompletedCurrentDatasetTraining,
            PythonCommunicationStatus communicationStatus,
            int pendingCandidateCount)
        {
            IReadOnlyList<WpfTrainingStepQueueState> queue = (queueItems ?? Enumerable.Empty<WpfTrainingStepQueueState>())
                .Where(item => item != null)
                .ToList();
            YoloDatasetStatistics statistics = report?.Statistics;
            int savedObjectCount = statistics?.TotalObjectCount ?? 0;
            int totalImageCount = statistics != null && statistics.TotalImageCount > 0
                ? statistics.TotalImageCount
                : queue.Count;
            int completedImageCount = statistics != null && totalImageCount > 0
                ? Math.Min(statistics.TotalLabelFileCount, totalImageCount)
                : queue.Count(item => item.HasCompletedLabelWork);
            bool hasImages = queue.Count > 0 || !string.IsNullOrWhiteSpace(activeImagePath);
            bool hasClasses = classCount > 0;
            bool hasAnyLabelWork = manualRoiCount > 0
                || confirmedCandidateCount > 0
                || savedObjectCount > 0
                || queue.Any(item => item.IsLabeled);
            bool isLabelingComplete = hasImages && totalImageCount > 0
                ? completedImageCount >= totalImageCount
                : hasAnyLabelWork;
            string labelingStateText = isLabelingComplete
                ? "완료"
                : completedImageCount > 0 && totalImageCount > 0
                    ? $"{completedImageCount}/{totalImageCount}"
                    : "라벨 필요";
            bool datasetReady = report?.IsReady == true;

            string trainingState = communicationStatus?.LastTrainingState?.Trim() ?? string.Empty;
            bool hasTrainingStatus = WpfTrainingProgressPresentationService.HasTrainingStatus(communicationStatus);
            bool trainingCompletedFromWorker = WpfTrainingWeightsService.IsCompletedTrainingState(trainingState);
            bool trainingCompleted = trainingCompletedFromWorker || hasCompletedCurrentDatasetTraining;
            bool trainingRunning = hasTrainingStatus
                && !trainingCompleted
                && !WpfTrainingProgressPresentationService.IsTerminalTrainingState(trainingState);
            bool hasInferenceResult = pendingCandidateCount > 0
                || queue.Any(item => item.IsCandidate);

            return new WpfTrainingStepCompletionSnapshot(
                hasImages,
                new[]
                {
                    new WpfTrainingStepState(1, hasDatasetSetup, hasDatasetSetup ? "완료" : "데이터셋 필요"),
                    new WpfTrainingStepState(2, hasImages, hasImages ? "완료" : "이미지 필요"),
                    new WpfTrainingStepState(3, hasClasses, hasClasses ? "완료" : "클래스 필요"),
                    new WpfTrainingStepState(4, isLabelingComplete, labelingStateText),
                    new WpfTrainingStepState(5, datasetReady, datasetReady ? "완료" : "점검 필요"),
                    new WpfTrainingStepState(6, trainingCompleted, trainingCompleted ? "완료" : trainingRunning ? "진행 중" : "대기"),
                    new WpfTrainingStepState(7, hasInferenceResult, hasInferenceResult ? "후보 있음" : "추론 필요")
                });
        }
    }

    public sealed class WpfTrainingStepCompletionSnapshot
    {
        public WpfTrainingStepCompletionSnapshot(
            bool hasImages,
            IEnumerable<WpfTrainingStepState> steps)
        {
            HasImages = hasImages;
            Steps = (steps ?? Enumerable.Empty<WpfTrainingStepState>()).ToList();
        }

        public bool HasImages { get; }

        public IReadOnlyList<WpfTrainingStepState> Steps { get; }
    }

    public sealed class WpfTrainingStepState
    {
        public WpfTrainingStepState(int order, bool isCompleted, string stateText)
        {
            Order = order;
            IsCompleted = isCompleted;
            StateText = stateText ?? string.Empty;
        }

        public int Order { get; }

        public bool IsCompleted { get; }

        public string StateText { get; }
    }

    public sealed class WpfTrainingStepQueueState
    {
        public WpfTrainingStepQueueState(
            bool isLabeled,
            bool isSaveRequired,
            YoloImageReviewState reviewState,
            YoloImageQualityReviewState qualityReviewState)
        {
            IsLabeled = isLabeled;
            IsSaveRequired = isSaveRequired;
            ReviewState = reviewState;
            QualityReviewState = qualityReviewState;
        }

        public bool IsLabeled { get; }

        public bool IsSaveRequired { get; }

        public YoloImageReviewState ReviewState { get; }

        public YoloImageQualityReviewState QualityReviewState { get; }

        public bool IsCandidate => ReviewState == YoloImageReviewState.Candidate;

        public bool HasCompletedLabelWork
        {
            get
            {
                if (IsSaveRequired || QualityReviewState == YoloImageQualityReviewState.NeedsFix)
                {
                    return false;
                }

                return IsLabeled
                    || ReviewState == YoloImageReviewState.Confirmed
                    || ReviewState == YoloImageReviewState.Skipped
                    || ReviewState == YoloImageReviewState.NoCandidate;
            }
        }
    }
}
