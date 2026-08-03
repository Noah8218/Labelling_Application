using System.Windows;
using Wpf.Ui.Appearance;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using MediaBrush = System.Windows.Media.Brush;

namespace MvcVisionSystem
{
    public partial class WpfEnvironmentSetupCenterWindow : FluentWindow
    {
        private static readonly string[] ThemeBrushKeys =
        {
            "AppBackgroundBrush",
            "PanelBrush",
            "CanvasBrush",
            "BorderBrushDark",
            "PrimaryTextBrush",
            "SecondaryTextBrush",
            "AccentBrush",
            "ToolbarButtonBrush",
            "ToolbarButtonBorderBrush",
            "SuccessBrush",
            "WarningBrush",
            "ProblemBrush"
        };

        public WpfEnvironmentSetupCenterWindow(WpfEnvironmentSetupCenterViewModel viewModel = null)
        {
            InitializeComponent();
            DataContext = viewModel ?? new WpfEnvironmentSetupCenterViewModel();
        }

        public WpfEnvironmentSetupCenterViewModel ViewModel
            => DataContext as WpfEnvironmentSetupCenterViewModel;

        public void ApplyThemeFrom(FrameworkElement source)
        {
            if (source == null)
            {
                return;
            }

            foreach (string key in ThemeBrushKeys)
            {
                if (source.TryFindResource(key) is MediaBrush brush)
                {
                    Resources[key] = brush;
                }
            }

            ApplicationThemeManager.Apply(this);
            if (TryFindResource("AppBackgroundBrush") is MediaBrush background)
            {
                Background = background;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
