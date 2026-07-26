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

internal static class YoloAnnotationsTests
{
    internal static void TestYoloAnnotationLines()
    {
        var classes = new List<CClassItem>
        {
            new CClassItem { Text = "OK", DrawColor = Color.Green },
            new CClassItem { Text = "NG", DrawColor = Color.Red }
        };
        var rois = new Dictionary<string, List<CRectangleObject>>
        {
            ["NG"] = new List<CRectangleObject>
            {
                new CRectangleObject { Roi = new Rectangle(10, 20, 30, 40), cClassItem = classes[1] }
            }
        };

        List<string> lines = YoloAnnotationService.BuildAnnotationLines(rois, classes, new Size(100, 200));

        AssertEqual(1, lines.Count);
        AssertEqual("1 0.25 0.2 0.3 0.2", lines[0]);

        var data = new CData();
        string root = CreateTempRoot();
        try
        {
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            IReadOnlyList<string> targetLabelPaths = YoloAnnotationService.GetTargetLabelPaths("part-001.png", data);

            AssertEqual(1, targetLabelPaths.Count);
            AssertEqual(Path.Combine(root, "data", "train", "labels", "part-001.txt"), targetLabelPaths[0]);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloAnnotationLoad()
    {
        string root = CreateTempRoot();
        try
        {
            string labelPath = Path.Combine(root, "sample.txt");
            File.WriteAllLines(labelPath, new[]
            {
                "1 0.25 0.2 0.3 0.2",
                "bad line",
                "2 0.5 0.5 0.1 0.1"
            });

            var classes = new List<CClassItem>
            {
                new CClassItem { Text = "OK", DrawColor = Color.Green },
                new CClassItem { Text = "NG", DrawColor = Color.Red }
            };

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectangles(
                labelPath,
                classes,
                new Size(100, 200));

            AssertEqual(1, loaded.Count);
            AssertTrue(loaded.ContainsKey("NG"), "NG label was not loaded");
            AssertEqual(new Rectangle(10, 20, 30, 40), loaded["NG"][0]);

            AssertTrue(YoloAnnotationService.TryParseYoloLine("0 0.5 0.5 1 1", new Size(20, 10), out int classIndex, out Rectangle roi), "valid YOLO line did not parse");
            AssertEqual(0, classIndex);
            AssertEqual(new Rectangle(0, 0, 20, 10), roi);
            AssertTrue(!YoloAnnotationService.TryParseYoloLine("0 1.2 0.5 0.1 0.1", new Size(20, 10), out _, out _), "out-of-range YOLO line was accepted");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
