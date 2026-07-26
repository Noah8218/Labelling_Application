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

internal static class YoloDatasetQualityAuditTests
{
    internal static void TestYoloDatasetQualityAuditReport()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });
            data.ClassNamedList.Add(new CClassItem { Text = "NG", DrawColor = Color.Red });

            var okRois = new Dictionary<string, List<CRectangleObject>>
            {
                ["OK"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
                }
            };
            var ngRois = new Dictionary<string, List<CRectangleObject>>
            {
                ["NG"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(10, 10, 20, 20), cClassItem = data.ClassNamedList[1] }
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(40, 40, Color.Black))
            using (Bitmap emptyTrainImage = CreateSolidBitmap(40, 40, Color.White))
            using (Bitmap validImage = CreateSolidBitmap(50, 50, Color.Gray))
            using (Bitmap missingLabelImage = CreateSolidBitmap(30, 30, Color.Blue))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations("train-ok.png", trainImage, okRois, data.ClassNamedList, data);
                YoloAnnotationService.SaveAnnotations(
                    "train-empty.png",
                    emptyTrainImage,
                    new Dictionary<string, List<CRectangleObject>>(),
                    data.ClassNamedList,
                    data);

                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations("valid-ng.png", validImage, ngRois, data.ClassNamedList, data);

                Directory.CreateDirectory(data.ValidImagesPath);
                missingLabelImage.Save(Path.Combine(data.ValidImagesPath, "valid-missing.png"), System.Drawing.Imaging.ImageFormat.Png);
            }

            string trainLabelPath = Path.Combine(root, "data", "train", "labels", "train-ok.txt");
            File.AppendAllLines(trainLabelPath, new[] { "9 0.5 0.5 0.1 0.1" });

            YoloDatasetQualityAuditReport report = YoloDatasetQualityAuditService.Build(data);
            YoloDatasetQualityAuditSplitSummary train = report.Splits.First(split => split.Split == YoloDatasetSplitService.TrainMode);
            YoloDatasetQualityAuditSplitSummary valid = report.Splits.First(split => split.Split == YoloDatasetSplitService.ValidMode);
            YoloDatasetQualityAuditSplitSummary test = report.Splits.First(split => split.Split == YoloDatasetSplitService.TestMode);

            AssertEqual(2, train.ImageCount);
            AssertEqual(2, train.LabelFileCount);
            AssertEqual(0, train.MissingLabelCount);
            AssertEqual(1, train.EmptyLabelCount);
            AssertEqual(1, train.InvalidLabelLineCount);
            AssertEqual(1, train.ObjectCount);
            AssertEqual(1, train.ObjectCountByClass["OK"]);

            AssertEqual(2, valid.ImageCount);
            AssertEqual(1, valid.LabelFileCount);
            AssertEqual(1, valid.MissingLabelCount);
            AssertEqual(0, valid.EmptyLabelCount);
            AssertEqual(0, valid.InvalidLabelLineCount);
            AssertEqual(1, valid.ObjectCount);
            AssertEqual(1, valid.ObjectCountByClass["NG"]);

            AssertEqual(0, test.ImageCount);
            AssertEqual(4, report.TotalImageCount);
            AssertEqual(3, report.TotalLabelFileCount);
            AssertEqual(1, report.TotalMissingLabelCount);
            AssertEqual(1, report.TotalEmptyLabelCount);
            AssertEqual(1, report.TotalInvalidLabelLineCount);
            AssertEqual(2, report.TotalObjectCount);
            AssertEqual(1, report.ObjectCountByClass["OK"]);
            AssertEqual(1, report.ObjectCountByClass["NG"]);
            AssertTrue(report.SummaryLines.Any(line => line.Contains("Split:train", StringComparison.Ordinal) && line.Contains("EmptyLabels:1", StringComparison.Ordinal)), "quality audit summary should include train empty-label count");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("Split:valid", StringComparison.Ordinal) && line.Contains("MissingLabels:1", StringComparison.Ordinal)), "quality audit summary should include valid missing-label count");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("OK:1", StringComparison.Ordinal)), "quality audit summary should include OK class distribution");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("NG:1", StringComparison.Ordinal)), "quality audit summary should include NG class distribution");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloDatasetQualityAuditMarkdownExport()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            var rois = new Dictionary<string, List<CRectangleObject>>
            {
                ["OK"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(40, 40, Color.Black))
            using (Bitmap missingLabelImage = CreateSolidBitmap(30, 30, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations("train-ok.png", trainImage, rois, data.ClassNamedList, data);
                File.AppendAllLines(Path.Combine(root, "data", "train", "labels", "train-ok.txt"), new[] { "9 0.5 0.5 0.1 0.1" });

                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                Directory.CreateDirectory(data.ValidImagesPath);
                missingLabelImage.Save(Path.Combine(data.ValidImagesPath, "valid-missing.png"), System.Drawing.Imaging.ImageFormat.Png);
            }

            YoloDatasetQualityAuditReport report = YoloDatasetQualityAuditService.Build(data);
            string outputPath = Path.Combine(root, "exports", "dataset-quality-audit.md");
            AssertEqual(Path.Combine(root, YoloDatasetQualityAuditExportService.DefaultFileName), YoloDatasetQualityAuditExportService.ResolveDefaultOutputPath(data));
            YoloDatasetQualityAuditExportResult result = YoloDatasetQualityAuditExportService.ExportMarkdown(report, outputPath);

            AssertTrue(File.Exists(outputPath), "dataset quality audit markdown was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(1, result.MissingLabelCount);
            AssertEqual(1, result.InvalidLabelLineCount);
            AssertTrue(result.LineCount >= 12, "dataset quality audit markdown should include summary and tables");

            string markdown = File.ReadAllText(outputPath);
            AssertTrue(markdown.Contains("# Dataset Quality Audit", StringComparison.Ordinal), "markdown export should include a title");
            AssertTrue(markdown.Contains("- Missing labels: 1", StringComparison.Ordinal), "markdown export should include missing label total");
            AssertTrue(markdown.Contains("- Invalid label lines: 1", StringComparison.Ordinal), "markdown export should include invalid label total");
            AssertTrue(markdown.Contains("| train | 1 | 1 | 0 | 0 | 1 | 1 |", StringComparison.Ordinal), "markdown export should include train split row");
            AssertTrue(markdown.Contains("| valid | 1 | 0 | 1 | 0 | 0 | 0 |", StringComparison.Ordinal), "markdown export should include valid split row");
            AssertTrue(markdown.Contains("| OK | 1 |", StringComparison.Ordinal), "markdown export should include class distribution");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
