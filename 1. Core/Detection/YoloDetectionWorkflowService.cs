using MvcVisionSystem._3._Communication.TCP;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MvcVisionSystem._1._Core
{
    public sealed class YoloDetectionWorkflowService
    {
        public bool TryStartCurrentImageDetection(
            LabelingProjectData data,
            PythonModelCommunication communication,
            DetectionTransportService detectionTransport,
            Func<bool> ensurePythonClientReady)
        {
            if (!ValidatePythonModelSettings(data, out string validationError))
            {
                communication?.SetLastError(validationError);
                return false;
            }

            if (communication == null)
            {
                AppLog.ABNORMAL("YOLO 검사 통신이 초기화되지 않았습니다.");
                return false;
            }

            if (detectionTransport == null)
            {
                AppLog.ABNORMAL("YOLO 검사 transport가 초기화되지 않았습니다.");
                return false;
            }

            if (ensurePythonClientReady == null || !ensurePythonClientReady())
            {
                return false;
            }

            int timeoutSeconds = data.ProjectSettings?.PythonModel?.DetectionTimeoutSeconds ?? 30;
            return detectionTransport.TrySendCurrentImageForDetection(communication, timeoutSeconds);
        }

        public bool TryStartCurrentImageDetection(
            LabelingProjectData data,
            PythonModelCommunication communication,
            DetectionResultApplicationService detectionResults,
            Func<bool> ensurePythonClientReady)
            => TryStartCurrentImageDetection(
                data,
                communication,
                detectionResults?.Transport,
                ensurePythonClientReady);

        public bool TryStartImagePathDetection(
            LabelingProjectData data,
            PythonModelCommunication communication,
            DetectionTransportService detectionTransport,
            string imagePath,
            Size imageSize,
            Func<bool> ensurePythonClientReady)
        {
            if (!ValidatePythonModelSettings(data, out string validationError))
            {
                communication?.SetLastError(validationError);
                return false;
            }

            if (communication == null)
            {
                AppLog.ABNORMAL("YOLO 검사 통신이 초기화되지 않았습니다.");
                return false;
            }

            if (detectionTransport == null)
            {
                AppLog.ABNORMAL("YOLO 검사 transport가 초기화되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                communication.SetLastError($"검사 이미지 파일을 찾을 수 없습니다: {imagePath}");
                return false;
            }

            if (imageSize.IsEmpty)
            {
                communication.SetLastError($"검사 이미지 크기를 확인할 수 없습니다: {imagePath}");
                return false;
            }

            if (ensurePythonClientReady == null || !ensurePythonClientReady())
            {
                return false;
            }

            int timeoutSeconds = data.ProjectSettings?.PythonModel?.DetectionTimeoutSeconds ?? 30;
            return detectionTransport.TrySendImagePathForDetection(
                communication,
                data,
                imagePath,
                imageSize,
                timeoutSeconds);
        }

        public bool TryStartImagePathDetection(
            LabelingProjectData data,
            PythonModelCommunication communication,
            DetectionResultApplicationService detectionResults,
            string imagePath,
            Size imageSize,
            Func<bool> ensurePythonClientReady)
            => TryStartImagePathDetection(
                data,
                communication,
                detectionResults?.Transport,
                imagePath,
                imageSize,
                ensurePythonClientReady);

        private static bool ValidatePythonModelSettings(LabelingProjectData data, out string validationError)
        {
            validationError = "";
            if (data == null)
            {
                validationError = "YOLO 검사 데이터가 초기화되지 않았습니다.";
                AppLog.ABNORMAL(validationError);
                return false;
            }

            data.ProjectSettings ??= new LabelingProjectSettings();
            data.ProjectSettings.EnsureDefaults();

            PythonModelValidationResult validation = PythonModelSettingsValidator.Validate(
                data.ProjectSettings.PythonModel,
                requireWeights: true);

            foreach (string warning in validation.Warnings)
            {
                AppLog.COMM(warning);
            }

            foreach (string error in validation.Errors)
            {
                AppLog.ABNORMAL(error);
            }

            validationError = validation.Errors.FirstOrDefault() ?? "";
            return validation.IsValid;
        }
    }
}
