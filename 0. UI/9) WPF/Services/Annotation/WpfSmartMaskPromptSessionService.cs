using System;
using System.Collections.Generic;
using System.Drawing;

namespace MvcVisionSystem
{
    public enum WpfSmartMaskPointKind
    {
        Positive,
        Negative
    }

    public enum WpfSmartMaskPointInputMode
    {
        None,
        Positive,
        Negative
    }

    public enum WpfSmartMaskPolygonDetail
    {
        Fast,
        Balanced,
        Detailed
    }

    public sealed class WpfSmartMaskPromptPoint
    {
        public Point Position { get; init; }
        public WpfSmartMaskPointKind Kind { get; init; }
        public int Label => Kind == WpfSmartMaskPointKind.Positive ? 1 : 0;
    }

    public sealed class WpfSmartMaskPromptSnapshot
    {
        public long Generation { get; init; }
        public string ImagePath { get; init; } = string.Empty;
        public string RecipeName { get; init; } = string.Empty;
        public Rectangle PromptBounds { get; init; }
        public int? ClassId { get; init; }
        public string ClassName { get; init; } = string.Empty;
        public WpfSmartMaskPolygonDetail PolygonDetail { get; init; }
        public IReadOnlyList<WpfSmartMaskPromptPoint> Points { get; init; } = Array.Empty<WpfSmartMaskPromptPoint>();
    }

    /// <summary>
    /// Owns one operator-driven Smart Mask instance. It contains prompts only;
    /// candidate review and canonical annotation persistence remain with their existing owners.
    /// </summary>
    public sealed class WpfSmartMaskPromptSessionService
    {
        private readonly List<WpfSmartMaskPromptPoint> points = new List<WpfSmartMaskPromptPoint>();
        private long generation;

        public bool HasSession { get; private set; }
        public string ImagePath { get; private set; } = string.Empty;
        public string RecipeName { get; private set; } = string.Empty;
        public Rectangle PromptBounds { get; private set; }
        public int? ClassId { get; private set; }
        public string ClassName { get; private set; } = string.Empty;
        public WpfSmartMaskPointInputMode InputMode { get; private set; }
        public WpfSmartMaskPolygonDetail PolygonDetail { get; private set; } = WpfSmartMaskPolygonDetail.Balanced;
        public bool HasProducedCandidate { get; private set; }
        public IReadOnlyList<WpfSmartMaskPromptPoint> Points => points;
        public int PositivePointCount => points.FindAll(point => point.Kind == WpfSmartMaskPointKind.Positive).Count;
        public int NegativePointCount => points.FindAll(point => point.Kind == WpfSmartMaskPointKind.Negative).Count;
        public int MaximumPolygonPoints => PolygonDetail switch
        {
            WpfSmartMaskPolygonDetail.Fast => 48,
            WpfSmartMaskPolygonDetail.Detailed => 256,
            _ => 96
        };

        public WpfSmartMaskPromptSnapshot Start(
            string imagePath,
            string recipeName,
            Rectangle promptBounds,
            int? classId,
            string className)
        {
            generation++;
            HasSession = true;
            ImagePath = imagePath ?? string.Empty;
            RecipeName = recipeName ?? string.Empty;
            PromptBounds = promptBounds;
            ClassId = classId;
            ClassName = string.IsNullOrWhiteSpace(className) ? "Defect" : className.Trim();
            InputMode = WpfSmartMaskPointInputMode.None;
            HasProducedCandidate = false;
            points.Clear();
            return Capture();
        }

        public void Reset()
        {
            generation++;
            HasSession = false;
            ImagePath = string.Empty;
            RecipeName = string.Empty;
            PromptBounds = Rectangle.Empty;
            ClassId = null;
            ClassName = string.Empty;
            InputMode = WpfSmartMaskPointInputMode.None;
            HasProducedCandidate = false;
            points.Clear();
        }

        public void SetInputMode(WpfSmartMaskPointInputMode mode)
            => InputMode = HasSession ? mode : WpfSmartMaskPointInputMode.None;

        public void SetPolygonDetail(WpfSmartMaskPolygonDetail detail)
        {
            if (PolygonDetail == detail)
            {
                return;
            }

            PolygonDetail = detail;
            generation++;
        }

        public void MarkCandidateProduced()
            => HasProducedCandidate = HasSession;

        public bool TryAddPoint(Point point, Size imageSize)
        {
            if (!HasSession
                || InputMode == WpfSmartMaskPointInputMode.None
                || imageSize.IsEmpty
                || point.X < 0
                || point.Y < 0
                || point.X >= imageSize.Width
                || point.Y >= imageSize.Height)
            {
                return false;
            }

            points.Add(new WpfSmartMaskPromptPoint
            {
                Position = point,
                Kind = InputMode == WpfSmartMaskPointInputMode.Positive
                    ? WpfSmartMaskPointKind.Positive
                    : WpfSmartMaskPointKind.Negative
            });
            generation++;
            return true;
        }

        public bool UndoPoint()
        {
            if (points.Count == 0)
            {
                return false;
            }

            points.RemoveAt(points.Count - 1);
            generation++;
            return true;
        }

        public bool ClearPoints()
        {
            if (points.Count == 0)
            {
                return false;
            }

            points.Clear();
            generation++;
            return true;
        }

        public bool Matches(WpfSmartMaskPromptSnapshot snapshot, string currentImagePath, string currentRecipeName)
            => snapshot != null
                && HasSession
                && generation == snapshot.Generation
                && string.Equals(ImagePath, currentImagePath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && string.Equals(RecipeName, currentRecipeName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        public WpfSmartMaskPromptSnapshot Capture()
            => new WpfSmartMaskPromptSnapshot
            {
                Generation = generation,
                ImagePath = ImagePath,
                RecipeName = RecipeName,
                PromptBounds = PromptBounds,
                ClassId = ClassId,
                ClassName = ClassName,
                PolygonDetail = PolygonDetail,
                Points = points.ToArray()
            };
    }
}
