using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class MobileSamBoxPromptTests
{
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
        WpfSmartMaskPolygonDetail selectedDetail = WpfSmartMaskPolygonDetail.Balanced;
        viewModel.ConfigureSmartMaskCommands(
            () => invoked = true,
            () => positiveInvoked = true,
            () => negativeInvoked = true,
            () => undoInvoked = true,
            () => clearInvoked = true,
            () => cancelInvoked = true,
            () => nextInvoked = true,
            detail => selectedDetail = detail);
        viewModel.SetSmartMaskState(isVisible: true, isEnabled: true, isBusy: false, "candidate only", hasSession: true);
        viewModel.SetSmartMaskSessionState(
            isVisible: true,
            isBusy: false,
            positivePointCount: 1,
            negativePointCount: 1,
            WpfSmartMaskPointInputMode.Positive,
            canMoveToNextInstance: true);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskVisibility);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.SmartMaskSessionVisibility);
        AssertTrue(viewModel.IsSmartMaskEnabled, "smart-mask command should be enabled only when its prompt/runtime gates pass");
        AssertEqual("후보 다시 생성", viewModel.SmartMaskActionText);
        AssertTrue(viewModel.IsPositiveSmartMaskPointMode && !viewModel.IsNegativeSmartMaskPointMode, "prompt mode should be explicit in the ViewModel");
        AssertTrue(viewModel.IsSmartMaskPointUndoEnabled && viewModel.IsSmartMaskNextInstanceEnabled, "point undo and next-instance enablement should be deterministic");
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
            canMoveToNextInstance: false);
        AssertTrue(viewModel.IsSmartMaskCancelEnabled, "running Smart Mask inference should expose deterministic cancellation");
        viewModel.CancelSmartMaskGenerationCommand.Execute(null);
        viewModel.NextSmartMaskInstanceCommand.Execute(null);
        viewModel.SelectedSmartMaskDetail = viewModel.SmartMaskDetails.Single(item => item.Detail == WpfSmartMaskPolygonDetail.Detailed);
        AssertTrue(invoked, "smart-mask command should cross the ViewModel command boundary");
        AssertTrue(positiveInvoked && negativeInvoked && undoInvoked && clearInvoked && cancelInvoked && nextInvoked, "all Smart Mask session actions should cross the ViewModel command boundary");
        AssertEqual(WpfSmartMaskPolygonDetail.Detailed, selectedDetail);
        viewModel.SetSmartMaskState(isVisible: true, isEnabled: true, isBusy: true, "running");
        AssertTrue(!viewModel.IsSmartMaskEnabled, "smart-mask command should disable while inference is running");

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
        AssertTrue(!session.Matches(session.Capture(), "other.png", "recipe-a"), "image changes should fail the stale-result guard");
        AssertTrue(!session.Matches(session.Capture(), "fixture.png", "recipe-b"), "recipe changes should fail the stale-result guard");

        string shellSource = ReadWpfLabelingShellWindowSources();
        AssertTrue(shellSource.Contains("clearConfirmed: false", StringComparison.Ordinal), "smart-mask assist should preserve already confirmed candidates");
        AssertTrue(shellSource.Contains("manualRois[currentPromptIndex] != promptBounds", StringComparison.Ordinal), "smart-mask result should compare the current rectangle with the requested prompt bounds");
        AssertTrue(shellSource.Contains("프롬프트 박스가 변경되어 후보를 적용하지 않았습니다", StringComparison.Ordinal), "smart-mask result should fail closed when its prompt changes");
        AssertTrue(shellSource.Contains("smartMaskPromptSession.Matches", StringComparison.Ordinal), "smart-mask result should reject stale image, recipe, or prompt generations");
        AssertTrue(shellSource.Contains("ApplyDetectionCandidatesPreservingConfirmed(new[] { result.Candidate }", StringComparison.Ordinal), "rerun should replace the one pending candidate instead of accumulating candidates");
        AssertTrue(shellSource.Contains("MainCanvasViewModel.IsTeachingMode =", StringComparison.Ordinal)
            && shellSource.Contains("activeAnnotationTool == WpfAnnotationTool.Rectangle", StringComparison.Ordinal),
            "ending a Smart Mask session should restore box teaching for the next instance");
        MobileSamUsabilityMatrixTests.TestMobileSamUsabilityMetric();
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
