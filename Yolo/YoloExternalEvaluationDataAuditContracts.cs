using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloExternalEvaluationDataAuditReport
    {
        public YoloExternalEvaluationDataAuditReport(
            string externalDirectory,
            int referenceImageCount,
            int externalImageCount,
            int nameOverlapCount,
            int contentOverlapCount,
            string overlapExample,
            IEnumerable<string> errors)
        {
            ExternalDirectory = externalDirectory ?? string.Empty;
            ReferenceImageCount = referenceImageCount;
            ExternalImageCount = externalImageCount;
            NameOverlapCount = nameOverlapCount;
            ContentOverlapCount = contentOverlapCount;
            OverlapExample = overlapExample ?? string.Empty;
            Errors = (errors ?? Enumerable.Empty<string>()).ToList();
        }

        public string ExternalDirectory { get; }
        public int ReferenceImageCount { get; }
        public int ExternalImageCount { get; }
        public int NameOverlapCount { get; }
        public int ContentOverlapCount { get; }
        public string OverlapExample { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool HasErrors => Errors.Count > 0;
        public bool HasExternalImages => ExternalImageCount > 0;
        public bool HasContentOverlap => ContentOverlapCount > 0;
        public bool IsIndependentByContent => !HasErrors && HasExternalImages && !HasContentOverlap;
    }
}
