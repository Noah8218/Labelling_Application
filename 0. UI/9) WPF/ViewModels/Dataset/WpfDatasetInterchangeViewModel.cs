using MvcVisionSystem.Yolo;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfDatasetInterchangeOption
    {
        public WpfDatasetInterchangeOption(DatasetExportCapability capability)
        {
            Capability = capability;
            DirectionText = string.Equals(capability.Direction, "import", StringComparison.OrdinalIgnoreCase)
                ? "\uAC00\uC838\uC624\uAE30"
                : "\uB0B4\uBCF4\uB0B4\uAE30";
            DisplayText = $"{capability.DisplayName} \u00B7 {DirectionText}";
            PurposeText = WpfDatasetContextPresentationService.FormatPurposeName(
                Enum.TryParse(capability.DatasetPurpose, out LabelingDatasetPurpose purpose)
                    ? purpose
                    : LabelingDatasetPurpose.ObjectDetection);
        }

        public DatasetExportCapability Capability { get; }

        public string DisplayText { get; }

        public string DirectionText { get; }

        public string PurposeText { get; }

        public bool IsImport => string.Equals(Capability.Direction, "import", StringComparison.OrdinalIgnoreCase);

        public bool RequiresImageRoot => IsImport
            && !string.Equals(Capability.FormatKey, "cvat-detection-import", StringComparison.Ordinal)
            && !string.Equals(Capability.FormatKey, "cvat-segmentation-import", StringComparison.Ordinal);

        public bool SourceIsDirectory => string.Equals(
            Capability.FormatKey,
            "pascal-voc-detection-import",
            StringComparison.Ordinal);

        public bool TargetIsDirectory => string.Equals(
            Capability.FormatKey,
            "pascal-voc-detection",
            StringComparison.Ordinal);
    }

    public sealed class WpfDatasetInterchangeIssueItem
    {
        public WpfDatasetInterchangeIssueItem(string text, bool isBlocking)
        {
            Text = text ?? string.Empty;
            IsBlocking = isBlocking;
        }

        public string Text { get; }

        public bool IsBlocking { get; }

        public string SeverityText => IsBlocking ? "\uCC28\uB2E8" : "\uC8FC\uC758";
    }

    public sealed class WpfDatasetInterchangeViewModel : WpfObservableViewModel
    {
        private readonly DatasetInterchangePreflightService preflightService;
        private CData data;
        private WpfDatasetInterchangeOption selectedOperation;
        private string sourcePath = string.Empty;
        private string imageRoot = string.Empty;
        private string targetPath = string.Empty;
        private string targetSplit = YoloDatasetSplitService.TrainMode;
        private string datasetName = "\uB370\uC774\uD130\uC14B \uBBF8\uC120\uD0DD";
        private string datasetPurposeText = "\uBAA9\uC801 \uBBF8\uD655\uC778";
        private string statusText = "\uC0AC\uC804\uAC80\uC0AC \uB300\uAE30";
        private string statusDetailText = "\uD615\uC2DD\uACFC \uACBD\uB85C\uB97C \uC120\uD0DD\uD55C \uB4A4 Dry-run\uC744 \uC2E4\uD589\uD558\uC138\uC694.";
        private string metricText = "\uC774\uBBF8\uC9C0 - \u00B7 \uC5B4\uB178\uD14C\uC774\uC158 - \u00B7 \uD074\uB798\uC2A4 -";
        private string sourceIntegrityText = "\uC6D0\uBCF8 \uBB34\uACB0\uC131: \uBBF8\uD655\uC778";
        private string targetIntegrityText = "\uC694\uCCAD \uB300\uC0C1: \uBBF8\uD655\uC778";
        private string lastDryRunSignature = string.Empty;
        private bool canApply;
        private Func<WpfDatasetInterchangeOption, string, string> pickSource =
            (_, current) => current;
        private Func<WpfDatasetInterchangeOption, string, string> pickTarget =
            (_, current) => current;
        private Func<string, string> pickImageRoot = current => current;

        public WpfDatasetInterchangeViewModel(
            CData data = null,
            DatasetInterchangePreflightService preflightService = null)
        {
            this.preflightService = preflightService ?? new DatasetInterchangePreflightService();
            foreach (DatasetExportCapability capability in this.preflightService.BuildSupportedCapabilities())
            {
                Operations.Add(new WpfDatasetInterchangeOption(capability));
            }

            Splits.Add(YoloDatasetSplitService.TrainMode);
            Splits.Add(YoloDatasetSplitService.ValidMode);
            Splits.Add(YoloDatasetSplitService.TestMode);
            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseTargetCommand = new RelayCommand(BrowseTarget);
            BrowseImageRootCommand = new RelayCommand(BrowseImageRoot);
            DryRunCommand = new RelayCommand(RunDryRun);
            ApplyCommand = new RelayCommand(Apply, () => CanApply);
            Refresh(data);
        }

        public ObservableCollection<WpfDatasetInterchangeOption> Operations { get; } =
            new ObservableCollection<WpfDatasetInterchangeOption>();

        public ObservableCollection<string> Splits { get; } = new ObservableCollection<string>();

        public ObservableCollection<WpfDatasetInterchangeIssueItem> Findings { get; } =
            new ObservableCollection<WpfDatasetInterchangeIssueItem>();

        public WpfDatasetInterchangeOption SelectedOperation
        {
            get => selectedOperation;
            set
            {
                if (SetProperty(ref selectedOperation, value))
                {
                    ResetPathsForSelection();
                    OnPropertyChanged(nameof(IsImport));
                    OnPropertyChanged(nameof(IsExport));
                    OnPropertyChanged(nameof(RequiresImageRoot));
                    OnPropertyChanged(nameof(SourceLabelText));
                    OnPropertyChanged(nameof(TargetLabelText));
                    OnPropertyChanged(nameof(OperationContractText));
                }
            }
        }

        public bool IsImport => SelectedOperation?.IsImport == true;

        public bool IsExport => !IsImport;

        public bool RequiresImageRoot => SelectedOperation?.RequiresImageRoot == true;

        public string SourceLabelText => IsImport
            ? "\uC678\uBD80 \uC5B4\uB178\uD14C\uC774\uC158"
            : "\uD604\uC7AC \uB370\uC774\uD130\uC14B";

        public string TargetLabelText => IsImport
            ? "\uAC00\uC838\uC624\uAE30 \uB300\uC0C1"
            : "\uB0B4\uBCF4\uB0B4\uAE30 \uB300\uC0C1";

        public string OperationContractText => SelectedOperation == null
            ? "\uD615\uC2DD\uC744 \uC120\uD0DD\uD558\uC138\uC694."
            : $"{SelectedOperation.PurposeText} \u00B7 {SelectedOperation.DirectionText} \u00B7 "
                + (IsImport
                    ? "\uC6D0\uBCF8\uC740 \uC77D\uAE30 \uC804\uC6A9\uC73C\uB85C \uC720\uC9C0\uB418\uBA70 \uD604\uC7AC \uB370\uC774\uD130\uC14B\uC5D0 \uC801\uC6A9\uB429\uB2C8\uB2E4."
                    : "\uD604\uC7AC \uB370\uC774\uD130\uC14B\uC740 \uC77D\uAE30 \uC804\uC6A9\uC73C\uB85C \uC720\uC9C0\uB429\uB2C8\uB2E4.");

        public string SourcePath
        {
            get => sourcePath;
            set
            {
                if (SetProperty(ref sourcePath, value ?? string.Empty))
                {
                    InvalidateDryRun();
                }
            }
        }

        public string ImageRoot
        {
            get => imageRoot;
            set
            {
                if (SetProperty(ref imageRoot, value ?? string.Empty))
                {
                    InvalidateDryRun();
                }
            }
        }

        public string TargetPath
        {
            get => targetPath;
            set
            {
                if (SetProperty(ref targetPath, value ?? string.Empty))
                {
                    InvalidateDryRun();
                }
            }
        }

        public string TargetSplit
        {
            get => targetSplit;
            set
            {
                if (SetProperty(ref targetSplit, value ?? YoloDatasetSplitService.TrainMode))
                {
                    InvalidateDryRun();
                }
            }
        }

        public string DatasetName
        {
            get => datasetName;
            private set => SetProperty(ref datasetName, value ?? string.Empty);
        }

        public string DatasetPurposeText
        {
            get => datasetPurposeText;
            private set => SetProperty(ref datasetPurposeText, value ?? string.Empty);
        }

        public string StatusText
        {
            get => statusText;
            private set => SetProperty(ref statusText, value ?? string.Empty);
        }

        public string StatusDetailText
        {
            get => statusDetailText;
            private set => SetProperty(ref statusDetailText, value ?? string.Empty);
        }

        public string MetricText
        {
            get => metricText;
            private set => SetProperty(ref metricText, value ?? string.Empty);
        }

        public string SourceIntegrityText
        {
            get => sourceIntegrityText;
            private set => SetProperty(ref sourceIntegrityText, value ?? string.Empty);
        }

        public string TargetIntegrityText
        {
            get => targetIntegrityText;
            private set => SetProperty(ref targetIntegrityText, value ?? string.Empty);
        }

        public bool CanApply
        {
            get => canApply;
            private set => SetProperty(ref canApply, value);
        }

        public ICommand BrowseSourceCommand { get; }

        public ICommand BrowseTargetCommand { get; }

        public ICommand BrowseImageRootCommand { get; }

        public ICommand DryRunCommand { get; }

        public ICommand ApplyCommand { get; }

        public void ConfigurePickers(
            Func<WpfDatasetInterchangeOption, string, string> sourcePicker,
            Func<WpfDatasetInterchangeOption, string, string> targetPicker,
            Func<string, string> imageRootPicker)
        {
            pickSource = sourcePicker ?? ((_, current) => current);
            pickTarget = targetPicker ?? ((_, current) => current);
            pickImageRoot = imageRootPicker ?? (current => current);
        }

        public void Refresh(CData sourceData)
        {
            data = sourceData;
            DatasetName = WpfDatasetContextPresentationService.BuildDatasetName(
                string.Empty,
                data?.OutputRootPath);
            LabelingDatasetPurpose purpose = data?.ProjectSettings?.DatasetPurpose
                ?? LabelingDatasetPurpose.ObjectDetection;
            DatasetPurposeText = WpfDatasetContextPresentationService.FormatPurposeName(purpose);
            WpfDatasetInterchangeOption preferred = Operations.FirstOrDefault(item =>
                !item.IsImport
                && string.Equals(item.Capability.DatasetPurpose, purpose.ToString(), StringComparison.Ordinal))
                ?? Operations.FirstOrDefault();
            SelectedOperation = preferred;
            InvalidateDryRun();
        }

        private void BrowseSource()
        {
            string selected = pickSource(SelectedOperation, SourcePath);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                SourcePath = selected;
            }
        }

        private void BrowseTarget()
        {
            string selected = pickTarget(SelectedOperation, TargetPath);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                TargetPath = selected;
            }
        }

        private void BrowseImageRoot()
        {
            string selected = pickImageRoot(ImageRoot);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                ImageRoot = selected;
            }
        }

        private void RunDryRun()
        {
            DatasetInterchangePreflightReport report = preflightService.DryRun(BuildRequest());
            lastDryRunSignature = BuildRequestSignature();
            ApplyReport(report);
            CanApply = report.CanApply;
        }

        private void Apply()
        {
            if (!CanApply || !string.Equals(lastDryRunSignature, BuildRequestSignature(), StringComparison.Ordinal))
            {
                InvalidateDryRun();
                return;
            }

            DatasetInterchangePreflightReport report = preflightService.Apply(BuildRequest());
            ApplyReport(report);
            CanApply = false;
        }

        private DatasetInterchangeRequest BuildRequest()
            => new DatasetInterchangeRequest
            {
                Data = data,
                FormatKey = SelectedOperation?.Capability.FormatKey ?? string.Empty,
                SourcePath = SourcePath,
                ImageRoot = ImageRoot,
                TargetPath = TargetPath,
                TargetSplit = TargetSplit
            };

        private string BuildRequestSignature()
            => string.Join(
                "|",
                SelectedOperation?.Capability.FormatKey ?? string.Empty,
                SourcePath,
                ImageRoot,
                TargetPath,
                TargetSplit,
                data?.OutputRootPath ?? string.Empty);

        private void ResetPathsForSelection()
        {
            string datasetRoot = data?.OutputRootPath ?? string.Empty;
            if (IsImport)
            {
                sourcePath = string.Empty;
                imageRoot = string.Empty;
                targetPath = datasetRoot;
            }
            else
            {
                sourcePath = datasetRoot;
                imageRoot = string.Empty;
                targetPath = string.Empty;
            }

            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(ImageRoot));
            OnPropertyChanged(nameof(TargetPath));
            InvalidateDryRun();
        }

        private void InvalidateDryRun()
        {
            lastDryRunSignature = string.Empty;
            CanApply = false;
            Findings.Clear();
            StatusText = "\uC0AC\uC804\uAC80\uC0AC \uB300\uAE30";
            StatusDetailText = "\uACBD\uB85C\uB098 \uD615\uC2DD\uC774 \uBC14\uB00C\uBA74 Dry-run\uC744 \uB2E4\uC2DC \uC2E4\uD589\uD574\uC57C \uD569\uB2C8\uB2E4.";
            MetricText = "\uC774\uBBF8\uC9C0 - \u00B7 \uC5B4\uB178\uD14C\uC774\uC158 - \u00B7 \uD074\uB798\uC2A4 -";
            SourceIntegrityText = "\uC6D0\uBCF8 \uBB34\uACB0\uC131: \uBBF8\uD655\uC778";
            TargetIntegrityText = "\uC694\uCCAD \uB300\uC0C1: \uBBF8\uD655\uC778";
        }

        private void ApplyReport(DatasetInterchangePreflightReport report)
        {
            Findings.Clear();
            foreach (string issue in report.Issues)
            {
                Findings.Add(new WpfDatasetInterchangeIssueItem(issue, isBlocking: true));
            }

            foreach (string warning in report.Warnings)
            {
                Findings.Add(new WpfDatasetInterchangeIssueItem(warning, isBlocking: false));
            }

            StatusText = TranslateStatus(report);
            StatusDetailText = report.DetailText;
            MetricText =
                $"\uC774\uBBF8\uC9C0 {report.ImageCount} \u00B7 \uC5B4\uB178\uD14C\uC774\uC158 {report.AnnotationCount} \u00B7 \uD074\uB798\uC2A4 {report.CategoryCount}";
            SourceIntegrityText = report.SourceUnchanged
                ? $"\uC6D0\uBCF8 \uBB34\uACB0\uC131: \uC720\uC9C0 \u00B7 {ShortFingerprint(report.SourceFingerprint)}"
                : "\uC6D0\uBCF8 \uBB34\uACB0\uC131: \uBCC0\uACBD \uAC10\uC9C0";
            TargetIntegrityText = report.IsDryRun
                ? report.RequestedTargetUnchanged
                    ? "\uC694\uCCAD \uB300\uC0C1: Dry-run \uC911 \uBCC0\uACBD \uC5C6\uC74C"
                    : "\uC694\uCCAD \uB300\uC0C1: \uBCC0\uACBD \uAC10\uC9C0"
                : "\uC694\uCCAD \uB300\uC0C1: \uC801\uC6A9 \uC644\uB8CC";
        }

        private static string TranslateStatus(DatasetInterchangePreflightReport report)
        {
            if (report.Issues.Count > 0)
            {
                return report.IsDryRun ? "Dry-run \uCC28\uB2E8" : "\uC801\uC6A9 \uC2E4\uD328";
            }

            if (!report.IsDryRun)
            {
                return "\uBCC0\uD658 \uC801\uC6A9 \uC644\uB8CC";
            }

            return report.Warnings.Count > 0
                ? "\uC8FC\uC758\uC0AC\uD56D \uD655\uC778 \uD6C4 \uC801\uC6A9 \uAC00\uB2A5"
                : "\uC801\uC6A9 \uAC00\uB2A5";
        }

        private static string ShortFingerprint(string value)
            => string.IsNullOrWhiteSpace(value) || value.Length <= 12
                ? value
                : value.Substring(0, 12);
    }
}
