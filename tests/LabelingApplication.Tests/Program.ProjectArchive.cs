using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;

namespace LabelingApplication.Tests;

internal static class ProjectArchiveTests
{
    internal static void TestPortableProjectArchiveRoundTrip()
    {
        string root = Path.Combine(Path.GetTempPath(), "ovl-project-archive-test-" + Guid.NewGuid().ToString("N"));
        string recipeRoot = Path.Combine(root, "source-recipes");
        string recipeName = "PortableRecipe";
        string recipeDirectory = Path.Combine(recipeRoot, recipeName);
        string datasetRoot = Path.Combine(root, "source-data", recipeName);
        string archivePath = Path.Combine(root, "exports", recipeName + ".ovl-project.zip");
        string importRecipeRoot = Path.Combine(root, "imported-recipes");
        string importDatasetParent = Path.Combine(root, "imported-data");
        Directory.CreateDirectory(recipeDirectory);
        Directory.CreateDirectory(datasetRoot);

        try
        {
            CData data = BuildSourceProject(recipeDirectory, datasetRoot, recipeName);
            var service = new WpfPortableProjectArchiveService();

            WpfProjectArchiveExportResult exportResult = service.Export(
                recipeName,
                recipeDirectory,
                datasetRoot,
                archivePath);
            AssertTrue(File.Exists(archivePath), "project archive export should create the requested zip");
            AssertTrue(exportResult.FileCount >= 10, "project archive should include recipe, split, label, and sidecar files");
            AssertTrue(exportResult.ExternalReferenceCount >= 1, "project archive should disclose external runtime references");

            WpfProjectArchiveManifest archiveManifest = service.ValidateArchive(archivePath);
            AssertEqual(WpfPortableProjectArchiveService.FormatName, archiveManifest.Format, "archive format");
            AssertSequenceEqual(
                new[] { "OK", "Defect" },
                archiveManifest.Classes,
                "archive class order");
            AssertTrue(
                archiveManifest.Files.Any(file =>
                    string.Equals(file.EntryName, "dataset/data/train/object-metadata/sample.json", StringComparison.Ordinal)),
                "archive should include object metadata sidecars");
            AssertTrue(
                archiveManifest.Files.Any(file =>
                    string.Equals(file.EntryName, "dataset/data/valid/images/valid.png", StringComparison.Ordinal)
                    && string.Equals(file.Split, "valid", StringComparison.Ordinal)),
                "archive should preserve split identity");
            AssertTrue(
                archiveManifest.Files.Any(file =>
                    string.Equals(file.EntryName, "recipe/dataset.versions/dsv2-test.json", StringComparison.Ordinal)),
                "archive should include immutable dataset-version evidence");

            WpfProjectArchiveImportResult importResult = service.Import(
                archivePath,
                importRecipeRoot,
                importDatasetParent);
            AssertEqual(recipeName, importResult.RecipeName, "imported recipe name");
            AssertTrue(File.Exists(Path.Combine(importResult.RecipeDirectory, "VISION.xml")), "import should restore VISION.xml");
            AssertTrue(
                File.Exists(Path.Combine(importResult.DatasetRootPath, "data", "train", "labels", "sample.txt")),
                "import should restore labels");
            AssertTrue(
                File.Exists(Path.Combine(importResult.DatasetRootPath, "data", "train", "object-metadata", "sample.json")),
                "import should restore object metadata");
            AssertTrue(
                File.Exists(Path.Combine(importResult.DatasetRootPath, "review-status.json")),
                "import should restore review evidence");

            CData importedData = LoadData(
                Path.Combine(importResult.RecipeDirectory, "VISION.xml"));
            AssertTrue(importedData != null, "imported VISION.xml should deserialize");
            AssertPathEqual(importResult.DatasetRootPath, importedData.OutputRootPath, "imported output root");
            AssertSequenceEqual(
                new[] { "OK", "Defect" },
                importedData.ClassNamedList.Select(item => item.Text),
                "imported class order");
            AssertPathEqual(
                Path.Combine(importResult.DatasetRootPath, "data", "train", "images"),
                importedData.ProjectSettings.PythonModel.ImageRootPath,
                "dataset-owned image root should be rebased");
            AssertEqual(
                @"C:\external-models\best.pt",
                importedData.ProjectSettings.PythonModel.WeightsPath,
                "external model reference should remain explicit");
            AssertTrue(
                !File.ReadAllText(Path.Combine(importResult.DatasetRootPath, "review-status.json"))
                    .Contains(datasetRoot, StringComparison.OrdinalIgnoreCase),
                "dataset text evidence should not retain the source dataset root");

            AssertThrows<IOException>(
                () => service.Import(archivePath, importRecipeRoot, importDatasetParent),
                "project archive import must not overwrite an existing Recipe or dataset");

            string tamperedPath = Path.Combine(root, "exports", "tampered.ovl-project.zip");
            File.Copy(archivePath, tamperedPath);
            using (ZipArchive tampered = ZipFile.Open(tamperedPath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry label = tampered.GetEntry("dataset/data/train/labels/sample.txt");
                label.Delete();
                using StreamWriter writer = new StreamWriter(
                    tampered.CreateEntry("dataset/data/train/labels/sample.txt").Open());
                writer.Write("tampered");
            }
            AssertThrows<InvalidDataException>(
                () => service.ValidateArchive(tamperedPath),
                "project archive validation must fail closed on checksum mismatch");

            string tamperedRecipeRoot = Path.Combine(root, "tampered-recipes");
            string tamperedDatasetParent = Path.Combine(root, "tampered-data");
            AssertThrows<InvalidDataException>(
                () => service.Import(tamperedPath, tamperedRecipeRoot, tamperedDatasetParent),
                "tampered project archive import should fail before promotion");
            AssertTrue(
                !Directory.Exists(Path.Combine(tamperedRecipeRoot, recipeName))
                && !Directory.Exists(Path.Combine(tamperedDatasetParent, recipeName)),
                "failed import should not leave promoted targets");

            string unsafePath = Path.Combine(root, "exports", "unsafe.ovl-project.zip");
            using (ZipArchive unsafeArchive = ZipFile.Open(unsafePath, ZipArchiveMode.Create))
            {
                using StreamWriter writer = new StreamWriter(
                    unsafeArchive.CreateEntry("../escape.txt").Open());
                writer.Write("unsafe");
            }
            AssertThrows<InvalidDataException>(
                () => service.ValidateArchive(unsafePath),
                "project archive validation must reject traversal paths");
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    internal static void TestProjectArchiveExplicitBoundaryAndUiContract()
    {
        WpfProjectArchivePreflightResult dirty = WpfProjectArchivePreflightService.Check(
            WpfProjectArchiveOperation.Export,
            new WpfApplicationCloseState { HasUnsavedAnnotations = true },
            "Recipe",
            "VISION.xml",
            "dataset");
        AssertTrue(!dirty.CanProceed && dirty.StatusText.Contains("라벨 저장", StringComparison.Ordinal),
            "unsaved labels should block archive export with the canonical explicit-save action");

        WpfProjectArchivePreflightResult pending = WpfProjectArchivePreflightService.Check(
            WpfProjectArchiveOperation.Import,
            new WpfApplicationCloseState { PendingCandidateCount = 1 });
        AssertTrue(!pending.CanProceed && pending.StatusText.Contains("자동 확정하지", StringComparison.Ordinal),
            "pending candidates should block archive operations without confirmation");

        WpfProjectArchivePreflightResult active = WpfProjectArchivePreflightService.Check(
            WpfProjectArchiveOperation.Import,
            new WpfApplicationCloseState { ActiveWorkNames = new[] { "모델 학습" } });
        AssertTrue(!active.CanProceed && active.StatusText.Contains("모델 학습", StringComparison.Ordinal),
            "active work should block archive operations");

        string repositoryRoot = FindRepositoryRoot();
        string xaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.xaml"));
        string adapter = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.ProjectArchiveCommands.cs"));
        AssertTrue(xaml.Contains("ExportProjectArchiveButton", StringComparison.Ordinal)
            && xaml.Contains("ImportProjectArchiveButton", StringComparison.Ordinal),
            "header tools should expose explicit project archive export and import");
        AssertTrue(xaml.Contains("기존 프로젝트 덮어쓰기 없음", StringComparison.Ordinal)
            && xaml.Contains("가져온 뒤 명시적 적용", StringComparison.Ordinal),
            "archive UI should disclose non-overwrite and explicit-apply behavior");
        AssertTrue(!adapter.Contains("SaveCurrentAnnotations(", StringComparison.Ordinal)
            && !adapter.Contains("SaveProjectConfigFromPanel(", StringComparison.Ordinal)
            && !adapter.Contains("Confirm", StringComparison.Ordinal),
            "archive adapter must not save labels/settings or confirm candidates implicitly");
        AssertTrue(adapter.Contains("SelectRecipeFromList", StringComparison.Ordinal)
            && !adapter.Contains("ApplyProjectRecipeFromPanel(", StringComparison.Ordinal),
            "archive import should select the restored Recipe but leave Apply explicit");
    }

    private static CData BuildSourceProject(
        string recipeDirectory,
        string datasetRoot,
        string recipeName)
    {
        var data = new CData();
        data.ConfigureOutputRoot(datasetRoot);
        data.ClassNamedList.Add(new CClassItem { Text = "OK" });
        data.ClassNamedList.Add(new CClassItem { Text = "Defect" });
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        data.ProjectSettings.ObjectReviewTags = new List<string> { "scratch", "edge" };
        data.ProjectSettings.PythonModel.ImageRootPath =
            Path.Combine(datasetRoot, "data", "train", "images");
        data.ProjectSettings.PythonModel.WeightsPath = @"C:\external-models\best.pt";

        WriteBytes(Path.Combine(datasetRoot, "data", "train", "images", "sample.png"), new byte[] { 1, 2, 3, 4 });
        WriteText(Path.Combine(datasetRoot, "data", "train", "labels", "sample.txt"), "1 0.5 0.5 0.2 0.2");
        WriteText(Path.Combine(datasetRoot, "data", "train", "segments", "sample.json"), "{\"objects\":[]}");
        WriteBytes(Path.Combine(datasetRoot, "data", "train", "masks", "sample.png"), new byte[] { 5, 6, 7 });
        WriteText(
            Path.Combine(datasetRoot, "data", "train", "object-metadata", "sample.json"),
            "{\"version\":2,\"objects\":[{\"kind\":\"Box\",\"className\":\"Defect\",\"isOccluded\":true}]}");
        WriteBytes(Path.Combine(datasetRoot, "data", "valid", "images", "valid.png"), new byte[] { 8, 9 });
        WriteText(Path.Combine(datasetRoot, "data", "valid", "labels", "valid.txt"), string.Empty);
        WriteBytes(Path.Combine(datasetRoot, "data", "test", "images", "test.png"), new byte[] { 10, 11 });
        WriteText(Path.Combine(datasetRoot, "data", "test", "labels", "test.txt"), string.Empty);
        WriteText(
            Path.Combine(datasetRoot, "review-status.json"),
            JsonConvert.SerializeObject(new { imagePath = Path.Combine(datasetRoot, "data", "train", "images", "sample.png") }));
        WriteText(Path.Combine(datasetRoot, "reports", "evidence.md"), "# Held-out evidence");
        data.SaveYoloDataYaml();

        SaveData(Path.Combine(recipeDirectory, "VISION.xml"), data);
        LabelingDatasetManifest manifest = LabelingDatasetManifestService.Build(data, recipeName);
        WriteText(
            Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName),
            JsonConvert.SerializeObject(manifest, Formatting.Indented));
        WriteText(
            Path.Combine(recipeDirectory, RecipeDatasetVersionService.HistoryDirectoryName, "dsv2-test.json"),
            "{\"datasetVersionId\":\"dsv2-test\"}");
        WriteText(Path.Combine(recipeDirectory, "VISION", "template.xml"), "<template />");
        return data;
    }

    private static void WriteText(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllText(path, contents ?? string.Empty);
    }

    private static void WriteBytes(string path, byte[] contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllBytes(path, contents ?? Array.Empty<byte>());
    }

    private static void SaveData(string path, CData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        using FileStream stream = File.Create(path);
        new XmlSerializer(typeof(CData)).Serialize(stream, data);
    }

    private static CData LoadData(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return new XmlSerializer(typeof(CData)).Deserialize(stream) as CData;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenVisionLab.LabelingStudio.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root could not be found.");
    }

    private static void AssertPathEqual(string expected, string actual, string message)
        => AssertEqual(
            Path.GetFullPath(expected).TrimEnd('\\', '/'),
            Path.GetFullPath(actual).TrimEnd('\\', '/'),
            message,
            StringComparer.OrdinalIgnoreCase);

    private static void AssertSequenceEqual(
        IEnumerable<string> expected,
        IEnumerable<string> actual,
        string message)
    {
        if (!(expected ?? Array.Empty<string>()).SequenceEqual(actual ?? Array.Empty<string>(), StringComparer.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual(
        string expected,
        string actual,
        string message,
        StringComparer comparer = null)
    {
        comparer ??= StringComparer.Ordinal;
        if (!comparer.Equals(expected ?? string.Empty, actual ?? string.Empty))
        {
            throw new InvalidOperationException($"{message}: expected '{expected}', actual '{actual}'");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<T>(Action action, string message)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
