using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.Wpf.MessageDialogs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace LabelingApplication.Tests;

internal static partial class Program
{
    internal static void TestApplicationClosePolicyAndShellAdapter()
    {
        TestApplicationClosePolicyStateCombinations();
        TestApplicationCloseShellAdapterPreservesPendingCandidatesAndBlocksFailedSave();
        TestNeverLoadedShellCleanupDoesNotOpenOperatorClosePrompt();
    }

    private static void TestApplicationClosePolicyStateCombinations()
    {
        var service = new WpfApplicationClosePolicyService();

        WpfApplicationClosePlan clean = service.Build(new WpfApplicationCloseState());
        AssertTrue(!clean.RequiresPrompt, "clean idle close should not require a prompt");

        WpfApplicationClosePlan dirty = service.Build(new WpfApplicationCloseState
        {
            HasUnsavedAnnotations = true,
            UnsavedAnnotationReason = "클래스 변경",
            ActiveImagePath = @"C:\dataset\part-001.png"
        });
        AssertTrue(
            dirty.PromptKind == WpfApplicationClosePromptKind.SaveDiscardCancel,
            "dirty annotations should offer save, discard, and cancel");
        AssertTrue(
            dirty.PrimaryButtonText == "저장 후 종료"
                && dirty.SecondaryButtonText == "저장하지 않고 종료"
                && dirty.TertiaryButtonText == "계속 작업",
            "dirty close actions should be explicit");
        AssertTrue(
            dirty.Details.Contains("part-001.png", StringComparison.Ordinal)
                && dirty.Details.Contains("클래스 변경", StringComparison.Ordinal),
            "dirty close details should identify the current image and edit reason");

        WpfApplicationClosePlan candidates = service.Build(new WpfApplicationCloseState
        {
            PendingCandidateCount = 2
        });
        AssertTrue(
            candidates.PromptKind == WpfApplicationClosePromptKind.DiscardCancel,
            "candidate-only close should offer discard or cancel without a save action");
        AssertTrue(
            candidates.Message.Contains("확정 라벨이 아니며", StringComparison.Ordinal)
                && candidates.Message.Contains("저장되지 않고 폐기", StringComparison.Ordinal),
            "candidate-only close should state that candidates are not labels and are not saved");

        WpfApplicationClosePlan activeOnly = service.Build(new WpfApplicationCloseState
        {
            ActiveWorkNames = new[] { "모델 학습" }
        });
        AssertTrue(
            activeOnly.Title == "진행 중인 작업이 있습니다"
                && activeOnly.PrimaryButtonText == "작업 중지 후 종료",
            "active-only close should explicitly offer stop-and-close");

        WpfApplicationClosePlan combined = service.Build(new WpfApplicationCloseState
        {
            HasUnsavedAnnotations = true,
            PendingCandidateCount = 3,
            ActiveWorkNames = new[]
            {
                "모델 학습",
                "Smart Mask 후보 생성",
                "모델 학습",
                ""
            }
        });
        AssertTrue(
            combined.PromptKind == WpfApplicationClosePromptKind.SaveDiscardCancel,
            "combined dirty state should retain the save option");
        AssertTrue(
            combined.Message.Contains("미확정 AI 후보 3개", StringComparison.Ordinal)
                && combined.Message.Contains("진행 중인 작업 2개", StringComparison.Ordinal),
            "combined close summary should count candidate and distinct active work");
        AssertTrue(
            combined.Details.Contains("모델 학습", StringComparison.Ordinal)
                && combined.Details.Contains("Smart Mask 후보 생성", StringComparison.Ordinal),
            "combined close details should name active work");
    }

    private static void TestApplicationCloseShellAdapterPreservesPendingCandidatesAndBlocksFailedSave()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
        }

        var window = new WpfLabelingShellWindow();
        try
        {
            WpfCandidateReviewStateService candidateState =
                GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
            candidateState.MutablePendingCandidates.Add(new YoloWorkerSmokeCandidate
            {
                Index = 1,
                ClassName = "Defect",
                Confidence = 0.91,
                X = 10,
                Y = 12,
                Width = 30,
                Height = 20
            });
            SetPrivateField(window, "isBatchDetectionRunning", true);

            WpfApplicationClosePlan candidatePlan =
                InvokePrivateResult<WpfApplicationClosePlan>(window, "BuildApplicationClosePlan");
            AssertTrue(
                candidatePlan.PromptKind == WpfApplicationClosePromptKind.DiscardCancel,
                "shell should map pending candidates and active work into discard/cancel");
            AssertTrue(
                candidatePlan.Details.Contains("일괄 AI 검사", StringComparison.Ordinal),
                "shell should name the running batch inspection");

            bool cancelled = InvokePrivateResult<bool>(
                window,
                "ApplyApplicationCloseDecision",
                WpfApplicationCloseDecision.Cancel);
            AssertTrue(!cancelled, "cancel should keep the application open");
            AssertTrue(candidateState.PendingCount == 1, "cancel must preserve pending candidates");

            bool discarded = InvokePrivateResult<bool>(
                window,
                "ApplyApplicationCloseDecision",
                WpfApplicationCloseDecision.DiscardAndClose);
            AssertTrue(discarded, "explicit discard should approve close");
            AssertTrue(
                candidateState.PendingCount == 1,
                "close approval must not auto-confirm or mutate pending candidates before cleanup");

            SetPrivateField(window, "isApplicationCloseApproved", false);
            SetPrivateField(window, "isBatchDetectionRunning", false);
            InvokePrivateResult<object>(window, "MarkAnnotationsDirty", "테스트 편집");

            bool failureDialogObserved = false;
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                WpfMessageDialogWindow dialog = System.Windows.Application.Current.Windows
                    .OfType<WpfMessageDialogWindow>()
                    .FirstOrDefault();
                AssertTrue(dialog != null, "save-failure warning should be visible");
                AssertTrue(
                    dialog.Title == "라벨을 저장하지 못했습니다",
                    "save-failure warning should explain why the window remains open");
                failureDialogObserved = true;
                dialog.Close();
            }), DispatcherPriority.ApplicationIdle);

            bool failedSaveApproved = InvokePrivateResult<bool>(
                window,
                "ApplyApplicationCloseDecision",
                WpfApplicationCloseDecision.SaveAndClose);
            AssertTrue(failureDialogObserved, "failed-save warning was not observed");
            AssertTrue(!failedSaveApproved, "failed save must reject application close");
            AssertTrue(
                !GetPrivateField<bool>(window, "isApplicationCloseApproved"),
                "failed save must leave close approval cleared");
        }
        finally
        {
            SetPrivateField(window, "isApplicationCloseApproved", true);
            window.Close();
        }
    }

    private static void TestNeverLoadedShellCleanupDoesNotOpenOperatorClosePrompt()
    {
        var window = new WpfLabelingShellWindow();
        bool closed = false;
        window.Closed += (_, _) => closed = true;
        try
        {
            AssertTrue(!window.IsLoaded, "test cleanup shell should never enter a visible operator session");
            InvokePrivateResult<object>(window, "MarkAnnotationsDirty", "테스트 정리");

            window.Close();

            AssertTrue(closed, "a never-loaded shell should close without opening an operator decision dialog");
        }
        finally
        {
            if (!closed)
            {
                SetPrivateField(window, "isApplicationCloseApproved", true);
                window.Close();
            }
        }
    }
}
