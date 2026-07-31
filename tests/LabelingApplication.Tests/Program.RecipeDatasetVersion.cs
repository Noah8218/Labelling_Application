using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;
using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;

namespace LabelingApplication.Tests;

using static Program;
using static TestSupport;

internal static class RecipeDatasetVersionTests
{
    internal static void TestRecipeDatasetVersionV2()
    {
        string datasetRoot = CreateTempRoot();
        string recipeName = "dataset_version_v2_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        try
        {
            string trainImages = Path.Combine(datasetRoot, "data", "train", "images");
            string trainLabels = Path.Combine(datasetRoot, "data", "train", "labels");
            Directory.CreateDirectory(trainImages);
            Directory.CreateDirectory(trainLabels);
            string imagePath = Path.Combine(trainImages, "part.png");
            string labelPath = Path.Combine(trainLabels, "part.txt");
            File.WriteAllBytes(imagePath, new byte[] { 1, 2, 3, 4, 5 });
            File.WriteAllText(labelPath, "0 0.5 0.5 0.2 0.2");

            var data = new CData();
            data.ConfigureOutputRoot(datasetRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            data.ProjectSettings.PythonModel.ImageRootPath = trainImages;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });

            RecipeDatasetVersionSnapshot first = RecipeDatasetVersionService.CreateSnapshot(data);
            RecipeDatasetVersionSnapshot same = RecipeDatasetVersionService.CreateSnapshot(data);
            AssertEqual(first.DatasetVersionId, same.DatasetVersionId);
            AssertEqual(first.ContentSha256, same.ContentSha256);
            AssertTrue(first.DatasetVersionId.StartsWith("dsv2-", StringComparison.Ordinal), "dataset version id should declare the dsv2 contract");
            AssertEqual(2, first.FileCount);
            AssertEqual(1, first.ImageFileCount);
            AssertEqual(1, first.AnnotationFileCount);

            string treeBefore = ComputeDatasetVersionTestTreeSha256(datasetRoot);
            data.SaveConfig(recipeName);
            string treeAfter = ComputeDatasetVersionTestTreeSha256(datasetRoot);
            AssertEqual(treeBefore, treeAfter);

            string manifestPath = Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName);
            LabelingDatasetManifest manifest =
                JsonConvert.DeserializeObject<LabelingDatasetManifest>(File.ReadAllText(manifestPath));
            AssertTrue(manifest != null, "v2 manifest should deserialize");
            AssertEqual(3, manifest.SchemaVersion);
            AssertEqual(first.DatasetVersionId, manifest.DatasetVersionId);
            AssertEqual(RecipeDatasetVersionService.IdentitySchemaVersion, manifest.ContentIdentity.IdentitySchemaVersion);
            AssertEqual(first.ContentSha256, manifest.ContentIdentity.ContentSha256);
            AssertEqual(RecipeDatasetVersionService.Algorithm, manifest.ContentIdentity.Algorithm);
            AssertEqual(1, RecipeDatasetVersionService.LoadHistory(recipeDirectory).Count);

            string immutablePath = Path.Combine(
                recipeDirectory,
                RecipeDatasetVersionService.HistoryDirectoryName,
                first.DatasetVersionId + ".json");
            string immutableBefore = ComputeDatasetVersionTestFileSha256(immutablePath);
            data.SaveConfig(recipeName);
            AssertEqual(1, RecipeDatasetVersionService.LoadHistory(recipeDirectory).Count);
            AssertEqual(immutableBefore, ComputeDatasetVersionTestFileSha256(immutablePath));

            File.WriteAllText(labelPath, "0 0.5 0.5 0.3 0.2");
            RecipeDatasetVersionSnapshot changedLabel = RecipeDatasetVersionService.CreateSnapshot(data);
            AssertTrue(
                !string.Equals(first.DatasetVersionId, changedLabel.DatasetVersionId, StringComparison.Ordinal),
                "changing label geometry without changing object count must create a new dataset version");
            data.SaveConfig(recipeName, refreshDatasetVersion: false);
            AssertEqual(1, RecipeDatasetVersionService.LoadHistory(recipeDirectory).Count);
            LabelingDatasetManifest unchangedManifest =
                JsonConvert.DeserializeObject<LabelingDatasetManifest>(File.ReadAllText(manifestPath));
            AssertEqual(first.DatasetVersionId, unchangedManifest.DatasetVersionId);
            data.SaveConfig(recipeName);
            AssertEqual(2, RecipeDatasetVersionService.LoadHistory(recipeDirectory).Count);

            data.ClassNamedList[0].Text = "Scratch";
            RecipeDatasetVersionSnapshot changedClass = RecipeDatasetVersionService.CreateSnapshot(data);
            AssertTrue(
                !string.Equals(changedLabel.DatasetVersionId, changedClass.DatasetVersionId, StringComparison.Ordinal),
                "changing the ordered class contract must create a new dataset version");

            string validImages = Path.Combine(datasetRoot, "data", "valid", "images");
            string validLabels = Path.Combine(datasetRoot, "data", "valid", "labels");
            Directory.CreateDirectory(validImages);
            Directory.CreateDirectory(validLabels);
            File.Move(imagePath, Path.Combine(validImages, Path.GetFileName(imagePath)));
            File.Move(labelPath, Path.Combine(validLabels, Path.GetFileName(labelPath)));
            RecipeDatasetVersionSnapshot changedSplit = RecipeDatasetVersionService.CreateSnapshot(data);
            AssertTrue(
                !string.Equals(changedClass.DatasetVersionId, changedSplit.DatasetVersionId, StringComparison.Ordinal),
                "moving identical content between train and valid must create a new dataset version");

            string anomalyRoot = Path.Combine(datasetRoot, "anomaly");
            string anomalyTrain = Path.Combine(anomalyRoot, "classification", "train", "normal");
            string anomalyValid = Path.Combine(anomalyRoot, "classification", "valid", "abnormal");
            Directory.CreateDirectory(anomalyTrain);
            Directory.CreateDirectory(anomalyValid);
            string anomalyImagePath = Path.Combine(anomalyTrain, "normal.png");
            File.WriteAllBytes(anomalyImagePath, new byte[] { 9, 8, 7, 6 });
            File.WriteAllBytes(Path.Combine(anomalyValid, "abnormal.png"), new byte[] { 5, 4, 3, 2 });
            var anomalyData = new CData();
            anomalyData.ConfigureOutputRoot(anomalyRoot);
            anomalyData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            RecipeDatasetVersionSnapshot anomalySnapshot = RecipeDatasetVersionService.CreateSnapshot(anomalyData);
            AssertTrue(
                anomalySnapshot.Files.Any(item =>
                    string.Equals(item.Split, "train", StringComparison.Ordinal)
                    && item.RelativePath.Contains("classification/train/normal", StringComparison.Ordinal)),
                "anomaly identity should include the exact exported classification split");
            string anomalyTest = Path.Combine(anomalyRoot, "classification", "test", "normal");
            Directory.CreateDirectory(anomalyTest);
            File.Move(anomalyImagePath, Path.Combine(anomalyTest, Path.GetFileName(anomalyImagePath)));
            RecipeDatasetVersionSnapshot changedAnomalySplit = RecipeDatasetVersionService.CreateSnapshot(anomalyData);
            AssertTrue(
                !string.Equals(anomalySnapshot.DatasetVersionId, changedAnomalySplit.DatasetVersionId, StringComparison.Ordinal),
                "moving anomaly classification content between train and test must create a new dataset version");

            var registry = new ModelRegistrySettings();
            ModelRegistryService.RecordTrainingCandidate(
                registry,
                data.ProjectSettings.PythonModel,
                data.ProjectSettings.DatasetPurpose,
                datasetRoot,
                Path.Combine(datasetRoot, "best.pt"),
                string.Empty,
                "mAP50 0.5",
                "completed",
                100,
                "done",
                savedToRecipe: false,
                datasetVersionId: changedSplit.DatasetVersionId,
                datasetContentSha256: changedSplit.ContentSha256);
            TrainingRun run = registry.TrainingRuns.Single();
            AssertEqual(changedSplit.DatasetVersionId, run.DatasetVersionId);
            AssertEqual(changedSplit.ContentSha256, run.DatasetContentSha256);
            WpfModelRegistryHistoryItem modelHistory =
                WpfModelRegistryPresentationService.Build(
                    data.ProjectSettings.PythonModel,
                    null,
                    data.ProjectSettings.TrainingGuide,
                    registry,
                    false)
                .HistoryItems
                .Single();
            AssertTrue(
                modelHistory.DetailText.Contains("Dataset dsv2-", StringComparison.Ordinal),
                "Model Center history should expose the dataset version used by the training run");

            WpfRecipeDatasetVersionPresentation presentation =
                WpfRecipeDatasetVersionPresentationService.Build(manifestPath);
            AssertTrue(presentation.VersionText.StartsWith("dsv2-", StringComparison.Ordinal), "project UI should expose the current dataset version");
            AssertTrue(presentation.DetailText.Contains("SHA-256", StringComparison.Ordinal), "project UI should explain the content identity");
            AssertTrue(presentation.DetailText.Contains("불변 이력", StringComparison.Ordinal), "project UI should show immutable history count");
        }
        finally
        {
            DeleteTempRoot(datasetRoot);
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }
        }
    }

    internal static int RunExeDatasetVersionSmoke(string[] args)
    {
        string datasetRoot = string.Empty;
        string recipeDirectory = string.Empty;
        Process process = null;
        try
        {
            string repositoryRoot = FindRepositoryRoot();
            string exePath = Path.GetFullPath(GetArgumentValue(
                args,
                "--exe",
                Path.Combine(repositoryRoot, "artifacts", "run", "Debug", "OpenVisionLab.LabelingStudio.exe")));
            string outputPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--output",
                Path.Combine(
                    repositoryRoot,
                    "artifacts",
                    "ui",
                    "recipe-dataset-version-v2",
                    "exe-dataset-version-1920.png")));
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("Dataset Version EXE smoke target was not found.", exePath);
            }

            datasetRoot = CreateTempRoot();
            string recipeName = "ExeDatasetVersion_" + Guid.NewGuid().ToString("N");
            recipeDirectory = Path.Combine(Path.GetDirectoryName(exePath) ?? string.Empty, "RECIPE", recipeName);
            Directory.CreateDirectory(recipeDirectory);
            string trainImages = Path.Combine(datasetRoot, "data", "train", "images");
            string trainLabels = Path.Combine(datasetRoot, "data", "train", "labels");
            Directory.CreateDirectory(trainImages);
            Directory.CreateDirectory(trainLabels);
            using (var preview = new Bitmap(64, 64))
            using (Graphics graphics = Graphics.FromImage(preview))
            {
                graphics.Clear(Color.Gray);
                preview.Save(
                    Path.Combine(trainImages, "preview.png"),
                    System.Drawing.Imaging.ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(trainLabels, "preview.txt"), "0 0.5 0.5 0.2 0.2");

            var data = new CData();
            data.ConfigureOutputRoot(datasetRoot);
            data.ProjectSettings.PythonModel.ImageRootPath = trainImages;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });
            string configPath = Path.Combine(recipeDirectory, "VISION.xml");
            using (FileStream configStream = File.Create(configPath))
            {
                new XmlSerializer(typeof(CData)).Serialize(configStream, data);
            }
            AssertTrue(File.Exists(configPath), "EXE dataset version fixture config was not written");
            RecipeDatasetVersionSnapshot snapshot = RecipeDatasetVersionService.RecordSnapshot(
                recipeDirectory,
                RecipeDatasetVersionService.CreateSnapshot(data));
            File.WriteAllText(
                Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName),
                JsonConvert.SerializeObject(LabelingDatasetManifestService.Build(data, recipeName), Formatting.Indented));

            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
            AssertTrue(process != null, "Dataset Version EXE smoke process did not start");
            IntPtr handle = WaitForMainWindowHandle(process, TimeSpan.FromSeconds(25));
            AssertTrue(handle != IntPtr.Zero, "Dataset Version EXE smoke window did not appear");
            PlaceExeSmokeWindowOnLeftmostMonitor(handle);
            BringNativeWindowToFront(handle);
            AssertTrue(
                OpenYoloModelCenterThroughExe(process, handle),
                "Dataset Version EXE smoke could not open the model center");
            System.Windows.Automation.AutomationElement setupRoot =
                RefreshAutomationRoot(process, handle, bringToFront: false);
            AssertTrue(
                SelectAutomationTabByAutomationId(setupRoot, "YoloModelCenterDataTaskTab"),
                "Dataset Version EXE smoke could not open the data task before applying the recipe");
            AssertTrue(ApplyExeSmokeRecipe(process, recipeName), "Dataset Version EXE smoke recipe was not applied");

            AssertTrue(
                OpenYoloModelCenterThroughExe(process, handle),
                "Dataset Version EXE smoke could not reopen the model center after applying the recipe");
            System.Windows.Automation.AutomationElement root =
                RefreshAutomationRoot(process, handle, bringToFront: false);
            AssertTrue(
                SelectAutomationTabByAutomationId(root, "YoloModelCenterDataTaskTab"),
                "Dataset Version EXE smoke could not open the data task");
            root = RefreshAutomationRoot(process);
            AssertTrue(
                TryExpandAutomationElementByAutomationId(root, "ProjectConfigExpander"),
                "Dataset Version EXE smoke could not expand project settings");
            bool versionVisible = WaitUntil(
                () => GetAutomationValueByAutomationId(
                        RefreshAutomationRoot(process, bringToFront: false),
                        "ProjectDatasetVersionBox")
                    .StartsWith("dsv2-", StringComparison.Ordinal),
                TimeSpan.FromSeconds(5));
            if (!versionVisible)
            {
                root = RefreshAutomationRoot(process, handle, bringToFront: false);
                string failurePath = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(outputPath) + "-failure.png");
                CaptureAutomationRoot(root, failurePath);
                throw new InvalidOperationException(
                    "current EXE did not show the Recipe Dataset Version v2 id"
                    + $" (recipe='{GetAutomationValueByAutomationId(root, "ProjectRecipeNameBox")}'"
                    + $", manifest='{GetAutomationValueByAutomationId(root, "ProjectManifestPathBox")}'"
                    + $", version='{GetAutomationValueByAutomationId(root, "ProjectDatasetVersionBox")}'"
                    + $", detail='{GetAutomationValueByAutomationId(root, "ProjectDatasetVersionDetailBox")}'"
                    + $", capture='{failurePath}')");
            }

            root = RefreshAutomationRoot(process);
            string versionText = GetAutomationValueByAutomationId(root, "ProjectDatasetVersionBox");
            string detailText = GetAutomationValueByAutomationId(root, "ProjectDatasetVersionDetailBox");
            AssertEqual(snapshot.DatasetVersionId, versionText);
            AssertTrue(detailText.Contains("SHA-256", StringComparison.Ordinal), "current EXE should show the dataset content hash");
            AssertTrue(detailText.Contains("불변 이력 1개", StringComparison.Ordinal), "current EXE should show immutable history count");
            CaptureAutomationRoot(root, outputPath);
            Console.WriteLine($"PASS EXE Recipe Dataset Version v2: {versionText}");
            Console.WriteLine($"EXE dataset-version smoke captured: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL EXE Recipe Dataset Version v2: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            CloseExeSmokeProcess(process);
            if (!string.IsNullOrWhiteSpace(datasetRoot))
            {
                DeleteTempRoot(datasetRoot);
            }

            if (!string.IsNullOrWhiteSpace(recipeDirectory) && Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }
        }
    }

    internal static int RunWpfDatasetVersionVisual(string[] args)
    {
        string datasetRoot = string.Empty;
        string recipeDirectory = string.Empty;
        try
        {
            if (System.Windows.Application.Current == null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };
            }

            string outputPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--output",
                Path.Combine(
                    FindRepositoryRoot(),
                    "artifacts",
                    "ui",
                    "recipe-dataset-version-v2",
                    "dataset-version-visual-1920.png")));
            datasetRoot = CreateTempRoot();
            string recipeName = "DatasetVersionPreview_" + Guid.NewGuid().ToString("N");
            recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
            string trainImages = Path.Combine(datasetRoot, "data", "train", "images");
            string trainLabels = Path.Combine(datasetRoot, "data", "train", "labels");
            Directory.CreateDirectory(trainImages);
            Directory.CreateDirectory(trainLabels);
            File.WriteAllBytes(Path.Combine(trainImages, "preview.png"), new byte[] { 1, 2, 3, 4 });
            File.WriteAllText(Path.Combine(trainLabels, "preview.txt"), "0 0.5 0.5 0.2 0.2");
            var data = new CData();
            data.ConfigureOutputRoot(datasetRoot);
            data.ProjectSettings.PythonModel.ImageRootPath = trainImages;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });
            data.SaveConfig(recipeName);

            var window = new WpfLabelingShellWindow
            {
                Width = Math.Max(VisualSmokeMinimumWindowWidth, 1920),
                Height = Math.Max(VisualSmokeMinimumWindowHeight, 1080)
            };
            try
            {
                window.Show();
                window.FocusYoloSettingsTab();
                if (window.FindName("YoloModelCenterDataTaskTab") is System.Windows.Controls.TabItem dataTaskTab
                    && window.FindName("YoloModelCenterTaskTabs") is System.Windows.Controls.TabControl taskTabs)
                {
                    taskTabs.SelectedItem = dataTaskTab;
                }

                if (window.FindName("ProjectConfigPanelControl") is WpfProjectConfigPanel panel)
                {
                    string recipeRoot = Path.Combine(AppContext.BaseDirectory, "RECIPE");
                    panel.ViewModel.LoadFrom(recipeName, recipeRoot);
                    panel.ViewModel.SetDatasetVersionInfo(
                        WpfRecipeDatasetVersionPresentationService.Build(
                            Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName)));
                    panel.SettingsExpander.IsExpanded = true;
                    panel.BringIntoView();
                }

                window.UpdateLayout();
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(200));
                CaptureWindow(window, outputPath);
                Console.WriteLine($"WPF dataset-version visual captured: {outputPath}");
                return 0;
            }
            finally
            {
                window.Close();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL WPF dataset-version visual: {ex.Message}");
            return 1;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(datasetRoot))
            {
                DeleteTempRoot(datasetRoot);
            }

            if (!string.IsNullOrWhiteSpace(recipeDirectory) && Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }
        }
    }

    private static string ComputeDatasetVersionTestTreeSha256(string rootPath)
    {
        var builder = new StringBuilder();
        foreach (string path in Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(rootPath, path), StringComparer.Ordinal))
        {
            builder
                .Append(Path.GetRelativePath(rootPath, path).Replace(Path.DirectorySeparatorChar, '/'))
                .Append('|')
                .Append(ComputeDatasetVersionTestFileSha256(path))
                .Append('\n');
        }

        using SHA256 sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string ComputeDatasetVersionTestFileSha256(string path)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream));
    }
}
