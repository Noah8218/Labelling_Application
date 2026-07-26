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

internal static class CocoSegmentationTests
{
    internal static void TestCocoSegmentationExportService()
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

            string outputPath = Path.Combine(root, "exports", "coco-segmentation.json");
            CocoSegmentationExportResult result = CocoSegmentationExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "COCO segmentation export JSON was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.ImageCount);
            AssertEqual(1, result.AnnotationCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(1, result.SkippedAnnotationCount);

            CocoSegmentationDataset exported = JsonConvert.DeserializeObject<CocoSegmentationDataset>(File.ReadAllText(outputPath));
            AssertTrue(exported != null, "COCO segmentation export JSON did not deserialize");
            AssertEqual(2, exported.Images.Count);
            AssertEqual(2, exported.Categories.Count);
            AssertEqual(1, exported.Annotations.Count);
            AssertTrue(exported.Categories.Any(category => category.Id == 1 && category.Name == "OK"), "COCO segmentation export did not include OK category");
            AssertTrue(exported.Categories.Any(category => category.Id == 2 && category.Name == "Defect"), "COCO segmentation export did not include Defect category");

            CocoSegmentationImage trainEntry = exported.Images.FirstOrDefault(image => image.FileName == "data/train/images/seg-train.png");
            CocoSegmentationImage validEntry = exported.Images.FirstOrDefault(image => image.FileName == "data/valid/images/seg-valid-empty.png");
            AssertTrue(trainEntry != null, "COCO segmentation export did not include train image with relative path");
            AssertTrue(validEntry != null, "COCO segmentation export did not include valid empty-label image with relative path");
            AssertEqual(100, trainEntry.Width);
            AssertEqual(200, trainEntry.Height);
            AssertEqual(50, validEntry.Width);
            AssertEqual(40, validEntry.Height);

            CocoSegmentationAnnotation annotation = exported.Annotations[0];
            AssertEqual(trainEntry.Id, annotation.ImageId);
            AssertEqual(2, annotation.CategoryId);
            AssertEqual(0, annotation.IsCrowd);
            AssertEqual(1200D, annotation.Area);
            AssertEqual(4, annotation.BBox.Length);
            AssertEqual(10D, annotation.BBox[0]);
            AssertEqual(20D, annotation.BBox[1]);
            AssertEqual(30D, annotation.BBox[2]);
            AssertEqual(40D, annotation.BBox[3]);
            AssertEqual(1, annotation.Segmentation.Count);
            AssertEqual(8, annotation.Segmentation[0].Length);
            AssertEqual(10D, annotation.Segmentation[0][0]);
            AssertEqual(20D, annotation.Segmentation[0][1]);
            AssertEqual(40D, annotation.Segmentation[0][2]);
            AssertEqual(60D, annotation.Segmentation[0][5]);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestCocoSegmentationImportService()
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

            var dataset = new CocoSegmentationDataset();
            dataset.Categories.Add(new CocoSegmentationCategory { Id = 1, Name = "OK", SuperCategory = "object" });
            dataset.Categories.Add(new CocoSegmentationCategory { Id = 2, Name = "Defect", SuperCategory = "object" });
            dataset.Images.Add(new CocoSegmentationImage
            {
                Id = 10,
                FileName = "images/seg-train.png",
                Width = 100,
                Height = 200
            });
            dataset.Images.Add(new CocoSegmentationImage
            {
                Id = 11,
                FileName = "images/seg-empty.png",
                Width = 50,
                Height = 40
            });
            dataset.Annotations.Add(new CocoSegmentationAnnotation
            {
                Id = 1,
                ImageId = 10,
                CategoryId = 2,
                Segmentation = new List<double[]>
                {
                    new[] { 10D, 20D, 40D, 20D, 40D, 60D, 10D, 60D }
                },
                BBox = new[] { 10D, 20D, 30D, 40D },
                Area = 1200D,
                IsCrowd = 0
            });
            dataset.Annotations.Add(new CocoSegmentationAnnotation
            {
                Id = 2,
                ImageId = 10,
                CategoryId = 99,
                Segmentation = new List<double[]>
                {
                    new[] { 1D, 1D, 5D, 1D, 5D, 5D, 1D, 5D }
                },
                BBox = new[] { 1D, 1D, 4D, 4D },
                Area = 16D,
                IsCrowd = 0
            });

            string annotationPath = Path.Combine(root, "coco-segmentation.json");
            File.WriteAllText(annotationPath, JsonConvert.SerializeObject(dataset, Formatting.Indented));

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;

            CocoSegmentationImportResult result = CocoSegmentationImportService.ImportDataset(
                data,
                annotationPath,
                sourceRoot,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(annotationPath, result.AnnotationPath);
            AssertEqual(Path.GetFullPath(sourceRoot), result.ImageRoot);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedImageCount);
            AssertEqual(1, result.ImportedAnnotationCount);
            AssertEqual(1, result.ImportedSegmentFileCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedImageCount);
            AssertEqual(1, result.SkippedAnnotationCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("Defect", data.ClassNamedList[1].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "seg-train.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "seg-empty.png");
            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-train.json");
            string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "seg-train.png");
            string emptySegmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-empty.json");
            AssertTrue(File.Exists(importedImagePath), "COCO segmentation import did not copy the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "COCO segmentation import did not copy the empty image");
            AssertTrue(File.Exists(segmentPath), "COCO segmentation import did not write the segment JSON");
            AssertTrue(File.Exists(maskPath), "COCO segmentation import did not write the segmentation mask");
            AssertTrue(!File.Exists(emptySegmentPath), "COCO segmentation import should not create a segment JSON for empty images");
            AssertTrue(File.Exists(data.DataYamlFilePath), "COCO segmentation import did not save data.yaml");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded = YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("Defect"), "COCO segmentation import segment should load as Defect");
            AssertEqual(1, loaded["Defect"].Count);
            AssertEqual(new Point(10, 20), loaded["Defect"][0].Points[0]);

            using (Bitmap mask = new Bitmap(maskPath))
            {
                AssertEqual(2, mask.GetPixel(15, 25).R);
                AssertEqual(0, mask.GetPixel(1, 1).R);
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
