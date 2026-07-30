using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LabelingApplication.Tests;

internal static class RuntimeDiagnosticsContractTests
{
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
            "RuntimeDiagnosticsViewModel.CreateSupportBundleCommand"
        })
        {
            AssertTrue(xaml.Contains(requiredToken, StringComparison.Ordinal), $"diagnostics UI token missing: {requiredToken}");
        }

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
