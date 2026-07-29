using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.DrawObject;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.ImageCanvas.Model;
using OpenVisionLab.ImageCanvas.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CvMat = OpenCvSharp.Mat;
using CvMatType = OpenCvSharp.MatType;
using CvScalar = OpenCvSharp.Scalar;

namespace LabelingApplication.Tests;

using static LabelingApplication.Tests.TestSupport;

internal static class LabelStudioDetectionTests
{
    internal static void TestLabelStudioDetectionExportService()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });
            data.ClassNamedList.Add(new CClassItem { Text = "NG", DrawColor = Color.Red });

            var rois = new Dictionary<string, List<CRectangleObject>>
            {
                ["NG"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(10, 20, 30, 40), cClassItem = data.ClassNamedList[1] }
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap validImage = CreateSolidBitmap(50, 40, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations(
                    "train-sample.png",
                    trainImage,
                    rois,
                    data.ClassNamedList,
                    data,
                    sourceImagePath: Path.Combine(root, "source", "train-sample.png"));

                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations(
                    "valid-empty.png",
                    validImage,
                    new Dictionary<string, List<CRectangleObject>>(),
                    data.ClassNamedList,
                    data,
                    sourceImagePath: Path.Combine(root, "source", "valid-empty.png"));
            }

            string trainLabelPath = Path.Combine(root, "data", "train", "labels", "train-sample.txt");
            File.AppendAllLines(trainLabelPath, new[] { "9 0.5 0.5 0.1 0.1", "bad line" });

            string outputPath = Path.Combine(root, "exports", "label-studio-detection.json");
            LabelStudioDetectionExportResult result = LabelStudioDetectionExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "Label Studio detection JSON was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.TaskCount);
            AssertEqual(2, result.ReviewedTaskCount);
            AssertEqual(1, result.ResultCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(2, result.SkippedAnnotationCount);

            List<LabelStudioDetectionTask> exported = JsonConvert.DeserializeObject<List<LabelStudioDetectionTask>>(File.ReadAllText(outputPath));
            AssertTrue(exported != null, "Label Studio detection JSON did not deserialize");
            AssertEqual(2, exported.Count);

            LabelStudioDetectionTask trainTask = exported.FirstOrDefault(task => task.Data?.Image == "data/train/images/train-sample.png");
            LabelStudioDetectionTask validTask = exported.FirstOrDefault(task => task.Data?.Image == "data/valid/images/valid-empty.png");
            AssertTrue(trainTask != null, "Label Studio export did not include train image with relative path");
            AssertTrue(validTask != null, "Label Studio export did not include valid empty-label image with relative path");
            AssertEqual(YoloDatasetSplitService.TrainMode, trainTask.Data.Split);
            AssertEqual(YoloDatasetSplitService.ValidMode, validTask.Data.Split);

            AssertEqual(1, trainTask.Annotations.Count);
            AssertEqual(1, trainTask.Annotations[0].Result.Count);
            LabelStudioDetectionResult annotation = trainTask.Annotations[0].Result[0];
            AssertEqual("bbox", annotation.FromName);
            AssertEqual("image", annotation.ToName);
            AssertEqual("$image", annotation.Source);
            AssertEqual("rectanglelabels", annotation.Type);
            AssertEqual("manual", annotation.Origin);
            AssertEqual(0, annotation.ImageRotation);
            AssertEqual(100, annotation.OriginalWidth);
            AssertEqual(200, annotation.OriginalHeight);
            AssertEqual(10D, annotation.Value.X);
            AssertEqual(10D, annotation.Value.Y);
            AssertEqual(30D, annotation.Value.Width);
            AssertEqual(20D, annotation.Value.Height);
            AssertEqual(0, annotation.Value.Rotation);
            AssertEqual(1, annotation.Value.RectangleLabels.Length);
            AssertEqual("NG", annotation.Value.RectangleLabels[0]);

            AssertEqual(1, validTask.Annotations.Count);
            AssertEqual(0, validTask.Annotations[0].Result.Count);
            AssertTrue(!validTask.Annotations[0].WasCancelled, "empty reviewed Label Studio annotation should not be cancelled");
            AssertTrue(!validTask.Annotations[0].GroundTruth, "empty reviewed Label Studio annotation should not be marked ground truth");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestLabelStudioDetectionImportService()
    {
        string root = CreateTempRoot();
        try
        {
            string sourceRoot = Path.Combine(root, "source");
            string sourceImageDirectory = Path.Combine(sourceRoot, "images");
            Directory.CreateDirectory(sourceImageDirectory);
            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap emptyImage = CreateSolidBitmap(50, 40, Color.White))
            {
                trainImage.Save(Path.Combine(sourceImageDirectory, "train-sample.png"));
                emptyImage.Save(Path.Combine(sourceImageDirectory, "empty-sample.png"));
            }

            var tasks = new List<LabelStudioDetectionTask>
            {
                new LabelStudioDetectionTask
                {
                    Id = 1,
                    Data = new LabelStudioDetectionTaskData
                    {
                        Image = "images/train-sample.png",
                        Split = YoloDatasetSplitService.TrainMode
                    }
                },
                new LabelStudioDetectionTask
                {
                    Id = 2,
                    Data = new LabelStudioDetectionTaskData
                    {
                        Image = "images/empty-sample.png",
                        Split = YoloDatasetSplitService.TrainMode
                    }
                }
            };
            tasks[0].Annotations.Add(new LabelStudioDetectionAnnotation
            {
                Id = "1",
                Result = new List<LabelStudioDetectionResult>
                {
                    new LabelStudioDetectionResult
                    {
                        Id = "r1",
                        FromName = "bbox",
                        ToName = "image",
                        Source = "$image",
                        Type = "rectanglelabels",
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioDetectionValue
                        {
                            X = 10,
                            Y = 10,
                            Width = 30,
                            Height = 20,
                            RectangleLabels = new[] { "NG" }
                        }
                    },
                    new LabelStudioDetectionResult
                    {
                        Id = "invalid",
                        FromName = "bbox",
                        ToName = "image",
                        Source = "$image",
                        Type = "rectanglelabels",
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioDetectionValue
                        {
                            X = 0,
                            Y = 0,
                            Width = 0,
                            Height = 10,
                            RectangleLabels = new[] { "NG" }
                        }
                    },
                    new LabelStudioDetectionResult
                    {
                        Id = "rotated-image",
                        FromName = "bbox",
                        ToName = "image",
                        Source = "$image",
                        Type = "rectanglelabels",
                        ImageRotation = 90,
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioDetectionValue
                        {
                            X = 10,
                            Y = 10,
                            Width = 30,
                            Height = 20,
                            RectangleLabels = new[] { "NG" }
                        }
                    },
                    new LabelStudioDetectionResult
                    {
                        Id = "rotated-box",
                        FromName = "bbox",
                        ToName = "image",
                        Source = "$image",
                        Type = "rectanglelabels",
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioDetectionValue
                        {
                            X = 10,
                            Y = 10,
                            Width = 30,
                            Height = 20,
                            Rotation = 15,
                            RectangleLabels = new[] { "NG" }
                        }
                    }
                }
            });

            string taskJsonPath = Path.Combine(root, "label-studio-tasks.json");
            File.WriteAllText(taskJsonPath, JsonConvert.SerializeObject(tasks, Formatting.Indented));

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            LabelStudioDetectionImportResult result = LabelStudioDetectionImportService.ImportTasks(
                data,
                taskJsonPath,
                sourceRoot,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(taskJsonPath, result.TaskJsonPath);
            AssertEqual(Path.GetFullPath(sourceRoot), result.ImageRoot);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedTaskCount);
            AssertEqual(2, result.LabelFileCount);
            AssertEqual(1, result.ImportedResultCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedTaskCount);
            AssertEqual(3, result.SkippedResultCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("NG", data.ClassNamedList[1].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "train-sample.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "empty-sample.png");
            string importedLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "train-sample.txt");
            string importedEmptyLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "empty-sample.txt");
            AssertTrue(File.Exists(importedImagePath), "Label Studio import did not copy the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "Label Studio import did not copy the empty image");
            AssertTrue(File.Exists(importedLabelPath), "Label Studio import did not write the label file");
            AssertTrue(File.Exists(importedEmptyLabelPath), "Label Studio import did not write the empty label file");
            AssertEqual("1 0.25 0.2 0.3 0.2", File.ReadAllText(importedLabelPath).Trim());
            AssertEqual(string.Empty, File.ReadAllText(importedEmptyLabelPath).Trim());
            AssertTrue(File.Exists(data.DataYamlFilePath), "Label Studio import did not save data.yaml");

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectangles(
                importedLabelPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("NG"), "Label Studio import label should load as NG");
            AssertEqual(new Rectangle(10, 20, 30, 40), loaded["NG"][0]);

            DatasetInterchangePreflightReport preflight = new DatasetInterchangePreflightService().DryRun(
                new DatasetInterchangeRequest
                {
                    Data = data,
                    FormatKey = "label-studio-detection-import",
                    SourcePath = taskJsonPath,
                    ImageRoot = sourceRoot,
                    TargetSplit = YoloDatasetSplitService.TrainMode
                });
            AssertTrue(!preflight.CanApply, "Label Studio rotated detection input should block Apply");
            AssertEqual(3, preflight.SkippedCount);
            AssertTrue(
                preflight.Issues.Any(issue => issue.Contains("3", StringComparison.Ordinal)),
                "Label Studio preflight should expose the unsupported rotated/invalid record count");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
