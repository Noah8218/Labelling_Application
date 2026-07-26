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

internal static class WpfTrainingGuideHistoryTests
{
    internal static void TestWpfTrainingGuideHistoryService()
    {
        var history = new YoloTrainingGuideHistory();
        var service = new WpfTrainingGuideHistoryService();
        string signature = string.Empty;
        Func<string, string> formatState = state => string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase) ? "completed" : state ?? string.Empty;
        Func<string, bool> isTerminal = state => string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase);

        service.UpdateDatasetHistory(history, isReady: false, issueKind: "Labels", summary: "labels missing", recordHistory: true);
        AssertTrue(!history.LastDatasetReady, "dataset history should store readiness state");
        AssertEqual("Labels", history.LastDatasetIssueKind);
        AssertEqual(1, history.RunHistory.Count);
        AssertEqual("DatasetCheck", history.RunHistory[0].EventKind);

        var completedStatus = new PythonCommunicationStatus
        {
            LastTrainingState = "completed",
            LastTrainingProgressPercent = 100,
            LastTrainingMessage = "done"
        };
        service.UpdateTrainingHistory(history, completedStatus, isTerminal, ref signature);
        AssertEqual("completed", history.LastTrainingState);
        AssertEqual(100, history.LastTrainingProgressPercent);
        AssertEqual(2, history.RunHistory.Count);
        AssertEqual("TrainingState", history.RunHistory[1].EventKind);

        service.UpdateTrainingHistory(history, completedStatus, isTerminal, ref signature);
        AssertEqual(2, history.RunHistory.Count);

        string weightsPath = Path.Combine("C:\\models", "best.pt");
        service.UpdateAppliedWeightsHistory(history, weightsPath, savedToRecipe: false);
        AssertEqual(weightsPath, history.AppliedWeightsPath);
        AssertTrue(!history.AppliedWeightsSavedToRecipe, "newly applied weights should start unsaved");
        AssertEqual(3, history.RunHistory.Count);
        service.UpdateAppliedWeightsHistory(history, weightsPath, savedToRecipe: true);
        AssertEqual(3, history.RunHistory.Count);
        AssertTrue(history.RunHistory.Last().AppliedWeightsSavedToRecipe, "existing weight history row should update saved state instead of duplicating");

        for (int i = 0; i < WpfTrainingGuideHistoryService.RunHistoryLimit + 3; i++)
        {
            service.UpdateDatasetHistory(history, isReady: true, issueKind: string.Empty, summary: "ok", recordHistory: true);
        }

        AssertEqual(WpfTrainingGuideHistoryService.RunHistoryLimit, history.RunHistory.Count);
        IReadOnlyList<string> runItems = service.BuildRunHistoryItems(history, formatState);
        AssertTrue(runItems.Count <= 5, "run history presentation should stay compact");
        string summaryText = service.BuildHistoryText(history, formatState);
        AssertTrue(!string.IsNullOrWhiteSpace(summaryText), "history summary should be operator-readable");
        AssertTrue(summaryText.Contains("weight", StringComparison.Ordinal), "history summary should include applied weight status");

        var downloadGuardHistory = new YoloTrainingGuideHistory();
        string downloadGuardSignature = string.Empty;
        var downloadGuardStatus = new PythonCommunicationStatus
        {
            LastTrainingState = "failed",
            LastTrainingMessage = "YOLO training could not start.",
            LastTrainingWeightsPath = "yolov8n-seg.pt",
            LastError = "TrainingWeightDownloadRequired: cache the file first"
        };
        service.UpdateTrainingHistory(downloadGuardHistory, downloadGuardStatus, isTerminal, ref downloadGuardSignature);
        AssertTrue(downloadGuardHistory.LastTrainingMessage.Contains("\uD559\uC2B5 weight \uC900\uBE44 \uD544\uC694", StringComparison.Ordinal), "YOLOv8 segmentation download guard should be translated in training history");
        AssertTrue(downloadGuardHistory.LastTrainingMessage.Contains("yolov8n-seg.pt", StringComparison.Ordinal), "YOLOv8 segmentation download guard history should keep the blocked segmentation weight filename");
        AssertTrue(!downloadGuardHistory.LastTrainingMessage.Contains("TrainingWeightDownloadRequired", StringComparison.Ordinal), "training history should not expose the raw download guard code");
        AssertEqual(1, downloadGuardHistory.RunHistory.Count);
        AssertTrue(downloadGuardHistory.RunHistory[0].TrainingMessage.Contains("yolov8n-seg.pt", StringComparison.Ordinal), "download guard run-history record should keep the blocked segmentation weight filename");
        string downloadGuardHistoryText = service.BuildHistoryText(downloadGuardHistory, formatState);
        AssertTrue(downloadGuardHistoryText.Contains("yolov8n-seg.pt", StringComparison.Ordinal), "download guard history summary should keep the blocked segmentation weight filename");
    }
}
