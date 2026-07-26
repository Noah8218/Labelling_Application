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

internal static class LabelStudioSegmentationTests
{
    internal static void TestLabelStudioSegmentationExportService()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });
            data.EnsureYoloOutputDirectories();

            var segments = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject>
                {
                    new LabelingSegmentationObject(
                        new[]
                        {
                            new Point(10, 20),
                            new Point(40, 20),
                            new Point(40, 60),
                            new Point(10, 60)
                        },
                        data.ClassNamedList[1])
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap validImage = CreateSolidBitmap(50, 40, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                trainImage.Save(Path.Combine(data.TrainImagesPath, "seg-train.png"));
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                    "seg-train.png",
                    trainImage,
                    segments,
                    data.ClassNamedList,
                    data);

                validImage.Save(Path.Combine(data.ValidImagesPath, "seg-valid-empty.png"));
            }

            string segmentPath = Path.Combine(root, "data", "train", "segments", "seg-train.json");
            SegmentationAnnotationFile annotationFile = JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
            annotationFile.Polygons.Add(new SegmentationPolygonRecord
            {
                ClassIndex = 9,
                ClassName = "Missing",
                Points = new List<SegmentationPointRecord>
                {
                    new SegmentationPointRecord { X = 1, Y = 1 },
                    new SegmentationPointRecord { X = 5, Y = 1 },
                    new SegmentationPointRecord { X = 5, Y = 5 },
                    new SegmentationPointRecord { X = 1, Y = 5 }
                }
            });
            File.WriteAllText(segmentPath, JsonConvert.SerializeObject(annotationFile, Formatting.Indented));

            string outputPath = Path.Combine(root, "exports", "label-studio-segmentation.json");
            LabelStudioSegmentationExportResult result = LabelStudioSegmentationExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "Label Studio segmentation JSON was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.TaskCount);
            AssertEqual(1, result.ReviewedTaskCount);
            AssertEqual(1, result.ResultCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(1, result.SkippedAnnotationCount);

            List<LabelStudioSegmentationTask> exported = JsonConvert.DeserializeObject<List<LabelStudioSegmentationTask>>(File.ReadAllText(outputPath));
            AssertTrue(exported != null, "Label Studio segmentation JSON did not deserialize");
            AssertEqual(2, exported.Count);

            LabelStudioSegmentationTask trainTask = exported.FirstOrDefault(task => task.Data?.Image == "data/train/images/seg-train.png");
            LabelStudioSegmentationTask validTask = exported.FirstOrDefault(task => task.Data?.Image == "data/valid/images/seg-valid-empty.png");
            AssertTrue(trainTask != null, "Label Studio segmentation export did not include train image with relative path");
            AssertTrue(validTask != null, "Label Studio segmentation export did not include valid image with relative path");
            AssertEqual(YoloDatasetSplitService.TrainMode, trainTask.Data.Split);
            AssertEqual(YoloDatasetSplitService.ValidMode, validTask.Data.Split);

            AssertEqual(1, trainTask.Annotations.Count);
            AssertEqual(1, trainTask.Annotations[0].Result.Count);
            LabelStudioSegmentationResult annotation = trainTask.Annotations[0].Result[0];
            AssertEqual("polygon", annotation.FromName);
            AssertEqual("image", annotation.ToName);
            AssertEqual("$image", annotation.Source);
            AssertEqual("polygonlabels", annotation.Type);
            AssertEqual("manual", annotation.Origin);
            AssertEqual(0, annotation.ImageRotation);
            AssertEqual(100, annotation.OriginalWidth);
            AssertEqual(200, annotation.OriginalHeight);
            AssertEqual(4, annotation.Value.Points.Count);
            AssertEqual(10D, annotation.Value.Points[0][0]);
            AssertEqual(10D, annotation.Value.Points[0][1]);
            AssertEqual(40D, annotation.Value.Points[1][0]);
            AssertEqual(30D, annotation.Value.Points[2][1]);
            AssertEqual(1, annotation.Value.PolygonLabels.Length);
            AssertEqual("Defect", annotation.Value.PolygonLabels[0]);

            AssertEqual(0, validTask.Annotations.Count);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestLabelStudioSegmentationImportService()
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
                trainImage.Save(Path.Combine(sourceImageDirectory, "seg-train.png"));
                emptyImage.Save(Path.Combine(sourceImageDirectory, "seg-empty.png"));
            }

            var tasks = new List<LabelStudioSegmentationTask>
            {
                new LabelStudioSegmentationTask
                {
                    Id = 1,
                    Data = new LabelStudioSegmentationTaskData
                    {
                        Image = "images/seg-train.png",
                        Split = YoloDatasetSplitService.TrainMode
                    }
                },
                new LabelStudioSegmentationTask
                {
                    Id = 2,
                    Data = new LabelStudioSegmentationTaskData
                    {
                        Image = "images/seg-empty.png",
                        Split = YoloDatasetSplitService.TrainMode
                    }
                }
            };
            tasks[0].Annotations.Add(new LabelStudioSegmentationAnnotation
            {
                Id = "1",
                Result = new List<LabelStudioSegmentationResult>
                {
                    new LabelStudioSegmentationResult
                    {
                        Id = "r1",
                        FromName = "polygon",
                        ToName = "image",
                        Source = "$image",
                        Type = "polygonlabels",
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioSegmentationValue
                        {
                            Points = new List<double[]>
                            {
                                new[] { 10D, 10D },
                                new[] { 40D, 10D },
                                new[] { 40D, 30D },
                                new[] { 10D, 30D }
                            },
                            PolygonLabels = new[] { "Defect" }
                        }
                    },
                    new LabelStudioSegmentationResult
                    {
                        Id = "invalid",
                        FromName = "polygon",
                        ToName = "image",
                        Source = "$image",
                        Type = "polygonlabels",
                        OriginalWidth = 100,
                        OriginalHeight = 200,
                        Value = new LabelStudioSegmentationValue
                        {
                            Points = new List<double[]>
                            {
                                new[] { 1D, 1D },
                                new[] { 2D, 2D }
                            },
                            PolygonLabels = new[] { "Defect" }
                        }
                    }
                }
            });

            string taskJsonPath = Path.Combine(root, "label-studio-segmentation.json");
            File.WriteAllText(taskJsonPath, JsonConvert.SerializeObject(tasks, Formatting.Indented));

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;

            LabelStudioSegmentationImportResult result = LabelStudioSegmentationImportService.ImportTasks(
                data,
                taskJsonPath,
                sourceRoot,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(taskJsonPath, result.TaskJsonPath);
            AssertEqual(Path.GetFullPath(sourceRoot), result.ImageRoot);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedTaskCount);
            AssertEqual(1, result.ImportedResultCount);
            AssertEqual(1, result.ImportedSegmentFileCount);
            AssertEqual(1, result.CategoryCount);
            AssertEqual(0, result.SkippedTaskCount);
            AssertEqual(1, result.SkippedResultCount);
            AssertEqual(1, data.ClassNamedList.Count);
            AssertEqual("Defect", data.ClassNamedList[0].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "seg-train.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "seg-empty.png");
            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-train.json");
            string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "seg-train.png");
            string emptySegmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-empty.json");
            AssertTrue(File.Exists(importedImagePath), "Label Studio segmentation import did not copy the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "Label Studio segmentation import did not copy the empty image");
            AssertTrue(File.Exists(segmentPath), "Label Studio segmentation import did not write the segment JSON");
            AssertTrue(File.Exists(maskPath), "Label Studio segmentation import did not write the segmentation mask");
            AssertTrue(!File.Exists(emptySegmentPath), "Label Studio segmentation import should not create a segment JSON for empty images");
            AssertTrue(File.Exists(data.DataYamlFilePath), "Label Studio segmentation import did not save data.yaml");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded = YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("Defect"), "Label Studio segmentation import segment should load as Defect");
            AssertEqual(1, loaded["Defect"].Count);
            AssertEqual(new Point(10, 20), loaded["Defect"][0].Points[0]);

            using (Bitmap mask = new Bitmap(maskPath))
            {
                AssertEqual(1, mask.GetPixel(15, 25).R);
                AssertEqual(0, mask.GetPixel(1, 1).R);
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
