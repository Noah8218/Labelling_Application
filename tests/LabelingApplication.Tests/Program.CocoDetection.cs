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

internal static class CocoDetectionTests
{
    internal static void TestCocoDetectionExportService()
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

            string outputPath = Path.Combine(root, "exports", "coco-detection.json");
            CocoDetectionExportResult result = CocoDetectionExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "COCO export JSON was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.ImageCount);
            AssertEqual(1, result.AnnotationCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(2, result.SkippedAnnotationCount);

            CocoDetectionDataset exported = JsonConvert.DeserializeObject<CocoDetectionDataset>(File.ReadAllText(outputPath));
            AssertTrue(exported != null, "COCO export JSON did not deserialize");
            AssertEqual(2, exported.Images.Count);
            AssertEqual(2, exported.Categories.Count);
            AssertEqual(1, exported.Annotations.Count);
            AssertTrue(exported.Categories.Any(category => category.Id == 1 && category.Name == "OK"), "COCO export did not include OK category");
            AssertTrue(exported.Categories.Any(category => category.Id == 2 && category.Name == "NG"), "COCO export did not include NG category");

            CocoDetectionImage trainEntry = exported.Images.FirstOrDefault(image => image.FileName == "data/train/images/train-sample.png");
            CocoDetectionImage validEntry = exported.Images.FirstOrDefault(image => image.FileName == "data/valid/images/valid-empty.png");
            AssertTrue(trainEntry != null, "COCO export did not include train image with relative path");
            AssertTrue(validEntry != null, "COCO export did not include valid empty-label image with relative path");
            AssertEqual(100, trainEntry.Width);
            AssertEqual(200, trainEntry.Height);
            AssertEqual(50, validEntry.Width);
            AssertEqual(40, validEntry.Height);

            CocoDetectionAnnotation annotation = exported.Annotations[0];
            AssertEqual(trainEntry.Id, annotation.ImageId);
            AssertEqual(2, annotation.CategoryId);
            AssertEqual(0, annotation.IsCrowd);
            AssertEqual(1200D, annotation.Area);
            AssertEqual(4, annotation.BBox.Length);
            AssertEqual(10D, annotation.BBox[0]);
            AssertEqual(20D, annotation.BBox[1]);
            AssertEqual(30D, annotation.BBox[2]);
            AssertEqual(40D, annotation.BBox[3]);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestCocoDetectionImportService()
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

            var dataset = new CocoDetectionDataset();
            dataset.Categories.Add(new CocoDetectionCategory { Id = 1, Name = "OK", SuperCategory = "object" });
            dataset.Categories.Add(new CocoDetectionCategory { Id = 2, Name = "NG", SuperCategory = "object" });
            dataset.Images.Add(new CocoDetectionImage
            {
                Id = 10,
                FileName = "images/train-sample.png",
                Width = 100,
                Height = 200
            });
            dataset.Images.Add(new CocoDetectionImage
            {
                Id = 11,
                FileName = "images/empty-sample.png",
                Width = 50,
                Height = 40
            });
            dataset.Annotations.Add(new CocoDetectionAnnotation
            {
                Id = 1,
                ImageId = 10,
                CategoryId = 2,
                BBox = new[] { 10D, 20D, 30D, 40D },
                Area = 1200D,
                IsCrowd = 0
            });
            dataset.Annotations.Add(new CocoDetectionAnnotation
            {
                Id = 2,
                ImageId = 10,
                CategoryId = 99,
                BBox = new[] { 1D, 1D, 3D, 3D },
                Area = 9D,
                IsCrowd = 0
            });

            string annotationPath = Path.Combine(root, "annotations.json");
            File.WriteAllText(annotationPath, JsonConvert.SerializeObject(dataset, Formatting.Indented));

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));

            CocoDetectionImportResult result = CocoDetectionImportService.ImportDataset(
                data,
                annotationPath,
                sourceRoot,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(annotationPath, result.AnnotationPath);
            AssertEqual(Path.GetFullPath(sourceRoot), result.ImageRoot);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedImageCount);
            AssertEqual(2, result.LabelFileCount);
            AssertEqual(1, result.ImportedAnnotationCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedImageCount);
            AssertEqual(1, result.SkippedAnnotationCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("NG", data.ClassNamedList[1].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "train-sample.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "empty-sample.png");
            string importedLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "train-sample.txt");
            string importedEmptyLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "empty-sample.txt");
            AssertTrue(File.Exists(importedImagePath), "COCO import did not copy the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "COCO import did not copy the empty image");
            AssertTrue(File.Exists(importedLabelPath), "COCO import did not write the label file");
            AssertTrue(File.Exists(importedEmptyLabelPath), "COCO import did not write the empty label file");
            AssertEqual("1 0.25 0.2 0.3 0.2", File.ReadAllText(importedLabelPath).Trim());
            AssertEqual(string.Empty, File.ReadAllText(importedEmptyLabelPath).Trim());
            AssertTrue(File.Exists(data.DataYamlFilePath), "COCO import did not save data.yaml");

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectangles(
                importedLabelPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("NG"), "COCO import label should load as NG");
            AssertEqual(new Rectangle(10, 20, 30, 40), loaded["NG"][0]);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
