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

internal static class PascalVocDetectionTests
{
    internal static void TestPascalVocDetectionExportService()
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

            string outputDirectory = Path.Combine(root, "exports", "pascal-voc");
            PascalVocDetectionExportResult result = PascalVocDetectionExportService.ExportDataset(
                data,
                outputDirectory,
                new[] { YoloDatasetSplitService.TrainMode, YoloDatasetSplitService.ValidMode });

            string trainXmlPath = Path.Combine(outputDirectory, "train", "train-sample.xml");
            string validXmlPath = Path.Combine(outputDirectory, "valid", "valid-empty.xml");
            AssertTrue(File.Exists(trainXmlPath), "Pascal VOC train XML was not written");
            AssertTrue(File.Exists(validXmlPath), "Pascal VOC valid empty-label XML was not written");
            AssertEqual(outputDirectory, result.OutputDirectory);
            AssertEqual(2, result.ImageCount);
            AssertEqual(2, result.XmlFileCount);
            AssertEqual(1, result.ObjectCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(2, result.SkippedAnnotationCount);
            AssertTrue(result.OutputPaths.Contains(trainXmlPath), "Pascal VOC result should list the train XML path");
            AssertTrue(result.OutputPaths.Contains(validXmlPath), "Pascal VOC result should list the valid XML path");

            XDocument trainDocument = XDocument.Load(trainXmlPath);
            XElement trainRoot = trainDocument.Root;
            AssertTrue(trainRoot != null, "Pascal VOC train XML should have an annotation root");
            AssertEqual("annotation", trainRoot.Name.LocalName);
            AssertEqual("train", trainRoot.Element("folder")?.Value);
            AssertEqual("train-sample.png", trainRoot.Element("filename")?.Value);
            AssertEqual("data/train/images/train-sample.png", trainRoot.Element("path")?.Value);
            XElement trainSize = trainRoot.Element("size");
            AssertTrue(trainSize != null, "Pascal VOC train XML should include image size");
            AssertEqual("100", trainSize.Element("width")?.Value);
            AssertEqual("200", trainSize.Element("height")?.Value);
            AssertEqual("3", trainSize.Element("depth")?.Value);

            XElement trainObject = trainRoot.Element("object");
            AssertTrue(trainObject != null, "Pascal VOC train XML should include one object");
            AssertEqual("NG", trainObject.Element("name")?.Value);
            XElement bounds = trainObject.Element("bndbox");
            AssertTrue(bounds != null, "Pascal VOC object should include bndbox");
            AssertEqual("11", bounds.Element("xmin")?.Value);
            AssertEqual("21", bounds.Element("ymin")?.Value);
            AssertEqual("40", bounds.Element("xmax")?.Value);
            AssertEqual("60", bounds.Element("ymax")?.Value);
            AssertEqual(1, trainRoot.Elements("object").Count());

            XDocument validDocument = XDocument.Load(validXmlPath);
            XElement validRoot = validDocument.Root;
            AssertTrue(validRoot != null, "Pascal VOC valid XML should have an annotation root");
            AssertEqual("valid-empty.png", validRoot.Element("filename")?.Value);
            AssertEqual("50", validRoot.Element("size")?.Element("width")?.Value);
            AssertEqual("40", validRoot.Element("size")?.Element("height")?.Value);
            AssertTrue(!validRoot.Elements("object").Any(), "Pascal VOC empty-label image should be exported without object entries");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestPascalVocDetectionImportService()
    {
        string root = CreateTempRoot();
        try
        {
            string sourceRoot = Path.Combine(root, "source-images");
            string annotationRoot = Path.Combine(root, "voc");
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(annotationRoot);

            using (Bitmap trainImage = CreateSolidBitmap(100, 200, Color.Black))
            using (Bitmap emptyImage = CreateSolidBitmap(50, 40, Color.White))
            {
                trainImage.Save(Path.Combine(sourceRoot, "train-sample.png"));
                emptyImage.Save(Path.Combine(sourceRoot, "empty-sample.png"));
            }

            var trainDocument = new XDocument(
                new XElement("annotation",
                    new XElement("folder", "source-images"),
                    new XElement("filename", "train-sample.png"),
                    new XElement("size",
                        new XElement("width", 100),
                        new XElement("height", 200),
                        new XElement("depth", 3)),
                    new XElement("object",
                        new XElement("name", "NG"),
                        new XElement("bndbox",
                            new XElement("xmin", 11),
                            new XElement("ymin", 21),
                            new XElement("xmax", 40),
                            new XElement("ymax", 60))),
                    new XElement("object",
                        new XElement("name", "NG"),
                        new XElement("bndbox",
                            new XElement("xmin", 40),
                            new XElement("ymin", 60),
                            new XElement("xmax", 10),
                            new XElement("ymax", 20)))));
            trainDocument.Save(Path.Combine(annotationRoot, "train-sample.xml"));

            var emptyDocument = new XDocument(
                new XElement("annotation",
                    new XElement("folder", "source-images"),
                    new XElement("filename", "empty-sample.png"),
                    new XElement("size",
                        new XElement("width", 50),
                        new XElement("height", 40),
                        new XElement("depth", 3))));
            emptyDocument.Save(Path.Combine(annotationRoot, "empty-sample.xml"));

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "imported"));
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            PascalVocDetectionImportResult result = PascalVocDetectionImportService.ImportDirectory(
                data,
                annotationRoot,
                sourceRoot,
                YoloDatasetSplitService.TrainMode);

            AssertEqual(Path.GetFullPath(annotationRoot), result.AnnotationDirectory);
            AssertEqual(Path.GetFullPath(sourceRoot), result.ImageRoot);
            AssertEqual(YoloDatasetSplitService.TrainMode, result.TargetSplit);
            AssertEqual(2, result.ImportedImageCount);
            AssertEqual(2, result.LabelFileCount);
            AssertEqual(1, result.ImportedObjectCount);
            AssertEqual(2, result.CategoryCount);
            AssertEqual(0, result.SkippedXmlCount);
            AssertEqual(1, result.SkippedObjectCount);
            AssertEqual(2, data.ClassNamedList.Count);
            AssertEqual("OK", data.ClassNamedList[0].Text);
            AssertEqual("NG", data.ClassNamedList[1].Text);

            string importedImagePath = Path.Combine(data.TrainImagesPath, "train-sample.png");
            string importedEmptyImagePath = Path.Combine(data.TrainImagesPath, "empty-sample.png");
            string importedLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "train-sample.txt");
            string importedEmptyLabelPath = Path.Combine(data.OutputRootPath, "data", "train", "labels", "empty-sample.txt");
            AssertTrue(File.Exists(importedImagePath), "Pascal VOC import did not copy the annotated image");
            AssertTrue(File.Exists(importedEmptyImagePath), "Pascal VOC import did not copy the empty image");
            AssertTrue(File.Exists(importedLabelPath), "Pascal VOC import did not write the label file");
            AssertTrue(File.Exists(importedEmptyLabelPath), "Pascal VOC import did not write the empty label file");
            AssertEqual("1 0.25 0.2 0.3 0.2", File.ReadAllText(importedLabelPath).Trim());
            AssertEqual(string.Empty, File.ReadAllText(importedEmptyLabelPath).Trim());
            AssertTrue(File.Exists(data.DataYamlFilePath), "Pascal VOC import did not save data.yaml");

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectangles(
                importedLabelPath,
                data.ClassNamedList,
                new Size(100, 200));
            AssertTrue(loaded.ContainsKey("NG"), "Pascal VOC import label should load as NG");
            AssertEqual(new Rectangle(10, 20, 30, 40), loaded["NG"][0]);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
