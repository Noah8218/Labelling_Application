using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public static class ClassCatalogService
    {
        private static readonly Color[] Palette =
        {
            Color.FromArgb(34, 197, 94),
            Color.FromArgb(239, 68, 68),
            Color.FromArgb(245, 158, 11),
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(168, 85, 247),
            Color.FromArgb(20, 184, 166),
            Color.FromArgb(236, 72, 153),
            Color.FromArgb(148, 163, 184)
        };

        public static IReadOnlyList<Color> DefaultPalette => Palette;

        public static int FindClassIndex(LabelingProjectData data, string className)
        {
            return FindClassIndex(data?.ClassNamedList, className);
        }

        public static int FindClassIndex(IReadOnlyList<LabelClass> classes, string className)
        {
            string normalizedName = NormalizeClassName(className);
            if (classes == null || string.IsNullOrWhiteSpace(normalizedName))
            {
                return -1;
            }

            for (int index = 0; index < classes.Count; index++)
            {
                if (string.Equals(classes[index]?.Text, normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int FindOrAddClass(LabelingProjectData data, string className)
        {
            int classIndex = FindClassIndex(data, className);
            if (classIndex >= 0)
            {
                return classIndex;
            }

            TryAddClass(data, className, out _);
            return FindClassIndex(data, className);
        }

        public static bool TryAddClass(LabelingProjectData data, string className, out LabelClass classItem)
        {
            classItem = null;
            if (data == null)
            {
                return false;
            }

            data.ClassNamedList ??= new List<LabelClass>();

            string normalizedName = NormalizeClassName(className);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            if (data.ClassNamedList.Any(item =>
                    string.Equals(item?.Text, normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            classItem = new LabelClass
            {
                Text = normalizedName,
                DrawColor = GetNextColor(data)
            };

            data.ClassNamedList.Add(classItem);
            return true;
        }

        public static bool TryArchiveClass(LabelingProjectData data, string className, out LabelClass classItem)
        {
            classItem = null;
            if (data == null)
            {
                return false;
            }

            data.ClassNamedList ??= new List<LabelClass>();

            string normalizedName = NormalizeClassName(className);
            classItem = data.ClassNamedList.FirstOrDefault(item =>
                string.Equals(item?.Text, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (classItem == null || classItem.IsArchived)
            {
                return false;
            }

            if (data.ClassNamedList.Count(item => IsActiveClass(item)) <= 1)
            {
                classItem = null;
                return false;
            }

            classItem.IsArchived = true;
            return true;
        }

        public static bool TryRestoreClass(LabelingProjectData data, string className, out LabelClass classItem)
        {
            classItem = null;
            if (data == null)
            {
                return false;
            }

            data.ClassNamedList ??= new List<LabelClass>();
            string normalizedName = NormalizeClassName(className);
            classItem = data.ClassNamedList.FirstOrDefault(item =>
                string.Equals(item?.Text, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (classItem == null || !classItem.IsArchived)
            {
                return false;
            }

            classItem.IsArchived = false;
            return true;
        }

        public static bool RemoveClass(LabelingProjectData data, string className)
        {
            return TryArchiveClass(data, className, out _);
        }

        public static bool IsActiveClass(LabelClass classItem)
        {
            return classItem != null
                && !classItem.IsArchived
                && !string.IsNullOrWhiteSpace(classItem.Text);
        }

        public static bool TryRenameClass(LabelingProjectData data, string currentName, string newName, out LabelClass classItem)
        {
            classItem = null;
            if (data == null)
            {
                return false;
            }

            data.ClassNamedList ??= new List<LabelClass>();

            string normalizedCurrentName = NormalizeClassName(currentName);
            string normalizedNewName = NormalizeClassName(newName);
            if (string.IsNullOrWhiteSpace(normalizedCurrentName)
                || string.IsNullOrWhiteSpace(normalizedNewName))
            {
                return false;
            }

            classItem = data.ClassNamedList.FirstOrDefault(item =>
                string.Equals(item?.Text, normalizedCurrentName, StringComparison.OrdinalIgnoreCase));
            if (classItem == null || classItem.IsArchived)
            {
                return false;
            }

            LabelClass targetItem = classItem;
            if (data.ClassNamedList.Any(item =>
                    !ReferenceEquals(item, targetItem)
                    && string.Equals(item?.Text, normalizedNewName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            classItem.Text = normalizedNewName;
            return true;
        }

        public static bool TrySetClassColor(LabelingProjectData data, string className, Color color, out LabelClass classItem)
        {
            classItem = null;
            if (data == null)
            {
                return false;
            }

            data.ClassNamedList ??= new List<LabelClass>();

            string normalizedName = NormalizeClassName(className);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            classItem = data.ClassNamedList.FirstOrDefault(item =>
                string.Equals(item?.Text, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (classItem == null)
            {
                return false;
            }

            classItem.DrawColor = color;
            return true;
        }

        public static string NormalizeClassName(string className)
        {
            return (className ?? string.Empty).Trim();
        }

        private static Color GetNextColor(LabelingProjectData data)
        {
            foreach (Color color in Palette)
            {
                if (!data.ClassNamedList.Any(item => item.DrawColor == color))
                {
                    return color;
                }
            }

            return Color.LimeGreen;
        }
    }
}
