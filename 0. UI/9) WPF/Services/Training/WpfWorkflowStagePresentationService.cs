using OpenVisionLab;

namespace MvcVisionSystem
{
    public sealed class WpfWorkflowStagePresentation
    {
        public WpfWorkflowStagePresentation(
            WpfShellWorkflowStage stage,
            string progressText,
            string titleText,
            string detailText,
            string nextActionText)
        {
            Stage = stage;
            ProgressText = progressText ?? string.Empty;
            TitleText = titleText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            NextActionText = nextActionText ?? string.Empty;
        }

        public WpfShellWorkflowStage Stage { get; }

        public string ProgressText { get; }

        public string TitleText { get; }

        public string DetailText { get; }

        public string NextActionText { get; }
    }

    public static class WpfWorkflowStagePresentationService
    {
        public static WpfWorkflowStagePresentation Build(WpfShellWorkflowStage stage)
        {
            return stage switch
            {
                WpfShellWorkflowStage.Labeling => new WpfWorkflowStagePresentation(
                    stage,
                    T("WpfShell.Workflow.Labeling.Progress"),
                    T("WpfShell.Workflow.Labeling.Title"),
                    T("WpfShell.Workflow.Labeling.Detail"),
                    T("WpfShell.Workflow.Labeling.Next")),

                WpfShellWorkflowStage.Inference => new WpfWorkflowStagePresentation(
                    stage,
                    T("WpfShell.Workflow.Inference.Progress"),
                    T("WpfShell.Workflow.Inference.Title"),
                    T("WpfShell.Workflow.Inference.Detail"),
                    T("WpfShell.Workflow.Inference.Next")),

                WpfShellWorkflowStage.TrainingModel => new WpfWorkflowStagePresentation(
                    stage,
                    T("WpfShell.Workflow.Training.Progress"),
                    T("WpfShell.Workflow.Training.Title"),
                    T("WpfShell.Workflow.Training.Detail"),
                    T("WpfShell.Workflow.Training.Next")),

                _ => new WpfWorkflowStagePresentation(
                    WpfShellWorkflowStage.Dataset,
                    T("WpfShell.Workflow.Dataset.Progress"),
                    T("WpfShell.Workflow.Dataset.Title"),
                    T("WpfShell.Workflow.Dataset.Detail"),
                    T("WpfShell.Workflow.Dataset.Next"))
            };
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);
    }
}
