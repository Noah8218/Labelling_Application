using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MvcVisionSystem.Yolo;

namespace MvcVisionSystem
{
    public sealed class LabelingDatasetManifest
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 3;

        [JsonProperty("generatedUtc")]
        public string GeneratedUtc { get; set; } = "";

        [JsonProperty("datasetVersionId")]
        public string DatasetVersionId { get; set; } = "";

        [JsonProperty("contentIdentity")]
        public LabelingDatasetManifestContentIdentity ContentIdentity { get; set; } = new LabelingDatasetManifestContentIdentity();

        [JsonProperty("recipeName")]
        public string RecipeName { get; set; } = "";

        [JsonProperty("datasetPurpose")]
        public string DatasetPurpose { get; set; } = LabelingDatasetPurpose.ObjectDetection.ToString();

        [JsonProperty("annotationProfile")]
        public string AnnotationProfile { get; set; } = "";

        [JsonProperty("visibleTools")]
        public List<string> VisibleTools { get; set; } = new List<string>();

        [JsonProperty("outputRootPath")]
        public string OutputRootPath { get; set; } = "";

        [JsonProperty("imageRootPath")]
        public string ImageRootPath { get; set; } = "";

        [JsonProperty("dataYamlFilePath")]
        public string DataYamlFilePath { get; set; } = "";

        [JsonProperty("classes")]
        public List<string> Classes { get; set; } = new List<string>();

        [JsonProperty("training")]
        public LabelingDatasetManifestTraining Training { get; set; } = new LabelingDatasetManifestTraining();

        [JsonProperty("artifactSummary")]
        public LabelingDatasetManifestArtifactSummary ArtifactSummary { get; set; } = new LabelingDatasetManifestArtifactSummary();
    }

    public sealed class LabelingDatasetManifestContentIdentity
    {
        [JsonProperty("identitySchemaVersion")]
        public int IdentitySchemaVersion { get; set; } = RecipeDatasetVersionService.IdentitySchemaVersion;

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; } = RecipeDatasetVersionService.Algorithm;

        [JsonProperty("contentSha256")]
        public string ContentSha256 { get; set; } = "";

        [JsonProperty("classContractSha256")]
        public string ClassContractSha256 { get; set; } = "";

        [JsonProperty("splitContractSha256")]
        public string SplitContractSha256 { get; set; } = "";

        [JsonProperty("fileCount")]
        public int FileCount { get; set; }

        [JsonProperty("imageFileCount")]
        public int ImageFileCount { get; set; }

        [JsonProperty("annotationFileCount")]
        public int AnnotationFileCount { get; set; }

        [JsonProperty("historyEntry")]
        public string HistoryEntry { get; set; } = "";
    }

    public sealed class LabelingDatasetManifestTraining
    {
        [JsonProperty("validationPercent")]
        public int ValidationPercent { get; set; }

        [JsonProperty("testPercent")]
        public int TestPercent { get; set; }

        [JsonProperty("splitSeed")]
        public int SplitSeed { get; set; }
    }

    public sealed class LabelingDatasetManifestArtifactSummary
    {
        [JsonProperty("primaryLabelKind")]
        public string PrimaryLabelKind { get; set; } = "";

        [JsonProperty("primaryLabelCount")]
        public int PrimaryLabelCount { get; set; }

        [JsonProperty("imageCount")]
        public int ImageCount { get; set; }

        [JsonProperty("anomalyReviewedImageCount")]
        public int AnomalyReviewedImageCount { get; set; }

        [JsonProperty("anomalyNormalImageCount")]
        public int AnomalyNormalImageCount { get; set; }

        [JsonProperty("anomalyAbnormalImageCount")]
        public int AnomalyAbnormalImageCount { get; set; }

        [JsonProperty("anomalyUnreviewedImageCount")]
        public int AnomalyUnreviewedImageCount { get; set; }

        [JsonProperty("boxObjectCount")]
        public int BoxObjectCount { get; set; }

        [JsonProperty("boxLabelFileCount")]
        public int BoxLabelFileCount { get; set; }

        [JsonProperty("segmentObjectCount")]
        public int SegmentObjectCount { get; set; }

        [JsonProperty("segmentFileCount")]
        public int SegmentFileCount { get; set; }

        [JsonProperty("maskFileCount")]
        public int MaskFileCount { get; set; }
    }

    public static class LabelingDatasetManifestService
    {
        public const string FileName = "dataset.manifest.json";

        public static string GetManifestPath(string recipeName)
        {
            return Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName ?? string.Empty, FileName);
        }

        public static void Save(CData data, string recipeName)
        {
            if (data == null)
            {
                return;
            }

            string manifestPath = GetManifestPath(recipeName);
            string recipeDirectory = Path.GetDirectoryName(manifestPath);
            Directory.CreateDirectory(recipeDirectory);
            RecipeDatasetVersionSnapshot snapshot = RecipeDatasetVersionService.RecordSnapshot(
                recipeDirectory,
                RecipeDatasetVersionService.CreateSnapshot(data));
            File.WriteAllText(
                manifestPath,
                JsonConvert.SerializeObject(Build(data, recipeName, snapshot), Formatting.Indented));
        }

        public static void Save(CData data, string recipeName, RecipeDatasetVersionSnapshot snapshot)
        {
            if (data == null)
            {
                return;
            }

            string manifestPath = GetManifestPath(recipeName);
            string recipeDirectory = Path.GetDirectoryName(manifestPath);
            Directory.CreateDirectory(recipeDirectory);
            RecipeDatasetVersionSnapshot stored = RecipeDatasetVersionService.RecordSnapshot(
                recipeDirectory,
                snapshot ?? RecipeDatasetVersionService.CreateSnapshot(data));
            File.WriteAllText(
                manifestPath,
                JsonConvert.SerializeObject(Build(data, recipeName, stored), Formatting.Indented));
        }

        public static LabelingDatasetManifest Build(CData data, string recipeName)
            => Build(data, recipeName, RecipeDatasetVersionService.CreateSnapshot(data));

        private static LabelingDatasetManifest Build(
            CData data,
            string recipeName,
            RecipeDatasetVersionSnapshot datasetVersion)
        {
            data ??= new CData();
            LabelingProjectSettings settings = data.ProjectSettings ?? new LabelingProjectSettings();
            settings.EnsureDefaults();
            YoloDatasetStatistics statistics = YoloDatasetValidator.BuildStatistics(data);
            AnomalyImageReviewSummary anomalySummary = settings.DatasetPurpose == LabelingDatasetPurpose.AnomalyDetection
                ? AnomalyImageReviewStatusService.LoadPersistedSummary(data, statistics.TotalImageCount)
                : new AnomalyImageReviewSummary();
            LabelingDatasetManifestArtifactSummary artifactSummary = BuildArtifactSummary(settings.DatasetPurpose, statistics, anomalySummary);

            var manifest = new LabelingDatasetManifest
            {
                RecipeName = recipeName ?? string.Empty,
                GeneratedUtc = DateTime.UtcNow.ToString("O"),
                DatasetPurpose = settings.DatasetPurpose.ToString(),
                AnnotationProfile = ResolveAnnotationProfile(settings.DatasetPurpose),
                VisibleTools = ResolveVisibleTools(settings.DatasetPurpose).ToList(),
                OutputRootPath = data.OutputRootPath,
                ImageRootPath = settings.PythonModel?.ImageRootPath ?? string.Empty,
                DataYamlFilePath = data.DataYamlFilePath,
                Classes = data.ClassNamedList?
                    .Select(item => item?.Text)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>(),
                Training = new LabelingDatasetManifestTraining
                {
                    ValidationPercent = settings.YoloDataset?.ValidationPercent ?? 0,
                    TestPercent = settings.YoloDataset?.TestPercent ?? 0,
                    SplitSeed = settings.YoloDataset?.SplitSeed ?? 0
                },
                ArtifactSummary = artifactSummary,
                DatasetVersionId = datasetVersion?.DatasetVersionId ?? string.Empty,
                ContentIdentity = new LabelingDatasetManifestContentIdentity
                {
                    IdentitySchemaVersion = datasetVersion?.IdentitySchemaVersion ?? RecipeDatasetVersionService.IdentitySchemaVersion,
                    Algorithm = datasetVersion?.Algorithm ?? RecipeDatasetVersionService.Algorithm,
                    ContentSha256 = datasetVersion?.ContentSha256 ?? string.Empty,
                    ClassContractSha256 = datasetVersion?.ClassContractSha256 ?? string.Empty,
                    SplitContractSha256 = datasetVersion?.SplitContractSha256 ?? string.Empty,
                    FileCount = datasetVersion?.FileCount ?? 0,
                    ImageFileCount = datasetVersion?.ImageFileCount ?? 0,
                    AnnotationFileCount = datasetVersion?.AnnotationFileCount ?? 0,
                    HistoryEntry = Path.Combine(
                            RecipeDatasetVersionService.HistoryDirectoryName,
                            (datasetVersion?.DatasetVersionId ?? string.Empty) + ".json")
                        .Replace(Path.DirectorySeparatorChar, '/')
                }
            };
            return manifest;
        }

        private static string ResolveAnnotationProfile(LabelingDatasetPurpose purpose)
        {
            return purpose switch
            {
                LabelingDatasetPurpose.Segmentation => "mask-and-polygon",
                LabelingDatasetPurpose.AnomalyDetection => "image-level-normal-abnormal",
                _ => "bounding-box"
            };
        }

        private static IEnumerable<string> ResolveVisibleTools(LabelingDatasetPurpose purpose)
        {
            return purpose switch
            {
                LabelingDatasetPurpose.Segmentation => new[] { "select", "polygon", "brush", "eraser", "panZoom" },
                LabelingDatasetPurpose.AnomalyDetection => new[] { "panZoom" },
                _ => new[] { "select", "rectangle", "panZoom" }
            };
        }

        private static LabelingDatasetManifestArtifactSummary BuildArtifactSummary(
            LabelingDatasetPurpose purpose,
            YoloDatasetStatistics statistics,
            AnomalyImageReviewSummary anomalySummary)
        {
            statistics ??= new YoloDatasetStatistics();
            anomalySummary ??= new AnomalyImageReviewSummary();
            string primaryLabelKind = purpose switch
            {
                LabelingDatasetPurpose.Segmentation => statistics.TotalSegmentationObjectCount > 0
                    ? "segments"
                    : "masks",
                LabelingDatasetPurpose.AnomalyDetection => "image-level-normal-abnormal",
                _ => "boxes"
            };

            int primaryLabelCount = purpose == LabelingDatasetPurpose.AnomalyDetection
                ? anomalySummary.ReviewedImageCount
                : primaryLabelKind == "segments"
                ? statistics.TotalSegmentationObjectCount
                : primaryLabelKind == "masks"
                    ? statistics.TotalMaskFileCount
                    : statistics.TotalObjectCount;

            return new LabelingDatasetManifestArtifactSummary
            {
                PrimaryLabelKind = primaryLabelKind,
                PrimaryLabelCount = primaryLabelCount,
                ImageCount = statistics.TotalImageCount,
                AnomalyReviewedImageCount = anomalySummary.ReviewedImageCount,
                AnomalyNormalImageCount = anomalySummary.NormalImageCount,
                AnomalyAbnormalImageCount = anomalySummary.AbnormalImageCount,
                AnomalyUnreviewedImageCount = anomalySummary.UnreviewedImageCount,
                BoxObjectCount = statistics.TotalObjectCount,
                BoxLabelFileCount = statistics.TotalLabelFileCount,
                SegmentObjectCount = statistics.TotalSegmentationObjectCount,
                SegmentFileCount = statistics.TotalSegmentFileCount,
                MaskFileCount = statistics.TotalMaskFileCount
            };
        }

    }
}
