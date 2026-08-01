using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using OpenVisionLab.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LabelingApplication.Tests;

internal static class RuntimeDiagnosticsContractTests
{
    internal static void TestHeadlessEnvironmentSelfTestCli()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            "headless-environment-self-test-" + Guid.NewGuid().ToString("N"));
        string applicationDataRoot = Path.Combine(root, "app-data");

        try
        {
            Directory.CreateDirectory(Path.Combine(applicationDataRoot, "Diagnostics"));
            Directory.CreateDirectory(Path.Combine(applicationDataRoot, "SupportBundles"));
            string executablePath = Path.Combine(AppContext.BaseDirectory, "OpenVisionLab.LabelingStudio.exe");
            AssertTrue(File.Exists(executablePath), "headless CLI test requires the built product executable");

            string[] filesBefore = Directory.GetFiles(applicationDataRoot, "*", SearchOption.AllDirectories);
            (int exitCode, string output, string error) = RunHeadlessCli(
                executablePath,
                applicationDataRoot,
                WpfHeadlessRuntimeCommandService.EnvironmentSelfTestArgument,
                WpfHeadlessRuntimeCommandService.JsonArgument);

            AssertTrue(
                exitCode == WpfHeadlessRuntimeCommandService.SuccessExitCode,
                "successful headless CLI should return exit code 0");
            AssertTrue(string.IsNullOrEmpty(error), "successful headless CLI should not write stderr");
            using (JsonDocument document = JsonDocument.Parse(output))
            {
                JsonElement result = document.RootElement;
                AssertTrue(result.GetProperty("schemaVersion").GetInt32() == 1, "headless CLI schema should be version 1");
                AssertTrue(string.Equals(result.GetProperty("command").GetString(), "environment-self-test", StringComparison.Ordinal), "headless CLI should identify the requested command");
                AssertTrue(string.Equals(result.GetProperty("mode").GetString(), "read-only", StringComparison.Ordinal), "headless CLI should disclose read-only mode");
                AssertTrue(!result.GetProperty("durableWrites").GetBoolean(), "headless self-test must declare no durable writes");
                AssertTrue(result.GetProperty("counts").GetProperty("failed").GetInt32() == 0, "isolated headless CLI should have no failed checks");
                AssertTrue(
                    result.GetProperty("checks").EnumerateArray().Any(check =>
                        string.Equals(
                            check.GetProperty("name").GetString(),
                            WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                            StringComparison.Ordinal)
                        && string.Equals(check.GetProperty("status").GetString(), "warning", StringComparison.Ordinal)),
                    "headless self-test should disclose that viewer graphics requires the UI context");
            }

            string[] filesAfter = Directory.GetFiles(applicationDataRoot, "*", SearchOption.AllDirectories);
            AssertTrue(filesBefore.Length == filesAfter.Length, "headless self-test must not add files below the application-data root");
            AssertTrue(
                !File.Exists(Path.Combine(applicationDataRoot, "Diagnostics", "self-test-latest.json")),
                "headless read-only self-test must not persist the latest self-test file");

            (int invalidExitCode, string invalidOutput, string invalidError) = RunHeadlessCli(
                executablePath,
                applicationDataRoot,
                WpfHeadlessRuntimeCommandService.EnvironmentSelfTestArgument);
            AssertTrue(
                invalidExitCode == WpfHeadlessRuntimeCommandService.InvalidArgumentsExitCode,
                "invalid headless CLI arguments should return exit code 64");
            AssertTrue(string.IsNullOrEmpty(invalidError), "headless usage errors should remain machine-readable on stdout");
            using JsonDocument invalidDocument = JsonDocument.Parse(invalidOutput);
            AssertTrue(
                string.Equals(
                    invalidDocument.RootElement.GetProperty("error").GetProperty("code").GetString(),
                    "invalid-arguments",
                    StringComparison.Ordinal),
                "invalid headless CLI arguments should return a stable machine-readable error code");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static (int ExitCode, string Output, string Error) RunHeadlessCli(
        string executablePath,
        string applicationDataRoot,
        params string[] arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment[WpfRuntimeDiagnosticsService.ApplicationDataRootEnvironmentVariable] = applicationDataRoot;
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("headless CLI process did not start");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        AssertTrue(process.WaitForExit(15000), "headless CLI did not exit within 15 seconds");
        AssertTrue(process.MainWindowHandle == IntPtr.Zero, "headless CLI must not open a product window");
        return (process.ExitCode, output, error);
    }

    internal static void TestRuntimeDiagnosticsAndSupportBundleContract()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            "runtime-diagnostics-" + Guid.NewGuid().ToString("N"));
        string applicationRoot = Path.Combine(root, "application");
        string applicationDataRoot = Path.Combine(root, "app-data");

        try
        {
            Directory.CreateDirectory(applicationRoot);
            Directory.CreateDirectory(applicationDataRoot);
            File.WriteAllText(
                Path.Combine(applicationRoot, "OpenVisionLab.LabelingStudio.exe"),
                "test-host-placeholder",
                Encoding.UTF8);

            var service = new WpfRuntimeDiagnosticsService(applicationRoot, applicationDataRoot);
            service.SetGraphicsCapabilityProvider(() => new WpfRuntimeSelfTestCheck(
                WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                "pass",
                "이미지 뷰어 사용 가능 · test renderer"));
            WpfRuntimeDiagnosticsPaths paths = service.Paths;
            paths.EnsureDirectories();

            string latestStartup = Path.Combine(paths.DiagnosticsDirectory, "startup-current.json");
            File.WriteAllText(
                latestStartup,
                """
                {
                  "eventName": "application-startup",
                  "dataset": "C:\\private\\dataset\\part-001.png",
                  "token": "startup-secret"
                }
                """,
                Encoding.UTF8);

            string logDirectory = Path.Combine(paths.LogDirectory, "2026", "07", "30");
            Directory.CreateDirectory(logDirectory);
            File.WriteAllText(
                Path.Combine(logDirectory, "All.log"),
                """
                startup ok
                image=C:\private\dataset\source-image.png
                token=raw-secret-value
                password: raw-password-value
                """,
                Encoding.UTF8);
            OVLog.ApplyFilePolicy(
                paths.LogDirectory,
                maxBackupFileCount: 10,
                maximumFileSizeInMB: 20);
            OVLog.Write("support-bundle-live-log token=live-log-secret");

            File.WriteAllBytes(Path.Combine(applicationDataRoot, "source-image.jpg"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(applicationDataRoot, "labels.txt"), "0 0.5 0.5 0.2 0.2", Encoding.UTF8);
            File.WriteAllBytes(Path.Combine(applicationDataRoot, "weights.pt"), new byte[] { 4, 5, 6 });
            File.WriteAllText(
                Path.Combine(applicationDataRoot, "runtime-config.json"),
                """{"apiKey":"raw-config-secret","dataset":"C:\\private\\dataset"}""",
                Encoding.UTF8);

            for (int index = 0; index < WpfRuntimeDiagnosticsService.MaximumDiagnosticFiles + 8; index++)
            {
                string path = Path.Combine(paths.DiagnosticsDirectory, $"historical-{index:D3}.json");
                File.WriteAllText(path, "{}", Encoding.UTF8);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-(index + 1)));
            }

            for (int index = 0; index < WpfRuntimeDiagnosticsService.MaximumSupportBundleFiles + 5; index++)
            {
                string path = Path.Combine(paths.SupportBundlesDirectory, $"historical-{index:D3}.zip");
                File.WriteAllText(path, "placeholder", Encoding.UTF8);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-(index + 1)));
            }

            WpfRuntimeSelfTestResult selfTest = service.RunSelfTest();
            AssertTrue(selfTest.FailedCount == 0, "environment self-test should have no failed checks in an isolated writable root");
            AssertTrue(
                selfTest.Checks.Any(check => string.Equals(check.Name, "logIsolation", StringComparison.Ordinal)
                    && string.Equals(check.Status, "pass", StringComparison.Ordinal)),
                "self-test should prove that logs are outside the application folder");
            AssertTrue(
                selfTest.Checks.Any(check =>
                    string.Equals(check.Name, WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName, StringComparison.Ordinal)
                    && string.Equals(check.Status, "pass", StringComparison.Ordinal)),
                "self-test should include the connected image-viewer graphics capability");
            AssertTrue(File.Exists(paths.LatestSelfTestPath), "explicit self-test should persist its structured result");

            string missingDatasetRoot = Path.Combine(root, "not-created-by-readiness", "dataset");
            var data = new CData();
            data.ConfigureOutputRoot(missingDatasetRoot);
            data.ClassNamedList.Add(new CClassItem { Text = "Defect" });
            YoloDatasetValidator.ValidateConfiguration(data);
            AssertTrue(
                !Directory.Exists(missingDatasetRoot),
                "read-only dataset readiness validation must not create the configured output root");

            WpfSupportBundleResult result = service.CreateSupportBundle();
            AssertTrue(File.Exists(result.ArchivePath), "explicit support bundle export should create an archive");
            AssertTrue(
                result.IncludedEntries.Any(entry => entry.StartsWith("logs/", StringComparison.Ordinal)),
                "support bundle should include a sanitized log while the logging appenders are active");
            AssertTrue(
                result.SkippedLogs.All(entry => !entry.Contains("IOException", StringComparison.Ordinal)),
                "support bundle should not lose active logs to exclusive file locking");
            AssertTrue(
                Directory.EnumerateFiles(paths.DiagnosticsDirectory, "*.json").Count()
                    <= WpfRuntimeDiagnosticsService.MaximumDiagnosticFiles,
                "diagnostic retention should keep a bounded file count");
            AssertTrue(
                Directory.EnumerateFiles(paths.SupportBundlesDirectory, "*.zip").Count()
                    <= WpfRuntimeDiagnosticsService.MaximumSupportBundleFiles,
                "support bundle retention should keep a bounded file count");

            using ZipArchive archive = ZipFile.OpenRead(result.ArchivePath);
            string[] entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();
            foreach (string requiredEntry in new[]
            {
                "support-manifest.json",
                "self-test.json",
                "config-summary.json"
            })
            {
                AssertTrue(entryNames.Contains(requiredEntry, StringComparer.Ordinal), $"support archive is missing {requiredEntry}");
            }

            AssertTrue(
                entryNames.All(name =>
                    !name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".pt", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("runtime-config", StringComparison.OrdinalIgnoreCase)),
                "allow-list export must exclude images, labels, weights, and raw runtime configuration");

            string archiveText = string.Join(
                Environment.NewLine,
                archive.Entries.Select(ReadEntryText));
            foreach (string forbiddenValue in new[]
            {
                "raw-secret-value",
                "raw-password-value",
                "raw-config-secret",
                "startup-secret",
                "live-log-secret",
                @"C:\private\dataset"
            })
            {
                AssertTrue(
                    !archiveText.Contains(forbiddenValue, StringComparison.OrdinalIgnoreCase),
                    $"support archive leaked sensitive value: {forbiddenValue}");
            }

            AssertTrue(
                archiveText.Contains("allow-list", StringComparison.Ordinal)
                && archiveText.Contains("dataset images", StringComparison.Ordinal)
                && archiveText.Contains("model weights", StringComparison.Ordinal),
                "support manifest should disclose its allow-list and default exclusions");

            JsonDocument selfTestDocument = JsonDocument.Parse(ReadEntryText(
                archive.GetEntry("self-test.json")
                ?? throw new InvalidOperationException("self-test.json was not reopenable")));
            AssertTrue(
                selfTestDocument.RootElement.GetProperty("checks").GetArrayLength() >= 6,
                "reopened self-test should retain the environment checks");

            VerifyGraphicsCapabilityFailurePresentation(applicationRoot, root);
            VerifyShellSurface();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void VerifyGraphicsCapabilityFailurePresentation(string applicationRoot, string root)
    {
        string failedApplicationDataRoot = Path.Combine(root, "failed-graphics-app-data");
        var service = new WpfRuntimeDiagnosticsService(applicationRoot, failedApplicationDataRoot);
        service.SetGraphicsCapabilityProvider(() => new WpfRuntimeSelfTestCheck(
            WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
            "fail",
            "이미지 뷰어 사용 불가 · glGenFramebuffersEXT"));
        var viewModel = new WpfRuntimeDiagnosticsViewModel(service);

        viewModel.RunSelfTestCommand.Execute(null);

        AssertTrue(
            viewModel.StatusTitleText.Contains("실패 1", StringComparison.Ordinal),
            "failed graphics capability should be visible in the environment-check title");
        AssertTrue(
            viewModel.StatusDetailText.Contains("glGenFramebuffersEXT", StringComparison.Ordinal),
            "failed graphics capability should retain the actionable missing-function detail");
        AssertTrue(
            !viewModel.EnsureViewerReadyForImageLoad(out string detail)
            && detail.Contains("glGenFramebuffersEXT", StringComparison.Ordinal),
            "failed graphics capability should block image loading before viewer execution");

        var warningService = new WpfRuntimeDiagnosticsService(
            applicationRoot,
            Path.Combine(root, "pending-graphics-app-data"));
        warningService.SetGraphicsCapabilityProvider(() => new WpfRuntimeSelfTestCheck(
            WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
            "warning",
            "이미지 뷰어 그래픽 컨텍스트가 아직 준비되지 않았습니다."));
        var warningViewModel = new WpfRuntimeDiagnosticsViewModel(warningService);
        AssertTrue(
            warningViewModel.EnsureViewerReadyForImageLoad(out _),
            "a not-yet-created viewer context should not block headless construction or startup restore");

        int retriableProbeCount = 0;
        var retriableService = new WpfRuntimeDiagnosticsService(
            applicationRoot,
            Path.Combine(root, "retriable-graphics-app-data"));
        retriableService.SetGraphicsCapabilityProvider(() =>
        {
            retriableProbeCount++;
            return retriableProbeCount == 1
                ? new WpfRuntimeSelfTestCheck(
                    WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                    "warning",
                    "이미지 뷰어 그래픽 컨텍스트가 아직 준비되지 않았습니다.")
                : new WpfRuntimeSelfTestCheck(
                    WpfRuntimeDiagnosticsService.ViewerGraphicsCheckName,
                    "fail",
                    "이미지 뷰어 사용 불가 · glGenFramebuffersEXT");
        });
        var retriableViewModel = new WpfRuntimeDiagnosticsViewModel(retriableService);
        retriableViewModel.RunSelfTestCommand.Execute(null);
        AssertTrue(
            !retriableViewModel.EnsureViewerReadyForImageLoad(out _)
            && retriableProbeCount == 2,
            "a warning captured before the viewer is ready should be re-probed before image loading");
    }

    private static void VerifyShellSurface()
    {
        string repoRoot = FindRepositoryRoot();
        string xaml = File.ReadAllText(
            Path.Combine(repoRoot, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.xaml"));
        foreach (string requiredToken in new[]
        {
            "HeaderToolsMenuSupportSection",
            "RuntimeSelfTestButton",
            "CreateSupportBundleButton",
            "RuntimeDiagnosticsStatusCard",
            "RuntimeDiagnosticsViewModel.RunSelfTestCommand",
            "RuntimeDiagnosticsViewModel.CreateSupportBundleCommand",
            "이미지 뷰어 그래픽"
        })
        {
            AssertTrue(xaml.Contains(requiredToken, StringComparison.Ordinal), $"diagnostics UI token missing: {requiredToken}");
        }

        string graphicsProbeSource = File.ReadAllText(
            Path.Combine(
                repoRoot,
                "0. UI",
                "9) WPF",
                "Services",
                "Runtime",
                "WpfOpenGlRuntimeCapabilityProbe.cs"));
        foreach (string requiredFunction in new[]
        {
            "glGenFramebuffersEXT",
            "glBindFramebufferEXT",
            "glFramebufferTexture2DEXT",
            "glCheckFramebufferStatusEXT",
            "glDeleteFramebuffersEXT",
            "glGenRenderbuffersEXT",
            "glBindRenderbufferEXT",
            "glRenderbufferStorageEXT",
            "glFramebufferRenderbufferEXT",
            "glDeleteRenderbuffersEXT",
            "glGenerateMipmapEXT"
        })
        {
            AssertTrue(
                graphicsProbeSource.Contains(requiredFunction, StringComparison.Ordinal),
                $"graphics capability probe should require {requiredFunction}");
        }

        string imageLoadingSource = File.ReadAllText(
            Path.Combine(
                repoRoot,
                "0. UI",
                "9) WPF",
                "Views",
                "WpfLabelingShellWindow.ImageLoading.cs"));
        AssertTrue(
            imageLoadingSource.Contains("EnsureViewerReadyForImageLoad", StringComparison.Ordinal)
            && imageLoadingSource.Contains("이미지 열기 차단", StringComparison.Ordinal)
            && imageLoadingSource.Contains("지원 자료", StringComparison.Ordinal),
            "the central image-loading path should fail closed with actionable graphics guidance");

        string systemSource = File.ReadAllText(
            Path.Combine(repoRoot, "1. Core", "ApplicationState", "CSystem.cs"));
        string dataSource = File.ReadAllText(
            Path.Combine(repoRoot, "1. Core", "ApplicationState", "CData.cs"));
        AssertTrue(
            !ExtractConstructorBody(systemSource, "public CSystem()").Contains("CUtil.InitDirectory", StringComparison.Ordinal),
            "CSystem construction must not pre-create writable folders in the package");
        AssertTrue(
            !ExtractConstructorBody(dataSource, "public CData()").Contains("CUtil.InitDirectory", StringComparison.Ordinal),
            "CData construction must not pre-create writable folders in the package");
    }

    private static string ReadEntryText(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string ExtractConstructorBody(string source, string signature)
    {
        int signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
        {
            throw new InvalidOperationException($"constructor signature not found: {signature}");
        }

        int openingBrace = source.IndexOf('{', signatureIndex);
        int closingBrace = source.IndexOf('}', openingBrace + 1);
        if (openingBrace < 0 || closingBrace < 0)
        {
            throw new InvalidOperationException($"constructor body not found: {signature}");
        }

        return source.Substring(openingBrace, closingBrace - openingBrace + 1);
    }

    private static string FindRepositoryRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OpenVisionLab.LabelingStudio.csproj")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
