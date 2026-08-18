using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelingApplication.Tests;

using static Program;
using static TestSupport;

internal static class ExeYoloV8DetectRestartSmokeTests
{
    private const string YoloV8DetectDefaultImage = @"D:\LabelingData\Test01\Images\Teaching_0.jpeg";
    private const string YoloV8DetectDefaultRoot = @"C:\Git\yolov8";

    internal static int RunExeYoloV8DetectRestartSmoke(string[] args)
    {
        string recipeName = "codex_yolov8_detect_restart_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        Process firstProcess = null;
        Process restartedProcess = null;
        string recipeDirectory = string.Empty;
        string lastOpenedRecipePath = string.Empty;
        byte[] previousLastOpenedRecipe = null;
        bool hadLastOpenedRecipe = false;

        try
        {
            string root = FindRepositoryRoot();
            string exePath = Path.GetFullPath(GetArgumentValue(
                args,
                "--exe",
                Path.Combine(root, "artifacts", "run", "Debug", "OpenVisionLab.LabelingStudio.exe")));
            string modelEngine = PythonModelSettings.NormalizeModelEngine(GetArgumentValue(args, "--engine", PythonModelSettings.EngineYoloV8));
            bool useYolo11 = string.Equals(modelEngine, PythonModelSettings.EngineYolo11, StringComparison.Ordinal);
            string engineDisplayName = useYolo11 ? "YOLO11" : "YOLOv8";
            string yoloRoot = Path.GetFullPath(GetArgumentValue(args, "--yolo-root", GetArgumentValue(args, "--yolov8-root", YoloV8DetectDefaultRoot)));
            string defaultWeightsPath = useYolo11
                ? Path.Combine(yoloRoot, "yolo11n.pt")
                : Path.Combine(
                    yoloRoot,
                    "runs",
                    "detect",
                    "openvisionlab-yolov8n-detect-test01-e100-img320-20260714",
                    "weights",
                    "best.pt");
            string weightsPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--weights",
                defaultWeightsPath));
            string sourceImagePath = Path.GetFullPath(GetArgumentValue(args, "--image", YoloV8DetectDefaultImage));
            string externalDataYamlPath = GetArgumentValue(args, "--external-data-yaml", string.Empty);
            bool allowEmptyCandidates = args.Any(arg => string.Equals(arg, "--allow-empty-candidates", StringComparison.OrdinalIgnoreCase));
            bool verifySafeClose = args.Any(arg => string.Equals(arg, "--verify-safe-close", StringComparison.OrdinalIgnoreCase));
            bool usePreconfiguredRecipe = args.Any(arg => string.Equals(arg, "--preconfigured-recipe", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(externalDataYamlPath))
            {
                externalDataYamlPath = Path.GetFullPath(externalDataYamlPath);
            }
            string artifactRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(root, "artifacts", "exe-yolov8-detect-restart-smoke", recipeName)));
            string screenshotDirectory = Path.Combine(artifactRoot, "screenshots");
            string inputRoot = Path.Combine(artifactRoot, "input");
            string outputRoot = Path.Combine(artifactRoot, "dataset");
            string smokeImagePath = Path.Combine(inputRoot, Path.GetFileName(sourceImagePath));
            string pythonPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--python-exe",
                Path.Combine(yoloRoot, ".venv", "Scripts", "python.exe")));
            string clientScriptPath = useYolo11
                ? PythonModelRuntimeBundledWorkerService.ResolveUltralyticsWorkerScriptPath()
                : Path.Combine(yoloRoot, "labeling_tcp_client.py");

            AssertTrue(File.Exists(exePath), "YOLOv8 Detect restart smoke EXE was not found: " + exePath);
            AssertTrue(Directory.Exists(yoloRoot), "YOLOv8 root was not found: " + yoloRoot);
            AssertTrue(File.Exists(pythonPath), "YOLOv8 Python was not found: " + pythonPath);
            AssertTrue(File.Exists(clientScriptPath), "YOLOv8 TCP adapter was not found: " + clientScriptPath);
            AssertTrue(File.Exists(weightsPath), "YOLOv8 Detect weights were not found: " + weightsPath);
            AssertTrue(File.Exists(sourceImagePath), "YOLOv8 Detect smoke image was not found: " + sourceImagePath);
            AssertTrue(
                string.IsNullOrWhiteSpace(externalDataYamlPath) || File.Exists(externalDataYamlPath),
                "optional external YOLO data.yaml was not found: " + externalDataYamlPath);

            string exeDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            string recipeRoot = Path.Combine(exeDirectory, "RECIPE");
            recipeDirectory = Path.Combine(recipeRoot, recipeName);
            lastOpenedRecipePath = Path.Combine(recipeRoot, ".last-opened-recipe");
            hadLastOpenedRecipe = File.Exists(lastOpenedRecipePath);
            previousLastOpenedRecipe = hadLastOpenedRecipe ? File.ReadAllBytes(lastOpenedRecipePath) : null;

            DeleteDirectoryIfExists(recipeDirectory);
            DeleteDirectoryIfExists(artifactRoot);
            Directory.CreateDirectory(screenshotDirectory);
            Directory.CreateDirectory(inputRoot);
            File.Copy(sourceImagePath, smokeImagePath, overwrite: true);

            if (usePreconfiguredRecipe)
            {
                PrepareDetectionRestartRecipe(
                    recipeName,
                    recipeDirectory,
                    lastOpenedRecipePath,
                    outputRoot,
                    inputRoot,
                    modelEngine,
                    yoloRoot,
                    pythonPath,
                    clientScriptPath,
                    weightsPath);
            }

            firstProcess = StartYoloV8RuntimeSmokeExe(exePath, out IntPtr firstHandle);
            CaptureWorkflowStep(RefreshAutomationRoot(firstProcess, firstHandle), screenshotDirectory, "01_before_recipe_setup");
            if (usePreconfiguredRecipe)
            {
                AssertTrue(
                    WaitUntil(
                        () => ImageRootAppearsLoaded(
                            RefreshAutomationRoot(firstProcess, firstHandle, bringToFront: false),
                            inputRoot,
                            Path.GetFileNameWithoutExtension(smokeImagePath)),
                        TimeSpan.FromSeconds(20)),
                    "preconfigured YOLOv8 Detect recipe did not load its image queue");
                CaptureWorkflowStep(RefreshAutomationRoot(firstProcess, firstHandle), screenshotDirectory, "01_preconfigured_recipe_loaded");
            }
            else
            {
                CreateDatasetRecipeThroughExe(
                    firstProcess,
                    firstHandle,
                    recipeName,
                    outputRoot,
                    recipeDirectory,
                    screenshotDirectory,
                    "\uAC1D\uCCB4 \uD0D0\uC9C0",
                    LabelingDatasetPurpose.ObjectDetection,
                    "OK, NG");
            }

            string visionPath = Path.Combine(recipeDirectory, "VISION.xml");
            AssertTrue(WaitUntil(() => File.Exists(visionPath), TimeSpan.FromSeconds(8)), "YOLOv8 Detect recipe VISION.xml was not created");
            File.Copy(visionPath, Path.Combine(artifactRoot, "created-before-runtime-VISION.xml"), overwrite: true);
            AssertEqual(LabelingDatasetPurpose.ObjectDetection, ReadRecipeData(visionPath).ProjectSettings.DatasetPurpose);
            CaptureWorkflowStep(RefreshAutomationRoot(firstProcess, firstHandle), screenshotDirectory, "01d_created_dataset_purpose_state");
            File.WriteAllText(
                Path.Combine(artifactRoot, "created-dataset-purpose-visible.txt"),
                GetAutomationValueByAutomationId(
                    RefreshAutomationRoot(firstProcess, firstHandle, bringToFront: false),
                    "CurrentDatasetPurposeText"),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            AssertDatasetPurposeVisibleThroughExe(
                firstProcess,
                firstHandle,
                "\uAC1D\uCCB4 \uD0D0\uC9C0",
                "newly created object-detection recipe did not show object detection as the active dataset purpose");

            if (!usePreconfiguredRecipe)
            {
                ConfigureYoloV8RuntimeThroughExe(
                    firstProcess,
                    firstHandle,
                    inputRoot,
                    yoloRoot,
                    pythonPath,
                    clientScriptPath,
                    weightsPath,
                    screenshotDirectory,
                    confidence: "0.25",
                    timeoutSeconds: "180",
                    inferenceImageSize: "320",
                    modelEngine: modelEngine);
            }

            if (!string.IsNullOrWhiteSpace(externalDataYamlPath))
            {
                SelectAndActivateExternalYoloDatasetThroughExe(
                    firstProcess,
                    firstHandle,
                    externalDataYamlPath,
                    screenshotDirectory);
            }
            if (!usePreconfiguredRecipe)
            {
                LoadConfiguredImageRootThroughExe(firstProcess, firstHandle, inputRoot, screenshotDirectory);
            }

            AssertTrue(WaitUntil(() => File.Exists(visionPath), TimeSpan.FromSeconds(8)), "YOLOv8 Detect recipe VISION.xml was not saved");
            File.Copy(visionPath, Path.Combine(artifactRoot, "saved-before-restart-VISION.xml"), overwrite: true);
            CData savedData = ReadRecipeData(visionPath);
            AssertYoloV8DetectRecipeSettings(savedData, yoloRoot, pythonPath, clientScriptPath, weightsPath, inputRoot, modelEngine, requireRegistry: !usePreconfiguredRecipe);
            AssertExternalYoloDatasetSettings(savedData, externalDataYamlPath);
            string savedVisionHash = TestSupport.ComputeFileSha256(visionPath);
            CaptureWorkflowStep(RefreshAutomationRoot(firstProcess, firstHandle), screenshotDirectory, "02_saved_yolov8_detect_profile");

            CloseExeSmokeProcess(firstProcess);
            firstProcess = null;
            Thread.Sleep(1_000);

            restartedProcess = StartYoloV8RuntimeSmokeExe(exePath, out IntPtr restartedHandle);
            string expectedImageMarker = Path.GetFileNameWithoutExtension(smokeImagePath);
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        var rootElement = RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false);
                        return ContainsAutomationText(rootElement, recipeName)
                            && ImageRootAppearsLoaded(rootElement, inputRoot, expectedImageMarker);
                    },
                    TimeSpan.FromSeconds(20)),
                "restarted EXE did not restore the saved YOLOv8 Detect recipe and image queue");
            CaptureWorkflowStep(RefreshAutomationRoot(restartedProcess, restartedHandle), screenshotDirectory, "03_restarted_recipe_restored");

            AssertTrue(
                string.Equals(File.ReadAllText(lastOpenedRecipePath).Trim(), recipeName, StringComparison.Ordinal),
                "restart marker did not preserve the YOLOv8 Detect smoke recipe");
            File.Copy(visionPath, Path.Combine(artifactRoot, "reopened-before-inference-VISION.xml"), overwrite: true);
            CData reopenedData = ReadRecipeData(visionPath);
            AssertYoloV8DetectRecipeSettings(reopenedData, yoloRoot, pythonPath, clientScriptPath, weightsPath, inputRoot, modelEngine, requireRegistry: !usePreconfiguredRecipe);
            AssertExternalYoloDatasetSettings(reopenedData, externalDataYamlPath);
            string reopenedVisionHash = TestSupport.ComputeFileSha256(visionPath);
            PythonModelRuntimeState persistedRuntimeState = PythonModelSettingsValidator.GetRuntimeState(
                reopenedData.ProjectSettings.PythonModel);
            AssertTrue(
                persistedRuntimeState.CanRunInference,
                "persisted YOLOv8 Detect settings were not inference-ready: "
                    + persistedRuntimeState.SummaryText + " / "
                    + persistedRuntimeState.DetailText + " / "
                    + persistedRuntimeState.NextActionText);
            if (!usePreconfiguredRecipe)
            {
                VerifyYoloV8SettingsVisibleAfterRestart(restartedProcess, restartedHandle, weightsPath, modelEngine);
            }
            var settingsRoot = RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false);
            string settingsRuntimeStatus = string.Join(
                " / ",
                new[]
                {
                    GetAutomationValueByAutomationId(settingsRoot, "YoloModelSettingsSummaryRuntimeStatusText"),
                    GetAutomationValueByAutomationId(settingsRoot, "YoloModelSettingsSummaryRuntimeText"),
                    GetAutomationValueByAutomationId(settingsRoot, "YoloRuntimeExecutionSummaryText"),
                    GetAutomationValueByAutomationId(settingsRoot, "YoloRuntimeExecutionInspectionText")
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            var inferenceRoot = RefreshAutomationRoot(restartedProcess, restartedHandle);
            AssertTrue(
                TryInvokeAutomationButtonByAutomationId(inferenceRoot, "InferenceReviewStageButton")
                    || TryNativeClickAutomationElementByAutomationId(inferenceRoot, "InferenceReviewStageButton"),
                "AI candidate review stage was not selectable after restart");
            Thread.Sleep(500);
            inferenceRoot = RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false);
            CaptureWorkflowStep(inferenceRoot, screenshotDirectory, "03a_ai_candidate_stage");
            string stageStatus = string.Join(
                " / ",
                new[]
                {
                    ReadExeInferenceStatusSnapshot(inferenceRoot),
                    settingsRuntimeStatus,
                    persistedRuntimeState.SummaryText,
                    persistedRuntimeState.DetailText,
                    GetAutomationHelpText(FindAutomationElementByAutomationId(inferenceRoot, "RightWorkflowInferenceInspectButton")),
                    GetAutomationValueByAutomationId(inferenceRoot, "ModelCenterPriorityButtonStateText"),
                    GetAutomationValueByAutomationId(inferenceRoot, "WorkflowStageSummaryTitleText"),
                    GetAutomationValueByAutomationId(inferenceRoot, "WorkflowStageSummaryNextActionText")
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
            File.WriteAllText(
                Path.Combine(artifactRoot, "ai-candidate-stage-status.txt"),
                stageStatus,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            AssertTrue(
                WaitUntil(
                    () => IsAutomationButtonEnabledByAutomationId(
                        RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false),
                        "RightWorkflowInferenceInspectButton"),
                    TimeSpan.FromSeconds(8)),
                "current-image inference did not become available in the AI candidate stage: " + stageStatus);
            inferenceRoot = RefreshAutomationRoot(restartedProcess, restartedHandle);
            bool invoked = TryInvokeAutomationButtonByAutomationId(inferenceRoot, "RightWorkflowInferenceInspectButton")
                || TryInvokeAutomationButtonByAutomationId(inferenceRoot, "WorkflowStageInspectCurrentImageButton")
                || TryInvokeAutomationButtonByAutomationId(inferenceRoot, "ModelCenterPriorityInspectCurrentButton")
                || TryInvokeAutomationButton(inferenceRoot, "\uD604\uC7AC \uAC80\uC0AC");
            AssertTrue(invoked, "current-image inference was not invokable after the YOLOv8 Detect restart");

            string inferenceStatus = string.Empty;
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        var latestRoot = RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false);
                        inferenceStatus = ReadExeInferenceStatusSnapshot(latestRoot);
                        return IsExeTrainedInferenceFinished(latestRoot, inferenceStatus);
                    },
                    TimeSpan.FromMinutes(4)),
                "first YOLOv8 Detect inference did not finish after restart");
            AssertTrue(!IsExeTrainedInferenceFailure(inferenceStatus), "first YOLOv8 Detect inference failed after restart: " + inferenceStatus);
            CaptureWorkflowStep(
                RefreshAutomationRoot(restartedProcess, restartedHandle),
                screenshotDirectory,
                "04_first_inference_after_restart",
                settleMilliseconds: 5_500);
            AssertTrue(inferenceStatus.Contains(engineDisplayName, StringComparison.OrdinalIgnoreCase), "inference status did not identify " + engineDisplayName + ": " + inferenceStatus);
            string weightsDirectory = Path.GetDirectoryName(weightsPath) ?? string.Empty;
            string expectedWeightsRunName = Path.GetFileName(weightsDirectory);
            if (string.Equals(expectedWeightsRunName, "weights", StringComparison.OrdinalIgnoreCase))
            {
                expectedWeightsRunName = Path.GetFileName(Path.GetDirectoryName(weightsDirectory) ?? string.Empty);
            }
            AssertTrue(
                !string.IsNullOrWhiteSpace(expectedWeightsRunName)
                    && inferenceStatus.Contains(expectedWeightsRunName, StringComparison.OrdinalIgnoreCase),
                "inference status did not identify the saved YOLOv8 Detect best.pt: " + inferenceStatus);
            AssertTrue(inferenceStatus.Contains("best.pt", StringComparison.OrdinalIgnoreCase), "inference status did not identify the saved YOLOv8 Detect weights file: " + inferenceStatus);
            AssertTrue(inferenceStatus.Contains("\uD6C4\uBCF4", StringComparison.Ordinal), "inference status did not report a candidate count: " + inferenceStatus);
            if (!allowEmptyCandidates)
            {
                AssertTrue(!inferenceStatus.Contains("\uD6C4\uBCF4 0", StringComparison.Ordinal), "YOLOv8 Detect smoke returned no UI-threshold candidates: " + inferenceStatus);
            }

            File.Copy(visionPath, Path.Combine(artifactRoot, "reopened-after-inference-VISION.xml"), overwrite: true);
            CData inferredData = ReadRecipeData(visionPath);
            AssertYoloV8DetectRecipeSettings(inferredData, yoloRoot, pythonPath, clientScriptPath, weightsPath, inputRoot, modelEngine, requireRegistry: !usePreconfiguredRecipe);
            string inferredVisionHash = TestSupport.ComputeFileSha256(visionPath);
            if (verifySafeClose)
            {
                AssertTrue(
                    !allowEmptyCandidates && !inferenceStatus.Contains("\uD6C4\uBCF4 0", StringComparison.Ordinal),
                    "candidate-only safe-close verification requires at least one pending candidate");
                string smokeStem = Path.GetFileNameWithoutExtension(smokeImagePath);
                bool HasSavedCandidateLabel()
                    => Directory.Exists(outputRoot)
                        && Directory.EnumerateFiles(
                                outputRoot,
                                smokeStem + ".txt",
                                SearchOption.AllDirectories)
                            .Any();
                AssertTrue(
                    !HasSavedCandidateLabel(),
                    "pending candidate should not have a label file before application close");

                Task<IntPtr> cancelCloseRequest = Task.Run(
                    () => SendMessage(restartedHandle, WmClose, IntPtr.Zero, IntPtr.Zero));
                System.Windows.Automation.AutomationElement candidateCloseDialog = null;
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            candidateCloseDialog = FindProcessWindowByName(
                                restartedProcess,
                                "\uD655\uC778\uB418\uC9C0 \uC54A\uC740 \uC791\uC5C5\uC774 \uC788\uC2B5\uB2C8\uB2E4");
                            return candidateCloseDialog != null;
                        },
                        TimeSpan.FromSeconds(5)),
                    "actual EXE candidate-only close did not open discard/cancel");
                CaptureWorkflowStep(
                    RefreshAutomationRoot(restartedProcess, restartedHandle, bringToFront: false),
                    screenshotDirectory,
                    "05_candidate_only_safe_close");
                AssertTrue(
                    FindVisibleAutomationElementByName(
                        candidateCloseDialog,
                        "\uC800\uC7A5 \uD6C4 \uC885\uB8CC",
                        maximumWidth: 240D,
                        maximumHeight: 80D) == null,
                    "candidate-only close must not offer save-and-close");
                AssertTrue(
                    TryInvokeAutomationButton(candidateCloseDialog, "\uACC4\uC18D \uC791\uC5C5"),
                    "candidate-only cancel was not invokable");
                AssertTrue(
                    cancelCloseRequest.Wait(TimeSpan.FromSeconds(3)),
                    "candidate-only cancel did not release the close request");
                AssertTrue(
                    !restartedProcess.HasExited && !HasSavedCandidateLabel(),
                    "candidate-only cancel must keep the EXE open without writing a label");

                Task<IntPtr> discardCloseRequest = Task.Run(
                    () => SendMessage(restartedHandle, WmClose, IntPtr.Zero, IntPtr.Zero));
                System.Windows.Automation.AutomationElement discardDialog = null;
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            discardDialog = FindProcessWindowByName(
                                restartedProcess,
                                "\uD655\uC778\uB418\uC9C0 \uC54A\uC740 \uC791\uC5C5\uC774 \uC788\uC2B5\uB2C8\uB2E4");
                            return discardDialog != null;
                        },
                        TimeSpan.FromSeconds(5)),
                    "actual EXE candidate-only discard did not reopen safe close");
                AssertTrue(
                    TryInvokeAutomationButton(discardDialog, "\uD3D0\uAE30\uD558\uACE0 \uC885\uB8CC"),
                    "candidate-only discard-and-close was not invokable");
                AssertTrue(
                    WaitUntil(() => restartedProcess.HasExited, TimeSpan.FromSeconds(8)),
                    "candidate-only discard did not close the actual EXE");
                AssertTrue(
                    discardCloseRequest.Wait(TimeSpan.FromSeconds(3)),
                    "candidate-only discard did not release the close request");
                AssertTrue(
                    !HasSavedCandidateLabel(),
                    "candidate-only discard must not write or auto-confirm the pending candidate");
            }

            string summaryPath = Path.Combine(artifactRoot, "summary.txt");
            File.WriteAllLines(summaryPath, new[]
            {
                "EXE YOLOv8 Detect restart smoke passed.",
                "recipe=" + recipeName,
                "sourceImage=" + sourceImagePath,
                "smokeImage=" + smokeImagePath,
                "weights=" + weightsPath,
                "weightsSha256=" + TestSupport.ComputeFileSha256(weightsPath),
                "savedVisionSha256=" + savedVisionHash,
                "reopenedVisionSha256=" + reopenedVisionHash,
                "inferredVisionSha256=" + inferredVisionHash,
                "engine=" + reopenedData.ProjectSettings.PythonModel.ModelEngine,
                "confidence=" + reopenedData.ProjectSettings.PythonModel.MinimumDetectionConfidence.ToString(CultureInfo.InvariantCulture),
                "inferenceImageSize=" + reopenedData.ProjectSettings.PythonModel.InferenceImageSize.ToString(CultureInfo.InvariantCulture),
                "allowEmptyCandidates=" + allowEmptyCandidates.ToString(CultureInfo.InvariantCulture),
                "safeCloseVerified=" + verifySafeClose.ToString(CultureInfo.InvariantCulture),
                "inferenceStatus=" + inferenceStatus,
                "screenshots=" + screenshotDirectory
            }, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE recipe=" + recipeName);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE weights=" + weightsPath);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE savedVisionSha256=" + savedVisionHash);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE reopenedVisionSha256=" + reopenedVisionHash);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE inferredVisionSha256=" + inferredVisionHash);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE inferenceStatus=" + inferenceStatus);
            Console.WriteLine("EXE_YOLOV8_DETECT_RESTART_SMOKE summary=" + summaryPath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL EXE YOLOv8 Detect restart smoke: " + ex.Message);
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            CloseExeSmokeProcess(restartedProcess);
            CloseExeSmokeProcess(firstProcess);
            RestoreLastOpenedRecipe(lastOpenedRecipePath, hadLastOpenedRecipe, previousLastOpenedRecipe);
            DeleteDirectoryIfExists(recipeDirectory);
        }
    }

    private static void AssertDatasetPurposeVisibleThroughExe(
        Process process,
        IntPtr stableHandle,
        string expectedPurpose,
        string failureMessage)
    {
        string visiblePurpose = string.Empty;
        AssertTrue(
            WaitUntil(
                () =>
                {
                    var root = RefreshAutomationRoot(process, stableHandle, bringToFront: false);
                    visiblePurpose = GetAutomationValueByAutomationId(root, "CurrentDatasetPurposeText");
                    return visiblePurpose.Contains(expectedPurpose, StringComparison.Ordinal);
                },
                TimeSpan.FromSeconds(5)),
            failureMessage + ": " + visiblePurpose);
    }

    private static void SelectAndActivateExternalYoloDatasetThroughExe(
        Process process,
        IntPtr stableHandle,
        string dataYamlPath,
        string screenshotDirectory)
    {
        YoloExternalDatasetIntakeReport expectedReport = YoloExternalDatasetIntakeService.Build(
            dataYamlPath,
            LabelingDatasetPurpose.ObjectDetection);
        AssertTrue(expectedReport.IsReady, "provided external data.yaml was not ready for Object Detection: " + string.Join(" / ", expectedReport.Errors));
        string expectedClassText = string.Join(", ", expectedReport.ClassNames);

        AssertTrue(OpenYoloModelCenterThroughExe(process, stableHandle), "model center was not selectable for external data.yaml intake");
        var root = RefreshAutomationRoot(process, stableHandle);
        AssertTrue(
            SelectAutomationTabByAutomationId(root, "YoloModelCenterDataTaskTab"),
            "model center data tab was not selectable for external data.yaml intake");
        Thread.Sleep(300);
        root = RefreshAutomationRoot(process, stableHandle, bringToFront: false);
        _ = TryExpandAutomationElementByAutomationId(root, "YoloDatasetReadinessQuickPanel");
        Thread.Sleep(300);
        root = RefreshAutomationRoot(process, stableHandle);
        AssertTrue(
            FindAutomationElementByAutomationId(root, "YoloExternalYoloDatasetSelectButton") != null,
            "external YOLO data.yaml selection was not reachable from the model center data tab");
        CaptureWorkflowStep(root, screenshotDirectory, "02a_external_yolo_data_before_select");

        AssertTrue(
            TryInvokeAutomationButtonByAutomationId(root, "YoloExternalYoloDatasetSelectButton"),
            "external YOLO data.yaml select button was not invokable");
        ChooseExternalYoloDataYamlFile(process, dataYamlPath);

        string statusText = string.Empty;
        string detailText = string.Empty;
        AssertTrue(
            WaitUntil(
                () =>
                {
                    var latestRoot = RefreshAutomationRoot(process, stableHandle, bringToFront: false);
                    statusText = GetAutomationValueByAutomationId(latestRoot, "YoloExternalYoloDatasetStatusText");
                    detailText = GetAutomationValueByAutomationId(latestRoot, "YoloExternalYoloDatasetDetailText");
                    return statusText.Contains("검증됨", StringComparison.Ordinal)
                        && detailText.Contains("외부 학습 클래스: " + expectedClassText, StringComparison.Ordinal)
                        && detailText.Contains("레시피 클래스는 자동으로 바꾸지 않음", StringComparison.Ordinal);
                },
                TimeSpan.FromSeconds(20)),
            "external YOLO data.yaml did not show its validated native class list: " + statusText + " / " + detailText);

        root = RefreshAutomationRoot(process, stableHandle);
        AssertTrue(
            TryInvokeAutomationButtonByAutomationId(root, "YoloExternalYoloDatasetActivateButton"),
            "external YOLO data.yaml activation button was not invokable");
        AssertTrue(
            WaitUntil(
                () =>
                {
                    var latestRoot = RefreshAutomationRoot(process, stableHandle, bringToFront: false);
                    statusText = GetAutomationValueByAutomationId(latestRoot, "YoloExternalYoloDatasetStatusText");
                    detailText = GetAutomationValueByAutomationId(latestRoot, "YoloExternalYoloDatasetDetailText");
                    return statusText.Contains("다음 학습에 사용", StringComparison.Ordinal)
                        && detailText.Contains("외부 학습 클래스: " + expectedClassText, StringComparison.Ordinal);
                },
                TimeSpan.FromSeconds(20)),
            "external YOLO data.yaml did not activate for the next training run: " + statusText + " / " + detailText);
        CaptureWorkflowStep(RefreshAutomationRoot(process, stableHandle), screenshotDirectory, "02b_external_yolo_data_activated");
    }

    private static void ChooseExternalYoloDataYamlFile(Process process, string dataYamlPath)
    {
        const string dialogTitle = "외부 YOLO data.yaml 선택";
        var dialog = WaitForProcessWindowByName(process, dialogTitle, TimeSpan.FromSeconds(8));
        AssertTrue(dialog != null, "external YOLO data.yaml file dialog did not appear");
        BringNativeWindowToFront(new IntPtr(dialog.Current.NativeWindowHandle));
        Thread.Sleep(200);

        // The common file dialog exposes its file-name editor as 1148. Prefer
        // that stable control over the address bar because an address-bar Enter
        // can navigate to the folder without confirming data.yaml.
        if (TrySetAutomationValueByAutomationId(dialog, "1148", dataYamlPath))
        {
            _ = TryInvokeAutomationButtonByAutomationId(dialog, "1");
            Thread.Sleep(800);
            return;
        }

        Clipboard.SetText(dataYamlPath);
        SendKeys.SendWait("%d");
        Thread.Sleep(120);
        SendKeys.SendWait("^v");
        Thread.Sleep(120);
        SendKeys.SendWait("{ENTER}");
        Thread.Sleep(800);

        dialog = FindProcessWindowByName(process, dialogTitle);
        if (dialog != null && !TryInvokeAutomationButton(dialog, "열기") && !TryInvokeAutomationButton(dialog, "Open"))
        {
            SendKeys.SendWait("{ENTER}");
        }
    }

    private static void AssertExternalYoloDatasetSettings(CData data, string expectedDataYamlPath)
    {
        if (string.IsNullOrWhiteSpace(expectedDataYamlPath))
        {
            return;
        }

        ExternalYoloDatasetSettings settings = data.ProjectSettings.ExternalYoloDataset;
        AssertPathEqual(expectedDataYamlPath, settings.DataYamlFilePath, "saved external data.yaml path mismatch");
        AssertEqual(LabelingDatasetPurpose.ObjectDetection, settings.DatasetPurpose);
        AssertTrue(settings.UseForTraining, "validated external data.yaml should remain explicitly active for the next training run");
        AssertTrue(settings.LastValidationSucceeded, "validated external data.yaml readiness snapshot should persist");
        AssertTrue(!string.IsNullOrWhiteSpace(settings.LastValidationClassNames), "validated external data.yaml class list should persist");
    }

    private static void AssertYoloV8DetectRecipeSettings(
        CData data,
        string yoloRoot,
        string pythonPath,
        string clientScriptPath,
        string weightsPath,
        string imageRoot,
        string expectedEngine = PythonModelSettings.EngineYoloV8,
        bool requireRegistry = true)
    {
        AssertEqual(LabelingDatasetPurpose.ObjectDetection, data.ProjectSettings.DatasetPurpose);
        PythonModelSettings settings = data.ProjectSettings.PythonModel;
        AssertEqual(expectedEngine, settings.ModelEngine);
        AssertPathEqual(yoloRoot, settings.ProjectRootPath, "saved YOLOv8 project root mismatch");
        AssertPathEqual(pythonPath, settings.PythonExecutablePath, "saved YOLOv8 Python mismatch");
        AssertPathEqual(clientScriptPath, settings.ClientScriptPath, "saved YOLOv8 client script mismatch");
        AssertPathEqual(weightsPath, settings.WeightsPath, "saved YOLOv8 Detect weights mismatch");
        AssertPathEqual(imageRoot, settings.ImageRootPath, "saved YOLOv8 image root mismatch");
        AssertEqual(320, settings.InferenceImageSize);
        AssertTrue(Math.Abs(settings.MinimumDetectionConfidence - 0.25F) < 0.0001F, "saved YOLOv8 confidence should be 0.25");
        AssertTrue(settings.AutoStartClient, "saved YOLOv8 runtime should auto-start after restart");

        if (!requireRegistry)
        {
            return;
        }

        ModelRegistrySettings registry = data.ProjectSettings.ModelRegistry;
        AssertTrue(
            registry.Profiles.Exists(profile => string.Equals(profile.ModelEngine, expectedEngine, StringComparison.Ordinal)
                && string.Equals(profile.DatasetPurpose, LabelingDatasetPurpose.ObjectDetection.ToString(), StringComparison.Ordinal)),
            "saved YOLOv8 Detect settings should register an ObjectDetection model profile");
        ModelCandidate currentModel = ModelRegistryService.FindCurrentInspectionModel(registry);
        AssertTrue(currentModel != null, "saved YOLOv8 Detect settings should register the current inspection model");
        AssertPathEqual(weightsPath, currentModel.WeightsPath, "saved YOLOv8 Detect registry weights mismatch");
        AssertEqual(0, registry.TrainingRuns.Count);
    }

    private static void PrepareDetectionRestartRecipe(
        string recipeName,
        string recipeDirectory,
        string lastOpenedRecipePath,
        string outputRoot,
        string imageRoot,
        string modelEngine,
        string yoloRoot,
        string pythonPath,
        string clientScriptPath,
        string weightsPath)
    {
        var data = new CData();
        data.ConfigureOutputRoot(outputRoot);
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
        data.ClassNamedList.Add(new CClassItem { Text = "contamination_spot" });
        data.ClassNamedList.Add(new CClassItem { Text = "scratch_crack" });
        data.ClassNamedList.Add(new CClassItem { Text = "edge_chip" });
        data.ClassNamedList.Add(new CClassItem { Text = "foreign_particle" });
        data.ClassNamedList.Add(new CClassItem { Text = "ring_deformation" });
        PythonModelSettings settings = data.ProjectSettings.PythonModel;
        settings.ModelEngine = modelEngine;
        settings.ProjectRootPath = yoloRoot;
        settings.PythonExecutablePath = pythonPath;
        settings.ClientScriptPath = clientScriptPath;
        settings.WeightsPath = weightsPath;
        settings.ImageRootPath = imageRoot;
        settings.AutoStartClient = true;
        settings.MinimumDetectionConfidence = 0.25F;
        settings.DetectionTimeoutSeconds = 180;
        settings.InferenceImageSize = 320;
        data.SaveYoloDataYaml();

        Directory.CreateDirectory(recipeDirectory);
        SerializeHelper.ToXmlFile(Path.Combine(recipeDirectory, "VISION.xml"), data);
        File.WriteAllText(
            Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName),
            JsonConvert.SerializeObject(LabelingDatasetManifestService.Build(data, recipeName), Formatting.Indented),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.WriteAllText(lastOpenedRecipePath, recipeName, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

}
