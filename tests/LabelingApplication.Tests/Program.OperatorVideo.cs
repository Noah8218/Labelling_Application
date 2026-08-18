using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace LabelingApplication.Tests;

using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using static TestSupport;

internal static partial class Program
{
    private const string OperatorVideoDefectStem = "kos14_Part7";

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out OperatorVideoNativePoint point);

    private static void TestSmartMaskAutoBoundaryPresentation()
    {
        var candidate = new YoloWorkerSmokeCandidate
        {
            Index = 1,
            ClassName = "Defect",
            CandidateType = "smart-mask",
            PredictionType = "segmentation-assist",
            SegmentationType = "polygon",
            PolygonPoints = Enumerable.Range(0, 96)
                .Select(index => new DetectionPolygonPoint
                {
                    X = 100F + index,
                    Y = 200F + (index % 7)
                })
                .ToArray()
        };
        string secondary = WpfCandidateReviewPresenter.BuildSecondaryText(
            candidate,
            new Rectangle(100, 200, 96, 20),
            new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D),
            minimumConfidence: 0.25F);
        AssertTrue(
            secondary.Contains("\uC790\uB3D9 \uACBD\uACC4 96\uC810", StringComparison.Ordinal),
            "Smart Mask candidate should expose its automatic boundary point count");

        var maskOverlay = new OpenVisionLab.ImageCanvas.ViewModels.RoiImageCanvasMaskOverlay(
            "smart-mask-test",
            Enumerable.Repeat((byte)255, 16).ToArray(),
            new Size(4, 4),
            new Rectangle(0, 0, 4, 4),
            Color.DeepSkyBlue,
            0.4F,
            renderVersion: 1,
            showMarker: false);
        AssertTrue(maskOverlay.IsValid, "Smart Mask candidate fill overlay should be valid");
        AssertTrue(!maskOverlay.ShowMarker, "Smart Mask candidate fill should suppress a duplicate mask badge");

        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.AnnotationPolygonOverlays.cs"));
        AssertTrue(
            source.Contains("AppendPendingSmartMaskCandidateMask", StringComparison.Ordinal)
                && source.Contains("WpfSegmentationMaskGeometryService.TryRasterize", StringComparison.Ordinal)
                && source.Contains("showMarker: false", StringComparison.Ordinal),
            "Smart Mask pending presentation should rasterize the selected contour into a badge-free fill");
    }

    private static int RunExeOperatorVideoSmoke(string[] args)
    {
        string recipeName = "operator_defect_demo_" + Guid.NewGuid().ToString("N");
        try
        {
            string repositoryRoot = FindRepositoryRoot();
            string exePath = Path.GetFullPath(GetArgumentValue(
                args,
                "--exe",
                Path.Combine(repositoryRoot, "artifacts", "run", "Debug", "OpenVisionLab.LabelingStudio.exe")));
            string runId = GetArgumentValue(
                args,
                "--run-id",
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            string outputDirectory = Path.GetFullPath(GetArgumentValue(
                args,
                "--output-dir",
                Path.Combine(repositoryRoot, "artifacts", "operator-video", runId)));
            string ffmpegPath = ResolveOperatorVideoExecutable(
                GetArgumentValue(args, "--ffmpeg", "ffmpeg"));
            string ffprobePath = ResolveOperatorVideoExecutable(
                GetArgumentValue(args, "--ffprobe", "ffprobe"));
            bool verifyContextualCorrection = HasArgument(args, "--verify-contextual-correction");
            bool verifyCandidateRestore = HasArgument(args, "--verify-candidate-restore");
            bool verifyAutoContourMode = HasArgument(args, "--verify-auto-contour-mode");

            AssertTrue(File.Exists(exePath), "operator video EXE was not found; build the Debug app first");
            OperatorVideoDefectSource defect = ResolveOperatorVideoDefectSource(repositoryRoot);
            AssertTrue(File.Exists(defect.ImagePath), "Kolektor defect source image was not found");
            AssertTrue(File.Exists(defect.MaskPath), "Kolektor defect source mask was not found");
            AssertTrue(defect.MaskBounds.Width > 20 && defect.MaskBounds.Height > 4, "Kolektor defect mask did not contain a usable defect");

            string exeDirectory = Path.GetDirectoryName(exePath) ?? throw new InvalidOperationException("EXE directory was unavailable");
            string recipeDirectory = Path.Combine(exeDirectory, "RECIPE", recipeName);
            string dataDirectory = Path.Combine(exeDirectory, "DATA", recipeName);
            AssertTrue(!Directory.Exists(recipeDirectory) && !Directory.Exists(dataDirectory), "operator video temporary Recipe already existed");

            OperatorVideoPaths paths = PrepareOperatorVideoPaths(outputDirectory);
            OperatorVideoRunResult result = ExecuteExeOperatorVideoSmoke(
                exePath,
                ffmpegPath,
                recipeName,
                recipeDirectory,
                dataDirectory,
                defect,
                paths,
                verifyContextualCorrection || verifyCandidateRestore,
                verifyCandidateRestore,
                verifyAutoContourMode);
            FinalizeOperatorVideoEvidence(ffmpegPath, ffprobePath, paths, result);

            Console.WriteLine($"EXE_OPERATOR_VIDEO_MP4={paths.RawVideoPath}");
            Console.WriteLine($"EXE_OPERATOR_VIDEO_EVENTS={paths.EventLogPath}");
            Console.WriteLine($"EXE_OPERATOR_VIDEO_CONTACT_SHEET={paths.ContactSheetPath}");
            Console.WriteLine($"EXE_OPERATOR_VIDEO_REVIEW={paths.SelfEvaluationPath}");
            Console.WriteLine(
                $"PASS EXE operator video defect={OperatorVideoDefectStem} durationMs={result.DurationMilliseconds:F0} "
                + $"cursorMoves={result.CursorMoveCount} maskPixels={result.SavedArtifact.MaskPixels} "
                + $"polygons={result.SavedArtifact.SegmentPolygons} points={result.SavedArtifact.SegmentPoints} "
                + $"iou={result.MaskQuality.IntersectionOverUnion:F4} "
                + $"precision={result.MaskQuality.Precision:F4} recall={result.MaskQuality.Recall:F4} "
                + $"autoContour={result.AutoContourVerified} "
                + $"candidateRestore={result.CandidateRestoreVerified} reopen={result.ReopenVerified}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL EXE operator video smoke: " + error);
            return 1;
        }
    }

    private static OperatorVideoRunResult ExecuteExeOperatorVideoSmoke(
        string exePath,
        string ffmpegPath,
        string recipeName,
        string recipeDirectory,
        string dataDirectory,
        OperatorVideoDefectSource defect,
        OperatorVideoPaths paths,
        bool verifyContextualCorrection,
        bool verifyCandidateRestore,
        bool verifyAutoContourMode)
    {
        Process appProcess = null;
        OperatorVideoRecorder recorder = null;
        int cursorMoveCount = 0;
        var eventLog = new OperatorVideoEventLog(paths.EventLogPath);
        try
        {
            appProcess = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
            AssertTrue(appProcess != null, "operator video EXE process did not start");

            IntPtr handle = WaitForMainWindowHandle(appProcess, TimeSpan.FromSeconds(25));
            AssertTrue(handle != IntPtr.Zero, "operator video EXE window did not appear");
            PlaceExeSmokeWindowOnLeftmostMonitor(handle);
            BringNativeWindowToFront(handle);
            Thread.Sleep(500);

            System.Windows.Automation.AutomationElement root =
                System.Windows.Automation.AutomationElement.FromHandle(handle);
            AssertTrue(root != null, "operator video automation root was unavailable");
            CreateOperatorVideoRecipeThroughUi(
                appProcess,
                recipeName,
                dataDirectory,
                recipeDirectory);

            AssertTrue(
                SelectImageQueueItemBySearch(appProcess, OperatorVideoDefectStem, TimeSpan.FromSeconds(15)),
                "operator video could not select the intended Kolektor defect image");
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest =
                            RefreshAutomationRoot(appProcess, bringToFront: false);
                        return ContainsAutomationText(latest, OperatorVideoDefectStem);
                    },
                    TimeSpan.FromSeconds(8)),
                "operator video active image did not become the intended defect");

            root = RefreshAutomationRoot(appProcess);
            System.Windows.Automation.AutomationElement canvas =
                FindOperatorVideoVisibleCanvas(root);
            AssertTrue(canvas != null, "operator video canvas was not found");
            System.Windows.Rect canvasRect = BuildOperatorVideoCanvasViewport(root, canvas);
            string activeImagePath = Path.Combine(
                dataDirectory,
                "data",
                "train",
                "images",
                OperatorVideoDefectStem + ".jpg");
            AssertTrue(File.Exists(activeImagePath), "operator video active image path was unavailable");
            AssertTrue(
                string.Equals(
                    ComputeOperatorVideoSha256(defect.ImagePath),
                    ComputeOperatorVideoSha256(activeImagePath),
                    StringComparison.Ordinal),
                "operator video source copy did not match the selected Kolektor defect");
            System.Windows.Rect imageRegion = FindOperatorVideoDisplayedImageBounds(
                root,
                canvasRect);
            AssertTrue(imageRegion.Width > 120 && imageRegion.Height > 300, "operator video fitted defect image bounds were unusable");
            AssertTrue(
                Math.Abs(
                    (imageRegion.Left + imageRegion.Width / 2D)
                    - (canvasRect.Left + canvasRect.Width / 2D)) <= 18D
                    && Math.Abs(
                        (imageRegion.Top + imageRegion.Height / 2D)
                        - (canvasRect.Top + canvasRect.Height / 2D)) <= 18D,
                "operator video loaded image was not automatically fitted to the initial canvas");

            CaptureAutomationRoot(root, Path.Combine(paths.ScreenshotDirectory, "00_ready_defect.png"));

            System.Windows.Rect windowBounds = root.Current.BoundingRectangle;
            AssertTrue(
                !windowBounds.IsEmpty
                    && windowBounds.Width >= 1600
                    && windowBounds.Height >= 900,
                $"operator video application-only crop was invalid: {windowBounds}");
            Point safeCursorPoint = new Point(
                (int)Math.Round(windowBounds.Left + windowBounds.Width * 0.54D),
                (int)Math.Round(windowBounds.Top + 110D));
            cursorMoveCount += HumanMoveCursorTo(safeCursorPoint);

            recorder = OperatorVideoRecorder.Start(
                ffmpegPath,
                paths.RawVideoPath,
                root.Current.Name);
            eventLog.Start();
            eventLog.Write(
                "recording-started",
                $"application-window-only {Math.Round(windowBounds.Width)}x{Math.Round(windowBounds.Height)}; "
                + $"defect={OperatorVideoDefectStem}; canvas={FormatOperatorVideoRect(canvasRect)}; "
                + $"image={FormatOperatorVideoRect(imageRegion)}");
            Thread.Sleep(900);

            if (verifyAutoContourMode)
            {
                System.Windows.Automation.AutomationElement autoContourToggle = null;
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            autoContourToggle = FindAutomationElementByAutomationId(
                                RefreshAutomationRoot(appProcess, bringToFront: false),
                                "CanvasSmartMaskAutoContourToggle");
                            return autoContourToggle != null && autoContourToggle.Current.IsEnabled;
                        },
                        TimeSpan.FromSeconds(4)),
                    "operator video automatic contour labeling option was unavailable");
                cursorMoveCount += HumanClickAutomationElement(autoContourToggle);
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            System.Windows.Automation.AutomationElement latest =
                                RefreshAutomationRoot(appProcess, bringToFront: false);
                            System.Windows.Automation.AutomationElement toggle =
                                FindAutomationElementByAutomationId(latest, "CanvasSmartMaskAutoContourToggle");
                            return toggle != null
                                && toggle.Current.Name.Contains("켜짐", StringComparison.Ordinal);
                        },
                        TimeSpan.FromSeconds(3)),
                    "operator video automatic contour mode did not remain selected");
                eventLog.Write(
                    "auto-contour-mode-enabled",
                    "segmentation labeling option selected once before repeated box drawing");
            }
            else
            {
                System.Windows.Automation.AutomationElement boxTool = null;
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            boxTool = FindAnnotationToolItem(
                                RefreshAutomationRoot(appProcess, bringToFront: false),
                                "\uBC15\uC2A4");
                            return boxTool != null && boxTool.Current.IsEnabled;
                        },
                        TimeSpan.FromSeconds(4)),
                    "operator video box tool was unavailable");
                cursorMoveCount += HumanClickAutomationElement(boxTool);
                eventLog.Write("box-tool-selected", "Smart Mask start-box tool selected");
            }
            AssertTrue(
                WaitUntil(
                    () => string.Equals(
                        GetSelectedAnnotationToolText(
                            RefreshAutomationRoot(appProcess, bringToFront: false)),
                        "\uBC15\uC2A4",
                        StringComparison.Ordinal),
                    TimeSpan.FromSeconds(3)),
                "operator video box tool selection did not become active");

            root = RefreshAutomationRoot(appProcess);
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest =
                            RefreshAutomationRoot(appProcess, bringToFront: false);
                        System.Windows.Automation.AutomationElement latestCanvas =
                            FindOperatorVideoVisibleCanvas(latest);
                        if (latestCanvas == null)
                        {
                            return false;
                        }

                        System.Windows.Rect latestCanvasRect =
                            BuildOperatorVideoCanvasViewport(latest, latestCanvas);
                        System.Windows.Rect latestImageRegion =
                            FindOperatorVideoDisplayedImageBounds(latest, latestCanvasRect);
                        return latestImageRegion.Width > 120D
                            && latestImageRegion.Height > 300D
                            && Math.Abs(
                                (latestImageRegion.Left + latestImageRegion.Width / 2D)
                                - (latestCanvasRect.Left + latestCanvasRect.Width / 2D)) <= 6D
                            && Math.Abs(
                                (latestImageRegion.Top + latestImageRegion.Height / 2D)
                                - (latestCanvasRect.Top + latestCanvasRect.Height / 2D)) <= 18D;
                    },
                    TimeSpan.FromSeconds(5)),
                "operator video viewer did not automatically fit after the workflow layout changed");
            root = RefreshAutomationRoot(appProcess);
            canvas = FindOperatorVideoVisibleCanvas(root);
            AssertTrue(canvas != null, "operator video visible canvas disappeared after automatic layout fit");
            canvasRect = BuildOperatorVideoCanvasViewport(root, canvas);
            imageRegion = FindOperatorVideoDisplayedImageBounds(root, canvasRect);
            eventLog.Write(
                "image-auto-fit-observed",
                "viewer remained centered after the workflow layout changed; no Fit action was clicked");
            OperatorVideoPromptGeometry prompt = BuildOperatorVideoPromptGeometry(
                imageRegion,
                defect.ImageSize,
                defect.MaskBounds);
            eventLog.Write(
                "defect-geometry-refreshed",
                $"canvas={FormatOperatorVideoRect(canvasRect)}; image={FormatOperatorVideoRect(imageRegion)}; "
                + $"box={prompt.BoxStart.X},{prompt.BoxStart.Y}-{prompt.BoxEnd.X},{prompt.BoxEnd.Y}");
            cursorMoveCount += HumanDragCursor(prompt.BoxStart, prompt.BoxEnd);
            eventLog.Write("defect-box-drawn", "rough box drawn around the visible crack defect");
            root = RefreshAutomationRoot(appProcess);
            CaptureAutomationRoot(root, Path.Combine(paths.ScreenshotDirectory, "01_defect_box.png"));

            System.Windows.Automation.AutomationElement create = null;
            if (verifyAutoContourMode)
            {
                eventLog.Write(
                    "candidate-auto-requested",
                    "rough box completion started real Smart Mask generation without another action click");
            }
            else
            {
                create = FindAutomationElementByAutomationId(root, "CanvasCreateSmartMaskButton");
                AssertTrue(create != null && create.Current.IsEnabled, "operator video Smart Mask action was unavailable");
                cursorMoveCount += HumanClickAutomationElement(create);
                eventLog.Write("candidate-requested", "real Smart Mask generation requested from the rough box");
            }
            AssertTrue(
                WaitForOperatorVideoSmartMaskReady(appProcess, TimeSpan.FromSeconds(55)),
                "operator video initial Smart Mask candidate did not become reviewable");
            root = RestoreOperatorVideoWindow(appProcess);
            CaptureAutomationRoot(root, Path.Combine(paths.ScreenshotDirectory, "02_initial_candidate.png"));
            eventLog.Write("candidate-ready", "automatic filled-mask candidate and boundary visible");
            if (verifyContextualCorrection)
            {
                System.Windows.Automation.AutomationElement correctionOptions =
                    FindAutomationElementByAutomationId(root, "CanvasSmartMaskCorrectionOptionsButton");
                System.Windows.Automation.AutomationElement positivePoint =
                    FindAutomationElementByAutomationId(root, "CanvasSmartMaskPositivePointButton");
                AssertTrue(
                    correctionOptions != null && correctionOptions.Current.IsEnabled,
                    "operator video contextual Smart Mask correction action was unavailable");
                AssertTrue(
                    positivePoint == null || positivePoint.Current.IsOffscreen,
                    "operator video Smart Mask point correction should be hidden by default");
                cursorMoveCount += HumanClickAutomationElement(correctionOptions);
                AssertTrue(
                    WaitUntil(
                        () =>
                        {
                            System.Windows.Automation.AutomationElement latest =
                                RefreshAutomationRoot(appProcess, bringToFront: false);
                            System.Windows.Automation.AutomationElement point =
                                FindAutomationElementByAutomationId(latest, "CanvasSmartMaskPositivePointButton");
                            return point != null && !point.Current.IsOffscreen;
                        },
                        TimeSpan.FromSeconds(3)),
                    "operator video Smart Mask correction controls did not expand");
                root = RestoreOperatorVideoWindow(appProcess);
                CaptureAutomationRoot(
                    root,
                    Path.Combine(paths.ScreenshotDirectory, "02b_contextual_correction_expanded.png"));
                eventLog.Write(
                    "contextual-correction-verified",
                    "point/detail controls hidden by default and expanded only on request");
                if (verifyCandidateRestore)
                {
                    root = RestoreOperatorVideoWindow(appProcess);
                    System.Windows.Automation.AutomationElement correctionPositivePoint =
                        FindAutomationElementByAutomationId(root, "CanvasSmartMaskPositivePointButton");
                    AssertTrue(
                        correctionPositivePoint != null
                            && correctionPositivePoint.Current.IsEnabled
                            && !correctionPositivePoint.Current.IsOffscreen,
                        "operator video positive Smart Mask point action was unavailable");
                    cursorMoveCount += HumanClickAutomationElement(correctionPositivePoint);
                    cursorMoveCount += HumanClickPoint(prompt.PositivePoint);

                    root = RestoreOperatorVideoWindow(appProcess);
                    System.Windows.Automation.AutomationElement negativePoint =
                        FindAutomationElementByAutomationId(root, "CanvasSmartMaskNegativePointButton");
                    AssertTrue(
                        negativePoint != null
                            && negativePoint.Current.IsEnabled
                            && !negativePoint.Current.IsOffscreen,
                        "operator video negative Smart Mask point action was unavailable");
                    cursorMoveCount += HumanClickAutomationElement(negativePoint);
                    cursorMoveCount += HumanClickPoint(prompt.NegativePoint);

                    root = RestoreOperatorVideoWindow(appProcess);
                    AssertTrue(
                        ContainsAutomationText(root, "+ 포함 1")
                            && ContainsAutomationText(root, "− 제외 1"),
                        "operator video correction prompt did not retain one positive and one negative point");
                    create = FindAutomationElementByAutomationId(root, "CanvasCreateSmartMaskButton");
                    AssertTrue(
                        create != null && create.Current.IsEnabled,
                        "operator video corrected Smart Mask rerun action was unavailable");
                    cursorMoveCount += HumanClickAutomationElement(create);
                    eventLog.Write(
                        "corrected-candidate-requested",
                        "one positive and one negative point submitted in one real MobileSAM rerun");

                    bool observedRerunBusy = false;
                    AssertTrue(
                        WaitUntil(
                            () =>
                            {
                                System.Windows.Automation.AutomationElement latest =
                                    RefreshAutomationRoot(appProcess, bringToFront: false);
                                System.Windows.Automation.AutomationElement rerun =
                                    FindAutomationElementByAutomationId(latest, "CanvasCreateSmartMaskButton");
                                if (rerun != null && !rerun.Current.IsEnabled)
                                {
                                    observedRerunBusy = true;
                                }

                                System.Windows.Automation.AutomationElement previous =
                                    FindAutomationElementByAutomationId(
                                        latest,
                                        "CanvasSmartMaskShowInitialCandidateButton");
                                return observedRerunBusy
                                    && rerun != null
                                    && rerun.Current.IsEnabled
                                    && previous != null
                                    && previous.Current.IsEnabled;
                            },
                            TimeSpan.FromSeconds(55)),
                        "operator video corrected Smart Mask candidate did not become comparable");

                    root = RestoreOperatorVideoWindow(appProcess);
                    CaptureAutomationRoot(
                        root,
                        Path.Combine(paths.ScreenshotDirectory, "02c_corrected_candidate.png"));
                    eventLog.Write(
                        "corrected-candidate-ready",
                        "latest corrected candidate visible with previous-candidate recovery enabled");

                    System.Windows.Automation.AutomationElement previousCandidate =
                        FindAutomationElementByAutomationId(
                            root,
                            "CanvasSmartMaskShowInitialCandidateButton");
                    AssertTrue(
                        previousCandidate != null
                            && previousCandidate.Current.IsEnabled
                            && !previousCandidate.Current.IsOffscreen,
                        "operator video previous Smart Mask candidate action was unavailable");
                    cursorMoveCount += HumanClickAutomationElement(previousCandidate);
                    AssertTrue(
                        WaitUntil(
                            () =>
                            {
                                System.Windows.Automation.AutomationElement latest =
                                    RefreshAutomationRoot(appProcess, bringToFront: false);
                                System.Windows.Automation.AutomationElement previous =
                                    FindAutomationElementByAutomationId(
                                        latest,
                                        "CanvasSmartMaskShowInitialCandidateButton");
                                System.Windows.Automation.AutomationElement current =
                                    FindAutomationElementByAutomationId(
                                        latest,
                                        "CanvasSmartMaskShowLatestCandidateButton");
                                return previous != null
                                    && !previous.Current.IsEnabled
                                    && current != null
                                    && current.Current.IsEnabled;
                            },
                            TimeSpan.FromSeconds(5)),
                        "operator video previous Smart Mask candidate was not restored as the selected version");
                    root = RestoreOperatorVideoWindow(appProcess);
                    CaptureAutomationRoot(
                        root,
                        Path.Combine(paths.ScreenshotDirectory, "02d_previous_candidate_restored.png"));
                    eventLog.Write(
                        "previous-candidate-restored",
                        "initial candidate selected again; latest candidate remains available and no save has occurred");
                }
                else
                {
                    correctionOptions = FindAutomationElementByAutomationId(
                        root,
                        "CanvasSmartMaskCorrectionOptionsButton");
                    cursorMoveCount += HumanClickAutomationElement(correctionOptions);
                    AssertTrue(
                        WaitUntil(
                            () =>
                            {
                                System.Windows.Automation.AutomationElement latest =
                                    RefreshAutomationRoot(appProcess, bringToFront: false);
                                System.Windows.Automation.AutomationElement point =
                                    FindAutomationElementByAutomationId(latest, "CanvasSmartMaskPositivePointButton");
                                return point == null || point.Current.IsOffscreen;
                            },
                            TimeSpan.FromSeconds(3)),
                        "operator video Smart Mask correction controls did not collapse");
                    root = RestoreOperatorVideoWindow(appProcess);
                    eventLog.Write(
                        "contextual-correction-collapsed",
                        "point/detail controls returned to auto-first review");
                }
            }
            eventLog.Write(
                "candidate-reviewed",
                verifyCandidateRestore
                    ? "corrected candidate rejected; restored automatic candidate selected for explicit confirmation"
                    : "box-only candidate accepted because optional point correction did not improve this sample");
            Thread.Sleep(1400);

            System.Windows.Automation.AutomationElement confirm = FindSmartMaskConfirmButton(root);
            AssertTrue(confirm != null && confirm.Current.IsEnabled, "operator video Smart Mask confirm was unavailable");
            cursorMoveCount += HumanClickAutomationElement(confirm);
            eventLog.Write("candidate-confirmed", "human review confirmed the automatic pending candidate");

            IReadOnlyList<string> artifactCandidates = EnumerateExeSmokeSaveArtifactPaths(
                    dataDirectory,
                    OperatorVideoDefectStem)
                .Where(IsExeSmokeSegmentationArtifactPath)
                .ToList();
            AssertTrue(
                WaitUntil(
                    () => artifactCandidates.Count(File.Exists) >= 2,
                    TimeSpan.FromSeconds(10)),
                "operator video explicit save did not create segmentation artifacts");
            IReadOnlyList<string> savedArtifacts = artifactCandidates.Where(File.Exists).ToList();
            ExeSmokeSavedArtifactDiagnostics savedArtifact =
                ValidateExeSmokeSavedArtifactContents(savedArtifacts);
            string savedMaskPath = savedArtifacts.Single(path =>
                string.Equals(
                    Directory.GetParent(path)?.Name,
                    "masks",
                    StringComparison.OrdinalIgnoreCase));
            OperatorVideoMaskQuality maskQuality =
                MeasureOperatorVideoMaskQuality(defect.MaskPath, savedMaskPath);
            AssertTrue(
                savedArtifact.SegmentPoints >= 48
                    && maskQuality.IntersectionOverUnion >= 0.35D
                    && maskQuality.Precision >= 0.50D
                    && maskQuality.Recall >= 0.35D,
                "operator video automatic candidate did not meet the human-review candidate gate: "
                    + $"points={savedArtifact.SegmentPoints}; iou={maskQuality.IntersectionOverUnion:F4}; "
                    + $"precision={maskQuality.Precision:F4}; recall={maskQuality.Recall:F4}");
            CopyOperatorVideoSavedEvidence(savedArtifacts, paths.SavedArtifactDirectory);
            eventLog.Write(
                "annotation-saved",
                $"maskPixels={savedArtifact.MaskPixels}; polygons={savedArtifact.SegmentPolygons}; "
                + $"points={savedArtifact.SegmentPoints}; iou={maskQuality.IntersectionOverUnion:F4}; "
                + $"precision={maskQuality.Precision:F4}; recall={maskQuality.Recall:F4}");
            root = RefreshAutomationRoot(appProcess);
            CaptureAutomationRoot(root, Path.Combine(paths.ScreenshotDirectory, "03_confirmed_label.png"));
            Thread.Sleep(1200);

            bool reopenVerified = false;
            System.Windows.Automation.AutomationElement nextImage =
                FindAutomationElementByAutomationId(root, "NextUnlabeledPrimaryButton");
            if (nextImage != null && nextImage.Current.IsEnabled)
            {
                cursorMoveCount += HumanClickAutomationElement(nextImage);
                eventLog.Write("next-incomplete-opened", "worklist advanced after explicit save");
                Thread.Sleep(900);
            }

            if (verifyCandidateRestore)
            {
                AssertTrue(
                    SelectImageQueueItemBySearch(
                        appProcess,
                        OperatorVideoDefectStem,
                        TimeSpan.FromSeconds(8)),
                    "operator video could not reopen the saved Smart Mask image");
                root = RestoreOperatorVideoWindow(appProcess);
                AssertTrue(
                    SelectAutomationTabByAutomationId(root, "ObjectsReviewTab")
                        || SelectTabItemByName(root, "저장 라벨"),
                    "operator video saved-label tab was unavailable for reopen verification");
                reopenVerified = WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest =
                            RefreshAutomationRoot(appProcess, bringToFront: false);
                        return ContainsAutomationText(latest, OperatorVideoDefectStem)
                            && GetObjectReviewSummaryCount(latest) == 1
                            && FindSmartMaskConfirmButton(latest)?.Current.IsEnabled != true;
                    },
                    TimeSpan.FromSeconds(8));
                AssertTrue(
                    reopenVerified,
                    "operator video restored candidate did not reopen as exactly one saved label");
                ExeSmokeSavedArtifactDiagnostics reopenedArtifact =
                    ValidateExeSmokeSavedArtifactContents(savedArtifacts);
                AssertEqual(savedArtifact.MaskPixels, reopenedArtifact.MaskPixels);
                AssertEqual(savedArtifact.SegmentPolygons, reopenedArtifact.SegmentPolygons);
                AssertEqual(savedArtifact.SegmentPoints, reopenedArtifact.SegmentPoints);
                root = RestoreOperatorVideoWindow(appProcess);
                CaptureAutomationRoot(
                    root,
                    Path.Combine(paths.ScreenshotDirectory, "04_reopened_saved_label.png"));
                eventLog.Write(
                    "saved-label-reopened",
                    "original image reopened with exactly one saved label and no pending Smart Mask confirmation");
            }

            root = RefreshAutomationRoot(appProcess);
            CaptureAutomationRoot(root, Path.Combine(paths.ScreenshotDirectory, "04_final_state.png"));
            eventLog.Write("recording-stopping", "final application state held");
            Thread.Sleep(1000);
            recorder.Stop();
            recorder = null;
            eventLog.Stop();

            AssertTrue(File.Exists(paths.RawVideoPath), "operator video MP4 was not created");
            AssertTrue(new FileInfo(paths.RawVideoPath).Length > 500_000, "operator video MP4 was unexpectedly small");
            return new OperatorVideoRunResult(
                eventLog.Elapsed.TotalMilliseconds,
                cursorMoveCount,
                savedArtifact,
                maskQuality,
                verifyAutoContourMode,
                verifyCandidateRestore,
                reopenVerified);
        }
        catch
        {
            try
            {
                if (appProcess != null && !appProcess.HasExited)
                {
                    System.Windows.Automation.AutomationElement root =
                        RefreshAutomationRoot(appProcess, bringToFront: false);
                    CaptureAutomationRoot(
                        root,
                        Path.Combine(paths.ScreenshotDirectory, "failure.png"));
                }
            }
            catch
            {
            }

            throw;
        }
        finally
        {
            recorder?.Stop();
            eventLog.Dispose();
            CloseExeSmokeProcess(appProcess);
            DeleteDirectoryIfExists(recipeDirectory);
            DeleteDirectoryIfExists(dataDirectory);
        }
    }

    private static void CreateOperatorVideoRecipeThroughUi(
        Process process,
        string recipeName,
        string outputRoot,
        string recipeDirectory)
    {
        System.Windows.Automation.AutomationElement root = RefreshAutomationRoot(process);
        AssertTrue(
            TryInvokeAutomationButtonByAutomationId(root, "DatasetHomeStageButton"),
            "operator video could not open the dataset stage");
        AssertTrue(
            WaitUntil(
                () =>
                {
                    System.Windows.Automation.AutomationElement latest =
                        RefreshAutomationRoot(process, bringToFront: false);
                    return FindAutomationElementByAutomationId(latest, "DatasetSetupStartButton") != null;
                },
                TimeSpan.FromSeconds(5)),
            "operator video dataset setup action did not become visible");
        root = RefreshAutomationRoot(process);
        System.Windows.Automation.AutomationElement setup =
            FindAutomationElementByAutomationId(root, "DatasetSetupStartButton");
        AssertTrue(
            setup != null && setup.Current.IsEnabled && !setup.Current.IsOffscreen,
            "operator video dataset setup action was not operable");
        if (!TryInvokeAutomationButtonByAutomationId(root, "DatasetSetupStartButton"))
        {
            NativeClick(GetAutomationCenter(setup));
        }

        System.Windows.Automation.AutomationElement wizard = WaitForProcessWindowByName(
            process,
            "새 Recipe 설정",
            TimeSpan.FromSeconds(8));
        AssertTrue(
            SelectListItemByText(wizard, "\uC138\uADF8\uBA58\uD14C\uC774\uC158"),
            "operator video could not select segmentation in the dataset wizard");
        wizard = WaitForProcessWindowByName(
            process,
            "새 Recipe 설정",
            TimeSpan.FromSeconds(8));
        WaitForAutomationText(wizard, "\uBAA9\uC801: \uC138\uADF8\uBA58\uD14C\uC774\uC158", TimeSpan.FromSeconds(3));
        AssertTrue(
            SelectListItemContainingText(wizard, "\uC0B0\uC5C5 \uACB0\uD568 \uB9C8\uC2A4\uD06C \uC0D8\uD50C"),
            "operator video could not select the industrial defect-mask preset");
        wizard = WaitForProcessWindowByName(
            process,
            "새 Recipe 설정",
            TimeSpan.FromSeconds(8));
        AssertTrue(
            WaitUntil(
                () => ContainsAutomationText(wizard, "\uC0B0\uC5C5")
                    && ContainsAutomationText(wizard, "\uB9C8\uC2A4\uD06C"),
                TimeSpan.FromSeconds(4)),
            "operator video wizard did not expose the industrial defect-mask configuration");
        AssertTrue(
            TrySetAutomationValueByAutomationId(wizard, "WizardRecipeNameBox", recipeName),
            "operator video Recipe name was not editable");
        AssertTrue(
            TrySetAutomationValueByAutomationId(wizard, "WizardOutputRootPathBox", outputRoot),
            "operator video output root was not editable");
        AssertTrue(
            TrySetAutomationValueByAutomationId(wizard, "WizardClassNamesBox", "Defect"),
            "operator video Defect class was not editable");
        System.Windows.Automation.AutomationElement create =
            FindAutomationElementByAutomationId(wizard, "WizardCreateButton");
        AssertTrue(create != null && create.Current.IsEnabled, "operator video dataset create action was unavailable");
        NativeClick(GetAutomationCenter(create));

        string manifestPath = Path.Combine(recipeDirectory, LabelingDatasetManifestService.FileName);
        AssertTrue(
            WaitUntil(
                () => File.Exists(manifestPath)
                    && Directory.Exists(Path.Combine(outputRoot, "data", "train", "images"))
                    && File.Exists(Path.Combine(
                        outputRoot,
                        "data",
                        "train",
                        "images",
                        OperatorVideoDefectStem + ".jpg")),
                TimeSpan.FromSeconds(15)),
            "operator video temporary Kolektor Recipe was not created through the UI");
    }

    private static bool WaitForOperatorVideoSmartMaskReady(Process process, TimeSpan timeout)
    {
        bool observedBusy = false;
        return WaitUntil(
            () =>
            {
                System.Windows.Automation.AutomationElement latest =
                    RefreshAutomationRoot(process, bringToFront: false);
                System.Windows.Automation.AutomationElement action =
                    FindAutomationElementByAutomationId(latest, "CanvasCreateSmartMaskButton");
                if (action != null && !action.Current.IsEnabled)
                {
                    observedBusy = true;
                }

                return action != null
                    && action.Current.IsEnabled
                    && (observedBusy
                        || FindSmartMaskConfirmButton(latest)?.Current.IsEnabled == true);
            },
            timeout);
    }

    private static System.Windows.Automation.AutomationElement RestoreOperatorVideoWindow(Process process)
    {
        AssertTrue(process != null && !process.HasExited, "operator video EXE exited before foreground restore");
        process.Refresh();
        IntPtr handle = process.MainWindowHandle;
        AssertTrue(handle != IntPtr.Zero, "operator video EXE lost its main window");
        BringNativeWindowToFront(handle);
        Thread.Sleep(350);
        return RefreshAutomationRoot(process, handle, bringToFront: false);
    }

    private static System.Windows.Automation.AutomationElement FindOperatorVideoVisibleCanvas(
        System.Windows.Automation.AutomationElement root)
        => EnumerateAutomationDescendants(root)
            .Where(element =>
            {
                try
                {
                    System.Windows.Rect bounds = element.Current.BoundingRectangle;
                    return string.Equals(
                            element.Current.ClassName,
                            "RoiImageCanvasView",
                            StringComparison.Ordinal)
                        && !element.Current.IsOffscreen
                        && bounds.Width > 400D
                        && bounds.Height > 300D;
                }
                catch (System.Windows.Automation.ElementNotAvailableException)
                {
                    return false;
                }
            })
            .OrderByDescending(element =>
            {
                System.Windows.Rect bounds = element.Current.BoundingRectangle;
                return bounds.Width * bounds.Height;
            })
            .FirstOrDefault();

    private static System.Windows.Rect BuildOperatorVideoCanvasViewport(
        System.Windows.Automation.AutomationElement root,
        System.Windows.Automation.AutomationElement canvas)
    {
        System.Windows.Rect bounds = canvas.Current.BoundingRectangle;
        System.Windows.Automation.AutomationElement toolRail =
            FindAutomationElementByAutomationId(root, "CanvasAnnotationToolListBox");
        if (toolRail == null || toolRail.Current.IsOffscreen)
        {
            return bounds;
        }

        System.Windows.Rect toolBounds = toolRail.Current.BoundingRectangle;
        double left = Math.Max(bounds.Left, toolBounds.Right + 4D);
        return new System.Windows.Rect(
            left,
            bounds.Top,
            Math.Max(100D, bounds.Right - left),
            bounds.Height);
    }

    private static System.Windows.Rect FindOperatorVideoDisplayedImageBounds(
        System.Windows.Automation.AutomationElement root,
        System.Windows.Rect searchBounds)
    {
        using Bitmap screenshot = CaptureAutomationRootBitmap(root);
        System.Windows.Rect rootBounds = root.Current.BoundingRectangle;
        int left = Math.Clamp(
            (int)Math.Floor(searchBounds.Left - rootBounds.Left),
            0,
            screenshot.Width - 1);
        int top = Math.Clamp(
            (int)Math.Floor(searchBounds.Top - rootBounds.Top),
            0,
            screenshot.Height - 1);
        int right = Math.Clamp(
            (int)Math.Ceiling(searchBounds.Right - rootBounds.Left),
            left + 1,
            screenshot.Width);
        int bottom = Math.Clamp(
            (int)Math.Ceiling(searchBounds.Bottom - rootBounds.Top),
            top + 1,
            screenshot.Height);
        int searchHeight = bottom - top;
        var brightColumns = new bool[right - left];
        for (int x = left; x < right; x++)
        {
            int bright = 0;
            for (int y = top; y < bottom; y++)
            {
                if (IsOperatorVideoImagePixel(screenshot.GetPixel(x, y)))
                {
                    bright++;
                }
            }

            brightColumns[x - left] = bright >= searchHeight * 0.55D;
        }

        (int start, int length) columnRun = FindOperatorVideoLongestRun(brightColumns);
        AssertTrue(columnRun.length > 100, "operator video could not locate the displayed defect image columns");
        int imageLeft = left + columnRun.start;
        int imageRight = imageLeft + columnRun.length;
        int imageWidth = imageRight - imageLeft;
        var brightRows = new bool[bottom - top];
        for (int y = top; y < bottom; y++)
        {
            int bright = 0;
            for (int x = imageLeft; x < imageRight; x++)
            {
                if (IsOperatorVideoImagePixel(screenshot.GetPixel(x, y)))
                {
                    bright++;
                }
            }

            // Candidate outlines and the review card can cover a material part of
            // this narrow industrial image. The x run is already isolated to the
            // bright source image, so a lower row ratio remains unambiguous.
            brightRows[y - top] = bright >= imageWidth * 0.35D;
        }

        int firstBrightRow = -1;
        int lastBrightRow = -1;
        for (int index = 0; index < brightRows.Length; index++)
        {
            if (!brightRows[index])
            {
                continue;
            }

            if (firstBrightRow < 0)
            {
                firstBrightRow = index;
            }

            lastBrightRow = index;
        }

        int imageHeight = lastBrightRow - firstBrightRow + 1;
        AssertTrue(imageHeight > 300, "operator video could not locate the displayed defect image rows");
        return new System.Windows.Rect(
            rootBounds.Left + imageLeft,
            rootBounds.Top + top + firstBrightRow,
            imageWidth,
            imageHeight);
    }

    private static bool IsOperatorVideoImagePixel(Color color)
    {
        int maximum = Math.Max(color.R, Math.Max(color.G, color.B));
        int minimum = Math.Min(color.R, Math.Min(color.G, color.B));
        return minimum >= 72 && maximum - minimum <= 38;
    }

    private static (int start, int length) FindOperatorVideoLongestRun(IReadOnlyList<bool> values)
    {
        int bestStart = 0;
        int bestLength = 0;
        int currentStart = 0;
        int currentLength = 0;
        for (int index = 0; index < values.Count; index++)
        {
            if (values[index])
            {
                if (currentLength == 0)
                {
                    currentStart = index;
                }

                currentLength++;
                if (currentLength > bestLength)
                {
                    bestStart = currentStart;
                    bestLength = currentLength;
                }
            }
            else
            {
                currentLength = 0;
            }
        }

        return (bestStart, bestLength);
    }

    private static string FormatOperatorVideoRect(System.Windows.Rect rectangle)
        => FormattableString.Invariant(
            $"{rectangle.X:F0},{rectangle.Y:F0},{rectangle.Width:F0},{rectangle.Height:F0}");

    private static OperatorVideoDefectSource ResolveOperatorVideoDefectSource(string repositoryRoot)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (string root in new[]
        {
            Path.Combine(repositoryRoot, "datasets", "industrial", "KolektorSDD", "raw", "expanded"),
            Path.Combine(userProfile, "LabelingIndustrialDatasets", "KolektorSDD", "raw", "expanded"),
            @"C:\temp\kolektor_test\KolektorSDD\raw\expanded"
        })
        {
            string imagePath = Path.Combine(root, "kos14", "Part7.jpg");
            string maskPath = Path.Combine(root, "kos14", "Part7_label.bmp");
            if (!File.Exists(imagePath) || !File.Exists(maskPath))
            {
                continue;
            }

            using var image = new Bitmap(imagePath);
            Rectangle maskBounds = FindOperatorVideoMaskBounds(maskPath);
            return new OperatorVideoDefectSource(
                imagePath,
                maskPath,
                image.Size,
                maskBounds);
        }

        throw new DirectoryNotFoundException(
            "KolektorSDD raw defect sample was not found in the supported local dataset roots.");
    }

    private static Rectangle FindOperatorVideoMaskBounds(string maskPath)
    {
        using var bitmap = new Bitmap(maskPath);
        int minX = bitmap.Width;
        int minY = bitmap.Height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.R <= 16 && pixel.G <= 16 && pixel.B <= 16)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX || maxY < minY
            ? Rectangle.Empty
            : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private static OperatorVideoPromptGeometry BuildOperatorVideoPromptGeometry(
        System.Windows.Rect imageRegion,
        Size imageSize,
        Rectangle maskBounds)
    {
        double left = Math.Max(0D, maskBounds.Left - 27D);
        double right = Math.Min(imageSize.Width, maskBounds.Right + 32D);
        double top = Math.Max(0D, maskBounds.Top - 28D);
        double bottom = Math.Min(imageSize.Height, maskBounds.Bottom + 32D);
        Point boxStart = OperatorVideoImageToScreen(imageRegion, imageSize, left, top);
        Point boxEnd = OperatorVideoImageToScreen(imageRegion, imageSize, right, bottom);
        if (Math.Abs(boxEnd.Y - boxStart.Y) < 54)
        {
            int centerY = (boxStart.Y + boxEnd.Y) / 2;
            boxStart.Y = centerY - 27;
            boxEnd.Y = centerY + 27;
        }

        Point positive = OperatorVideoImageToScreen(
            imageRegion,
            imageSize,
            maskBounds.Left + (maskBounds.Width * 0.60D),
            maskBounds.Top + (maskBounds.Height * 0.78D));
        Point negative = OperatorVideoImageToScreen(
            imageRegion,
            imageSize,
            maskBounds.Left + (maskBounds.Width * 0.11D),
            maskBounds.Top + (maskBounds.Height * 0.06D));
        return new OperatorVideoPromptGeometry(boxStart, boxEnd, positive, negative);
    }

    private static IReadOnlyList<Point> BuildOperatorVideoDefectPolygon(
        System.Windows.Rect imageRegion,
        Size imageSize,
        Rectangle maskBounds)
    {
        double left = maskBounds.Left + 3D;
        double right = maskBounds.Right - 4D;
        double top = maskBounds.Top + 2D;
        double bottom = maskBounds.Bottom - 3D;
        double rightTop = maskBounds.Top + (maskBounds.Height * 0.20D);
        double leftBottom = maskBounds.Bottom - (maskBounds.Height * 0.20D);
        Point first = OperatorVideoImageToScreen(imageRegion, imageSize, left, top);
        return new[]
        {
            first,
            OperatorVideoImageToScreen(imageRegion, imageSize, right, rightTop),
            OperatorVideoImageToScreen(imageRegion, imageSize, right, bottom),
            OperatorVideoImageToScreen(imageRegion, imageSize, left, leftBottom),
            first
        };
    }

    private static Point OperatorVideoImageToScreen(
        System.Windows.Rect imageRegion,
        Size imageSize,
        double imageX,
        double imageY)
        => new Point(
            (int)Math.Round(imageRegion.Left + (imageX / imageSize.Width * imageRegion.Width)),
            (int)Math.Round(imageRegion.Top + (imageY / imageSize.Height * imageRegion.Height)));

    private static int HumanClickAutomationElement(
        System.Windows.Automation.AutomationElement element)
    {
        AssertTrue(element != null, "human click automation element was null");
        return HumanClickPoint(GetAutomationCenter(element));
    }

    private static int HumanClickPoint(Point point)
    {
        int moveCount = HumanMoveCursorTo(point);
        Thread.Sleep(170);
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(85);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(340);
        return moveCount;
    }

    private static int HumanDragCursor(Point start, Point end)
    {
        int moveCount = HumanMoveCursorTo(start);
        Thread.Sleep(210);
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(130);
        moveCount += HumanMoveCursorAlongCurve(start, end, 820, 18D);
        Thread.Sleep(90);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(520);
        return moveCount;
    }

    private static int HumanMoveCursorTo(Point target)
    {
        Point start;
        if (GetCursorPos(out OperatorVideoNativePoint native))
        {
            start = new Point(native.X, native.Y);
        }
        else
        {
            start = target;
        }

        double distance = Math.Sqrt(
            Math.Pow(target.X - start.X, 2D)
            + Math.Pow(target.Y - start.Y, 2D));
        int duration = (int)Math.Clamp(distance / 880D * 1000D, 280D, 1250D);
        double arc = Math.Clamp(distance * 0.035D, 6D, 32D);
        return HumanMoveCursorAlongCurve(start, target, duration, arc);
    }

    private static int HumanMoveCursorAlongCurve(
        Point start,
        Point end,
        int durationMilliseconds,
        double arcPixels)
    {
        int steps = Math.Max(12, durationMilliseconds / 16);
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Max(1D, Math.Sqrt((dx * dx) + (dy * dy)));
        double perpendicularX = -dy / length;
        double perpendicularY = dx / length;
        double direction = ((start.X + start.Y + end.X + end.Y) & 1) == 0 ? 1D : -1D;
        double controlX = (start.X + end.X) / 2D + (perpendicularX * arcPixels * direction);
        double controlY = (start.Y + end.Y) / 2D + (perpendicularY * arcPixels * direction);
        int sleep = Math.Max(8, durationMilliseconds / steps);
        for (int step = 1; step <= steps; step++)
        {
            double linear = step / (double)steps;
            double eased = linear < 0.5D
                ? 4D * linear * linear * linear
                : 1D - (Math.Pow(-2D * linear + 2D, 3D) / 2D);
            double inverse = 1D - eased;
            int x = (int)Math.Round(
                (inverse * inverse * start.X)
                + (2D * inverse * eased * controlX)
                + (eased * eased * end.X));
            int y = (int)Math.Round(
                (inverse * inverse * start.Y)
                + (2D * inverse * eased * controlY)
                + (eased * eased * end.Y));
            SetCursorPos(x, y);
            Thread.Sleep(sleep);
        }

        SetCursorPos(end.X, end.Y);
        return steps + 1;
    }

    private static OperatorVideoPaths PrepareOperatorVideoPaths(string outputDirectory)
    {
        string source = Path.Combine(outputDirectory, "source");
        string evidence = Path.Combine(outputDirectory, "evidence");
        string screenshots = Path.Combine(evidence, "screenshots");
        string keyframes = Path.Combine(evidence, "keyframes");
        string saved = Path.Combine(evidence, "saved-artifacts");
        string review = Path.Combine(outputDirectory, "review");
        string publish = Path.Combine(outputDirectory, "publish");
        foreach (string directory in new[]
        {
            source,
            evidence,
            screenshots,
            keyframes,
            saved,
            review,
            publish
        })
        {
            Directory.CreateDirectory(directory);
        }

        return new OperatorVideoPaths(
            outputDirectory,
            Path.Combine(source, "actual-exe-defect-labeling.mp4"),
            Path.Combine(source, "ffprobe.json"),
            Path.Combine(source, "sha256.txt"),
            Path.Combine(evidence, "events.jsonl"),
            screenshots,
            keyframes,
            saved,
            Path.Combine(evidence, "contact-sheet.png"),
            Path.Combine(review, "self-evaluation.md"),
            Path.Combine(review, "defects.md"),
            publish);
    }

    private static void FinalizeOperatorVideoEvidence(
        string ffmpegPath,
        string ffprobePath,
        OperatorVideoPaths paths,
        OperatorVideoRunResult result)
    {
        string ffprobeJson = RunOperatorVideoProcess(
            ffprobePath,
            new[]
            {
                "-v", "error",
                "-show_format",
                "-show_streams",
                "-of", "json",
                paths.RawVideoPath
            });
        File.WriteAllText(paths.FfprobePath, ffprobeJson, new UTF8Encoding(false));
        File.WriteAllText(
            paths.Sha256Path,
            ComputeOperatorVideoSha256(paths.RawVideoPath)
                + "  "
                + Path.GetFileName(paths.RawVideoPath)
                + Environment.NewLine,
            new UTF8Encoding(false));

        RunOperatorVideoProcess(
            ffmpegPath,
            new[]
            {
                "-hide_banner",
                "-loglevel", "error",
                "-y",
                "-i", paths.RawVideoPath,
                "-vf", "fps=1/3,scale=360:-2:flags=lanczos,tile=5x4:padding=6:margin=8:color=0x111827",
                "-frames:v", "1",
                paths.ContactSheetPath
            });
        RunOperatorVideoProcess(
            ffmpegPath,
            new[]
            {
                "-hide_banner",
                "-loglevel", "error",
                "-y",
                "-i", paths.RawVideoPath,
                "-vf", "fps=1,scale=960:-2:flags=lanczos",
                Path.Combine(paths.KeyframeDirectory, "frame-%03d.png")
            });

        File.WriteAllText(
            paths.SelfEvaluationPath,
            BuildOperatorVideoInitialEvaluation(result),
            new UTF8Encoding(false));
        File.WriteAllText(
            paths.DefectsPath,
            "# Operator Video Defects\n\n"
                + "Status: Pending visual review\n\n"
                + "The raw MP4, contact sheet, key frames, screenshots, and event log must be reviewed "
                + "before a promotional GIF is generated. No issue is hidden by editing at this stage.\n",
            new UTF8Encoding(false));
    }

    private static string BuildOperatorVideoInitialEvaluation(OperatorVideoRunResult result)
        => "# Actual EXE Defect Labeling - Initial Evidence Review\n\n"
            + "Status: Pending visual review\n\n"
            + "## Proven automatically\n\n"
            + $"- actual Kolektor defect sample selected: `{OperatorVideoDefectStem}`\n"
            + "- application-window-only recording completed\n"
            + $"- human-path cursor samples issued: {result.CursorMoveCount}\n"
            + "- actual rough-box input, automatic filled mask, automatic boundary, human confirmation, and save completed\n"
            + $"- saved mask non-zero pixels: {result.SavedArtifact.MaskPixels}\n"
            + $"- saved segment polygons: {result.SavedArtifact.SegmentPolygons}\n"
            + $"- saved segment points: {result.SavedArtifact.SegmentPoints}\n"
            + $"- automatic-contour box completion verified: {result.AutoContourVerified}\n"
            + $"- previous-candidate restore verified: {result.CandidateRestoreVerified}\n"
            + $"- saved-label reopen verified: {result.ReopenVerified}\n"
            + $"- ground-truth mask IoU: {result.MaskQuality.IntersectionOverUnion:F4}\n"
            + $"- ground-truth precision / recall: {result.MaskQuality.Precision:F4} / {result.MaskQuality.Recall:F4}\n"
            + $"- recorded interaction duration: {result.DurationMilliseconds / 1000D:F1}s\n\n"
            + "## Visual gate still required\n\n"
            + "- watch the uncut MP4 without pausing;\n"
            + "- inspect the contact sheet for desktop exposure, clipping, stale state, and discontinuity;\n"
            + "- inspect key frames around box input, automatic candidate, review, and save;\n"
            + "- assign P0-P3 findings and score the ten-category rubric;\n"
            + "- create a GitHub GIF only after every visible P0/P1/P2 issue in the hero path is resolved.\n";

    private static OperatorVideoMaskQuality MeasureOperatorVideoMaskQuality(
        string groundTruthPath,
        string savedMaskPath)
    {
        using var groundTruth = new Bitmap(groundTruthPath);
        using var savedMask = new Bitmap(savedMaskPath);
        AssertTrue(
            groundTruth.Size == savedMask.Size,
            $"operator video mask size mismatch: groundTruth={groundTruth.Size}; saved={savedMask.Size}");
        int groundTruthPixels = 0;
        int predictedPixels = 0;
        int intersectionPixels = 0;
        int unionPixels = 0;
        for (int y = 0; y < groundTruth.Height; y++)
        {
            for (int x = 0; x < groundTruth.Width; x++)
            {
                bool expected = groundTruth.GetPixel(x, y).R > 0;
                bool predicted = savedMask.GetPixel(x, y).R > 0;
                if (expected)
                {
                    groundTruthPixels++;
                }

                if (predicted)
                {
                    predictedPixels++;
                }

                if (expected && predicted)
                {
                    intersectionPixels++;
                }

                if (expected || predicted)
                {
                    unionPixels++;
                }
            }
        }

        return new OperatorVideoMaskQuality(
            groundTruthPixels,
            predictedPixels,
            intersectionPixels,
            unionPixels == 0 ? 0D : intersectionPixels / (double)unionPixels,
            predictedPixels == 0 ? 0D : intersectionPixels / (double)predictedPixels,
            groundTruthPixels == 0 ? 0D : intersectionPixels / (double)groundTruthPixels);
    }

    private static void CopyOperatorVideoSavedEvidence(
        IEnumerable<string> savedArtifacts,
        string outputDirectory)
    {
        foreach (string path in savedArtifacts)
        {
            string parentName = Directory.GetParent(path)?.Name ?? "artifact";
            string fileName = parentName + "-" + Path.GetFileName(path);
            File.Copy(path, Path.Combine(outputDirectory, fileName), overwrite: true);
        }
    }

    private static string ResolveOperatorVideoExecutable(string executable)
    {
        if (Path.IsPathRooted(executable))
        {
            AssertTrue(File.Exists(executable), $"operator video executable was not found: {executable}");
            return executable;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "where.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(executable);
        using Process process = Process.Start(startInfo);
        string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
        process?.WaitForExit();
        string resolved = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(File.Exists);
        AssertTrue(!string.IsNullOrWhiteSpace(resolved), $"operator video executable was not found on PATH: {executable}");
        return resolved;
    }

    private static string RunOperatorVideoProcess(string executable, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo);
        AssertTrue(process != null, $"operator video process did not start: {executable}");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        AssertTrue(process.WaitForExit(120_000), $"operator video process timed out: {executable}");
        AssertTrue(
            process.ExitCode == 0,
            $"operator video process failed ({process.ExitCode}): {Path.GetFileName(executable)} {stderr}");
        return stdout;
    }

    private static string ComputeOperatorVideoSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct OperatorVideoNativePoint
    {
        public readonly int X;
        public readonly int Y;
    }

    private sealed class OperatorVideoRecorder
    {
        private readonly Process process;
        private readonly StringBuilder errors;
        private bool stopped;

        private OperatorVideoRecorder(Process process, StringBuilder errors)
        {
            this.process = process;
            this.errors = errors;
        }

        public static OperatorVideoRecorder Start(
            string ffmpegPath,
            string outputPath,
            string windowTitle)
        {
            AssertTrue(!string.IsNullOrWhiteSpace(windowTitle), "operator video window title was unavailable");
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in new[]
            {
                "-hide_banner",
                "-loglevel", "warning",
                "-y",
                "-f", "gdigrab",
                "-framerate", "30",
                "-draw_mouse", "1",
                "-i", "title=" + windowTitle,
                "-c:v", "libx264",
                "-preset", "veryfast",
                "-crf", "19",
                "-pix_fmt", "yuv420p",
                "-movflags", "+faststart",
                outputPath
            })
            {
                startInfo.ArgumentList.Add(argument);
            }

            var errors = new StringBuilder();
            Process process = Process.Start(startInfo);
            AssertTrue(process != null, "FFmpeg recording process did not start");
            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                {
                    lock (errors)
                    {
                        errors.AppendLine(eventArgs.Data);
                    }
                }
            };
            process.BeginErrorReadLine();
            Thread.Sleep(900);
            AssertTrue(
                !process.HasExited,
                "FFmpeg exited before recording began: " + errors);
            return new OperatorVideoRecorder(process, errors);
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            if (process.HasExited)
            {
                AssertTrue(
                    process.ExitCode == 0,
                    "FFmpeg recording exited unexpectedly: " + errors);
                process.Dispose();
                return;
            }

            process.StandardInput.WriteLine("q");
            process.StandardInput.Flush();
            if (!process.WaitForExit(15_000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
                throw new TimeoutException("FFmpeg did not finalize the MP4 after receiving q");
            }

            int exitCode = process.ExitCode;
            process.Dispose();
            AssertTrue(exitCode == 0, "FFmpeg recording failed: " + errors);
        }
    }

    private sealed class OperatorVideoEventLog : IDisposable
    {
        private readonly StreamWriter writer;
        private readonly Stopwatch stopwatch = new Stopwatch();
        private int sequence;

        public OperatorVideoEventLog(string path)
        {
            writer = new StreamWriter(path, append: false, new UTF8Encoding(false));
        }

        public TimeSpan Elapsed => stopwatch.Elapsed;

        public void Start()
        {
            stopwatch.Restart();
            Write("event-log-started", "operator video event clock started");
        }

        public void Write(string eventName, string detail)
        {
            var payload = new
            {
                sequence = ++sequence,
                elapsedMilliseconds = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1),
                @event = eventName,
                detail
            };
            writer.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(payload));
            writer.Flush();
        }

        public void Stop()
        {
            stopwatch.Stop();
            writer.Flush();
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }

    private readonly record struct OperatorVideoDefectSource(
        string ImagePath,
        string MaskPath,
        Size ImageSize,
        Rectangle MaskBounds);

    private readonly record struct OperatorVideoPromptGeometry(
        Point BoxStart,
        Point BoxEnd,
        Point PositivePoint,
        Point NegativePoint);

    private readonly record struct OperatorVideoPaths(
        string RootDirectory,
        string RawVideoPath,
        string FfprobePath,
        string Sha256Path,
        string EventLogPath,
        string ScreenshotDirectory,
        string KeyframeDirectory,
        string SavedArtifactDirectory,
        string ContactSheetPath,
        string SelfEvaluationPath,
        string DefectsPath,
        string PublishDirectory);

    private readonly record struct OperatorVideoRunResult(
        double DurationMilliseconds,
        int CursorMoveCount,
        ExeSmokeSavedArtifactDiagnostics SavedArtifact,
        OperatorVideoMaskQuality MaskQuality,
        bool AutoContourVerified,
        bool CandidateRestoreVerified,
        bool ReopenVerified);

    private readonly record struct OperatorVideoMaskQuality(
        int GroundTruthPixels,
        int PredictedPixels,
        int IntersectionPixels,
        double IntersectionOverUnion,
        double Precision,
        double Recall);
}
