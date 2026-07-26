using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class ModelAdapterCatalogTests
{
    internal static void TestModelAdapterCatalog()
    {
        var catalog = ModelAdapterCatalogService.BuildCatalog();

        AssertEqual(6, catalog.Count);
        AssertEqual(
            "recipe-interchange,yolov5-detect,yolov8-local,unet-segmentation,onnx-inference,yolo11-local",
            string.Join(",", catalog.Select(item => item.AdapterKey)));
        AssertTrue(
            catalog.All(item => !string.IsNullOrWhiteSpace(item.DisplayName)
                                && !string.IsNullOrWhiteSpace(item.AvailabilityText)
                                && !string.IsNullOrWhiteSpace(item.TaskContractText)
                                && !string.IsNullOrWhiteSpace(item.DataContractText)
                                && !string.IsNullOrWhiteSpace(item.RuntimeContractText)
                                && !string.IsNullOrWhiteSpace(item.EvidenceContractText)
                                && !string.IsNullOrWhiteSpace(item.NextActionText)),
            "every adapter must declare task, data, runtime, evidence, and next-action contracts");

        ModelAdapterCatalogItem interchange = catalog.Single(item => item.AdapterKey == "recipe-interchange");
        AssertTrue(interchange.DataContractText.Contains("COCO", StringComparison.Ordinal), "implemented interchange should name COCO export");
        AssertTrue(interchange.DataContractText.Contains("Pascal VOC", StringComparison.Ordinal), "implemented interchange should name Pascal VOC export");
        AssertTrue(interchange.DataContractText.Contains("Label Studio", StringComparison.Ordinal), "implemented interchange should name Label Studio export");
        AssertTrue(interchange.DataContractText.Contains("CVAT", StringComparison.Ordinal), "implemented interchange should name CVAT export");
        AssertTrue(interchange.RuntimeContractText.Contains("모델 런타임이 아닙니다", StringComparison.Ordinal), "data export must not be represented as a runnable model runtime");

        ModelAdapterCatalogItem yolov5 = catalog.Single(item => item.AdapterKey == "yolov5-detect");
        AssertTrue(yolov5.TaskContractText.Contains("객체 탐지만", StringComparison.Ordinal), "YOLOv5 contract should stay detection-only");
        AssertTrue(yolov5.DataContractText.Contains("정규화 xywh", StringComparison.Ordinal), "YOLOv5 contract should retain label geometry requirements");

        ModelAdapterCatalogItem yolov8 = catalog.Single(item => item.AdapterKey == "yolov8-local");
        AssertTrue(yolov8.TaskContractText.Contains("세그멘테이션", StringComparison.Ordinal), "YOLOv8 contract should expose segmentation separately from detection");
        AssertTrue(yolov8.DataContractText.Contains("native data.yaml", StringComparison.OrdinalIgnoreCase), "YOLOv8 contract should describe native YAML intake provenance");

        ModelAdapterCatalogItem unet = catalog.Single(item => item.AdapterKey == "unet-segmentation");
        AssertTrue(unet.TaskContractText.Contains("세그멘테이션만", StringComparison.Ordinal), "U-Net contract should stay segmentation-only");
        AssertTrue(unet.DataContractText.Contains("앱 소유", StringComparison.Ordinal), "U-Net contract should name the app-owned canonical export");
        AssertTrue(unet.EvidenceContractText.Contains("Dice/IoU", StringComparison.Ordinal), "U-Net comparison should declare its common mask metric");
        AssertTrue(unet.EvidenceContractText.Contains("자동 채택하지 않습니다", StringComparison.Ordinal), "U-Net comparison must not imply automatic adoption");

        ModelAdapterCatalogItem onnx = catalog.Single(item => item.AdapterKey == "onnx-inference");
        AssertTrue(onnx.AvailabilityText.Contains("Inference-only", StringComparison.OrdinalIgnoreCase), "ONNX contract should not imply application-owned training");
        AssertTrue(onnx.NextActionText.Contains("보지 마세요", StringComparison.Ordinal), "ONNX contract should prevent conversion from being mistaken for training evidence");

        ModelAdapterCatalogItem yolo11 = catalog.Single(item => item.AdapterKey == "yolo11-local");
        AssertTrue(yolo11.AvailabilityText.Contains("로컬", StringComparison.Ordinal), "YOLO11 must be represented as a local runtime");
        AssertTrue(yolo11.TaskContractText.Contains("세그멘테이션", StringComparison.Ordinal), "YOLO11 contract should include its verified segmentation scope");
        AssertTrue(yolo11.TaskContractText.Contains("이상분류", StringComparison.Ordinal), "YOLO11 contract should include its verified anomaly-classification runtime scope");
        AssertTrue(yolo11.DataContractText.Contains("native data.yaml", StringComparison.OrdinalIgnoreCase), "YOLO11 contract should preserve native data.yaml intake");
        AssertTrue(yolo11.DataContractText.Contains("normal/abnormal", StringComparison.Ordinal), "YOLO11 contract should name the anomaly classification dataset mapping");
        AssertTrue(yolo11.RuntimeContractText.Contains("yolo11n-cls.pt", StringComparison.OrdinalIgnoreCase), "YOLO11 contract should name the verified classification seed path");
        AssertTrue(yolo11.EvidenceContractText.Contains("82/104", StringComparison.Ordinal), "YOLO11 contract should expose the current held anomaly result");
        AssertTrue(yolo11.EvidenceContractText.Contains("자동 채택", StringComparison.Ordinal), "YOLO11 comparison must not imply automatic adoption");
        AssertTrue(yolo11.NextActionText.Contains("Recipe 작업", StringComparison.Ordinal), "YOLO11 next action should remain task-neutral across detection, segmentation, and anomaly classification");

        var viewModel = new WpfYoloModelSettingsPanelViewModel();
        viewModel.LoadFrom(new PythonModelSettings
        {
            ModelEngine = PythonModelSettings.EngineYoloV8
        });
        AssertEqual(catalog.Count, viewModel.ModelAdapterCatalogItems.Count);
        AssertTrue(
            viewModel.ModelAdapterCatalogItems.Any(item => item.AdapterKey == "yolo11-local"),
            "model settings panel should expose the local YOLO11 runtime boundary");
        AssertTrue(
            viewModel.ModelAdapterCatalogItems.Any(item => item.AdapterKey == "unet-segmentation"),
            "model settings panel should expose the verified U-Net segmentation adapter");

        string xamlPath = Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Views", "WpfYoloModelSettingsPanel.xaml");
        XDocument xaml = XDocument.Load(xamlPath);
        XName xName = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
        AssertNamedXamlElement(xaml, xName, "Expander", "ModelAdapterCatalogExpander");
        AssertNamedXamlBinding(xaml, xName, "ModelAdapterCatalogBoundaryText", "Text", "ModelAdapterCatalogBoundaryText");
        AssertNamedXamlBinding(xaml, xName, "ModelAdapterCatalogItems", "ItemsSource", "ModelAdapterCatalogItems");
    }
}
