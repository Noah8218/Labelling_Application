using Lib.Common;
using MvcVisionSystem.Yolo;
using System;
using System.IO;
using System.Linq;

namespace MvcVisionSystem._1._Core
{
    public sealed class YoloTrainingDatasetPreparationService
    {
        public const string AnomalyClassificationRuntimeError =
            "anomaly classification training requires a YOLOv8 or YOLO11 runtime";

        public string LastPreparationFailureMessage { get; private set; } = string.Empty;

        public bool TryPrepare(CData data)
        {
            return TryPrepare(data, out _);
        }

        internal bool TryPrepare(CData data, out YoloTrainingDatasetRequest trainingRequest)
        {
            LastPreparationFailureMessage = string.Empty;
            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = data?.DataYamlFilePath ?? string.Empty,
                Task = ResolveTrainingTask(data?.ProjectSettings?.DatasetPurpose ?? LabelingDatasetPurpose.ObjectDetection)
            };

            data?.ProjectSettings?.EnsureDefaults();
            ExternalYoloDatasetSettings externalDataset = data?.ProjectSettings?.ExternalYoloDataset;
            string model = data?.ProjectSettings?.PythonModel?.GetProtocolModelName() ?? "yolov5";
            if (string.Equals(model, "unet", StringComparison.OrdinalIgnoreCase))
            {
                return TryPrepareUnetSegmentationTrainingDataset(data, externalDataset, out trainingRequest);
            }

            if (string.Equals(model, "patchcore", StringComparison.OrdinalIgnoreCase))
            {
                return TryPreparePatchCoreAnomalyTrainingDataset(data, out trainingRequest);
            }

            if (externalDataset?.RequiresExplicitReactivation == true)
            {
                LastPreparationFailureMessage = string.IsNullOrWhiteSpace(externalDataset.LastValidationSummary)
                    ? "External YOLO data.yaml requires explicit revalidation and activation before training."
                    : externalDataset.LastValidationSummary;
                AppLog.ABNORMAL($"External YOLO training dataset requires explicit reactivation: {LastPreparationFailureMessage}");
                return false;
            }

            if (externalDataset?.UseForTraining == true)
            {
                return TryPrepareExternalYoloTrainingDataset(data, externalDataset, out trainingRequest);
            }

            if (data?.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.AnomalyDetection)
            {
                return TryPrepareAnomalyClassificationTrainingDataset(data, out trainingRequest);
            }

            YoloSegmentationTrainingLabelExportResult segmentationExportResult = null;
            if (data?.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.Segmentation)
            {
                segmentationExportResult = YoloSegmentationTrainingLabelService.Export(data);
                foreach (string error in segmentationExportResult.Errors)
                {
                    AppLog.ABNORMAL($"YOLO segmentation label export failed: {error}");
                }

                if (!segmentationExportResult.IsReady)
                {
                    LastPreparationFailureMessage = string.Join(Environment.NewLine, segmentationExportResult.Errors);
                    return false;
                }
            }

            YoloDatasetReadinessReport report = YoloDatasetReadinessService.Build(data, refreshYaml: true);
            foreach (string error in report.Errors)
            {
                AppLog.ABNORMAL($"YOLO 학습 준비 점검 실패: {error}");
            }

            if (!report.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, report.Errors);
                return false;
            }

            foreach (string line in report.SummaryLines)
            {
                AppLog.NORMAL(line);
            }

            if (data?.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.Segmentation)
            {
                AppLog.NORMAL($"YOLO segmentation labels ready. Images:{segmentationExportResult.ImageCount}, LabelFiles:{segmentationExportResult.LabelFileCount}, Polygons:{segmentationExportResult.PolygonCount}, Backgrounds:{segmentationExportResult.BackgroundImageCount}");
            }

            return true;
        }

        private bool TryPrepareExternalYoloTrainingDataset(
            CData data,
            ExternalYoloDatasetSettings externalDataset,
            out YoloTrainingDatasetRequest trainingRequest)
        {
            YoloExternalDatasetIntakeReport report = YoloExternalDatasetIntakeService.Build(
                externalDataset?.DataYamlFilePath,
                externalDataset?.DatasetPurpose ?? LabelingDatasetPurpose.ObjectDetection);
            if (!report.IsReady)
            {
                YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);
                LastPreparationFailureMessage = string.Join(Environment.NewLine, report.Errors);
                foreach (string error in report.Errors)
                {
                    AppLog.ABNORMAL($"External YOLO training dataset validation failed: {error}");
                }

                trainingRequest = new YoloTrainingDatasetRequest();
                return false;
            }

            if (!YoloExternalDatasetIntakeService.HasCurrentSourceIdentity(externalDataset, report, out string identityError))
            {
                YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);
                YoloExternalDatasetIntakeService.MarkSourceIdentityRequiresReactivation(externalDataset, identityError);
                LastPreparationFailureMessage = identityError;
                AppLog.ABNORMAL($"External YOLO training dataset source identity changed: {identityError}");
                trainingRequest = new YoloTrainingDatasetRequest();
                return false;
            }

            YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);

            string runtimeParentPath = Path.Combine(data?.OutputRootPath ?? string.Empty, "external-yolo-runtime");
            YoloExternalRuntimeDatasetResult runtime = YoloExternalDatasetIntakeService.PrepareRuntimeDataset(
                report.DataYamlFilePath,
                report.Purpose,
                runtimeParentPath);
            if (!runtime.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, runtime.Errors);
                foreach (string error in runtime.Errors)
                {
                    AppLog.ABNORMAL($"External YOLO runtime dataset preparation failed: {error}");
                }

                trainingRequest = new YoloTrainingDatasetRequest();
                return false;
            }

            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = runtime.RuntimeDataYamlFilePath,
                Task = ResolveTrainingTask(report.Purpose),
                IsExternalSource = true,
                SourceFingerprintSha256 = report.SourceFingerprintSha256,
                RuntimeDataYamlFilePath = runtime.RuntimeDataYamlFilePath,
                DatasetVersionId = RecipeDatasetVersionService.BuildExternalDatasetVersionId(report.SourceFingerprintSha256),
                DatasetContentSha256 = report.SourceFingerprintSha256
            };
            AppLog.NORMAL($"External native YOLO dataset ready. {report.Summary} / Source:{report.DataYamlFilePath} / Runtime:{runtime.RuntimeDataYamlFilePath}");
            return true;
        }

        private bool TryPrepareAnomalyClassificationTrainingDataset(
            CData data,
            out YoloTrainingDatasetRequest trainingRequest)
        {
            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = string.Empty,
                Task = "classify"
            };

            string model = data?.ProjectSettings?.PythonModel?.GetProtocolModelName() ?? "yolov5";
            if (!string.Equals(model, "yolov8", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(model, "yolo11", StringComparison.OrdinalIgnoreCase))
            {
                LastPreparationFailureMessage = $"{AnomalyClassificationRuntimeError}. Current:{model}";
                AppLog.ABNORMAL($"YOLO anomaly classification training blocked: {LastPreparationFailureMessage}");
                return false;
            }

            AnomalyClassificationTrainingReadinessReport readiness =
                AnomalyClassificationTrainingReadinessService.Build(data);
            if (!readiness.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, readiness.Errors);
                foreach (string error in readiness.Errors)
                {
                    AppLog.ABNORMAL($"YOLO anomaly classification training failed: {error}");
                }

                return false;
            }

            AnomalyClassificationDatasetExportResult result;
            try
            {
                var exportService = new AnomalyClassificationDatasetExportService();
                result = exportService.Export(data, readiness.SourceImagePaths);
            }
            catch (Exception ex)
            {
                LastPreparationFailureMessage = $"classification dataset export failed. {ex.Message}";
                AppLog.ABNORMAL($"YOLO anomaly classification training failed: {LastPreparationFailureMessage}");
                return false;
            }
            if (result.NormalImageCount == 0 || result.AbnormalImageCount == 0)
            {
                LastPreparationFailureMessage = $"{AnomalyClassificationTrainingReadinessService.NeedsReviewedNormalAndAbnormalError}. Normal:{result.NormalImageCount}, Abnormal:{result.AbnormalImageCount}";
                AppLog.ABNORMAL($"YOLO anomaly classification training failed: {LastPreparationFailureMessage}");
                return false;
            }

            AppLog.NORMAL($"YOLO anomaly classification dataset ready. Normal:{result.NormalImageCount}, Abnormal:{result.AbnormalImageCount}, Skipped:{result.SkippedImageCount}, Path:{result.DatasetRootPath}");
            trainingRequest.DataPath = result.DatasetRootPath;
            return true;
        }

        private bool TryPreparePatchCoreAnomalyTrainingDataset(
            CData data,
            out YoloTrainingDatasetRequest trainingRequest)
        {
            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = string.Empty,
                Task = "anomaly"
            };
            PatchCoreAnomalyTrainingReadinessReport readiness =
                PatchCoreAnomalyTrainingReadinessService.Build(data);
            if (!readiness.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, readiness.Errors);
                foreach (string error in readiness.Errors)
                {
                    AppLog.ABNORMAL($"PatchCore anomaly training failed: {error}");
                }

                return false;
            }

            AnomalyClassificationDatasetExportResult result;
            try
            {
                result = new AnomalyClassificationDatasetExportService()
                    .Export(data, readiness.SourceImagePaths, Path.Combine(data.OutputRootPath, "patchcore"));
            }
            catch (Exception ex)
            {
                LastPreparationFailureMessage = $"PatchCore dataset export failed. {ex.Message}";
                AppLog.ABNORMAL(LastPreparationFailureMessage);
                return false;
            }

            string trainNormalPath = Path.Combine(
                result.DatasetRootPath,
                YoloDatasetSplitService.TrainMode,
                AnomalyClassificationDatasetExportService.NormalClassFolderName);
            int exportedTrainNormalCount = Directory.Exists(trainNormalPath)
                ? Directory.EnumerateFiles(trainNormalPath, "*", SearchOption.AllDirectories).Count()
                : 0;
            if (exportedTrainNormalCount < 2)
            {
                LastPreparationFailureMessage = $"{PatchCoreAnomalyTrainingReadinessService.NeedsReviewedNormalError}. ExportedTrainNormal:{exportedTrainNormalCount}";
                AppLog.ABNORMAL(LastPreparationFailureMessage);
                return false;
            }

            foreach (string warning in readiness.Warnings)
            {
                AppLog.NORMAL($"PatchCore anomaly training warning: {warning}");
            }

            trainingRequest.DataPath = result.DatasetRootPath;
            AppLog.NORMAL($"PatchCore normal-only dataset ready. TrainNormal:{readiness.TrainNormalCount}, ValidationNormal:{readiness.ValidationNormalCount}, ReviewedAbnormalExcludedFromLearning:{readiness.ReviewedAbnormalCount}, Path:{result.DatasetRootPath}");
            return true;
        }

        private bool TryPrepareUnetSegmentationTrainingDataset(
            CData data,
            ExternalYoloDatasetSettings externalDataset,
            out YoloTrainingDatasetRequest trainingRequest)
        {
            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = string.Empty,
                Task = "segment"
            };
            if (data?.ProjectSettings?.DatasetPurpose != LabelingDatasetPurpose.Segmentation)
            {
                LastPreparationFailureMessage = "U-Net training requires a segmentation recipe with masks or polygons.";
                AppLog.ABNORMAL(LastPreparationFailureMessage);
                return false;
            }

            if (externalDataset?.RequiresExplicitReactivation == true)
            {
                LastPreparationFailureMessage = string.IsNullOrWhiteSpace(externalDataset.LastValidationSummary)
                    ? "External YOLO data.yaml requires explicit revalidation and activation before U-Net training."
                    : externalDataset.LastValidationSummary;
                AppLog.ABNORMAL(LastPreparationFailureMessage);
                return false;
            }

            if (externalDataset?.UseForTraining == true)
            {
                return TryPrepareExternalUnetSegmentationTrainingDataset(data, externalDataset, out trainingRequest);
            }

            UnetSegmentationDatasetExportResult result = UnetSegmentationDatasetExportService.Export(data);
            foreach (string error in result.Errors)
            {
                AppLog.ABNORMAL($"U-Net segmentation export failed: {error}");
            }

            if (!result.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, result.Errors);
                return false;
            }

            trainingRequest.DataPath = result.OutputRootPath;
            AppLog.NORMAL($"U-Net segmentation dataset ready. Images:{result.ImageCount}, PositiveMasks:{result.PositiveMaskImageCount}, Path:{result.OutputRootPath}");
            return true;
        }

        private bool TryPrepareExternalUnetSegmentationTrainingDataset(
            CData data,
            ExternalYoloDatasetSettings externalDataset,
            out YoloTrainingDatasetRequest trainingRequest)
        {
            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = string.Empty,
                Task = "segment"
            };
            if (externalDataset?.DatasetPurpose != LabelingDatasetPurpose.Segmentation)
            {
                LastPreparationFailureMessage = "U-Net external data.yaml training requires a Segmentation native YOLO source.";
                AppLog.ABNORMAL(LastPreparationFailureMessage);
                return false;
            }

            YoloExternalDatasetIntakeReport report = YoloExternalDatasetIntakeService.Build(
                externalDataset.DataYamlFilePath,
                LabelingDatasetPurpose.Segmentation);
            if (!report.IsReady)
            {
                YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);
                LastPreparationFailureMessage = string.Join(Environment.NewLine, report.Errors);
                foreach (string error in report.Errors)
                {
                    AppLog.ABNORMAL($"External U-Net segmentation dataset validation failed: {error}");
                }
                return false;
            }

            if (!YoloExternalDatasetIntakeService.HasCurrentSourceIdentity(externalDataset, report, out string identityError))
            {
                YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);
                YoloExternalDatasetIntakeService.MarkSourceIdentityRequiresReactivation(externalDataset, identityError);
                LastPreparationFailureMessage = identityError;
                AppLog.ABNORMAL($"External U-Net segmentation source identity changed: {identityError}");
                return false;
            }

            YoloExternalDatasetIntakeService.ApplyValidation(externalDataset, report);
            UnetSegmentationDatasetExportResult result =
                ExternalYoloSegmentationCanonicalExportService.Export(data, report.DataYamlFilePath);
            foreach (string error in result.Errors)
            {
                AppLog.ABNORMAL($"External U-Net segmentation canonical export failed: {error}");
            }
            if (!result.IsReady)
            {
                LastPreparationFailureMessage = string.Join(Environment.NewLine, result.Errors);
                return false;
            }

            trainingRequest = new YoloTrainingDatasetRequest
            {
                DataPath = result.OutputRootPath,
                Task = "segment",
                IsExternalSource = true,
                SourceFingerprintSha256 = report.SourceFingerprintSha256,
                RuntimeDataYamlFilePath = result.OutputRootPath,
                DatasetVersionId = RecipeDatasetVersionService.BuildExternalDatasetVersionId(report.SourceFingerprintSha256),
                DatasetContentSha256 = report.SourceFingerprintSha256
            };
            AppLog.NORMAL($"External native YOLO segmentation U-Net dataset ready. Images:{result.ImageCount}, PositiveMasks:{result.PositiveMaskImageCount}, Source:{report.DataYamlFilePath}, Canonical:{result.OutputRootPath}");
            return true;
        }

        private static string ResolveTrainingTask(LabelingDatasetPurpose datasetPurpose)
        {
            return datasetPurpose == LabelingDatasetPurpose.Segmentation
                ? "segment"
                : "detect";
        }
    }

    internal sealed class YoloTrainingDatasetRequest
    {
        public string DataPath { get; set; } = string.Empty;

        public string Task { get; set; } = "detect";

        public bool IsExternalSource { get; set; }

        public string SourceFingerprintSha256 { get; set; } = string.Empty;

        public string RuntimeDataYamlFilePath { get; set; } = string.Empty;

        public string DatasetVersionId { get; set; } = string.Empty;

        public string DatasetContentSha256 { get; set; } = string.Empty;
    }
}
