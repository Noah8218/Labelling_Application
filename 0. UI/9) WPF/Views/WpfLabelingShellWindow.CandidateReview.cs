using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using OpenVisionLab.ImageCanvas.ViewModels;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingRectangleF = System.Drawing.RectangleF;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // AI candidate review is grouped here so selection, confirmation, and overlay state stay traceable as one workflow.
        private YoloWorkerSmokeCandidate GetSelectedCandidate()
            => WpfCandidateReviewSelectionService.GetSelectedCandidate(
                viewModels.CandidateReviewViewModel?.SelectedCandidate);

        private void UpdateDetectionResultOverlay()
        {
            if (CanvasPanelViewModel == null)
            {
                return;
            }

            if (!ShouldShowInferenceOverlays())
            {
                CanvasPanelViewModel.ClearDetectionOverlay();
                return;
            }

            WpfDetectionOverlayPresentation presentation = candidateReviewPresentationService.BuildOverlayPresentation(
                activeImagePath,
                pendingDetectionCandidates,
                GetSelectedCandidate(),
                GetCandidateConfidenceFilter(),
                IsCandidateHighOverlap,
                IsCandidateConfirmable,
                candidate => WpfCandidateReviewPresenter.BuildSecondaryText(
                    candidate,
                    WpfCandidateReviewPresentationService.ClipCandidateBounds(candidate, activeImageSize),
                    GetCandidateOverlapInfo(candidate),
                    GetMinimumDetectionConfidence()));
            if (presentation.IsEmpty)
            {
                CanvasPanelViewModel.ClearDetectionOverlay();
                return;
            }

            CanvasPanelViewModel.SetDetectionOverlay(
                presentation.Title,
                presentation.Summary,
                presentation.SelectedText,
                presentation.Detail,
                presentation.Status);
        }

        private void MainCanvasViewModel_DetectionOverlayClicked(object sender, int candidateIndex)
        {
            if (isApplicationCloseApproved
                || candidateIndex < 0
                || candidateIndex >= pendingDetectionCandidates.Count)
            {
                return;
            }

            YoloWorkerSmokeCandidate candidate = pendingDetectionCandidates[candidateIndex];
            RefreshCandidateListWithPreferred(candidate);
            ShowCandidateReviewWorkflowView();
            CandidateListBox?.ScrollIntoView(CandidateReviewViewModel?.SelectedCandidate);
            ApplyCandidateSelectionReview(candidate);
            UpdateDetectionResultOverlay();
            RedrawReviewRois();
            SetModelStatus($"AI 후보 선택: {WpfCandidateReviewPresenter.FormatCandidate(
                candidate,
                WpfCandidateReviewPresentationService.ClipCandidateBounds(candidate, activeImageSize))}");
        }

        private void ExecuteCandidateConfidenceChangedCommand(double confidence)
        {
            UpdateCandidateConfidenceText();
            if (CandidateListBox == null)
            {
                return;
            }

            RefreshCandidateList();
        }




    }
}
