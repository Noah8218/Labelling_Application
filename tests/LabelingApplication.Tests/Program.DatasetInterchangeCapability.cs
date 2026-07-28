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

internal static class DatasetInterchangeCapabilityTests
{
    internal static void TestDatasetExportCapabilityInventory()
    {
        IReadOnlyList<DatasetExportCapability> capabilities = DatasetExportCapabilityService.BuildCapabilities();
        AssertTrue(capabilities.Count >= 5, "export capability inventory should cover implemented and planned external targets");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "yolo-detection-directory"
            && item.IsImplemented
            && item.DatasetPurpose == LabelingDatasetPurpose.ObjectDetection.ToString()), "YOLO detection directory should remain declared as implemented");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "coco-detection-json"
            && item.IsImplemented
            && item.VerificationSwitch == "--coco-detection-export"), "COCO detection export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "pascal-voc-detection"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--pascal-voc-detection-export"), "Pascal VOC export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "label-studio-detection-json"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--label-studio-detection-export"), "Label Studio detection JSON export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "cvat-images-archive"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--cvat-image-export"), "CVAT image task archive export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "coco-segmentation-json"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--coco-segmentation-export"), "COCO segmentation export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "label-studio-segmentation-json"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--label-studio-segmentation-export"), "Label Studio segmentation JSON export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "cvat-segmentation-archive"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.VerificationSwitch == "--cvat-segmentation-export"), "CVAT segmentation archive export should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "coco-detection-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--coco-detection-import"), "COCO detection import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "pascal-voc-detection-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--pascal-voc-detection-import"), "Pascal VOC detection import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "label-studio-detection-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--label-studio-detection-import"), "Label Studio detection import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "cvat-detection-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--cvat-detection-import"), "CVAT detection import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "coco-segmentation-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--coco-segmentation-import"), "COCO segmentation import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "label-studio-segmentation-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--label-studio-segmentation-import"), "Label Studio segmentation import should be declared as implemented and verified");
        AssertTrue(capabilities.Any(item =>
            item.FormatKey == "cvat-segmentation-import"
            && item.IsImplemented
            && !item.IsRecommendedNext
            && item.Direction == "import"
            && item.VerificationSwitch == "--cvat-segmentation-import"), "CVAT segmentation import should be declared as implemented and verified");

        DatasetExportCapability recommended = DatasetExportCapabilityService.GetRecommendedNext();
        AssertTrue(recommended != null, "export capability inventory should identify the next target");
        AssertEqual("labelbox-ndjson-detection-import", recommended.FormatKey);
        AssertTrue(!recommended.IsImplemented, "recommended next interoperability target should not be marked implemented before its importer exists");
        AssertEqual("import", recommended.Direction);
        AssertTrue(recommended.DisplayName.Contains("NDJSON", StringComparison.Ordinal), "recommended next interoperability target should be NDJSON import");
        AssertEqual(LabelingDatasetPurpose.ObjectDetection.ToString(), recommended.DatasetPurpose);
        AssertTrue(recommended.RequirementSummary.Contains("NDJSON", StringComparison.OrdinalIgnoreCase), "NDJSON import requirement should mention NDJSON");
        AssertTrue(capabilities.Count(item => item.IsRecommendedNext) == 1, "export capability inventory should have exactly one recommended next target");
        AssertTrue(DatasetExportCapabilityService.BuildImplementedCapabilities().All(item => item.IsImplemented), "implemented capability helper should return only implemented targets");
    }

    internal static void TestDatasetInterchangeDryRunAndApply()
    {
        string root = CreateTempRoot();
        try
        {
            string sourceRoot = Path.Combine(root, "source-dataset");
            CData sourceData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                sourceRoot,
                LabelingDatasetPurpose.ObjectDetection,
                includeBoxes: true,
                includeSegments: false);
            string sourceBefore = CaptureExternalSourceSnapshot(sourceRoot);
            string exportPath = Path.Combine(root, "external-export", "annotations.json");
            var service = new DatasetInterchangePreflightService();
            AssertEqual(14, service.BuildSupportedCapabilities().Count);
            var exportRequest = new DatasetInterchangeRequest
            {
                Data = sourceData,
                FormatKey = "coco-detection-json",
                TargetPath = exportPath
            };

            DatasetInterchangePreflightReport exportDryRun = service.DryRun(exportRequest);
            AssertTrue(exportDryRun.CanApply, string.Join(Environment.NewLine, exportDryRun.Issues));
            AssertTrue(exportDryRun.IsDryRun, "export preflight should be marked as dry-run");
            AssertTrue(exportDryRun.SourceUnchanged, "export dry-run should preserve the source tree");
            AssertTrue(exportDryRun.RequestedTargetUnchanged, "export dry-run should preserve the requested target");
            AssertEqual(2, exportDryRun.ImageCount);
            AssertEqual(2, exportDryRun.AnnotationCount);
            AssertEqual(1, exportDryRun.CategoryCount);
            AssertTrue(!File.Exists(exportPath), "export dry-run must not create the requested output");
            AssertEqual(sourceBefore, CaptureExternalSourceSnapshot(sourceRoot));

            DatasetInterchangePreflightReport exportApply = service.Apply(exportRequest);
            AssertTrue(exportApply.WasApplied, "explicit export apply should report applied state");
            AssertTrue(exportApply.Issues.Count == 0, string.Join(Environment.NewLine, exportApply.Issues));
            AssertTrue(File.Exists(exportPath), "explicit export apply should create the requested output");
            AssertEqual(sourceBefore, CaptureExternalSourceSnapshot(sourceRoot));

            string importRoot = Path.Combine(root, "imported-dataset");
            var importData = new CData();
            importData.ConfigureOutputRoot(importRoot);
            importData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            var importRequest = new DatasetInterchangeRequest
            {
                Data = importData,
                FormatKey = "coco-detection-import",
                SourcePath = exportPath,
                ImageRoot = sourceRoot,
                TargetPath = importRoot,
                TargetSplit = YoloDatasetSplitService.TrainMode
            };
            string exportHashBeforeImport = ComputeFileSha256(exportPath);

            DatasetInterchangePreflightReport importDryRun = service.DryRun(importRequest);
            AssertTrue(importDryRun.CanApply, string.Join(Environment.NewLine, importDryRun.Issues));
            AssertTrue(importDryRun.SourceUnchanged, "import dry-run should preserve the external annotation source");
            AssertTrue(importDryRun.RequestedTargetUnchanged, "import dry-run should preserve the current dataset target");
            AssertTrue(!Directory.Exists(importRoot), "import dry-run must not create the current dataset target");
            AssertEqual(2, importDryRun.ImageCount);
            AssertEqual(2, importDryRun.AnnotationCount);
            AssertEqual(1, importDryRun.CategoryCount);
            AssertEqual(exportHashBeforeImport, ComputeFileSha256(exportPath));

            DatasetInterchangePreflightReport importApply = service.Apply(importRequest);
            AssertTrue(importApply.WasApplied, "explicit import apply should report applied state");
            AssertTrue(importApply.Issues.Count == 0, string.Join(Environment.NewLine, importApply.Issues));
            AssertTrue(File.Exists(Path.Combine(importRoot, "data.yaml")), "explicit import apply should write the target dataset");
            AssertEqual(exportHashBeforeImport, ComputeFileSha256(exportPath));

            string viewModelExportPath = Path.Combine(root, "view-model-export", "annotations.json");
            var viewModel = new WpfDatasetInterchangeViewModel(sourceData, service);
            WpfDatasetInterchangeOption cocoExport = viewModel.Operations.Single(item =>
                item.Capability.FormatKey == "coco-detection-json");
            viewModel.SelectedOperation = cocoExport;
            viewModel.TargetPath = viewModelExportPath;
            viewModel.DryRunCommand.Execute(null);
            AssertTrue(viewModel.CanApply, "view model should enable Apply only after a passing dry-run");
            viewModel.TargetPath = Path.Combine(root, "view-model-export", "changed.json");
            AssertTrue(!viewModel.CanApply, "changing an input should invalidate the previous dry-run");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
