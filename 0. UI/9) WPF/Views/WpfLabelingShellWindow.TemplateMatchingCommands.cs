using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.Wpf.MessageDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow : IWpfTemplateMatchingAutoLabelHost
    {
        bool IWpfTemplateMatchingAutoLabelHost.IsAutoLabelBusy => isBatchDetectionRunning || isDetecting;

        bool IWpfTemplateMatchingAutoLabelHost.IsAutoLabelCloseApproved => isApplicationCloseApproved;

        bool IWpfTemplateMatchingAutoLabelHost.HasActiveAutoLabelImage => activeImageBitmap != null && !activeImageSize.IsEmpty;

        DrawingBitmap IWpfTemplateMatchingAutoLabelHost.ActiveAutoLabelImage => activeImageBitmap;

        string IWpfTemplateMatchingAutoLabelHost.ActiveAutoLabelImagePath => activeImagePath;

        LabelingProjectData IWpfTemplateMatchingAutoLabelHost.AutoLabelData => global.Data;

        int IWpfTemplateMatchingAutoLabelHost.MaximumTemplateMatchingCandidateCount
        {
            get
            {
                int configured = global.Data?.ProjectSettings?.PythonModel?.MaximumDetectionCandidates ?? 20;
                return Math.Clamp(configured, 1, 200);
            }
        }

        bool IWpfTemplateMatchingAutoLabelHost.TryResolveTemplateMatchingSource(out DrawingRectangle templateBounds, out string className)
        {
            return templateMatchingSourceService.TryResolveTemplateMatchingSource(
                CreateTemplateMatchingSourceSnapshot(),
                out templateBounds,
                out className);
        }

        bool IWpfTemplateMatchingAutoLabelHost.TryResolveTemplateMatchingSourceSegment(
            out IReadOnlyList<DrawingPoint> points,
            out IReadOnlyList<IReadOnlyList<DrawingPoint>> cutouts)
        {
            return templateMatchingSourceService.TryResolveTemplateMatchingSourceSegment(
                CreateTemplateMatchingSourceSnapshot(),
                out points,
                out cutouts);
        }

        bool IWpfTemplateMatchingAutoLabelHost.TryResolveTemplateMatchingSourceMask(
            out byte[] maskData,
            out System.Drawing.Size maskSize,
            out DrawingRectangle maskBounds)
        {
            return templateMatchingSourceService.TryResolveTemplateMatchingSourceMask(
                CreateTemplateMatchingSourceSnapshot(),
                out maskData,
                out maskSize,
                out maskBounds);
        }

        private WpfTemplateMatchingSourceSnapshot CreateTemplateMatchingSourceSnapshot()
        {
            TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef selected);
            return new WpfTemplateMatchingSourceSnapshot(
                selected,
                manualRois,
                manualRoiClassNames,
                manualSegments);
        }

        LabelClass IWpfTemplateMatchingAutoLabelHost.EnsureAutoLabelClassItem(string className)
        {
            return EnsureClassItem(className);
        }

        IReadOnlyList<WpfImageQueueItem> IWpfTemplateMatchingAutoLabelHost.GetVisibleAutoLabelQueueItems()
        {
            return GetVisibleQueueItems();
        }

        IReadOnlyList<WpfImageQueueItem> IWpfTemplateMatchingAutoLabelHost.GetAllAutoLabelQueueItems()
        {
            return imageQueueItems.ToList();
        }

        IReadOnlyList<WpfImageQueueItem> IWpfTemplateMatchingAutoLabelHost.BuildAutoLabelBatchQueue(IEnumerable<WpfImageQueueItem> items)
        {
            return detectionTargetService.BuildBatchQueue(items);
        }

        void IWpfTemplateMatchingAutoLabelHost.AppendAutoLabelLog(string message)
        {
            AppendLog(message);
        }

        void IWpfTemplateMatchingAutoLabelHost.ShowAutoLabelGuide(string title, string message)
        {
            SetGlobalInferenceStatus(title ?? string.Empty, isBusy: false, isWarning: true);
            WpfMessageDialog.ShowInfo(
                this,
                string.IsNullOrWhiteSpace(title) ? "\uD15C\uD50C\uB9BF \uC548\uB0B4" : title,
                message ?? string.Empty,
                "\uD655\uC778");
        }

        int IWpfTemplateMatchingAutoLabelHost.ApplyAutoLabelCandidates(
            IReadOnlyList<YoloWorkerSmokeCandidate> candidates,
            bool succeeded,
            DrawingRectangle? sourceSegmentBounds,
            IReadOnlyList<DrawingPoint> sourceSegmentPoints,
            IReadOnlyList<IReadOnlyList<DrawingPoint>> sourceSegmentCutouts,
            byte[] sourceMaskData,
            System.Drawing.Size sourceMaskSize,
            DrawingRectangle sourceMaskBounds)
        {
            IReadOnlyList<YoloWorkerSmokeCandidate> safeCandidates = candidates ?? Array.Empty<YoloWorkerSmokeCandidate>();
            if (!succeeded)
            {
                ApplyDetectionCandidates(safeCandidates, succeeded: false);
                return 0;
            }

            if (succeeded && safeCandidates.Count == 0)
            {
                ApplyTemplateNoCandidateResult();
                return 0;
            }

            return ApplyTemplateLabelCandidates(
                safeCandidates,
                sourceSegmentBounds,
                sourceSegmentPoints,
                sourceSegmentCutouts,
                sourceMaskData,
                sourceMaskSize,
                sourceMaskBounds);
        }

        void IWpfTemplateMatchingAutoLabelHost.SetAutoLabelPythonStatus(string text)
        {
            SetPythonStatus(text);
        }

        void IWpfTemplateMatchingAutoLabelHost.SetAutoLabelCommandStatus(string text, bool isBusy)
        {
            SetYoloCommandStatus(text, isBusy);
        }

        void IWpfTemplateMatchingAutoLabelHost.SetAutoLabelGlobalInferenceStatus(string text, bool isBusy, bool isWarning)
        {
            SetGlobalInferenceStatus(text, isBusy, isWarning);
        }

        CancellationToken IWpfTemplateMatchingAutoLabelHost.StartAutoLabelBatch(int totalCount, string scopeText)
        {
            batchDetectionCts?.Cancel();
            batchDetectionCts?.Dispose();
            batchDetectionCts = new CancellationTokenSource();
            isBatchDetectionRunning = true;
            batchDetectionTotalCount = Math.Max(0, totalCount);
            batchDetectionCompletedCount = 0;
            UpdateBatchDetectionControls(scopeText, string.Empty);
            UpdateYoloCommandButtons();
            return batchDetectionCts.Token;
        }

        void IWpfTemplateMatchingAutoLabelHost.MarkAutoLabelBatchItemRequested(WpfImageQueueItem item)
        {
            if (item == null)
            {
                return;
            }

            string imageName = Path.GetFileNameWithoutExtension(item.ImagePath);
            ApplyReviewStatusToItem(item, imageQualityReviewWorkflowService.SetDetectionRequested(item.ImagePath, imageName));
        }

        void IWpfTemplateMatchingAutoLabelHost.UpdateAutoLabelBatchProgress(
            string scopeText,
            string currentFileName,
            int completedCount,
            int totalCount)
        {
            batchDetectionCompletedCount = Math.Max(0, completedCount);
            batchDetectionTotalCount = Math.Max(0, totalCount);
            UpdateBatchDetectionControls(scopeText, currentFileName);
        }

        void IWpfTemplateMatchingAutoLabelHost.ApplyAutoLabelBatchResult(
            WpfImageQueueItem item,
            TemplateMatchingBatchAutoLabelItemResult result,
            bool saveReviewStatus)
        {
            if (item == null || result == null)
            {
                return;
            }

            YoloImageReviewStatus status;
            string imageName = Path.GetFileNameWithoutExtension(item.ImagePath);
            if (result.Saved)
            {
                status = imageQualityReviewWorkflowService.RefreshLabelStatusAndReviewState(
                    item.ImagePath,
                    result.ImageSize,
                    global.Data,
                    hasActiveCandidates: false)
                    ?? imageQualityReviewWorkflowService.MarkConfirmed(item.ImagePath, imageName);
            }
            else if (result.NoCandidate)
            {
                status = imageQualityReviewWorkflowService.SetDetectionNoCandidates(item.ImagePath, imageName);
            }
            else
            {
                status = imageQualityReviewWorkflowService.SetDetectionFailed(item.ImagePath, imageName, result.Message);
            }

            ApplyReviewStatusToItem(item, status);
            if (saveReviewStatus)
            {
                imageQualityReviewWorkflowService.SaveReviewStatus(global.Data);
            }

            UpdateImageQueueStatusText();
        }

        void IWpfTemplateMatchingAutoLabelHost.SaveAutoLabelReviewStatus()
        {
            imageQualityReviewWorkflowService.SaveReviewStatus(global.Data);
        }

        void IWpfTemplateMatchingAutoLabelHost.CompleteAutoLabelBatch(
            bool canceled,
            int completedCount,
            int totalCount,
            string scopeText)
        {
            isBatchDetectionRunning = false;
            batchDetectionCompletedCount = Math.Max(0, completedCount);
            batchDetectionTotalCount = Math.Max(0, totalCount);
            imageQueueView?.Refresh();
            RefreshActiveImageQueueStatus(hasActiveCandidates: pendingDetectionCandidates.Count > 0);
            UpdateBatchDetectionControls(canceled ? "canceled" : "complete", string.Empty);
            UpdateYoloCommandButtons();
        }

        void IWpfTemplateMatchingAutoLabelHost.NotifyAutoLabelDataChanged()
        {
            global.System?.UpdateData();
        }

        Task IWpfTemplateMatchingAutoLabelHost.YieldAutoLabelBatchFrameAsync(CancellationToken token)
        {
            return YieldBatchDetectionResultFrameAsync(token);
        }

        private void ApplyTemplateNoCandidateResult()
        {
            candidateReviewState.LoadPendingCandidates(Array.Empty<YoloWorkerSmokeCandidate>(), clearConfirmed: true);
            CandidateReviewViewModel?.ClearReviewHistory();
            RefreshCandidateList();
            RefreshObjectList();
            RedrawReviewRois();
            AddCandidateReviewHistory("템플릿 초안 없음: 기준 박스는 결과에서 제외되며, 현재 이미지에서 추가 위치를 찾지 못했습니다.");
            AppendLog("Template matching no candidate: source box excluded, no extra current-image candidate.");

            if (!string.IsNullOrWhiteSpace(activeImagePath) && !activeImageSize.IsEmpty)
            {
                RefreshActiveImageQueueStatus(hasActiveCandidates: false);
            }

            RefreshImageQueueViewAfterItemStateChange();
            UpdateImageQueueStatusText();
        }

        private int ApplyTemplateLabelCandidates(
            IReadOnlyList<YoloWorkerSmokeCandidate> candidates,
            DrawingRectangle? sourceSegmentBounds,
            IReadOnlyList<DrawingPoint> sourceSegmentPoints,
            IReadOnlyList<IReadOnlyList<DrawingPoint>> sourceSegmentCutouts,
            byte[] sourceMaskData,
            System.Drawing.Size sourceMaskSize,
            DrawingRectangle sourceMaskBounds)
        {
            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                return 0;
            }

            var labelsToAdd = new List<(YoloWorkerSmokeCandidate Candidate, DrawingRectangle Bounds)>();
            foreach (YoloWorkerSmokeCandidate candidate in candidates ?? Array.Empty<YoloWorkerSmokeCandidate>())
            {
                DrawingRectangle bounds = WpfCandidateReviewPresentationService.ClipCandidateBounds(candidate, activeImageSize);
                if (bounds.IsEmpty || IsTemplateLabelDuplicate(bounds, WpfCandidateReviewPresenter.GetClassName(candidate), labelsToAdd.Select(item => item.Bounds)))
                {
                    continue;
                }

                labelsToAdd.Add((candidate, bounds));
            }

            if (labelsToAdd.Count == 0)
            {
                ApplyTemplateNoCandidateResult();
                return 0;
            }

            RegisterAnnotationHistoryBeforeChange("Template label");
            candidateReviewState.LoadPendingCandidates(Array.Empty<YoloWorkerSmokeCandidate>(), clearConfirmed: true);
            int addedCount;
            if (IsSegmentationDatasetPurposeActive())
            {
                string className = WpfCandidateReviewPresenter.GetClassName(labelsToAdd[0].Candidate);
                LabelClass classItem = EnsureClassItem(className);
                IReadOnlyDictionary<string, List<LabelingSegmentationObject>> segmentsByClass =
                    TemplateMatchingBatchAutoLabelService.BuildSegmentsByClass(
                        classItem,
                        className,
                        labelsToAdd.Select(item => item.Candidate).ToList(),
                        activeImageSize,
                        sourceSegmentBounds,
                        sourceSegmentPoints,
                        sourceSegmentCutouts,
                        sourceMaskData,
                        sourceMaskSize,
                        sourceMaskBounds);
                List<LabelingSegmentationObject> transferredSegments = segmentsByClass
                    .Values
                    .Where(items => items != null)
                    .SelectMany(items => items)
                    .Where(segment => segment != null)
                    .ToList();
                int nextZOrder = WpfSegmentationZOrderService.GetNextZOrder(manualSegments);
                for (int index = 0; index < transferredSegments.Count; index++)
                {
                    transferredSegments[index].ZOrder = nextZOrder + index;
                }

                manualSegments.AddRange(transferredSegments);
                addedCount = transferredSegments.Count;
            }
            else
            {
                foreach ((YoloWorkerSmokeCandidate candidate, DrawingRectangle bounds) in labelsToAdd)
                {
                    string className = WpfCandidateReviewPresenter.GetClassName(candidate);
                    EnsureClassItem(className);
                    manualRois.Add(bounds);
                    manualRoiClassNames.Add(className);
                    manualRoiShapeKinds.Add(CanvasRoiShapeKind.Rectangle);
                    manualRoiOverlayIds.Add(string.Empty);
                }

                addedCount = labelsToAdd.Count;
            }

            if (addedCount == 0)
            {
                ApplyTemplateNoCandidateResult();
                return 0;
            }

            ApplyCanvasDisplayMode(WpfCanvasDisplayMode.LabelsOnly, redraw: false, logChange: false);
            RefreshCandidateList();
            RefreshObjectList();
            RedrawReviewRois();
            PopulateClassList();
            ShowSavedLabelsWorkflowView();
            SetModelStatus($"템플릿 라벨 초안 생성: {addedCount}개 / 위치 확인 후 라벨 저장");
            AddCandidateReviewHistory($"템플릿 라벨 초안 생성: {addedCount}개 / 저장 전 초안");
            AppendLog($"Template labels added: {addedCount}");
            RefreshImageQueueViewAfterItemStateChange();
            UpdateImageQueueStatusText();
            return addedCount;
        }

        private bool IsTemplateLabelDuplicate(
            DrawingRectangle bounds,
            string className,
            IEnumerable<DrawingRectangle> pendingBounds)
        {
            string normalizedClassName = ClassCatalogService.NormalizeClassName(className);
            foreach (DrawingRectangle pending in pendingBounds ?? Array.Empty<DrawingRectangle>())
            {
                if (WpfCandidateReviewPresenter.CalculateIntersectionOverUnion(bounds, pending) >= 0.9D)
                {
                    return true;
                }
            }

            for (int i = 0; i < manualRois.Count; i++)
            {
                if (string.Equals(ClassCatalogService.NormalizeClassName(GetManualRoiClassName(i)), normalizedClassName, StringComparison.OrdinalIgnoreCase)
                    && WpfCandidateReviewPresenter.CalculateIntersectionOverUnion(bounds, manualRois[i]) >= 0.9D)
                {
                    return true;
                }
            }

            foreach (LabelingSegmentationObject segment in manualSegments)
            {
                if (segment != null
                    && string.Equals(ClassCatalogService.NormalizeClassName(templateMatchingSourceService.GetManualSegmentClassName(segment)), normalizedClassName, StringComparison.OrdinalIgnoreCase)
                    && WpfCandidateReviewPresenter.CalculateIntersectionOverUnion(bounds, segment.Bounds) >= 0.9D)
                {
                    return true;
                }
            }

            foreach (YoloWorkerSmokeCandidate confirmed in confirmedDetectionCandidates)
            {
                if (string.Equals(ClassCatalogService.NormalizeClassName(WpfCandidateReviewPresenter.GetClassName(confirmed)), normalizedClassName, StringComparison.OrdinalIgnoreCase)
                    && WpfCandidateReviewPresenter.CalculateIntersectionOverUnion(
                        bounds,
                        WpfCandidateReviewPresentationService.ClipCandidateBounds(confirmed, activeImageSize)) >= 0.9D)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
