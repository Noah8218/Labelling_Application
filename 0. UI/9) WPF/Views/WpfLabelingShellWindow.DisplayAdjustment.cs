using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CvMat = OpenCvSharp.Mat;
using CvMatType = OpenCvSharp.MatType;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImageLockMode = System.Drawing.Imaging.ImageLockMode;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using DrawingRectangle = System.Drawing.Rectangle;

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
            ApplyDisplayAdjustmentNow();
        }

        private void ApplyDisplayAdjustmentNow()
        {
            if (activeImageBitmap == null || activeImageSize.IsEmpty)
            {
                return;
            }

            WpfImageDisplayAdjustmentOptions options =
                CanvasPanelViewModel.GetDisplayAdjustmentOptions();
            using DrawingBitmap adjusted =
                imageDisplayAdjustmentService.CreateAdjustedCopy(activeImageBitmap, options);
            using CvMat displayMat = CopyDisplayBitmapToMat(adjusted);
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

        private static CvMat CopyDisplayBitmapToMat(DrawingBitmap bitmap)
        {
            var bounds = new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(
                bounds,
                DrawingImageLockMode.ReadOnly,
                DrawingPixelFormat.Format24bppRgb);
            try
            {
                int rowBytes = bitmap.Width * 3;
                int stride = Math.Abs(data.Stride);
                var source = new byte[stride * bitmap.Height];
                Marshal.Copy(data.Scan0, source, 0, source.Length);
                var compact = new byte[rowBytes * bitmap.Height];
                for (int row = 0; row < bitmap.Height; row++)
                {
                    int sourceRow = data.Stride >= 0 ? row : bitmap.Height - 1 - row;
                    Buffer.BlockCopy(source, sourceRow * stride, compact, row * rowBytes, rowBytes);
                }

                var mat = new CvMat(bitmap.Height, bitmap.Width, CvMatType.CV_8UC3);
                Marshal.Copy(compact, 0, mat.Data, compact.Length);
                return mat;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
