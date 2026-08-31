using MvcVisionSystem.Yolo;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DrawingColor = System.Drawing.Color;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        public void FocusClassCatalogTab()
        {
            ShowClassCatalogWorkflowView(ShellViewModel?.IsDatasetStageActive == true
                ? WpfShellWorkflowStage.Dataset
                : WpfShellWorkflowStage.Labeling);
            PopulateClassList(GetSelectedClassName());
            UpdateLayout();
        }

        private void ClassNameBox_KeyDown(object sender, KeyInputCommandArgs e)
        {
            if (e?.Key == Key.Enter)
            {
                ExecuteAddClassCommand();
                e.Handled = true;
            }
        }

        private void ExecuteAddClassCommand()
        {
            string className = ClassCatalogService.NormalizeClassName(ClassCatalogViewModel?.ClassName);
            if (string.IsNullOrWhiteSpace(className))
            {
                SetClassEditStatus("\uC0C8 \uD074\uB798\uC2A4 \uC774\uB984\uC744 \uC785\uB825\uD558\uC138\uC694.");
                return;
            }

            if (!ClassCatalogService.TryAddClass(global.Data, className, out LabelClass addedClass))
            {
                SetClassEditStatus(string.Format("\uC774\uBBF8 \uC874\uC7AC\uD558\uAC70\uB098 \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uB294 \uD074\uB798\uC2A4 \uC774\uB984\uC785\uB2C8\uB2E4: {0}", className));
                return;
            }

            if (!SaveClassCatalog(true, out string saveError))
            {
                global.Data.ClassNamedList.Remove(addedClass);
                PopulateClassList(GetSelectedClassName());
                SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC800\uC7A5 \uC2E4\uD328: {0}", saveError));
                return;
            }

            PopulateClassList(addedClass.Text);
            ClassCatalogViewModel?.ClearClassName();
            ClassNameBox?.Focus();
            SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uCD94\uAC00: {0}", addedClass.Text));
        }

        private void ExecuteRenameClassCommand()
        {
            string currentName = GetSelectedClassName();
            if (string.IsNullOrWhiteSpace(currentName))
            {
                SetClassEditStatus("\uD074\uB798\uC2A4\uB97C \uC120\uD0DD\uD558\uC138\uC694.");
                return;
            }

            if (ClassCatalogViewModel?.SelectedClass?.IsArchived == true)
            {
                SetClassEditStatus("\uBCF4\uAD00\uB41C \uD074\uB798\uC2A4\uB294 \uBA3C\uC800 \uBCF5\uC6D0\uD55C \uB4A4 \uC774\uB984\uC744 \uBC14\uAFC0 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
                return;
            }

            string newName = ClassCatalogService.NormalizeClassName(ClassCatalogViewModel?.ClassName);
            if (string.IsNullOrWhiteSpace(newName))
            {
                SetClassEditStatus("\uC0C8 \uD074\uB798\uC2A4 \uC774\uB984\uC744 \uC785\uB825\uD558\uC138\uC694.");
                return;
            }

            if (!ClassCatalogService.TryRenameClass(global.Data, currentName, newName, out LabelClass renamedClass))
            {
                SetClassEditStatus(string.Format("\uC774\uBBF8 \uC874\uC7AC\uD558\uAC70\uB098 \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uB294 \uD074\uB798\uC2A4 \uC774\uB984\uC785\uB2C8\uB2E4: {0}", newName));
                return;
            }

            RenameActiveAnnotationClasses(currentName, renamedClass.Text, renamedClass);
            if (!SaveClassCatalog(true, out string saveError))
            {
                string failedNewName = renamedClass.Text;
                if (ClassCatalogService.TryRenameClass(global.Data, failedNewName, currentName, out LabelClass restoredClass))
                {
                    RenameActiveAnnotationClasses(failedNewName, restoredClass.Text, restoredClass);
                }

                PopulateClassList(currentName);
                RefreshObjectList();
                RedrawReviewRois();
                SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC800\uC7A5 \uC2E4\uD328: {0}", saveError));
                return;
            }

            PopulateClassList(renamedClass.Text);
            RefreshObjectList();
            RedrawReviewRois();
            SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC774\uB984 \uBCC0\uACBD: {0} -> {1}", currentName, renamedClass.Text));
        }

        private void ExecuteArchiveClassCommand()
        {
            string className = GetSelectedClassName();
            if (string.IsNullOrWhiteSpace(className))
            {
                SetClassEditStatus("\uD074\uB798\uC2A4\uB97C \uC120\uD0DD\uD558\uC138\uC694.");
                return;
            }

            LabelClass classItem = global.Data?.ClassNamedList?
                .FirstOrDefault(item => string.Equals(item?.Text, className, StringComparison.OrdinalIgnoreCase));
            if (classItem == null)
            {
                SetClassEditStatus(string.Format("\uD074\uB798\uC2A4\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4: {0}", className));
                return;
            }

            bool wasArchived = classItem.IsArchived;
            bool changed = wasArchived
                ? ClassCatalogService.TryRestoreClass(global.Data, className, out _)
                : ClassCatalogService.TryArchiveClass(global.Data, className, out _);
            if (!changed)
            {
                SetClassEditStatus(wasArchived
                    ? string.Format("\uBCF5\uC6D0\uD560 \uD074\uB798\uC2A4\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4: {0}", className)
                    : "\uCD5C\uC18C 1\uAC1C\uC758 \uD65C\uC131 \uD074\uB798\uC2A4\uB294 \uC720\uC9C0\uD574\uC57C \uD569\uB2C8\uB2E4.");
                return;
            }

            // Archiving changes only availability for new work; canonical names
            // and data.yaml order stay untouched, so no YAML rewrite is needed.
            if (!SaveClassCatalog(false, out string saveError))
            {
                classItem.IsArchived = wasArchived;
                PopulateClassList(className);
                SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC800\uC7A5 \uC2E4\uD328: {0}", saveError));
                return;
            }

            PopulateClassList(wasArchived ? className : string.Empty);
            if (!wasArchived)
            {
                ClassCatalogViewModel?.ClearClassName();
            }

            SetClassEditStatus(string.Format(
                wasArchived ? "\uD074\uB798\uC2A4 \uBCF5\uC6D0: {0}" : "\uD074\uB798\uC2A4 \uBCF4\uAD00: {0}",
                className));
        }

        private void ExecuteApplyClassColorCommand()
        {
            string className = GetSelectedClassName();
            if (string.IsNullOrWhiteSpace(className))
            {
                SetClassEditStatus("\uD074\uB798\uC2A4\uB97C \uC120\uD0DD\uD558\uC138\uC694.");
                return;
            }

            DrawingColor color = ClassCatalogViewModel?.SelectedColorPreset?.Color ?? DrawingColor.LimeGreen;
            LabelClass existingClass = global.Data?.ClassNamedList?
                .FirstOrDefault(item => string.Equals(item?.Text, className, StringComparison.OrdinalIgnoreCase));
            DrawingColor previousColor = existingClass?.DrawColor ?? DrawingColor.LimeGreen;
            if (!ClassCatalogService.TrySetClassColor(global.Data, className, color, out LabelClass classItem))
            {
                SetClassEditStatus(string.Format("\uC0AD\uC81C\uD560 \uD074\uB798\uC2A4\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4: {0}", className));
                return;
            }

            RenameActiveAnnotationClasses(classItem.Text, classItem.Text, classItem);
            if (!SaveClassCatalog(true, out string saveError))
            {
                classItem.DrawColor = previousColor;
                RenameActiveAnnotationClasses(classItem.Text, classItem.Text, classItem);
                PopulateClassList(classItem.Text);
                RefreshObjectList();
                RedrawReviewRois();
                SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC800\uC7A5 \uC2E4\uD328: {0}", saveError));
                return;
            }

            PopulateClassList(classItem.Text);
            RefreshObjectList();
            RedrawReviewRois();
            SetClassEditStatus(string.Format("\uD074\uB798\uC2A4 \uC0C9\uC0C1 \uBCC0\uACBD: {0}", classItem.Text));
        }

        private void ClassListBox_SelectionChanged(object sender, object selectedItem)
        {
            CancelFourPointBoxDraft(updateStatus: false);
            WpfClassCatalogListItem selectedClass = selectedItem as WpfClassCatalogListItem;
            string className = selectedClass?.Text ?? GetSelectedClassName();
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            if (ClassCatalogViewModel != null)
            {
                ClassCatalogViewModel.ClassName = className;
            }

            if (selectedClass?.IsArchived == true)
            {
                SetClassEditStatus(string.Format("\uBCF4\uAD00\uB41C \uD074\uB798\uC2A4: {0}. \uC0C8 \uB77C\uBCA8\uC5D0\uB294 \uC0AC\uC6A9\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.", className));
                return;
            }

            CanvasPanelViewModel?.SelectLabelClass(className);
            RefreshObjectClassOptions(className);
        }

        private void CanvasLabelClass_SelectionChanged(object sender, object selectedItem)
        {
            CancelFourPointBoxDraft(updateStatus: false);
            string className = (selectedItem as WpfCanvasLabelClassItem)?.Text
                ?? CanvasPanelViewModel?.SelectedLabelClass?.Text;
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            // The drawing code reads the selected class from the catalog. Keep the
            // always-visible canvas chips and the project class catalog on one source of truth.
            ClassCatalogViewModel?.SelectClass(className);
            CanvasPanelViewModel?.SelectLabelClass(className);
            RefreshObjectClassOptions(className);
        }

        private void ExecuteBrowseOutputRootCommand()
        {
            if (TryPickFolder("\uB370\uC774\uD130\uC14B \uCD9C\uB825 \uD3F4\uB354 \uC120\uD0DD", ClassCatalogViewModel?.OutputRootPath, out string selectedPath))
            {
                if (ClassCatalogViewModel != null)
                {
                    ClassCatalogViewModel.OutputRootPath = selectedPath;
                }

                SaveOutputRootFromEditor();
            }
        }

        private void ExecuteSaveOutputRootCommand()
        {
            SaveOutputRootFromEditor();
        }

        private LabelClass EnsureClassItem(string className)
        {
            global.Data.ClassNamedList ??= new List<LabelClass>();
            string normalizedName = ClassCatalogService.NormalizeClassName(className);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                normalizedName = "Defect";
            }

            LabelClass existing = global.Data.ClassNamedList
                .FirstOrDefault(item => string.Equals(item.Text, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing;
            }

            if (ClassCatalogService.TryAddClass(global.Data, normalizedName, out LabelClass added))
            {
                return added;
            }

            return new LabelClass
            {
                Text = normalizedName,
                DrawColor = DrawingColor.FromArgb(34, 197, 94)
            };
        }

        private void PopulateClassList(string selectedName = "")
        {
            PopulateClassCatalogFields();
            if (global.Data.ClassNamedList == null
                || !global.Data.ClassNamedList.Any(ClassCatalogService.IsActiveClass))
            {
                EnsureClassItem("Defect");
            }

            List<LabelClass> classItems = global.Data.ClassNamedList
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Text))
                .ToList();

            string effectiveSelectedName = ClassCatalogService.NormalizeClassName(selectedName);
            if (string.IsNullOrWhiteSpace(effectiveSelectedName))
            {
                effectiveSelectedName = GetSelectedClassName();
            }

            List<LabelClass> activeClassItems = classItems
                .Where(ClassCatalogService.IsActiveClass)
                .ToList();
            if (string.IsNullOrWhiteSpace(effectiveSelectedName))
            {
                effectiveSelectedName = activeClassItems.FirstOrDefault()?.Text
                    ?? classItems.FirstOrDefault()?.Text
                    ?? string.Empty;
            }
            else if (!classItems.Any(item => string.Equals(item.Text, effectiveSelectedName, StringComparison.OrdinalIgnoreCase)))
            {
                effectiveSelectedName = activeClassItems.FirstOrDefault()?.Text
                    ?? classItems.FirstOrDefault()?.Text
                    ?? string.Empty;
            }

            string drawingSelectedName = activeClassItems.Any(item =>
                string.Equals(item.Text, effectiveSelectedName, StringComparison.OrdinalIgnoreCase))
                ? effectiveSelectedName
                : activeClassItems.FirstOrDefault()?.Text ?? string.Empty;
            ClassCatalogViewModel?.SetClasses(classItems, effectiveSelectedName);
            CanvasPanelViewModel?.SetLabelClasses(classItems, drawingSelectedName);

            RefreshObjectClassOptions(drawingSelectedName);
            RefreshYoloTrainingStepCompletion();
        }

        private void PopulateClassCatalogFields()
        {
            EnsureProjectSettings();
            global.Data.NormalizeOutputPaths();
            ClassCatalogViewModel?.LoadOutputRoot(global.Data.OutputRootPath);
        }

        private string GetSelectedClassName()
        {
            if (ClassCatalogViewModel?.SelectedClass != null)
            {
                return ClassCatalogViewModel.SelectedClass.Text;
            }

            return string.Empty;
        }

        private void RenameActiveAnnotationClasses(string oldName, string newName, LabelClass classItem)
        {
            string normalizedOldName = ClassCatalogService.NormalizeClassName(oldName);
            string normalizedNewName = ClassCatalogService.NormalizeClassName(newName);
            if (string.IsNullOrWhiteSpace(normalizedOldName)
                || string.IsNullOrWhiteSpace(normalizedNewName))
            {
                return;
            }

            // Class catalog edits are project-level. Keep already drawn objects on the current image aligned
            // so a rename from Defect to NG does not leave stale labels in Object Review.
            for (int i = 0; i < manualRoiClassNames.Count; i++)
            {
                if (string.Equals(manualRoiClassNames[i], normalizedOldName, StringComparison.OrdinalIgnoreCase))
                {
                    manualRoiClassNames[i] = normalizedNewName;
                }
            }

            foreach (LabelingSegmentationObject segment in manualSegments)
            {
                if (segment == null)
                {
                    continue;
                }

                bool matches = string.Equals(segment.ClassName, normalizedOldName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(segment.ClassItem?.Text, normalizedOldName, StringComparison.OrdinalIgnoreCase);
                if (matches)
                {
                    segment.ClassName = normalizedNewName;
                    segment.ClassItem = classItem;
                }
            }

            foreach (var candidate in confirmedDetectionCandidates)
            {
                if (string.Equals(candidate?.ClassName, normalizedOldName, StringComparison.OrdinalIgnoreCase))
                {
                    candidate.ClassName = normalizedNewName;
                }
            }
        }

        private void SetClassEditStatus(string message)
        {
            if (ClassCatalogViewModel != null)
            {
                ClassCatalogViewModel.StatusText = message;
            }
        }

        private bool SaveClassCatalog(bool updateYoloDataYaml, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (updateYoloDataYaml)
                {
                    RecipeConfigurationSaveResult pairedSaveResult = global.Data.SaveConfigAndYoloDataYaml(global.Recipe.Name);
                    if (!pairedSaveResult.IsSuccess)
                    {
                        errorMessage = pairedSaveResult.ErrorMessage;
                        return false;
                    }
                }
                else
                {
                    RecipeConfigurationSaveResult saveResult = global.Data.SaveConfig(global.Recipe.Name);
                    if (!saveResult.IsSuccess)
                    {
                        errorMessage = saveResult.ErrorMessage;
                        return false;
                    }
                }

                PopulateClassCatalogFields();
                PopulateProjectConfigPanelFields();
                return true;
            }
            catch (Exception error)
            {
                errorMessage = error.Message;
                return false;
            }
        }

        private void SaveOutputRootFromEditor()
        {
            string outputRootPath = (ClassCatalogViewModel?.OutputRootPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(outputRootPath))
            {
                SetClassEditStatus("\uC800\uC7A5 \uACBD\uB85C\uB97C \uC785\uB825\uD558\uAC70\uB098 \uC120\uD0DD\uD558\uC138\uC694.");
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(annotationDirtyReason))
                {
                    SetClassEditStatus("\uC800\uC7A5\uD558\uC9C0 \uC54A\uC740 \uB77C\uBCA8\uC774 \uC788\uC2B5\uB2C8\uB2E4. \uBA3C\uC800 \uB77C\uBCA8\uC744 \uC800\uC7A5\uD55C \uB4A4 \uC800\uC7A5 \uACBD\uB85C\uB97C \uBC14\uAFB8\uC138\uC694.");
                    return;
                }

                string previousOutputRootPath = global.Data?.OutputRootPath ?? string.Empty;
                global.Data.ConfigureOutputRoot(outputRootPath);
                if (!SaveClassCatalog(true, out string saveError))
                {
                    global.Data.ConfigureOutputRoot(previousOutputRootPath);
                    PopulateClassCatalogFields();
                    throw new IOException(saveError);
                }
                ReloadActiveImageAnnotationsAfterOutputRootChange(previousOutputRootPath, global.Data.OutputRootPath);
                RefreshTrainingReadinessPanel(refreshYaml: false);
                SetDatasetStatus(string.Format("\uB370\uC774\uD130\uC14B: \uCD9C\uB825 \uACBD\uB85C {0}", global.Data.OutputRootPath));
                SetClassEditStatus(string.Format("\uC800\uC7A5 \uACBD\uB85C \uC801\uC6A9: {0} / \uD074\uB798\uC2A4\uB294 \uB808\uC2DC\uD53C\uC5D0 \uC720\uC9C0\uB418\uACE0, \uD604\uC7AC \uC774\uBBF8\uC9C0\uB294 \uC0C8 \uACBD\uB85C\uC758 \uB77C\uBCA8 \uAE30\uC900\uC73C\uB85C \uB2E4\uC2DC \uD655\uC778\uD588\uC2B5\uB2C8\uB2E4.", global.Data.OutputRootPath));
                AppendLog(string.Format("\uB370\uC774\uD130\uC14B \uCD9C\uB825 \uACBD\uB85C \uC800\uC7A5: {0}", global.Data.OutputRootPath));
            }
            catch (Exception ex)
            {
                SetClassEditStatus(string.Format("\uC800\uC7A5 \uACBD\uB85C \uC801\uC6A9 \uC2E4\uD328: {0}", ex.Message));
                AppendLog(string.Format("\uB370\uC774\uD130\uC14B \uCD9C\uB825 \uACBD\uB85C \uC800\uC7A5 \uC2E4\uD328: {0}", ex.Message));
            }
        }

        private void ReloadActiveImageAnnotationsAfterOutputRootChange(string previousOutputRootPath, string currentOutputRootPath)
        {
            if (string.IsNullOrWhiteSpace(activeImagePath)
                || activeImageBitmap == null
                || activeImageSize.IsEmpty
                || PathsEqual(previousOutputRootPath, currentOutputRootPath))
            {
                return;
            }

            TryLoadImage(
                activeImagePath,
                populateQueue: false,
                refreshQueueDetails: true,
                refreshActiveStatus: true,
                appendLoadLog: false);
        }
    }
}
