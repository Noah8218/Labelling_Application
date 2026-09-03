using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using System;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Worker progress polling is isolated from readiness binding so live YOLO state updates can be changed without touching setup UI.
        private void UpdateTrainingProgressFromWorker()
        {
            PythonCommunicationStatus status = global.GetPythonCommunicationStatusSnapshot();
            bool hasStatus = WpfTrainingProgressPresentationService.HasTrainingStatus(status);
            bool hasCurrentStatus = hasStatus && IsTrainingStatusCurrent(status);
            bool isLiveTraining = hasCurrentStatus && WpfTrainingProgressPresentationService.IsLiveTrainingStatus(status);
            if (hasCurrentStatus)
            {
                isTrainingWorkflowRunning = isLiveTraining;
            }

            if (hasCurrentStatus && status.LastTrainingProgressPercent.HasValue)
            {
                SetTrainingProgressValue(Math.Clamp(status.LastTrainingProgressPercent.Value, 0, 100));
            }
            else if (!isTrainingCommandRunning && !isTrainingWorkflowRunning)
            {
                SetTrainingProgressValue(0);
            }

            if (hasCurrentStatus)
            {
                SetTrainingProgressStatus(
                    WpfTrainingProgressPresentationService.BuildProgressSummary(status),
                    WpfTrainingProgressPresentationService.BuildEpochSummary(status, isLiveTraining),
                    TrainingSettingsViewModel?.TrainingProgressValue ?? TrainingProgressBar?.Value ?? 0D,
                    isIndeterminate: isLiveTraining && !status.LastTrainingProgressPercent.HasValue);
                UpdateYoloTrainingGuideTrainingHistory(status);
                if (WpfTrainingWeightsService.IsCompletedTrainingState(status.LastTrainingState))
                {
                    TryApplyLatestTrainingWeightsFromProject(logIfUnchanged: false);
                }
            }
            else if (isTrainingWorkflowRunning)
            {
                SetTrainingProgressStatus(
                    WpfTrainingProgressPresentationService.BuildAcceptedWorkerWaitProgressText(),
                    WpfTrainingProgressPresentationService.BuildBeforeEpochText(),
                    TrainingSettingsViewModel?.TrainingProgressValue ?? TrainingProgressBar?.Value ?? 0D,
                    isIndeterminate: true);
            }
            else if (!isTrainingCommandRunning)
            {
                SetTrainingProgressStatus(WpfTrainingProgressPresentationService.BuildIdleProgressText(), string.Empty, 0D, isIndeterminate: false);
            }

            UpdateTrainingStatusVisual(status, lastYoloTrainingReadinessReport);
            UpdateYoloTrainingRecoveryStatus(status);
            RefreshYoloTrainingStepCompletion();
            UpdateYoloCommandButtons();
            if (hasCurrentStatus && WpfTrainingProgressPresentationService.IsTerminalTrainingState(status.LastTrainingState))
            {
                StopTrainingStatusPolling();
            }
        }

        private void StartTrainingStatusPolling()
        {
            trainingStatusPollStartedUtc = DateTime.UtcNow;
            RequestTrainingStatusSnapshotFromWorker();
            if (!trainingStatusPollTimer.IsEnabled)
            {
                trainingStatusPollTimer.Start();
            }
        }

        private void StopTrainingStatusPolling()
        {
            if (trainingStatusPollTimer.IsEnabled)
            {
                trainingStatusPollTimer.Stop();
            }
        }

        private bool IsTrainingStatusCurrent(PythonCommunicationStatus status)
        {
            if (!WpfTrainingProgressPresentationService.HasTrainingStatus(status))
            {
                return false;
            }

            if (!isTrainingWorkflowRunning
                || trainingStatusPollStartedUtc == DateTime.MinValue
                || !status.LastTrainingStatusAtUtc.HasValue)
            {
                return true;
            }

            return status.LastTrainingStatusAtUtc.Value >= trainingStatusPollStartedUtc.AddSeconds(-1);
        }

        private void TrainingStatusPollTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationCloseApproved)
            {
                StopTrainingStatusPolling();
                return;
            }

            RequestTrainingStatusSnapshotFromWorker();
            PythonCommunicationStatus status = global.GetPythonCommunicationStatusSnapshot();
            UpdateTrainingProgressFromWorker();
            bool hasCurrentStatus = WpfTrainingProgressPresentationService.HasTrainingStatus(status) && IsTrainingStatusCurrent(status);
            if (hasCurrentStatus && WpfTrainingProgressPresentationService.IsTerminalTrainingState(status.LastTrainingState))
            {
                StopTrainingStatusPolling();
                return;
            }

            if (!hasCurrentStatus
                && trainingStatusPollStartedUtc != DateTime.MinValue
                && DateTime.UtcNow - trainingStatusPollStartedUtc > TimeSpan.FromSeconds(TrainingStatusPollTimeoutSeconds))
            {
                string timeoutText = WpfTrainingProgressPresentationService.BuildStatusNoResponseText();
                WpfTrainingRecoveryStatus recovery = WpfTrainingProgressPresentationService.BuildStatusNoResponseRecovery(timeoutText);
                SetTrainingProgressStatus(timeoutText, string.Empty, 0D, isIndeterminate: false);
                SetYoloRecoveryStatus(recovery.Title, recovery.Detail, recovery.Action);
                StopTrainingStatusPolling();
            }
        }

        private void UpdateTrainingStatusVisual(PythonCommunicationStatus status, YoloDatasetReadinessReport report = null)
        {
            MediaBrush readinessBrush = report == null
                ? ResolveBrushResource("SecondaryTextBrush", MediaBrushes.Gray)
                : report.IsReady
                    ? ResolveBrushResource("SuccessBrush", MediaBrushes.LimeGreen)
                    : ResolveBrushResource("WarningBrush", MediaBrushes.DarkOrange);
            MediaBrush stateBrush = ResolveTrainingStateBrush(status);
            SetTrainingStatusBrushes(readinessBrush, stateBrush);
        }

        private void RequestTrainingStatusSnapshotFromWorker()
        {
            if (!isTrainingWorkflowRunning)
            {
                return;
            }

            global.ModelRuntime.DeepLearning?.SendModelStatus(
                WpfYoloRuntimePresentationService.CreateRequestId(),
                ensureLoaded: false);
        }

        private void UpdateYoloTrainingRecoveryStatus(PythonCommunicationStatus status)
        {
            if (!WpfTrainingProgressPresentationService.HasTrainingStatus(status))
            {
                return;
            }

            string state = status.LastTrainingState?.Trim() ?? string.Empty;
            if (string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
            {
                WpfTrainingRecoveryStatus recovery = WpfTrainingProgressPresentationService.BuildFailedRecovery(
                    WpfTrainingProgressPresentationService.BuildFailureDetail(status));
                SetYoloRecoveryStatus(recovery.Title, recovery.Detail, recovery.Action);
                return;
            }

            if (WpfTrainingWeightsService.IsCompletedTrainingState(state)
                || WpfTrainingProgressPresentationService.IsLiveTrainingStatus(status)
                || string.Equals(state, "started", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "running", StringComparison.OrdinalIgnoreCase))
            {
                ClearYoloRecoveryStatus();
            }
        }

        private MediaBrush ResolveTrainingStateBrush(PythonCommunicationStatus status)
        {
            if (!WpfTrainingProgressPresentationService.HasTrainingStatus(status))
            {
                return ResolveBrushResource("SecondaryTextBrush", MediaBrushes.Gray);
            }

            string state = status.LastTrainingState?.Trim() ?? string.Empty;
            if (WpfTrainingWeightsService.IsCompletedTrainingState(state))
            {
                return ResolveBrushResource("SuccessBrush", MediaBrushes.LimeGreen);
            }

            if (string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveBrushResource("ErrorBrush", MediaBrushes.IndianRed);
            }

            if (string.Equals(state, "stopped", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveBrushResource("WarningBrush", MediaBrushes.DarkOrange);
            }

            if (!WpfTrainingProgressPresentationService.IsTerminalTrainingState(state) || status.LastTrainingProgressPercent.HasValue)
            {
                return ResolveBrushResource("InfoBrush", MediaBrushes.DodgerBlue);
            }

            return ResolveBrushResource("SecondaryTextBrush", MediaBrushes.Gray);
        }

        private MediaBrush ResolveBrushResource(string key, MediaBrush fallback)
        {
            return TryFindResource(key) as MediaBrush ?? fallback;
        }


    }
}
