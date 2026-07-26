using Newtonsoft.Json;
using System.Collections.Generic;

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
}
