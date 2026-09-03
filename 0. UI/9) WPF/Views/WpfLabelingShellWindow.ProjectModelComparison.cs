using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private async void ExecuteRunModelComparisonCommand()
        {
            if (isApplicationCloseApproved || isModelComparisonRunning)
            {
                return;
            }

            EnsureProjectSettings();
            SaveTrainingEditorFields();
            RefreshTrainingReadinessPanel(refreshYaml: true);

            WpfModelComparisonRunRequest request = modelComparisonRunService.BuildRequest(
                global.Data,
                trainingWeightsService,
                task: "test",
                baselineWeightsOverride: GetTrainingComparisonCurrentWeightsPath(global.Data.ProjectSettings.PythonModel.WeightsPath));
            IReadOnlyList<string> validationErrors = modelComparisonRunService.ValidateRequest(request);
            if (validationErrors.Count > 0)
            {
                string message = "\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD589 \uBD88\uAC00: " + string.Join(" / ", validationErrors.Take(3));
                LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                    comparisonText: message,
                    adoptionDecisionText: "\uAD50\uCCB4 \uD310\uB2E8: \uBCF4\uB958 - \uCD5C\uC885 \uAC80\uC99D \uBE44\uAD50 \uBD88\uAC00");
                SetYoloCommandStatus(message, isBusy: false);
                AppendLog(message);
                return;
            }

            isModelComparisonRunning = true;
            UpdateYoloCommandButtons();
            LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                comparisonText: "\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD589 \uC911: \uCD5C\uC885 \uAC80\uC99D \uC774\uBBF8\uC9C0\uB85C \uAE30\uC874 \uBAA8\uB378\uACFC \uC0C8 \uD559\uC2B5 \uBAA8\uB378\uC744 \uBE44\uAD50\uD569\uB2C8\uB2E4.",
                adoptionDecisionText: "\uAD50\uCCB4 \uD310\uB2E8: \uBE44\uAD50 \uC2E4\uD589 \uC911");
            SetYoloCommandStatus("\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD589 \uC911...", isBusy: true);
            AppendLog($"\uBAA8\uB378 \uBE44\uAD50 \uC2DC\uC791: \uAE30\uC874={Path.GetFileName(request.BaselineWeightsPath)}, \uC0C8 \uBAA8\uB378={Path.GetFileName(request.CandidateWeightsPath)}, \uB300\uC0C1={request.Task}");

            try
            {
                WpfModelComparisonRunResult result = await modelComparisonRunService
                    .RunAsync(request)
                    .ConfigureAwait(true);
                if (isApplicationCloseApproved)
                {
                    return;
                }

                if (!result.Succeeded)
                {
                    string errorText = BuildModelComparisonFailureText(result);
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        comparisonText: errorText,
                        adoptionDecisionText: "\uAD50\uCCB4 \uD310\uB2E8: \uBCF4\uB958 - \uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD328");
                    SetYoloCommandStatus(errorText, isBusy: false);
                    AppendLog(errorText);
                    return;
                }

                WpfTrainingWeightsComparison comparison = trainingWeightsService.BuildComparison(
                    global.Data.ProjectSettings.PythonModel.ProjectRootPath,
                    global.Data.OutputRootPath,
                    GetTrainingComparisonCurrentWeightsPath(global.Data.ProjectSettings.PythonModel.WeightsPath));
                UpdateTrainingComparisonViewModel(
                    comparison,
                    WpfTrainingComparisonPresentationService.BuildComparisonStatusText(comparison));
                UpdateCandidateModelComparisonReviewPanel(comparison);

                string summaryName = string.IsNullOrWhiteSpace(result.SummaryPath)
                    ? "comparison-summary.json"
                    : Path.GetFileName(Path.GetDirectoryName(result.SummaryPath) ?? result.SummaryPath);
                string completeText = $"\uBAA8\uB378 \uBE44\uAD50 \uC644\uB8CC: {summaryName}. Candidate Review\uC758 \uBAA8\uB378 \uCC28\uC774 \uC608\uC2DC\uB97C \uD074\uB9AD\uD574 \uC774\uBBF8\uC9C0 \uC704\uCE58\uB97C \uD655\uC778\uD558\uC138\uC694.";
                LearningWorkflowViewModel.SetTrainingComparisonResultTexts(comparisonText: completeText);
                if (!LearningWorkflowViewModel.TrainingModelAdoptionDecisionText.Contains("\uAD50\uCCB4 \uD310\uB2E8:", StringComparison.Ordinal)
                    && !LearningWorkflowViewModel.TrainingModelAdoptionDecisionText.Contains("Adoption decision:", StringComparison.OrdinalIgnoreCase))
                {
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        adoptionDecisionText: "\uAD50\uCCB4 \uD310\uB2E8: \uCC28\uC774 \uC608\uC2DC \uD655\uC778 \uD544\uC694");
                }

                CandidateReviewViewModel?.AddReviewHistory(completeText);
                ShowCandidateReviewWorkflowView();
                SetYoloCommandStatus(completeText, isBusy: false);
                AppendLog(completeText);
            }
            catch (Exception ex)
            {
                if (!isApplicationCloseApproved)
                {
                    string errorText = $"\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD328: {ex.Message}";
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        comparisonText: errorText,
                        adoptionDecisionText: "\uAD50\uCCB4 \uD310\uB2E8: \uBCF4\uB958 - \uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD328");
                    SetYoloCommandStatus(errorText, isBusy: false);
                    AppendLog(errorText);
                }
            }
            finally
            {
                isModelComparisonRunning = false;
                if (!isApplicationCloseApproved)
                {
                    UpdateYoloCommandButtons();
                }
            }
        }

        private async void ExecuteRunYoloEngineComparisonCommand()
        {
            if (isApplicationCloseApproved || isModelComparisonRunning)
            {
                return;
            }

            EnsureProjectSettings();
            SaveTrainingEditorFields();
            RefreshTrainingReadinessPanel(refreshYaml: true);
            if (string.Equals(
                    PythonModelSettings.NormalizeModelEngine(global.Data.ProjectSettings.PythonModel.ModelEngine),
                    PythonModelSettings.EngineUnet,
                    StringComparison.Ordinal))
            {
                ExecuteRunSegmentationAdapterComparisonCommand();
                return;
            }
            if (global.Data.ProjectSettings.DatasetPurpose != LabelingDatasetPurpose.ObjectDetection)
            {
                const string purposeError = "YOLO 엔진 비교는 객체탐지 데이터셋에서만 실행할 수 있습니다.";
                LearningWorkflowViewModel.SetTrainingComparisonResultTexts(comparisonText: purposeError);
                SetYoloCommandStatus(purposeError, isBusy: false);
                AppendLog(purposeError);
                return;
            }

            bool compareYoloV8ToYolo11 = string.Equals(
                PythonModelSettings.NormalizeModelEngine(global.Data.ProjectSettings.PythonModel.ModelEngine),
                PythonModelSettings.EngineYolo11,
                StringComparison.Ordinal);
            WpfModelComparisonRunRequest request = compareYoloV8ToYolo11
                ? modelComparisonRunService.BuildYoloV8Yolo11DetectionRequest(global.Data)
                : modelComparisonRunService.BuildYoloV5YoloV8DetectionRequest(global.Data);
            string enginePair = BuildEnginePairLabel(request);
            string comparisonBasisText = string.Equals(request.Task, "val", StringComparison.OrdinalIgnoreCase)
                ? "학습 검증(val, 교체 판단 아님)"
                : "최종 검증(test)";
            IReadOnlyList<string> validationErrors = modelComparisonRunService.ValidateRequest(request);
            if (validationErrors.Count > 0)
            {
                string message = enginePair + " 분석 실행 불가: " + string.Join(" / ", validationErrors.Take(4));
                LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                    comparisonText: message,
                    adoptionDecisionText: "엔진 비교: 준비 필요");
                SetYoloCommandStatus(message, isBusy: false);
                AppendLog(message);
                return;
            }

            isModelComparisonRunning = true;
            UpdateYoloCommandButtons();
            LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                summaryText: enginePair + " 객체탐지 분석 중",
                comparisonText: $"동일한 {comparisonBasisText} 이미지에서 정확도와 모델 Takt를 측정하는 중입니다.",
                adoptionDecisionText: "엔진 비교: 실행 중");
            SetYoloCommandStatus(enginePair + " 객체탐지 분석 중...", isBusy: true);
            AppendLog($"YOLO 엔진 비교 시작: {enginePair}; baseline={Path.GetFileName(request.BaselineWeightsPath)}, candidate={Path.GetFileName(request.CandidateWeightsPath)}, batch=1, task={request.Task}");

            try
            {
                WpfModelComparisonRunResult result = await modelComparisonRunService
                    .RunAsync(request)
                    .ConfigureAwait(true);
                if (isApplicationCloseApproved)
                {
                    return;
                }
                if (!result.Succeeded)
                {
                    string errorText = BuildModelComparisonFailureText(result);
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        comparisonText: errorText,
                        adoptionDecisionText: "엔진 비교: 실패");
                    SetYoloCommandStatus(errorText, isBusy: false);
                    AppendLog(errorText);
                    return;
                }

                IReadOnlyList<string> classNames = global.Data.ClassNamedList?
                    .Select(item => item?.Text ?? string.Empty)
                    .ToList() ?? new List<string>();
                WpfModelComparisonReviewReport report = modelComparisonReviewService.BuildFromSummaryFile(
                    result.SummaryPath,
                    classNames,
                    request.UiConfidence,
                    maxExamples: 5);
                if (!report.HasComparison)
                {
                    string errorText = enginePair + " 분석 결과를 읽지 못했습니다.";
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        comparisonText: errorText,
                        adoptionDecisionText: "엔진 비교: 결과 확인 필요");
                    SetYoloCommandStatus(errorText, isBusy: false);
                    AppendLog(errorText);
                    return;
                }

                string dataYamlName = Path.GetFileName(request.DataYamlPath);
                string sourceText = $"비교 대상: {enginePair}; baseline={Path.GetFileName(request.BaselineWeightsPath)}, candidate={Path.GetFileName(request.CandidateWeightsPath)} / 데이터: {dataYamlName} / 기준: {comparisonBasisText}";
                WpfModelComparisonHistoryItem historyItem = RefreshModelComparisonHistoryItems(
                    request.BaselineWeightsPath,
                    request.CandidateWeightsPath,
                    result.SummaryPath);
                CandidateReviewViewModel.SetModelComparisonSourceText(sourceText);
                CandidateReviewViewModel.SetModelComparisonReview(
                    report,
                    isHistoricalSelection: historyItem?.IsLatest == false);
                CandidateReviewViewModel.SetModelCandidateDecisionState(false, false, null, null, null, null);
                LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                    summaryText: enginePair + " 객체탐지 분석 완료",
                    comparisonText: string.IsNullOrWhiteSpace(report.BenchmarkText)
                        ? report.DetailText
                        : report.BenchmarkText + Environment.NewLine + report.DetailText,
                    adoptionDecisionText: string.IsNullOrWhiteSpace(report.RecommendationText)
                        ? "엔진 비교: 예시 확인 필요"
                        : report.RecommendationText);

                string completeText = $"{enginePair} 객체탐지 분석 완료 ({comparisonBasisText}): Candidate Review에서 정확도, 모델 Takt, 이미지별 차이를 확인하세요.";
                CandidateReviewViewModel.AddReviewHistory(completeText);
                ShowCandidateReviewWorkflowView();
                SetYoloCommandStatus(completeText, isBusy: false);
                AppendLog(completeText);
            }
            catch (Exception ex)
            {
                if (!isApplicationCloseApproved)
                {
                    string errorText = $"{enginePair} 분석 실패: {ex.Message}";
                    LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                        comparisonText: errorText,
                        adoptionDecisionText: "엔진 비교: 실패");
                    SetYoloCommandStatus(errorText, isBusy: false);
                    AppendLog(errorText);
                }
            }
            finally
            {
                isModelComparisonRunning = false;
                if (!isApplicationCloseApproved)
                {
                    UpdateYoloCommandButtons();
                }
            }
        }

        private static string BuildEnginePairLabel(WpfModelComparisonRunRequest request)
        {
            return PythonModelSettings.FormatModelEngineName(request?.BaselineModelEngine)
                + " vs "
                + PythonModelSettings.FormatModelEngineName(request?.CandidateModelEngine);
        }

        private static string BuildModelComparisonFailureText(WpfModelComparisonRunResult result)
        {
            string detail = result?.Error ?? string.Empty;
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = result?.Output ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                return "\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD328: \uC2E4\uD589 \uACB0\uACFC\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
            }

            string firstLine = detail
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? detail.Trim();
            return $"\uBAA8\uB378 \uBE44\uAD50 \uC2E4\uD328: {firstLine}";
        }

        // Historical comparison loading is part of the same model-comparison
        // call path, so its review-history projection stays with this owner.
        private WpfModelComparisonHistoryItem RefreshModelComparisonHistoryItems(
            string baselineWeightsPath,
            string candidateWeightsPath,
            string preferredSummaryPath = "")
        {
            if (string.IsNullOrWhiteSpace(baselineWeightsPath)
                || string.IsNullOrWhiteSpace(candidateWeightsPath))
            {
                CandidateReviewViewModel.SetModelComparisonHistory(Array.Empty<WpfModelComparisonHistoryItem>());
                return null;
            }

            IReadOnlyList<WpfModelComparisonHistoryItem> items = modelComparisonReviewService.BuildHistory(
                baselineWeightsPath,
                candidateWeightsPath,
                maxItems: 8);
            CandidateReviewViewModel.SetModelComparisonHistory(items, preferredSummaryPath);
            return CandidateReviewViewModel.SelectedModelComparisonHistoryItem;
        }

        private WpfModelComparisonReviewReport BuildModelComparisonHistoryReport(
            WpfModelComparisonHistoryItem item)
        {
            if (item == null)
            {
                return WpfModelComparisonReviewReport.Empty;
            }

            IReadOnlyList<string> classNames = global.Data?.ClassNamedList == null
                ? Array.Empty<string>()
                : global.Data.ClassNamedList
                    .Select(classItem => classItem?.Text ?? string.Empty)
                    .ToList();
            double confidence = global.Data?.ProjectSettings?.PythonModel?.MinimumDetectionConfidence ?? 0.25D;
            return modelComparisonReviewService.BuildFromSummaryFile(
                item.SourcePath,
                classNames,
                confidence,
                maxExamples: 5);
        }

        private void ExecuteModelComparisonHistorySelectionChangedCommand(object selectedItem)
        {
            if (selectedItem is not WpfModelComparisonHistoryItem item)
            {
                return;
            }

            WpfModelComparisonReviewReport report = BuildModelComparisonHistoryReport(item);
            if (!report.HasComparison)
            {
                AppendLog($"\uBAA8\uB378 \uBE44\uAD50 \uC774\uB825 \uBD88\uB7EC\uC624\uAE30 \uC2E4\uD328: {item.SourcePath}");
                return;
            }

            CandidateReviewViewModel.SetModelComparisonSourceText(
                $"{item.DisplayText} / {item.DetailText} / {item.SourcePath}");
            CandidateReviewViewModel.SetModelComparisonReview(
                report,
                isHistoricalSelection: !item.IsLatest);
            try
            {
                RefreshModelCenterDashboard(BuildCurrentTrainingWeightsComparison());
            }
            catch (Exception ex)
            {
                AppendLog($"\uBAA8\uB378 \uBE44\uAD50 \uC774\uB825 \uD310\uB2E8 \uAC31\uC2E0 \uC2E4\uD328: {ex.Message}");
            }

            AppendLog($"\uBAA8\uB378 \uBE44\uAD50 \uC774\uB825 \uC120\uD0DD: {item.DisplayText}");
        }
    }
}
