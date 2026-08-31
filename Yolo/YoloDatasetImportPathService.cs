using System.IO;

namespace MvcVisionSystem.Yolo
{
    public static class YoloDatasetImportPathService
    {
        public static string ResolveImageRoot(string annotationPath, string imageRoot)
            => string.IsNullOrWhiteSpace(imageRoot)
                ? Path.GetDirectoryName(Path.GetFullPath(annotationPath)) ?? string.Empty
                : Path.GetFullPath(imageRoot);

        public static string ResolveSourceImagePath(string imageRoot, string imagePath)
        {
            string normalized = NormalizePathSeparators(imagePath);
            return Path.IsPathRooted(normalized)
                ? normalized
                : Path.Combine(imageRoot ?? string.Empty, normalized);
        }

        public static string GetFileName(string imagePath)
            => Path.GetFileName(NormalizePathSeparators(imagePath));

        private static string NormalizePathSeparators(string path)
            => (path ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
    }
}
