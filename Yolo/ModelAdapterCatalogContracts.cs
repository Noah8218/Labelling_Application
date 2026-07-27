namespace MvcVisionSystem.Yolo
{
    public sealed class ModelAdapterCatalogItem
    {
        public string AdapterKey { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string AvailabilityText { get; init; } = string.Empty;

        public string TaskContractText { get; init; } = string.Empty;

        public string DataContractText { get; init; } = string.Empty;

        public string RuntimeContractText { get; init; } = string.Empty;

        public string EvidenceContractText { get; init; } = string.Empty;

        public string NextActionText { get; init; } = string.Empty;
    }
}
