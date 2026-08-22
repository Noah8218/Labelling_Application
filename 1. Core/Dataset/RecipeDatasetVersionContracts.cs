using Newtonsoft.Json;
using System.Collections.Generic;

namespace MvcVisionSystem
{
    public sealed class RecipeDatasetVersionFile
    {
        [JsonProperty("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonProperty("split")]
        public string Split { get; set; } = string.Empty;

        [JsonProperty("relativePath")]
        public string RelativePath { get; set; } = string.Empty;

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; } = string.Empty;
    }

    public sealed class RecipeDatasetVersionSnapshot
    {
        [JsonProperty("identitySchemaVersion")]
        public int IdentitySchemaVersion { get; set; } = RecipeDatasetVersionService.IdentitySchemaVersion;

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; } = RecipeDatasetVersionService.Algorithm;

        [JsonProperty("datasetVersionId")]
        public string DatasetVersionId { get; set; } = string.Empty;

        [JsonProperty("capturedUtc")]
        public string CapturedUtc { get; set; } = string.Empty;

        [JsonProperty("datasetPurpose")]
        public string DatasetPurpose { get; set; } = string.Empty;

        [JsonProperty("contentSha256")]
        public string ContentSha256 { get; set; } = string.Empty;

        [JsonProperty("classContractSha256")]
        public string ClassContractSha256 { get; set; } = string.Empty;

        [JsonProperty("splitContractSha256")]
        public string SplitContractSha256 { get; set; } = string.Empty;

        [JsonProperty("fileCount")]
        public int FileCount { get; set; }

        [JsonProperty("imageFileCount")]
        public int ImageFileCount { get; set; }

        [JsonProperty("annotationFileCount")]
        public int AnnotationFileCount { get; set; }

        [JsonProperty("classes")]
        public List<string> Classes { get; set; } = new List<string>();

        [JsonProperty("files")]
        public List<RecipeDatasetVersionFile> Files { get; set; } = new List<RecipeDatasetVersionFile>();
    }
}
