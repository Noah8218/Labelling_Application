using System;

namespace MvcVisionSystem
{
    /// <summary>
    /// Lazy owner for model/inference presentation state. Labeling startup does
    /// not create these ViewModels until a model workflow explicitly requests them.
    /// </summary>
    public sealed class WpfModelWorkflowViewModels
    {
        private readonly Lazy<WpfCandidateReviewPanelViewModel> candidateReviewViewModel =
            new Lazy<WpfCandidateReviewPanelViewModel>(() => new WpfCandidateReviewPanelViewModel());
        private readonly Lazy<WpfYoloStatusPanelViewModel> yoloStatusViewModel =
            new Lazy<WpfYoloStatusPanelViewModel>(() => new WpfYoloStatusPanelViewModel());
        private readonly Lazy<WpfYoloModelSettingsPanelViewModel> yoloModelSettingsViewModel =
            new Lazy<WpfYoloModelSettingsPanelViewModel>(() => new WpfYoloModelSettingsPanelViewModel());
        private readonly Lazy<WpfTrainingSettingsPanelViewModel> trainingSettingsViewModel =
            new Lazy<WpfTrainingSettingsPanelViewModel>(() => new WpfTrainingSettingsPanelViewModel());

        public WpfCandidateReviewPanelViewModel CandidateReviewViewModel => candidateReviewViewModel.Value;

        public WpfCandidateReviewPanelViewModel ExistingCandidateReviewViewModel =>
            candidateReviewViewModel.IsValueCreated ? candidateReviewViewModel.Value : null;

        public WpfYoloStatusPanelViewModel YoloStatusViewModel => yoloStatusViewModel.Value;

        public WpfYoloStatusPanelViewModel ExistingYoloStatusViewModel =>
            yoloStatusViewModel.IsValueCreated ? yoloStatusViewModel.Value : null;

        public WpfYoloModelSettingsPanelViewModel YoloModelSettingsViewModel => yoloModelSettingsViewModel.Value;

        public WpfYoloModelSettingsPanelViewModel ExistingYoloModelSettingsViewModel =>
            yoloModelSettingsViewModel.IsValueCreated ? yoloModelSettingsViewModel.Value : null;

        public WpfTrainingSettingsPanelViewModel TrainingSettingsViewModel => trainingSettingsViewModel.Value;

        public WpfTrainingSettingsPanelViewModel ExistingTrainingSettingsViewModel =>
            trainingSettingsViewModel.IsValueCreated ? trainingSettingsViewModel.Value : null;

        public bool HasCreatedViewModel => candidateReviewViewModel.IsValueCreated
            || yoloStatusViewModel.IsValueCreated
            || yoloModelSettingsViewModel.IsValueCreated
            || trainingSettingsViewModel.IsValueCreated;
    }
}
