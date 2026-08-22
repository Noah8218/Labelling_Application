using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfBatchExistingLabelPolicyOption
    {
        public WpfBatchExistingLabelPolicyOption(
            WpfBatchExistingLabelPolicy policy,
            string displayText,
            string detailText)
        {
            Policy = policy;
            DisplayText = displayText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
        }

        public WpfBatchExistingLabelPolicy Policy { get; }

        public string DisplayText { get; }

        public string DetailText { get; }
    }

    public sealed class WpfBatchPreflightFindingItem
    {
        public WpfBatchPreflightFindingItem(string text, bool isBlocking)
        {
            Text = text ?? string.Empty;
            IsBlocking = isBlocking;
        }

        public string Text { get; }

        public bool IsBlocking { get; }

        public string SeverityText => IsBlocking ? "\uCC28\uB2E8" : "\uC8FC\uC758";
    }

    public sealed class WpfBatchDetectionPreflightViewModel : WpfObservableViewModel
    {
        private readonly WpfBatchDetectionPreflightService preflightService;
        private CData data;
        private IReadOnlyList<WpfImageQueueItem> items = Array.Empty<WpfImageQueueItem>();
        private string scopeText = string.Empty;
        private WpfBatchExistingLabelPolicyOption selectedExistingLabelPolicy;
        private WpfBatchDetectionPreflightReport currentReport;
        private string statusText = "\uC0AC\uC804\uAC80\uC0AC \uB300\uAE30";
        private string countText = "\uC694\uCCAD 0 \u00B7 \uC2E4\uD589 0";
        private string modelText = string.Empty;
        private string classContractText = string.Empty;
        private string destinationPolicyText = string.Empty;

        public WpfBatchDetectionPreflightViewModel(
            CData data,
            IReadOnlyList<WpfImageQueueItem> items,
            string scopeText,
            WpfBatchDetectionPreflightService preflightService = null)
        {
            this.preflightService = preflightService ?? new WpfBatchDetectionPreflightService();
            ExistingLabelPolicies.Add(new WpfBatchExistingLabelPolicyOption(
                WpfBatchExistingLabelPolicy.SkipLabeled,
                "\uAE30\uC874 \uB77C\uBCA8 \uC774\uBBF8\uC9C0 \uC81C\uC678 (\uAD8C\uC7A5)",
                "\uC800\uC7A5\uB41C \uB77C\uBCA8\uC774 \uC788\uB294 \uC774\uBBF8\uC9C0\uB294 \uC2E4\uD589 \uB300\uC0C1\uC5D0\uC11C \uC81C\uC678\uD569\uB2C8\uB2E4."));
            ExistingLabelPolicies.Add(new WpfBatchExistingLabelPolicyOption(
                WpfBatchExistingLabelPolicy.IncludeAndKeep,
                "\uD3EC\uD568\uD558\uB418 \uAE30\uC874 \uB77C\uBCA8 \uBCF4\uC874",
                "\uBAA8\uB4E0 \uC774\uBBF8\uC9C0\uB97C \uAC80\uC0AC\uD558\uACE0 AI \uACB0\uACFC\uB9CC \uD6C4\uBCF4\uB85C \uC313\uC2B5\uB2C8\uB2E4."));
            RecheckCommand = new RelayCommand(RunDryRun);
            StartCommand = new RelayCommand(RequestStart, () => CanStart);
            Refresh(data, items, scopeText);
        }

        public event EventHandler<WpfBatchDetectionPlan> StartRequested;

        public ObservableCollection<WpfBatchExistingLabelPolicyOption> ExistingLabelPolicies { get; } =
            new ObservableCollection<WpfBatchExistingLabelPolicyOption>();

        public ObservableCollection<WpfBatchClassMappingItem> ClassMappings { get; } =
            new ObservableCollection<WpfBatchClassMappingItem>();

        public ObservableCollection<WpfBatchPreflightFindingItem> Findings { get; } =
            new ObservableCollection<WpfBatchPreflightFindingItem>();

        public WpfBatchExistingLabelPolicyOption SelectedExistingLabelPolicy
        {
            get => selectedExistingLabelPolicy;
            set
            {
                if (SetProperty(ref selectedExistingLabelPolicy, value))
                {
                    RunDryRun();
                }
            }
        }

        public string ScopeText => scopeText;

        public string StatusText
        {
            get => statusText;
            private set => SetProperty(ref statusText, value ?? string.Empty);
        }

        public string CountText
        {
            get => countText;
            private set => SetProperty(ref countText, value ?? string.Empty);
        }

        public string ModelText
        {
            get => modelText;
            private set => SetProperty(ref modelText, value ?? string.Empty);
        }

        public string ClassContractText
        {
            get => classContractText;
            private set => SetProperty(ref classContractText, value ?? string.Empty);
        }

        public string DestinationPolicyText
        {
            get => destinationPolicyText;
            private set => SetProperty(ref destinationPolicyText, value ?? string.Empty);
        }

        public bool CanStart => currentReport?.CanStart == true;

        public ICommand RecheckCommand { get; }

        public ICommand StartCommand { get; }

        public void Refresh(CData sourceData, IReadOnlyList<WpfImageQueueItem> sourceItems, string sourceScopeText)
        {
            data = sourceData;
            items = sourceItems ?? Array.Empty<WpfImageQueueItem>();
            scopeText = sourceScopeText ?? string.Empty;
            OnPropertyChanged(nameof(ScopeText));
            if (SelectedExistingLabelPolicy == null)
            {
                SelectedExistingLabelPolicy = ExistingLabelPolicies.First();
            }
            else
            {
                RunDryRun();
            }
        }

        private void RunDryRun()
        {
            currentReport = preflightService.DryRun(new WpfBatchDetectionPreflightRequest
            {
                Data = data,
                Items = items,
                ScopeText = scopeText,
                ExistingLabelPolicy = SelectedExistingLabelPolicy?.Policy
                    ?? WpfBatchExistingLabelPolicy.SkipLabeled
            });

            Findings.Clear();
            foreach (string issue in currentReport.Issues)
            {
                Findings.Add(new WpfBatchPreflightFindingItem(issue, isBlocking: true));
            }

            foreach (string warning in currentReport.Warnings)
            {
                Findings.Add(new WpfBatchPreflightFindingItem(warning, isBlocking: false));
            }

            ClassMappings.Clear();
            foreach (WpfBatchClassMappingItem mapping in currentReport.ClassMappings)
            {
                ClassMappings.Add(mapping);
            }

            StatusText = currentReport.CanStart
                ? currentReport.Warnings.Count > 0
                    ? "\uC8FC\uC758\uC0AC\uD56D \uD655\uC778 \uD6C4 \uC2E4\uD589 \uAC00\uB2A5"
                    : "\uC2E4\uD589 \uAC00\uB2A5"
                : "\uC2E4\uD589 \uCC28\uB2E8";
            CountText =
                $"\uC694\uCCAD {currentReport.RequestedCount} \u00B7 \uC2E4\uD589 {currentReport.RunnableItems.Count} \u00B7 "
                + $"\uAE30\uC874 \uB77C\uBCA8 {currentReport.ExistingLabelCount} \u00B7 \uC81C\uC678 {currentReport.SkippedExistingLabelCount}";
            ModelText =
                $"{currentReport.ModelEngineText} \u00B7 {currentReport.DatasetPurposeText} \u00B7 \uC2E0\uB8B0\uB3C4 {currentReport.ConfidenceText}"
                + Environment.NewLine
                + currentReport.WeightsPath;
            ClassContractText = ClassMappings.Count == 0
                ? "\uD074\uB798\uC2A4 \uB9E4\uD551 \uC5C6\uC74C"
                : $"Recipe \uD074\uB798\uC2A4 {ClassMappings.Count}\uAC1C \u00B7 worker className\uC744 \uB300\uC18C\uBB38\uC790 \uAD6C\uBD84 \uC5C6\uC774 \uB3D9\uC77C \uC774\uB984\uC73C\uB85C \uD574\uC11D";
            DestinationPolicyText = currentReport.DestinationPolicyText;
            OnPropertyChanged(nameof(CanStart));
            if (StartCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }

        private void RequestStart()
        {
            if (!CanStart)
            {
                return;
            }

            StartRequested?.Invoke(
                this,
                new WpfBatchDetectionPlan(
                    currentReport.RunnableItems,
                    currentReport.ScopeText,
                    SelectedExistingLabelPolicy.Policy));
        }
    }
}
