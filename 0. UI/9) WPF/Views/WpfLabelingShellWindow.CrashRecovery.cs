using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.Wpf.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private bool TryHandleCrashRecoveryOnStartup()
        {
            WpfCrashRecoveryReadResult result = crashRecoveryJournalService.ReadAvailable(
                GetCurrentRecipeName(),
                global.Data?.OutputRootPath);
            if (result.Status == WpfCrashRecoveryReadStatus.None)
            {
                return false;
            }

            if (result.Status == WpfCrashRecoveryReadStatus.Invalid)
            {
                string quarantineDetail = string.IsNullOrWhiteSpace(result.QuarantinePath)
                    ? "손상된 초안은 제거되었습니다."
                    : $"검토용 격리 위치: {result.QuarantinePath}";
                AppendLog($"비정상 종료 복구 초안을 사용할 수 없습니다: {result.Error} {quarantineDetail}");
                return false;
            }

            WpfMessageDialogResult decision = ShowCrashRecoveryPrompt(result.Draft);
            if (decision != WpfMessageDialogResult.Yes)
            {
                DiscardCrashRecoveryJournal();
                AppendLog("비정상 종료 복구 초안을 폐기했습니다.");
                return false;
            }

            if (!TryRestoreCrashRecoveryDraft(result.Draft))
            {
                WpfMessageDialog.Show(this, new WpfMessageDialogOptions
                {
                    Title = "편집 초안을 복구하지 못했습니다",
                    Message = "원본 이미지 또는 현재 Recipe 상태를 확인한 뒤 다시 시작해 주세요.",
                    Details = "복구 초안은 안전을 위해 격리되거나 유지되지 않습니다. 저장된 라벨 파일은 변경하지 않았습니다.",
                    Kind = WpfMessageDialogKind.Warning,
                    Buttons = WpfMessageDialogButtons.OK,
                    PrimaryButtonText = "확인"
                });
                DiscardCrashRecoveryJournal();
                return false;
            }

            return true;
        }

        private WpfMessageDialogResult ShowCrashRecoveryPrompt(WpfCrashRecoveryDraft draft)
        {
            string imageName = Path.GetFileName(draft?.ImagePath ?? string.Empty);
            string localTime = draft == null
                ? "-"
                : draft.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            return WpfMessageDialog.Show(this, new WpfMessageDialogOptions
            {
                Title = "비정상 종료 편집 복구",
                Message = "저장되지 않은 현재 이미지 편집 초안을 발견했습니다.",
                Details =
                    $"Recipe: {draft?.RecipeName}\n" +
                    $"이미지: {imageName}\n" +
                    $"초안 시각: {localTime}\n" +
                    $"편집 사유: {draft?.DirtyReason}\n" +
                    $"객체: 박스 {draft?.Boxes?.Count ?? 0}개, 세그멘테이션 {draft?.Segments?.Count ?? 0}개\n\n" +
                    "복구하면 화면의 미저장 편집 상태로만 돌아옵니다. " +
                    "AI 후보를 승인하거나 라벨 파일을 저장하지 않습니다. 검토 후 `라벨 저장`을 눌러야 합니다.",
                Kind = WpfMessageDialogKind.Warning,
                Buttons = WpfMessageDialogButtons.YesNo,
                DefaultResult = WpfMessageDialogResult.No,
                PrimaryButtonText = "편집 복구",
                SecondaryButtonText = "초안 폐기",
                MaxWidth = 660D
            });
        }

        private bool TryRestoreCrashRecoveryDraft(WpfCrashRecoveryDraft draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.ImagePath))
            {
                return false;
            }

            suppressCrashRecoveryJournal = true;
            try
            {
                if (!TryLoadImage(draft.ImagePath, populateQueue: true))
                {
                    return false;
                }

                WpfAnnotationHistorySnapshot savedState = CaptureAnnotationHistory("비정상 종료 복구");
                ClearAnnotationHistory();
                PushAnnotationHistorySnapshot(savedState, markDirty: false);

                manualRois.Clear();
                manualRoiClassNames.Clear();
                manualRoiShapeKinds.Clear();
                manualRoiOverlayIds.Clear();
                manualSegments.Clear();
                objectMetadataStateService.Clear();
                candidateReviewState.ClearAll();
                smartMaskPromptSession.Reset();

                foreach (WpfCrashRecoveryBox box in draft.Boxes ?? Enumerable.Empty<WpfCrashRecoveryBox>())
                {
                    manualRois.Add(new Rectangle(box.X, box.Y, box.Width, box.Height));
                    manualRoiClassNames.Add(box.ClassName);
                    manualRoiShapeKinds.Add(
                        Enum.TryParse(box.ShapeKind, ignoreCase: true, out CanvasRoiShapeKind shapeKind)
                            ? shapeKind
                            : CanvasRoiShapeKind.Rectangle);
                    manualRoiOverlayIds.Add(string.Empty);
                    objectMetadataStateService.SetManualRoiMetadata(
                        manualRois.Count - 1,
                        ToPersistentMetadata(box.Metadata));
                }

                foreach (WpfCrashRecoverySegment source in draft.Segments
                    ?? Enumerable.Empty<WpfCrashRecoverySegment>())
                {
                    CClassItem classItem = EnsureClassItem(source.ClassName);
                    var segment = new LabelingSegmentationObject
                    {
                        ClassName = classItem?.Text ?? source.ClassName,
                        ClassItem = classItem,
                        ObjectId = source.ObjectId ?? string.Empty,
                        ComponentIndex = source.ComponentIndex,
                        ZOrder = source.ZOrder,
                        LastStructuralOperation = source.LastStructuralOperation ?? string.Empty,
                        Points = (source.Points ?? new List<WpfCrashRecoveryPoint>())
                            .Select(point => new Point(point.X, point.Y))
                            .ToList(),
                        CutoutPolygons = (source.CutoutPolygons
                            ?? new List<List<WpfCrashRecoveryPoint>>())
                            .Select(cutout => (cutout ?? new List<WpfCrashRecoveryPoint>())
                                .Select(point => new Point(point.X, point.Y))
                                .ToList())
                            .ToList(),
                        MaskData = source.MaskData?.ToArray(),
                        MaskSize = new Size(source.MaskWidth, source.MaskHeight),
                        MaskBounds = new Rectangle(
                            source.MaskBoundsX,
                            source.MaskBoundsY,
                            source.MaskBoundsWidth,
                            source.MaskBoundsHeight),
                        RenderVersion = source.MaskData?.Length > 0 ? 1 : 0,
                        RenderDirtyBounds = source.MaskData?.Length > 0
                            ? new Rectangle(0, 0, source.MaskWidth, source.MaskHeight)
                            : Rectangle.Empty
                    };
                    manualSegments.Add(segment);
                    objectMetadataStateService.SetManualSegmentMetadata(
                        segment,
                        ToPersistentMetadata(source.Metadata));
                }

                objectMetadataStateService.DissolveInvalidGroups(manualRois.Count, manualSegments);
                RedrawReviewRois();
                RefreshPolygonOverlays();
                RefreshObjectList();
                RefreshCandidateList();
                PopulateClassList();
                UpdateDetectionResultOverlay();
                RefreshActiveImageQueueStatus(hasActiveCandidates: false);
                SetPythonStatus("추론: 대기 0 / 확정 0");
            }
            catch (Exception ex)
            {
                AppendLog($"비정상 종료 편집 복구 실패: {ex.Message}");
                return false;
            }
            finally
            {
                suppressCrashRecoveryJournal = false;
            }

            MarkAnnotationsDirty("비정상 종료 편집 복구");
            SetYoloCommandStatus("편집 초안 복구 완료 · 검토 후 라벨 저장이 필요합니다.", isBusy: false);
            AppendLog("비정상 종료 편집 초안을 미저장 상태로 복구했습니다. AI 후보 승인이나 라벨 저장은 실행하지 않았습니다.");
            return true;
        }

        private void ScheduleCrashRecoveryJournalWrite()
        {
            if (suppressCrashRecoveryJournal
                || activeImageBitmap == null
                || activeImageSize.IsEmpty
                || string.IsNullOrWhiteSpace(activeImagePath)
                || string.IsNullOrWhiteSpace(annotationDirtyReason))
            {
                return;
            }

            int captureVersion = ++crashRecoveryCaptureVersion;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (captureVersion != crashRecoveryCaptureVersion
                        || suppressCrashRecoveryJournal
                        || string.IsNullOrWhiteSpace(annotationDirtyReason)
                        || HasPendingMaskStrokeCommitWork())
                    {
                        return;
                    }

                    WpfCrashRecoveryDraft draft;
                    try
                    {
                        draft = CaptureCrashRecoveryDraft();
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"비정상 종료 복구 초안 캡처 실패: {ex.Message}");
                        return;
                    }

                    long revision = ++crashRecoveryJournalRevision;
                    crashRecoveryWriteTask = crashRecoveryWriteTask.ContinueWith(
                        _ =>
                        {
                            try
                            {
                                crashRecoveryJournalService.Write(draft, revision);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Crash recovery journal write failed: {ex.Message}");
                            }
                        },
                        TaskScheduler.Default);
                }),
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private WpfCrashRecoveryDraft CaptureCrashRecoveryDraft()
        {
            var imageInfo = new FileInfo(activeImagePath);
            var draft = new WpfCrashRecoveryDraft
            {
                CreatedUtc = DateTime.UtcNow,
                ApplicationVersion = GetType().Assembly.GetName().Version?.ToString() ?? string.Empty,
                RecipeName = GetCurrentRecipeName(),
                DatasetRootPath = global.Data.OutputRootPath,
                ImagePath = activeImagePath,
                ImageLength = imageInfo.Length,
                ImageLastWriteUtcTicks = imageInfo.LastWriteTimeUtc.Ticks,
                ImageWidth = activeImageSize.Width,
                ImageHeight = activeImageSize.Height,
                DirtyReason = annotationDirtyReason
            };

            for (int index = 0; index < manualRois.Count; index++)
            {
                Rectangle bounds = manualRois[index];
                if (bounds.IsEmpty)
                {
                    continue;
                }

                draft.Boxes.Add(new WpfCrashRecoveryBox
                {
                    ClassName = GetManualRoiClassName(index),
                    ShapeKind = index < manualRoiShapeKinds.Count
                        ? manualRoiShapeKinds[index].ToString()
                        : CanvasRoiShapeKind.Rectangle.ToString(),
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Metadata = ToRecoveryMetadata(objectMetadataStateService.GetManualRoiMetadata(index))
                });
            }

            foreach (YoloWorkerSmokeCandidate candidate in confirmedDetectionCandidates)
            {
                Rectangle bounds = GetClippedCandidateBounds(candidate);
                if (bounds.IsEmpty || candidate?.PolygonPoints?.Count >= 3)
                {
                    continue;
                }

                draft.Boxes.Add(new WpfCrashRecoveryBox
                {
                    ClassName = FirstNonEmpty(candidate.ClassName, "Defect"),
                    ShapeKind = CanvasRoiShapeKind.Rectangle.ToString(),
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Metadata = new WpfCrashRecoveryMetadata()
                });
            }

            Dictionary<string, List<LabelingSegmentationObject>> segmentsByClass = BuildAnnotationSegments();
            foreach (LabelingSegmentationObject segment in segmentsByClass
                .Values
                .Where(items => items != null)
                .SelectMany(items => items)
                .Where(item => item != null))
            {
                WpfPersistentObjectMetadata metadata = manualSegments.Contains(segment)
                    ? objectMetadataStateService.GetManualSegmentMetadata(segment)
                    : WpfPersistentObjectMetadata.Default;
                draft.Segments.Add(ToRecoverySegment(segment, metadata));
            }

            return draft;
        }

        private void DiscardCrashRecoveryJournal()
        {
            crashRecoveryCaptureVersion++;
            long revision = ++crashRecoveryJournalRevision;
            crashRecoveryJournalService.Discard(revision);
        }

        private static WpfCrashRecoverySegment ToRecoverySegment(
            LabelingSegmentationObject segment,
            WpfPersistentObjectMetadata metadata)
        {
            Rectangle maskBounds = segment.MaskBounds;
            return new WpfCrashRecoverySegment
            {
                ClassName = FirstNonEmpty(segment.ClassName, segment.ClassItem?.Text, "Defect"),
                ObjectId = segment.ObjectId ?? string.Empty,
                ComponentIndex = segment.ComponentIndex,
                ZOrder = segment.ZOrder,
                LastStructuralOperation = segment.LastStructuralOperation ?? string.Empty,
                Points = (segment.Points ?? new List<Point>())
                    .Select(point => new WpfCrashRecoveryPoint { X = point.X, Y = point.Y })
                    .ToList(),
                CutoutPolygons = (segment.CutoutPolygons ?? new List<List<Point>>())
                    .Select(cutout => (cutout ?? new List<Point>())
                        .Select(point => new WpfCrashRecoveryPoint { X = point.X, Y = point.Y })
                        .ToList())
                    .ToList(),
                MaskData = segment.MaskData?.ToArray() ?? Array.Empty<byte>(),
                MaskWidth = segment.MaskSize.Width,
                MaskHeight = segment.MaskSize.Height,
                MaskBoundsX = maskBounds.X,
                MaskBoundsY = maskBounds.Y,
                MaskBoundsWidth = maskBounds.Width,
                MaskBoundsHeight = maskBounds.Height,
                Metadata = ToRecoveryMetadata(metadata)
            };
        }

        private static WpfCrashRecoveryMetadata ToRecoveryMetadata(WpfPersistentObjectMetadata metadata)
        {
            WpfPersistentObjectMetadata normalized = metadata ?? WpfPersistentObjectMetadata.Default;
            return new WpfCrashRecoveryMetadata
            {
                IsOccluded = normalized.IsOccluded,
                Tags = normalized.Tags.ToList(),
                GroupId = normalized.GroupId
            };
        }

        private static WpfPersistentObjectMetadata ToPersistentMetadata(WpfCrashRecoveryMetadata metadata)
            => metadata == null
                ? WpfPersistentObjectMetadata.Default
                : new WpfPersistentObjectMetadata(
                    metadata.IsOccluded,
                    metadata.Tags,
                    metadata.GroupId);
    }
}
