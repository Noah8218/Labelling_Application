using System;
using System.IO;

namespace MvcVisionSystem
{
    public class YoloDatasetSettings
    {
        public string OutputRootPath { get; set; } = "";

        public string DataYamlFilePath { get; set; } = "";

        public string TrainImagesPath => Path.Combine(OutputRootPath, "data", "train", "images");

        public string TrainLabelsPath => Path.Combine(OutputRootPath, "data", "train", "labels");

        public string ValidImagesPath => Path.Combine(OutputRootPath, "data", "valid", "images");

        public string ValidLabelsPath => Path.Combine(OutputRootPath, "data", "valid", "labels");

        public string TestImagesPath => Path.Combine(OutputRootPath, "data", "test", "images");

        public string TestLabelsPath => Path.Combine(OutputRootPath, "data", "test", "labels");

        public int ValidationPercent { get; set; } = 20;

        public int TestPercent { get; set; } = 0;

        public int SplitSeed { get; set; } = 17;

        public void ConfigureOutputRoot(string outputRootPath)
        {
            if (string.IsNullOrWhiteSpace(outputRootPath))
            {
                return;
            }

            OutputRootPath = outputRootPath;
            DataYamlFilePath = Path.Combine(outputRootPath, "data.yaml");
        }

        public string ResolveOutputRootPath(string fallbackRootPath)
        {
            if (!string.IsNullOrWhiteSpace(OutputRootPath))
            {
                return OutputRootPath;
            }

            if (!string.IsNullOrWhiteSpace(DataYamlFilePath))
            {
                if (IsYamlFilePath(DataYamlFilePath))
                {
                    string directoryName = Path.GetDirectoryName(DataYamlFilePath);
                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        return directoryName;
                    }
                }

                return DataYamlFilePath;
            }

            return fallbackRootPath;
        }

        public static bool IsYamlFilePath(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase);
        }
    }
}
