using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LabelingApplication.Tests;

internal static partial class Program
{
    private static class CrashRecoveryTests
    {
        public static void TestCrashRecoveryJournalRoundTripAndSafety()
        {
            string root = CreateTempRoot();
            try
            {
                string datasetRoot = Path.Combine(root, "dataset");
                string imagePath = Path.Combine(datasetRoot, "data", "train", "images", "sample.png");
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
                File.WriteAllBytes(imagePath, Enumerable.Range(0, 128).Select(value => (byte)value).ToArray());
                FileInfo imageInfo = new FileInfo(imagePath);

                var service = new WpfCrashRecoveryJournalService(Path.Combine(root, "app-data"));
                WpfCrashRecoveryDraft draft = CreateDraft(
                    imagePath,
                    datasetRoot,
                    imageInfo,
                    DateTime.UtcNow);

                AssertTrue(service.Write(draft, revision: 1), "first crash-recovery draft should be written");
                WpfCrashRecoveryReadResult read = service.ReadAvailable("Recipe-A", datasetRoot);
                AssertEqual(WpfCrashRecoveryReadStatus.Available, read.Status);
                AssertEqual("scratch edit", read.Draft.DirtyReason);
                AssertEqual(1, read.Draft.Boxes.Count);
                AssertEqual(CanvasRoiShapeKind.Ellipse.ToString(), read.Draft.Boxes[0].ShapeKind);
                AssertTrue(read.Draft.Boxes[0].Metadata.IsOccluded, "box occlusion metadata round-trip");
                AssertTrue(
                    new[] { "edge", "review" }.SequenceEqual(read.Draft.Boxes[0].Metadata.Tags, StringComparer.Ordinal),
                    "box tags round-trip");
                AssertEqual("11111111111111111111111111111111", read.Draft.Boxes[0].Metadata.GroupId);
                AssertEqual(2, read.Draft.Segments.Count);
                AssertEqual(3, read.Draft.Segments[0].Points.Count);
                AssertEqual(48, read.Draft.Segments[1].MaskData.Length);

                service.Discard(revision: 2);
                AssertTrue(!File.Exists(service.JournalPath), "discard should remove current journal");
                AssertTrue(!service.Write(draft, revision: 1), "an older queued write must not recreate a discarded journal");
                AssertTrue(service.Write(draft, revision: 3), "a later edit revision should create a new journal");

                string tampered = File.ReadAllText(service.JournalPath)
                    .Replace("scratch edit", "tampered edit", StringComparison.Ordinal);
                File.WriteAllText(service.JournalPath, tampered);
                WpfCrashRecoveryReadResult tamperedRead = service.ReadAvailable("Recipe-A", datasetRoot);
                AssertEqual(WpfCrashRecoveryReadStatus.Invalid, tamperedRead.Status);
                AssertTrue(!File.Exists(service.JournalPath), "tampered journal should leave the active slot");
                AssertTrue(
                    string.IsNullOrWhiteSpace(tamperedRead.QuarantinePath)
                    || File.Exists(tamperedRead.QuarantinePath),
                    "tampered journal should be quarantined when possible");

                var staleService = new WpfCrashRecoveryJournalService(Path.Combine(root, "stale-app-data"));
                WpfCrashRecoveryDraft staleDraft = CreateDraft(
                    imagePath,
                    datasetRoot,
                    imageInfo,
                    DateTime.UtcNow - WpfCrashRecoveryJournalService.MaximumDraftAge - TimeSpan.FromHours(1));
                AssertTrue(staleService.Write(staleDraft, revision: 1), "stale fixture should be writable before read-time retention validation");
                WpfCrashRecoveryReadResult staleRead = staleService.ReadAvailable("Recipe-A", datasetRoot);
                AssertEqual(WpfCrashRecoveryReadStatus.Invalid, staleRead.Status);

                var contextService = new WpfCrashRecoveryJournalService(Path.Combine(root, "context-app-data"));
                AssertTrue(contextService.Write(draft, revision: 1), "context fixture should be written");
                WpfCrashRecoveryReadResult contextRead = contextService.ReadAvailable("Recipe-B", datasetRoot);
                AssertEqual(WpfCrashRecoveryReadStatus.Invalid, contextRead.Status);
            }
            finally
            {
                DeleteTempRoot(root);
            }
        }

        public static void TestCrashRecoveryShellPreservesExplicitSaveAndCandidateBoundaries()
        {
            string root = FindRepositoryRoot();
            string adapterPath = Path.Combine(
                root,
                "0. UI",
                "9) WPF",
                "Views",
                "WpfLabelingShellWindow.CrashRecovery.cs");
            string persistencePath = Path.Combine(
                root,
                "0. UI",
                "9) WPF",
                "Views",
                "WpfLabelingShellWindow.AnnotationPersistence.cs");
            string lifecyclePath = Path.Combine(
                root,
                "0. UI",
                "9) WPF",
                "Views",
                "WpfLabelingShellWindow.ShellLifecycle.cs");
            string servicePath = Path.Combine(
                root,
                "0. UI",
                "9) WPF",
                "Services",
                "Annotation",
                "WpfCrashRecoveryJournalService.cs");
            string dialogControlPath = Path.Combine(
                root,
                "OpenVisionLab",
                "Library",
                "OpenVisionLab.Wpf.MessageDialogs",
                "WpfMessageDialogControl.xaml.cs");

            string adapter = File.ReadAllText(adapterPath);
            string persistence = File.ReadAllText(persistencePath);
            string lifecycle = File.ReadAllText(lifecyclePath);
            string service = File.ReadAllText(servicePath);
            string dialogControl = File.ReadAllText(dialogControlPath);

            AssertTrue(adapter.Contains("비정상 종료 편집 복구", StringComparison.Ordinal), "recovery prompt title");
            AssertTrue(adapter.Contains("편집 복구", StringComparison.Ordinal), "explicit restore action");
            AssertTrue(adapter.Contains("초안 폐기", StringComparison.Ordinal), "explicit discard action");
            AssertTrue(
                adapter.Contains("MarkAnnotationsDirty(\"비정상 종료 편집 복구\")", StringComparison.Ordinal),
                "restored state must remain dirty");
            AssertTrue(
                adapter.Contains("candidateReviewState.ClearAll()", StringComparison.Ordinal),
                "pending candidate state must not be restored");
            AssertTrue(
                !adapter.Contains("SaveCurrentAnnotations(", StringComparison.Ordinal),
                "recovery adapter must not save annotations");
            AssertTrue(
                !adapter.Contains("ExecuteConfirm", StringComparison.Ordinal)
                && !adapter.Contains("candidateConfirmationService", StringComparison.Ordinal),
                "recovery adapter must not confirm AI candidates");
            AssertTrue(
                persistence.Contains("ScheduleCrashRecoveryJournalWrite()", StringComparison.Ordinal),
                "dirty annotations should schedule a journal");
            AssertTrue(
                persistence.Contains("DiscardCrashRecoveryJournal()", StringComparison.Ordinal),
                "explicit label save should clear the journal");
            AssertTrue(
                lifecycle.Contains("TryHandleCrashRecoveryOnStartup()", StringComparison.Ordinal),
                "startup should inspect the journal");
            AssertTrue(
                lifecycle.Contains("DiscardCrashRecoveryJournal();", StringComparison.Ordinal),
                "normal close should clear the journal");
            AssertTrue(
                service.Contains("MaximumDraftAge = TimeSpan.FromDays(7)", StringComparison.Ordinal),
                "journal retention must be bounded");
            AssertTrue(
                service.Contains("File.Move(TemporaryPath, JournalPath, overwrite: true)", StringComparison.Ordinal),
                "journal writes must be atomic");
            AssertTrue(
                service.Contains("PayloadSha256", StringComparison.Ordinal),
                "journal payload must be integrity checked");
            AssertTrue(
                dialogControl.Contains("\"상세 정보\"", StringComparison.Ordinal)
                && dialogControl.Contains("\"상세 정보 닫기\"", StringComparison.Ordinal)
                && !dialogControl.Contains("\"Hide details\"", StringComparison.Ordinal),
                "recovery dialog details toggle should remain operator-readable Korean");

            TestActualShellCrashRecoveryRestore();
        }

        public static WpfCrashRecoveryDraft CreateVisualDraft(string imagePath, string datasetRoot)
        {
            FileInfo imageInfo = new FileInfo(imagePath);
            WpfCrashRecoveryDraft draft = CreateDraft(imagePath, datasetRoot, imageInfo, DateTime.UtcNow);
            draft.RecipeName = "Recovery-Visual";
            draft.DirtyReason = "박스 이동 및 검수 태그 변경";
            return draft;
        }

        private static WpfCrashRecoveryDraft CreateDraft(
            string imagePath,
            string datasetRoot,
            FileInfo imageInfo,
            DateTime createdUtc)
        {
            byte[] mask = new byte[8 * 6];
            mask[10] = 255;
            mask[11] = 255;
            return new WpfCrashRecoveryDraft
            {
                CreatedUtc = createdUtc,
                ApplicationVersion = "0.1.0",
                RecipeName = "Recipe-A",
                DatasetRootPath = datasetRoot,
                ImagePath = imagePath,
                ImageLength = imageInfo.Length,
                ImageLastWriteUtcTicks = imageInfo.LastWriteTimeUtc.Ticks,
                ImageWidth = 8,
                ImageHeight = 6,
                DirtyReason = "scratch edit",
                Boxes = new List<WpfCrashRecoveryBox>
                {
                    new WpfCrashRecoveryBox
                    {
                        ClassName = "Defect",
                        ShapeKind = CanvasRoiShapeKind.Ellipse.ToString(),
                        X = 1,
                        Y = 1,
                        Width = 3,
                        Height = 2,
                        Metadata = new WpfCrashRecoveryMetadata
                        {
                            IsOccluded = true,
                            Tags = new List<string> { "edge", "review" },
                            GroupId = "11111111-1111-1111-1111-111111111111"
                        }
                    }
                },
                Segments = new List<WpfCrashRecoverySegment>
                {
                    new WpfCrashRecoverySegment
                    {
                        ClassName = "Defect",
                        ObjectId = "polygon-1",
                        Points = new List<WpfCrashRecoveryPoint>
                        {
                            new WpfCrashRecoveryPoint { X = 1, Y = 1 },
                            new WpfCrashRecoveryPoint { X = 5, Y = 1 },
                            new WpfCrashRecoveryPoint { X = 3, Y = 4 }
                        },
                        Metadata = new WpfCrashRecoveryMetadata()
                    },
                    new WpfCrashRecoverySegment
                    {
                        ClassName = "Defect",
                        ObjectId = "mask-1",
                        MaskData = mask,
                        MaskWidth = 8,
                        MaskHeight = 6,
                        MaskBoundsX = 2,
                        MaskBoundsY = 1,
                        MaskBoundsWidth = 2,
                        MaskBoundsHeight = 1,
                        Metadata = new WpfCrashRecoveryMetadata
                        {
                            Tags = new List<string> { "mask" }
                        }
                    }
                }
            };
        }

        private static void TestActualShellCrashRecoveryRestore()
        {
            string root = CreateTempRoot();
            string applicationDataRoot = Path.Combine(root, "app-data");
            string datasetRoot = Path.Combine(root, "dataset");
            string imagePath = Path.Combine(datasetRoot, "data", "train", "images", "sample.png");
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
            using (var bitmap = new Bitmap(32, 24))
            {
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.DimGray);
                bitmap.Save(imagePath);
            }

            CData previousData = CGlobal.Inst.Data;
            CRecipe previousRecipe = CGlobal.Inst.Recipe;
            string previousApplicationDataRoot = Environment.GetEnvironmentVariable(
                WpfRuntimeDiagnosticsService.ApplicationDataRootEnvironmentVariable);
            WpfLabelingShellWindow sourceWindow = null;
            WpfLabelingShellWindow restoredWindow = null;
            try
            {
                Environment.SetEnvironmentVariable(
                    WpfRuntimeDiagnosticsService.ApplicationDataRootEnvironmentVariable,
                    applicationDataRoot);
                var testData = new CData();
                testData.ConfigureOutputRoot(datasetRoot);
                testData.ClassNamedList.Add(new CClassItem { Text = "Defect" });
                var testRecipe = new CRecipe { Name = "Recovery-Shell-Test" };
                CGlobal.Inst.Data = testData;
                CGlobal.Inst.Recipe = testRecipe;

                if (System.Windows.Application.Current == null)
                {
                    _ = new System.Windows.Application
                    {
                        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                    };
                }

                sourceWindow = new WpfLabelingShellWindow();
                CGlobal.Inst.Data = testData;
                CGlobal.Inst.Recipe = testRecipe;
                AssertTrue(
                    sourceWindow.TryLoadImage(imagePath, populateQueue: false, refreshQueueDetails: false),
                    "source image should load before recovery capture");
                List<Rectangle> sourceRois =
                    GetPrivateField<List<Rectangle>>(sourceWindow, "manualRois");
                List<string> sourceClasses =
                    GetPrivateField<List<string>>(sourceWindow, "manualRoiClassNames");
                List<CanvasRoiShapeKind> sourceShapes =
                    GetPrivateField<List<CanvasRoiShapeKind>>(sourceWindow, "manualRoiShapeKinds");
                List<string> sourceOverlayIds =
                    GetPrivateField<List<string>>(sourceWindow, "manualRoiOverlayIds");
                sourceRois.Add(new Rectangle(4, 5, 10, 8));
                sourceClasses.Add("Defect");
                sourceShapes.Add(CanvasRoiShapeKind.Rectangle);
                sourceOverlayIds.Add(string.Empty);
                InvokePrivateResult<object>(sourceWindow, "MarkAnnotationsDirty", "shell recovery test");
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(300));
                Task writeTask = GetPrivateField<Task>(sourceWindow, "crashRecoveryWriteTask");
                AssertTrue(writeTask.Wait(TimeSpan.FromSeconds(5)), "shell recovery journal write should finish");

                WpfCrashRecoveryJournalService sourceService =
                    GetPrivateField<WpfCrashRecoveryJournalService>(
                        sourceWindow,
                        "crashRecoveryJournalService");
                WpfCrashRecoveryReadResult read = sourceService.ReadAvailable(
                    "Recovery-Shell-Test",
                    datasetRoot);
                AssertTrue(
                    read.Status == WpfCrashRecoveryReadStatus.Available,
                    $"shell recovery journal should be available: {read.Error} / {read.QuarantinePath}");

                restoredWindow = new WpfLabelingShellWindow();
                CGlobal.Inst.Data = testData;
                CGlobal.Inst.Recipe = testRecipe;
                bool restored = InvokePrivateResult<bool>(
                    restoredWindow,
                    "TryRestoreCrashRecoveryDraft",
                    read.Draft);
                AssertTrue(restored, "actual shell should restore the validated draft");
                List<Rectangle> restoredRois =
                    GetPrivateField<List<Rectangle>>(restoredWindow, "manualRois");
                AssertEqual(1, restoredRois.Count);
                AssertEqual(new Rectangle(4, 5, 10, 8), restoredRois[0]);
                AssertEqual(
                    "비정상 종료 편집 복구",
                    GetPrivateField<string>(restoredWindow, "annotationDirtyReason"));
                WpfCandidateReviewStateService candidateState =
                    GetPrivateField<WpfCandidateReviewStateService>(
                        restoredWindow,
                        "candidateReviewState");
                AssertEqual(0, candidateState.PendingCount);
            }
            finally
            {
                if (sourceWindow != null)
                {
                    SetPrivateField(sourceWindow, "isApplicationCloseApproved", true);
                    sourceWindow.Close();
                }
                if (restoredWindow != null)
                {
                    SetPrivateField(restoredWindow, "isApplicationCloseApproved", true);
                    restoredWindow.Close();
                }
                CGlobal.Inst.Data = previousData;
                CGlobal.Inst.Recipe = previousRecipe;
                Environment.SetEnvironmentVariable(
                    WpfRuntimeDiagnosticsService.ApplicationDataRootEnvironmentVariable,
                    previousApplicationDataRoot);
                DeleteTempRoot(root);
            }
        }
    }
}
