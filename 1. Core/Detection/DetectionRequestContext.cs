using System;
using System.Drawing;
using System.IO;

namespace MvcVisionSystem._1._Core
{
    /// <summary>
    /// Shared request/image identity between detection transport and label application.
    /// </summary>
    internal sealed class DetectionRequestContext
    {
        public static readonly DetectionRequestContext Empty =
            new DetectionRequestContext(string.Empty, string.Empty, Size.Empty);

        public DetectionRequestContext(
            string imageName,
            string imagePath,
            Size imageSize,
            string requestId = "",
            string imageId = "")
        {
            ImageName = imageName ?? string.Empty;
            ImagePath = imagePath ?? string.Empty;
            ImageSize = imageSize;
            RequestId = requestId ?? string.Empty;
            ImageId = imageId ?? string.Empty;
        }

        public string ImageName { get; }

        public string ImagePath { get; }

        public Size ImageSize { get; }

        public string RequestId { get; }

        public string ImageId { get; }

        internal static DetectionRequestContext Capture(
            LabelingImageSnapshot image,
            Size imageSize,
            string requestId = "",
            string imageId = "")
        {
            LabelingImageSnapshot snapshot = image ?? LabelingImageSnapshot.Empty;
            return new DetectionRequestContext(
                snapshot.ImageName,
                snapshot.ImagePath,
                imageSize,
                requestId,
                imageId);
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ImagePath))
                {
                    return Path.GetFileName(ImagePath);
                }

                return !string.IsNullOrWhiteSpace(ImageName) ? ImageName : "(unknown)";
            }
        }

        internal bool Matches(DetectionRequestContext current)
        {
            if (ReferenceEquals(this, Empty) || IsEmpty())
            {
                return true;
            }

            if (current == null || current.IsEmpty())
            {
                return !HasIdentity;
            }

            if (!string.IsNullOrWhiteSpace(ImagePath) && !string.IsNullOrWhiteSpace(current.ImagePath)
                && !PathsEqual(ImagePath, current.ImagePath))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ImageName) && !string.IsNullOrWhiteSpace(current.ImageName)
                && !string.Equals(ImageName, current.ImageName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!ImageSize.IsEmpty && !current.ImageSize.IsEmpty && ImageSize != current.ImageSize)
            {
                return false;
            }

            return !HasIdentity || current.HasIdentity;
        }

        internal bool MatchesResponse(string requestId, string imageId)
        {
            if (!string.IsNullOrWhiteSpace(RequestId)
                && !string.IsNullOrWhiteSpace(requestId)
                && !string.Equals(RequestId, requestId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ImageId)
                && !string.IsNullOrWhiteSpace(imageId)
                && !string.Equals(ImageId, imageId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private bool HasIdentity =>
            !string.IsNullOrWhiteSpace(ImagePath) || !string.IsNullOrWhiteSpace(ImageName);

        private bool IsEmpty()
        {
            return !HasIdentity && ImageSize.IsEmpty;
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch
            {
                return path.Trim();
            }
        }

    }
}
