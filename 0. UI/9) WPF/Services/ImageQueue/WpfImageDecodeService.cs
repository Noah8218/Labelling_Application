using MvcVisionSystem.Yolo;
using System;
using CvMat = OpenCvSharp.Mat;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MvcVisionSystem
{
    public sealed class WpfImageDecodeService
    {
        public WpfCachedDecodedImage DecodeForCanvas(string imagePath)
            => DecodeCore(imagePath, long.MaxValue);

        public WpfCachedDecodedImage TryDecodeForCache(string imagePath)
            => TryDecodeForCache(imagePath, WpfImageDecodeCacheService.DefaultMaxPixels);

        public WpfCachedDecodedImage TryDecodeForCache(string imagePath, long maxPixels)
        {
            try
            {
                return DecodeCore(imagePath, Math.Max(1L, maxPixels));
            }
            catch
            {
                return null;
            }
        }

        private static WpfCachedDecodedImage DecodeCore(string imagePath, long maxPixels)
        {
            DrawingBitmap workspaceBitmap = null;
            CvMat imageMat = null;
            try
            {
                using DrawingBitmap loaded = AppImageLoader.LoadBitmap(imagePath);
                if ((long)loaded.Width * loaded.Height > maxPixels)
                {
                    return null;
                }

                // Decode ownership lives here so Shell and preload paths cannot drift on clone format or Mat lifetime rules.
                workspaceBitmap = loaded.Clone(
                    new DrawingRectangle(0, 0, loaded.Width, loaded.Height),
                    DrawingPixelFormat.Format24bppRgb);
                imageMat = WpfBitmapMatConversionService.CopyToMat(workspaceBitmap);
                return new WpfCachedDecodedImage(imagePath, workspaceBitmap, imageMat);
            }
            catch
            {
                workspaceBitmap?.Dispose();
                imageMat?.Dispose();
                throw;
            }
        }

    }
}
