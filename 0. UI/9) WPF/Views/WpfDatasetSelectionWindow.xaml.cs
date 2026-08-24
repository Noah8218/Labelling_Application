using System.Windows;

namespace MvcVisionSystem
{
    public partial class WpfDatasetSelectionWindow : Window
    {
        public WpfDatasetSelectionWindow()
        {
            InitializeComponent();
            WpfLocalizationTextRuntimeService.RegisterWindow(this);
        }

        public WpfDatasetSelectionWindowViewModel ViewModel => DataContext as WpfDatasetSelectionWindowViewModel;
    }
}
