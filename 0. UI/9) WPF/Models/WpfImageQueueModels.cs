using MahApps.Metro.IconPacks;
using MvcVisionSystem.Yolo;
using OpenVisionLab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using DrawingSize = System.Drawing.Size;

namespace MvcVisionSystem
{
    public sealed class WpfImageQueueCatalogEntry
    {
        private WpfImageQueueCatalogEntry(
            string imagePath,
            string fileName,
            string folderName,
            string fileSize,
            string modified)
        {
            ImagePath = imagePath ?? string.Empty;
            FileName = fileName ?? string.Empty;
            FolderName = folderName ?? string.Empty;
            FileSize = fileSize ?? string.Empty;
            Modified = modified ?? string.Empty;
        }

        public string ImagePath { get; }

        public string FileName { get; }

        public string FolderName { get; }

        public string FileSize { get; }

        public string Modified { get; }

        public static WpfImageQueueCatalogEntry Create(string imagePath)
        {
            FileInfo fileInfo = new FileInfo(imagePath ?? string.Empty);
            return new WpfImageQueueCatalogEntry(
                imagePath,
                Path.GetFileName(imagePath),
                fileInfo.Directory?.Name,
                FormatFileSize(fileInfo.Exists ? fileInfo.Length : 0),
                fileInfo.Exists
                    ? fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty);
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / 1024D / 1024D:0.#} MB";
            }

            if (bytes >= 1024)
            {
                return $"{bytes / 1024D:0.#} KB";
            }

            return $"{Math.Max(0, bytes)} B";
        }
    }

    public sealed class WpfImageQueueItem : INotifyPropertyChanged
    {
        internal static readonly Brush ErrorBrush = CreateFrozenBrush("#FF5A5F");
        internal static readonly Brush InfoBrush = CreateFrozenBrush("#4EA1FF");
        internal static readonly Brush MutedBrush = CreateFrozenBrush("#7A8491");
        internal static readonly Brush SuccessBrush = CreateFrozenBrush("#57C785");
        internal static readonly Brush WarningBrush = CreateFrozenBrush("#FFC857");
        internal static readonly Brush ErrorBadgeBrush = CreateFrozenBrush("#3D1F25");
        internal static readonly Brush InfoBadgeBrush = CreateFrozenBrush("#17314A");
        internal static readonly Brush MutedBadgeBrush = CreateFrozenBrush("#242B35");
        internal static readonly Brush SuccessBadgeBrush = CreateFrozenBrush("#173B29");
        internal static readonly Brush WarningBadgeBrush = CreateFrozenBrush("#403316");
        internal static readonly Brush TransparentBrush = CreateFrozenBrush("#00000000");

        private string labelStatus = "확인중";
        private string detectStatus = "대기";
        private string dimensions = string.Empty;
        private string detail = string.Empty;
        private string queueStatusSummary = "상태 확인 중";
        private string queueBadgeText = string.Empty;
        private PackIconMaterialKind queueIconKind = PackIconMaterialKind.ImageOutline;
        private Brush queueIconBrush = MutedBrush;
        private Brush queueBadgeBackgroundBrush = TransparentBrush;
        private Brush queueRowAccentBrush = TransparentBrush;
        private bool isLabeled;
        private bool isSaveRequired;
        private YoloImageReviewState reviewState;
        private YoloImageQualityReviewState qualityReviewState;
        private AnomalyImageReviewState anomalyReviewState;
        private ImageSource thumbnailSource;
        private bool thumbnailLoadAttempted;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ImagePath { get; private set; } = string.Empty;

        public string FileName { get; private set; } = string.Empty;

        public string DisplayFileName => FormatCompactFileName(FileName);

        public string FolderName { get; private set; } = string.Empty;

        public string FileSize { get; private set; } = string.Empty;

        public string Modified { get; private set; } = string.Empty;

        public ImageSource ThumbnailSource
        {
            get
            {
                if (!thumbnailLoadAttempted)
                {
                    thumbnailLoadAttempted = true;
                    thumbnailSource = CreateThumbnailSource(ImagePath);
                }

                return thumbnailSource;
            }
        }

        public string LabelStatus
        {
            get => labelStatus;
            set => SetField(ref labelStatus, value ?? string.Empty);
        }

        public string DetectStatus
        {
            get => detectStatus;
            set => SetField(ref detectStatus, value ?? string.Empty);
        }

        public string LocalizedLabelStatus => LocalizeStatus(labelStatus, "WpfImageQueue.Row.SavePrefix");

        public string LocalizedDetectStatus => LocalizeStatus(detectStatus, "WpfImageQueue.Row.InspectPrefix");

        public string Dimensions
        {
            get => dimensions;
            set => SetField(ref dimensions, value ?? string.Empty);
        }

        public string Detail
        {
            get => detail;
            set => SetField(ref detail, value ?? string.Empty);
        }

        public string QueueRowToolTip => BuildQueueRowText(Environment.NewLine);

        public string QueueRowAccessibleName => BuildQueueRowText(" / ");

        public string LocalizedQueueRowToolTip => BuildLocalizedQueueRowText(Environment.NewLine);

        public string LocalizedQueueRowAccessibleName => BuildLocalizedQueueRowText(" / ");

        public string QueueStatusSummary
        {
            get => queueStatusSummary;
            set => SetField(ref queueStatusSummary, value ?? string.Empty);
        }

        public string QueueBadgeText
        {
            get => queueBadgeText;
            set => SetField(ref queueBadgeText, value ?? string.Empty);
        }

        public string LocalizedQueueBadgeText => LocalizeBadgeText(queueBadgeText);

        public string LocalizedQueueStatusSummary => LocalizeSummary(queueStatusSummary);

        public PackIconMaterialKind QueueIconKind
        {
            get => queueIconKind;
            set => SetField(ref queueIconKind, value);
        }

        public Brush QueueIconBrush
        {
            get => queueIconBrush;
            set => SetField(ref queueIconBrush, value ?? MutedBrush);
        }

        public Brush QueueBadgeBackgroundBrush
        {
            get => queueBadgeBackgroundBrush;
            set => SetField(ref queueBadgeBackgroundBrush, value ?? TransparentBrush);
        }

        public Brush QueueRowAccentBrush
        {
            get => queueRowAccentBrush;
            set => SetField(ref queueRowAccentBrush, value ?? TransparentBrush);
        }

        public bool IsLabeled
        {
            get => isLabeled;
            set => SetField(ref isLabeled, value);
        }

        public bool IsSaveRequired
        {
            get => isSaveRequired;
            set => SetField(ref isSaveRequired, value);
        }

        public YoloImageReviewState ReviewState
        {
            get => reviewState;
            set => SetField(ref reviewState, value);
        }

        public YoloImageQualityReviewState QualityReviewState
        {
            get => qualityReviewState;
            set => SetField(ref qualityReviewState, value);
        }

        public AnomalyImageReviewState AnomalyReviewState
        {
            get => anomalyReviewState;
            set => SetField(ref anomalyReviewState, value);
        }

        internal void RefreshLocalizedPresentation()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedLabelStatus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedDetectStatus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueBadgeText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueStatusSummary)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueRowToolTip)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueRowAccessibleName)));
        }

        public static WpfImageQueueItem CreateShell(string imagePath)
        {
            return CreateShell(WpfImageQueueCatalogEntry.Create(imagePath));
        }

        public static WpfImageQueueItem CreateShell(WpfImageQueueCatalogEntry entry)
        {
            return new WpfImageQueueItem
            {
                ImagePath = entry?.ImagePath ?? string.Empty,
                FileName = entry?.FileName ?? string.Empty,
                FolderName = entry?.FolderName ?? string.Empty,
                FileSize = entry?.FileSize ?? string.Empty,
                Modified = entry?.Modified ?? string.Empty
            };
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (AffectsQueueRowText(propertyName))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueueRowToolTip)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueueRowAccessibleName)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedLabelStatus)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedDetectStatus)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueStatusSummary)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueRowToolTip)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueRowAccessibleName)));
            }

            if (string.Equals(propertyName, nameof(QueueBadgeText), StringComparison.Ordinal))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedQueueBadgeText)));
            }

            return true;
        }

        private string BuildLocalizedQueueRowText(string separator)
        {
            string normalizedSeparator = string.IsNullOrEmpty(separator) ? " / " : separator;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.FilePrefix"), FileName.Trim()));
            }

            parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.SavePrefix"), LocalizedLabelStatus));
            parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.InspectPrefix"), LocalizedDetectStatus));
            if (!string.IsNullOrWhiteSpace(Dimensions))
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.SizePrefix"), Dimensions.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(LocalizedQueueStatusSummary))
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.StatusPrefix"), LocalizedQueueStatusSummary));
            }

            if (!string.IsNullOrWhiteSpace(Detail))
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", T("WpfImageQueue.Row.DetailPrefix"), LocalizeSummary(Detail)));
            }

            return string.Join(normalizedSeparator, parts);
        }

        private static string LocalizeStatus(string value, string prefixKey)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeInlineText(value);
            if (OpenVisionLanguageService.CurrentLanguage == OpenVisionLanguage.Korean)
            {
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }

                return string.Equals(prefixKey, "WpfImageQueue.Row.SavePrefix", StringComparison.Ordinal)
                    ? "없음"
                    : "대기";
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Equals(prefixKey, "WpfImageQueue.Row.SavePrefix", StringComparison.Ordinal)
                    ? "None"
                    : "Waiting";
            }

            return TranslateKnownStatus(normalized);
        }

        private static string LocalizeBadgeText(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeInlineText(value);
            if (OpenVisionLanguageService.CurrentLanguage == OpenVisionLanguage.Korean)
            {
                return normalized;
            }

            return TranslateKnownStatus(normalized);
        }

        private static string LocalizeSummary(string value)
        {
            string normalized = NormalizeInlineText(value);
            if (OpenVisionLanguageService.CurrentLanguage == OpenVisionLanguage.Korean
                || string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            string localized = normalized
                .Replace("상태 확인 중", "Checking status", StringComparison.Ordinal)
                .Replace("수정 필요: 라벨", "Needs fix: labels", StringComparison.Ordinal)
                .Replace("검수 완료: 라벨", "Reviewed: labels", StringComparison.Ordinal)
                .Replace("저장 완료: 라벨", "Saved: labels", StringComparison.Ordinal)
                .Replace("객체 없음 완료: 라벨", "No object: labels", StringComparison.Ordinal)
                .Replace("후보 숨김 완료: 라벨", "Candidate hidden: labels", StringComparison.Ordinal)
                .Replace("저장 라벨", "Saved labels", StringComparison.Ordinal)
                .Replace("AI 후보", "AI candidates", StringComparison.Ordinal)
                .Replace("검사 실패", "Inspection failed", StringComparison.Ordinal)
                .Replace("검사중", "Inspecting", StringComparison.Ordinal)
                .Replace("저장 필요", "Save required", StringComparison.Ordinal)
                .Replace("수정 필요", "Needs fix", StringComparison.Ordinal)
                .Replace("검수 완료", "Reviewed", StringComparison.Ordinal)
                .Replace("객체 없음", "No object", StringComparison.Ordinal)
                .Replace("후보 숨김", "Candidate hidden", StringComparison.Ordinal)
                .Replace("완료", "Complete", StringComparison.Ordinal)
                .Replace("대기", "Waiting", StringComparison.Ordinal)
                .Replace("없음", "None", StringComparison.Ordinal)
                .Replace("확인 필요", "Review needed", StringComparison.Ordinal);
            return localized;
        }

        private static string TranslateKnownStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.StartsWith("AI 후보 ", StringComparison.Ordinal))
            {
                return "AI candidates " + value.Substring("AI 후보 ".Length);
            }

            if (value.StartsWith("실패 x", StringComparison.Ordinal))
            {
                return "Failed x" + value.Substring("실패 x".Length);
            }

            if (value.StartsWith("실패 ", StringComparison.Ordinal))
            {
                return "Failed " + value.Substring("실패 ".Length);
            }

            if (value.EndsWith("개", StringComparison.Ordinal)
                && value.Length > 1
                && value.Substring(0, value.Length - 1).All(char.IsDigit))
            {
                return value.Substring(0, value.Length - 1) + " items";
            }

            return value switch
            {
                "확인중" => "Checking",
                "대기" => "Waiting",
                "없음" => "None",
                "객체 없음" => "No object",
                "객체없음" => "No object",
                "저장 필요" => "Save required",
                "검사중" => "Inspecting",
                "실패" => "Failed",
                "저장됨" => "Saved",
                "후보 숨김" => "Candidate hidden",
                "수정 필요" => "Needs fix",
                "검수 완료" => "Reviewed",
                "미판정" => "Unreviewed",
                "완료" => "Complete",
                "확인 필요" => "Review needed",
                "작업" => "Work",
                _ => value
            };
        }

        private static string T(string key) => OpenVisionLanguageService.T(key);

        private string BuildQueueRowText(string separator)
        {
            string normalizedSeparator = string.IsNullOrEmpty(separator) ? " / " : separator;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                parts.Add($"파일: {FileName.Trim()}");
            }

            parts.Add($"저장: {NormalizeStatusText(LabelStatus, "없음")}");
            parts.Add($"검사: {NormalizeStatusText(DetectStatus, "대기")}");
            if (!string.IsNullOrWhiteSpace(Dimensions))
            {
                parts.Add($"크기: {Dimensions.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(QueueStatusSummary))
            {
                parts.Add($"상태: {NormalizeInlineText(QueueStatusSummary)}");
            }

            if (!string.IsNullOrWhiteSpace(Detail))
            {
                parts.Add($"상세: {NormalizeInlineText(Detail)}");
            }

            return string.Join(normalizedSeparator, parts);
        }

        public static string FormatCompactFileName(string fileName, int maximumLength = 34)
        {
            string normalized = (fileName ?? string.Empty).Trim();
            if (normalized.Length <= maximumLength || maximumLength < 12)
            {
                return normalized;
            }

            string extension = Path.GetExtension(normalized);
            int tailLength = Math.Min(Math.Max(extension.Length + 8, 12), maximumLength / 2);
            int headLength = maximumLength - tailLength - 1;
            return normalized.Substring(0, headLength) + "…" + normalized.Substring(normalized.Length - tailLength);
        }

        private static string NormalizeStatusText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : NormalizeInlineText(value);
        }

        private static string NormalizeInlineText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string[] lines = value
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" / ", lines.Select(line => line.Trim()).Where(line => line.Length > 0));
        }

        private static bool AffectsQueueRowText(string propertyName)
        {
            return string.Equals(propertyName, nameof(LabelStatus), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(DetectStatus), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(Dimensions), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(Detail), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(QueueStatusSummary), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(QualityReviewState), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(AnomalyReviewState), StringComparison.Ordinal);
        }

        private static ImageSource CreateThumbnailSource(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.DecodePixelWidth = 42;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex) when (ex is IOException
                || ex is UnauthorizedAccessException
                || ex is NotSupportedException
                || ex is InvalidOperationException
                || ex is ArgumentException)
            {
                return null;
            }
        }

        private static Brush CreateFrozenBrush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }
    }

    public sealed class WpfImageQueueFilterOption : INotifyPropertyChanged
    {
        public WpfImageQueueFilter Filter { get; set; }

        public string TextKey { get; set; } = string.Empty;

        public string Text => string.IsNullOrWhiteSpace(TextKey)
            ? string.Empty
            : OpenVisionLanguageService.T(TextKey);

        public event PropertyChangedEventHandler PropertyChanged;

        public static IReadOnlyList<WpfImageQueueFilterOption> CreateDefaults()
        {
            return Enum.GetValues(typeof(WpfImageQueueFilter))
                .Cast<WpfImageQueueFilter>()
                .Select(filter => new WpfImageQueueFilterOption
                {
                    Filter = filter,
                    TextKey = GetTextKey(filter)
                })
                .ToList();
        }

        internal void RefreshLocalizedPresentation()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }

        private static string GetTextKey(WpfImageQueueFilter filter)
        {
            return filter switch
            {
                WpfImageQueueFilter.Unlabeled => "WpfImageQueue.FilterOption.Unfinished",
                WpfImageQueueFilter.NeedsFix => "WpfImageQueue.FilterOption.NeedsFix",
                WpfImageQueueFilter.Requested => "WpfImageQueue.FilterOption.Requested",
                WpfImageQueueFilter.Candidate => "WpfImageQueue.FilterOption.Candidate",
                WpfImageQueueFilter.Confirmed => "WpfImageQueue.FilterOption.Confirmed",
                WpfImageQueueFilter.Skipped => "WpfImageQueue.FilterOption.Skipped",
                WpfImageQueueFilter.NoCandidate => "WpfImageQueue.FilterOption.NoCandidate",
                WpfImageQueueFilter.Failed => "WpfImageQueue.FilterOption.Failed",
                _ => "WpfImageQueue.FilterOption.All"
            };
        }

        public static string GetDisplayName(WpfImageQueueFilter filter)
        {
            return OpenVisionLanguageService.T(GetTextKey(filter));
        }
    }

    public enum WpfImageQueueFilter
    {
        All,
        Unlabeled,
        NeedsFix,
        Requested,
        Candidate,
        Confirmed,
        Skipped,
        NoCandidate,
        Failed
    }

    public sealed class WpfImageQueueDetail
    {
        public DrawingSize ImageSize { get; set; }

        public YoloImageReviewStatus ReviewStatus { get; set; }
    }
}
