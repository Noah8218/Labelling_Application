using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Detection execution is kept apart from panel wiring so worker latency, fallback, and canvas update paths can be audited in one place.
        private async Task RunInteractiveDetectionAsync(string imagePath = "", bool allowSmokeFallback = false)
        {
            if (isApplicationCloseApproved || isDetecting || isBatchDetectionRunning)
            {
                return;
            }

            EnsureProjectSettings();
            isDetecting = true;
            UpdateYoloCommandButtons();
            UpdateCandidateActionState();
            SetYoloCommandStatus(WpfInferenceStatusPresentationService.BuildInteractivePreparingCommandStatus(), isBusy: true);
            SetGlobalInferenceStatus(WpfInferenceStatusPresentationService.BuildInteractivePreparingInferenceStatus(), isBusy: true);
            SetPythonStatus("\uCD94\uB860: \uC900\uBE44 \uC911");
            var totalStopwatch = Stopwatch.StartNew();
            try
            {
                string targetImagePath = detectionTargetService.ResolveInteractiveTargetPath(
                    imagePath,
                    activeImagePath,
                    global.Data.ProjectSettings.PythonModel);
                string inferencePath = "worker";
                YoloWorkerSmokeTestResult result = await RunWorkerDetectionForImageAsync(
                        targetImagePath,
                        applyToCanvas: true,
                        CancellationToken.None,
                        WpfYoloRuntimePresentationService.GetInteractiveWorkerConnectTimeoutMilliseconds(
                            global.Data?.ProjectSettings?.PythonModel?.DetectionTimeoutSeconds ?? 30,
                            global.Data?.ProjectSettings?.PythonModel?.AutoStartClient != false,
                            allowSmokeFallback))
                    .ConfigureAwait(true);
                if (isApplicationCloseApproved)
                {
                    return;
                }

                if (!result.Succeeded && allowSmokeFallback)
                {
                    AppendLog($"\uCD94\uB860 \uC2E4\uD328, \uD14C\uC2A4\uD2B8 \uACBD\uB85C\uB85C \uC804\uD658: {Path.GetFileName(targetImagePath)}");
                    inferencePath = "smoke fallback";
                    result = await RunDetectionForImageAsync(targetImagePath, applyToCanvas: true, CancellationToken.None)
                        .ConfigureAwait(true);
                    if (isApplicationCloseApproved)
                    {
                        return;
                    }
                }

                string elapsed = WpfYoloRuntimePresentationService.FormatElapsed(totalStopwatch.Elapsed);
                string inferencePathText = WpfYoloRuntimePresentationService.FormatInferencePath(inferencePath);
                SetYoloCommandStatus(
                    WpfInferenceStatusPresentationService.BuildInteractiveCompletionCommandStatus(result, elapsed),
                    isBusy: false);
                SetGlobalInferenceStatus(
                    WpfInferenceStatusPresentationService.BuildInteractiveCompletionInferenceStatus(result, elapsed),
                    isBusy: false,
                    isWarning: !result.Succeeded);
                AppendLog(WpfInferenceStatusPresentationService.BuildInteractiveCompletionLog(result, elapsed, inferencePathText));
            }
            finally
            {
                isDetecting = false;
                if (!isApplicationCloseApproved)
                {
                    UpdateYoloCommandButtons();
                    UpdateCandidateActionState();
                }
            }
        }

        private static string BuildInteractiveDetectionFailureSummary(YoloWorkerSmokeTestResult result)
        {
            return WpfInferenceStatusPresentationService.BuildInteractiveDetectionFailureSummary(result);
        }

        private async Task<YoloWorkerSmokeTestResult> RunDetectionForImageAsync(
            string imagePath,
            bool applyToCanvas,
            CancellationToken cancellationToken)
        {
            if (isApplicationCloseApproved)
            {
                return new YoloWorkerSmokeTestResult
                {
                    ImagePath = imagePath ?? string.Empty
                };
            }

            var stopwatch = Stopwatch.StartNew();
            EnsureProjectSettings();
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                AppendLog($"검출 이미지 없음: {imagePath}");
                return new YoloWorkerSmokeTestResult
                {
                    Succeeded = false,
                    Summary = "검출 이미지를 찾지 못했습니다.",
                    ImagePath = imagePath ?? string.Empty,
                    Errors = new[] { $"검출 이미지를 찾지 못했습니다: {imagePath}" }
                };
            }

            if (applyToCanvas && !string.Equals(imagePath, activeImagePath, StringComparison.OrdinalIgnoreCase))
            {
                TryLoadImage(imagePath);
            }

            SetPythonStatus("\uCD94\uB860: \uD14C\uC2A4\uD2B8 \uC2E4\uD589 \uC911");
            AppendLog($"\uD14C\uC2A4\uD2B8 \uCD94\uB860 \uC2DC\uC791: {Path.GetFileName(imagePath)}");
            YoloWorkerSmokeTestResult result = await YoloWorkerSmokeTestService
                .RunAsync(global.Data.ProjectSettings.PythonModel, imagePath, cancellationToken)
                .ConfigureAwait(true);
            if (isApplicationCloseApproved)
            {
                return new YoloWorkerSmokeTestResult
                {
                    ImagePath = imagePath ?? string.Empty
                };
            }

            if (applyToCanvas)
            {
                // Keep existing manual labels when smoke detection returns the already-active image;
                // Candidate Review needs those labels to compute duplicate/current-label focus.
                if (!string.IsNullOrWhiteSpace(result.ImagePath)
                    && File.Exists(result.ImagePath)
                    && !string.Equals(result.ImagePath, activeImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    TryLoadImage(result.ImagePath);
                }

                ApplyDetectionCandidates(result.Candidates, result.Succeeded);
                SetPythonStatus(detectionResultPresentationService.BuildSmokeStatus(result));
                foreach (string error in result.Errors)
                {
                    AppendLog($"- {error}");
                }
            }

            AppendLog(result.Summary);
            AppendLog($"\uD14C\uC2A4\uD2B8 \uCD94\uB860 \uC2DC\uAC04: {WpfYoloRuntimePresentationService.FormatElapsed(stopwatch.Elapsed)}");
            return result;
        }

    }
}
