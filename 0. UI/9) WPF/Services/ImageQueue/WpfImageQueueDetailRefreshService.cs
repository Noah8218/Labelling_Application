using MvcVisionSystem.Yolo;
using OpenVisionLab;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    // The service owns file/detail batch work. WPF row mutation and dispatcher
    // lifetime stay with the Shell so the result boundary is independently testable.
    public sealed class WpfImageQueueDetailRefreshService
    {
        public const int BatchSize = 64;
        public const int Parallelism = 4;

        public async Task RefreshAsync(
            IReadOnlyList<string> imagePaths,
            WpfImageQualityReviewWorkflowService reviewWorkflow,
            LabelingProjectData data,
            Func<IReadOnlyList<WpfImageQueueDetailRefreshResult>, int, int, Task> applyBatchAsync,
            CancellationToken token)
        {
            if (imagePaths == null || imagePaths.Count == 0 || reviewWorkflow == null)
            {
                return;
            }

            if (applyBatchAsync == null)
            {
                throw new ArgumentNullException(nameof(applyBatchAsync));
            }

            int loadedCount = 0;
            var pendingResults = new List<WpfImageQueueDetailRefreshResult>(BatchSize);
            try
            {
                for (int startIndex = 0; startIndex < imagePaths.Count; startIndex += Parallelism)
                {
                    token.ThrowIfCancellationRequested();
                    int endIndex = Math.Min(imagePaths.Count, startIndex + Parallelism);
                    var detailTasks = new List<Task<WpfImageQueueDetailRefreshResult>>(endIndex - startIndex);
                    for (int index = startIndex; index < endIndex; index++)
                    {
                        detailTasks.Add(BuildResultAsync(imagePaths[index], reviewWorkflow, data, token));
                    }

                    WpfImageQueueDetailRefreshResult[] results = await Task.WhenAll(detailTasks).ConfigureAwait(false);
                    pendingResults.AddRange(results);
                    loadedCount = endIndex;
                    if (pendingResults.Count >= BatchSize || loadedCount == imagePaths.Count)
                    {
                        await ApplyBatchAsync(
                            pendingResults,
                            loadedCount,
                            imagePaths.Count,
                            applyBatchAsync).ConfigureAwait(false);
                    }
                }

                if (pendingResults.Count > 0)
                {
                    await ApplyBatchAsync(
                        pendingResults,
                        loadedCount,
                        imagePaths.Count,
                        applyBatchAsync).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // A newer queue root or window close owns the newer state.
            }
        }

        private static Task<WpfImageQueueDetailRefreshResult> BuildResultAsync(
            string imagePath,
            WpfImageQualityReviewWorkflowService reviewWorkflow,
            LabelingProjectData data,
            CancellationToken token)
        {
            return Task.Run(
                () =>
                {
                    try
                    {
                        return WpfImageQueueDetailRefreshResult.Success(
                            imagePath,
                            WpfImageQueueDetailLoader.Build(imagePath, reviewWorkflow, data));
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        return WpfImageQueueDetailRefreshResult.Failure(imagePath, ex);
                    }
                },
                token);
        }

        private static async Task ApplyBatchAsync(
            List<WpfImageQueueDetailRefreshResult> pendingResults,
            int loadedCount,
            int totalCount,
            Func<IReadOnlyList<WpfImageQueueDetailRefreshResult>, int, int, Task> applyBatchAsync)
        {
            if (pendingResults == null || pendingResults.Count == 0)
            {
                return;
            }

            WpfImageQueueDetailRefreshResult[] results = pendingResults.ToArray();
            pendingResults.Clear();
            await applyBatchAsync(results, loadedCount, totalCount).ConfigureAwait(false);
        }
    }

    public sealed class WpfImageQueueDetailRefreshResult
    {
        private WpfImageQueueDetailRefreshResult(
            string imagePath,
            WpfImageQueueDetail detail,
            Exception error)
        {
            ImagePath = imagePath ?? string.Empty;
            Detail = detail;
            Error = error;
        }

        public string ImagePath { get; }

        public WpfImageQueueDetail Detail { get; }

        public Exception Error { get; }

        public static WpfImageQueueDetailRefreshResult Success(
            string imagePath,
            WpfImageQueueDetail detail)
        {
            return new WpfImageQueueDetailRefreshResult(imagePath, detail, null);
        }

        public static WpfImageQueueDetailRefreshResult Failure(
            string imagePath,
            Exception error)
        {
            return new WpfImageQueueDetailRefreshResult(imagePath, null, error);
        }
    }
}
