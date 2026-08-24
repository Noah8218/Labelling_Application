using MvcVisionSystem.Yolo;
using OpenVisionLab;
using OpenVisionLab.Mvvm;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DrawingColor = System.Drawing.Color;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace MvcVisionSystem
{
    public sealed class WpfClassCatalogPanelViewModel : WpfObservableViewModel
    {
        private static readonly Action NoOpCommand = () => { };
        private static readonly Action<KeyInputCommandArgs> NoOpKeyCommand = _ => { };
        private static readonly Action<object> NoOpSelectionCommand = _ => { };
        private string className = string.Empty;
        private string outputRootPath = string.Empty;
        private string statusText = T("WpfClassCatalog.Status.Initial");
        private bool hasExplicitStatusText;
        private WpfClassCatalogListItem selectedClass;
        private ICommand classNamePreviewKeyDownCommand = new RelayCommand<KeyInputCommandArgs>(NoOpKeyCommand);
        private ICommand addClassCommand = new RelayCommand(NoOpCommand);
        private ICommand renameClassCommand = new RelayCommand(NoOpCommand);
        private ICommand removeClassCommand = new RelayCommand(NoOpCommand);
        private ICommand applyClassColorCommand = new RelayCommand(NoOpCommand);
        private ICommand classSelectionChangedCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private WpfClassCatalogColorPreset selectedColorPreset;

        public WpfClassCatalogPanelViewModel()
        {
            foreach (WpfClassCatalogColorPreset preset in BuildDefaultColorPresets())
            {
                ColorPresets.Add(preset);
            }

            SelectedColorPreset = ColorPresets.FirstOrDefault();
            Classes.CollectionChanged += (_, _) => NotifyClassCatalogSummaryChanged();
        }

        public string ViewName => nameof(WpfClassCatalogPanel);

        public string PanelTitleText => T("WpfClassCatalog.Panel.Title");

        public string ClassSectionLabelText => T("WpfClassCatalog.Section.Classes");

        public string ClassNameToolTipText => T("WpfClassCatalog.ClassName.ToolTip");

        public string AddClassAutomationNameText => T("WpfClassCatalog.Add.Name");

        public string AddClassToolTipText => T("WpfClassCatalog.Add.ToolTip");

        public string AddClassButtonText => T("WpfClassCatalog.Add.Text");

        public string RemoveClassAutomationNameText => T("WpfClassCatalog.Remove.Name");

        public string RemoveClassToolTipText => T("WpfClassCatalog.Remove.ToolTip");

        public string RemoveClassButtonText => T("WpfClassCatalog.Remove.Text");

        public string RenameClassAutomationNameText => T("WpfClassCatalog.Rename.Name");

        public string RenameClassToolTipText => T("WpfClassCatalog.Rename.ToolTip");

        public string RenameClassButtonText => T("WpfClassCatalog.Rename.Text");

        public string ClassColorAutomationNameText => T("WpfClassCatalog.Color.Name");

        public string ClassColorToolTipText => T("WpfClassCatalog.Color.ToolTip");

        public string ApplyClassColorAutomationNameText => T("WpfClassCatalog.Color.Apply.Name");

        public string ApplyClassColorToolTipText => T("WpfClassCatalog.Color.Apply.ToolTip");

        public string ApplyClassColorButtonText => T("WpfClassCatalog.Color.Apply.Text");

        public string ClassCatalogGuideTitleText => T("WpfClassCatalog.Guide.Title");

        public string ClassCatalogGuideDetailText => T("WpfClassCatalog.Guide.Detail");

        public string ClassCatalogSummaryText
        {
            get
            {
                string selected = SelectedClass?.CanonicalDisplayText;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    selected = T("WpfClassCatalog.Selection.None");
                }

                return Format("WpfClassCatalog.Summary", Classes.Count, selected);
            }
        }

        public string CurrentDrawingClassTitleText => T("WpfClassCatalog.Current.Title");

        public string CurrentDrawingClassDetailText
        {
            get
            {
                string selected = SelectedClass?.CanonicalDisplayText;
                return string.IsNullOrWhiteSpace(selected)
                    ? T("WpfClassCatalog.Current.EmptyDetail")
                    : Format("WpfClassCatalog.Current.SelectedDetail", selected);
            }
        }

        public string ClassCatalogActionText
        {
            get
            {
                return Classes.Count <= 0
                    ? T("WpfClassCatalog.Action.Empty")
                    : T("WpfClassCatalog.Action.Populated");
            }
        }

        public string ClassColorSectionTitleText => T("WpfClassCatalog.Color.Section");

        public string RecipeClassListTitleText => T("WpfClassCatalog.Recipe.Title");

        public string RecipeClassListGuideText => T("WpfClassCatalog.Recipe.Guide");

        public string ClassIndexContractText => T("WpfClassCatalog.IndexContract");

        public ObservableCollection<WpfClassCatalogListItem> Classes { get; } = new ObservableCollection<WpfClassCatalogListItem>();

        public ObservableCollection<WpfClassCatalogColorPreset> ColorPresets { get; } = new ObservableCollection<WpfClassCatalogColorPreset>();

        public ICommand ClassNamePreviewKeyDownCommand
        {
            get => classNamePreviewKeyDownCommand;
            private set => SetProperty(ref classNamePreviewKeyDownCommand, value);
        }

        public ICommand AddClassCommand
        {
            get => addClassCommand;
            private set => SetProperty(ref addClassCommand, value);
        }

        public ICommand RenameClassCommand
        {
            get => renameClassCommand;
            private set => SetProperty(ref renameClassCommand, value);
        }

        public ICommand RemoveClassCommand
        {
            get => removeClassCommand;
            private set => SetProperty(ref removeClassCommand, value);
        }

        public ICommand ApplyClassColorCommand
        {
            get => applyClassColorCommand;
            private set => SetProperty(ref applyClassColorCommand, value);
        }

        public ICommand ClassSelectionChangedCommand
        {
            get => classSelectionChangedCommand;
            private set => SetProperty(ref classSelectionChangedCommand, value);
        }

        public string ClassName
        {
            get => className;
            set => SetProperty(ref className, value ?? string.Empty);
        }

        public string OutputRootPath
        {
            get => outputRootPath;
            set => SetProperty(ref outputRootPath, value ?? string.Empty);
        }

        public string StatusText
        {
            get => statusText;
            set
            {
                hasExplicitStatusText = true;
                SetProperty(ref statusText, value ?? string.Empty);
            }
        }

        public WpfClassCatalogColorPreset SelectedColorPreset
        {
            get => selectedColorPreset;
            set => SetProperty(ref selectedColorPreset, value);
        }

        public WpfClassCatalogListItem SelectedClass
        {
            get => selectedClass;
            set
            {
                if (SetProperty(ref selectedClass, value))
                {
                    if (value != null)
                    {
                        ClassName = value.Text;
                        SelectedColorPreset = FindColorPreset(value.DrawColor) ?? SelectedColorPreset;
                    }

                    NotifyClassCatalogSummaryChanged();
                }
            }
        }

        public void ConfigureCommands(
            Action<KeyInputCommandArgs> classNamePreviewKeyDown,
            Action addClass,
            Action renameClass,
            Action removeClass,
            Action applyClassColor,
            Action<object> classSelectionChanged)
        {
            // Class catalog commands use DTO/value parameters so this ViewModel stays independent from WPF event args.
            ClassNamePreviewKeyDownCommand = new RelayCommand<KeyInputCommandArgs>(classNamePreviewKeyDown ?? NoOpKeyCommand);
            AddClassCommand = new RelayCommand(addClass ?? NoOpCommand);
            RenameClassCommand = new RelayCommand(renameClass ?? NoOpCommand);
            RemoveClassCommand = new RelayCommand(removeClass ?? NoOpCommand);
            ApplyClassColorCommand = new RelayCommand(applyClassColor ?? NoOpCommand);
            ClassSelectionChangedCommand = new RelayCommand<object>(classSelectionChanged ?? NoOpSelectionCommand);
        }

        public void LoadOutputRoot(string path)
        {
            OutputRootPath = path ?? string.Empty;
        }

        public void SetClasses(IEnumerable<CClassItem> classItems, string selectedName = "")
        {
            string normalizedSelectedName = ClassCatalogService.NormalizeClassName(selectedName);
            WpfClassCatalogListItem selectedItem = null;

            SelectedClass = null;
            Classes.Clear();

            int canonicalIndex = 0;
            foreach (CClassItem classItem in classItems ?? Array.Empty<CClassItem>())
            {
                int currentIndex = canonicalIndex++;
                if (classItem == null || string.IsNullOrWhiteSpace(classItem.Text))
                {
                    continue;
                }

                var listItem = new WpfClassCatalogListItem(classItem, currentIndex);
                Classes.Add(listItem);
                if (!string.IsNullOrWhiteSpace(normalizedSelectedName)
                    && string.Equals(listItem.Text, normalizedSelectedName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedItem = listItem;
                }
            }

            if (selectedItem != null)
            {
                SelectedClass = selectedItem;
            }

            NotifyClassCatalogSummaryChanged();
        }

        public void SelectClass(string name)
        {
            string normalizedName = ClassCatalogService.NormalizeClassName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return;
            }

            WpfClassCatalogListItem item = Classes.FirstOrDefault(candidate =>
                string.Equals(candidate.Text, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                SelectedClass = item;
                return;
            }

            ClassName = normalizedName;
        }

        public void ClearClassName()
        {
            ClassName = string.Empty;
        }

        public void RefreshLocalizedPresentation()
        {
            if (!hasExplicitStatusText)
            {
                statusText = T("WpfClassCatalog.Status.Initial");
            }

            foreach (WpfClassCatalogColorPreset preset in ColorPresets)
            {
                preset.RefreshLocalizedPresentation();
            }

            foreach (WpfClassCatalogListItem item in Classes)
            {
                item.RefreshLocalizedPresentation();
            }

            OnPropertyChanged(string.Empty);
        }

        public WpfClassCatalogColorPreset FindColorPreset(DrawingColor color)
        {
            return ColorPresets.FirstOrDefault(preset => preset.Matches(color));
        }

        private static IEnumerable<WpfClassCatalogColorPreset> BuildDefaultColorPresets()
        {
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.Normal", DrawingColor.FromArgb(34, 197, 94));
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.Defect", DrawingColor.FromArgb(239, 68, 68));
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.Warning", DrawingColor.FromArgb(245, 158, 11));
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.Review", DrawingColor.FromArgb(59, 130, 246));
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.Segmentation", DrawingColor.FromArgb(168, 85, 247));
            yield return new WpfClassCatalogColorPreset("WpfClassCatalog.ColorPreset.ForeignMaterial", DrawingColor.FromArgb(20, 184, 166));
        }

        private void NotifyClassCatalogSummaryChanged()
        {
            OnPropertyChanged(nameof(ClassCatalogSummaryText));
            OnPropertyChanged(nameof(CurrentDrawingClassDetailText));
            OnPropertyChanged(nameof(ClassCatalogActionText));
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);

        private static string Format(string key, params object[] arguments)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                T(key),
                arguments ?? Array.Empty<object>());
        }
    }

    public sealed class WpfClassCatalogListItem : INotifyPropertyChanged
    {
        public WpfClassCatalogListItem(CClassItem classItem, int canonicalIndex = 0)
        {
            Text = ClassCatalogService.NormalizeClassName(classItem?.Text);
            CanonicalIndex = Math.Max(0, canonicalIndex);
            DrawColor = classItem?.DrawColor ?? DrawingColor.LimeGreen;
            var brush = new MediaSolidColorBrush(MediaColor.FromRgb(DrawColor.R, DrawColor.G, DrawColor.B));
            brush.Freeze();
            DrawBrush = brush;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Text { get; }

        public int CanonicalIndex { get; }

        public string CanonicalDisplayText => $"{CanonicalIndex} \u00B7 {Text}";

        public string DisplayText => CanonicalDisplayText;

        public string ToolTip => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            OpenVisionLanguageService.T("WpfClassCatalog.Item.ToolTip"),
            CanonicalIndex,
            Text);

        public DrawingColor DrawColor { get; }

        public MediaBrush DrawBrush { get; }

        internal void RefreshLocalizedPresentation()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolTip)));
        }
    }

    public sealed class WpfClassCatalogColorPreset
    {
        public WpfClassCatalogColorPreset(string nameKey, DrawingColor color)
        {
            NameKey = nameKey ?? string.Empty;
            Color = color;
            var brush = new MediaSolidColorBrush(MediaColor.FromRgb(color.R, color.G, color.B));
            brush.Freeze();
            Brush = brush;
        }

        public string NameKey { get; }

        public string Name => OpenVisionLanguageService.T(NameKey);

        public DrawingColor Color { get; }

        public MediaBrush Brush { get; }

        public bool Matches(DrawingColor color)
            => color.ToArgb() == Color.ToArgb();

        internal void RefreshLocalizedPresentation()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
