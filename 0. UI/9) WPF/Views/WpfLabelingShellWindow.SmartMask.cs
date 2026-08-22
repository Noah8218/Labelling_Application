using MvcVisionSystem._1._Core;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private async void ExecuteCreateSmartMaskCandidateCommand()
        {
            if (isCreatingSmartMask)
            {
                return;
            }

            bool isStartingSession = !smartMaskPromptSession.HasSession;
            string promptOverlayId = string.Empty;
            Rectangle promptBounds = Rectangle.Empty;
            if (isStartingSession)
            {
                int promptIndex = FindSmartMaskPromptIndex();
                if (promptIndex < 0 || activeImageSize.IsEmpty || string.IsNullOrWhiteSpace(activeImagePath))
                {
                    RefreshSmartMaskCommandState("결함 둘레에 사각형 박스를 먼저 그린 뒤 다시 누르세요.");
                    AppendLog("스마트 마스크: 결함 둘레에 사각형 박스를 먼저 그리세요.");
                    return;
                }

                promptOverlayId = GetManualRoiOverlayId(promptIndex);
                promptBounds = manualRois[promptIndex];
                string className = GetManualRoiClassName(promptIndex);
                int classIdValue = global.Data.ClassNamedList.FindIndex(item =>
                    string.Equals(item?.Text, className, StringComparison.OrdinalIgnoreCase));
                int? classId = classIdValue >= 0 ? classIdValue : null;
                smartMaskPromptSession.Start(
                    activeImagePath,
                    GetCurrentSmartMaskRecipeName(),
                    promptBounds,
                    classId,
                    className);
            }

            WpfSmartMaskPromptSnapshot snapshot = smartMaskPromptSession.Capture();
            WpfMobileSamBoxPromptRequest request = mobileSamBoxPromptService.BuildRequest(
                global.Data.ProjectSettings?.PythonModel,
                snapshot.ImagePath,
                snapshot.PromptBounds,
                snapshot.ClassId,
                snapshot.ClassName,
                snapshot.Points,
                smartMaskPromptSession.MaximumPolygonPoints);
            if (!request.IsValid)
            {
                string error = string.Join(" ", request.Errors);
                if (isStartingSession)
                {
                    ResetSmartMaskPromptSession();
                }
                RefreshSmartMaskCommandState(error);
                AppendLog("스마트 마스크 준비 실패: " + error);
                return;
            }

            isCreatingSmartMask = true;
            if (MainCanvasViewModel != null)
            {
                MainCanvasViewModel.IsTeachingMode = false;
            }
            smartMaskCancellation?.Dispose();
            var cancellation = new CancellationTokenSource();
            smartMaskCancellation = cancellation;
            RefreshSmartMaskCommandState("MobileSAM이 박스와 보정점을 사용해 후보 경계를 계산하고 있습니다.");
            SetYoloCommandStatus("스마트 마스크 후보 생성 중...", isBusy: true);
            WpfMobileSamBoxPromptResult result;
            try
            {
                result = await mobileSamBoxPromptService.RunAsync(request, cancellation.Token);
            }
            finally
            {
                if (ReferenceEquals(smartMaskCancellation, cancellation))
                {
                    smartMaskCancellation = null;
                }
                cancellation.Dispose();
                isCreatingSmartMask = false;
            }

            if (!smartMaskPromptSession.Matches(
                    snapshot,
                    activeImagePath,
                    GetCurrentSmartMaskRecipeName()))
            {
                RefreshSmartMaskCommandState("이미지, 레시피 또는 프롬프트가 변경되어 이전 결과를 적용하지 않았습니다.");
                AppendLog("스마트 마스크 결과 무시: 실행 중 이미지, 레시피 또는 프롬프트가 변경되었습니다.");
                return;
            }
            if (string.Equals(result.ErrorCode, "Canceled", StringComparison.Ordinal))
            {
                RefreshSmartMaskCommandState("후보 생성을 취소했습니다. 프롬프트는 유지되며 다시 실행할 수 있습니다.");
                SetYoloCommandStatus("스마트 마스크 후보 생성 취소", isBusy: false);
                AppendLog("스마트 마스크 후보 생성을 취소했습니다.");
                return;
            }
            if (!result.Succeeded || result.Candidate == null)
            {
                RefreshSmartMaskCommandState(result.Error);
                SetYoloCommandStatus("스마트 마스크 실패: " + result.Error, isBusy: false);
                AppendLog("스마트 마스크 실패: " + result.Error);
                return;
            }

            if (isStartingSession)
            {
                int currentPromptIndex = FindManualRoiIndexByOverlayId(promptOverlayId);
                if (currentPromptIndex < 0 || manualRois[currentPromptIndex] != promptBounds)
                {
                    smartMaskPromptSession.Reset();
                    RefreshSmartMaskCommandState("프롬프트 박스가 변경되어 후보를 적용하지 않았습니다.");
                    AppendLog("스마트 마스크 결과 무시: 프롬프트 박스가 변경되었습니다.");
                    return;
                }

                RegisterAnnotationHistoryBeforeChange("박스를 스마트 마스크 프롬프트로 전환", markDirty: false);
                manualRois.RemoveAt(currentPromptIndex);
                RemoveAtIfPresent(manualRoiClassNames, currentPromptIndex);
                RemoveAtIfPresent(manualRoiShapeKinds, currentPromptIndex);
                RemoveAtIfPresent(manualRoiOverlayIds, currentPromptIndex);
            }
            else
            {
                RegisterAnnotationHistoryBeforeChange("스마트 마스크 후보 다시 생성", markDirty: false);
            }

            // Candidate Review still owns the one visible pending candidate. The Smart Mask
            // session retains only the initial/latest alternatives until the object is resolved.
            ApplyDetectionCandidatesPreservingConfirmed(new[] { result.Candidate }, succeeded: true);
            smartMaskPromptSession.RecordCandidate(result.Candidate);
            RefreshPolygonOverlays();
            SetYoloCommandStatus(result.Summary + " / 확정 전 후보", isBusy: false);
            AppendLog($"{result.Summary} / {result.RuntimeSummary} / mask area {result.MaskArea}");
            RefreshSmartMaskCommandState("자동 후보가 부족할 때 보정 옵션에서 한 점을 추가하고 다시 생성해 비교하세요. 확정 전에는 저장되지 않습니다.");
        }

        private void ExecuteSetSmartMaskPointModeCommand(WpfSmartMaskPointInputMode mode)
        {
            if (!smartMaskPromptSession.HasSession || isCreatingSmartMask)
            {
                return;
            }

            WpfSmartMaskPointInputMode nextMode = smartMaskPromptSession.InputMode == mode
                ? WpfSmartMaskPointInputMode.None
                : mode;
            smartMaskPromptSession.SetInputMode(nextMode);
            if (MainCanvasViewModel != null)
            {
                MainCanvasViewModel.IsTeachingMode = false;
                MainCanvasViewModel.IsImagePointInputMode = nextMode != WpfSmartMaskPointInputMode.None;
                MainCanvasViewModel.ImageViewer.SetViewMode(
                    activeAnnotationTool == WpfAnnotationTool.PanZoom
                        ? CanvasInteractionMode.Drag
                        : CanvasInteractionMode.None);
            }

            RefreshSmartMaskCommandState(nextMode == WpfSmartMaskPointInputMode.Positive
                ? "캔버스에서 객체에 포함할 위치를 클릭하세요."
                : nextMode == WpfSmartMaskPointInputMode.Negative
                    ? "캔버스에서 후보에서 제외할 위치를 클릭하세요."
                    : "보정점 입력을 종료했습니다.");
        }

        private void ExecuteCancelSmartMaskGenerationCommand()
        {
            if (!isCreatingSmartMask || smartMaskCancellation == null)
            {
                return;
            }

            smartMaskCancellation.Cancel();
            RefreshSmartMaskCommandState("후보 생성을 취소하는 중입니다.");
        }

        private void ExecuteUndoSmartMaskPointCommand()
        {
            if (smartMaskPromptSession.UndoPoint())
            {
                RefreshPolygonOverlays();
                RefreshSmartMaskCommandState("마지막 보정점을 취소했습니다. 후보 다시 생성을 눌러 반영하세요.");
            }
        }

        private void ExecuteClearSmartMaskPointsCommand()
        {
            if (smartMaskPromptSession.ClearPoints())
            {
                RefreshPolygonOverlays();
                RefreshSmartMaskCommandState("모든 보정점을 지웠습니다. 후보 다시 생성을 눌러 반영하세요.");
            }
        }

        private void ExecuteSetSmartMaskPolygonDetailCommand(WpfSmartMaskPolygonDetail detail)
        {
            smartMaskPromptSession.SetPolygonDetail(detail);
            RefreshSmartMaskCommandState();
        }

        private void ExecuteNextSmartMaskInstanceCommand()
        {
            if (!smartMaskPromptSession.HasSession
                || !smartMaskPromptSession.HasProducedCandidate
                || candidateReviewState.HasPendingCandidates
                || isCreatingSmartMask)
            {
                RefreshSmartMaskCommandState("현재 후보를 먼저 확정하거나 스킵하세요.");
                return;
            }

            ResetSmartMaskPromptSession();
            SelectAnnotationTool(WpfAnnotationTool.Rectangle);
            string nextDetail = CanvasPanelViewModel?.IsSmartMaskAutoContourEnabled == true
                ? "다음 객체를 사각형으로 감싸면 자동 윤곽 후보가 바로 생성됩니다."
                : "다음 객체 둘레에 사각형 박스를 그린 뒤 자동 윤곽 옵션을 켜세요.";
            RefreshSmartMaskCommandState(nextDetail);
            SetYoloCommandStatus("스마트 마스크: 다음 객체 박스를 기다립니다.", isBusy: false);
        }

        private void ExecuteSetSmartMaskAutoContourMode(bool enabled)
        {
            EnsureProjectSettings();
            global.Data.ProjectSettings.SmartMaskAutoContourEnabled = enabled;
            string recipeName = GetCurrentRecipeName();
            if (!string.IsNullOrWhiteSpace(recipeName))
            {
                try
                {
                    global.Data.SaveConfig(recipeName, refreshDatasetVersion: false);
                }
                catch (Exception error)
                {
                    AppendLog("자동 윤곽 옵션 저장 실패: " + error.Message);
                }
            }

            if (enabled)
            {
                SelectAnnotationTool(WpfAnnotationTool.Rectangle);
                RefreshSmartMaskCommandState("자동 윤곽 켜짐 · 새 사각형을 완성하면 MobileSAM 후보를 바로 만듭니다.");
                SetYoloCommandStatus("라벨링 옵션: 자동 윤곽 켜짐", isBusy: false);
                AppendLog("자동 윤곽 켜짐: 새 사각형마다 검토용 윤곽 후보를 자동 생성합니다.");
                return;
            }

            RefreshSmartMaskCommandState("자동 윤곽 꺼짐 · 사각형은 일반 박스 라벨로 유지됩니다.");
            SetYoloCommandStatus("라벨링 옵션: 일반 박스", isBusy: false);
            AppendLog("자동 윤곽 꺼짐: 새 사각형을 일반 박스 라벨로 유지합니다.");
        }

        private void TryStartAutoSmartMaskForNewRoi(CanvasRect<float> roiRect)
        {
            if (roiRect == null
                || roiRect.ShapeKind != CanvasRoiShapeKind.Rectangle
                || CanvasPanelViewModel?.IsSmartMaskAutoContourEnabled != true
                || global.Data.ProjectSettings?.DatasetPurpose != LabelingDatasetPurpose.Segmentation
                || activeAnnotationTool != WpfAnnotationTool.Rectangle
                || smartMaskPromptSession.HasSession
                || candidateReviewState.HasPendingCandidates
                || isCreatingSmartMask)
            {
                return;
            }

            AppendLog("자동 윤곽: 새 사각형을 MobileSAM 프롬프트로 사용합니다.");
            ExecuteCreateSmartMaskCandidateCommand();
        }

        private void ContinueAutoSmartMaskAfterResolvedCandidate(string resolution)
        {
            if (CanvasPanelViewModel?.IsSmartMaskAutoContourEnabled != true
                || candidateReviewState.HasPendingCandidates)
            {
                return;
            }

            ResetSmartMaskPromptSession();
            SelectAnnotationTool(WpfAnnotationTool.Rectangle);
            RefreshSmartMaskCommandState($"{resolution} 완료 · 다음 객체를 사각형으로 감싸세요.");
            SetYoloCommandStatus($"자동 윤곽: {resolution} 완료 · 다음 박스 대기", isBusy: false);
        }

        private void ExecuteSelectSmartMaskCandidateVersionCommand(WpfSmartMaskCandidateVersion version)
        {
            if (isCreatingSmartMask
                || !candidateReviewState.HasPendingCandidates
                || !smartMaskPromptSession.TrySelectCandidate(version, out YoloWorkerSmokeCandidate candidate))
            {
                RefreshSmartMaskCommandState("비교할 Smart Mask 후보가 없습니다.");
                return;
            }

            candidateReviewState.LoadPendingCandidates(new[] { candidate }, clearConfirmed: false);
            ApplyCanvasDisplayMode(WpfCanvasDisplayMode.InferenceOnly, redraw: false, logChange: false);
            RefreshCandidateListWithPreferred(candidate);
            RefreshObjectList();
            RedrawReviewRois();
            RefreshCanvasWorkflowContext();
            string versionText = version == WpfSmartMaskCandidateVersion.Initial ? "이전" : "현재";
            AddCandidateReviewHistory($"Smart Mask {versionText} 후보 보기 · 확정 전");
            SetYoloCommandStatus($"Smart Mask {versionText} 후보 선택 / 확정 전", isBusy: false);
            AppendLog($"Smart Mask {versionText} 후보로 전환했습니다. 확정 전에는 저장되지 않습니다.");
            RefreshSmartMaskCommandState($"{versionText} 후보를 보고 있습니다. 확정하면 이 후보만 저장됩니다.");
        }

        private bool TryApplySmartMaskPointInput(CanvasImagePointEventArgs e)
        {
            if (e == null
                || !smartMaskPromptSession.HasSession
                || smartMaskPromptSession.InputMode == WpfSmartMaskPointInputMode.None)
            {
                return false;
            }

            if (e.Button == CanvasPointerButton.Right)
            {
                ExecuteUndoSmartMaskPointCommand();
                return true;
            }
            if (e.Button != CanvasPointerButton.Left)
            {
                return true;
            }
            if (!smartMaskPromptSession.TryAddPoint(e.ImagePoint, activeImageSize))
            {
                return true;
            }

            RefreshPolygonOverlays();
            RefreshSmartMaskCommandState("보정점을 추가했습니다. 후보 다시 생성으로 효과를 비교한 뒤 부족할 때 다음 점을 추가하세요.");
            return true;
        }

        private void ResetSmartMaskPromptSession()
        {
            smartMaskCancellation?.Cancel();
            smartMaskPromptSession.Reset();
            if (MainCanvasViewModel != null)
            {
                MainCanvasViewModel.IsImagePointInputMode = false;
                MainCanvasViewModel.IsTeachingMode =
                    activeAnnotationTool == WpfAnnotationTool.Rectangle
                    || activeAnnotationTool == WpfAnnotationTool.Ellipse;
                MainCanvasViewModel.ImageViewer.SetViewMode(
                    activeAnnotationTool == WpfAnnotationTool.PanZoom
                        ? CanvasInteractionMode.Drag
                        : CanvasInteractionMode.None);
            }
            RefreshPolygonOverlays();
        }

        private string GetCurrentSmartMaskRecipeName()
            => global.Recipe?.Name ?? string.Empty;

        private int FindSmartMaskPromptIndex()
        {
            EnsureManualRoiMetadataCount();
            for (int index = manualRois.Count - 1; index >= 0; index--)
            {
                if (GetManualRoiShapeKind(index) == CanvasRoiShapeKind.Rectangle
                    && !manualRois[index].IsEmpty)
                {
                    return index;
                }
            }

            return -1;
        }

        private void RefreshSmartMaskCommandState(string detail = "")
        {
            if (CanvasPanelViewModel == null)
            {
                return;
            }

            bool isVisible = global.Data.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.Segmentation;
            if (smartMaskPromptSession.HasSession
                && !smartMaskPromptSession.MatchesContext(activeImagePath, GetCurrentSmartMaskRecipeName()))
            {
                ResetSmartMaskPromptSession();
            }
            bool hasSession = isVisible && smartMaskPromptSession.HasSession;
            int promptIndex = isVisible && !hasSession ? FindSmartMaskPromptIndex() : -1;
            string effectiveDetail = detail;
            bool isReady = false;
            if (isVisible && !isCreatingSmartMask && !activeImageSize.IsEmpty)
            {
                WpfSmartMaskPromptSnapshot snapshot = hasSession
                    ? smartMaskPromptSession.Capture()
                    : promptIndex >= 0
                        ? new WpfSmartMaskPromptSnapshot
                        {
                            ImagePath = activeImagePath,
                            PromptBounds = manualRois[promptIndex],
                            ClassName = GetManualRoiClassName(promptIndex)
                        }
                        : null;
                if (snapshot != null)
                {
                    WpfMobileSamBoxPromptRequest request = mobileSamBoxPromptService.BuildRequest(
                        global.Data.ProjectSettings?.PythonModel,
                        snapshot.ImagePath,
                        snapshot.PromptBounds,
                        snapshot.ClassId,
                        snapshot.ClassName,
                        snapshot.Points,
                        hasSession ? smartMaskPromptSession.MaximumPolygonPoints : 96);
                    isReady = request.IsValid;
                    if (string.IsNullOrWhiteSpace(effectiveDetail) && !isReady)
                    {
                        effectiveDetail = string.Join(" ", request.Errors);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(effectiveDetail))
            {
                effectiveDetail = hasSession
                    ? "포함점/제외점을 한 점씩 추가하고 다시 생성해 비교하세요. 다시 생성하면 대기 후보 하나를 교체합니다."
                    : promptIndex < 0
                        ? "결함 둘레에 사각형 박스를 그리면 MobileSAM 후보 마스크를 만들 수 있습니다."
                        : "마지막 사각형을 시작 박스로 사용합니다. 결과는 확정 전 후보로만 표시됩니다.";
            }

            CanvasPanelViewModel.SetSmartMaskState(
                isVisible,
                isReady,
                isCreatingSmartMask,
                effectiveDetail,
                hasSession);
            CanvasPanelViewModel.SetSmartMaskSessionState(
                hasSession,
                isCreatingSmartMask,
                smartMaskPromptSession.PositivePointCount,
                smartMaskPromptSession.NegativePointCount,
                smartMaskPromptSession.InputMode,
                smartMaskPromptSession.HasProducedCandidate,
                smartMaskPromptSession.HasProducedCandidate && !candidateReviewState.HasPendingCandidates,
                smartMaskPromptSession.HasCandidateComparison
                    && candidateReviewState.PendingCandidates.Count == 1
                    && smartMaskPromptSession.IsSelectedCandidate(candidateReviewState.PendingCandidates[0]),
                smartMaskPromptSession.SelectedCandidateVersion);
        }
    }
}
