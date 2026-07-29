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

internal static class CvatTests
{
    internal static void TestCvatImageTaskArchiveExportService()
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

            string outputPath = Path.Combine(root, "exports", "cvat-images.zip");
            CvatImageTaskArchiveExportResult result = CvatImageTaskArchiveExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "CVAT image task archive was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.ImageCount);
            AssertEqual(1, result.BoxCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(2, result.SkippedAnnotationCount);
            AssertTrue(result.ArchiveEntryNames.Contains("annotations.xml"), "CVAT archive result should list annotations.xml");
            AssertTrue(result.ArchiveEntryNames.Contains("images/train/train-sample.png"), "CVAT archive result should list train image entry");
            AssertTrue(result.ArchiveEntryNames.Contains("images/valid/valid-empty.png"), "CVAT archive result should list valid image entry");

            using (ZipArchive archive = ZipFile.OpenRead(outputPath))
            {
                AssertTrue(archive.GetEntry("annotations.xml") != null, "CVAT archive should contain annotations.xml");
                AssertTrue(archive.GetEntry("images/train/train-sample.png") != null, "CVAT archive should contain train image");
                AssertTrue(archive.GetEntry("images/valid/valid-empty.png") != null, "CVAT archive should contain valid image");

                ZipArchiveEntry annotationEntry = archive.GetEntry("annotations.xml");
                using Stream annotationStream = annotationEntry.Open();
                XDocument document = XDocument.Load(annotationStream);
                XElement rootElement = document.Root;
                AssertTrue(rootElement != null, "CVAT annotations XML should have a root");
                AssertEqual("annotations", rootElement.Name.LocalName);
                AssertEqual("1.1", rootElement.Element("version")?.Value);

                XElement task = rootElement.Element("meta")?.Element("task");
                AssertTrue(task != null, "CVAT annotations XML should include task metadata");
                AssertEqual("OpenVisionLab Labeling Studio export", task.Element("name")?.Value);
                AssertEqual("2", task.Element("size")?.Value);
                AssertEqual("annotation", task.Element("mode")?.Value);

                XElement labels = task.Element("labels");
                AssertTrue(labels != null, "CVAT task metadata should include labels");
                AssertTrue(labels.Elements("label").Any(label =>
                    label.Element("name")?.Value == "OK"
                    && label.Element("type")?.Value == "bbox"), "CVAT labels should include OK bbox label");
                AssertTrue(labels.Elements("label").Any(label =>
                    label.Element("name")?.Value == "NG"
                    && label.Element("type")?.Value == "bbox"), "CVAT labels should include NG bbox label");

                XElement trainImageElement = rootElement.Elements("image")
                    .FirstOrDefault(image => image.Attribute("name")?.Value == "train/train-sample.png");
                XElement validImageElement = rootElement.Elements("image")
                    .FirstOrDefault(image => image.Attribute("name")?.Value == "valid/valid-empty.png");
                AssertTrue(trainImageElement != null, "CVAT annotations XML should include train image");
                AssertTrue(validImageElement != null, "CVAT annotations XML should include valid empty-label image");
                AssertEqual("0", trainImageElement.Attribute("id")?.Value);
                AssertEqual("100", trainImageElement.Attribute("width")?.Value);
                AssertEqual("200", trainImageElement.Attribute("height")?.Value);

                XElement box = trainImageElement.Element("box");
                AssertTrue(box != null, "CVAT train image should include one box");
                AssertEqual("NG", box.Attribute("label")?.Value);
                AssertEqual("10.00", box.Attribute("xtl")?.Value);
                AssertEqual("20.00", box.Attribute("ytl")?.Value);
                AssertEqual("40.00", box.Attribute("xbr")?.Value);
                AssertEqual("60.00", box.Attribute("ybr")?.Value);
                AssertEqual("0", box.Attribute("occluded")?.Value);
                AssertEqual("0", box.Attribute("z_order")?.Value);
                AssertEqual(1, trainImageElement.Elements("box").Count());
                AssertTrue(!validImageElement.Elements("box").Any(), "CVAT empty-label image should be exported without box entries");
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestCvatDetectionImportService()
    {
        string root = CreateTempRoot();
        try
        {
            string imageRoot = Path.Combine(root, "images");
            Directory.CreateDirectory(imageRoot);
            string trainSourcePath = Path.Combine(imageRoot, "train-sample.png");
            string emptySourcePath = Path.Combine(imageRoot, "empty-sample.png");
            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap emptyImage = CreateSolidBitmap(50, 40, Color.White))
            {
                trainImage.Save(trainSourcePath);
                emptyImage.Save(emptySourcePath);
            }

            var document = new XDocument(
                new XElement("annotations",
                    new XElement("version", "1.1"),
                    new XElement("image",
                        new XAttribute("id", 0),
                        new XAttribute("name", "train/train-sample.png"),
                        new XAttribute("width", 100),
                        new XAttribute("height", 200),
                        new XElement("box",
                            new XAttribute("label", "NG"),
                            new XAttribute("xtl", "10.00"),
                            new XAttribute("ytl", "20.00"),
                            new XAttribute("xbr", "40.00"),
                            new XAttribute("ybr", "60.00"),
                            new XAttribute("occluded", "0"),
                            new XAttribute("z_order", "0")),
                        new XElement("box",
                            new XAttribute("label", "NG"),
                            new XAttribute("xtl", "40.00"),
                            new XAttribute("ytl", "60.00"),
                            new XAttribute("xbr", "10.00"),
                            new XAttribute("ybr", "20.00"),
                            new XAttribute("occluded", "0"),
                            new XAttribute("z_order", "0")),
                        new XElement("box",
                            new XAttribute("label", "NG"),
                            new XAttribute("xtl", "10.00"),
                            new XAttribute("ytl", "20.00"),
                            new XAttribute("xbr", "40.00"),
                            new XAttribute("ybr", "60.00"),
                            new XAttribute("rotation", "22.5"),
                            new XAttribute("occluded", "0"),
                            new XAttribute("z_order", "0"))),
                    new XElement("image",
                        new XAttribute("id", 1),
                        new XAttribute("name", "valid/empty-sample.png"),
                        new XAttribute("width", 50),
                        new XAttribute("height", 40))));

            string archivePath = Path.Combine(root, "cvat-detection.zip");
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry annotationEntry = archive.CreateEntry("annotations.xml");
                using (Stream stream = annotationEntry.Open())
                {
                    document.Save(stream);
                }

                archive.CreateEntryFromFile(trainSourcePath, "images/train/train-sample.png");
                archive.CreateEntryFromFile(emptySourcePath, "images/valid/empty-sample.png");
            }

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            CvatDetectionImportResult result = CvatDetectionImportService.ImportArchive(
                data,
                archivePath,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(archivePath, result.ArchivePath);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedImageCount);
            AssertEqual(2, result.LabelFileCount);
            AssertEqual(1, result.ImportedBoxCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedImageCount);
            AssertEqual(2, result.SkippedBoxCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("NG", data.ClassNamedList[1].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "train-sample.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "empty-sample.png");
            string importedLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "train-sample.txt");
            string importedEmptyLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "empty-sample.txt");
            AssertTrue(File.Exists(importedImagePath), "CVAT import did not extract the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "CVAT import did not extract the empty image");
            AssertTrue(File.Exists(importedLabelPath), "CVAT import did not write the label file");
            AssertTrue(File.Exists(importedEmptyLabelPath), "CVAT import did not write the empty label file");
            AssertEqual("1 0.25 0.2 0.3 0.2", File.ReadAllText(importedLabelPath).Trim());
            AssertEqual(string.Empty, File.ReadAllText(importedEmptyLabelPath).Trim());
            AssertTrue(File.Exists(data.DataYamlFilePath), "CVAT import did not save data.yaml");

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectangles(
                importedLabelPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("NG"), "CVAT import label should load as NG");
            AssertEqual(new Rectangle(10, 20, 30, 40), loaded["NG"][0]);

            DatasetInterchangePreflightReport preflight = new DatasetInterchangePreflightService().DryRun(
                new DatasetInterchangeRequest
                {
                    Data = data,
                    FormatKey = "cvat-detection-import",
                    SourcePath = archivePath,
                    TargetSplit = YoloDatasetSplitService.TrainMode
                });
            AssertTrue(!preflight.CanApply, "CVAT rotated detection input should block Apply");
            AssertEqual(2, preflight.SkippedCount);
            AssertTrue(
                preflight.Issues.Any(issue => issue.Contains("2", StringComparison.Ordinal)),
                "CVAT preflight should expose the unsupported rotated/invalid box count");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestCvatSegmentationArchiveExportService()
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

            string outputPath = Path.Combine(root, "exports", "cvat-segmentation.zip");
            CvatSegmentationArchiveExportResult result = CvatSegmentationArchiveExportService.ExportDataset(
                data,
                outputPath,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            AssertTrue(File.Exists(outputPath), "CVAT segmentation archive was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(2, result.ImageCount);
            AssertEqual(1, result.PolygonCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(1, result.SkippedAnnotationCount);
            AssertTrue(result.ArchiveEntryNames.Contains("annotations.xml"), "CVAT segmentation archive result should list annotations.xml");
            AssertTrue(result.ArchiveEntryNames.Contains("images/train/seg-train.png"), "CVAT segmentation archive result should list train image entry");
            AssertTrue(result.ArchiveEntryNames.Contains("images/valid/seg-valid-empty.png"), "CVAT segmentation archive result should list valid image entry");

            using (ZipArchive archive = ZipFile.OpenRead(outputPath))
            {
                AssertTrue(archive.GetEntry("annotations.xml") != null, "CVAT segmentation archive should contain annotations.xml");
                AssertTrue(archive.GetEntry("images/train/seg-train.png") != null, "CVAT segmentation archive should contain train image");
                AssertTrue(archive.GetEntry("images/valid/seg-valid-empty.png") != null, "CVAT segmentation archive should contain valid image");

                ZipArchiveEntry annotationEntry = archive.GetEntry("annotations.xml");
                using Stream annotationStream = annotationEntry.Open();
                XDocument document = XDocument.Load(annotationStream);
                XElement rootElement = document.Root;
                AssertTrue(rootElement != null, "CVAT segmentation annotations XML should have a root");
                AssertEqual("annotations", rootElement.Name.LocalName);
                AssertEqual("1.1", rootElement.Element("version")?.Value);

                XElement task = rootElement.Element("meta")?.Element("task");
                AssertTrue(task != null, "CVAT segmentation annotations XML should include task metadata");
                AssertEqual("OpenVisionLab Labeling Studio segmentation export", task.Element("name")?.Value);
                AssertEqual("2", task.Element("size")?.Value);
                AssertEqual("annotation", task.Element("mode")?.Value);

                XElement labels = task.Element("labels");
                AssertTrue(labels != null, "CVAT segmentation task metadata should include labels");
                AssertTrue(labels.Elements("label").Any(label =>
                    label.Element("name")?.Value == "OK"
                    && label.Element("type")?.Value == "polygon"), "CVAT segmentation labels should include OK polygon label");
                AssertTrue(labels.Elements("label").Any(label =>
                    label.Element("name")?.Value == "Defect"
                    && label.Element("type")?.Value == "polygon"), "CVAT segmentation labels should include Defect polygon label");

                XElement trainImageElement = rootElement.Elements("image")
                    .FirstOrDefault(image => image.Attribute("name")?.Value == "train/seg-train.png");
                XElement validImageElement = rootElement.Elements("image")
                    .FirstOrDefault(image => image.Attribute("name")?.Value == "valid/seg-valid-empty.png");
                AssertTrue(trainImageElement != null, "CVAT segmentation annotations XML should include train image");
                AssertTrue(validImageElement != null, "CVAT segmentation annotations XML should include valid empty-label image");
                AssertEqual("0", trainImageElement.Attribute("id")?.Value);
                AssertEqual("100", trainImageElement.Attribute("width")?.Value);
                AssertEqual("200", trainImageElement.Attribute("height")?.Value);

                XElement polygon = trainImageElement.Element("polygon");
                AssertTrue(polygon != null, "CVAT segmentation train image should include one polygon");
                AssertEqual("Defect", polygon.Attribute("label")?.Value);
                AssertEqual("10.00,20.00;40.00,20.00;40.00,60.00;10.00,60.00", polygon.Attribute("points")?.Value);
                AssertEqual("0", polygon.Attribute("occluded")?.Value);
                AssertEqual("0", polygon.Attribute("z_order")?.Value);
                AssertEqual(1, trainImageElement.Elements("polygon").Count());
                AssertTrue(!validImageElement.Elements("polygon").Any(), "CVAT segmentation empty image should be exported without polygon entries");
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestCvatSegmentationImportService()
    {
        string root = CreateTempRoot();
        try
        {
            string imageRoot = Path.Combine(root, "images");
            Directory.CreateDirectory(imageRoot);
            string trainSourcePath = Path.Combine(imageRoot, "seg-train.png");
            string emptySourcePath = Path.Combine(imageRoot, "seg-empty.png");
            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap emptyImage = CreateSolidBitmap(50, 40, Color.White))
            {
                trainImage.Save(trainSourcePath);
                emptyImage.Save(emptySourcePath);
            }

            var document = new XDocument(
                new XElement("annotations",
                    new XElement("version", "1.1"),
                    new XElement("image",
                        new XAttribute("id", 0),
                        new XAttribute("name", "train/seg-train.png"),
                        new XAttribute("width", 100),
                        new XAttribute("height", 200),
                        new XElement("polygon",
                            new XAttribute("label", "Defect"),
                            new XAttribute("points", "10.00,20.00;40.00,20.00;40.00,60.00;10.00,60.00"),
                            new XAttribute("occluded", "0"),
                            new XAttribute("z_order", "0")),
                        new XElement("polygon",
                            new XAttribute("label", "Invalid"),
                            new XAttribute("points", "1.00,1.00;2.00,2.00"),
                            new XAttribute("occluded", "0"),
                            new XAttribute("z_order", "0"))),
                    new XElement("image",
                        new XAttribute("id", 1),
                        new XAttribute("name", "valid/seg-empty.png"),
                        new XAttribute("width", 50),
                        new XAttribute("height", 40))));

            string archivePath = Path.Combine(root, "cvat-segmentation.zip");
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry annotationEntry = archive.CreateEntry("annotations.xml");
                using (Stream stream = annotationEntry.Open())
                {
                    document.Save(stream);
                }

                archive.CreateEntryFromFile(trainSourcePath, "images/train/seg-train.png");
                archive.CreateEntryFromFile(emptySourcePath, "images/valid/seg-empty.png");
            }

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            CvatSegmentationImportResult result = CvatSegmentationImportService.ImportArchive(
                data,
                archivePath,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(archivePath, result.ArchivePath);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedImageCount);
            AssertEqual(1, result.ImportedPolygonCount);
            AssertEqual(1, result.ImportedSegmentFileCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedImageCount);
            AssertEqual(1, result.SkippedPolygonCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("Defect", data.ClassNamedList[1].Text);
            AssertTrue(!data.ClassNamedList.Any(item => string.Equals(item.Text, "Invalid", StringComparison.OrdinalIgnoreCase)), "CVAT segmentation import should not add skipped invalid polygon classes");

            string importedImagePath = Path.Combine(data.TrainImagesPath, "seg-train.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "seg-empty.png");
            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-train.json");
            string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "seg-train.png");
            string emptySegmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "seg-empty.json");
            AssertTrue(File.Exists(importedImagePath), "CVAT segmentation import did not extract the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "CVAT segmentation import did not extract the empty image");
            AssertTrue(File.Exists(segmentPath), "CVAT segmentation import did not write the segment JSON");
            AssertTrue(File.Exists(maskPath), "CVAT segmentation import did not write the segmentation mask");
            AssertTrue(!File.Exists(emptySegmentPath), "CVAT segmentation import should not create a segment JSON for empty images");
            AssertTrue(File.Exists(data.DataYamlFilePath), "CVAT segmentation import did not save data.yaml");

            string dataYaml = File.ReadAllText(data.DataYamlFilePath);
            AssertTrue(dataYaml.Contains("nc: 2", StringComparison.Ordinal), "CVAT segmentation import data.yaml should include the imported class count");
            AssertTrue(dataYaml.Contains("OK", StringComparison.Ordinal), "CVAT segmentation import data.yaml should include the existing class");
            AssertTrue(dataYaml.Contains("Defect", StringComparison.Ordinal), "CVAT segmentation import data.yaml should include the imported class");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded = YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("Defect"), "CVAT segmentation import segment should load as Defect");
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
