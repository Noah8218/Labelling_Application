using System.Drawing;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteToggleObjectHiddenCommand()
            => ToggleSelectedObjectSessionState(WpfObjectSessionStateKind.Hidden);

        private void ExecuteToggleObjectLockedCommand()
            => ToggleSelectedObjectSessionState(WpfObjectSessionStateKind.Locked);

        private void ExecuteToggleObjectPinnedCommand()
            => ToggleSelectedObjectSessionState(WpfObjectSessionStateKind.Pinned);

        private void ToggleSelectedObjectSessionState(WpfObjectSessionStateKind kind)
        {
            if (!TryGetSelectedObjectReviewItem(out WpfObjectReviewItemRef item))
            {
                const string unsupported = "\uC218\uB3D9 \uBC15\uC2A4 \uB610\uB294 \uC138\uADF8\uBA3C\uD2B8\uB97C \uC120\uD0DD\uD558\uC138\uC694.";
                SetYoloCommandStatus(unsupported, isBusy: false);
                return;
            }

            CompleteMaskAnnotationStroke();
            FlushQueuedMaskStrokeCommits();
            CompleteSelectedSegmentEdit();
            CancelPendingSegmentationRemoveUnderlying(updateStatus: false);
            CancelPendingSegmentationSplit(updateStatus: false);
            CancelPendingSegmentationHoleEdit(updateStatus: false);
            CancelPendingPolygonVertexEdit(updateStatus: false);
            CancelPendingIntelligentScissors(updateStatus: false);
            if (!TryToggleObjectSessionState(item, kind, out WpfObjectSessionState state))
            {
                const string unsupported = "\uC218\uB3D9 \uBC15\uC2A4 \uB610\uB294 \uC138\uADF8\uBA3C\uD2B8\uB97C \uC120\uD0DD\uD558\uC138\uC694.";
                SetYoloCommandStatus(unsupported, isBusy: false);
                return;
            }

            if (state.IsHidden || state.IsLocked)
            {
                MainCanvasViewModel?.ClearRoiSelection();
                MainCanvasViewModel.IsImagePointInputMode = false;
            }

            if (item.Source == WpfObjectReviewSource.ManualRoi)
            {
                RedrawReviewRois();
            }
            else
            {
                RefreshPolygonOverlays();
            }

            RefreshObjectListWithSelection(item);
            string action = FormatObjectSessionStateKind(kind);
            string stateText = kind switch
            {
                WpfObjectSessionStateKind.Hidden => state.IsHidden ? "\uCF1C\uC9D0" : "\uAEBC\uC9D0",
                WpfObjectSessionStateKind.Locked => state.IsLocked ? "\uCF1C\uC9D0" : "\uAEBC\uC9D0",
                WpfObjectSessionStateKind.Pinned => state.IsPinned ? "\uCF1C\uC9D0" : "\uAEBC\uC9D0",
                _ => string.Empty
            };
            string status = $"{action}: {stateText} \u00B7 \uD604\uC7AC \uC774\uBBF8\uC9C0 \uC138\uC158\uB9CC";
            SetYoloCommandStatus(status, isBusy: false);
            AppendLog(status);
        }

        private bool TryToggleObjectSessionState(
            WpfObjectReviewItemRef item,
            WpfObjectSessionStateKind kind,
            out WpfObjectSessionState state)
        {
            state = WpfObjectSessionState.Default;
            if (item == null)
            {
                return false;
            }

            if (item.Source == WpfObjectReviewSource.ManualRoi
                && item.Index >= 0
                && item.Index < manualRois.Count)
            {
                state = objectSessionStateService.ToggleManualRoiState(item.Index, kind);
                return true;
            }

            if (item.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count
                && manualSegments[item.Index] != null)
            {
                state = objectSessionStateService.ToggleManualSegmentState(manualSegments[item.Index], kind);
                return true;
            }

            return false;
        }

        private WpfObjectSessionState GetObjectSessionState(WpfObjectReviewItemRef item)
        {
            if (item?.Source == WpfObjectReviewSource.ManualRoi)
            {
                return objectSessionStateService.GetManualRoiState(item.Index);
            }

            if (item?.Source == WpfObjectReviewSource.ManualSegment
                && item.Index >= 0
                && item.Index < manualSegments.Count)
            {
                return objectSessionStateService.GetManualSegmentState(manualSegments[item.Index]);
            }

            return WpfObjectSessionState.Default;
        }

        private WpfObjectSessionState GetManualRoiSessionState(int index)
            => objectSessionStateService.GetManualRoiState(index);

        private WpfObjectSessionState GetManualSegmentSessionState(int index)
            => index >= 0 && index < manualSegments.Count
                ? objectSessionStateService.GetManualSegmentState(manualSegments[index])
                : WpfObjectSessionState.Default;

        private bool CanMutateSelectedObject(
            WpfObjectReviewItemRef item,
            bool requireVisible,
            out string error)
        {
            WpfObjectSessionState state = GetObjectSessionState(item);
            if (state.IsLocked)
            {
                error = "\uC7A0\uAE34 \uAC1D\uCCB4\uC785\uB2C8\uB2E4. \uC7A0\uAE08\uC744 \uD574\uC81C\uD55C \uD6C4 \uC218\uC815\uD558\uC138\uC694.";
                return false;
            }

            if (requireVisible && state.IsHidden)
            {
                error = "\uC228\uAE34 \uAC1D\uCCB4\uC785\uB2C8\uB2E4. \uD45C\uC2DC\uB97C \uCF20 \uD6C4 \uAD6C\uC870\uB97C \uC218\uC815\uD558\uC138\uC694.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool CanEditManualSegment(LabelingSegmentationObject segment)
        {
            WpfObjectSessionState state = objectSessionStateService.GetManualSegmentState(segment);
            return !state.IsHidden && !state.IsLocked;
        }

        private static string FormatObjectSessionStateKind(WpfObjectSessionStateKind kind)
            => kind switch
            {
                WpfObjectSessionStateKind.Hidden => "\uC228\uAE40",
                WpfObjectSessionStateKind.Locked => "\uC7A0\uAE08",
                WpfObjectSessionStateKind.Pinned => "\uC774\uB3D9 \uACE0\uC815",
                _ => "\uAC1D\uCCB4 \uC138\uC158 \uC0C1\uD0DC"
            };
    }
}
