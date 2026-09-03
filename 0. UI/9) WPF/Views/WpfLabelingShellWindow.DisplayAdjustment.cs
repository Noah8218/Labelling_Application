using System;
using CvMat = OpenCvSharp.Mat;
using DrawingBitmap = System.Drawing.Bitmap;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ScheduleDisplayAdjustmentRefresh()
        {
            displayAdjustmentRefreshTimer.Stop();
            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                return;
            }

            displayAdjustmentRefreshTimer.Start();
        }

        private void DisplayAdjustmentRefreshTimer_Tick(object sender, EventArgs e)
        {
            displayAdjustmentRefreshTimer.Stop();
            if (isApplicationCloseApproved)
            {
                return;
            }

            ApplyDisplayAdjustmentNow();
        }

        private void ApplyDisplayAdjustmentNow()
        {
            if (isApplicationCloseApproved
                || activeImageBitmap == null
                || activeImageSize.IsEmpty)
            {
                return;
            }

            WpfImageDisplayAdjustmentOptions options =
                CanvasPanelViewModel.GetDisplayAdjustmentOptions();
            using DrawingBitmap adjusted =
                imageDisplayAdjustmentService.CreateAdjustedCopy(activeImageBitmap, options);
            using CvMat displayMat = WpfBitmapMatConversionService.CopyToMat(adjusted);
            using (MainCanvasViewModel.ImageViewer.SuppressRefresh())
            {
                MainCanvasViewModel.LoadImage(
                    displayMat,
                    string.IsNullOrWhiteSpace(activeImagePath)
                        ? "display-preview"
                        : System.IO.Path.GetFileName(activeImagePath));
            }
            MainCanvasViewModel.ImageViewer.RefreshGL();
        }

    }
}
