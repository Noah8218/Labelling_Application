using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingColor = System.Drawing.Color;
using DrawingImage = System.Drawing.Image;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;
using MediaColor = System.Windows.Media.Color;
using MediaDrawingImage = System.Windows.Media.DrawingImage;
using MediaPen = System.Windows.Media.Pen;
using MediaPoint = System.Windows.Point;
using MediaRect = System.Windows.Rect;

namespace MvcVisionSystem
{
    public sealed class WpfDatasetVisualQaService
    {
        public const int MaximumCatalogItemCount = 500;
        public const int HealthySampleCount = 48;
        private static readonly string[] Splits = { "train", "valid", "test" };
        private static readonly HashSet<string> ImageExtensions =
            new HashSet<string>(new[] { ".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff" }, StringComparer.OrdinalIgnoreCase);

        public WpfDatasetVisualQaCatalog BuildCatalog(CData data)
        {
            if (data == null)
            {
                return new WpfDatasetVisualQaCatalog(Array.Empty<WpfDatasetVisualQaItem>(), 0, 0, false);
            }

            data.NormalizeOutputPaths();
            return data.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.AnomalyDetection
                ? BuildAnomalyCatalog(data)
                : BuildYoloCatalog(data);
        }

        private static WpfDatasetVisualQaCatalog BuildYoloCatalog(CData data)
        {
            var problems = new List<WpfDatasetVisualQaItem>();
            var samples = new List<WpfDatasetVisualQaItem>();
            int scanned = 0;
            int problemCount = 0;
            foreach (string split in Splits)
            {
                string imageDirectory = Path.Combine(data.OutputRootPath ?? string.Empty, "data", split, "images");
                foreach (string imagePath in EnumerateImages(imageDirectory, SearchOption.TopDirectoryOnly))
                {
                    scanned++;
                    WpfDatasetVisualQaItem item = BuildYoloItem(data, split, imagePath);
                    if (item.IsProblem)
                    {
                        problemCount++;
                        if (problems.Count < MaximumCatalogItemCount)
                        {
                            problems.Add(item);
                        }
                    }
                    else if (samples.Count < HealthySampleCount)
                    {
                        samples.Add(item);
                    }
                }
            }

            WpfDatasetVisualQaItem[] items = problems
                .Concat(samples)
                .Take(MaximumCatalogItemCount)
                .ToArray();
            return new WpfDatasetVisualQaCatalog(
                items,
                scanned,
                problemCount,
                problemCount > problems.Count || problems.Count + samples.Count > MaximumCatalogItemCount);
        }

        private static WpfDatasetVisualQaItem BuildYoloItem(CData data, string split, string imagePath)
        {
            try
            {
                DrawingSize imageSize;
                using (DrawingImage image = DrawingImage.FromFile(imagePath))
                {
                    imageSize = image.Size;
                }

                YoloImageLabelStatus status = YoloImageLabelStatusService.Build(imagePath, imageSize, data);
                bool isSegmentation = data.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.Segmentation;
                string segmentPath = isSegmentation
                    ? YoloSegmentationAnnotationService.GetCandidateSegmentPaths(imagePath, data).FirstOrDefault(File.Exists)
                    : string.Empty;
                bool isMissing = isSegmentation ? string.IsNullOrWhiteSpace(segmentPath) : !status.HasLabelFile;
                bool isInvalid = status.InvalidLineCount > 0
                    || isSegmentation && IsInvalidSegmentationAnnotation(segmentPath, data?.ClassNamedList?.Count ?? 0);
                bool isProblem = isMissing || isInvalid;
                string statusText = isMissing
                    ? "라벨 누락"
                    : isInvalid
                        ? "라벨 오류"
                        : status.ObjectCount == 0
                            ? "빈 라벨 검토"
                            : "저장 라벨 표본";
                string detail = isMissing
                    ? "이미지와 짝이 되는 저장 annotation이 없습니다."
                    : isInvalid
                        ? isSegmentation
                            ? "저장된 세그먼트 JSON의 geometry 또는 클래스 정보를 해석할 수 없습니다."
                            : $"해석할 수 없는 라벨 {status.InvalidLineCount}줄"
                        : status.ObjectCount == 0
                            ? "배경 이미지로 의도한 빈 라벨인지 확인하세요."
                            : "저장된 geometry를 읽기 전용으로 확인합니다.";
                return CreateItem(data, imagePath, split, statusText, detail, status.ObjectCount, isProblem);
            }
            catch (Exception ex)
            {
                return CreateItem(data, imagePath, split, "파일/annotation 오류", ex.Message, 0, true);
            }
        }

        private static bool IsInvalidSegmentationAnnotation(string segmentPath, int classCount)
        {
            if (string.IsNullOrWhiteSpace(segmentPath) || !File.Exists(segmentPath))
            {
                return false;
            }

            try
            {
                SegmentationAnnotationFile annotation =
                    JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
                return annotation?.Polygons == null
                    || annotation.Polygons.Any(record =>
                        record == null
                        || record.Points == null
                        || record.Points.Count < 3
                        || record.ClassIndex < 0
                        || classCount > 0 && record.ClassIndex >= classCount);
            }
            catch
            {
                return true;
            }
        }

        private static WpfDatasetVisualQaCatalog BuildAnomalyCatalog(CData data)
        {
            string root = data.ProjectSettings?.PythonModel?.ImageRootPath;
            string[] imagePaths = EnumerateImages(root, SearchOption.AllDirectories).ToArray();
            var review = new AnomalyImageReviewStatusService();
            review.LoadReviewStatus(data, imagePaths);
            List<AnomalyImageReviewStatus> statuses = review.GetItems().ToList();
            int problemCount = statuses.Count(item => !item.IsReviewed);
            WpfDatasetVisualQaItem[] items = statuses
                .OrderBy(item => item.IsReviewed)
                .ThenBy(item => item.ImagePath, StringComparer.OrdinalIgnoreCase)
                .Take(MaximumCatalogItemCount)
                .Select(item => new WpfDatasetVisualQaItem(
                    item.ImagePath,
                    "원본",
                    item.IsReviewed ? FormatAnomalyState(item.ReviewState) : "미검토",
                    item.IsReviewed ? "저장된 이미지 판정 표본입니다." : "학습 전에 정상 또는 이상으로 판정하세요.",
                    0,
                    !item.IsReviewed,
                    () => CreatePreview(item.ImagePath, data)))
                .ToArray();
            return new WpfDatasetVisualQaCatalog(
                items,
                imagePaths.Length,
                problemCount,
                imagePaths.Length > MaximumCatalogItemCount);
        }

        private static WpfDatasetVisualQaItem CreateItem(
            CData data,
            string imagePath,
            string split,
            string status,
            string detail,
            int objectCount,
            bool isProblem)
            => new WpfDatasetVisualQaItem(
                imagePath,
                split,
                status,
                detail,
                objectCount,
                isProblem,
                () => CreatePreview(imagePath, data));

        internal static ImageSource CreatePreview(string imagePath, CData data)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                DrawingSize sourceSize;
                using (DrawingImage source = DrawingImage.FromFile(imagePath))
                {
                    sourceSize = source.Size;
                }

                BitmapImage bitmap = LoadBitmap(imagePath);
                var drawing = new DrawingGroup();
                drawing.Children.Add(new ImageDrawing(bitmap, new MediaRect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight)));
                double scaleX = bitmap.PixelWidth / (double)Math.Max(1, sourceSize.Width);
                double scaleY = bitmap.PixelHeight / (double)Math.Max(1, sourceSize.Height);
                if (data?.ProjectSettings?.DatasetPurpose == LabelingDatasetPurpose.Segmentation)
                {
                    DrawSegments(drawing, imagePath, sourceSize, data, scaleX, scaleY);
                }
                else if (data?.ProjectSettings?.DatasetPurpose != LabelingDatasetPurpose.AnomalyDetection)
                {
                    DrawBoxes(drawing, imagePath, sourceSize, data, scaleX, scaleY);
                }

                drawing.Freeze();
                var result = new MediaDrawingImage(drawing);
                result.Freeze();
                return result;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage LoadBitmap(string imagePath)
        {
            var bitmap = new BitmapImage();
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.DecodePixelWidth = 800;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static void DrawBoxes(
            DrawingGroup drawing,
            string imagePath,
            DrawingSize imageSize,
            CData data,
            double scaleX,
            double scaleY)
        {
            string labelPath = YoloAnnotationService.GetCandidateLabelPaths(imagePath, data).FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(labelPath))
            {
                return;
            }

            foreach (string line in File.ReadLines(labelPath))
            {
                if (!YoloAnnotationService.TryParseYoloLine(line, imageSize, out int classIndex, out DrawingRectangle bounds))
                {
                    continue;
                }

                DrawRectangle(drawing, bounds, scaleX, scaleY, ResolveColor(data, classIndex));
            }
        }

        private static void DrawSegments(
            DrawingGroup drawing,
            string imagePath,
            DrawingSize imageSize,
            CData data,
            double scaleX,
            double scaleY)
        {
            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> groups =
                YoloSegmentationAnnotationService.LoadSegmentationObjectsForImage(imagePath, data?.ClassNamedList, data, imageSize);
            IEnumerable<List<LabelingSegmentationObject>> segmentGroups =
                groups?.Values ?? Enumerable.Empty<List<LabelingSegmentationObject>>();
            foreach (LabelingSegmentationObject segment in segmentGroups
                .Where(list => list != null)
                .SelectMany(list => list)
                .Where(item => item != null))
            {
                MediaColor color = ToMediaColor(segment.Color);
                if (segment.IsRasterMask)
                {
                    foreach (SegmentationGeometry.SegmentationMaskRegion region in RasterMaskPolygonService.BuildRegions(
                        segment.MaskData,
                        segment.MaskSize,
                        imageSize))
                    {
                        DrawPolygon(drawing, region.Points, scaleX, scaleY, color);
                        foreach (IReadOnlyList<DrawingPoint> cutout in region.Cutouts)
                        {
                            DrawPolygon(drawing, cutout, scaleX, scaleY, color);
                        }
                    }
                }
                else
                {
                    DrawPolygon(drawing, segment.Points, scaleX, scaleY, color);
                    foreach (IReadOnlyList<DrawingPoint> cutout in segment.CutoutPolygons)
                    {
                        DrawPolygon(drawing, cutout, scaleX, scaleY, color);
                    }
                }
            }
        }

        private static void DrawRectangle(
            DrawingGroup drawing,
            DrawingRectangle bounds,
            double scaleX,
            double scaleY,
            MediaColor color)
        {
            MediaPen pen = CreatePen(color);
            drawing.Children.Add(new GeometryDrawing(
                null,
                pen,
                new RectangleGeometry(new MediaRect(
                    bounds.X * scaleX,
                    bounds.Y * scaleY,
                    bounds.Width * scaleX,
                    bounds.Height * scaleY))));
        }

        private static void DrawPolygon(
            DrawingGroup drawing,
            IEnumerable<DrawingPoint> points,
            double scaleX,
            double scaleY,
            MediaColor color)
        {
            MediaPoint[] scaled = (points ?? Enumerable.Empty<DrawingPoint>())
                .Select(point => new MediaPoint(point.X * scaleX, point.Y * scaleY))
                .ToArray();
            if (scaled.Length < 2)
            {
                return;
            }

            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(scaled[0], isFilled: false, isClosed: true);
                context.PolyLineTo(scaled.Skip(1).ToArray(), isStroked: true, isSmoothJoin: false);
            }
            geometry.Freeze();
            drawing.Children.Add(new GeometryDrawing(null, CreatePen(color), geometry));
        }

        private static MediaPen CreatePen(MediaColor color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            var pen = new MediaPen(brush, 2.5);
            pen.Freeze();
            return pen;
        }

        private static MediaColor ResolveColor(CData data, int classIndex)
        {
            DrawingColor color = classIndex >= 0 && classIndex < (data?.ClassNamedList?.Count ?? 0)
                ? data.ClassNamedList[classIndex]?.DrawColor ?? DrawingColor.LimeGreen
                : DrawingColor.LimeGreen;
            return ToMediaColor(color);
        }

        private static MediaColor ToMediaColor(DrawingColor color)
            => MediaColor.FromArgb(color.A, color.R, color.G, color.B);

        private static IEnumerable<string> EnumerateImages(string root, SearchOption option)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(root, "*", option)
                .Where(path => ImageExtensions.Contains(Path.GetExtension(path)))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
        }

        private static string FormatAnomalyState(AnomalyImageReviewState state)
            => state == AnomalyImageReviewState.Normal ? "정상(OK)" : "이상(NG)";
    }
}
