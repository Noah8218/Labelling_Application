using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using OpenVisionLab;
using OpenVisionLab.Wpf.MessageDialogs;
using System.Windows.Threading;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Window lifecycle is invoked through WpfLabelingShellViewModel commands, not XAML event handlers.
        private void ExecuteLoadedCommand()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isApplicationCloseApproved)
                {
                    return;
                }

                RefreshYoloStatus();
                _ = RefreshYoloSettingsPanelAsync();
                if (!TryHandleCrashRecoveryOnStartup())
                {
                    TryLoadStartupSampleImage();
                }
                SetPythonStatus(OpenVisionLanguageService.T("WpfShell.Status.InferenceWaiting"));
                AppendLog("시작 완료. 추론은 사용자가 명시적으로 실행할 때만 시작합니다.");
            }), DispatcherPriority.ApplicationIdle);
        }

        // The language event is subscribed and released with the Window. Its
        // fan-out is therefore part of this lifecycle adapter rather than a
        // second Shell partial or a speculative localization service.
        private void LanguageViewModel_LanguageChanged(object sender, EventArgs e)
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.DataBind,
                    new Action(() => LanguageViewModel_LanguageChanged(sender, e)));
                return;
            }

            ShellViewModel?.RefreshLocalizedPresentation();
            ClassCatalogViewModel?.RefreshLocalizedPresentation();
            ImageQueueViewModel?.RefreshLocalizedPresentation(imageQueueItems);
            StatusBarViewModel?.RefreshLocalizedPresentation();
            CanvasPanelControl?.RefreshLocalizedViewerStatus();
            if (ImageQueueFilterBox?.ItemsSource is IEnumerable<WpfImageQueueFilterOption> filterOptions)
            {
                foreach (WpfImageQueueFilterOption filterOption in filterOptions)
                {
                    filterOption?.RefreshLocalizedPresentation();
                }
            }
            RefreshShellDatasetContext();
            UpdateImageQueueStatusText();
            UpdateYoloCommandButtons();
            WpfLocalizationTextRuntimeService.RefreshAll();
            CanvasPanelControl?.RefreshLocalizedViewerStatus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!IsLoaded)
            {
                isApplicationCloseApproved = true;
                base.OnClosing(e);
                return;
            }

            if (!isApplicationCloseApproved && !TryApproveApplicationClose())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        private bool TryApproveApplicationClose()
        {
            if (isApplicationClosePromptOpen)
            {
                return false;
            }

            WpfApplicationClosePlan plan = BuildApplicationClosePlan();
            if (!plan.RequiresPrompt)
            {
                isApplicationCloseApproved = true;
                return true;
            }

            isApplicationClosePromptOpen = true;
            try
            {
                WpfApplicationCloseDecision decision = ShowApplicationClosePrompt(plan);
                return ApplyApplicationCloseDecision(decision);
            }
            finally
            {
                isApplicationClosePromptOpen = false;
            }
        }

        private WpfApplicationClosePlan BuildApplicationClosePlan()
        {
            return applicationClosePolicyService.Build(new WpfApplicationCloseState
            {
                HasUnsavedAnnotations =
                    !string.IsNullOrWhiteSpace(annotationDirtyReason)
                    || HasPendingMaskStrokeCommitWork(),
                UnsavedAnnotationReason = annotationDirtyReason,
                PendingCandidateCount = candidateReviewState.PendingCount,
                ActiveWorkNames = GetActiveApplicationCloseWorkNames(),
                ActiveImagePath = activeImagePath
            });
        }

        private IReadOnlyList<string> GetActiveApplicationCloseWorkNames()
        {
            var names = new List<string>();
            if (isCreatingSmartMask)
            {
                names.Add("Smart Mask 후보 생성");
            }
            if (isDetecting)
            {
                names.Add("현재 이미지 AI 검사");
            }
            if (isBatchDetectionRunning)
            {
                names.Add("일괄 AI 검사");
            }
            if (isExternalYoloDatasetIntakeRunning)
            {
                names.Add("외부 YOLO 데이터셋 확인");
            }
            if (isExternalEvaluationDataAuditRunning)
            {
                names.Add("외부 평가 데이터 대조");
            }
            if (isHistoricalSegmentationRemediationAuditRunning)
            {
                names.Add("SEG 보정 검토");
            }
            if (isTrainingCommandRunning
                || isTrainingWorkflowRunning
                || WpfTrainingProgressPresentationService.IsTrainingStopAvailable(global.GetPythonCommunicationStatusSnapshot()))
            {
                names.Add("모델 학습");
            }
            if (isYoloEnvironmentCommandRunning)
            {
                names.Add("모델 실행환경 설정");
            }
            if (isModelComparisonRunning)
            {
                names.Add("모델 비교");
            }
            if (isSegmentationAdapterComparisonRunning)
            {
                names.Add("세그멘테이션 어댑터 비교");
            }
            if (isAnomalyEvaluationRunning)
            {
                names.Add("이상 분류 평가");
            }

            return names;
        }

        private WpfApplicationCloseDecision ShowApplicationClosePrompt(WpfApplicationClosePlan plan)
        {
            bool canSave = plan.PromptKind == WpfApplicationClosePromptKind.SaveDiscardCancel;
            WpfMessageDialogResult result = WpfMessageDialog.Show(this, new WpfMessageDialogOptions
            {
                Title = plan.Title,
                Message = plan.Message,
                Details = plan.Details,
                Kind = WpfMessageDialogKind.Warning,
                Buttons = canSave
                    ? WpfMessageDialogButtons.YesNoCancel
                    : WpfMessageDialogButtons.OKCancel,
                DefaultResult = WpfMessageDialogResult.Cancel,
                PrimaryButtonText = plan.PrimaryButtonText,
                SecondaryButtonText = plan.SecondaryButtonText,
                TertiaryButtonText = plan.TertiaryButtonText,
                MaxWidth = 620D
            });

            if (canSave)
            {
                return result switch
                {
                    WpfMessageDialogResult.Yes => WpfApplicationCloseDecision.SaveAndClose,
                    WpfMessageDialogResult.No => WpfApplicationCloseDecision.DiscardAndClose,
                    _ => WpfApplicationCloseDecision.Cancel
                };
            }

            return result == WpfMessageDialogResult.OK
                ? WpfApplicationCloseDecision.DiscardAndClose
                : WpfApplicationCloseDecision.Cancel;
        }

        private bool ApplyApplicationCloseDecision(WpfApplicationCloseDecision decision)
        {
            if (decision == WpfApplicationCloseDecision.Cancel)
            {
                return false;
            }

            if (decision == WpfApplicationCloseDecision.SaveAndClose)
            {
                string failureDetails = string.Empty;
                bool saved;
                try
                {
                    saved = SaveCurrentAnnotations(out _);
                }
                catch (Exception ex)
                {
                    saved = false;
                    failureDetails = ex.Message;
                }

                if (!saved)
                {
                    WpfMessageDialog.Show(this, new WpfMessageDialogOptions
                    {
                        Title = "라벨을 저장하지 못했습니다",
                        Message = "현재 이미지의 라벨 저장에 실패하여 창을 닫지 않았습니다.",
                        Details = string.IsNullOrWhiteSpace(failureDetails)
                            ? "데이터셋 출력 경로와 파일 쓰기 권한을 확인한 뒤 다시 시도하세요."
                            : failureDetails,
                        Kind = WpfMessageDialogKind.Warning,
                        Buttons = WpfMessageDialogButtons.OK,
                        PrimaryButtonText = "확인"
                    });
                    return false;
                }
            }

            isApplicationCloseApproved = true;
            return true;
        }

        private void ExecuteClosedCommand()
        {
            // Invalidate queued queue-status workers before disposing the Shell-owned state.
            // A worker may still finish its label scan, but it must not persist or marshal
            // its result after the owning Shell has closed.
            Interlocked.Increment(ref queuedActiveImageQueueStatusRefreshVersion);
            viewModels.LanguageViewModel.LanguageChanged -= LanguageViewModel_LanguageChanged;
            DiscardCrashRecoveryJournal();
            SaveWorkspaceLayoutSettings();
            CloseModelBenchmarkWindow();
            CloseDatasetHealthWindow();
            CloseDatasetInterchangeWindow();
            CloseEnvironmentSetupCenterWindow();
            StopInferenceStatusPulse();
            inferenceStatusPulseTimer.Tick -= InferenceStatusPulseTimer_Tick;
            StopTrainingStatusPolling();
            trainingStatusPollTimer.Tick -= TrainingStatusPollTimer_Tick;
            maskStrokePreviewCommitSwapTimer.Stop();
            maskStrokePreviewCommitSwapTimer.Tick -= MaskStrokePreviewCommitSwapTimer_Tick;
            maskStrokeCommitQueueTimer.Stop();
            maskStrokeCommitQueueTimer.Tick -= MaskStrokeCommitQueueTimer_Tick;
            displayAdjustmentRefreshTimer.Stop();
            displayAdjustmentRefreshTimer.Tick -= DisplayAdjustmentRefreshTimer_Tick;
            imageDecodePreloadService.CancelAndWait(TimeSpan.FromSeconds(2));
            CancelImageQueueCatalogLoad(waitForCompletion: true);
            CancelImageQueueDetailRefresh(waitForCompletion: true);
            projectRecipeSessionCts.Cancel();
            projectRecipeSessionCts.Dispose();
            smartMaskCancellation?.Cancel();
            smartMaskCancellation?.Dispose();
            smartMaskCancellation = null;
            isCreatingSmartMask = false;
            batchDetectionCts?.Cancel();
            batchDetectionCts?.Dispose();
            batchDetectionCts = null;
            isBatchDetectionRunning = false;
            isDetecting = false;
            isExternalYoloDatasetIntakeRunning = false;
            isExternalEvaluationDataAuditRunning = false;
            isHistoricalSegmentationRemediationAuditRunning = false;
            isSegmentationAdapterComparisonRunning = false;
            isModelComparisonRunning = false;
            isAnomalyEvaluationRunning = false;
            isYoloEnvironmentCommandRunning = false;
            isTrainingCommandRunning = false;
            isTrainingWorkflowRunning = false;
            global.StopPythonModelClientConnection();
            imageDecodeCacheService.Clear();
            activeImageBitmap?.Dispose();
            activeImageBitmap = null;
            viewModels.Dispose();
        }
    }
}
