using MvcVisionSystem._1._Core;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfEnvironmentSetupCenterItem
    {
        public WpfEnvironmentSetupCenterItem(
            string categoryText,
            string nameText,
            string requirementText,
            string statusText,
            string detailText,
            string nextActionText,
            bool isReady,
            bool isWarning,
            bool isRequired)
        {
            CategoryText = categoryText ?? string.Empty;
            NameText = nameText ?? string.Empty;
            RequirementText = requirementText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            NextActionText = nextActionText ?? string.Empty;
            IsReady = isReady;
            IsWarning = isWarning;
            IsRequired = isRequired;
        }

        public string CategoryText { get; }
        public string NameText { get; }
        public string RequirementText { get; }
        public string StatusText { get; }
        public string DetailText { get; }
        public string NextActionText { get; }
        public bool IsReady { get; }
        public bool IsWarning { get; }
        public bool IsRequired { get; }
    }

    public sealed class WpfEnvironmentSetupCenterViewModel : WpfObservableViewModel
    {
        private readonly WpfRuntimeDiagnosticsService diagnosticsService;
        private readonly Func<PythonModelSettings> pythonSettingsProvider;
        private readonly Action openModelSettingsAction;
        private string overallStatusText = "환경 확인 전";
        private string overallDetailText = "앱과 모델 실행 유틸리티를 읽기 전용으로 확인합니다.";
        private string selectedRuntimeText = "선택된 모델 실행기: 미설정";
        private string lastCheckedText = "마지막 확인: 없음";
        private int readyCount;
        private int attentionCount;
        private int optionalCount;
        private bool isBusy;

        public WpfEnvironmentSetupCenterViewModel()
            : this(
                new WpfRuntimeDiagnosticsService(),
                () => CGlobal.Inst.Data.ProjectSettings?.PythonModel ?? new PythonModelSettings(),
                null)
        {
        }

        public WpfEnvironmentSetupCenterViewModel(
            WpfRuntimeDiagnosticsService diagnosticsService,
            Func<PythonModelSettings> pythonSettingsProvider,
            Action openModelSettingsAction)
        {
            this.diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            this.pythonSettingsProvider = pythonSettingsProvider ?? throw new ArgumentNullException(nameof(pythonSettingsProvider));
            this.openModelSettingsAction = openModelSettingsAction;
            RefreshCommand = new RelayCommand(Refresh, () => !IsBusy);
            OpenModelSettingsCommand = new RelayCommand(OpenModelSettings, () => !IsBusy && this.openModelSettingsAction != null);
            Refresh();
        }

        public ObservableCollection<WpfEnvironmentSetupCenterItem> Items { get; }
            = new ObservableCollection<WpfEnvironmentSetupCenterItem>();

        public ICommand RefreshCommand { get; }
        public ICommand OpenModelSettingsCommand { get; }

        public string OverallStatusText
        {
            get => overallStatusText;
            private set => SetProperty(ref overallStatusText, value);
        }

        public string OverallDetailText
        {
            get => overallDetailText;
            private set => SetProperty(ref overallDetailText, value);
        }

        public string SelectedRuntimeText
        {
            get => selectedRuntimeText;
            private set => SetProperty(ref selectedRuntimeText, value);
        }

        public string LastCheckedText
        {
            get => lastCheckedText;
            private set => SetProperty(ref lastCheckedText, value);
        }

        public int ReadyCount
        {
            get => readyCount;
            private set => SetProperty(ref readyCount, value);
        }

        public int AttentionCount
        {
            get => attentionCount;
            private set => SetProperty(ref attentionCount, value);
        }

        public int OptionalCount
        {
            get => optionalCount;
            private set => SetProperty(ref optionalCount, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string InstallGuideText
            => "1. Python이 없으면 Python 3.11 x64를 설치하고 전용 venv의 Scripts\\python.exe를 연결합니다.\n"
                + "2. 모델 실행기 설정에서 YOLOv8/YOLO11, U-Net 또는 PatchCore 프로필을 선택합니다.\n"
                + "3. 표시된 대상 venv와 설치 명령을 확인한 뒤 설치 버튼을 직접 누릅니다.\n"
                + "4. 다시 점검한 뒤 학습 또는 현재 검사를 명시적으로 실행합니다.";

        public string SafetyBoundaryText
            => "이 화면을 열거나 새로고침해도 설치·제거·학습·추론·모델 적용·설정 저장은 실행되지 않습니다. "
                + "GPU 드라이버와 CUDA는 하드웨어·버전·재부팅 영향이 있어 자동 설치하지 않습니다.";

        public void Refresh()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                Items.Clear();
                WpfRuntimeSelfTestResult applicationReport = diagnosticsService.RunReadOnlySelfTest();
                AddApplicationItems(applicationReport);

                PythonModelSettings settings = pythonSettingsProvider() ?? new PythonModelSettings();
                PythonModelRuntimeSelfTestReport runtimeReport = PythonModelRuntimeSelfTestService.BuildReport(settings);
                AddRuntimeItems(runtimeReport);
                AddEngineSpecificUtilityItems(settings);
                AddOptionalUtilityItems();

                SelectedRuntimeText = "선택된 모델 실행기: " + FormatEngine(settings.ModelEngine);
                ReadyCount = Items.Count(item => item.IsReady);
                AttentionCount = Items.Count(item => !item.IsReady && item.IsRequired);
                OptionalCount = Items.Count(item => !item.IsRequired);
                OverallStatusText = AttentionCount > 0
                    ? $"설정이 필요한 필수 항목 {AttentionCount}개"
                    : Items.Any(item => !item.IsReady)
                        ? "필수 환경 준비됨 · 선택 항목 확인 가능"
                        : "환경 준비 완료";
                OverallDetailText = AttentionCount > 0
                    ? "아래의 다음 조치를 순서대로 확인하세요. 점검 중 설치나 모델 실행은 시작하지 않았습니다."
                    : "현재 필수 항목은 준비되어 있습니다. 필요한 모델 기능만 선택해 설정할 수 있습니다.";
                LastCheckedText = "마지막 확인: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                OverallStatusText = "환경 목록 생성 실패";
                OverallDetailText = ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenModelSettings()
        {
            openModelSettingsAction?.Invoke();
        }

        private void AddApplicationItems(WpfRuntimeSelfTestResult report)
        {
            foreach (WpfRuntimeSelfTestCheck check in report?.Checks ?? Array.Empty<WpfRuntimeSelfTestCheck>())
            {
                bool ready = string.Equals(check.Status, "pass", StringComparison.Ordinal);
                bool warning = string.Equals(check.Status, "warning", StringComparison.Ordinal);
                Items.Add(new WpfEnvironmentSetupCenterItem(
                    "앱 기본 환경",
                    FormatApplicationCheckName(check.Name),
                    warning ? "권장" : "필수",
                    ready ? "준비됨" : warning ? "확인 필요" : "조치 필요",
                    check.Detail,
                    ready ? "추가 조치 없음" : FormatApplicationNextAction(check.Name),
                    ready,
                    warning,
                    isRequired: !warning));
            }
        }

        private void AddRuntimeItems(PythonModelRuntimeSelfTestReport report)
        {
            foreach (PythonModelRuntimeSelfTestItem item in report?.Items ?? Array.Empty<PythonModelRuntimeSelfTestItem>())
            {
                Items.Add(new WpfEnvironmentSetupCenterItem(
                    "모델 실행 유틸리티",
                    item.LabelText,
                    item.IsWarning ? "기능 사용 시" : "필수",
                    item.IsPassed ? "준비됨" : item.IsWarning ? "확인 필요" : "설정 필요",
                    item.DetailText,
                    item.IsPassed ? "추가 조치 없음" : FormatRuntimeNextAction(item.LabelText),
                    item.IsPassed,
                    item.IsWarning,
                    isRequired: !item.IsWarning));
            }
        }

        private void AddOptionalUtilityItems()
        {
            Items.Add(new WpfEnvironmentSetupCenterItem(
                "선택 유틸리티",
                "GPU 가속 드라이버 / CUDA",
                "선택",
                "필요할 때 확인",
                "CPU 실행은 가능하며 GPU 학습·추론을 사용할 때만 호환 드라이버와 PyTorch CUDA 조합이 필요합니다.",
                "GPU 사용이 필요하면 모델 실행기 설정과 그래픽 진단 결과를 확인한 뒤 제조사 드라이버를 별도로 설치하세요.",
                isReady: false,
                isWarning: true,
                isRequired: false));
        }

        private void AddEngineSpecificUtilityItems(PythonModelSettings settings)
        {
            if (!string.Equals(
                PythonModelSettings.NormalizeModelEngine(settings?.ModelEngine),
                PythonModelSettings.EngineYoloV5,
                StringComparison.Ordinal))
            {
                return;
            }

            string projectRoot = settings?.ProjectRootPath?.Trim() ?? string.Empty;
            string[] requiredRelativePaths =
            {
                "hubconf.py",
                "train.py",
                "detect.py",
                Path.Combine("models", "common.py")
            };
            var missing = new List<string>();
            foreach (string relativePath in requiredRelativePaths)
            {
                if (string.IsNullOrWhiteSpace(projectRoot)
                    || !File.Exists(Path.Combine(projectRoot, relativePath)))
                {
                    missing.Add(relativePath.Replace('\\', '/'));
                }
            }

            bool ready = missing.Count == 0;
            Items.Add(new WpfEnvironmentSetupCenterItem(
                "모델 실행 유틸리티",
                "YOLOv5 실행 파일 묶음",
                "필수",
                ready ? "준비됨" : "복구 필요",
                ready
                    ? "hubconf.py, train.py, detect.py, models/common.py 확인"
                    : "누락: " + string.Join(", ", missing),
                ready
                    ? "추가 조치 없음"
                    : "완전한 YOLOv5 저장소를 복구하거나 다른 준비된 모델 프로필을 선택하세요.",
                ready,
                isWarning: false,
                isRequired: true));
        }

        private static string FormatApplicationCheckName(string name)
            => name switch
            {
                "productIdentity" => "제품 버전",
                "productBinary" => "제품 필수 파일",
                "applicationExecutable" => "실행 파일",
                "releaseManifest" => "배포 파일 무결성",
                "diagnosticsPath" => "진단 저장 경로",
                "supportBundlePath" => "지원 자료 저장 경로",
                "logIsolation" => "로그 경로 분리",
                WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName => "이미지 뷰어 그래픽",
                _ => name ?? string.Empty
            };

        private static string FormatApplicationNextAction(string name)
            => name switch
            {
                "productBinary" or "applicationExecutable" => "검증된 설치/휴대형 패키지로 제품 파일을 복구하세요.",
                "releaseManifest" => "배포 폴더의 release-manifest.json과 원본 패키지를 다시 확인하세요.",
                "diagnosticsPath" or "supportBundlePath" or "logIsolation" => "사용자 저장 경로의 권한과 남은 디스크 공간을 확인하세요.",
                WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName => "그래픽 드라이버와 지원 GPU 환경을 확인한 뒤 앱에서 다시 점검하세요.",
                _ => "지원 자료를 만든 뒤 설치 파일과 환경을 다시 확인하세요."
            };

        private static string FormatRuntimeNextAction(string label)
            => label switch
            {
                "Python" => "Python 3.11 x64 설치 후 전용 venv의 Scripts\\python.exe를 모델 실행기 설정에서 연결하세요.",
                "Ultralytics" or "PyTorch U-Net" or "PyTorch PatchCore" => "모델 실행기 설정에서 대상 venv와 명령을 확인한 뒤 설치 버튼을 직접 누르세요.",
                "검사 모델" => "학습 결과를 검사 모델로 저장하거나 사용할 .pt/.onnx 파일을 선택하세요.",
                "이미지" => "검사할 이미지 폴더를 선택하세요. 라벨링만 할 때는 나중에 설정할 수 있습니다.",
                _ => "모델 실행기 설정에서 경로와 선택 프로필을 확인하세요."
            };

        private static string FormatEngine(string engine)
            => PythonModelSettings.NormalizeModelEngine(engine) switch
            {
                PythonModelSettings.EngineYoloV5 => "YOLOv5",
                PythonModelSettings.EngineYoloV8 => "YOLOv8",
                PythonModelSettings.EngineYolo11 => "YOLO11",
                PythonModelSettings.EngineUnet => "U-Net",
                PythonModelSettings.EnginePatchCore => "PatchCore",
                PythonModelSettings.EngineOnnx => "ONNX",
                _ => "미설정"
            };
    }
}
