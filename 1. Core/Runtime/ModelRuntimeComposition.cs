using MvcVisionSystem._3._Communication.TCP;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MvcVisionSystem._1._Core
{
    /// <summary>
    /// Owns model-only workflow, Python process, and TCP lifecycle state.
    /// The labeling composition can inspect status without creating this owner.
    /// </summary>
    public sealed class ModelRuntimeComposition
    {
        private readonly Func<LabelingProjectData> dataAccessor;
        private readonly Action<LabelingProjectData> dataSetter;
        private readonly Action<IReadOnlyList<DefectInfo>, string, string> detectionResultSink;
        private readonly Lazy<YoloDetectionWorkflowService> detectionWorkflow =
            new Lazy<YoloDetectionWorkflowService>(() => new YoloDetectionWorkflowService());
        private readonly Lazy<YoloTrainingWorkflowService> trainingWorkflow =
            new Lazy<YoloTrainingWorkflowService>(() => new YoloTrainingWorkflowService());
        private readonly Lazy<YoloPythonClientProcessService> pythonClientProcess =
            new Lazy<YoloPythonClientProcessService>(() => new YoloPythonClientProcessService());
        private Lazy<PythonModelCommunication> deepLearning;

        public ModelRuntimeComposition(
            Func<LabelingProjectData> dataAccessor,
            Action<LabelingProjectData> dataSetter,
            Action<IReadOnlyList<DefectInfo>, string, string> detectionResultSink = null)
        {
            this.dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this.dataSetter = dataSetter ?? throw new ArgumentNullException(nameof(dataSetter));
            this.detectionResultSink = detectionResultSink;
            deepLearning = new Lazy<PythonModelCommunication>(() => CreateCommunication());
        }

        public YoloDetectionWorkflowService DetectionWorkflow => detectionWorkflow.Value;

        public YoloTrainingWorkflowService TrainingWorkflow => trainingWorkflow.Value;

        public YoloPythonClientProcessService PythonClientProcess => pythonClientProcess.Value;

        public PythonModelCommunication DeepLearning => deepLearning.Value;

        public bool IsPythonClientProcessCreated => pythonClientProcess.IsValueCreated;

        public bool IsCommunicationCreated => deepLearning.IsValueCreated;

        public PythonCommunicationStatus GetPythonCommunicationStatusSnapshot()
        {
            return deepLearning.IsValueCreated
                ? deepLearning.Value.GetStatusSnapshot()
                : new PythonCommunicationStatus();
        }

        public bool EnsurePythonModelClientStarted()
        {
            LabelingProjectData data = EnsureData();
            PythonModelSettings settings = data.ProjectSettings.PythonModel;
            if (!settings.AutoStartClient)
            {
                return true;
            }

            return PythonClientProcess.EnsureStarted(settings);
        }

        public bool StartPythonModelClientConnection(int timeoutMilliseconds = 5000)
        {
            DeepLearning.Start();
            return EnsurePythonModelClientReady(timeoutMilliseconds);
        }

        public Task<bool> StartPythonModelClientConnectionAsync(int timeoutMilliseconds = 5000)
        {
            return Task.Run(() => StartPythonModelClientConnection(timeoutMilliseconds));
        }

        public void StopPythonModelClientConnection()
        {
            if (deepLearning.IsValueCreated)
            {
                deepLearning.Value.Close();
            }

            if (pythonClientProcess.IsValueCreated)
            {
                pythonClientProcess.Value.Stop();
            }
        }

        public Task StopPythonModelClientConnectionAsync()
        {
            return Task.Run(StopPythonModelClientConnection);
        }

        public bool RestartPythonModelClientConnection(int timeoutMilliseconds = 5000)
        {
            StopPythonModelClientConnection();
            return StartPythonModelClientConnection(timeoutMilliseconds);
        }

        public Task<bool> RestartPythonModelClientConnectionAsync(int timeoutMilliseconds = 5000)
        {
            return Task.Run(() => RestartPythonModelClientConnection(timeoutMilliseconds));
        }

        public bool EnsurePythonModelClientReady(int timeoutMilliseconds = 5000)
        {
            LabelingProjectData data = EnsureData();
            DeepLearning.Start();

            bool autoStartClient = data.ProjectSettings.PythonModel.AutoStartClient;
            if (autoStartClient && !EnsurePythonModelClientStarted())
            {
                return false;
            }

            DateTime? requiredConnectionUtc = autoStartClient ? PythonClientProcess.LastStartedAtUtc : null;
            int safeTimeoutMilliseconds = Math.Max(0, timeoutMilliseconds);
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(safeTimeoutMilliseconds);
            string pendingStatusRequestId = "";
            DateTime? probedConnectionUtc = null;
            while (DateTime.UtcNow <= deadline)
            {
                PythonCommunicationStatus status = GetPythonCommunicationStatusSnapshot();
                bool connectedAfterClientStart = !requiredConnectionUtc.HasValue
                    || (status.LastConnectedAtUtc.HasValue && status.LastConnectedAtUtc.Value >= requiredConnectionUtc.Value);
                if (status.IsClientConnected && connectedAfterClientStart)
                {
                    if (!string.IsNullOrWhiteSpace(pendingStatusRequestId)
                        && string.Equals(status.LastModelStatusRequestId, pendingStatusRequestId, StringComparison.Ordinal))
                    {
                        if (PythonModelIdentity.Matches(data.ProjectSettings.PythonModel, status.LastModelEngine, status.LastModelWeightsPath))
                        {
                            return true;
                        }

                        string mismatch = $"Connected YOLO worker does not match the current model settings. Expected:{data.ProjectSettings.PythonModel.ModelEngine} / {data.ProjectSettings.PythonModel.WeightsPath}, Actual:{FirstNonEmpty(status.LastModelEngine, "unknown")} / {FirstNonEmpty(status.LastModelWeightsPath, "unknown")}";
                        AppLog.ABNORMAL(mismatch);
                        DeepLearning.DropActiveClient(mismatch);
                        pendingStatusRequestId = "";
                        probedConnectionUtc = null;
                        Thread.Sleep(100);
                        continue;
                    }

                    if (probedConnectionUtc != status.LastConnectedAtUtc)
                    {
                        pendingStatusRequestId = Guid.NewGuid().ToString("N");
                        if (DeepLearning.SendModelStatus(pendingStatusRequestId, ensureLoaded: false))
                        {
                            probedConnectionUtc = status.LastConnectedAtUtc;
                        }
                        else
                        {
                            pendingStatusRequestId = "";
                        }
                    }
                }

                Thread.Sleep(100);
            }

            PythonCommunicationStatus finalStatus = GetPythonCommunicationStatusSnapshot();
            string error = FirstNonEmpty(finalStatus.LastError, PythonClientProcess.LastError, "none");
            string message = $"YOLO Python client did not connect with the configured engine and weights within {safeTimeoutMilliseconds}ms. Listener:{finalStatus.IsListening}, Client:{finalStatus.IsClientConnected}, ProcessRunning:{PythonClientProcess.IsRunning}, Error:{error}";
            DeepLearning.SetLastError(message);
            AppLog.ABNORMAL(message);
            return false;
        }

        public Task<bool> EnsurePythonModelClientReadyAsync(int timeoutMilliseconds = 5000)
        {
            return Task.Run(() => EnsurePythonModelClientReady(timeoutMilliseconds));
        }

        public void SetDeepLearning(PythonModelCommunication communication)
        {
            deepLearning = communication == null
                ? new Lazy<PythonModelCommunication>(CreateCommunication)
                : new Lazy<PythonModelCommunication>(() => ConfigureCommunication(communication));
        }

        private PythonModelCommunication CreateCommunication()
        {
            return ConfigureCommunication(new PythonModelCommunication());
        }

        private PythonModelCommunication ConfigureCommunication(PythonModelCommunication communication)
        {
            communication?.SetDetectionResultSink(detectionResultSink);
            return communication;
        }

        private LabelingProjectData EnsureData()
        {
            LabelingProjectData data = dataAccessor();
            if (data == null)
            {
                data = new LabelingProjectData();
                dataSetter(data);
            }

            data.ProjectSettings ??= new LabelingProjectSettings();
            PythonModelRuntimePathResolver.ApplyDefaults(data.ProjectSettings);
            return data;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return "";
        }
    }
}
