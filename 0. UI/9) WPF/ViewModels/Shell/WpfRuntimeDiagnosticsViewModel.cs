using OpenVisionLab.Mvvm;
using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfRuntimeDiagnosticsViewModel : WpfObservableViewModel
    {
        private readonly WpfRuntimeDiagnosticsService diagnosticsService;
        private string statusTitleText = "환경 점검 전";
        private string statusDetailText = "지원 자료는 버튼을 눌러야 생성되며 이미지·라벨·가중치는 제외됩니다.";
        private WpfRuntimeSelfTestCheck lastGraphicsCapabilityCheck;
        private bool isBusy;

        public WpfRuntimeDiagnosticsViewModel()
            : this(new WpfRuntimeDiagnosticsService())
        {
        }

        public WpfRuntimeDiagnosticsViewModel(WpfRuntimeDiagnosticsService diagnosticsService)
        {
            this.diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            RunSelfTestCommand = new RelayCommand(RunSelfTest, () => !IsBusy);
            CreateSupportBundleCommand = new RelayCommand(CreateSupportBundle, () => !IsBusy);
        }

        public ICommand RunSelfTestCommand { get; }
        public ICommand CreateSupportBundleCommand { get; }

        public string StatusTitleText
        {
            get => statusTitleText;
            private set => SetProperty(ref statusTitleText, value);
        }

        public string StatusDetailText
        {
            get => statusDetailText;
            private set => SetProperty(ref statusDetailText, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        internal WpfRuntimeDiagnosticsService DiagnosticsService => diagnosticsService;

        public void AttachGraphicsCapabilityProvider(Func<WpfRuntimeSelfTestCheck> provider)
        {
            diagnosticsService.SetGraphicsCapabilityProvider(provider);
            lastGraphicsCapabilityCheck = null;
        }

        public bool EnsureViewerReadyForImageLoad(out string detail)
        {
            WpfRuntimeSelfTestCheck check = lastGraphicsCapabilityCheck
                ?? diagnosticsService.RunGraphicsCapabilityCheck();
            if (!string.Equals(check.Status, "warning", StringComparison.Ordinal))
            {
                lastGraphicsCapabilityCheck = check;
            }

            detail = check.Detail;
            if (!string.Equals(check.Status, "fail", StringComparison.Ordinal))
            {
                return true;
            }

            StatusTitleText = "이미지 뷰어 환경 확인 필요";
            StatusDetailText = check.Detail;
            return false;
        }

        private void RunSelfTest()
        {
            Execute(
                () =>
                {
                    WpfRuntimeSelfTestResult result = diagnosticsService.RunSelfTest();
                    RememberGraphicsCapability(result.Checks.FirstOrDefault(check =>
                        string.Equals(
                            check.Name,
                            WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                            StringComparison.Ordinal)));
                    ApplySelfTestResult(result);
                });
        }

        private void CreateSupportBundle()
        {
            Execute(
                () =>
                {
                    WpfSupportBundleResult result = diagnosticsService.CreateSupportBundle();
                    RememberGraphicsCapability(result.SelfTest?.Checks.FirstOrDefault(check =>
                        string.Equals(
                            check.Name,
                            WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                            StringComparison.Ordinal)));
                    StatusTitleText = result.SelfTest?.FailedCount > 0
                        ? $"지원 자료 생성 완료 · 환경 실패 {result.SelfTest.FailedCount}"
                        : "지원 자료 생성 완료";
                    StatusDetailText =
                        $"{Path.GetFileName(result.ArchivePath)} · 이미지/라벨/가중치/자격 증명 제외";
                });
        }

        private void RememberGraphicsCapability(WpfRuntimeSelfTestCheck check)
        {
            lastGraphicsCapabilityCheck = check != null
                && !string.Equals(check.Status, "warning", StringComparison.Ordinal)
                    ? check
                    : null;
        }

        private void ApplySelfTestResult(WpfRuntimeSelfTestResult result)
        {
            WpfRuntimeSelfTestCheck failure = result.Checks.FirstOrDefault(check =>
                string.Equals(check.Status, "fail", StringComparison.Ordinal));
            WpfRuntimeSelfTestCheck warning = result.Checks.FirstOrDefault(check =>
                string.Equals(check.Status, "warning", StringComparison.Ordinal));
            WpfRuntimeSelfTestCheck graphics = result.Checks.FirstOrDefault(check =>
                string.Equals(
                    check.Name,
                    WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                    StringComparison.Ordinal));

            if (failure != null)
            {
                StatusTitleText = $"환경 점검 필요 · 실패 {result.FailedCount}";
                StatusDetailText = failure.Detail + " · 학습/추론은 실행하지 않았습니다.";
                return;
            }

            StatusTitleText = $"환경 점검 완료 · 통과 {result.PassedCount}";
            StatusDetailText = warning != null
                ? $"{graphics?.Detail} · 경고 {result.WarningCount}건: {warning.Detail} · 학습/추론은 실행하지 않았습니다."
                : "필수 파일, 사용자 쓰기 경로, 이미지 뷰어 그래픽이 정상입니다. 학습/추론은 실행하지 않았습니다.";
        }

        private void Execute(Action action)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                StatusTitleText = "진단 작업 실패";
                StatusDetailText = ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
