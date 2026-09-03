using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Queue-driven detection owns batch progress and review-state writes; single-image worker calls stay in the main detection flow.
        private async void ExecuteDetectSelectedQueueCommand()
        {
            if (!EnsureInferenceModeForDetection())
            {
                return;
            }

            if (ImageQueueGrid.SelectedItem is not WpfImageQueueItem item)
            {
                AppendLog("\uBA3C\uC800 \uC774\uBBF8\uC9C0\uB97C \uC120\uD0DD\uD558\uC138\uC694.");
                return;
            }

            await RunInteractiveDetectionAsync(item.ImagePath, allowSmokeFallback: false).ConfigureAwait(true);
        }

        private async void ExecuteBatchDetectQueueCommand()
        {
            if (!EnsureInferenceModeForDetection())
            {
                return;
            }

            WpfBatchDetectionPlan plan = ShowBatchDetectionPreflight(
                GetVisibleQueueItems(),
                "\uD45C\uC2DC \uD589");
            if (plan != null)
            {
                await RunBatchDetectionAsync(plan.Items, plan.ScopeText).ConfigureAwait(true);
            }
        }

        private async void ExecuteRetryFailedQueueCommand()
        {
            if (!EnsureInferenceModeForDetection())
            {
                return;
            }

            WpfBatchDetectionPlan plan = ShowBatchDetectionPreflight(
                imageQueueItems.Where(item => item.ReviewState == YoloImageReviewState.Failed).ToList(),
                "\uC2E4\uD328 \uC7AC\uC2DC\uB3C4");
            if (plan != null)
            {
                await RunBatchDetectionAsync(plan.Items, plan.ScopeText).ConfigureAwait(true);
            }
        }

        private WpfBatchDetectionPlan ShowBatchDetectionPreflight(
            IReadOnlyList<WpfImageQueueItem> items,
            string scopeText)
        {
            var viewModel = new WpfBatchDetectionPreflightViewModel(global.Data, items, scopeText);
            var window = new WpfBatchDetectionPreflightWindow(viewModel)
            {
                Owner = this
            };
            window.ApplyThemeFrom(this);
            bool? accepted = window.ShowDialog();
            if (accepted != true || window.SelectedPlan == null)
            {
                AppendLog($"AI \uBC30\uCE58 \uAC80\uC0AC \uCDE8\uC18C: {scopeText}");
                return null;
            }

            AppendLog(
                $"AI \uBC30\uCE58 \uC0AC\uC804\uC810\uAC80 \uD1B5\uACFC: {scopeText} \u00B7 "
                + $"\uC2E4\uD589 {window.SelectedPlan.Items.Count}\uAC1C \u00B7 "
                + "\uACB0\uACFC\uB294 Candidate Review \uB300\uAE30, \uC790\uB3D9 \uC800\uC7A5 \uC5C6\uC74C");
            return window.SelectedPlan;
        }

        private void ExecuteStopBatchQueueCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            batchDetectionCts?.Cancel();
            AppendLog("\uC77C\uAD04 \uAC80\uC0AC \uC911\uC9C0\uB97C \uC694\uCCAD\uD588\uC2B5\uB2C8\uB2E4.");
        }

        // Queue result and progress projection stay beside the batch commands;
        // asynchronous execution/lifetime remains in BatchDetectionExecution.cs.
        private void ApplyDetectionResultToQueueItem(
            WpfImageQueueItem item,
            YoloWorkerSmokeTestResult result,
            bool saveReviewStatus = true,
            bool refreshQueueView = true,
            bool updateQueueStatusText = true)
        {
            if (item == null || result == null)
            {
                return;
            }

            string imageName = Path.GetFileNameWithoutExtension(item.ImagePath);
            YoloImageReviewStatus status = result.Succeeded
                ? result.CandidateCount > 0
                    ? imageQualityReviewWorkflowService.SetDetectionCandidates(item.ImagePath, imageName, result.CandidateCount)
                    : imageQualityReviewWorkflowService.SetDetectionNoCandidates(item.ImagePath, imageName)
                : imageQualityReviewWorkflowService.SetDetectionFailed(item.ImagePath, imageName, result.Summary);
            ApplyReviewStatusToItem(item, status);
            ApplyAnomalyClassificationToImage(item.ImagePath, imageName, result.Candidates, saveReviewStatus);
            if (saveReviewStatus)
            {
                imageQualityReviewWorkflowService.SaveReviewStatus(global.Data);
            }

            if (refreshQueueView)
            {
                imageQueueView?.Refresh();
            }

            if (updateQueueStatusText)
            {
                UpdateImageQueueStatusText();
            }
        }

        private IReadOnlyList<WpfImageQueueItem> GetVisibleQueueItems()
        {
            return imageQueueView == null
                ? imageQueueItems.ToList()
                : imageQueueView.Cast<object>().OfType<WpfImageQueueItem>().ToList();
        }

        private void UpdateBatchDetectionControls(string scopeText = "", string currentFileName = "")
        {
            UpdateYoloCommandButtons();
            WpfBatchDetectionControlState controlState = batchDetectionProgressService.BuildControlState(
                isBatchDetectionRunning,
                batchDetectionTotalCount,
                batchDetectionCompletedCount,
                scopeText,
                currentFileName);

            BatchProgressBar.Maximum = controlState.ProgressMaximum;
            BatchProgressBar.Value = controlState.ProgressValue;
            BatchStatusText.Text = controlState.StatusText;

            if (controlState.ShouldRefreshQueueStatus)
            {
                UpdateImageQueueStatusText();
            }
            else
            {
                SetDatasetStatus(controlState.DatasetStatusText);
            }
        }
    }
}
