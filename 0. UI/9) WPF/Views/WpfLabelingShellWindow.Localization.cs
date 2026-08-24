using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void LanguageViewModel_LanguageChanged(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.DataBind,
                    new Action(() => LanguageViewModel_LanguageChanged(sender, e)));
                return;
            }

            ShellViewModel?.RefreshLocalizedPresentation();
            ClassCatalogViewModel?.RefreshLocalizedPresentation();
            ImageQueueViewModel?.RefreshLocalizedPresentation(imageQueueItems);
            StatusBarViewModel?.RefreshLocalizedPresentation();
            CanvasPanelControl?.RefreshLocalizedViewerStatus();
            if (ImageQueueFilterBox?.ItemsSource is IEnumerable<WpfImageQueueFilterOption> filterOptions)
            {
                foreach (WpfImageQueueFilterOption filterOption in filterOptions)
                {
                    filterOption?.RefreshLocalizedPresentation();
                }
            }
            RefreshShellDatasetContext();
            UpdateImageQueueStatusText();
            UpdateYoloCommandButtons();
            WpfLocalizationTextRuntimeService.RefreshAll();
            CanvasPanelControl?.RefreshLocalizedViewerStatus();
        }
    }
}
