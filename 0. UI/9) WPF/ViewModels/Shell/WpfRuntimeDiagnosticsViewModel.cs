using OpenVisionLab.Mvvm;
using System;
using System.IO;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfRuntimeDiagnosticsViewModel : WpfObservableViewModel
    {
        private readonly WpfRuntimeDiagnosticsService diagnosticsService;
        private string statusTitleText = "환경 점검 전";
        private string statusDetailText = "지원 자료는 버튼을 눌러야 생성되며 이미지·라벨·가중치는 제외됩니다.";
        private bool isBusy;

        public WpfRuntimeDiagnosticsViewModel()
            : this(new WpfRuntimeDiagnosticsService())
        {
        }

        internal WpfRuntimeDiagnosticsViewModel(WpfRuntimeDiagnosticsService diagnosticsService)
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

        private void RunSelfTest()
        {
            Execute(
                () =>
                {
                    WpfRuntimeSelfTestResult result = diagnosticsService.RunSelfTest();
                    StatusTitleText = result.FailedCount == 0
                        ? $"환경 점검 완료 · 통과 {result.PassedCount}"
                        : $"환경 점검 필요 · 실패 {result.FailedCount}";
                    StatusDetailText = result.WarningCount > 0
                        ? $"경고 {result.WarningCount}건 · 학습/추론은 실행하지 않았습니다."
                        : "필수 파일과 사용자 쓰기 경로가 정상입니다. 학습/추론은 실행하지 않았습니다.";
                });
        }

        private void CreateSupportBundle()
        {
            Execute(
                () =>
                {
                    WpfSupportBundleResult result = diagnosticsService.CreateSupportBundle();
                    StatusTitleText = "지원 자료 생성 완료";
                    StatusDetailText =
                        $"{Path.GetFileName(result.ArchivePath)} · 이미지/라벨/가중치/자격 증명 제외";
                });
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
