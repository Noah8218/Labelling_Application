using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static partial class Program
{
    private static int RunExeSmartMaskPointSmoke(string[] args)
    {
        try
        {
            string root = FindRepositoryRoot();
            string exePath = Path.GetFullPath(GetArgumentValue(
                args,
                "--exe",
                Path.Combine(root, "artifacts", "run", "Debug", "OpenVisionLab.LabelingStudio.exe")));
            string outputPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--output",
                Path.Combine(root, "artifacts", "ui", "exe-smart-mask-point-after-confirm.png")));
            string beforeConfirmPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--before-confirm-output",
                Path.Combine(root, "artifacts", "ui", "exe-smart-mask-point-before-confirm.png")));
            AssertTrue(File.Exists(exePath), "EXE Smart Mask smoke target was not found. Build the app first.");
            ExecuteExeSmartMaskPointSmoke(exePath, beforeConfirmPath, outputPath);
            Console.WriteLine("EXE_SMART_MASK_POINT_BEFORE_CONFIRM=" + beforeConfirmPath);
            Console.WriteLine("EXE_SMART_MASK_POINT_AFTER_CONFIRM=" + outputPath);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL EXE Smart Mask point smoke: " + error);
            return 1;
        }
    }

    private static void ExecuteExeSmartMaskPointSmoke(
        string exePath,
        string beforeConfirmPath,
        string outputPath)
    {
        Process process = null;
        Dictionary<string, ExeSmokeFileSnapshot> saveSnapshot = null;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
            AssertTrue(process != null, "failed to start Smart Mask EXE smoke process");
            IntPtr handle = WaitForMainWindowHandle(process, TimeSpan.FromSeconds(25));
            AssertTrue(handle != IntPtr.Zero, "Smart Mask EXE smoke window did not appear");
            BringNativeWindowToFront(handle);
            System.Windows.Automation.AutomationElement root =
                System.Windows.Automation.AutomationElement.FromHandle(handle);
            AssertTrue(root != null, "Smart Mask EXE smoke automation root was unavailable");
            WaitForAutomationText(root, "캔버스", TimeSpan.FromSeconds(10));

            TryInvokeAutomationButton(root, "샘플");
            Thread.Sleep(800);
            root = RefreshAutomationRoot(process);
            string smokeImagePath = ResolveSmartMaskSampleImagePath(root, exePath);
            IReadOnlyList<string> smokeOutputRoots = ResolveExeSmokeOutputRoots(smokeImagePath, exePath);
            saveSnapshot = CaptureExeSmokeSaveSnapshot(
                smokeOutputRoots,
                Path.GetFileNameWithoutExtension(smokeImagePath));
            DeleteExeSmokeAnnotationArtifacts(saveSnapshot);
            AssertTrue(
                WaitUntil(
                    () => ContainsAutomationText(
                        RefreshAutomationRoot(process, bringToFront: false),
                        "317/317 이미지"),
                    TimeSpan.FromSeconds(15)),
                "Smart Mask EXE smoke image queue did not finish loading");
            root = RefreshAutomationRoot(process);
            AssertTrue(
                TryInvokeAutomationButtonByAutomationId(root, "NextUnlabeledPrimaryButton"),
                "Smart Mask EXE smoke could not invoke next incomplete before isolated annotation reload");
            AssertTrue(
                WaitUntil(
                    () => !string.Equals(
                        ResolveExeSmokeActiveImagePath(RefreshAutomationRoot(process, bringToFront: false)),
                        smokeImagePath,
                        StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(5)),
                "Smart Mask EXE smoke did not switch away before isolated annotation reload");
            AssertTrue(
                SelectImageQueueItemBySearch(
                    process,
                    Path.GetFileNameWithoutExtension(smokeImagePath),
                    TimeSpan.FromSeconds(5)),
                "Smart Mask EXE smoke could not reload the isolated source image");
            root = RefreshAutomationRoot(process);
            if (!ClickDatasetPurposeByText(root, "세그멘테이션"))
            {
                AssertTrue(
                    SelectAutomationTabByAutomationId(root, "LearningReviewTab")
                        || SelectTabItemByName(root, "가이드/도구"),
                    "guide/tools tab was not selectable before Smart Mask");
                root = RefreshAutomationRoot(process);
                AssertTrue(
                    ClickDatasetPurposeByText(root, "세그멘테이션"),
                    "native click did not select segmentation purpose for Smart Mask");
            }

            root = RefreshAutomationRoot(process);
            System.Windows.Automation.AutomationElement canvas =
                FindAutomationElementByClass(root, "RoiImageCanvasView");
            AssertTrue(canvas != null, "Smart Mask EXE smoke canvas was not found");
            System.Windows.Rect canvasRect = canvas.Current.BoundingRectangle;
            AssertTrue(canvasRect.Width > 300 && canvasRect.Height > 300, "Smart Mask EXE canvas bounds were unusable");
            string activeImagePathForPrompt = ResolveExeSmokeActiveImagePath(root);
            System.Windows.Rect region = File.Exists(activeImagePathForPrompt)
                ? BuildExeSmokeFittedImageRegion(canvasRect, activeImagePathForPrompt)
                : BuildExeSmokeAnnotationRegion(canvasRect);

            System.Windows.Automation.AutomationElement boxTool = null;
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest = RefreshAutomationRoot(process, bringToFront: false);
                        boxTool = FindAnnotationToolItem(latest, "박스");
                        return boxTool != null && boxTool.Current.IsEnabled;
                    },
                    TimeSpan.FromSeconds(4)),
                "Smart Mask EXE smoke did not find the box tool");
            NativeClick(GetAutomationCenter(boxTool));
            AssertTrue(
                WaitUntil(
                    () => string.Equals(
                        GetSelectedAnnotationToolText(RefreshAutomationRoot(process, bringToFront: false)),
                        "박스",
                        StringComparison.Ordinal),
                    TimeSpan.FromSeconds(2)),
                "Smart Mask EXE smoke did not select the box tool");
            Point center = new Point(
                (int)(region.Left + region.Width * 0.50D),
                (int)(region.Top + region.Height * 0.50D));
            int halfWidth = Math.Max(20, (int)(region.Width * 0.16D));
            int halfHeight = Math.Max(30, (int)(region.Height * 0.15D));
            var promptDrag = new List<Point>
            {
                new Point(center.X - halfWidth, center.Y - halfHeight),
                new Point(center.X - halfWidth / 2, center.Y - halfHeight / 2),
                new Point(center.X, center.Y),
                new Point(center.X + halfWidth, center.Y + halfHeight)
            };
            NativeHumanDrag(promptDrag, moveDelayMilliseconds: 5, postMouseUpMilliseconds: 120);

            root = RefreshAutomationRoot(process);
            System.Windows.Automation.AutomationElement create =
                FindAutomationElementByAutomationId(root, "CanvasCreateSmartMaskButton");
            AssertTrue(create != null && create.Current.IsEnabled, "Smart Mask create button was not enabled after drawing a box");
            AssertTrue(
                TryInvokeAutomationButtonByAutomationId(root, "CanvasCreateSmartMaskButton"),
                "Smart Mask create button was not invokable after drawing a box");
            bool initialCandidateReady = WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest = RefreshAutomationRoot(process, bringToFront: false);
                        System.Windows.Automation.AutomationElement rerun =
                            FindAutomationElementByAutomationId(latest, "CanvasCreateSmartMaskButton");
                        return rerun != null
                            && rerun.Current.IsEnabled
                            && string.Equals(rerun.Current.Name, "후보 다시 생성", StringComparison.Ordinal);
                    },
                    TimeSpan.FromSeconds(55));
            if (!initialCandidateReady)
            {
                root = RefreshAutomationRoot(process, bringToFront: false);
                CaptureAutomationRoot(root, beforeConfirmPath);
                create = FindAutomationElementByAutomationId(root, "CanvasCreateSmartMaskButton");
                string modelStatus = GetAutomationValueByAutomationId(root, "ModelStatusText");
                string createName = create?.Current.Name ?? string.Empty;
                string createHelp = GetAutomationHelpText(create);
                throw new InvalidOperationException(
                    $"initial real Smart Mask candidate did not become confirmable; status=\"{modelStatus}\" action=\"{createName}\" detail=\"{createHelp}\"");
            }

            root = RefreshAutomationRoot(process);
            System.Windows.Automation.AutomationElement positive =
                FindAutomationElementByAutomationId(root, "CanvasSmartMaskPositivePointButton");
            AssertTrue(positive != null && positive.Current.IsEnabled, "positive-point button was unavailable");
            NativeClick(GetAutomationCenter(positive));
            NativeClick(new Point(center.X - halfWidth / 4, center.Y));

            root = RefreshAutomationRoot(process);
            System.Windows.Automation.AutomationElement negative =
                FindAutomationElementByAutomationId(root, "CanvasSmartMaskNegativePointButton");
            AssertTrue(negative != null && negative.Current.IsEnabled, "negative-point button was unavailable");
            NativeClick(GetAutomationCenter(negative));
            NativeClick(new Point(center.X + halfWidth * 3 / 4, center.Y));

            root = RefreshAutomationRoot(process);
            AssertTrue(
                ContainsAutomationText(root, "+ 포함 1")
                    && ContainsAutomationText(root, "− 제외 1"),
                "Smart Mask prompt bar did not expose one positive and one negative point");
            create = FindAutomationElementByAutomationId(root, "CanvasCreateSmartMaskButton");
            AssertTrue(create != null && create.Current.IsEnabled, "Smart Mask rerun button was not enabled after point input");
            AssertTrue(
                TryInvokeAutomationButtonByAutomationId(root, "CanvasCreateSmartMaskButton"),
                "Smart Mask rerun button was not invokable after point input");
            bool observedRerunBusy = false;
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest = RefreshAutomationRoot(process, bringToFront: false);
                        System.Windows.Automation.AutomationElement rerun =
                            FindAutomationElementByAutomationId(latest, "CanvasCreateSmartMaskButton");
                        if (rerun != null && !rerun.Current.IsEnabled)
                        {
                            observedRerunBusy = true;
                        }

                        return observedRerunBusy
                            && rerun != null
                            && rerun.Current.IsEnabled;
                    },
                    TimeSpan.FromSeconds(55)),
                "point-corrected Smart Mask candidate rerun did not complete");

            root = RefreshAutomationRoot(process);
            CaptureAutomationRoot(root, beforeConfirmPath);

            System.Windows.Automation.AutomationElement confirmButton =
                FindSmartMaskConfirmButton(root);
            AssertTrue(
                confirmButton != null && confirmButton.Current.IsEnabled,
                "point-corrected candidate confirm button was unavailable; "
                    + DescribeSmartMaskAutomation(root));
            NativeClick(GetAutomationCenter(confirmButton));
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest = RefreshAutomationRoot(process, bringToFront: false);
                        System.Windows.Automation.AutomationElement next =
                            FindAutomationElementByAutomationId(latest, "CanvasSmartMaskNextInstanceButton");
                        return next != null && next.Current.IsEnabled;
                    },
                    TimeSpan.FromSeconds(8)),
                "Smart Mask confirm did not complete the current instance");
            root = RefreshAutomationRoot(process);
            CaptureAutomationRoot(root, outputPath);

            System.Windows.Automation.AutomationElement nextInstance =
                FindAutomationElementByAutomationId(root, "CanvasSmartMaskNextInstanceButton");
            AssertTrue(nextInstance != null && nextInstance.Current.IsEnabled, "next-instance button was unavailable after confirmation");
            AssertTrue(
                TryInvokeAutomationButtonByAutomationId(root, "CanvasSmartMaskNextInstanceButton"),
                "next-instance button was not invokable after confirmation");
            AssertTrue(
                WaitUntil(
                    () =>
                    {
                        System.Windows.Automation.AutomationElement latest = RefreshAutomationRoot(process, bringToFront: false);
                        System.Windows.Automation.AutomationElement createNext =
                            FindAutomationElementByAutomationId(latest, "CanvasCreateSmartMaskButton");
                        return createNext != null
                            && string.Equals(createNext.Current.Name, "박스 → 스마트 마스크", StringComparison.Ordinal)
                            && string.Equals(GetSelectedAnnotationToolText(latest), "박스", StringComparison.Ordinal);
                    },
                    TimeSpan.FromSeconds(4)),
                "next-instance transition did not restore box prompt mode");
        }
        finally
        {
            if (saveSnapshot != null)
            {
                RestoreExeSmokeSaveSnapshot(saveSnapshot);
            }
            CloseExeSmokeProcess(process);
        }
    }

    private static System.Windows.Automation.AutomationElement FindSmartMaskConfirmButton(
        System.Windows.Automation.AutomationElement root)
        => FindAutomationElementByAutomationId(root, "CanvasOverlayConfirmSelectedCandidateButton")
            ?? FindEnabledAutomationButton(root, "선택 확정")
            ?? FindEnabledAutomationButton(root, "라벨 확정")
            ?? FindEnabledAutomationButton(root, "확정");

    private static string ResolveSmartMaskSampleImagePath(
        System.Windows.Automation.AutomationElement root,
        string exePath)
    {
        var recipePattern = new System.Text.RegularExpressions.Regex(
            @"\bcodex_exe_industrial_object_[0-9a-f]{32}\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        string dataRoot = Path.Combine(Path.GetDirectoryName(exePath) ?? string.Empty, "DATA");
        foreach (System.Windows.Automation.AutomationElement element in EnumerateAutomationDescendants(root))
        {
            string name;
            try
            {
                name = element.Current.Name ?? string.Empty;
            }
            catch (System.Windows.Automation.ElementNotAvailableException)
            {
                continue;
            }

            System.Text.RegularExpressions.Match match = recipePattern.Match(name);
            if (!match.Success)
            {
                continue;
            }

            string candidate = Path.Combine(
                dataRoot,
                match.Value,
                "data",
                "train",
                "images",
                "kos01_Part0.jpg");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Smart Mask EXE smoke could not resolve the current sample image path.");
    }

    private static string DescribeSmartMaskAutomation(
        System.Windows.Automation.AutomationElement root)
    {
        var description = new StringBuilder();
        int count = 0;
        foreach (System.Windows.Automation.AutomationElement element in EnumerateAutomationDescendants(root))
        {
            try
            {
                string name = element.Current.Name ?? string.Empty;
                string id = element.Current.AutomationId ?? string.Empty;
                if (!name.Contains("확정", StringComparison.Ordinal)
                    && !name.Contains("후보", StringComparison.Ordinal)
                    && !id.Contains("Candidate", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (description.Length > 0)
                {
                    description.Append(" | ");
                }
                description
                    .Append(element.Current.ControlType.ProgrammaticName)
                    .Append(" name=\"").Append(name)
                    .Append("\" id=\"").Append(id)
                    .Append("\" enabled=").Append(element.Current.IsEnabled)
                    .Append(" offscreen=").Append(element.Current.IsOffscreen);
                count++;
                if (count >= 40)
                {
                    break;
                }
            }
            catch (System.Windows.Automation.ElementNotAvailableException)
            {
            }
        }

        return description.ToString();
    }
}
