using System;
using System.Collections.Generic;
using System.IO;

namespace MvcVisionSystem
{
    public class PythonModelSettings
    {
        public const string EngineYoloV5 = "YOLOv5";
        public const string EngineYoloV8 = "YOLOv8";
        public const string EngineYolo11 = "YOLO11";
        public const string EngineUnet = "U-Net";
        public const string EngineOnnx = "ONNX";

        private const string ProjectRootPathDefault = @"C:\Git\yolov5";
        private const string BundledTrainImageRootPathDefault = @"C:\Git\yolov5\data\train\images";
        private const string BundledValidImageRootPathDefault = @"C:\Git\yolov5\data\valid\images";
        private const string LegacyTrainedImageRootPathDefault = @"C:\Git\py\data\train\images";
        private const string LegacyImageRootPathDefault = @"C:\Git\py\KtemData";
        private const string RetiredProjectRootPath = @"C:\Git\새 폴더\yolov5";
        private const string RetiredImageRootPath = @"C:\Git\새 폴더\py\KtemData";

        public string PythonExecutablePath { get; set; } = "";

        public string ModelEngine { get; set; } = EngineYoloV5;

        public string ProjectRootPath { get; set; } = GetDefaultProjectRootPath();

        public string ClientScriptPath { get; set; } = Path.Combine(GetDefaultProjectRootPath(), "labelling_tcp_client.py");

        public string WeightsPath { get; set; } = Path.Combine(GetDefaultProjectRootPath(), "best.pt");

        public string ImageRootPath { get; set; } = GetDefaultImageRootPath();

        public float MinimumDetectionConfidence { get; set; } = 0.25F;

        public int MaximumDetectionCandidates { get; set; } = 20;

        public int InferenceImageSize { get; set; } = 320;

        public int DetectionTimeoutSeconds { get; set; } = 30;

        public bool AutoStartClient { get; set; } = true;

        public void EnsureDefaults()
        {
            MigrateRetiredDefaults();
            RepairPortableYoloPaths();
            ModelEngine = NormalizeModelEngine(ModelEngine);

            if (string.IsNullOrWhiteSpace(ProjectRootPath))
            {
                ProjectRootPath = GetDefaultProjectRootPath();
            }

            if (string.IsNullOrWhiteSpace(ClientScriptPath))
            {
                ClientScriptPath = ModelEngine == EngineUnet
                    ? _1._Core.PythonModelRuntimeBundledWorkerService.ResolveUnetWorkerScriptPath()
                    : Path.Combine(ProjectRootPath, "labelling_tcp_client.py");
            }

            if (string.IsNullOrWhiteSpace(WeightsPath))
            {
                WeightsPath = ModelEngine == EngineUnet
                    ? GetDefaultUnetWeightsPath(ProjectRootPath)
                    : Path.Combine(ProjectRootPath, "best.pt");
            }

            if (string.IsNullOrWhiteSpace(ImageRootPath))
            {
                ImageRootPath = GetDefaultImageRootPath();
            }

            MinimumDetectionConfidence = Math.Clamp(MinimumDetectionConfidence, 0F, 1F);
            MaximumDetectionCandidates = Math.Clamp(MaximumDetectionCandidates, 1, 200);
            InferenceImageSize = Math.Clamp(InferenceImageSize, 64, 2048);
            DetectionTimeoutSeconds = Math.Clamp(DetectionTimeoutSeconds, 1, 600);
        }

        public static IReadOnlyList<string> GetSupportedModelEngines()
            => new[] { EngineYoloV5, EngineYoloV8, EngineYolo11, EngineUnet, EngineOnnx };

        public static string NormalizeModelEngine(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (string.Equals(normalized, "yolov8", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "yolo8", StringComparison.OrdinalIgnoreCase))
            {
                return EngineYoloV8;
            }

            if (string.Equals(normalized, "yolo11", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "yolov11", StringComparison.OrdinalIgnoreCase))
            {
                return EngineYolo11;
            }

            if (string.Equals(normalized, "unet", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "u-net", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "u net", StringComparison.OrdinalIgnoreCase))
            {
                return EngineUnet;
            }

            if (string.Equals(normalized, "onnx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "onnxruntime", StringComparison.OrdinalIgnoreCase))
            {
                return EngineOnnx;
            }

            return EngineYoloV5;
        }

        public string GetProtocolModelName()
        {
            return NormalizeModelEngine(ModelEngine) switch
            {
                EngineYoloV8 => "yolov8",
                EngineYolo11 => "yolo11",
                EngineUnet => "unet",
                EngineOnnx => "onnx",
                _ => "yolov5"
            };
        }

        public string GetModelRootPath()
        {
            string projectRootPath = ProjectRootPath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                return string.Empty;
            }

            return NormalizeModelEngine(ModelEngine) switch
            {
                EngineYoloV5 => Path.Combine(projectRootPath, "yolov5Master"),
                _ => projectRootPath
            };
        }

        public static string GetDefaultProjectRootPath()
        {
            string siblingRoot = ResolveSiblingPath("yolov5");
            if (Directory.Exists(siblingRoot))
            {
                return siblingRoot;
            }

            return Directory.Exists(ProjectRootPathDefault) ? ProjectRootPathDefault : siblingRoot;
        }

        public static string GetDefaultUnetProjectRootPath()
        {
            string siblingRoot = ResolveSiblingPath("unet");
            return Directory.Exists(siblingRoot) ? siblingRoot : Path.Combine(@"C:\Git", "unet");
        }

        public static string GetDefaultUnetWeightsPath(string projectRootPath = "")
        {
            string root = string.IsNullOrWhiteSpace(projectRootPath)
                ? GetDefaultUnetProjectRootPath()
                : projectRootPath.Trim();
            return Path.Combine(root, "runs", "segment", "openvisionlab-unet-segmentation", "weights", "best.pt");
        }

        public static string GetDefaultImageRootPath()
        {
            string projectRootPath = GetDefaultProjectRootPath();
            string siblingPyRoot = ResolveSiblingPath("py");
            foreach (string candidate in new[]
            {
                Path.Combine(projectRootPath, "data", "train", "images"),
                Path.Combine(projectRootPath, "data", "valid", "images"),
                Path.Combine(projectRootPath, "data", "images"),
                Path.Combine(projectRootPath, "yolov5Master", "data", "images"),
                BundledTrainImageRootPathDefault,
                BundledValidImageRootPathDefault,
                Path.Combine(siblingPyRoot, "data", "train", "images"),
                Path.Combine(siblingPyRoot, "KtemData"),
                LegacyTrainedImageRootPathDefault,
                LegacyImageRootPathDefault
            })
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return "";
        }

        public string GetRequirementsPath()
        {
            return string.IsNullOrWhiteSpace(ProjectRootPath)
                ? string.Empty
                : Path.Combine(ProjectRootPath, "requirements.txt");
        }

        private void RepairPortableYoloPaths()
        {
            string defaultProjectRootPath = GetDefaultProjectRootPath();
            bool repairedProjectRoot = false;
            if (string.IsNullOrWhiteSpace(ProjectRootPath) && IsUsableYoloProjectRoot(defaultProjectRootPath))
            {
                ProjectRootPath = defaultProjectRootPath;
                repairedProjectRoot = true;
            }

            string preferredScriptPath = ResolvePreferredClientScriptPath(ProjectRootPath);
            if (!string.IsNullOrWhiteSpace(preferredScriptPath)
                && (repairedProjectRoot || string.IsNullOrWhiteSpace(ClientScriptPath)))
            {
                ClientScriptPath = preferredScriptPath;
            }

            string preferredWeightsPath = string.IsNullOrWhiteSpace(ProjectRootPath)
                ? string.Empty
                : Path.Combine(ProjectRootPath, "best.pt");
            if (!string.IsNullOrWhiteSpace(preferredWeightsPath)
                && File.Exists(preferredWeightsPath)
                && (repairedProjectRoot || string.IsNullOrWhiteSpace(WeightsPath)))
            {
                WeightsPath = preferredWeightsPath;
            }

            string preferredImageRootPath = GetDefaultImageRootPath();
            if (!string.IsNullOrWhiteSpace(preferredImageRootPath)
                && Directory.Exists(preferredImageRootPath)
                && (repairedProjectRoot || string.IsNullOrWhiteSpace(ImageRootPath)))
            {
                ImageRootPath = preferredImageRootPath;
            }
        }

        private static bool IsUsableYoloProjectRoot(string projectRootPath)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath) || !Directory.Exists(projectRootPath))
            {
                return false;
            }

            string clientScriptPath = ResolvePreferredClientScriptPath(projectRootPath);
            return File.Exists(clientScriptPath)
                && (File.Exists(Path.Combine(projectRootPath, "requirements.txt"))
                    || Directory.Exists(Path.Combine(projectRootPath, "yolov5Master")));
        }

        private static string ResolvePreferredClientScriptPath(string projectRootPath)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                return string.Empty;
            }

            foreach (string fileName in new[] { "labelling_tcp_client.py", "labeling_tcp_client.py" })
            {
                string candidate = Path.Combine(projectRootPath, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(projectRootPath, "labelling_tcp_client.py");
        }

        private static string ResolveSiblingPath(string directoryName)
        {
            string repositoryRoot = FindLabelingRepositoryRoot();
            string parent = string.IsNullOrWhiteSpace(repositoryRoot)
                ? Directory.GetParent(AppContext.BaseDirectory)?.FullName
                : Directory.GetParent(repositoryRoot)?.FullName;

            return string.IsNullOrWhiteSpace(parent)
                ? Path.Combine(AppContext.BaseDirectory, directoryName)
                : Path.Combine(parent, directoryName);
        }

        private static string FindLabelingRepositoryRoot()
        {
            foreach (string startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                string current = startPath;
                while (!string.IsNullOrWhiteSpace(current))
                {
                    if (File.Exists(Path.Combine(current, "OpenVisionLab.LabelingStudio.sln"))
                        || File.Exists(Path.Combine(current, "OpenVisionLab.LabelingStudio.csproj"))
                        || File.Exists(Path.Combine(current, "MvcVisionSystem.sln"))
                        || File.Exists(Path.Combine(current, "MvcVisionSystem.csproj")))
                    {
                        return current;
                    }

                    current = Directory.GetParent(current)?.FullName;
                }
            }

            return string.Empty;
        }

        private void MigrateRetiredDefaults()
        {
            string defaultProjectRootPath = GetDefaultProjectRootPath();

            if (string.Equals(ProjectRootPath, RetiredProjectRootPath, StringComparison.OrdinalIgnoreCase))
            {
                ProjectRootPath = defaultProjectRootPath;
            }

            string retiredScriptPath = Path.Combine(RetiredProjectRootPath, "labelling_tcp_client.py");
            if (string.Equals(ClientScriptPath, retiredScriptPath, StringComparison.OrdinalIgnoreCase))
            {
                ClientScriptPath = Path.Combine(defaultProjectRootPath, "labelling_tcp_client.py");
            }

            string retiredWeightsPath = Path.Combine(RetiredProjectRootPath, "best.pt");
            if (string.Equals(WeightsPath, retiredWeightsPath, StringComparison.OrdinalIgnoreCase))
            {
                WeightsPath = Path.Combine(defaultProjectRootPath, "best.pt");
            }

            if (string.Equals(ImageRootPath, RetiredImageRootPath, StringComparison.OrdinalIgnoreCase))
            {
                ImageRootPath = GetDefaultImageRootPath();
            }
        }
    }
}
