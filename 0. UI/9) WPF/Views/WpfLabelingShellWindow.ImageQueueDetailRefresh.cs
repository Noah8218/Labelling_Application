using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenVisionLab;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Queue detail scanning stays in the concrete service. Only a bounded set of changed rows is applied at background priority.
        private async Task StartImageQueueDetailRefreshAsync(
            IReadOnlyList<string> imagePaths,
            IReadOnlyDictionary<string, WpfImageQueueItem> itemLookup,
            WpfImageQualityReviewWorkflowService reviewWorkflow,
            LabelingProjectData data,
            CancellationToken token)
        {
            if (imagePaths == null || imagePaths.Count == 0 || itemLookup == null || reviewWorkflow == null)
            {
                return;
            }

            try
            {
                await imageQueueDetailRefreshService.RefreshAsync(
                    imagePaths,
                    reviewWorkflow,
                    data,
                    (results, loadedCount, totalCount) => ApplyImageQueueDetailBatchAsync(
                        results,
                        itemLookup,
                        loadedCount,
                        totalCount,
                        token),
                    token).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();
                if (isApplicationCloseApproved)
                {
                    return;
                }

                await Dispatcher.InvokeAsync(
                    () => CompleteImageQueueDetailRefresh(token),
                    DispatcherPriority.Background,
                    token).Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // A new root or window close owns the newer queue state.
            }
        }

        private async Task ApplyImageQueueDetailBatchAsync(
            IReadOnlyList<WpfImageQueueDetailRefreshResult> results,
            IReadOnlyDictionary<string, WpfImageQueueItem> itemLookup,
            int loadedCount,
            int totalCount,
            CancellationToken token)
        {
            if (results == null || results.Count == 0)
            {
                return;
            }

            if (isApplicationCloseApproved)
            {
                return;
            }

            await Dispatcher.InvokeAsync(
                () => ApplyImageQueueDetailBatch(results, itemLookup, loadedCount, totalCount, token),
                DispatcherPriority.Background,
                token).Task.ConfigureAwait(false);
        }

        private void ApplyImageQueueDetailBatch(
            IReadOnlyList<WpfImageQueueDetailRefreshResult> results,
            IReadOnlyDictionary<string, WpfImageQueueItem> itemLookup,
            int loadedCount,
            int totalCount,
            CancellationToken token)
        {
            if (isApplicationCloseApproved || token.IsCancellationRequested)
            {
                return;
            }

            foreach (WpfImageQueueDetailRefreshResult result in results ?? Array.Empty<WpfImageQueueDetailRefreshResult>())
            {
                if (result == null
                    || itemLookup == null
                    || !itemLookup.TryGetValue(result.ImagePath, out WpfImageQueueItem item)
                    || item == null)
                {
                    continue;
                }

                if (result.Error != null)
                {
                    item.LabelStatus = "\uC0C1\uD0DC \uD655\uC778 \uC2E4\uD328";
                    item.DetectStatus = "\uB300\uAE30";
                    AppendLog($"Image status failed: {Path.GetFileName(item.ImagePath)}  {result.Error.Message}");
                    continue;
                }

                ApplyImageQueueDetail(item, result.Detail);
            }

            UpdateImageQueueDetailProgress(loadedCount, totalCount);
        }

        private void CompleteImageQueueDetailRefresh(CancellationToken token)
        {
            if (isApplicationCloseApproved || token.IsCancellationRequested)
            {
                return;
            }

            // One final full view refresh makes the active filter exact without re-evaluating all rows for every detail batch.
            imageQueueView?.Refresh();
            UpdateImageQueueStatusText();
            RefreshYoloTrainingStepCompletion();
        }

        private void UpdateImageQueueDetailProgress(int loadedCount, int totalCount)
        {
            int total = Math.Max(0, totalCount);
            int loaded = Math.Min(Math.Max(0, loadedCount), total);
            string activeText = string.IsNullOrWhiteSpace(activeImagePath)
                ? string.Empty
                : string.Format(
                    CultureInfo.InvariantCulture,
                    OpenVisionLanguageService.T("WpfShell.Status.DatasetDetailActiveImage"),
                    Path.GetFileName(activeImagePath));
            SetDatasetStatus(string.Format(
                CultureInfo.InvariantCulture,
                OpenVisionLanguageService.T("WpfShell.Status.DatasetDetailProgress"),
                imageQueueItems.Count,
                total,
                loaded,
                total,
                activeText));
        }

        private void ApplyImageQueueDetail(WpfImageQueueItem item, WpfImageQueueDetail detail)
        {
            if (item == null || detail == null)
            {
                return;
            }

            item.Dimensions = WpfImageQueueDetailLoader.FormatImageSize(detail.ImageSize);
            if (IsAnomalyDatasetPurpose())
            {
                ApplyAnomalyReviewStatusToItem(item, anomalyImageReviewWorkflowService.GetOrCreate(item.ImagePath));
                return;
            }
            ApplyReviewStatusToItemCore(item, detail.ReviewStatus, refreshTrainingStepCompletion: false);
        }

        private void ApplyReviewStatusToItem(WpfImageQueueItem item, YoloImageReviewStatus status)
        {
            ApplyReviewStatusToItemCore(item, status, refreshTrainingStepCompletion: true);
        }

        private void ApplyReviewStatusToItemCore(
            WpfImageQueueItem item,
            YoloImageReviewStatus status,
            bool refreshTrainingStepCompletion)
        {
            if (item == null || status == null)
            {
                return;
            }

            item.LabelStatus = FormatLabelStatusForQueue(status.LabelText);
            item.DetectStatus = FormatDetectionStatusForQueue(status);
            item.IsLabeled = status.IsLabeled;
            item.IsSaveRequired = false;
            item.ReviewState = status.ReviewState;
            item.QualityReviewState = status.QualityReviewState;
            item.QueueIconKind = GetQueueIconKind(status);
            item.QueueIconBrush = GetQueueIconBrush(status);
            item.QueueBadgeBackgroundBrush = GetQueueBadgeBackgroundBrush(status);
            item.QueueRowAccentBrush = GetQueueRowAccentBrush(status);
            item.QueueBadgeText = BuildQueueBadgeText(status);
            item.QueueStatusSummary = BuildQueueStatusSummary(status);
            item.Detail = BuildReviewDetailText(status);
            if (IsActiveImageQueueSaveRequired(item))
            {
                ApplySaveRequiredStatusToQueueItem(item, annotationDirtyReason);
            }

            RefreshActiveImageQualityReviewPresentation(item, status);

            if (refreshTrainingStepCompletion)
            {
                RefreshYoloTrainingStepCompletion();
            }
        }

        private void CancelImageQueueCatalogLoad(bool waitForCompletion)
        {
            CancellationTokenSource cts = imageQueueCatalogLoadCts;
            Task catalogTask = imageQueueCatalogLoadTask;
            if (cts == null)
            {
                return;
            }

            imageQueueCatalogLoadVersion++;
            cts.Cancel();
            if (waitForCompletion)
            {
                WaitForImageQueueDetailRefresh(catalogTask);
            }

            if (catalogTask == null || catalogTask.IsCompleted)
            {
                cts.Dispose();
            }
            else
            {
                catalogTask.ContinueWith(
                    _ => cts.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            if (ReferenceEquals(cts, imageQueueCatalogLoadCts))
            {
                imageQueueCatalogLoadCts = null;
            }

            if (ReferenceEquals(catalogTask, imageQueueCatalogLoadTask))
            {
                imageQueueCatalogLoadTask = Task.CompletedTask;
            }
        }

        private void CancelImageQueueDetailRefresh(bool waitForCompletion)
        {
            CancellationTokenSource cts = imageQueueDetailLoadCts;
            Task detailTask = imageQueueDetailLoadTask;
            if (cts == null)
            {
                return;
            }

            cts.Cancel();
            if (waitForCompletion)
            {
                WaitForImageQueueDetailRefresh(detailTask);
            }

            if (detailTask == null || detailTask.IsCompleted)
            {
                cts.Dispose();
            }
            else
            {
                detailTask.ContinueWith(
                    _ => cts.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            if (ReferenceEquals(cts, imageQueueDetailLoadCts))
            {
                imageQueueDetailLoadCts = null;
            }

            if (ReferenceEquals(detailTask, imageQueueDetailLoadTask))
            {
                imageQueueDetailLoadTask = Task.CompletedTask;
            }
        }

        private void WaitForImageQueueDetailRefresh(Task detailTask)
        {
            if (detailTask == null || detailTask.IsCompleted)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    detailTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                }

                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!detailTask.IsCompleted && stopwatch.Elapsed < TimeSpan.FromSeconds(2))
            {
                // Detail refresh resumes on the UI dispatcher; pump briefly so close can release image file handles.
                var frame = new DispatcherFrame();
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }

            if (detailTask.IsFaulted)
            {
                _ = detailTask.Exception;
            }
        }

    }
}
