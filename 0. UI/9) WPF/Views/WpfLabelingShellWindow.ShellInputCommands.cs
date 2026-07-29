using Lib.Common;
using MahApps.Metro.IconPacks;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.DrawObject;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Views;
using OpenVisionLab.ImageCanvas.ViewModels;
using OpenVisionLab.Mvvm;
using OpenVisionLab.Mvvm.Behaviors;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using CvMat = OpenCvSharp.Mat;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingSize = System.Drawing.Size;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfUiApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme;
using WpfUiApplicationThemeManager = Wpf.Ui.Appearance.ApplicationThemeManager;
using WpfUiFluentWindow = Wpf.Ui.Controls.FluentWindow;
using WpfUiWindowBackdropType = Wpf.Ui.Controls.WindowBackdropType;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Shell-level keyboard shortcuts stay outside the constructor/field file so command routing is easier to audit.
        private void ExecuteShellPreviewKeyDownCommand(KeyInputCommandArgs e)
        {
            if (e == null || IsTextEditingElement(e.OriginalSource))
            {
                return;
            }

            if (e.Modifiers == ModifierKeys.None && e.Key == Key.Escape)
            {
                e.Handled = CancelFourPointBoxDraft(updateStatus: true);
                if (e.Handled)
                {
                    return;
                }
            }

            if (e.Modifiers == ModifierKeys.None && e.Key == Key.Back)
            {
                e.Handled = RemoveLastFourPointBoxPoint();
                if (e.Handled)
                {
                    return;
                }
            }

            if ((e.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    bool isRedoShortcut = (e.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    e.Handled = isRedoShortcut ? RedoWpfAnnotationHistory() : UndoWpfAnnotationHistory();
                    return;
                }

                if (e.Key == Key.Y)
                {
                    e.Handled = RedoWpfAnnotationHistory();
                    return;
                }

                WpfAnnotationShortcut controlShortcut = WpfAnnotationProductivityService.ResolveShortcut(
                    e.Key,
                    e.Modifiers);
                if (controlShortcut.Kind == WpfAnnotationShortcutKind.DuplicateSelected)
                {
                    e.Handled = TryDuplicateSelectedAnnotation();
                }

                return;
            }

            if (e.Modifiers != ModifierKeys.None)
            {
                return;
            }

            WpfAnnotationShortcut shortcut = WpfAnnotationProductivityService.ResolveShortcut(e.Key, e.Modifiers);
            if (TryExecuteAnnotationProductivityShortcut(shortcut))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down || e.Key == Key.Right)
            {
                e.Handled = TryOpenAdjacentQueueImage(1);
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Left)
            {
                e.Handled = TryOpenAdjacentQueueImage(-1);
            }
        }

        private bool TryExecuteAnnotationProductivityShortcut(WpfAnnotationShortcut shortcut)
        {
            if (shortcut == null || shortcut.Kind == WpfAnnotationShortcutKind.None)
            {
                return false;
            }

            switch (shortcut.Kind)
            {
                case WpfAnnotationShortcutKind.SelectTool:
                    WpfAnnotationToolItem selectedTool = ResolveSelectableAnnotationTool(shortcut.Tool);
                    if (selectedTool == null)
                    {
                        return false;
                    }

                    ApplyAnnotationToolSelection(selectedTool);
                    return true;

                case WpfAnnotationShortcutKind.SelectClass:
                    if (CanvasPanelViewModel?.TrySelectLabelClassByShortcut(shortcut.ClassIndex) != true)
                    {
                        return false;
                    }

                    CanvasLabelClass_SelectionChanged(
                        CanvasLabelClassListBox,
                        CanvasPanelViewModel.SelectedLabelClass);
                    SetModelStatus($"클래스 단축키: {CanvasPanelViewModel.SelectedLabelClass.Text}");
                    return true;

                case WpfAnnotationShortcutKind.OpenClassCatalog:
                    ShowClassCatalogWorkflowView(WpfShellWorkflowStage.Labeling);
                    return true;

                case WpfAnnotationShortcutKind.RepeatLast:
                    return TryRepeatLastAnnotationToolAndClass();

                case WpfAnnotationShortcutKind.ToggleShortcutHelp:
                    CanvasPanelViewModel?.ToggleShortcutHelp();
                    return true;

                default:
                    return false;
            }
        }

        private bool TryRepeatLastAnnotationToolAndClass()
        {
            if (CanvasPanelViewModel?.TryGetRepeatSelection(out WpfAnnotationTool tool, out string className) != true)
            {
                return false;
            }

            WpfAnnotationToolItem selectedTool = ResolveSelectableAnnotationTool(tool);
            if (selectedTool == null)
            {
                return false;
            }

            CanvasPanelViewModel.SelectLabelClass(className);
            CanvasLabelClass_SelectionChanged(CanvasLabelClassListBox, CanvasPanelViewModel.SelectedLabelClass);
            ApplyAnnotationToolSelection(selectedTool);
            SetModelStatus($"마지막 라벨링 반복: {selectedTool.Text} / {className}");
            return true;
        }

        private static bool IsTextEditingElement(object source)
        {
            return source is TextBox
                || source is ComboBox
                || source is System.Windows.Controls.Primitives.RangeBase
                || source is System.Windows.Controls.Primitives.TextBoxBase;
        }
    }
}
