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
using static LabelingApplication.Tests.TestSupport;

namespace LabelingApplication.Tests;

internal static class YoloImageReviewStatusTests
{
    internal static void TestYoloImageLabelStatusService()
    {
        string root = CreateTempRoot();
        string externalRoot = root + "-source";
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "Part", DrawColor = Color.Blue });
            data.EnsureYoloOutputDirectories();

            string imagePath = Path.Combine(root, "source", "sample.png");
            YoloImageLabelStatus missing = YoloImageLabelStatusService.Build(imagePath, new Size(20, 20), data);
            AssertTrue(!missing.HasLabelFile, "missing label status should not report a label file");
            AssertEqual("No Label", missing.Text);

            string reusedImagesDirectory = Path.Combine(externalRoot, "Images");
            string reusedLabelsDirectory = Path.Combine(externalRoot, "labels");
            Directory.CreateDirectory(reusedImagesDirectory);
            Directory.CreateDirectory(reusedLabelsDirectory);
            string reusedImagePath = Path.Combine(reusedImagesDirectory, "shared.png");
            string reusedSiblingLabelPath = Path.Combine(reusedLabelsDirectory, "shared.txt");
            string reusedSidecarLabelPath = Path.ChangeExtension(reusedImagePath, ".txt");
            File.WriteAllText(reusedImagePath, string.Empty);
            File.WriteAllText(reusedSiblingLabelPath, "0 0.5 0.5 0.5 0.5");
            File.WriteAllText(reusedSidecarLabelPath, "0 0.5 0.5 0.5 0.5");

            YoloImageLabelStatus legacyShared = YoloImageLabelStatusService.Build(reusedImagePath, new Size(20, 20), null);
            AssertTrue(legacyShared.HasLabelFile, "legacy standalone lookup should still find a sibling label file");
            AssertEqual(reusedSiblingLabelPath, legacyShared.LabelPath);

            YoloImageLabelStatus isolatedShared = YoloImageLabelStatusService.Build(reusedImagePath, new Size(20, 20), data);
            AssertTrue(!isolatedShared.HasLabelFile, "new dataset should not inherit labels from a reused external image folder");
            IReadOnlyDictionary<string, List<Rectangle>> inheritedAnnotations = YoloAnnotationService.LoadAnnotationRectanglesForImage(
                reusedImagePath,
                data.ClassNamedList,
                data,
                new Size(20, 20));
            AssertEqual(0, inheritedAnnotations.Count);

            string activeSharedLabelPath = Path.Combine(root, "data", "train", "labels", "shared.txt");
            File.WriteAllText(activeSharedLabelPath, "0 0.5 0.5 0.5 0.5");
            YoloImageLabelStatus activeShared = YoloImageLabelStatusService.Build(reusedImagePath, new Size(20, 20), data);
            AssertTrue(activeShared.HasLabelFile, "active dataset label lookup should find the new dataset label");
            AssertEqual(activeSharedLabelPath, activeShared.LabelPath);
            IReadOnlyDictionary<string, List<Rectangle>> activeSharedAnnotations = YoloAnnotationService.LoadAnnotationRectanglesForImage(
                reusedImagePath,
                data.ClassNamedList,
                data,
                new Size(20, 20));
            AssertEqual(1, activeSharedAnnotations["Part"].Count);

            string labelPath = Path.Combine(root, "data", "train", "labels", "sample.txt");
            File.WriteAllLines(labelPath, new[]
            {
                "0 0.5 0.5 0.5 0.5",
                "9 0.5 0.5 0.5 0.5",
                "bad line"
            });

            YoloImageLabelStatus status = YoloImageLabelStatusService.Build(imagePath, new Size(20, 20), data);
            AssertTrue(status.HasLabelFile, "label status did not locate the saved label file");
            AssertEqual(labelPath, status.LabelPath);
            AssertEqual(1, status.ObjectCount);
            AssertEqual(2, status.InvalidLineCount);
            AssertEqual("Label 1 / Invalid 2", status.Text);
        }
        finally
        {
            DeleteTempRoot(root);
            DeleteDirectoryIfExists(externalRoot);
        }
    }

    internal static void TestYoloImageReviewStatusService()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "Part", DrawColor = Color.Blue });
            data.EnsureYoloOutputDirectories();

            string reviewStatusPath = YoloImageReviewStatusService.ResolveReviewStatusFilePath(data);
            var untouched = new YoloImageReviewStatusService();
            untouched.SetImages(new[] { Path.Combine(root, "source", "untouched.png") });
            untouched.SaveReviewStatus(data);
            AssertTrue(!File.Exists(reviewStatusPath), "untouched review state should not create an empty cache");

            string sourceDirectory = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceDirectory);

            string labeledImagePath = Path.Combine(sourceDirectory, "labeled.png");
            string candidateImagePath = Path.Combine(sourceDirectory, "candidate.png");
            string failedImagePath = Path.Combine(sourceDirectory, "failed.png");
            string skippedImagePath = Path.Combine(sourceDirectory, "skipped.png");
            string emptyImagePath = Path.Combine(sourceDirectory, "empty.png");
            var imagePaths = new List<string> { labeledImagePath, candidateImagePath, failedImagePath, skippedImagePath, emptyImagePath };

            string labelPath = Path.Combine(root, "data", "train", "labels", "labeled.txt");
            File.WriteAllText(labelPath, "0 0.5 0.5 0.25 0.25");

            var service = new YoloImageReviewStatusService();
            service.SetImages(imagePaths);
            service.RefreshLabelStatus(labeledImagePath, new Size(100, 100), data);
            service.RefreshLabelStatus(candidateImagePath, new Size(100, 100), data);
            service.RefreshLabelStatus(failedImagePath, new Size(100, 100), data);
            service.RefreshLabelStatus(skippedImagePath, new Size(100, 100), data);

            AssertEqual(1, service.GetLabeledCount());
            AssertTrue(service.TryFindNextUnlabeled(imagePaths, labeledImagePath, out string nextImagePath), "next unlabeled image was not found");
            AssertEqual(candidateImagePath, nextImagePath);

            YoloImageReviewStatus requested = service.SetDetectionRequested(candidateImagePath);
            AssertEqual("Requested", requested.DetectionText);

            YoloImageReviewStatus candidates = service.SetDetectionCandidates(string.Empty, "candidate", 2);
            AssertEqual(candidateImagePath, candidates.ImagePath);
            AssertEqual(2, candidates.DetectionCandidateCount);
            AssertEqual("Candidate 2", candidates.DetectionText);

            string candidateLabelPath = Path.Combine(root, "data", "train", "labels", "candidate.txt");
            File.WriteAllText(candidateLabelPath, "0 0.5 0.5 0.25 0.25");
            YoloImageReviewStatus candidateWithActiveBoxes = service.RefreshLabelStatusAndReviewState(candidateImagePath, new Size(100, 100), data, hasActiveCandidates: true);
            AssertEqual("Candidate 2", candidateWithActiveBoxes.DetectionText);
            AssertTrue(candidateWithActiveBoxes.IsLabeled, "saved label status should be preserved while active candidates remain");
            AssertEqual("Label 1", candidateWithActiveBoxes.LabelText);

            YoloImageReviewStatus noCandidates = service.SetDetectionNoCandidates(candidateImagePath, "candidate");
            AssertEqual("No Candidate", noCandidates.DetectionText);

            YoloImageReviewStatus cleared = service.ClearDetectionStatus(candidateImagePath);
            AssertEqual(string.Empty, cleared.DetectionText);

            YoloImageReviewStatus failedRequest = service.SetDetectionRequested(failedImagePath);
            AssertEqual(1, failedRequest.DetectionAttemptCount);

            YoloImageReviewStatus failed = service.SetDetectionFailed(failedImagePath, string.Empty, "Batch detection timed out.");
            AssertEqual("Failed", failed.DetectionText);
            AssertEqual(1, failed.DetectionAttemptCount);
            AssertTrue(failed.DetectionDetailText.Contains("Batch detection timed out."), "failed detection detail did not include the failure reason");

            service.SetDetectionRequested(failedImagePath);
            YoloImageReviewStatus retriedFailure = service.SetDetectionFailed(failedImagePath, string.Empty, "Detection request failed.");
            AssertEqual(2, retriedFailure.DetectionAttemptCount);
            AssertTrue(retriedFailure.DetectionDetailText.Contains("Attempt 2"), "retry count was not reflected in detection detail text");

            YoloImageReviewStatus skipped = service.MarkSkipped(skippedImagePath);
            AssertEqual("Skipped", skipped.DetectionText);
            AssertEqual(0, skipped.DetectionCandidateCount);
            AssertTrue(skipped.DetectionDetailText.Contains("Candidate skipped."), "skipped review detail did not include the skip reason");

            string emptyLabelPath = Path.Combine(root, "data", "train", "labels", "empty.txt");
            File.WriteAllText(emptyLabelPath, string.Empty);
            YoloImageReviewStatus emptyCompleted = service.RefreshLabelStatusAndReviewState(emptyImagePath, new Size(100, 100), data, hasActiveCandidates: false);
            AssertEqual("No Candidate", emptyCompleted.DetectionText);
            AssertEqual("Empty Label", emptyCompleted.LabelText);

            YoloImageReviewStatus reviewedNoCandidate = service.SetDetectionNoCandidates(candidateImagePath, "candidate");
            AssertEqual("No Candidate", reviewedNoCandidate.DetectionText);
            AssertTrue(service.TryFindNextUnlabeled(imagePaths, labeledImagePath, out string nextReviewImagePath), "next image that still needs review was not found");
            AssertEqual(failedImagePath, nextReviewImagePath);

            YoloImageReviewStatus confirmed = service.MarkConfirmed(labeledImagePath);
            AssertEqual("Confirmed", confirmed.DetectionText);
            AssertEqual(0, confirmed.DetectionCandidateCount);
            AssertTrue(confirmed.DetectionDetailText.Contains("Candidates confirmed."), "confirmed review detail did not include the confirmation reason");

            YoloImageReviewStatus confirmedBySavedLabel = service.RefreshLabelStatusAndReviewState(labeledImagePath, new Size(100, 100), data, hasActiveCandidates: false);
            AssertEqual("Confirmed", confirmedBySavedLabel.DetectionText);

            YoloImageReviewStatus needsFix = service.MarkQualityNeedsFix(
                labeledImagePath,
                qualityReviewNote: "  경계가 흐림\r\n마스크 재작업  ");
            AssertEqual(YoloImageQualityReviewState.NeedsFix, needsFix.QualityReviewState);
            AssertEqual("경계가 흐림  마스크 재작업", needsFix.QualityReviewNote);
            AssertEqual(
                YoloImageQualityReviewState.NeedsFix,
                service.InvalidateQualityReviewAfterEdit(labeledImagePath).QualityReviewState);
            YoloImageReviewStatus qualityReviewed = service.MarkQualityReviewed(labeledImagePath);
            AssertEqual(YoloImageQualityReviewState.Reviewed, qualityReviewed.QualityReviewState);
            AssertEqual(string.Empty, qualityReviewed.QualityReviewNote);
            AssertEqual(
                YoloImageQualityReviewState.Unreviewed,
                service.InvalidateQualityReviewAfterEdit(labeledImagePath).QualityReviewState);
            AssertEqual(
                YoloImageQualityReviewState.Unreviewed,
                service.ClearQualityReview(labeledImagePath).QualityReviewState);
            service.MarkQualityNeedsFix(labeledImagePath, qualityReviewNote: "경계 | 마스크 수정");

            service.SetDetectionCandidates(candidateImagePath, "candidate", 2);
            service.SaveReviewStatus(data);

            AssertTrue(File.Exists(reviewStatusPath), "review status file was not saved");
            string reviewStatusJson = File.ReadAllText(reviewStatusPath);
            AssertTrue(reviewStatusJson.Contains("\"ReviewStateName\": \"Confirmed\""), "review status file did not include a readable confirmed state name");
            AssertTrue(reviewStatusJson.Contains("\"ReviewStateName\": \"Candidate\""), "review status file did not include a readable candidate state name");
            AssertTrue(reviewStatusJson.Contains("\"QualityReviewStateName\": \"NeedsFix\""), "review status file did not include a readable quality-review state name");
            AssertTrue(reviewStatusJson.Contains("\"QualityReviewNote\": \"경계 | 마스크 수정\""), "review status file did not include the short quality-review note");

            var restored = new YoloImageReviewStatusService();
            restored.LoadReviewStatus(data, imagePaths);

            AssertEqual("Confirmed", restored.GetOrCreate(labeledImagePath).DetectionText);
            AssertEqual(YoloImageQualityReviewState.NeedsFix, restored.GetOrCreate(labeledImagePath).QualityReviewState);
            AssertEqual("경계 | 마스크 수정", restored.GetOrCreate(labeledImagePath).QualityReviewNote);
            AssertEqual("Candidate 2", restored.GetOrCreate(candidateImagePath).DetectionText);
            AssertEqual("Failed", restored.GetOrCreate(failedImagePath).DetectionText);
            AssertEqual(2, restored.GetOrCreate(failedImagePath).DetectionAttemptCount);
            AssertEqual("Detection request failed.", restored.GetOrCreate(failedImagePath).LastDetectionMessage);
            AssertEqual("Skipped", restored.GetOrCreate(skippedImagePath).DetectionText);

            string namedOnlyStatusJson = reviewStatusJson.Replace("\"ReviewState\": 4,", "\"ReviewState\": 999,");
            File.WriteAllText(reviewStatusPath, namedOnlyStatusJson);
            var restoredFromName = new YoloImageReviewStatusService();
            restoredFromName.LoadReviewStatus(data, imagePaths);
            AssertEqual("Confirmed", restoredFromName.GetOrCreate(labeledImagePath).DetectionText);

            var clearedCache = new YoloImageReviewStatusService();
            clearedCache.SetImages(imagePaths);
            clearedCache.SaveReviewStatus(data);
            AssertTrue(
                string.Equals("[]", File.ReadAllText(reviewStatusPath), StringComparison.Ordinal),
                "existing review cache should remain clearable");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloImageQualityReviewReportExport()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            string needsFixPath = Path.Combine(root, "images", "needs-fix.png");
            string reviewedPath = Path.Combine(root, "images", "reviewed.png");
            string unreviewedPath = Path.Combine(root, "images", "unreviewed.png");
            var service = new YoloImageReviewStatusService();
            service.SetImages(new[] { needsFixPath, reviewedPath, unreviewedPath });
            service.MarkQualityNeedsFix(
                needsFixPath,
                qualityReviewNote: "경계 | 불명확\n마스크 재작업");
            service.MarkQualityReviewed(reviewedPath);

            string outputPath = YoloImageQualityReviewReportExportService.ResolveDefaultOutputPath(data);
            var generatedAtUtc = new DateTime(2026, 7, 11, 3, 0, 0, DateTimeKind.Utc);
            YoloImageQualityReviewReportExportResult result = YoloImageQualityReviewReportExportService.ExportMarkdown(
                service.GetItems(),
                outputPath,
                generatedAtUtc);

            AssertTrue(File.Exists(outputPath), "local label quality review markdown was not written");
            AssertEqual(outputPath, result.OutputPath);
            AssertEqual(3, result.TotalImageCount);
            AssertEqual(1, result.UnreviewedCount);
            AssertEqual(1, result.NeedsFixCount);
            AssertEqual(1, result.ReviewedCount);
            string markdown = File.ReadAllText(outputPath);
            AssertTrue(markdown.Contains("# 라벨 품질 검수 보고서", StringComparison.Ordinal), "quality report should have an operator-facing title");
            AssertTrue(markdown.Contains("2026-07-11 03:00:00", StringComparison.Ordinal), "quality report should include a deterministic UTC generation time");
            AssertTrue(markdown.Contains("needs-fix.png", StringComparison.Ordinal), "quality report should list the issue image");
            AssertTrue(markdown.Contains("경계 \\| 불명확 마스크 재작업", StringComparison.Ordinal), "quality report should normalize and escape the issue reason");
            AssertTrue(!markdown.Contains(root, StringComparison.OrdinalIgnoreCase), "quality report should not expose the local absolute dataset path");

            string oversizedNote = new string('A', YoloImageReviewStatusService.QualityReviewNoteMaxLength + 20);
            YoloImageReviewStatus normalized = service.MarkQualityNeedsFix(unreviewedPath, qualityReviewNote: oversizedNote);
            AssertEqual(YoloImageReviewStatusService.QualityReviewNoteMaxLength, normalized.QualityReviewNote.Length);
            service.ClearQualityReview(unreviewedPath);
            AssertEqual(string.Empty, service.GetOrCreate(unreviewedPath).QualityReviewNote);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
