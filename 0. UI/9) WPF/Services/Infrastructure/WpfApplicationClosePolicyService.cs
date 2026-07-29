using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public sealed class WpfApplicationClosePolicyService
    {
        public WpfApplicationClosePlan Build(WpfApplicationCloseState state)
        {
            state ??= new WpfApplicationCloseState();

            string[] activeWorkNames = (state.ActiveWorkNames ?? Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            int pendingCandidateCount = Math.Max(0, state.PendingCandidateCount);
            bool hasUnsavedAnnotations = state.HasUnsavedAnnotations;
            if (!hasUnsavedAnnotations && pendingCandidateCount == 0 && activeWorkNames.Length == 0)
            {
                return WpfApplicationClosePlan.NoPrompt;
            }

            var messageLines = new List<string>();
            if (hasUnsavedAnnotations)
            {
                messageLines.Add("현재 이미지의 라벨 편집이 아직 파일에 저장되지 않았습니다.");
            }

            if (pendingCandidateCount > 0)
            {
                messageLines.Add(
                    $"미확정 AI 후보 {pendingCandidateCount}개는 확정 라벨이 아니며 종료하면 저장되지 않고 폐기됩니다.");
            }

            if (activeWorkNames.Length > 0)
            {
                messageLines.Add(
                    $"진행 중인 작업 {activeWorkNames.Length}개는 창이 닫힐 때 중지됩니다.");
            }

            messageLines.Add(
                hasUnsavedAnnotations
                    ? "라벨을 저장할지 선택한 뒤 종료하세요."
                    : pendingCandidateCount > 0 && activeWorkNames.Length > 0
                        ? "후보를 폐기하고 진행 중인 작업을 중지한 뒤 종료할지 선택하세요."
                        : activeWorkNames.Length > 0
                            ? "진행 중인 작업을 중지하고 종료할지 선택하세요."
                            : "현재 후보를 폐기하고 종료할지 선택하세요.");

            var detailLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(state.ActiveImagePath))
            {
                detailLines.Add("현재 이미지: " + Path.GetFileName(state.ActiveImagePath));
            }

            if (hasUnsavedAnnotations && !string.IsNullOrWhiteSpace(state.UnsavedAnnotationReason))
            {
                detailLines.Add("저장되지 않은 편집: " + state.UnsavedAnnotationReason.Trim());
            }

            if (pendingCandidateCount > 0)
            {
                detailLines.Add($"미확정 AI 후보: {pendingCandidateCount}개");
            }

            if (activeWorkNames.Length > 0)
            {
                detailLines.Add("중지할 작업: " + string.Join(", ", activeWorkNames));
            }

            return new WpfApplicationClosePlan
            {
                PromptKind = hasUnsavedAnnotations
                    ? WpfApplicationClosePromptKind.SaveDiscardCancel
                    : WpfApplicationClosePromptKind.DiscardCancel,
                Title = hasUnsavedAnnotations
                    ? "저장하지 않은 라벨이 있습니다"
                    : pendingCandidateCount > 0
                        ? "확인되지 않은 작업이 있습니다"
                        : "진행 중인 작업이 있습니다",
                Message = string.Join(Environment.NewLine, messageLines),
                Details = string.Join(Environment.NewLine, detailLines),
                PrimaryButtonText = hasUnsavedAnnotations
                    ? "저장 후 종료"
                    : pendingCandidateCount > 0 && activeWorkNames.Length > 0
                        ? "폐기·중지 후 종료"
                        : activeWorkNames.Length > 0
                            ? "작업 중지 후 종료"
                            : "폐기하고 종료",
                SecondaryButtonText = hasUnsavedAnnotations ? "저장하지 않고 종료" : "계속 작업",
                TertiaryButtonText = hasUnsavedAnnotations ? "계속 작업" : string.Empty
            };
        }
    }

    public sealed class WpfApplicationCloseState
    {
        public bool HasUnsavedAnnotations { get; set; }

        public string UnsavedAnnotationReason { get; set; } = string.Empty;

        public int PendingCandidateCount { get; set; }

        public IReadOnlyList<string> ActiveWorkNames { get; set; } = Array.Empty<string>();

        public string ActiveImagePath { get; set; } = string.Empty;
    }

    public enum WpfApplicationClosePromptKind
    {
        None,
        SaveDiscardCancel,
        DiscardCancel
    }

    public enum WpfApplicationCloseDecision
    {
        Cancel,
        SaveAndClose,
        DiscardAndClose
    }

    public sealed class WpfApplicationClosePlan
    {
        public static WpfApplicationClosePlan NoPrompt { get; } = new WpfApplicationClosePlan();

        public WpfApplicationClosePromptKind PromptKind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public string PrimaryButtonText { get; set; } = string.Empty;

        public string SecondaryButtonText { get; set; } = string.Empty;

        public string TertiaryButtonText { get; set; } = string.Empty;

        public bool RequiresPrompt => PromptKind != WpfApplicationClosePromptKind.None;
    }
}
