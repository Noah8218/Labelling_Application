using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenVisionLab.ImageCanvas.CanvasShapes;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class MobileSamBoxPromptTests
{
    internal static int RunSmartMaskCandidateCompareRestore(string[] args)
    {
        try
        {
            string artifactRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(FindRepositoryRoot(), "artifacts", "smart-mask-candidate-compare-restore")));
            string runRoot = Path.Combine(artifactRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(runRoot);
            string imagePath = Path.Combine(runRoot, "compare-source.png");
            using (var source = new Bitmap(400, 300))
            using (Graphics graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.FromArgb(36, 42, 52));
                using var brush = new SolidBrush(Color.FromArgb(210, 220, 225));
                graphics.FillEllipse(brush, 60, 40, 200, 160);
                source.Save(imagePath);
            }

            if (System.Windows.Application.Current == null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };
            }

            CData previousData = CGlobal.Inst.Data;
            var data = new CData();
            data.ConfigureOutputRoot(runRoot);
            data.LastSelectImageName = Path.GetFileNameWithoutExtension(imagePath);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.DeepSkyBlue });
            CGlobal.Inst.Data = data;

            WpfLabelingShellWindow window = new WpfLabelingShellWindow();
            Bitmap bitmap = new Bitmap(imagePath);
            try
            {
                SetPrivateField(window, "activeImagePath", imagePath);
                SetPrivateField(window, "activeImageSize", bitmap.Size);
                SetPrivateField(window, "activeImageBitmap", bitmap);
                SetPrivateField(window.MainCanvasViewModel, "_imageSize", bitmap.Size);

                Rectangle promptBounds = new Rectangle(60, 40, 200, 160);
                var initialCandidate = CreateVisualSmokeSmartMaskCandidate(promptBounds, inset: 0, index: 1);
                var latestCandidate = CreateVisualSmokeSmartMaskCandidate(promptBounds, inset: 20, index: 2);
                var session = GetPrivateField<WpfSmartMaskPromptSessionService>(window, "smartMaskPromptSession");
                string recipeName = InvokePrivateResult<string>(window, "GetCurrentSmartMaskRecipeName");
                session.Start(imagePath, recipeName, promptBounds, 0, "Defect");
                AssertTrue(session.RecordCandidate(initialCandidate), "initial comparison candidate should be retained");
                AssertTrue(session.RecordCandidate(latestCandidate), "latest comparison candidate should be retained");

                InvokePrivateResult<object>(
                    window,
                    "ApplyDetectionCandidatesPreservingConfirmed",
                    new[] { latestCandidate },
                    true);
                InvokePrivateResult<object>(
                    window,
                    "ExecuteSelectSmartMaskCandidateVersionCommand",
                    WpfSmartMaskCandidateVersion.Initial);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));

                WpfCandidateReviewStateService candidateState =
                    GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
                AssertTrue(
                    candidateState.PendingCount == 1
                        && ReferenceEquals(candidateState.PendingCandidates[0], initialCandidate),
                    "restoring the initial candidate should replace the one visible pending candidate");
                string segmentDirectory = Path.Combine(runRoot, "data", "train", "segments");
                AssertTrue(
                    !Directory.Exists(segmentDirectory)
                        || !Directory.EnumerateFiles(segmentDirectory, "*.json").Any(),
                    "candidate comparison should not create canonical segment files before confirmation");

                window.CandidateReviewViewModel.ConfirmSelectedCommand.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(200));
                AssertEqual(0, candidateState.PendingCount);
                AssertEqual(1, candidateState.ConfirmedCount);
                AssertTrue(
                    ReferenceEquals(candidateState.ConfirmedCandidates[0], initialCandidate),
                    "confirmation should adopt only the explicitly restored candidate");
                AssertTrue(!session.HasCandidateComparison, "confirmation should clear session-only candidate history");

                string segmentPath = Directory.EnumerateFiles(
                    segmentDirectory,
                    "*.json").Single();
                JObject segment = JObject.Parse(File.ReadAllText(segmentPath));
                JObject savedFirstPoint = (JObject)segment["Polygons"]?[0]?["Points"]?[0];
                DetectionPolygonPoint expectedFirstPoint = initialCandidate.PolygonPoints[0];
                AssertEqual((int)Math.Round(expectedFirstPoint.X), savedFirstPoint?.Value<int>("X") ?? int.MinValue);
                AssertEqual((int)Math.Round(expectedFirstPoint.Y), savedFirstPoint?.Value<int>("Y") ?? int.MinValue);

                string evidencePath = Path.Combine(runRoot, "candidate-compare-restore-evidence.json");
                File.WriteAllText(
                    evidencePath,
                    new JObject
                    {
                        ["status"] = "Complete",
                        ["scope"] = "Session-only Smart Mask initial/latest compare, restore, and selected canonical save",
                        ["evidenceOrigin"] = "synthetic",
                        ["fieldValidation"] = "Not evaluated",
                        ["productionAccuracyClaimed"] = false,
                        ["selectedVersion"] = WpfSmartMaskCandidateVersion.Initial.ToString(),
                        ["confirmedCandidateIndex"] = candidateState.ConfirmedCandidates[0].Index,
                        ["savedSegmentPath"] = segmentPath,
                        ["savedFirstPoint"] = new JArray(
                            savedFirstPoint?.Value<int>("X") ?? int.MinValue,
                            savedFirstPoint?.Value<int>("Y") ?? int.MinValue),
                        ["boundary"] = "Workflow and persistence safety evidence only; no model-accuracy claim."
                    }.ToString(Formatting.Indented));
                Console.WriteLine("SMART_MASK_COMPARE_RESTORE_EVIDENCE=" + evidencePath);
                return 0;
            }
            finally
            {
                SetPrivateField(window, "activeImageBitmap", null);
                window.Close();
                bitmap.Dispose();
                CGlobal.Inst.Data = previousData;
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("SMART_MASK_COMPARE_RESTORE_FAILED=" + error);
            return 1;
        }
    }

    internal static int RunRealMobileSamPointCorrection(string[] args)
    {
        try
        {
            string artifactRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(FindRepositoryRoot(), "artifacts", "mobile-sam-point-correction")));
            string runRoot = Path.Combine(artifactRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(runRoot);
            string imagePath = Path.Combine(runRoot, "two-objects-source.png");
            using (var bitmap = new Bitmap(512, 512))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var desiredBrush = new SolidBrush(Color.FromArgb(230, 230, 230)))
            using (var unwantedBrush = new SolidBrush(Color.FromArgb(190, 190, 190)))
            {
                graphics.Clear(Color.FromArgb(35, 35, 35));
                graphics.FillEllipse(desiredBrush, 90, 150, 150, 190);
                graphics.FillEllipse(unwantedBrush, 280, 150, 150, 190);
                bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            string sourceHashBefore = ComputeFileSha256(imagePath);
            var service = new WpfMobileSamBoxPromptService();
            var settings = new PythonModelSettings();
            settings.EnsureDefaults();
            WpfMobileSamBoxPromptResult partialBox = RunPointCorrectionTrial(
                service,
                settings,
                imagePath,
                new Rectangle(140, 200, 60, 80),
                Array.Empty<WpfSmartMaskPromptPoint>(),
                96);
            WpfMobileSamBoxPromptResult positiveExpanded = RunPointCorrectionTrial(
                service,
                settings,
                imagePath,
                new Rectangle(140, 200, 60, 80),
                new[]
                {
                    new WpfSmartMaskPromptPoint
                    {
                        Position = new Point(160, 170),
                        Kind = WpfSmartMaskPointKind.Positive
                    }
                },
                96);
            WpfMobileSamBoxPromptResult positiveWide = RunPointCorrectionTrial(
                service,
                settings,
                imagePath,
                new Rectangle(70, 130, 380, 230),
                new[]
                {
                    new WpfSmartMaskPromptPoint
                    {
                        Position = new Point(160, 245),
                        Kind = WpfSmartMaskPointKind.Positive
                    }
                },
                48);
            WpfMobileSamBoxPromptResult positiveNegative = RunPointCorrectionTrial(
                service,
                settings,
                imagePath,
                new Rectangle(70, 130, 380, 230),
                new[]
                {
                    new WpfSmartMaskPromptPoint
                    {
                        Position = new Point(160, 245),
                        Kind = WpfSmartMaskPointKind.Positive
                    },
                    new WpfSmartMaskPromptPoint
                    {
                        Position = new Point(355, 245),
                        Kind = WpfSmartMaskPointKind.Negative
                    }
                },
                48);

            Rectangle baseBounds = partialBox.Candidate.ToRectangle();
            Rectangle positiveBounds = positiveExpanded.Candidate.ToRectangle();
            AssertTrue(
                positiveExpanded.MaskArea >= partialBox.MaskArea * 1.20D,
                "a positive point outside the partial box should add the desired upper object region");
            AssertTrue(
                positiveBounds.Top <= 175 && positiveBounds.Top < baseBounds.Top - 20,
                "positive correction should expand the candidate toward the clicked desired region");
            AssertTrue(
                positiveNegative.MaskArea <= positiveWide.MaskArea * 0.70D,
                "a negative point on the unwanted object should remove substantial unwanted mask area");
            AssertTrue(
                positiveNegative.MaskArea >= 15000,
                "negative correction should retain the positively selected desired object");
            AssertTrue(
                positiveWide.Candidate.PolygonPoints.Count <= 48
                    && positiveNegative.Candidate.PolygonPoints.Count <= 48,
                "fast polygon detail should cap the returned review polygon at 48 points");
            string sourceHashAfter = ComputeFileSha256(imagePath);
            AssertEqual(sourceHashBefore, sourceHashAfter);

            var evidence = new JObject
            {
                ["status"] = "Complete",
                ["scope"] = "Real MobileSAM positive/negative point-correction acceptance fixture",
                ["evidenceOrigin"] = "synthetic",
                ["fieldValidation"] = "Not evaluated",
                ["productionAccuracyClaimed"] = false,
                ["sourceImagePath"] = imagePath,
                ["sourceImageSha256Before"] = sourceHashBefore,
                ["sourceImageSha256After"] = sourceHashAfter,
                ["runtime"] = positiveNegative.RuntimeSummary,
                ["weightsSha256"] = positiveNegative.WeightsSha256,
                ["partialBoxMaskArea"] = partialBox.MaskArea,
                ["positiveExpandedMaskArea"] = positiveExpanded.MaskArea,
                ["positiveExpandedBounds"] = new JObject
                {
                    ["x"] = positiveBounds.X,
                    ["y"] = positiveBounds.Y,
                    ["width"] = positiveBounds.Width,
                    ["height"] = positiveBounds.Height
                },
                ["positiveWideMaskArea"] = positiveWide.MaskArea,
                ["positiveNegativeMaskArea"] = positiveNegative.MaskArea,
                ["negativeAreaReductionRatio"] = 1D - positiveNegative.MaskArea / (double)positiveWide.MaskArea,
                ["fastPolygonPointCount"] = positiveNegative.Candidate.PolygonPoints.Count,
                ["boundary"] = "Synthetic workflow acceptance only; no field or production accuracy claim."
            };
            string evidencePath = Path.Combine(runRoot, "point-correction-evidence.json");
            File.WriteAllText(evidencePath, evidence.ToString(Formatting.Indented));
            Console.WriteLine("REAL_MOBILE_SAM_POINT_EVIDENCE=" + evidencePath);
            Console.WriteLine("REAL_MOBILE_SAM_POSITIVE_AREA_GAIN=" + (positiveExpanded.MaskArea - partialBox.MaskArea));
            Console.WriteLine("REAL_MOBILE_SAM_NEGATIVE_AREA_REMOVED=" + (positiveWide.MaskArea - positiveNegative.MaskArea));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("REAL_MOBILE_SAM_POINT_FAILED=" + error);
            return 1;
        }
    }

    private static WpfMobileSamBoxPromptResult RunPointCorrectionTrial(
        WpfMobileSamBoxPromptService service,
        PythonModelSettings settings,
        string imagePath,
        Rectangle promptBounds,
        IReadOnlyList<WpfSmartMaskPromptPoint> points,
        int maximumPolygonPoints)
    {
        WpfMobileSamBoxPromptRequest request = service.BuildRequest(
            settings,
            imagePath,
            promptBounds,
            classId: 0,
            className: "desired_object",
            promptPoints: points,
            maximumPolygonPoints: maximumPolygonPoints);
        AssertTrue(request.IsValid, string.Join(" ", request.Errors));
        WpfMobileSamBoxPromptResult result = service.RunAsync(request).GetAwaiter().GetResult();
        AssertTrue(result.Succeeded, result.Error);
        AssertTrue(result.Candidate?.PolygonPoints?.Count >= 3, "real point-correction result should contain a review polygon");
        return result;
    }

    internal static int RunRealMobileSamBoxPrompt(string[] args)
    {
        try
        {
            string imagePath = Path.GetFullPath(GetArgumentValue(args, "--image", string.Empty));
            AssertTrue(File.Exists(imagePath), "--image must point to a real prompt image");
            Size imageSize = GetImageSize(imagePath);
            AssertTrue(
                TryParsePromptBox(GetArgumentValue(args, "--prompt-box", string.Empty), imageSize, out Rectangle promptBounds),
                "--prompt-box must be x,y,width,height inside the image");
            string evidenceRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(FindRepositoryRoot(), "artifacts", "mobile-sam-box-prompt")));
            string runRoot = Path.Combine(evidenceRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(runRoot);

            string sourceHashBefore = ComputeFileSha256(imagePath);
            var service = new WpfMobileSamBoxPromptService();
            var settings = new PythonModelSettings();
            settings.EnsureDefaults();
            WpfMobileSamBoxPromptRequest request = service.BuildRequest(
                settings,
                imagePath,
                promptBounds,
                classId: 0,
                className: "contamination_spot");
            AssertTrue(request.IsValid, string.Join(" ", request.Errors));
            WpfMobileSamBoxPromptResult result = service.RunAsync(request).GetAwaiter().GetResult();
            AssertTrue(result.Succeeded, result.Error);
            AssertTrue(result.Candidate?.PolygonPoints?.Count >= 3, "real MobileSAM result should contain a polygon");

            if (System.Windows.Application.Current == null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };
            }

            CData previousData = CGlobal.Inst.Data;
            var data = new CData();
            data.ConfigureOutputRoot(runRoot);
            data.LastSelectImageName = Path.GetFileNameWithoutExtension(imagePath);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Add(new CClassItem { Text = "contamination_spot", DrawColor = Color.LimeGreen });
            CGlobal.Inst.Data = data;

            WpfLabelingShellWindow window = new WpfLabelingShellWindow();
            Bitmap bitmap = new Bitmap(imagePath);
            try
            {
                SetPrivateField(window, "activeImagePath", imagePath);
                SetPrivateField(window, "activeImageSize", bitmap.Size);
                SetPrivateField(window, "activeImageBitmap", bitmap);
                SetPrivateField(window.MainCanvasViewModel, "_imageSize", bitmap.Size);
                InvokePrivateResult<object>(
                    window,
                    "ApplyDetectionCandidatesPreservingConfirmed",
                    new List<YoloWorkerSmokeCandidate> { result.Candidate },
                    true);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                window.CandidateReviewViewModel.ConfirmSelectedCommand.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(200));

                WpfCandidateReviewStateService candidateState = GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
                AssertEqual(0, candidateState.PendingCount);
                AssertEqual(1, candidateState.ConfirmedCount);
                string segmentPath = Directory.EnumerateFiles(Path.Combine(runRoot, "data", "train", "segments"), "*.json").Single();
                string maskPath = Directory.EnumerateFiles(Path.Combine(runRoot, "data", "train", "masks"), "*.png").Single();
                AssertTrue(CountSavedMaskPixels(maskPath) > 0, "confirmed real MobileSAM mask should contain foreground pixels");

                string sourceHashAfter = ComputeFileSha256(imagePath);
                AssertEqual(sourceHashBefore, sourceHashAfter);
                var evidence = new JObject
                {
                    ["status"] = "Complete",
                    ["scope"] = "MobileSAM box prompt to confirmed canonical segmentation label",
                    ["evidenceOrigin"] = "synthetic",
                    ["fieldValidation"] = "Not evaluated",
                    ["sourceImagePath"] = imagePath,
                    ["sourceImageSha256Before"] = sourceHashBefore,
                    ["sourceImageSha256After"] = sourceHashAfter,
                    ["promptBox"] = JArray.FromObject(new[] { promptBounds.X, promptBounds.Y, promptBounds.Width, promptBounds.Height }),
                    ["weightsPath"] = request.WeightsPath,
                    ["weightsSha256"] = result.WeightsSha256,
                    ["runtime"] = result.RuntimeSummary,
                    ["elapsedMs"] = result.ElapsedMilliseconds,
                    ["polygonPointCount"] = result.Candidate.PolygonPoints.Count,
                    ["maskArea"] = result.MaskArea,
                    ["segmentPath"] = segmentPath,
                    ["maskPath"] = maskPath,
                    ["boundary"] = "Synthetic labeling workflow evidence only; no production accuracy claim."
                };
                string evidencePath = Path.Combine(runRoot, "mobile-sam-evidence.json");
                File.WriteAllText(evidencePath, evidence.ToString(Formatting.Indented));
                Console.WriteLine("REAL_MOBILE_SAM_EVIDENCE=" + evidencePath);
                Console.WriteLine("REAL_MOBILE_SAM_SEGMENT=" + segmentPath);
                Console.WriteLine("REAL_MOBILE_SAM_MASK=" + maskPath);
                Console.WriteLine("REAL_MOBILE_SAM_POINTS=" + result.Candidate.PolygonPoints.Count);
                Console.WriteLine("REAL_MOBILE_SAM_ELAPSED_MS=" + result.ElapsedMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                return 0;
            }
            finally
            {
                SetPrivateField(window, "activeImageBitmap", null);
                window.Close();
                bitmap.Dispose();
                CGlobal.Inst.Data = previousData;
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("REAL_MOBILE_SAM_FAILED=" + error);
            return 1;
        }
    }

    internal static void TestMobileSamBoxPromptContract()
    {
        string root = FindRepositoryRoot();
        string workerPath = Path.Combine(root, "Runtime", "Python", "openvisionlab_mobile_sam_box_prompt.py");
        AssertTrue(File.Exists(workerPath), "MobileSAM box-prompt worker should exist");
        string worker = File.ReadAllText(workerPath);
        AssertTrue(worker.Contains("from ultralytics import SAM", StringComparison.Ordinal), "MobileSAM worker should use the existing Ultralytics runtime");
        AssertTrue(worker.Contains("\"bboxes\": [left, top, right, bottom]", StringComparison.Ordinal), "MobileSAM worker should use an operator box prompt");
        AssertTrue(worker.Contains("\"points\"", StringComparison.Ordinal) && worker.Contains("\"labels\"", StringComparison.Ordinal), "MobileSAM worker should pass positive/negative points to the current Ultralytics API");
        AssertTrue(worker.Contains("--max-polygon-points", StringComparison.Ordinal), "MobileSAM worker should expose deterministic polygon detail selection");
        AssertTrue(worker.Contains("weightsSha256", StringComparison.Ordinal), "MobileSAM worker should report weight provenance");

        var service = new WpfMobileSamBoxPromptService();
        string relativeImagePath = Path.GetRelativePath(Environment.CurrentDirectory, workerPath);
        WpfMobileSamBoxPromptRequest normalizedRequest = service.BuildRequest(
            new PythonModelSettings(),
            relativeImagePath,
            new Rectangle(1, 1, 2, 2),
            classId: 0,
            className: "Defect");
        AssertEqual(Path.GetFullPath(relativeImagePath), normalizedRequest.ImagePath);
        var request = new WpfMobileSamBoxPromptRequest
        {
            ImagePath = "fixture.png",
            PromptBounds = new Rectangle(10, 20, 30, 40),
            ClassId = 2,
            ClassName = "scratch",
            PromptPoints = new[]
            {
                new WpfSmartMaskPromptPoint { Position = new Point(20, 30), Kind = WpfSmartMaskPointKind.Positive },
                new WpfSmartMaskPromptPoint { Position = new Point(35, 45), Kind = WpfSmartMaskPointKind.Negative }
            },
            MaximumPolygonPoints = 48
        };
        string payload = JsonConvert.SerializeObject(new
        {
            success = true,
            model = "MobileSAM",
            ultralyticsVersion = "8.4.101",
            torchVersion = "2.12.1+cpu",
            device = "cpu",
            weightsSha256 = new string('a', 64),
            elapsedMs = 1234.5,
            maskArea = 456,
            bounds = new { x = 11.0, y = 21.0, width = 27.0, height = 35.0 },
            polygon = new[]
            {
                new { x = 11.0, y = 21.0 },
                new { x = 38.0, y = 22.0 },
                new { x = 30.0, y = 56.0 },
                new { x = 12.0, y = 50.0 }
            }
        });
        WpfMobileSamBoxPromptResult result = service.ParseResult(0, payload, string.Empty, request);
        AssertTrue(result.Succeeded, "valid MobileSAM output should parse successfully");
        AssertEqual("smart-mask", result.Candidate.CandidateType);
        AssertEqual("polygon", result.Candidate.SegmentationType);
        AssertEqual("scratch", result.Candidate.ClassName);
        AssertEqual(4, result.Candidate.PolygonPoints.Count);
        AssertEqual(456, result.MaskArea);
        AssertTrue(result.RuntimeSummary.Contains("8.4.101", StringComparison.Ordinal), "MobileSAM result should preserve runtime provenance");
        AssertTrue(result.Summary.Contains("+1 -1", StringComparison.Ordinal), "MobileSAM result summary should disclose correction-point provenance");
        AssertEqual("Smart Mask 프롬프트", WpfCandidateReviewPresenter.FormatConfidence(result.Candidate, "P1"));

        var viewModel = new WpfCanvasPanelViewModel();
        bool invoked = false;
        bool positiveInvoked = false;
        bool negativeInvoked = false;
        bool undoInvoked = false;
        bool clearInvoked = false;
        bool cancelInvoked = false;
        bool nextInvoked = false;
        bool showInitialInvoked = false;
        bool showLatestInvoked = false;
        int autoContourChangeCount = 0;
        bool autoContourChangedValue = false;
        WpfSmartMaskPolygonDetail selectedDetail = WpfSmartMaskPolygonDetail.Balanced;
        viewModel.ConfigureSmartMaskCommands(
            () => invoked = true,
            () => positiveInvoked = true,
            () => negativeInvoked = true,
            () => undoInvoked = true,
            () => clearInvoked = true,
            () => cancelInvoked = true,
            () => nextInvoked = true,
            () => showInitialInvoked = true,
            () => showLatestInvoked = true,
            enabled =>
            {
                autoContourChangeCount++;
                autoContourChangedValue = enabled;
            },
            detail => selectedDetail = detail);
        viewModel.SetSmartMaskState(isVisible: true, isEnabled: false, isBusy: false, "draw a box", hasSession: false);
        AssertEqual(System.Windows.Visibility.Collapsed, viewModel.SmartMaskSessionActionVisibility);
        AssertTrue(viewModel.IsSmartMaskAutoContourToggleEnabled, "automatic contour mode should be selectable before drawing a prompt box");
        viewModel.ToggleSmartMaskAutoContourCommand.Execute(null);
        AssertTrue(
            viewModel.IsSmartMaskAutoContourEnabled
                && autoContourChangeCount == 1
                && autoContourChangedValue,
            "automatic contour mode should cross the ViewModel command boundary and remain selected for repeated boxes");
        viewModel.RestoreSmartMaskAutoContourMode(false);
        AssertTrue(
            !viewModel.IsSmartMaskAutoContourEnabled && autoContourChangeCount == 1,
            "restoring a recipe option should not execute the automatic-contour action");
        viewModel.RestoreSmartMaskAutoContourMode(true);
        viewModel.SetSmartMaskState(isVisible: true, isEnabled: true, isBusy: false, "candidate only", hasSession: true);
        viewModel.SetSmartMaskSessionState(
            isVisible: true,
            isBusy: false,
            positivePointCount: 0,
            negativePointCount: 0,
            WpfSmartMaskPointInputMode.Positive,
            hasProducedCandidate: true,
            canMoveToNextInstance: true);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskVisibility);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskSessionActionVisibility);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskSessionVisibility);
        AssertTrue(viewModel.IsSmartMaskEnabled, "smart-mask command should be enabled only when its prompt/runtime gates pass");
        AssertTrue(!viewModel.IsSmartMaskAutoContourToggleEnabled, "automatic contour mode should stay locked while one object session is active");
        AssertEqual("후보 다시 생성", viewModel.SmartMaskActionText);
        AssertEqual(System.Windows.Visibility.Collapsed, viewModel.SmartMaskCorrectionOptionsVisibility);
        AssertEqual(System.Windows.Visibility.Collapsed, viewModel.SmartMaskCandidateComparisonVisibility);
        AssertEqual("보정 옵션", viewModel.SmartMaskCorrectionOptionsText);
        AssertTrue(
            viewModel.SmartMaskPromptSummaryText.Contains("필요할 때만 보정", StringComparison.Ordinal),
            "automatic Smart Mask candidates should lead with optional correction guidance");
        viewModel.ToggleSmartMaskCorrectionOptionsCommand.Execute(null);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskCorrectionOptionsVisibility);
        AssertEqual("보정 닫기", viewModel.SmartMaskCorrectionOptionsText);
        viewModel.SetSmartMaskSessionState(
            isVisible: true,
            isBusy: false,
            positivePointCount: 1,
            negativePointCount: 1,
            WpfSmartMaskPointInputMode.Positive,
            hasProducedCandidate: true,
            canMoveToNextInstance: true,
            hasCandidateComparison: true,
            selectedCandidateVersion: WpfSmartMaskCandidateVersion.Latest);
        AssertTrue(
            viewModel.SmartMaskPromptSummaryText.Contains("한 점씩", StringComparison.Ordinal),
            "Smart Mask correction guidance should encourage incremental point-by-point comparison");
        AssertTrue(viewModel.IsPositiveSmartMaskPointMode && !viewModel.IsNegativeSmartMaskPointMode, "prompt mode should be explicit in the ViewModel");
        AssertTrue(viewModel.IsSmartMaskPointUndoEnabled && viewModel.IsSmartMaskNextInstanceEnabled, "point undo and next-instance enablement should be deterministic");
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskCandidateComparisonVisibility);
        AssertTrue(viewModel.IsShowInitialSmartMaskCandidateEnabled && !viewModel.IsShowLatestSmartMaskCandidateEnabled, "latest candidate selection should expose only the previous candidate action");
        AssertTrue(viewModel.SmartMaskCandidateComparisonText.Contains("이 후보만 저장", StringComparison.Ordinal), "candidate comparison should disclose explicit-save ownership");
        viewModel.CreateSmartMaskCommand.Execute(null);
        viewModel.AddPositiveSmartMaskPointCommand.Execute(null);
        viewModel.AddNegativeSmartMaskPointCommand.Execute(null);
        viewModel.UndoSmartMaskPointCommand.Execute(null);
        viewModel.ClearSmartMaskPointsCommand.Execute(null);
        viewModel.SetSmartMaskSessionState(
            isVisible: true,
            isBusy: true,
            positivePointCount: 1,
            negativePointCount: 1,
            WpfSmartMaskPointInputMode.Positive,
            hasProducedCandidate: true,
            canMoveToNextInstance: false);
        AssertTrue(
            viewModel.SmartMaskCorrectionOptionsVisibility == System.Windows.Visibility.Visible,
            "rerunning the same Smart Mask session should preserve an explicitly expanded correction panel");
        AssertTrue(viewModel.IsSmartMaskCancelEnabled, "running Smart Mask inference should expose deterministic cancellation");
        viewModel.CancelSmartMaskGenerationCommand.Execute(null);
        viewModel.NextSmartMaskInstanceCommand.Execute(null);
        viewModel.ShowInitialSmartMaskCandidateCommand.Execute(null);
        viewModel.ShowLatestSmartMaskCandidateCommand.Execute(null);
        viewModel.SelectedSmartMaskDetail = viewModel.SmartMaskDetails.Single(item => item.Detail == WpfSmartMaskPolygonDetail.Detailed);
        AssertTrue(invoked, "smart-mask command should cross the ViewModel command boundary");
        AssertTrue(
            positiveInvoked
                && negativeInvoked
                && undoInvoked
                && clearInvoked
                && cancelInvoked
                && nextInvoked
                && showInitialInvoked
                && showLatestInvoked
                && autoContourChangeCount == 1,
            "all Smart Mask session actions should cross the ViewModel command boundary");
        AssertEqual(WpfSmartMaskPolygonDetail.Detailed, selectedDetail);
        viewModel.SetSmartMaskState(isVisible: true, isEnabled: true, isBusy: true, "running");
        AssertTrue(!viewModel.IsSmartMaskEnabled, "smart-mask command should disable while inference is running");
        viewModel.SetSmartMaskSessionState(
            isVisible: false,
            isBusy: false,
            positivePointCount: 0,
            negativePointCount: 0,
            WpfSmartMaskPointInputMode.None,
            hasProducedCandidate: false,
            canMoveToNextInstance: false);
        AssertTrue(
            viewModel.SmartMaskCorrectionOptionsVisibility == System.Windows.Visibility.Collapsed,
            "ending a Smart Mask session should restore auto-first collapsed correction options");

        var session = new WpfSmartMaskPromptSessionService();
        WpfSmartMaskPromptSnapshot initial = session.Start(
            "fixture.png",
            "recipe-a",
            new Rectangle(10, 20, 30, 40),
            2,
            "scratch");
        AssertTrue(session.Matches(initial, "fixture.png", "recipe-a"), "new prompt session should match its image and recipe identity");
        session.SetInputMode(WpfSmartMaskPointInputMode.Positive);
        AssertTrue(session.TryAddPoint(new Point(20, 30), new Size(100, 100)), "positive point should be accepted inside the image");
        WpfSmartMaskPromptSnapshot positive = session.Capture();
        AssertEqual(1, positive.Points.Count);
        AssertEqual(1, positive.Points[0].Label);
        AssertTrue(!session.Matches(initial, "fixture.png", "recipe-a"), "adding a point should invalidate an in-flight older generation");
        session.SetInputMode(WpfSmartMaskPointInputMode.Negative);
        AssertTrue(session.TryAddPoint(new Point(35, 45), new Size(100, 100)), "negative point should be accepted inside the image");
        AssertEqual(1, session.PositivePointCount);
        AssertEqual(1, session.NegativePointCount);
        AssertTrue(session.UndoPoint(), "point undo should remove only the most recent point");
        AssertEqual(1, session.PositivePointCount);
        AssertEqual(0, session.NegativePointCount);
        session.SetPolygonDetail(WpfSmartMaskPolygonDetail.Detailed);
        AssertEqual(256, session.MaximumPolygonPoints);
        AssertTrue(session.ClearPoints(), "clear should remove all correction points");
        AssertEqual(0, session.Points.Count);
        var initialCandidate = new YoloWorkerSmokeCandidate { Index = 1, CandidateType = "smart-mask" };
        var latestCandidate = new YoloWorkerSmokeCandidate { Index = 2, CandidateType = "smart-mask" };
        AssertTrue(session.RecordCandidate(initialCandidate), "first Smart Mask candidate should enter session-only history");
        AssertTrue(!session.HasCandidateComparison, "one Smart Mask candidate should not expose comparison");
        AssertTrue(session.RecordCandidate(latestCandidate), "latest Smart Mask candidate should enter session-only history");
        AssertTrue(session.HasCandidateComparison, "a rerun should expose initial/latest comparison");
        AssertEqual(WpfSmartMaskCandidateVersion.Latest, session.SelectedCandidateVersion);
        AssertTrue(
            session.TrySelectCandidate(WpfSmartMaskCandidateVersion.Initial, out YoloWorkerSmokeCandidate restoredCandidate)
                && ReferenceEquals(initialCandidate, restoredCandidate),
            "initial Smart Mask candidate should be explicitly restorable");
        AssertTrue(session.IsSelectedCandidate(initialCandidate), "restored initial candidate should become the selected pending version");
        var candidateState = new WpfCandidateReviewStateService();
        candidateState.LoadPendingCandidates(new[] { restoredCandidate }, clearConfirmed: false);
        WpfCandidateConfirmationPlan selectedPlan = candidateState.BuildConfirmationPlan(
            new[] { restoredCandidate },
            _ => true,
            _ => false);
        candidateState.ApplyConfirmation(selectedPlan.ConfirmableCandidates);
        AssertTrue(
            candidateState.ConfirmedCount == 1
                && ReferenceEquals(candidateState.ConfirmedCandidates[0], initialCandidate)
                && !candidateState.ConfirmedCandidates.Contains(latestCandidate),
            "Candidate Review confirmation should persist only the explicitly restored Smart Mask version");
        AssertTrue(session.MatchesContext("fixture.png", "recipe-a"), "session candidate history should match its image and recipe context");
        session.MarkCandidateResolved();
        AssertTrue(!session.HasCandidateComparison, "confirm/skip resolution should clear session candidate comparison");
        AssertTrue(
            !session.TrySelectCandidate(WpfSmartMaskCandidateVersion.Latest, out _),
            "resolved Smart Mask candidate history should not be restorable");
        AssertTrue(!session.Matches(session.Capture(), "other.png", "recipe-a"), "image changes should fail the stale-result guard");
        AssertTrue(!session.Matches(session.Capture(), "fixture.png", "recipe-b"), "recipe changes should fail the stale-result guard");

        string shellSource = ReadWpfLabelingShellWindowSources();
        AssertTrue(shellSource.Contains("clearConfirmed: false", StringComparison.Ordinal), "smart-mask assist should preserve already confirmed candidates");
        AssertTrue(shellSource.Contains("manualRois[currentPromptIndex] != promptBounds", StringComparison.Ordinal), "smart-mask result should compare the current rectangle with the requested prompt bounds");
        AssertTrue(shellSource.Contains("프롬프트 박스가 변경되어 후보를 적용하지 않았습니다", StringComparison.Ordinal), "smart-mask result should fail closed when its prompt changes");
        AssertTrue(shellSource.Contains("smartMaskPromptSession.Matches", StringComparison.Ordinal), "smart-mask result should reject stale image, recipe, or prompt generations");
        AssertTrue(shellSource.Contains("ApplyDetectionCandidatesPreservingConfirmed(new[] { result.Candidate }", StringComparison.Ordinal), "rerun should replace the one pending candidate instead of accumulating candidates");
        AssertTrue(shellSource.Contains("ExecuteSelectSmartMaskCandidateVersionCommand", StringComparison.Ordinal), "Smart Mask comparison commands should switch the one visible pending candidate");
        AssertTrue(shellSource.Contains("MarkCandidateResolved", StringComparison.Ordinal), "confirm/skip should close Smart Mask candidate comparison history");
        string canvasXaml = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfCanvasPanel.xaml"));
        AssertTrue(
            canvasXaml.Contains("CanvasSmartMaskShowInitialCandidateButton", StringComparison.Ordinal)
                && canvasXaml.Contains("CanvasSmartMaskShowLatestCandidateButton", StringComparison.Ordinal),
            "Smart Mask comparison should expose contextual previous/current candidate controls");
        AssertTrue(
            canvasXaml.Contains("CanvasSmartMaskLabelingOptions", StringComparison.Ordinal)
                && canvasXaml.Contains("CanvasSmartMaskAutoContourToggle", StringComparison.Ordinal)
                && canvasXaml.Contains("SmartMaskSessionActionVisibility", StringComparison.Ordinal),
            "segmentation labeling options should expose one persistent automatic-contour mode and reserve the action button for active correction");
        AssertTrue(
            shellSource.Contains("TryStartAutoSmartMaskForNewRoi(e.RoiRect)", StringComparison.Ordinal)
                && shellSource.Contains("ContinueAutoSmartMaskAfterResolvedCandidate", StringComparison.Ordinal),
            "a new rectangle should start automatic contour inference and resolved candidates should return to the next-box loop");
        AssertTrue(shellSource.Contains("MainCanvasViewModel.IsTeachingMode =", StringComparison.Ordinal)
            && shellSource.Contains("activeAnnotationTool == WpfAnnotationTool.Rectangle", StringComparison.Ordinal),
            "ending a Smart Mask session should restore box teaching for the next instance");
        TestSmartMaskAutoContourRecipePersistence();
        MobileSamUsabilityMatrixTests.TestMobileSamUsabilityMetric();
    }

    private static void TestSmartMaskAutoContourRecipePersistence()
    {
        string recipeName = "codex_smart_mask_auto_contour_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        string outputRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            recipeName);
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.SmartMaskAutoContourEnabled = true;
            data.SaveConfig(recipeName, refreshDatasetVersion: false);

            string configPath = Path.Combine(recipeDirectory, "VISION.xml");
            AssertTrue(File.Exists(configPath), "automatic-contour recipe config should be saved");
            AssertTrue(
                File.ReadAllText(configPath).Contains(
                    "<SmartMaskAutoContourEnabled>true</SmartMaskAutoContourEnabled>",
                    StringComparison.OrdinalIgnoreCase),
                "automatic-contour recipe option should be serialized explicitly");

            CData loaded = new CData().LoadConfig(recipeName);
            AssertTrue(
                loaded.ProjectSettings.SmartMaskAutoContourEnabled,
                "automatic-contour recipe option should survive save and reload");
            AssertTrue(
                !new LabelingProjectSettings().SmartMaskAutoContourEnabled,
                "unconfigured recipes should retain the safe regular-box default");
        }
        finally
        {
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    internal static void ApplyVisualSmokeSmartMaskCandidate(
        WpfLabelingShellWindow window,
        Size imageSize,
        string promptBoxText,
        string positivePointText = "",
        string negativePointText = "")
    {
        ApplyVisualSmokeSmartMaskPrompt(window, imageSize, promptBoxText);
        InvokePrivateResult<object>(window, "ExecuteCreateSmartMaskCandidateCommand");
        DateTime deadline = DateTime.UtcNow.AddSeconds(50);
        while (DateTime.UtcNow < deadline)
        {
            PumpWpfDispatcher(TimeSpan.FromMilliseconds(250));
            WpfCandidateReviewStateService state = GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
            if (state.PendingCandidates.Any(candidate =>
                    string.Equals(candidate.CandidateType, "smart-mask", StringComparison.OrdinalIgnoreCase)))
            {
                ApplyVisualSmokeSmartMaskPoints(window, imageSize, positivePointText, negativePointText);
                return;
            }
            if (!string.Equals(window.CanvasPanelViewModel.SmartMaskActionText, "마스크 생성 중...", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "smart-mask visual smoke stopped without a candidate: "
                    + window.CanvasPanelViewModel.SmartMaskToolTip);
            }
        }

        throw new InvalidOperationException("smart-mask visual smoke did not produce a candidate within 50 seconds");
    }

    internal static void ApplyVisualSmokeSmartMaskCandidateComparison(
        WpfLabelingShellWindow window,
        Size imageSize,
        string promptBoxText)
    {
        ApplyVisualSmokeSmartMaskPrompt(window, imageSize, promptBoxText);
        List<Rectangle> prompts = GetPrivateField<List<Rectangle>>(window, "manualRois");
        int promptIndex = prompts.Count - 1;
        Rectangle promptBounds = prompts[promptIndex];
        string imagePath = GetPrivateField<string>(window, "activeImagePath");
        string recipeName = InvokePrivateResult<string>(window, "GetCurrentSmartMaskRecipeName");
        var session = GetPrivateField<WpfSmartMaskPromptSessionService>(window, "smartMaskPromptSession");
        session.Start(imagePath, recipeName, promptBounds, 0, "Defect");

        var initialCandidate = CreateVisualSmokeSmartMaskCandidate(promptBounds, inset: 0, index: 1);
        var latestCandidate = CreateVisualSmokeSmartMaskCandidate(promptBounds, inset: 8, index: 2);
        AssertTrue(session.RecordCandidate(initialCandidate), "visual comparison should retain its initial candidate");
        AssertTrue(session.RecordCandidate(latestCandidate), "visual comparison should retain its latest candidate");

        prompts.RemoveAt(promptIndex);
        RemoveVisualSmokePromptMetadata(GetPrivateField<List<string>>(window, "manualRoiClassNames"), promptIndex);
        RemoveVisualSmokePromptMetadata(GetPrivateField<List<CanvasRoiShapeKind>>(window, "manualRoiShapeKinds"), promptIndex);
        RemoveVisualSmokePromptMetadata(GetPrivateField<List<string>>(window, "manualRoiOverlayIds"), promptIndex);

        InvokePrivateResult<object>(
            window,
            "ApplyDetectionCandidatesPreservingConfirmed",
            new[] { latestCandidate },
            true);
        InvokePrivateResult<object>(
            window,
            "RefreshSmartMaskCommandState",
            "보정 전후 후보를 비교하고 확정할 하나를 선택하세요.");
        PumpWpfDispatcher(TimeSpan.FromMilliseconds(250));
    }

    private static void RemoveVisualSmokePromptMetadata<T>(IList<T> items, int index)
    {
        if (items != null && index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }

    private static YoloWorkerSmokeCandidate CreateVisualSmokeSmartMaskCandidate(
        Rectangle promptBounds,
        int inset,
        int index)
    {
        int left = promptBounds.Left + inset;
        int top = promptBounds.Top + inset;
        int right = Math.Max(left + 3, promptBounds.Right - inset);
        int bottom = Math.Max(top + 3, promptBounds.Bottom - inset);
        return new YoloWorkerSmokeCandidate
        {
            Index = index,
            ClassId = 0,
            ClassName = "Defect",
            Confidence = 1D,
            X = left,
            Y = top,
            Width = right - left,
            Height = bottom - top,
            CandidateType = "smart-mask",
            PredictionType = "segmentation-assist",
            SegmentationType = "polygon",
            PolygonPoints = new[]
            {
                new DetectionPolygonPoint { X = left, Y = top + ((bottom - top) * 0.2F) },
                new DetectionPolygonPoint { X = left + ((right - left) * 0.18F), Y = top },
                new DetectionPolygonPoint { X = right - ((right - left) * 0.12F), Y = top + ((bottom - top) * 0.08F) },
                new DetectionPolygonPoint { X = right, Y = top + ((bottom - top) * 0.48F) },
                new DetectionPolygonPoint { X = right - ((right - left) * 0.16F), Y = bottom },
                new DetectionPolygonPoint { X = left + ((right - left) * 0.22F), Y = bottom - ((bottom - top) * 0.06F) }
            }
        };
    }

    private static void ApplyVisualSmokeSmartMaskPoints(
        WpfLabelingShellWindow window,
        Size imageSize,
        string positivePointText,
        string negativePointText)
    {
        WpfSmartMaskPromptSessionService session =
            GetPrivateField<WpfSmartMaskPromptSessionService>(window, "smartMaskPromptSession");
        if (TryParsePromptPoint(positivePointText, imageSize, out Point positivePoint))
        {
            session.SetInputMode(WpfSmartMaskPointInputMode.Positive);
            AssertTrue(session.TryAddPoint(positivePoint, imageSize), "visual smoke positive point should be accepted");
        }
        if (TryParsePromptPoint(negativePointText, imageSize, out Point negativePoint))
        {
            session.SetInputMode(WpfSmartMaskPointInputMode.Negative);
            AssertTrue(session.TryAddPoint(negativePoint, imageSize), "visual smoke negative point should be accepted");
        }
        if (!string.IsNullOrWhiteSpace(positivePointText) || !string.IsNullOrWhiteSpace(negativePointText))
        {
            InvokePrivate(window, "RefreshPolygonOverlays");
            InvokePrivateResult<object>(window, "RefreshSmartMaskCommandState", "보정점 입력 완료 · 후보 다시 생성으로 반영");
        }
    }

    internal static void ApplyVisualSmokeSmartMaskPrompt(
        WpfLabelingShellWindow window,
        Size imageSize,
        string promptBoxText)
    {
        List<Rectangle> prompts = GetPrivateField<List<Rectangle>>(window, "manualRois");
        if (prompts.Count == 0)
        {
            throw new InvalidOperationException("smart-mask visual smoke requires a rectangle prompt");
        }

        if (TryParsePromptBox(promptBoxText, imageSize, out Rectangle promptBounds))
        {
            prompts[prompts.Count - 1] = promptBounds;
            InvokePrivate(window, "RedrawReviewRois");
            InvokePrivate(window, "RefreshObjectList");
            InvokePrivateResult<object>(
                window,
                "SetModelStatus",
                $"스마트 마스크 프롬프트: {WpfCandidateReviewPresenter.FormatBoundsCompact(promptBounds)}");
        }
        InvokePrivateResult<object>(window, "RefreshSmartMaskCommandState", string.Empty);
    }

    private static bool TryParsePromptBox(string value, Size imageSize, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        string[] parts = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4
            || !int.TryParse(parts[0], out int x)
            || !int.TryParse(parts[1], out int y)
            || !int.TryParse(parts[2], out int width)
            || !int.TryParse(parts[3], out int height))
        {
            return false;
        }

        bounds = Rectangle.Intersect(
            new Rectangle(x, y, width, height),
            new Rectangle(Point.Empty, imageSize));
        return !bounds.IsEmpty;
    }

    private static bool TryParsePromptPoint(string value, Size imageSize, out Point point)
    {
        point = Point.Empty;
        string[] parts = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out int x)
            || !int.TryParse(parts[1], out int y)
            || x < 0
            || y < 0
            || x >= imageSize.Width
            || y >= imageSize.Height)
        {
            return false;
        }

        point = new Point(x, y);
        return true;
    }
}
