using MvcVisionSystem.Yolo;
using MvcVisionSystem._1._Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Training commands are separated from inference commands because their status and cancellation paths differ.
        private void ExecuteRefreshTrainingReadinessCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            SaveTrainingEditorFields();
            RefreshTrainingReadinessPanel(refreshYaml: true);
        }

        private async void ExecuteStartTrainingCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            if (isTrainingWorkflowRunning || WpfTrainingProgressPresentationService.IsTrainingStopAvailable(global.GetPythonCommunicationStatusSnapshot()))
            {
                string alreadyRunningText = WpfTrainingCommandPresentationService.BuildAlreadyRunningStatus();
                SetTrainingReadinessStatus(alreadyRunningText);
                AppendLog(alreadyRunningText);
                UpdateYoloCommandButtons();
                return;
            }

            if (!EnsureModelRuntimeForTraining())
            {
                UpdateYoloCommandButtons();
                return;
            }

            if (!BeginTrainingCommand(WpfTrainingCommandPresentationService.BuildPreparingDatasetStatus()))
            {
                return;
            }

            WpfTrainingRecoveryStatus pendingRecovery = null;
            try
            {
                SaveTrainingEditorFields();
                RefreshTrainingReadinessPanel(refreshYaml: true);
                bool ready = await global.ModelRuntime
                    .EnsurePythonModelClientReadyAsync(
                        WpfYoloRuntimePresentationService.GetWorkerConnectTimeoutMilliseconds(
                            global.Data?.ProjectSettings?.PythonModel?.DetectionTimeoutSeconds ?? 30))
                    .ConfigureAwait(true);
                if (isApplicationCloseApproved)
                {
                    return;
                }

                if (!ready)
                {
                    string readinessText = WpfYoloRuntimePresentationService.BuildPythonWorkerFailureText(
                        global.GetPythonCommunicationStatusSnapshot(),
                        global.ModelRuntime.PythonClientProcess?.LastError);
                    SetTrainingReadinessStatus(readinessText);
                    pendingRecovery = WpfTrainingCommandPresentationService.BuildWorkerConnectionFailureRecovery(readinessText);
                    AppendLog(readinessText);
                    return;
                }

                bool started = global.ModelRuntime.TrainingWorkflow.TryStartTraining(
                    global.Data,
                    global.ModelRuntime.DeepLearning,
                    recipeName: GetCurrentRecipeName());
                if (global?.Data?.ProjectSettings?.ExternalYoloDataset?.HasSelection == true)
                {
                    TrySaveExternalYoloDatasetSettings();
                }
                string startText = WpfTrainingCommandPresentationService.BuildStartCommandResultStatus(
                    started,
                    global.ModelRuntime.TrainingWorkflow.LastPreparationFailureMessage);
                SetTrainingReadinessStatus(startText);
                if (!started)
                {
                    pendingRecovery = WpfTrainingCommandPresentationService.BuildStartFailureRecovery(startText);
                }

                AppendLog(startText);
                if (started)
                {
                    isTrainingWorkflowRunning = true;
                    SetTrainingProgressStatus(WpfTrainingCommandPresentationService.BuildTrainingAcceptedProgressText(), string.Empty, 0D, isIndeterminate: true);
                    StartTrainingStatusPolling();
                    UpdateYoloCommandButtons();
                }
            }
            catch (Exception ex)
            {
                if (isApplicationCloseApproved)
                {
                    return;
                }

                string errorText = WpfTrainingCommandPresentationService.BuildStartExceptionStatus(ex.Message);
                SetTrainingReadinessStatus(errorText);
                pendingRecovery = WpfTrainingCommandPresentationService.BuildStartExceptionRecovery(errorText);
                AppendLog(errorText);
            }
            finally
            {
                EndTrainingCommand();
                if (!isApplicationCloseApproved && pendingRecovery != null)
                {
                    SetYoloRecoveryStatus(pendingRecovery.Title, pendingRecovery.Detail, pendingRecovery.Action);
                }
            }
        }

        private async void ExecuteStopTrainingCommand()
        {
            if (!BeginTrainingCommand(WpfTrainingCommandPresentationService.BuildStoppingStatus()))
            {
                return;
            }

            WpfTrainingRecoveryStatus pendingRecovery = null;
            try
            {
                bool stopped = await Task.Run(() => global.ModelRuntime.TrainingWorkflow.TryStopTraining(global.ModelRuntime.DeepLearning)).ConfigureAwait(true);
                if (isApplicationCloseApproved)
                {
                    return;
                }

                string stopText = WpfTrainingCommandPresentationService.BuildStopCommandResultStatus(stopped);
                if (stopped)
                {
                    isTrainingWorkflowRunning = false;
                }

                SetTrainingReadinessStatus(stopText);
                if (!stopped)
                {
                    pendingRecovery = WpfTrainingCommandPresentationService.BuildStopFailureRecovery(stopText);
                }

                AppendLog(stopText);
            }
            catch (Exception ex)
            {
                if (isApplicationCloseApproved)
                {
                    return;
                }

                string errorText = WpfTrainingCommandPresentationService.BuildStopExceptionStatus(ex.Message);
                SetTrainingReadinessStatus(errorText);
                pendingRecovery = WpfTrainingCommandPresentationService.BuildStopExceptionRecovery(errorText);
                AppendLog(errorText);
            }
            finally
            {
                EndTrainingCommand();
                if (!isApplicationCloseApproved && pendingRecovery != null)
                {
                    SetYoloRecoveryStatus(pendingRecovery.Title, pendingRecovery.Detail, pendingRecovery.Action);
                }
            }
        }
    }
}
