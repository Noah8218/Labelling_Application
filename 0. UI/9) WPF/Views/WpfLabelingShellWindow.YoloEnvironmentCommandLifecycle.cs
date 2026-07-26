namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private bool BeginYoloEnvironmentCommand(string statusText)
        {
            if (isYoloEnvironmentCommandRunning || isTrainingCommandRunning || isDetecting || isBatchDetectionRunning)
            {
                AppendLog(WpfYoloEnvironmentCommandPresentationService.BuildBusyCommandLog());
                return false;
            }

            isYoloEnvironmentCommandRunning = true;
            ClearYoloRecoveryStatus();
            SetYoloCommandStatus(statusText, isBusy: true);
            UpdateYoloCommandButtons();
            return true;
        }

        private void EndYoloEnvironmentCommand()
        {
            isYoloEnvironmentCommandRunning = false;
            YoloStatusViewModel.SetCommandBusy(false);

            UpdateYoloCommandButtons();
            RefreshYoloStatus();
        }

        private void SetYoloCommandStatus(string text, bool isBusy)
        {
            YoloStatusViewModel.SetCommandStatus(text, isBusy);
        }

        private void SetYoloRecoveryStatus(string titleText, string detailText, string actionText)
        {
            ShellViewModel?.SetModelCenterRecoveryState(titleText, detailText, actionText);
            YoloStatusViewModel?.SetRecoveryState(titleText, detailText, actionText);
        }

        private void ClearYoloRecoveryStatus()
        {
            ShellViewModel?.ClearModelCenterRecoveryState();
            YoloStatusViewModel?.ClearRecoveryState();
        }
    }
}
