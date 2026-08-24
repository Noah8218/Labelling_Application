using MahApps.Metro.IconPacks;
using Newtonsoft.Json;
using OpenVisionLab;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfDatasetSelectionWindowViewModel : WpfObservableViewModel
    {
        private static readonly Action NoOpCommand = () => { };
        private WpfDatasetSelectionItem selectedDataset;
        private string statusText = string.Empty;
        private Visibility emptyStateVisibility = Visibility.Collapsed;
        private ICommand openCommand = new RelayCommand(NoOpCommand);
        private ICommand createNewCommand = new RelayCommand(NoOpCommand);
        private ICommand refreshCommand = new RelayCommand(NoOpCommand);
        private ICommand cancelCommand = new RelayCommand(NoOpCommand);

        public WpfDatasetSelectionWindowViewModel()
        {
            OpenVisionLanguageService.LanguageChanged += OpenVisionLanguageService_LanguageChanged;
            RefreshStatusText();
        }

        public string ViewName => nameof(WpfDatasetSelectionWindow);

        public string WindowTitleText => T("WpfDatasetSelection.Title");

        public string DatasetSourceRuleTitleText => T("WpfDatasetSelection.SourceRule.Title");

        public string DatasetSourceRuleDetailText => T("WpfDatasetSelection.SourceRule.Detail");

        public string ExistingDatasetGuideTitleText => T("WpfDatasetSelection.Guide.Existing.Title");

        public string ExistingDatasetGuideDetailText => T("WpfDatasetSelection.Guide.Existing.Detail");

        public string CreateDatasetGuideTitleText => T("WpfDatasetSelection.Guide.Create.Title");

        public string CreateDatasetGuideDetailText => T("WpfDatasetSelection.Guide.Create.Detail");

        public string CreateDatasetGuideButtonText => T("WpfDatasetSelection.Action.Create.Short");

        public string EmptyStateTitleText => T("WpfDatasetSelection.Empty.Title");

        public string EmptyStateDetailText => T("WpfDatasetSelection.Empty.Detail");

        public string CreateFirstDatasetButtonText => T("WpfDatasetSelection.Action.Create");

        public string RefreshButtonText => T("WpfDatasetSelection.Action.Refresh");

        public string CreateNewButtonText => T("WpfDatasetSelection.Action.Create.Short");

        public string CancelButtonText => T("WpfDatasetSelection.Action.Cancel");

        public string OpenSelectedButtonText => T("WpfDatasetSelection.Action.Open");

        public ObservableCollection<WpfDatasetSelectionItem> Datasets { get; } = new ObservableCollection<WpfDatasetSelectionItem>();

        public WpfDatasetSelectionItem SelectedDataset
        {
            get => selectedDataset;
            set => SetProperty(ref selectedDataset, value);
        }

        public string StatusText
        {
            get => statusText;
            set => SetProperty(ref statusText, value ?? string.Empty);
        }

        public Visibility EmptyStateVisibility
        {
            get => emptyStateVisibility;
            private set => SetProperty(ref emptyStateVisibility, value);
        }

        public ICommand OpenCommand
        {
            get => openCommand;
            private set => SetProperty(ref openCommand, value);
        }

        public ICommand CreateNewCommand
        {
            get => createNewCommand;
            private set => SetProperty(ref createNewCommand, value);
        }

        public ICommand RefreshCommand
        {
            get => refreshCommand;
            private set => SetProperty(ref refreshCommand, value);
        }

        public ICommand CancelCommand
        {
            get => cancelCommand;
            private set => SetProperty(ref cancelCommand, value);
        }

        public void ConfigureCommands(Action open, Action createNew, Action refresh, Action cancel)
        {
            OpenCommand = new RelayCommand(open ?? NoOpCommand);
            CreateNewCommand = new RelayCommand(createNew ?? NoOpCommand);
            RefreshCommand = new RelayCommand(refresh ?? NoOpCommand);
            CancelCommand = new RelayCommand(cancel ?? NoOpCommand);
        }

        public void LoadDatasets(string recipeRootPath, string currentRecipeName)
        {
            Datasets.Clear();
            IReadOnlyList<string> recipeNames = WpfProjectRecipeService.ListRecipeNames(recipeRootPath);
            foreach (string recipeName in recipeNames)
            {
                Datasets.Add(BuildDatasetItem(recipeRootPath, recipeName, currentRecipeName));
            }

            SelectedDataset = Datasets.FirstOrDefault(item => item.IsCurrent)
                ?? Datasets.FirstOrDefault();
            RefreshStatusText();
            EmptyStateVisibility = Datasets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenVisionLanguageService_LanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitleText));
            OnPropertyChanged(nameof(DatasetSourceRuleTitleText));
            OnPropertyChanged(nameof(DatasetSourceRuleDetailText));
            OnPropertyChanged(nameof(ExistingDatasetGuideTitleText));
            OnPropertyChanged(nameof(ExistingDatasetGuideDetailText));
            OnPropertyChanged(nameof(CreateDatasetGuideTitleText));
            OnPropertyChanged(nameof(CreateDatasetGuideDetailText));
            OnPropertyChanged(nameof(CreateDatasetGuideButtonText));
            OnPropertyChanged(nameof(EmptyStateTitleText));
            OnPropertyChanged(nameof(EmptyStateDetailText));
            OnPropertyChanged(nameof(CreateFirstDatasetButtonText));
            OnPropertyChanged(nameof(RefreshButtonText));
            OnPropertyChanged(nameof(CreateNewButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(OpenSelectedButtonText));
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            StatusText = Datasets.Count > 0
                ? Format("WpfDatasetSelection.Status.Count", Datasets.Count)
                : T("WpfDatasetSelection.Status.Empty");
        }

        private static WpfDatasetSelectionItem BuildDatasetItem(string recipeRootPath, string recipeName, string currentRecipeName)
        {
            string manifestPath = WpfProjectRecipeService.BuildManifestPath(recipeRootPath, recipeName);
            string configPath = WpfProjectRecipeService.BuildConfigPath(recipeRootPath, recipeName);
            LabelingDatasetManifest manifest = TryReadManifest(manifestPath);
            CData recipeData = TryReadRecipeConfig(configPath);
            string purposeKey = GetDatasetPurposeKey(manifest?.DatasetPurpose);
            string outputRootPath = FirstNonEmpty(manifest?.OutputRootPath, recipeData?.OutputRootPath);
            string imageRootPath = FirstNonEmpty(manifest?.ImageRootPath, recipeData?.ProjectSettings?.PythonModel?.ImageRootPath);
            List<string> configClasses = recipeData?.ClassNamedList?
                .Select(item => item?.Text)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList()
                ?? new List<string>();
            IReadOnlyList<string> classes = manifest?.Classes?.Count > 0
                ? manifest.Classes
                : configClasses;
            string classesText = classes.Count > 0
                ? string.Join(", ", classes.Take(4)) + (classes.Count > 4 ? " ..." : string.Empty)
                : string.Empty;
            int imageCount = manifest?.ArtifactSummary?.ImageCount ?? 0;
            int labelCount = manifest?.ArtifactSummary?.PrimaryLabelCount ?? 0;
            return new WpfDatasetSelectionItem(
                recipeName,
                purposeKey,
                outputRootPath,
                imageRootPath,
                classesText,
                imageCount,
                labelCount,
                manifestPath,
                File.Exists(manifestPath),
                string.Equals(recipeName, currentRecipeName, StringComparison.OrdinalIgnoreCase));
        }

        private static LabelingDatasetManifest TryReadManifest(string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<LabelingDatasetManifest>(File.ReadAllText(manifestPath));
            }
            catch
            {
                return null;
            }
        }

        private static CData TryReadRecipeConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            {
                return null;
            }

            try
            {
                CData data = SerializeHelper.FromXmlFile<CData>(configPath);
                data?.NormalizeOutputPaths();
                return data;
            }
            catch
            {
                return null;
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static string GetDatasetPurposeKey(string purpose)
        {
            if (Enum.TryParse(purpose, out LabelingDatasetPurpose parsed))
            {
                return parsed switch
                {
                    LabelingDatasetPurpose.Segmentation => "WpfShell.Dataset.Purpose.Segmentation",
                    LabelingDatasetPurpose.AnomalyDetection => "WpfShell.Dataset.Purpose.AnomalyDetection",
                    _ => "WpfShell.Dataset.Purpose.ObjectDetection"
                };
            }

            return "WpfShell.Dataset.Purpose.Unselected";
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);

        private static string Format(string key, params object[] values)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture, T(key), values ?? Array.Empty<object>());
    }

    public sealed class WpfDatasetSelectionItem : WpfObservableViewModel
    {
        public WpfDatasetSelectionItem(
            string recipeName,
            string purposeKey,
            string outputRootPath,
            string imageRootPath,
            string classesText,
            int imageCount,
            int labelCount,
            string manifestPath,
            bool hasManifest,
            bool isCurrent)
        {
            RecipeName = recipeName ?? string.Empty;
            PurposeKey = purposeKey ?? "WpfShell.Dataset.Purpose.Unselected";
            OutputRootPath = outputRootPath ?? string.Empty;
            ImageRootPath = imageRootPath ?? string.Empty;
            ClassesText = classesText ?? string.Empty;
            ImageCount = imageCount;
            LabelCount = labelCount;
            ManifestPath = manifestPath ?? string.Empty;
            HasManifest = hasManifest;
            IsCurrent = isCurrent;
            OpenVisionLanguageService.LanguageChanged += OpenVisionLanguageService_LanguageChanged;
        }

        public string RecipeName { get; }

        public string PurposeKey { get; }

        public string PurposeText => T(PurposeKey);

        public string OutputRootPath { get; }

        public string ImageRootPath { get; }

        public string ClassesText { get; }

        public int ImageCount { get; }

        public int LabelCount { get; }

        public string ManifestPath { get; }

        public bool HasManifest { get; }

        public bool IsCurrent { get; }

        public string ToolTipText => Format(
            "WpfDatasetSelection.Item.Tooltip",
            string.IsNullOrWhiteSpace(OutputRootPath) ? T("WpfDatasetSelection.Item.StorageUnknown") : OutputRootPath,
            string.IsNullOrWhiteSpace(ImageRootPath) ? T("WpfDatasetSelection.Item.ImageRootUnknown") : ImageRootPath);

        public string StoragePathText => Format(
            "WpfDatasetSelection.Item.StoragePath",
            string.IsNullOrWhiteSpace(OutputRootPath) ? T("WpfDatasetSelection.Item.StorageUnknown") : OutputRootPath);

        public string ImageRootPathText => Format(
            "WpfDatasetSelection.Item.ImageRootPath",
            string.IsNullOrWhiteSpace(ImageRootPath) ? T("WpfDatasetSelection.Item.ImageRootUnknown") : ImageRootPath);

        public string ClassesLabelText => Format(
            "WpfDatasetSelection.Item.Classes",
            string.IsNullOrWhiteSpace(ClassesText) ? T("WpfDatasetSelection.Item.ClassesUnknown") : ClassesText);

        public string OpenActionText => T(IsCurrent
            ? "WpfDatasetSelection.Item.OpenAction.Current"
            : "WpfDatasetSelection.Item.OpenAction.Open");

        public string StatusText => T(IsCurrent
            ? "WpfDatasetSelection.Item.Status.Current"
            : (HasManifest ? "WpfDatasetSelection.Item.Status.Ready" : "WpfDatasetSelection.Item.Status.Configured"));

        public string CountText => Format("WpfDatasetSelection.Item.Count", ImageCount, LabelCount);

        public PackIconMaterialKind IconKind => HasManifest ? PackIconMaterialKind.DatabaseCheckOutline : PackIconMaterialKind.DatabaseAlertOutline;

        private void OpenVisionLanguageService_LanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PurposeText));
            OnPropertyChanged(nameof(ToolTipText));
            OnPropertyChanged(nameof(StoragePathText));
            OnPropertyChanged(nameof(ImageRootPathText));
            OnPropertyChanged(nameof(ClassesLabelText));
            OnPropertyChanged(nameof(OpenActionText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(CountText));
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);

        private static string Format(string key, params object[] values)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture, T(key), values ?? Array.Empty<object>());
    }
}
