using MahApps.Metro.IconPacks;
using OpenVisionLab;
using OpenVisionLab.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MvcVisionSystem
{
    public sealed class WpfLearningWorkflowPanelViewModel : WpfObservableViewModel, IDisposable
    {
        private static readonly Action NoOpCommand = () => { };
        private static readonly Action<object> NoOpSelectionCommand = _ => { };
        private static readonly Action<WpfYoloTrainingWorkflowStepItem> NoOpTrainingStepCommand = _ => { };
        private static readonly Action<WpfFirstRunChecklistItem> NoOpFirstRunSamplePathCommand = _ => { };
        private static readonly Action<WpfDatasetDashboardMetricItem> NoOpDatasetDashboardMetricCommand = _ => { };
        private static string T(string key) => OpenVisionLanguageService.T(key);

        private static string Format(string key, params object[] arguments)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                T(key),
                arguments ?? Array.Empty<object>());
        }

        private WpfLearningModeItem selectedMode;
        private WpfLearningModeItem selectedDatasetPurposeMode;
        private WpfAnnotationToolItem selectedTool;
        private WpfLearningStepItem selectedStep;
        private string datasetPurposeSummaryText = string.Empty;
        private string datasetPurposeToolSummaryText = string.Empty;
        private string datasetSetupFirstActionText = "\uCC98\uC74C \uC2DC\uC791: \uBAA9\uC801\uC744 \uACE0\uB974\uACE0 \uB370\uC774\uD130\uC14B\uC744 \uBA3C\uC800 \uC900\uBE44\uD558\uC138\uC694.";
        private string datasetSetupActionText = "\uB370\uC774\uD130\uC14B \uC2DC\uC791";
        private string currentWorkflowActionText = string.Empty;
        private string datasetSetupStatusText = "\uB370\uC774\uD130\uC14B \uC2DC\uC791 \uC804";
        private string currentLabelingTaskStepText = "\uC0D8\uD50C";
        private string currentLabelingTaskToolText = "\uB3C4\uAD6C: \uC120\uD0DD";
        private string currentLabelingTaskActionText = "\uC774\uBBF8\uC9C0 \uD050\uC5D0\uC11C \uC791\uC5C5\uD560 \uC774\uBBF8\uC9C0\uB97C \uC5F4\uACE0 \uCCAB \uB77C\uBCA8\uC744 \uC2DC\uC791\uD558\uC138\uC694.";
        private string currentLabelingTaskChecklistFirstText = "1  \uC774\uBBF8\uC9C0";
        private string currentLabelingTaskChecklistSecondText = "2  \uC5F4\uAE30";
        private string currentLabelingTaskChecklistThirdText = "3  \uB77C\uBCA8";
        private string currentLabelingTaskChecklistSummaryText = "\uD750\uB984: \uC774\uBBF8\uC9C0 > \uC5F4\uAE30 > \uB77C\uBCA8";
        private Visibility datasetOnboardingVisibility = Visibility.Visible;
        private Visibility labelingTaskVisibility = Visibility.Collapsed;
        private string modeDetailText = string.Empty;
        private string stepDetailText = string.Empty;
        private string toolDetailText = string.Empty;
        private string trainingChecklistStatusText = T("WpfLearningWorkflow.TrainingChecklist.Status.Initial");
        private string trainingChecklistDetailText = T("WpfLearningWorkflow.TrainingChecklist.Detail.Initial");
        private string trainingChecklistActionText = WpfTrainingChecklistLocalizationService.CreateInitialAction().ActionText;
        private string datasetDashboardStatusText = T("WpfLearningWorkflow.DatasetDashboard.Status.Before");
        private string datasetDashboardSummaryText = T("WpfLearningWorkflow.DatasetDashboard.Summary.Before");
        private string datasetDashboardActionText = T("WpfLearningWorkflow.DatasetDashboardAction.Initial");
        private string externalEvaluationDataAuditStatusText = "\uC678\uBD80 \uD3C9\uAC00 \uD3F4\uB354\uB97C \uB300\uC870\uD558\uBA74 \uD559\uC2B5 \uB370\uC774\uD130\uC640\uC758 \uC911\uBCF5\uC744 \uD655\uC778\uD569\uB2C8\uB2E4.";
        private string externalEvaluationDataAuditDetailText = string.Empty;
        private string externalEvaluationDataAuditPathText = string.Empty;
        private WpfLearningModeItem selectedExternalYoloDatasetPurposeMode;
        private string externalYoloDatasetIntakeStatusText = "\uC678\uBD80 YOLO data.yaml: \uC120\uD0DD \uC548 \uD568";
        private string externalYoloDatasetIntakeDetailText = "\uB0B4\uBD80 \uB77C\uBCA8\uB9C1 \uB370\uC774\uD130\uC640 \uBD84\uB9AC\uB41C \uC6D0\uBCF8 YOLO \uB370\uC774\uD130\uC14B\uC744 \uAC80\uC99D\uD55C \uB4A4 \uB2E4\uC74C \uD559\uC2B5\uC5D0\uB9CC \uC0AC\uC6A9\uD569\uB2C8\uB2E4.";
        private string externalYoloDatasetIntakePathText = string.Empty;
        private string objectDetectionMvpNextActionText = T("WpfLearningWorkflow.ObjectDetectionMvpNextAction.Empty");
        private string modelReplacementStatusText = WpfModelReplacementLocalizationService.CreateInitial().StatusText;
        private string modelReplacementDetailText = WpfModelReplacementLocalizationService.CreateInitial().DetailText;
        private string trainingHistoryText = string.Empty;
        private string trainingResultComparisonSummaryText = string.Empty;
        private string trainingResultComparisonText = string.Empty;
        private string trainingModelAdoptionDecisionText = string.Empty;
        private string trainingModelLifecycleCurrentText = T("WpfLearningWorkflow.TrainingModelLifecycle.Current.Initial");
        private string trainingModelLifecycleCandidateText = T("WpfLearningWorkflow.TrainingModelLifecycle.Candidate.Initial");
        private string trainingModelLifecycleDecisionText = T("WpfLearningWorkflow.TrainingModelLifecycle.Decision.Initial");
        private string trainingModelLifecycleNextActionText = T("WpfLearningWorkflow.TrainingModelLifecycle.Next.Initial");
        private string runModelComparisonActionText = string.Empty;
        private string runModelComparisonToolTipText = string.Empty;
        private string modelComparisonBasisText = string.Empty;
        private string trainingHistorySourceText = string.Empty;
        private string trainingResultComparisonSummarySourceText = string.Empty;
        private string trainingResultComparisonSourceText = string.Empty;
        private string trainingModelAdoptionDecisionSourceText = string.Empty;
        private string runModelComparisonActionSourceText = string.Empty;
        private string runModelComparisonToolTipSourceText = string.Empty;
        private string modelComparisonBasisSourceText = string.Empty;
        private bool isRunModelComparisonEnabled = true;
        private WpfYoloTrainingWorkflowStepItem currentYoloTrainingStep;
        private string currentYoloTrainingStepTitleText = string.Empty;
        private string currentYoloTrainingStepDetailText = string.Empty;
        private string currentYoloTrainingActionText = string.Empty;
        private bool hasCurrentYoloTrainingStep;
        private bool isYoloFixClassesEnabled = true;
        private bool isYoloFixLabelsEnabled;
        private bool isYoloFixDatasetEnabled = true;
        private int brushSize = 12;
        private double maskOpacity = 0.66;
        private ICommand datasetPurposeSelectionChangedCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private ICommand datasetSetupStartCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private ICommand datasetOpenExistingCommand = new RelayCommand(NoOpCommand);
        private ICommand learningModeSelectionChangedCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private ICommand annotationToolSelectionChangedCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private ICommand learningStepSelectionChangedCommand = new RelayCommand<object>(NoOpSelectionCommand);
        private ICommand yoloTrainingWorkflowStepCommand = new RelayCommand<WpfYoloTrainingWorkflowStepItem>(NoOpTrainingStepCommand);
        private ICommand firstRunSamplePathCommand = new RelayCommand<WpfFirstRunChecklistItem>(NoOpFirstRunSamplePathCommand);
        private ICommand datasetDashboardMetricCommand = new RelayCommand<WpfDatasetDashboardMetricItem>(NoOpDatasetDashboardMetricCommand);
        private ICommand tutorialOpenHtmlGuideCommand = new RelayCommand(NoOpCommand);
        private ICommand yoloFixClassesCommand = new RelayCommand(NoOpCommand);
        private ICommand yoloFixLabelsCommand = new RelayCommand(NoOpCommand);
        private ICommand yoloFixDatasetCommand = new RelayCommand(NoOpCommand);
        private ICommand runModelComparisonCommand = new RelayCommand(NoOpCommand);
        private ICommand externalEvaluationDataAuditCommand = new RelayCommand(NoOpCommand);
        private ICommand selectExternalYoloDatasetCommand = new RelayCommand(NoOpCommand);
        private ICommand activateExternalYoloDatasetCommand = new RelayCommand(NoOpCommand);
        private ICommand clearExternalYoloDatasetCommand = new RelayCommand(NoOpCommand);
        private ICommand templateCurrentImageCommand = new RelayCommand(NoOpCommand);
        private ICommand templateBatchCommand = new RelayCommand(NoOpCommand);
        private WpfTrainingChecklistLocalizationSnapshot trainingChecklistLocalizationSnapshot;
        private WpfTrainingChecklistActionLocalizationSnapshot trainingChecklistActionLocalizationSnapshot;
        private WpfModelReplacementLocalizationSnapshot modelReplacementLocalizationSnapshot;
        private WpfTrainingModelLifecycleLocalizationSnapshot trainingModelLifecycleLocalizationSnapshot;
        private WpfTrainingComparisonLocalizationSnapshot trainingComparisonLocalizationSnapshot;
        private WpfDatasetDashboardLocalizationSnapshot datasetDashboardLocalizationSnapshot;
        private bool refreshingTrainingChecklistLocalization;
        private bool refreshingModelReplacementLocalization;
        private bool refreshingTrainingModelLifecycleLocalization;
        private bool refreshingTrainingComparisonLocalization;
        private bool disposed;

        public WpfLearningWorkflowPanelViewModel()
        {
            modelReplacementLocalizationSnapshot = WpfModelReplacementLocalizationService.CreateInitial();
            trainingModelLifecycleLocalizationSnapshot = WpfTrainingModelLifecycleLocalizationService.CreateInitial();
            trainingComparisonLocalizationSnapshot = WpfTrainingComparisonLocalizationService.CreateInitial();
            OpenVisionLanguageService.LanguageChanged += OpenVisionLanguageService_LanguageChanged;

            SetTrainingComparisonLocalization(trainingComparisonLocalizationSnapshot);

            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.LabelingBasics, "\uB77C\uBCA8\uB9C1", PackIconMaterialKind.SchoolOutline, "\uC815\uB2F5 \uB77C\uBCA8\uC744 \uADF8\uB9AC\uB294 \uD750\uB984"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.ObjectDetection, "\uAC1D\uCCB4 \uD0D0\uC9C0", PackIconMaterialKind.ShapeSquareRoundedPlus, "\uBC15\uC2A4 \uB77C\uBCA8\uACFC \uBAA8\uB378 \uD6C4\uBCF4 \uAC80\uD1A0"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.Segmentation, "\uC138\uADF8\uBA58\uD14C\uC774\uC158", PackIconMaterialKind.ViewListOutline, "\uD3F4\uB9AC\uACE4\uACFC \uB9C8\uC2A4\uD06C \uB77C\uBCA8"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.AnomalyDetection, "\uC774\uC0C1 \uD0D0\uC9C0", PackIconMaterialKind.AlertCircleOutline, "이미지 전체 정상/이상 판정"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.Train, "\uD559\uC2B5", PackIconMaterialKind.PlayCircleOutline, "\uB370\uC774\uD130\uC14B \uC900\uBE44\uC640 \uD559\uC2B5"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.Infer, "\uCD94\uB860", PackIconMaterialKind.RobotIndustrial, "\uBAA8\uB378 \uC2E4\uD589\uACFC \uC608\uCE21 \uD655\uC778"));
            LearningModes.Add(new WpfLearningModeItem(WpfLearningMode.Review, "\uAC80\uD1A0", PackIconMaterialKind.CheckAll, "\uC608\uCE21\uC744 \uD655\uC815 \uB77C\uBCA8\uB85C \uC804\uD658"));
            DatasetPurposeModes.Add(LearningModes.First(item => item.Mode == WpfLearningMode.ObjectDetection));
            DatasetPurposeModes.Add(LearningModes.First(item => item.Mode == WpfLearningMode.Segmentation));
            DatasetPurposeModes.Add(LearningModes.First(item => item.Mode == WpfLearningMode.AnomalyDetection));
            ExternalYoloDatasetPurposeModes.Add(DatasetPurposeModes.First(item => item.Mode == WpfLearningMode.ObjectDetection));
            ExternalYoloDatasetPurposeModes.Add(DatasetPurposeModes.First(item => item.Mode == WpfLearningMode.Segmentation));
            SelectedExternalYoloDatasetPurposeMode = ExternalYoloDatasetPurposeModes.FirstOrDefault();

            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Select, "\uC120\uD0DD", PackIconMaterialKind.CursorDefaultOutline, "\uAC1D\uCCB4 \uC120\uD0DD\uACFC \uD3B8\uC9D1"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Rectangle, "\uBC15\uC2A4", PackIconMaterialKind.VectorRectangle, "\uAC1D\uCCB4 \uBC15\uC2A4 \uC601\uC5ED"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Ellipse, "\uC6D0/\uD0C0\uC6D0", PackIconMaterialKind.VectorEllipse, "\uC6D0\uD615 \uD639\uC740 \uD0C0\uC6D0 \uC601\uC5ED"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Polygon, "\uD3F4\uB9AC\uACE4", PackIconMaterialKind.VectorPolygon, "\uB2E4\uAC01\uD615 \uC138\uADF8\uBA58\uD14C\uC774\uC158"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Brush, "\uBE0C\uB7EC\uC2DC", PackIconMaterialKind.BrushVariant, "\uBE0C\uB7EC\uC2DC \uB9C8\uC2A4\uD06C \uD3B8\uC9D1"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Eraser, "\uC9C0\uC6B0\uAC1C", PackIconMaterialKind.EraserVariant, "\uB9C8\uC2A4\uD06C\uB098 \uC601\uC5ED \uC9C0\uC6B0\uAE30"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.PanZoom, "\uC774\uB3D9", PackIconMaterialKind.CursorMove, "\uD654\uBA74 \uC774\uB3D9\uACFC \uD655\uB300"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Undo, "\uB418\uB3CC\uB9AC\uAE30", PackIconMaterialKind.Refresh, "\uB9C8\uC9C0\uB9C9 \uD3B8\uC9D1 \uB418\uB3CC\uB9AC\uAE30"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Redo, "\uB2E4\uC2DC \uC801\uC6A9", PackIconMaterialKind.Reload, "\uB418\uB3CC\uB9B0 \uD3B8\uC9D1 \uB2E4\uC2DC \uC801\uC6A9"));
            RegisterAnnotationTool(new WpfAnnotationToolItem(WpfAnnotationTool.Delete, "\uC0AD\uC81C", PackIconMaterialKind.TrashCanOutline, "\uC120\uD0DD \uB77C\uBCA8 \uC0AD\uC81C"));
            ApplyDatasetPurpose(LabelingDatasetPurpose.ObjectDetection);

            LearningSteps.Add(new WpfLearningStepItem(WpfLearningStep.Sample, "\uC0D8\uD50C", PackIconMaterialKind.FolderImage));
            LearningSteps.Add(new WpfLearningStepItem(WpfLearningStep.Label, "\uB77C\uBCA8", PackIconMaterialKind.ShapeSquareRoundedPlus));
            LearningSteps.Add(new WpfLearningStepItem(WpfLearningStep.Infer, "\uCD94\uB860", PackIconMaterialKind.RobotIndustrial));
            LearningSteps.Add(new WpfLearningStepItem(WpfLearningStep.Review, "\uB9AC\uBDF0", PackIconMaterialKind.CheckAll));
            LearningSteps.Add(new WpfLearningStepItem(WpfLearningStep.Save, "\uC800\uC7A5", PackIconMaterialKind.ContentSaveOutline));

            TemplateWorkflowSteps.Add(new WpfTemplateWorkflowStepItem(
                1,
                "\uAE30\uC900 \uB77C\uBCA8 \uC120\uD0DD",
                "\uC798 \uADF8\uB824\uC9C4 \uBC15\uC2A4 1\uAC1C\uB97C \uC120\uD0DD\uD558\uBA74 \uADF8 \uC601\uC5ED\uC774 \uD15C\uD50C\uB9BF\uC774 \uB429\uB2C8\uB2E4.",
                "\uC800\uC7A5 \uB77C\uBCA8",
                PackIconMaterialKind.CursorDefaultClickOutline));
            TemplateWorkflowSteps.Add(new WpfTemplateWorkflowStepItem(
                2,
                "\uD604\uC7AC \uC774\uBBF8\uC9C0 \uB77C\uBCA8 \uCD08\uC548",
                "\uB2E4\uB978 \uC774\uBBF8\uC9C0\uC5D0\uC11C \uAC19\uC740 \uBAA8\uC591\uC744 \uCC3E\uACE0 \uADF8 \uC704\uCE58\uC5D0 \uC800\uC7A5 \uC804 \uB77C\uBCA8 \uCD08\uC548\uC744 \uCD94\uAC00\uD569\uB2C8\uB2E4.",
                "\uC0C1\uB2E8/\uC624\uB978\uCABD",
                PackIconMaterialKind.SelectionSearch));
            TemplateWorkflowSteps.Add(new WpfTemplateWorkflowStepItem(
                3,
                "\uC804\uCCB4 \uC774\uBBF8\uC9C0 \uC790\uB3D9 \uC800\uC7A5",
                "\uC774\uBBF8\uC9C0 \uBAA9\uB85D\uC744 \uD55C \uBC88\uC529 \uB3CC\uBA70 \uB77C\uBCA8\uC774 \uC5C6\uB294 \uD56D\uBAA9\uC5D0\uB9CC \uC800\uC7A5\uD569\uB2C8\uB2E4.",
                "\uC774\uBBF8\uC9C0 \uD050",
                PackIconMaterialKind.PlaylistCheck));
            TemplateWorkflowSteps.Add(new WpfTemplateWorkflowStepItem(
                4,
                "\uAC80\uD1A0\uC640 \uC800\uC7A5",
                "\uD604\uC7AC \uC774\uBBF8\uC9C0 \uCD08\uC548\uC740 \uC704\uCE58\uB97C \uD655\uC778\uD55C \uB4A4 \uB77C\uBCA8 \uC800\uC7A5\uC744 \uB20C\uB7EC\uC57C \uBC18\uC601\uB429\uB2C8\uB2E4.",
                "\uC800\uC7A5 \uC804 \uCD08\uC548",
                PackIconMaterialKind.ContentSaveCheckOutline));

            FirstRunSamplePathItems.Add(new WpfFirstRunChecklistItem(
                1,
                "\uB370\uC774\uD130\uC14B",
                "\uC0C8\uB85C \uB9CC\uB4E4\uAE30 \uB610\uB294 \uAE30\uC874 \uC5F4\uAE30",
                "\uC800\uC7A5 \uD3F4\uB354\uC640 \uC774\uBBF8\uC9C0 \uD3F4\uB354\uB97C \uBD84\uB9AC\uD574 \uC0C8 \uC2E4\uC2B5\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.DatabasePlusOutline,
                shortcutWorkflowStepOrder: 1,
                shortcutActionText: "\uC2DC\uC791"));
            FirstRunSamplePathItems.Add(new WpfFirstRunChecklistItem(
                2,
                "\uC774\uBBF8\uC9C0",
                "\uD3F4\uB354 \uC5F4\uACE0 \uD050 \uD655\uC778",
                "\uC774\uBBF8\uC9C0\uAC00 \uBCF4\uC774\uBA74 \uCCAB \uC774\uBBF8\uC9C0\uB97C \uC120\uD0DD\uD574 \uC791\uC5C5\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.FolderImage,
                shortcutWorkflowStepOrder: 2,
                shortcutActionText: "\uC5F4\uAE30"));
            FirstRunSamplePathItems.Add(new WpfFirstRunChecklistItem(
                3,
                "\uCCAB \uB77C\uBCA8",
                "\uBC15\uC2A4 \uADF8\uB9B0 \uB4A4 \uB77C\uBCA8 \uC800\uC7A5",
                "\uC800\uC7A5\uD574\uC57C \uBC15\uC2A4 \uB77C\uBCA8 \uD30C\uC77C\uC774 \uC0DD\uC131\uB418\uACE0 \uD559\uC2B5 \uC810\uAC80\uC5D0 \uBC18\uC601\uB429\uB2C8\uB2E4.",
                PackIconMaterialKind.ShapeSquareRoundedPlus,
                shortcutWorkflowStepOrder: 4,
                shortcutActionText: "\uB77C\uBCA8\uB9C1"));
            FirstRunSamplePathItems.Add(new WpfFirstRunChecklistItem(
                4,
                "\uD6C4\uBCF4 \uD655\uC778",
                "\uD6C4\uBCF4 \uC0DD\uC131 \uD6C4 \uC218\uB77D/\uC2A4\uD0B5",
                "\uD6C4\uBCF4\uB294 \uC815\uB2F5\uC774 \uC544\uB2C8\uBBC0\uB85C \uAC80\uD1A0 \uD6C4 \uC800\uC7A5\uD55C \uAC83\uB9CC \uD559\uC2B5\uC5D0 \uC0AC\uC6A9\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.RobotIndustrial,
                shortcutWorkflowStepOrder: 7,
                shortcutActionText: "\uAC80\uD1A0"));
            FirstRunSamplePathItems.Add(new WpfFirstRunChecklistItem(
                5,
                "\uD559\uC2B5 \uC900\uBE44",
                "\uC810\uAC80 \uD1B5\uACFC \uB4A4 \uD559\uC2B5 \uC2DC\uC791",
                "\uD559\uC2B5\uC774 \uB05D\uB098\uBA74 \uBAA8\uB378\uC13C\uD130\uC5D0\uC11C \uC0C8 \uD559\uC2B5 \uACB0\uACFC \uD6C4\uBCF4\uB97C \uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.CheckAll,
                shortcutWorkflowStepOrder: 5,
                shortcutActionText: "\uC810\uAC80"));

            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                1,
                "\uB370\uC774\uD130\uC14B",
                "\uC0C8\uB85C \uB9CC\uB4E4\uAE30 \uB610\uB294 \uAE30\uC874 \uC5F4\uAE30",
                "\uC800\uC7A5 \uD3F4\uB354\uC640 \uC774\uBBF8\uC9C0 \uD3F4\uB354\uB97C \uBA3C\uC800 \uAD6C\uBD84\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.DatabasePlusOutline));
            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                2,
                "\uC774\uBBF8\uC9C0",
                "\uC6D0\uBCF8 \uC774\uBBF8\uC9C0 \uD3F4\uB354 \uD655\uC778",
                "\uC774\uBBF8\uC9C0 \uD050\uC5D0 \uD30C\uC77C\uC774 \uBCF4\uC774\uBA74 \uB2E4\uC74C \uB2E8\uACC4\uC785\uB2C8\uB2E4.",
                PackIconMaterialKind.FolderImage));
            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                3,
                "\uD074\uB798\uC2A4",
                "OK, NG \uB4F1 \uB77C\uBCA8 \uC774\uB984 \uD655\uC778",
                "\uBAA8\uB378\uC774 \uBC30\uC6B8 \uC774\uB984\uC744 \uBA3C\uC800 \uC815\uD574 \uB450\uBA74 \uC800\uC7A5 \uD6C4 \uD63C\uB780\uC774 \uC904\uC5B4\uB4ED\uB2C8\uB2E4.",
                PackIconMaterialKind.TagMultipleOutline));
            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                4,
                "\uCCAB \uBC15\uC2A4",
                "\uBC15\uC2A4 \uB3C4\uAD6C\uB85C 1\uAC1C \uADF8\uB9AC\uAE30",
                "\uAC1D\uCCB4\uB97C \uD3EC\uD568\uD558\uB294 \uBC15\uC2A4\uB97C \uADF8\uB9AC\uACE0 \uC62C\uBC14\uB978 \uD074\uB798\uC2A4\uB97C \uC120\uD0DD\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.ShapeSquareRoundedPlus));
            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                5,
                "\uB77C\uBCA8 \uC800\uC7A5",
                "\uC800\uC7A5 \uD6C4 \uB2E4\uC74C \uC774\uBBF8\uC9C0",
                "\uB77C\uBCA8 \uC800\uC7A5 \uBC84\uD2BC\uC744 \uB20C\uB7EC \uD604\uC7AC \uC774\uBBF8\uC9C0\uC758 \uC815\uB2F5\uC744 \uD30C\uC77C\uC5D0 \uBC18\uC601\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.ContentSaveOutline));
            FirstRunChecklistItems.Add(new WpfFirstRunChecklistItem(
                6,
                "\uD559\uC2B5 \uC900\uBE44",
                "\uB370\uC774\uD130\uC14B \uC810\uAC80\uC73C\uB85C \uBD80\uC871\uD55C \uD56D\uBAA9 \uD655\uC778",
                "\uD559\uC2B5 \uC2DC\uC791 \uC804\uC5D0 \uB77C\uBCA8, \uD074\uB798\uC2A4, \uBD84\uD560 \uC0C1\uD0DC\uB97C \uD55C \uBC88\uC5D0 \uD655\uC778\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.CheckAll));

            TutorialChecklistItems.Add("\uB370\uC774\uD130\uC14B\uC744 \uBA3C\uC800 \uB9CC\uB4E4\uACE0 \uC800\uC7A5 \uC704\uCE58\uC640 \uD559\uC2B5 \uBAA9\uC801\uC744 \uC815\uD569\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uC0D8\uD50C \uB610\uB294 \uC774\uBBF8\uC9C0 \uD3F4\uB354\uB97C \uC5F4\uACE0 \uC88C\uCE21 \uD050\uC5D0\uC11C \uC774\uBBF8\uC9C0\uB97C \uC120\uD0DD\uD569\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uC624\uB978\uCABD \uD074\uB798\uC2A4 \uD0ED\uC5D0\uC11C OK, NG\uCC98\uB7FC \uBAA8\uB378\uC774 \uBC30\uC6B8 \uC774\uB984\uC744 \uB4F1\uB85D\uD569\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uB77C\uBCA8\uB9C1 \uBAA8\uB4DC\uC5D0\uC11C \uBC15\uC2A4\uB97C \uADF8\uB9AC\uACE0 \uC800\uC7A5\uD558\uC5EC \uBC15\uC2A4 \uB77C\uBCA8 \uD30C\uC77C\uC744 \uB9CC\uB4ED\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uB370\uC774\uD130\uC14B \uC810\uAC80\uC73C\uB85C \uB77C\uBCA8, \uD074\uB798\uC2A4, \uD559\uC2B5 \uC124\uC815\uC744 \uD655\uC778\uD569\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uBAA8\uB378 \uD559\uC2B5\uC744 \uC2E4\uD589\uD558\uACE0 \uC644\uB8CC \uD6C4 \uD559\uC2B5 \uACB0\uACFC \uBAA8\uB378\uC744 \uC801\uC6A9\uD569\uB2C8\uB2E4.");
            TutorialChecklistItems.Add("\uD604\uC7AC \uAC80\uC0AC\uB85C AI \uD6C4\uBCF4\uB97C \uD655\uC778\uD558\uACE0 \uD655\uC815 \uB610\uB294 \uC2A4\uD0B5\uD569\uB2C8\uB2E4.");

            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                1,
                "\uB370\uC774\uD130\uC14B \uB9CC\uB4E4\uAE30",
                "\uD559\uC2B5 \uBAA9\uC801, \uC800\uC7A5 \uC704\uCE58, \uAE30\uBCF8 \uD074\uB798\uC2A4\uB97C \uC815\uD574 \uB370\uC774\uD130\uC14B\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.",
                "\uB370\uC774\uD130\uC14B \uB9CC\uB4E4\uAE30 \uCC3D\uC5D0\uC11C \uD3F4\uB354 \uAD6C\uC870\uC640 \uD074\uB798\uC2A4\uB97C \uC900\uBE44\uD558\uC138\uC694.",
                PackIconMaterialKind.FolderImage));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                2,
                "\uC774\uBBF8\uC9C0 \uBD88\uB7EC\uC624\uAE30",
                "\uD559\uC2B5\uD560 N\uAC1C \uC774\uBBF8\uC9C0\uB97C \uC774\uBBF8\uC9C0 \uD050\uC5D0 \uC62C\uB9BD\uB2C8\uB2E4.",
                "\uC774\uBBF8\uC9C0 \uD050\uC5D0 \uD3F4\uB354 \uACBD\uB85C\uC640 \uD30C\uC77C \uC218\uAC00 \uBCF4\uC774\uBA74 \uB2E4\uC74C \uB2E8\uACC4\uC785\uB2C8\uB2E4.",
                PackIconMaterialKind.ImageMultipleOutline));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                3,
                "\uD074\uB798\uC2A4 \uB4F1\uB85D",
                "OK, NG, defect\uCC98\uB7FC \uBAA8\uB378\uC774 \uBC30\uC6B8 \uC774\uB984\uC744 \uBA3C\uC800 \uB9CC\uB4ED\uB2C8\uB2E4.",
                "\uC624\uB978\uCABD \uD074\uB798\uC2A4 \uD0ED\uC5D0\uC11C \uBAA9\uB85D\uACFC \uC800\uC7A5 \uACBD\uB85C\uB97C \uD655\uC778\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.TagMultipleOutline));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                4,
                "\uBC15\uC2A4 \uB77C\uBCA8\uB9C1",
                "\uAC01 \uC774\uBBF8\uC9C0\uC5D0\uC11C \uAC1D\uCCB4\uB97C \uBC15\uC2A4\uB85C \uADF8\uB9AC\uACE0 \uD074\uB798\uC2A4\uB97C \uBD99\uC785\uB2C8\uB2E4.",
                "\uB77C\uBCA8 \uC218\uAC00 \uB298\uACE0 \uC800\uC7A5\uD558\uBA74 \uBC15\uC2A4 \uB77C\uBCA8 \uD30C\uC77C\uC774 \uC0DD\uC131\uB429\uB2C8\uB2E4.",
                PackIconMaterialKind.ShapeSquareRoundedPlus));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                5,
                "\uC800\uC7A5\uACFC \uB370\uC774\uD130\uC14B \uC810\uAC80",
                "\uB77C\uBCA8\uC774 \uBE60\uC9C4 \uC774\uBBF8\uC9C0, \uD074\uB798\uC2A4, \uD559\uC2B5 \uC124\uC815\uC744 \uAC80\uC0AC\uD569\uB2C8\uB2E4.",
                "\uD559\uC2B5/\uBAA8\uB378 \uC13C\uD130\uC758 \uC0C8\uB85C\uACE0\uCE68\uC5D0\uC11C \uD559\uC2B5 \uAC00\uB2A5 \uC0C1\uD0DC\uAC00 \uB098\uC640\uC57C \uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.CheckAll));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                6,
                "YOLO \uBAA8\uB378 \uD559\uC2B5",
                "\uC774\uBBF8\uC9C0 \uD06C\uAE30, \uBC30\uCE58, \uC5D0\uD3ED, \uAC00\uC911\uCE58\uB97C \uD655\uC778\uD558\uACE0 \uD559\uC2B5\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.",
                "\uC9C4\uD589\uB960\uACFC \uC5D0\uD3ED\uC744 \uBCF4\uACE0, \uC644\uB8CC \uD6C4 \uD559\uC2B5 \uACB0\uACFC \uBAA8\uB378\uC744 \uD655\uC778\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.PlayCircleOutline));
            YoloTrainingWorkflowSteps.Add(new WpfYoloTrainingWorkflowStepItem(
                7,
                "\uD559\uC2B5 \uACB0\uACFC \uCD94\uB860 \uAC80\uD1A0",
                "\uC0C8\uB85C \uB9CC\uB4E0 \uBAA8\uB378\uB85C \uD604\uC7AC \uC774\uBBF8\uC9C0\uB97C \uAC80\uC0AC\uD558\uACE0 \uD6C4\uBCF4\uB97C \uD655\uC815\uD569\uB2C8\uB2E4.",
                "\uACB0\uACFC\uAC00 \uB9DE\uC73C\uBA74 \uB77C\uBCA8\uB85C \uD655\uC815\uD558\uACE0, \uD2C0\uB9AC\uBA74 \uB370\uC774\uD130\uB97C \uCD94\uAC00\uD569\uB2C8\uB2E4.",
                PackIconMaterialKind.RobotIndustrial));

            // Keep the file-format lesson close to dataset status: saving a box creates a paired label txt, not a hidden app-only object.
            RefreshYoloDatasetStructureItems();

            RefreshCurrentYoloTrainingStep();
            SelectedMode = LearningModes.FirstOrDefault(item => item.Mode == WpfLearningMode.LabelingBasics) ?? LearningModes.FirstOrDefault();
            SelectedTool = SelectableAnnotationTools.FirstOrDefault();
            SelectedStep = LearningSteps.FirstOrDefault();
            SetAnnotationHistoryState(canUndo: false, canRedo: false, undoActionName: string.Empty, redoActionName: string.Empty);
            SetTrainingChecklistLocalization(WpfTrainingChecklistLocalizationService.CreateInitial());
            SetTrainingChecklistActionLocalization(WpfTrainingChecklistLocalizationService.CreateInitialAction());
            WpfDatasetDashboardLocalizationSnapshot initialDashboard = WpfDatasetDashboardLocalizationService.CreateInitial();
            SetDatasetDashboard(
                initialDashboard.StatusText,
                initialDashboard.SummaryText,
                datasetDashboardActionText,
                BuildInitialDatasetDashboardMetrics(),
                initialDashboard.IssueItems,
                initialDashboard);
        }

        public string ViewName => nameof(WpfLearningWorkflowPanel);

        public ICommand DatasetPurposeSelectionChangedCommand
        {
            get => datasetPurposeSelectionChangedCommand;
            private set => SetProperty(ref datasetPurposeSelectionChangedCommand, value);
        }

        public ICommand DatasetSetupStartCommand
        {
            get => datasetSetupStartCommand;
            private set => SetProperty(ref datasetSetupStartCommand, value);
        }

        public ICommand DatasetOpenExistingCommand
        {
            get => datasetOpenExistingCommand;
            private set => SetProperty(ref datasetOpenExistingCommand, value);
        }

        public ICommand LearningModeSelectionChangedCommand
        {
            get => learningModeSelectionChangedCommand;
            private set => SetProperty(ref learningModeSelectionChangedCommand, value);
        }

        public ICommand AnnotationToolSelectionChangedCommand
        {
            get => annotationToolSelectionChangedCommand;
            private set => SetProperty(ref annotationToolSelectionChangedCommand, value);
        }

        public ICommand LearningStepSelectionChangedCommand
        {
            get => learningStepSelectionChangedCommand;
            private set => SetProperty(ref learningStepSelectionChangedCommand, value);
        }

        public ICommand YoloTrainingWorkflowStepCommand
        {
            get => yoloTrainingWorkflowStepCommand;
            private set => SetProperty(ref yoloTrainingWorkflowStepCommand, value);
        }

        public ICommand FirstRunSamplePathCommand
        {
            get => firstRunSamplePathCommand;
            private set => SetProperty(ref firstRunSamplePathCommand, value);
        }

        public ICommand DatasetDashboardMetricCommand
        {
            get => datasetDashboardMetricCommand;
            private set => SetProperty(ref datasetDashboardMetricCommand, value);
        }

        public ICommand TutorialOpenHtmlGuideCommand
        {
            get => tutorialOpenHtmlGuideCommand;
            private set => SetProperty(ref tutorialOpenHtmlGuideCommand, value);
        }

        public ICommand YoloFixClassesCommand
        {
            get => yoloFixClassesCommand;
            private set => SetProperty(ref yoloFixClassesCommand, value);
        }

        public ICommand YoloFixLabelsCommand
        {
            get => yoloFixLabelsCommand;
            private set => SetProperty(ref yoloFixLabelsCommand, value);
        }

        public ICommand YoloFixDatasetCommand
        {
            get => yoloFixDatasetCommand;
            private set => SetProperty(ref yoloFixDatasetCommand, value);
        }

        public ICommand RunModelComparisonCommand
        {
            get => runModelComparisonCommand;
            private set => SetProperty(ref runModelComparisonCommand, value);
        }

        public ICommand ExternalEvaluationDataAuditCommand
        {
            get => externalEvaluationDataAuditCommand;
            private set => SetProperty(ref externalEvaluationDataAuditCommand, value);
        }

        public ICommand SelectExternalYoloDatasetCommand
        {
            get => selectExternalYoloDatasetCommand;
            private set => SetProperty(ref selectExternalYoloDatasetCommand, value);
        }

        public ICommand ActivateExternalYoloDatasetCommand
        {
            get => activateExternalYoloDatasetCommand;
            private set => SetProperty(ref activateExternalYoloDatasetCommand, value);
        }

        public ICommand ClearExternalYoloDatasetCommand
        {
            get => clearExternalYoloDatasetCommand;
            private set => SetProperty(ref clearExternalYoloDatasetCommand, value);
        }

        public ICommand TemplateCurrentImageCommand
        {
            get => templateCurrentImageCommand;
            private set => SetProperty(ref templateCurrentImageCommand, value);
        }

        public ICommand TemplateBatchCommand
        {
            get => templateBatchCommand;
            private set => SetProperty(ref templateBatchCommand, value);
        }

        public ObservableCollection<WpfLearningModeItem> LearningModes { get; } = new ObservableCollection<WpfLearningModeItem>();

        public ObservableCollection<WpfLearningModeItem> DatasetPurposeModes { get; } = new ObservableCollection<WpfLearningModeItem>();

        public ObservableCollection<WpfLearningModeItem> ExternalYoloDatasetPurposeModes { get; } = new ObservableCollection<WpfLearningModeItem>();

        public ObservableCollection<WpfAnnotationToolItem> AnnotationTools { get; } = new ObservableCollection<WpfAnnotationToolItem>();

        public ObservableCollection<WpfAnnotationToolItem> SelectableAnnotationTools { get; } = new ObservableCollection<WpfAnnotationToolItem>();

        public ObservableCollection<WpfAnnotationToolItem> AnnotationCommandTools { get; } = new ObservableCollection<WpfAnnotationToolItem>();

        public ObservableCollection<WpfAnnotationToolItem> VisibleAnnotationTools { get; } = new ObservableCollection<WpfAnnotationToolItem>();

        public ObservableCollection<WpfLearningStepItem> LearningSteps { get; } = new ObservableCollection<WpfLearningStepItem>();

        public ObservableCollection<WpfTemplateWorkflowStepItem> TemplateWorkflowSteps { get; } = new ObservableCollection<WpfTemplateWorkflowStepItem>();

        public ObservableCollection<WpfFirstRunChecklistItem> FirstRunSamplePathItems { get; } = new ObservableCollection<WpfFirstRunChecklistItem>();

        public ObservableCollection<WpfFirstRunChecklistItem> FirstRunChecklistItems { get; } = new ObservableCollection<WpfFirstRunChecklistItem>();

        public ObservableCollection<string> TutorialChecklistItems { get; } = new ObservableCollection<string>();

        public ObservableCollection<WpfYoloTrainingWorkflowStepItem> YoloTrainingWorkflowSteps { get; } = new ObservableCollection<WpfYoloTrainingWorkflowStepItem>();

        public ObservableCollection<WpfYoloDatasetStructureItem> YoloDatasetStructureItems { get; } = new ObservableCollection<WpfYoloDatasetStructureItem>();

        public ObservableCollection<WpfDatasetDashboardMetricItem> DatasetDashboardMetrics { get; } = new ObservableCollection<WpfDatasetDashboardMetricItem>();

        public ObservableCollection<string> DatasetDashboardIssueItems { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> TrainingRunHistoryItems { get; } = new ObservableCollection<string>();

        public ObservableCollection<WpfTrainingResultReportItem> TrainingResultReportItems { get; } = new ObservableCollection<WpfTrainingResultReportItem>();

        public string TutorialTitleText => "\uCC98\uC74C 10\uBD84 \uD29C\uD1A0\uB9AC\uC5BC";

        public string TutorialSummaryText => "\uAE43\uD5C8\uBE0C\uC5D0\uC11C \uBC1B\uC740 \uD6C4 \uC0D8\uD50C \uC774\uBBF8\uC9C0, \uB77C\uBCA8, \uD559\uC2B5, \uCD94\uB860 \uAC80\uD1A0\uAE4C\uC9C0 \uC544\uB798 \uC21C\uC11C\uB300\uB85C \uB530\uB77C\uD569\uB2C8\uB2E4.";

        public string TutorialHtmlPathText => "HTML: docs/tutorial/labeling-workbench-tutorial.html";

        public string TrainingWorkflowSummaryText => "\uB370\uC774\uD130\uC14B \u2192 \uC774\uBBF8\uC9C0 \u2192 \uD074\uB798\uC2A4 \u2192 \uB77C\uBCA8 \u2192 \uC810\uAC80 \u2192 \uD559\uC2B5 \u2192 \uCD94\uB860/\uAC80\uD1A0";

        public string GuideToolsRoleTitleText => T("WpfLearningWorkflow.GuideToolsRoleTitle");

        public string GuideToolsPrimaryTaskText => T("WpfLearningWorkflow.GuideToolsPrimaryTask");

        public string GuideToolsHelperTaskText => T("WpfLearningWorkflow.GuideToolsHelperTask");

        public string TemplateWorkflowTitleText => "\uD15C\uD50C\uB9BF \uBC18\uBCF5 \uB77C\uBCA8\uB9C1";

        public string TemplateWorkflowRoleText => "\uBCF4\uC870 \uB3C4\uAD6C: \uD604\uC7AC \uC774\uBBF8\uC9C0\uB294 \uB77C\uBCA8 \uCD08\uC548\uC744 \uB9CC\uB4E4\uACE0, \uC704\uCE58 \uD655\uC778 \uD6C4 \uB77C\uBCA8 \uC800\uC7A5\uC744 \uB20C\uB7EC\uC57C \uD559\uC2B5\uC5D0 \uBC18\uC601\uB429\uB2C8\uB2E4. \uC804\uCCB4 \uC774\uBBF8\uC9C0\uB294 \uB77C\uBCA8 \uC5C6\uB294 \uD56D\uBAA9\uC5D0 \uBC14\uB85C \uC800\uC7A5\uD569\uB2C8\uB2E4.";

        public string TemplateWorkflowSummaryText => "\uAE30\uC900 \uB77C\uBCA8 1\uAC1C\uB97C \uB4F1\uB85D\uD55C \uB4A4 \uD604\uC7AC \uC774\uBBF8\uC9C0\uC5D0\uB294 \uB77C\uBCA8 \uCD08\uC548\uC744, \uC804\uCCB4 \uC774\uBBF8\uC9C0\uC5D0\uB294 \uC800\uC7A5 \uB77C\uBCA8\uC744 \uCD94\uAC00\uD569\uB2C8\uB2E4.";

        public string TemplateCurrentActionText => "\uD604\uC7AC \uC774\uBBF8\uC9C0 \uCD08\uC548";

        public string TemplateBatchActionText => "\uC804\uCCB4 \uC790\uB3D9 \uC800\uC7A5";

        public string TemplateCurrentActionToolTipText => "\uC120\uD0DD\uD55C \uAE30\uC900 \uB77C\uBCA8\uC744 \uB4F1\uB85D\uD558\uAC70\uB098, \uB4F1\uB85D\uB41C \uD15C\uD50C\uB9BF\uC73C\uB85C \uD604\uC7AC \uC774\uBBF8\uC9C0\uC5D0 \uB77C\uBCA8 \uD6C4\uBCF4\uB97C \uB9CC\uB4ED\uB2C8\uB2E4.";

        public string TemplateBatchActionToolTipText => "\uB4F1\uB85D\uB41C \uD15C\uD50C\uB9BF\uC73C\uB85C \uC774\uBBF8\uC9C0 \uBAA9\uB85D\uC744 \uD55C \uBC88\uC529 \uB3CC\uBA70 \uB77C\uBCA8 \uC5C6\uB294 \uD56D\uBAA9\uC5D0\uB9CC \uC800\uC7A5\uD569\uB2C8\uB2E4.";

        public string FirstRunSamplePathTitleText => T("WpfLearningWorkflow.FirstRunSamplePathTitle");

        public string FirstRunSamplePathSummaryText => T("WpfLearningWorkflow.FirstRunSamplePathSummary");

        public string FirstRunSamplePathPrimaryActionText => T("WpfLearningWorkflow.FirstRunSamplePathPrimaryAction");

        public string FirstRunChecklistTitleText => T("WpfLearningWorkflow.FirstRunChecklistTitle");

        public string FirstRunChecklistSummaryText => T("WpfLearningWorkflow.FirstRunChecklistSummary");

        public string DatasetSetupSequenceText => T("WpfLearningWorkflow.DatasetSetupSequence");

        public string YoloDatasetStructureTitleText => T("WpfLearningWorkflow.YoloDatasetStructureTitle");

        public string YoloDatasetStructureSummaryText => T("WpfLearningWorkflow.YoloDatasetStructureSummary");

        public string YoloDatasetPairSummaryText => T("WpfLearningWorkflow.YoloDatasetPairSummary");

        public string GroundTruthChipText => T("WpfLearningWorkflow.GroundTruthChip");

        public string PredictionChipText => T("WpfLearningWorkflow.PredictionChip");

        public string TrainingChecklistStatusText
        {
            get => trainingChecklistStatusText;
            set
            {
                if (!refreshingTrainingChecklistLocalization)
                {
                    trainingChecklistLocalizationSnapshot = null;
                }

                SetProperty(ref trainingChecklistStatusText, value ?? string.Empty);
            }
        }

        public string TrainingChecklistDetailText
        {
            get => trainingChecklistDetailText;
            set
            {
                if (!refreshingTrainingChecklistLocalization)
                {
                    trainingChecklistLocalizationSnapshot = null;
                }

                SetProperty(ref trainingChecklistDetailText, value ?? string.Empty);
            }
        }

        public string TrainingChecklistActionText
        {
            get => trainingChecklistActionText;
            set
            {
                if (!refreshingTrainingChecklistLocalization)
                {
                    trainingChecklistActionLocalizationSnapshot = null;
                }

                SetProperty(ref trainingChecklistActionText, value ?? string.Empty);
            }
        }

        public string DatasetDashboardStatusText
        {
            get => datasetDashboardStatusText;
            set => SetProperty(ref datasetDashboardStatusText, value ?? string.Empty);
        }

        public string DatasetDashboardSummaryText
        {
            get => datasetDashboardSummaryText;
            set => SetProperty(ref datasetDashboardSummaryText, value ?? string.Empty);
        }

        public string DatasetDashboardActionText
        {
            get => datasetDashboardActionText;
            set => SetProperty(ref datasetDashboardActionText, value ?? string.Empty);
        }

        public void SetTrainingChecklistLocalization(WpfTrainingChecklistLocalizationSnapshot localization)
        {
            trainingChecklistLocalizationSnapshot = localization;
            refreshingTrainingChecklistLocalization = true;
            try
            {
                TrainingChecklistStatusText = localization?.StatusText ?? string.Empty;
                TrainingChecklistDetailText = localization?.DetailText ?? string.Empty;
            }
            finally
            {
                refreshingTrainingChecklistLocalization = false;
            }

            trainingChecklistLocalizationSnapshot = localization;
        }

        public void SetTrainingChecklistActionLocalization(WpfTrainingChecklistActionLocalizationSnapshot localization)
        {
            trainingChecklistActionLocalizationSnapshot = localization;
            refreshingTrainingChecklistLocalization = true;
            try
            {
                TrainingChecklistActionText = localization?.ActionText ?? string.Empty;
            }
            finally
            {
                refreshingTrainingChecklistLocalization = false;
            }

            trainingChecklistActionLocalizationSnapshot = localization;
        }

        public string ExternalEvaluationDataAuditStatusText
        {
            get => externalEvaluationDataAuditStatusText;
            private set => SetProperty(ref externalEvaluationDataAuditStatusText, value ?? string.Empty);
        }

        public string ExternalEvaluationDataAuditDetailText
        {
            get => externalEvaluationDataAuditDetailText;
            private set => SetProperty(ref externalEvaluationDataAuditDetailText, value ?? string.Empty);
        }

        public string ExternalEvaluationDataAuditPathText
        {
            get => externalEvaluationDataAuditPathText;
            private set => SetProperty(ref externalEvaluationDataAuditPathText, value ?? string.Empty);
        }

        public string ExternalYoloDatasetIntakeStatusText
        {
            get => externalYoloDatasetIntakeStatusText;
            private set => SetProperty(ref externalYoloDatasetIntakeStatusText, value ?? string.Empty);
        }

        public string ExternalYoloDatasetIntakeDetailText
        {
            get => externalYoloDatasetIntakeDetailText;
            private set => SetProperty(ref externalYoloDatasetIntakeDetailText, value ?? string.Empty);
        }

        public string ExternalYoloDatasetIntakePathText
        {
            get => externalYoloDatasetIntakePathText;
            private set => SetProperty(ref externalYoloDatasetIntakePathText, value ?? string.Empty);
        }

        public string ObjectDetectionMvpNextActionText
        {
            get => objectDetectionMvpNextActionText;
            private set => SetProperty(ref objectDetectionMvpNextActionText, value ?? string.Empty);
        }

        public string ModelReplacementStatusText
        {
            get => modelReplacementStatusText;
            set
            {
                if (!refreshingModelReplacementLocalization)
                {
                    modelReplacementLocalizationSnapshot = null;
                }

                SetProperty(ref modelReplacementStatusText, value ?? string.Empty);
            }
        }

        public string ModelReplacementDetailText
        {
            get => modelReplacementDetailText;
            set
            {
                if (!refreshingModelReplacementLocalization)
                {
                    modelReplacementLocalizationSnapshot = null;
                }

                SetProperty(ref modelReplacementDetailText, value ?? string.Empty);
            }
        }

        public string TrainingHistoryText
        {
            get => trainingHistoryText;
            set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    trainingHistorySourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref trainingHistoryText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().HistoryText
                        : value);
            }
        }

        public string TrainingResultComparisonText
        {
            get => trainingResultComparisonText;
            set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    trainingResultComparisonSourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref trainingResultComparisonText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().ComparisonText
                        : value);
            }
        }

        public string TrainingResultComparisonSummaryText
        {
            get => trainingResultComparisonSummaryText;
            set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    trainingResultComparisonSummarySourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref trainingResultComparisonSummaryText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().SummaryText
                        : value);
            }
        }

        public string TrainingModelAdoptionDecisionText
        {
            get => trainingModelAdoptionDecisionText;
            set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    trainingModelAdoptionDecisionSourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref trainingModelAdoptionDecisionText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().AdoptionDecisionText
                        : value);
            }
        }

        public string TrainingModelLifecycleCurrentText
        {
            get => trainingModelLifecycleCurrentText;
            private set
            {
                if (!refreshingTrainingModelLifecycleLocalization)
                {
                    trainingModelLifecycleLocalizationSnapshot = null;
                }

                SetProperty(ref trainingModelLifecycleCurrentText, string.IsNullOrWhiteSpace(value) ? T("WpfLearningWorkflow.TrainingModelLifecycle.Current.Initial") : value);
            }
        }

        public string TrainingModelLifecycleCandidateText
        {
            get => trainingModelLifecycleCandidateText;
            private set
            {
                if (!refreshingTrainingModelLifecycleLocalization)
                {
                    trainingModelLifecycleLocalizationSnapshot = null;
                }

                SetProperty(ref trainingModelLifecycleCandidateText, string.IsNullOrWhiteSpace(value) ? T("WpfLearningWorkflow.TrainingModelLifecycle.Candidate.Initial") : value);
            }
        }

        public string TrainingModelLifecycleDecisionText
        {
            get => trainingModelLifecycleDecisionText;
            private set
            {
                if (!refreshingTrainingModelLifecycleLocalization)
                {
                    trainingModelLifecycleLocalizationSnapshot = null;
                }

                SetProperty(ref trainingModelLifecycleDecisionText, string.IsNullOrWhiteSpace(value) ? T("WpfLearningWorkflow.TrainingModelLifecycle.Decision.Initial") : value);
            }
        }

        public string TrainingModelLifecycleNextActionText
        {
            get => trainingModelLifecycleNextActionText;
            private set
            {
                if (!refreshingTrainingModelLifecycleLocalization)
                {
                    trainingModelLifecycleLocalizationSnapshot = null;
                }

                SetProperty(ref trainingModelLifecycleNextActionText, string.IsNullOrWhiteSpace(value) ? T("WpfLearningWorkflow.TrainingModelLifecycle.Next.Initial") : value);
            }
        }

        public string TrainingModelLifecycleSummaryTitleText => T("WpfLearningWorkflow.TrainingModelLifecycle.Title");

        public string TrainingModelLifecycleCurrentCaptionText => T("WpfLearningWorkflow.TrainingModelLifecycle.Label.Current");

        public string TrainingModelLifecycleCandidateCaptionText => T("WpfLearningWorkflow.TrainingModelLifecycle.Label.Candidate");

        public string TrainingModelLifecycleDecisionCaptionText => T("WpfLearningWorkflow.TrainingModelLifecycle.Label.Decision");

        public string TrainingModelLifecycleNextActionCaptionText => T("WpfLearningWorkflow.TrainingModelLifecycle.Label.NextAction");

        public string TrainingResultComparisonTitleText => T("WpfLearningWorkflow.TrainingComparison.Title");

        public string RunModelComparisonActionText
        {
            get => runModelComparisonActionText;
            private set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    runModelComparisonActionSourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref runModelComparisonActionText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().RunActionText
                        : value);
            }
        }

        public string RunModelComparisonToolTipText
        {
            get => runModelComparisonToolTipText;
            private set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    runModelComparisonToolTipSourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref runModelComparisonToolTipText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().RunToolTipText
                        : value);
            }
        }

        public string ModelComparisonBasisText
        {
            get => modelComparisonBasisText;
            private set
            {
                if (!refreshingTrainingComparisonLocalization)
                {
                    modelComparisonBasisSourceText = value ?? string.Empty;
                    trainingComparisonLocalizationSnapshot = null;
                }

                SetProperty(
                    ref modelComparisonBasisText,
                    string.IsNullOrWhiteSpace(value)
                        ? WpfTrainingComparisonLocalizationService.CreateInitial().ComparisonBasisText
                        : value);
            }
        }

        public bool IsRunModelComparisonEnabled
        {
            get => isRunModelComparisonEnabled;
            private set => SetProperty(ref isRunModelComparisonEnabled, value);
        }

        public WpfYoloTrainingWorkflowStepItem CurrentYoloTrainingStep
        {
            get => currentYoloTrainingStep;
            private set
            {
                if (SetProperty(ref currentYoloTrainingStep, value))
                {
                    HasCurrentYoloTrainingStep = value != null;
                    CurrentYoloTrainingStepTitleText = value == null
                        ? "다음 단계 없음"
                        : $"{value.Order}. {value.Title}";
                    CurrentYoloTrainingStepDetailText = value?.ActionText ?? string.Empty;
                    CurrentYoloTrainingActionText = value == null
                        ? "대기"
                        : ResolveCurrentYoloTrainingActionText(value);
                }
            }
        }

        public string CurrentYoloTrainingStepTitleText
        {
            get => currentYoloTrainingStepTitleText;
            private set => SetProperty(ref currentYoloTrainingStepTitleText, value ?? string.Empty);
        }

        public string CurrentYoloTrainingStepDetailText
        {
            get => currentYoloTrainingStepDetailText;
            private set => SetProperty(ref currentYoloTrainingStepDetailText, value ?? string.Empty);
        }

        public string CurrentYoloTrainingActionText
        {
            get => currentYoloTrainingActionText;
            private set => SetProperty(ref currentYoloTrainingActionText, value ?? string.Empty);
        }

        public bool HasCurrentYoloTrainingStep
        {
            get => hasCurrentYoloTrainingStep;
            private set => SetProperty(ref hasCurrentYoloTrainingStep, value);
        }

        public bool IsYoloFixClassesEnabled
        {
            get => isYoloFixClassesEnabled;
            private set => SetProperty(ref isYoloFixClassesEnabled, value);
        }

        public bool IsYoloFixLabelsEnabled
        {
            get => isYoloFixLabelsEnabled;
            private set => SetProperty(ref isYoloFixLabelsEnabled, value);
        }

        public bool IsYoloFixDatasetEnabled
        {
            get => isYoloFixDatasetEnabled;
            private set => SetProperty(ref isYoloFixDatasetEnabled, value);
        }

        private void RegisterAnnotationTool(WpfAnnotationToolItem tool)
        {
            if (tool == null)
            {
                return;
            }

            AnnotationTools.Add(tool);
            // The guide separates persistent drawing tools from one-shot edit commands;
            // the full AnnotationTools list stays as the shared source for canvas toolbar state.
            if (IsOneShotCommandTool(tool.Tool))
            {
                AnnotationCommandTools.Add(tool);
                return;
            }

            SelectableAnnotationTools.Add(tool);
        }

        private static bool IsOneShotCommandTool(WpfAnnotationTool tool)
            => tool == WpfAnnotationTool.Undo
                || tool == WpfAnnotationTool.Redo
                || tool == WpfAnnotationTool.Delete;

        private void RefreshAnnotationToolScopeForMode(WpfLearningMode mode)
        {
            // Dataset purpose owns which drawing tools are visible. Keep the full
            // AnnotationTools catalog for commands/tests, but only expose tools that
            // match the labeling task so operators do not see irrelevant controls.
            List<WpfAnnotationTool> visibleSelectableTools = ResolveSelectableToolsForMode(mode).ToList();
            SelectableAnnotationTools.Clear();
            AnnotationCommandTools.Clear();
            VisibleAnnotationTools.Clear();

            foreach (WpfAnnotationTool toolKind in visibleSelectableTools)
            {
                WpfAnnotationToolItem tool = AnnotationTools.FirstOrDefault(candidate => candidate.Tool == toolKind);
                if (tool != null)
                {
                    SelectableAnnotationTools.Add(tool);
                    VisibleAnnotationTools.Add(tool);
                }
            }

            foreach (WpfAnnotationToolItem tool in AnnotationTools.Where(tool => mode != WpfLearningMode.AnomalyDetection && IsOneShotCommandTool(tool.Tool)))
            {
                AnnotationCommandTools.Add(tool);
                VisibleAnnotationTools.Add(tool);
            }

            if (SelectedTool == null
                || !SelectableAnnotationTools.Contains(SelectedTool)
                || ShouldPreferModeDefaultTool(mode, SelectedTool.Tool))
            {
                SelectedTool = ResolvePreferredSelectableToolForMode(mode) ?? SelectableAnnotationTools.FirstOrDefault();
            }
        }

        private static IEnumerable<WpfAnnotationTool> ResolveSelectableToolsForMode(WpfLearningMode mode)
        {
            switch (mode)
            {
                case WpfLearningMode.Segmentation:
                    return new[]
                    {
                        WpfAnnotationTool.Rectangle,
                        WpfAnnotationTool.Brush,
                        WpfAnnotationTool.Eraser,
                        WpfAnnotationTool.Polygon,
                        WpfAnnotationTool.Select,
                        WpfAnnotationTool.PanZoom
                    };

                case WpfLearningMode.AnomalyDetection:
                    return new[]
                    {
                        WpfAnnotationTool.PanZoom
                    };

                case WpfLearningMode.ObjectDetection:
                case WpfLearningMode.Train:
                case WpfLearningMode.Infer:
                case WpfLearningMode.Review:
                    return new[]
                    {
                        WpfAnnotationTool.Select,
                        WpfAnnotationTool.Rectangle,
                        WpfAnnotationTool.PanZoom
                    };

                default:
                    return new[]
                    {
                        WpfAnnotationTool.Select,
                        WpfAnnotationTool.Rectangle,
                        WpfAnnotationTool.Polygon,
                        WpfAnnotationTool.Brush,
                        WpfAnnotationTool.Eraser,
                        WpfAnnotationTool.PanZoom
                    };
            }
        }

        private WpfAnnotationToolItem ResolvePreferredSelectableToolForMode(WpfLearningMode mode)
        {
            WpfAnnotationTool preferredTool = mode == WpfLearningMode.Segmentation
                ? WpfAnnotationTool.Brush
                : WpfAnnotationTool.Select;
            return SelectableAnnotationTools.FirstOrDefault(item => item.Tool == preferredTool);
        }

        private static bool ShouldPreferModeDefaultTool(WpfLearningMode mode, WpfAnnotationTool currentTool)
            => (mode == WpfLearningMode.Segmentation && currentTool == WpfAnnotationTool.Select)
                || (mode == WpfLearningMode.AnomalyDetection
                    && (currentTool == WpfAnnotationTool.Brush || currentTool == WpfAnnotationTool.Eraser))
                || (mode == WpfLearningMode.ObjectDetection && currentTool == WpfAnnotationTool.PanZoom);

        public void ApplyDatasetPurpose(LabelingDatasetPurpose purpose)
        {
            WpfLearningMode mode = ToLearningMode(purpose);
            SelectedDatasetPurposeMode = DatasetPurposeModes.FirstOrDefault(item => item.Mode == mode)
                ?? DatasetPurposeModes.FirstOrDefault(item => item.Mode == WpfLearningMode.ObjectDetection);
        }

        public LabelingDatasetPurpose GetSelectedDatasetPurpose()
            => ToDatasetPurpose(SelectedDatasetPurposeMode?.Mode ?? WpfLearningMode.ObjectDetection);

        public static WpfLearningMode ToLearningMode(LabelingDatasetPurpose purpose)
        {
            return purpose switch
            {
                LabelingDatasetPurpose.Segmentation => WpfLearningMode.Segmentation,
                LabelingDatasetPurpose.AnomalyDetection => WpfLearningMode.AnomalyDetection,
                _ => WpfLearningMode.ObjectDetection
            };
        }

        public static LabelingDatasetPurpose ToDatasetPurpose(WpfLearningMode mode)
        {
            return mode switch
            {
                WpfLearningMode.Segmentation => LabelingDatasetPurpose.Segmentation,
                WpfLearningMode.AnomalyDetection => LabelingDatasetPurpose.AnomalyDetection,
                _ => LabelingDatasetPurpose.ObjectDetection
            };
        }

        public void ConfigureCommands(
            Action<object> datasetPurposeSelectionChanged,
            Action<object> datasetSetupStart,
            Action<object> learningModeSelectionChanged,
            Action<object> annotationToolSelectionChanged,
            Action<object> learningStepSelectionChanged,
            Action<WpfYoloTrainingWorkflowStepItem> yoloTrainingWorkflowStep,
            Action tutorialOpenHtmlGuide,
            Action yoloFixClasses,
            Action yoloFixLabels,
            Action yoloFixDataset,
            Action<WpfDatasetDashboardMetricItem> datasetDashboardMetricSelected = null,
            Action runModelComparison = null,
            Action datasetOpenExisting = null,
            Action<WpfFirstRunChecklistItem> firstRunSamplePathSelected = null,
            Action runTemplateCurrentImage = null,
            Action runTemplateBatch = null,
            Action runExternalEvaluationDataAudit = null,
            Action selectExternalYoloDataset = null,
            Action activateExternalYoloDataset = null,
            Action clearExternalYoloDataset = null)
        {
            // Dataset purpose is a project setting; learning mode is only guide/navigation.
            // Keep both command paths separate so task-specific tools do not change when the operator browses lesson concepts.
            DatasetPurposeSelectionChangedCommand = new RelayCommand<object>(datasetPurposeSelectionChanged ?? NoOpSelectionCommand);
            DatasetSetupStartCommand = new RelayCommand<object>(datasetSetupStart ?? NoOpSelectionCommand);
            DatasetOpenExistingCommand = new RelayCommand(datasetOpenExisting ?? NoOpCommand);
            LearningModeSelectionChangedCommand = new RelayCommand<object>(learningModeSelectionChanged ?? NoOpSelectionCommand);
            AnnotationToolSelectionChangedCommand = new RelayCommand<object>(annotationToolSelectionChanged ?? NoOpSelectionCommand);
            LearningStepSelectionChangedCommand = new RelayCommand<object>(learningStepSelectionChanged ?? NoOpSelectionCommand);
            YoloTrainingWorkflowStepCommand = new RelayCommand<WpfYoloTrainingWorkflowStepItem>(yoloTrainingWorkflowStep ?? NoOpTrainingStepCommand);
            FirstRunSamplePathCommand = new RelayCommand<WpfFirstRunChecklistItem>(firstRunSamplePathSelected ?? NoOpFirstRunSamplePathCommand);
            DatasetDashboardMetricCommand = new RelayCommand<WpfDatasetDashboardMetricItem>(datasetDashboardMetricSelected ?? NoOpDatasetDashboardMetricCommand);
            TutorialOpenHtmlGuideCommand = new RelayCommand(tutorialOpenHtmlGuide ?? NoOpCommand);
            YoloFixClassesCommand = new RelayCommand(yoloFixClasses ?? NoOpCommand);
            YoloFixLabelsCommand = new RelayCommand(yoloFixLabels ?? NoOpCommand);
            YoloFixDatasetCommand = new RelayCommand(yoloFixDataset ?? NoOpCommand);
            RunModelComparisonCommand = new RelayCommand(runModelComparison ?? NoOpCommand);
            ExternalEvaluationDataAuditCommand = new RelayCommand(runExternalEvaluationDataAudit ?? NoOpCommand);
            SelectExternalYoloDatasetCommand = new RelayCommand(selectExternalYoloDataset ?? NoOpCommand);
            ActivateExternalYoloDatasetCommand = new RelayCommand(activateExternalYoloDataset ?? NoOpCommand);
            ClearExternalYoloDatasetCommand = new RelayCommand(clearExternalYoloDataset ?? NoOpCommand);
            TemplateCurrentImageCommand = new RelayCommand(runTemplateCurrentImage ?? NoOpCommand);
            TemplateBatchCommand = new RelayCommand(runTemplateBatch ?? NoOpCommand);
        }

        public void SetYoloFixActionAvailability(bool canFixClasses, bool canFixLabels, bool canFixDataset)
        {
            IsYoloFixClassesEnabled = canFixClasses;
            IsYoloFixLabelsEnabled = canFixLabels;
            IsYoloFixDatasetEnabled = canFixDataset;
        }

        public void SetTrainingRunHistoryItems(IEnumerable<string> items)
        {
            TrainingRunHistoryItems.Clear();
            foreach (string item in items ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    TrainingRunHistoryItems.Add(item);
                }
            }
        }

        public void SetTrainingResultReportItems(IEnumerable<WpfTrainingResultReportItem> items)
        {
            TrainingResultReportItems.Clear();
            foreach (WpfTrainingResultReportItem item in items ?? Enumerable.Empty<WpfTrainingResultReportItem>())
            {
                if (item != null)
                {
                    TrainingResultReportItems.Add(item);
                }
            }
        }

        public void SetTrainingHistoryText(string historyText)
        {
            trainingHistorySourceText = historyText ?? string.Empty;
            RefreshTrainingComparisonLocalization();
        }

        public void SetTrainingComparisonResultTexts(
            string summaryText = null,
            string comparisonText = null,
            string adoptionDecisionText = null)
        {
            if (summaryText != null)
            {
                trainingResultComparisonSummarySourceText = summaryText;
            }

            if (comparisonText != null)
            {
                trainingResultComparisonSourceText = comparisonText;
            }

            if (adoptionDecisionText != null)
            {
                trainingModelAdoptionDecisionSourceText = adoptionDecisionText;
            }

            RefreshTrainingComparisonLocalization();
        }

        public void SetTrainingComparisonLocalization(WpfTrainingComparisonLocalizationSnapshot localization)
        {
            localization ??= WpfTrainingComparisonLocalizationService.CreateInitial();
            trainingComparisonLocalizationSnapshot = localization;
            refreshingTrainingComparisonLocalization = true;
            try
            {
                TrainingHistoryText = localization.HistoryText;
                TrainingResultComparisonSummaryText = localization.SummaryText;
                TrainingResultComparisonText = localization.ComparisonText;
                TrainingModelAdoptionDecisionText = localization.AdoptionDecisionText;
                RunModelComparisonActionText = localization.RunActionText;
                RunModelComparisonToolTipText = localization.RunToolTipText;
                ModelComparisonBasisText = localization.ComparisonBasisText;
            }
            finally
            {
                refreshingTrainingComparisonLocalization = false;
            }
        }

        private void RefreshTrainingComparisonLocalization()
        {
            SetTrainingComparisonLocalization(
                WpfTrainingComparisonLocalizationService.Build(
                    trainingHistorySourceText,
                    trainingResultComparisonSummarySourceText,
                    trainingResultComparisonSourceText,
                    trainingModelAdoptionDecisionSourceText,
                    runModelComparisonActionSourceText,
                    runModelComparisonToolTipSourceText,
                    modelComparisonBasisSourceText));
        }

        public void SetModelComparisonRunState(bool enabled, string actionText)
            => SetModelComparisonRunState(enabled, actionText, string.Empty);

        public void SetModelComparisonRunState(bool enabled, string actionText, string toolTipText)
            => SetModelComparisonRunState(enabled, actionText, toolTipText, string.Empty);

        public void SetModelComparisonRunState(bool enabled, string actionText, string toolTipText, string basisText)
        {
            IsRunModelComparisonEnabled = enabled;
            runModelComparisonActionSourceText = actionText ?? string.Empty;
            runModelComparisonToolTipSourceText = toolTipText ?? string.Empty;
            modelComparisonBasisSourceText = basisText ?? string.Empty;
            RefreshTrainingComparisonLocalization();
        }

        public void SetModelReplacementReadiness(string statusText, string detailText)
        {
            // Training readiness and model replacement are intentionally separate:
            // Keep replacement stricter than training readiness: users may train with learning/validation data only,
            // but switching the active model needs a separate final-verification set.
            modelReplacementLocalizationSnapshot = null;
            ModelReplacementStatusText = string.IsNullOrWhiteSpace(statusText)
                ? T("WpfLearningWorkflow.ModelReplacement.Status.Initial")
                : statusText;
            ModelReplacementDetailText = string.IsNullOrWhiteSpace(detailText)
                ? T("WpfLearningWorkflow.ModelReplacement.Detail.Initial")
                : detailText;
        }

        public void SetModelReplacementLocalization(WpfModelReplacementLocalizationSnapshot localization)
        {
            localization ??= WpfModelReplacementLocalizationService.CreateInitial();
            modelReplacementLocalizationSnapshot = localization;
            refreshingModelReplacementLocalization = true;
            try
            {
                ModelReplacementStatusText = localization.StatusText;
                ModelReplacementDetailText = localization.DetailText;
            }
            finally
            {
                refreshingModelReplacementLocalization = false;
            }
        }

        public void SetTrainingModelLifecycleState(
            string currentModelText,
            string candidateModelText,
            string decisionText,
            string nextActionText)
        {
            SetTrainingModelLifecycleLocalization(
                WpfTrainingModelLifecycleLocalizationService.Build(
                    currentModelText,
                    candidateModelText,
                    decisionText,
                    nextActionText));
        }

        public void SetTrainingModelLifecycleLocalization(
            WpfTrainingModelLifecycleLocalizationSnapshot localization)
        {
            localization ??= WpfTrainingModelLifecycleLocalizationService.CreateInitial();
            trainingModelLifecycleLocalizationSnapshot = localization;
            refreshingTrainingModelLifecycleLocalization = true;
            try
            {
                TrainingModelLifecycleCurrentText = localization.CurrentText;
                TrainingModelLifecycleCandidateText = localization.CandidateText;
                TrainingModelLifecycleDecisionText = localization.DecisionText;
                TrainingModelLifecycleNextActionText = localization.NextActionText;
            }
            finally
            {
                refreshingTrainingModelLifecycleLocalization = false;
            }
        }

        public void SetDatasetDashboard(
            string statusText,
            string summaryText,
            string actionText,
            IEnumerable<WpfDatasetDashboardMetricItem> metrics,
            IEnumerable<string> issues,
            WpfDatasetDashboardLocalizationSnapshot localization = null)
        {
            List<WpfDatasetDashboardMetricItem> metricItems = (metrics ?? Enumerable.Empty<WpfDatasetDashboardMetricItem>())
                .Where(item => item != null)
                .ToList();
            datasetDashboardLocalizationSnapshot = localization?.WithMetricItems(metricItems);
            DatasetDashboardStatusText = datasetDashboardLocalizationSnapshot?.StatusText
                ?? (string.IsNullOrWhiteSpace(statusText)
                    ? T("WpfLearningWorkflow.DatasetDashboard.Status.Before")
                    : statusText);
            DatasetDashboardSummaryText = datasetDashboardLocalizationSnapshot?.SummaryText
                ?? (string.IsNullOrWhiteSpace(summaryText) ? string.Empty : summaryText);
            string localizedActionText = datasetDashboardLocalizationSnapshot?.ActionText;
            DatasetDashboardActionText = string.IsNullOrWhiteSpace(localizedActionText)
                ? (string.IsNullOrWhiteSpace(actionText) ? string.Empty : actionText)
                : localizedActionText;
            ObjectDetectionMvpNextActionText = BuildObjectDetectionMvpNextActionText(DatasetDashboardActionText);

            DatasetDashboardMetrics.Clear();
            IEnumerable<WpfDatasetDashboardMetricItem> localizedMetrics = datasetDashboardLocalizationSnapshot?.MetricItems
                ?? metricItems;
            foreach (WpfDatasetDashboardMetricItem item in localizedMetrics)
            {
                DatasetDashboardMetrics.Add(item);
            }

            IEnumerable<string> localizedIssues = datasetDashboardLocalizationSnapshot?.IssueItems
                ?? issues
                ?? Enumerable.Empty<string>();
            DatasetDashboardIssueItems.Clear();
            foreach (string issue in localizedIssues)
            {
                if (!string.IsNullOrWhiteSpace(issue))
                {
                    DatasetDashboardIssueItems.Add(issue);
                }
            }

            if (DatasetDashboardIssueItems.Count == 0)
            {
                DatasetDashboardIssueItems.Add(T("WpfLearningWorkflow.DatasetDashboard.Issue.NoIssues"));
            }
        }

        public void SetExternalEvaluationDataAuditResult(string statusText, string detailText, string pathText)
        {
            ExternalEvaluationDataAuditStatusText = string.IsNullOrWhiteSpace(statusText)
                ? "\uC678\uBD80 \uD3C9\uAC00 \uB300\uC870: \uC810\uAC80 \uC804"
                : statusText;
            ExternalEvaluationDataAuditDetailText = detailText ?? string.Empty;
            ExternalEvaluationDataAuditPathText = pathText ?? string.Empty;
        }

        public LabelingDatasetPurpose GetSelectedExternalYoloDatasetPurpose()
            => ToDatasetPurpose(SelectedExternalYoloDatasetPurposeMode?.Mode ?? WpfLearningMode.ObjectDetection);

        public void SetExternalYoloDatasetIntakeResult(
            LabelingDatasetPurpose purpose,
            string statusText,
            string detailText,
            string pathText)
        {
            WpfLearningMode mode = ToLearningMode(purpose);
            SelectedExternalYoloDatasetPurposeMode = ExternalYoloDatasetPurposeModes.FirstOrDefault(item => item.Mode == mode)
                ?? ExternalYoloDatasetPurposeModes.FirstOrDefault();
            ExternalYoloDatasetIntakeStatusText = string.IsNullOrWhiteSpace(statusText)
                ? "\uC678\uBD80 YOLO data.yaml: \uC120\uD0DD \uC548 \uD568"
                : statusText;
            ExternalYoloDatasetIntakeDetailText = detailText ?? string.Empty;
            ExternalYoloDatasetIntakePathText = pathText ?? string.Empty;
        }

        private static string BuildObjectDetectionMvpNextActionText(string actionText)
        {
            if (string.IsNullOrWhiteSpace(actionText))
            {
                return T("WpfLearningWorkflow.ObjectDetectionMvpNextAction.Empty");
            }

            string normalized = actionText.Trim();
            if (normalized.StartsWith("\uC644\uB8CC:", StringComparison.Ordinal))
            {
                return T("WpfLearningWorkflow.ObjectDetectionMvpNextAction.Complete");
            }

            const string nextPrefix = "\uB2E4\uC74C:";
            if (normalized.StartsWith(nextPrefix, StringComparison.Ordinal))
            {
                normalized = normalized.Substring(nextPrefix.Length).Trim();
            }

            return Format(
                "WpfLearningWorkflow.ObjectDetectionMvpNextAction.Dynamic",
                WpfLocalizationTextRuntimeService.Translate(normalized));
        }

        private static IEnumerable<WpfDatasetDashboardMetricItem> BuildInitialDatasetDashboardMetrics()
        {
            yield return WpfDatasetDashboardLocalizationService.CreateMetric(
                "WpfLearningWorkflow.DatasetDashboard.Metric.Images.Title",
                "-",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Before",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Waiting",
                PackIconMaterialKind.FolderImage,
                isProblem: false,
                isWarning: false,
                actionKind: WpfDatasetDashboardActionKind.OpenImages);
            yield return WpfDatasetDashboardLocalizationService.CreateMetric(
                "WpfLearningWorkflow.DatasetDashboard.Metric.Progress.Title",
                "-",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Before",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Waiting",
                PackIconMaterialKind.ProgressClock,
                isProblem: false,
                isWarning: false,
                actionKind: WpfDatasetDashboardActionKind.OpenLabelingProgress);
            yield return WpfDatasetDashboardLocalizationService.CreateMetric(
                "WpfLearningWorkflow.DatasetDashboard.Metric.Initial.Labels.Title",
                "-",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Before",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Waiting",
                PackIconMaterialKind.ShapeSquareRoundedPlus,
                isProblem: false,
                isWarning: false,
                actionKind: WpfDatasetDashboardActionKind.OpenLabelingTool);
            yield return WpfDatasetDashboardLocalizationService.CreateMetric(
                "WpfLearningWorkflow.DatasetDashboard.Metric.Split.Title",
                "-",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Before",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Waiting",
                PackIconMaterialKind.CheckAll,
                isProblem: false,
                isWarning: false,
                actionKind: WpfDatasetDashboardActionKind.OpenDatasetSettings);
            yield return WpfDatasetDashboardLocalizationService.CreateMetric(
                "WpfLearningWorkflow.DatasetDashboard.Metric.Class.Title",
                "-",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Before",
                "WpfLearningWorkflow.DatasetDashboard.Metric.State.Waiting",
                PackIconMaterialKind.TagMultipleOutline,
                isProblem: false,
                isWarning: false,
                actionKind: WpfDatasetDashboardActionKind.OpenClassCatalog);
        }

        public void SetYoloTrainingStepState(int order, bool isCompleted, string stateText)
        {
            WpfYoloTrainingWorkflowStepItem step = YoloTrainingWorkflowSteps.FirstOrDefault(item => item.Order == order);
            if (step == null)
            {
                return;
            }

            step.IsCompleted = isCompleted;
            step.StateText = string.IsNullOrWhiteSpace(stateText) ? (isCompleted ? "완료" : "대기") : stateText;
            step.StateIconKind = isCompleted ? PackIconMaterialKind.CheckCircleOutline : PackIconMaterialKind.ClockOutline;
            RefreshCurrentYoloTrainingStep();
        }

        private void RefreshCurrentYoloTrainingStep()
        {
            WpfYoloTrainingWorkflowStepItem nextStep = YoloTrainingWorkflowSteps.FirstOrDefault(item => !item.IsCompleted)
                ?? YoloTrainingWorkflowSteps.LastOrDefault();
            CurrentYoloTrainingStep = nextStep;
        }

        private static string ResolveCurrentYoloTrainingActionText(WpfYoloTrainingWorkflowStepItem step)
        {
            return step?.Order switch
            {
                1 => "데이터셋 만들기",
                2 => "이미지 불러오기",
                3 => "클래스 등록",
                4 => "라벨링 시작",
                5 => "데이터셋 점검",
                6 => "학습 설정 확인",
                7 => "AI 후보 검토",
                _ => "이 단계로 이동"
            };
        }

        public void SetAnnotationHistoryState(bool canUndo, bool canRedo, string undoActionName, string redoActionName)
        {
            SetAnnotationToolRuntimeState(
                WpfAnnotationTool.Undo,
                canUndo,
                canUndo ? "\uAC00\uB2A5" : "\uC5C6\uC74C",
                canUndo
                    ? "\uB418\uB3CC\uB9AC\uAE30 \uAC00\uB2A5" + FormatHistoryActionSuffix(undoActionName)
                    : "\uB418\uB3CC\uB9B4 \uD3B8\uC9D1 \uC774\uB825\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.");
            SetAnnotationToolRuntimeState(
                WpfAnnotationTool.Redo,
                canRedo,
                canRedo ? "\uAC00\uB2A5" : "\uC5C6\uC74C",
                canRedo
                    ? "\uB2E4\uC2DC \uC801\uC6A9 \uAC00\uB2A5" + FormatHistoryActionSuffix(redoActionName)
                    : "\uB2E4\uC2DC \uC801\uC6A9\uD560 \uD3B8\uC9D1 \uC774\uB825\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        private void SetAnnotationToolRuntimeState(WpfAnnotationTool tool, bool isEnabled, string stateText, string statusText)
        {
            WpfAnnotationToolItem item = AnnotationTools.FirstOrDefault(candidate => candidate.Tool == tool);
            item?.SetRuntimeAvailability(isEnabled, stateText, statusText);
        }

        private static string FormatHistoryActionSuffix(string actionName)
            => string.IsNullOrWhiteSpace(actionName) ? string.Empty : $": {actionName}";

        public string DatasetPurposeHeaderText => T("WpfLearningWorkflow.DatasetPurposeHeader");

        public string DatasetSetupSectionTitleText => T("WpfLearningWorkflow.DatasetSetupSectionTitle");

        public string DatasetOpenExistingButtonText => T("WpfLearningWorkflow.DatasetOpenExisting");

        public string DatasetOpenExistingToolTipText => T("WpfLearningWorkflow.DatasetOpenExisting.ToolTip");

        public string DatasetSetupStartToolTipText => T("WpfLearningWorkflow.DatasetSetupStart.ToolTip");

        public string DatasetPurposeSummaryText
        {
            get => datasetPurposeSummaryText;
            private set => SetProperty(ref datasetPurposeSummaryText, value ?? string.Empty);
        }

        public string DatasetPurposeToolSummaryText
        {
            get => datasetPurposeToolSummaryText;
            private set => SetProperty(ref datasetPurposeToolSummaryText, value ?? string.Empty);
        }

        public string DatasetSetupFirstActionText
        {
            get => datasetSetupFirstActionText;
            private set => SetProperty(ref datasetSetupFirstActionText, value ?? string.Empty);
        }

        public string DatasetSetupActionText
        {
            get => datasetSetupActionText;
            private set => SetProperty(ref datasetSetupActionText, value ?? string.Empty);
        }

        public string CurrentWorkflowActionText
        {
            get => currentWorkflowActionText;
            private set => SetProperty(ref currentWorkflowActionText, value ?? string.Empty);
        }

        public string DatasetSetupStatusText
        {
            get => datasetSetupStatusText;
            set => SetProperty(ref datasetSetupStatusText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskStepText
        {
            get => currentLabelingTaskStepText;
            private set => SetProperty(ref currentLabelingTaskStepText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskToolText
        {
            get => currentLabelingTaskToolText;
            private set => SetProperty(ref currentLabelingTaskToolText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskActionText
        {
            get => currentLabelingTaskActionText;
            private set => SetProperty(ref currentLabelingTaskActionText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskChecklistFirstText
        {
            get => currentLabelingTaskChecklistFirstText;
            private set => SetProperty(ref currentLabelingTaskChecklistFirstText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskChecklistSecondText
        {
            get => currentLabelingTaskChecklistSecondText;
            private set => SetProperty(ref currentLabelingTaskChecklistSecondText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskChecklistThirdText
        {
            get => currentLabelingTaskChecklistThirdText;
            private set => SetProperty(ref currentLabelingTaskChecklistThirdText, value ?? string.Empty);
        }

        public string CurrentLabelingTaskChecklistSummaryText
        {
            get => currentLabelingTaskChecklistSummaryText;
            private set => SetProperty(ref currentLabelingTaskChecklistSummaryText, value ?? string.Empty);
        }

        public Visibility DatasetOnboardingVisibility
        {
            get => datasetOnboardingVisibility;
            private set => SetProperty(ref datasetOnboardingVisibility, value);
        }

        public Visibility LabelingTaskVisibility
        {
            get => labelingTaskVisibility;
            private set => SetProperty(ref labelingTaskVisibility, value);
        }

        public void ShowDatasetOnboarding()
        {
            DatasetOnboardingVisibility = Visibility.Visible;
            LabelingTaskVisibility = Visibility.Collapsed;
        }

        public void ShowLabelingTask()
        {
            DatasetOnboardingVisibility = Visibility.Collapsed;
            LabelingTaskVisibility = Visibility.Visible;
        }

        public void SetLiveLabelingTask(string stepText, string toolText, string actionText)
        {
            CurrentLabelingTaskStepText = string.IsNullOrWhiteSpace(stepText)
                ? "\uB77C\uBCA8"
                : stepText.Trim();
            CurrentLabelingTaskToolText = string.IsNullOrWhiteSpace(toolText)
                ? "\uB3C4\uAD6C: \uC120\uD0DD"
                : FormatLiveLabelingTaskToolText(toolText);
            CurrentLabelingTaskActionText = string.IsNullOrWhiteSpace(actionText)
                ? "\uB77C\uBCA8\uC744 \uADF8\uB9AC\uACE0 \uACB0\uACFC\uB97C \uD655\uC778\uD55C \uB4A4 \uB77C\uBCA8 \uC800\uC7A5\uC744 \uB204\uB974\uC138\uC694."
                : actionText.Trim();
            UpdateLiveLabelingTaskChecklist(
                CurrentLabelingTaskStepText,
                CurrentLabelingTaskToolText,
                CurrentLabelingTaskActionText);
        }

        private static string FormatLiveLabelingTaskToolText(string toolText)
        {
            string value = toolText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "\uB3C4\uAD6C: \uC120\uD0DD";
            }

            if (value.IndexOf("\uAC80\uC0AC", StringComparison.Ordinal) >= 0
                || value.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("\uD050", StringComparison.Ordinal) >= 0)
            {
                return $"\uBAA8\uB4DC: {value}";
            }

            return $"\uB3C4\uAD6C: {value}";
        }

        private void UpdateLiveLabelingTaskChecklist(string stepText, string toolText, string actionText)
        {
            string normalizedStep = stepText?.Trim() ?? string.Empty;
            if (normalizedStep.IndexOf("\uAC80\uD1A0", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uD655\uC778", "\uD655\uC815", "\uC2A4\uD0B5");
                return;
            }

            if (normalizedStep.IndexOf("\uCD94\uB860", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uAC80\uC0AC", "\uD655\uC778", "\uAC80\uD1A0");
                return;
            }

            if (normalizedStep.IndexOf("\uC800\uC7A5", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uD655\uC778", "\uC800\uC7A5", "\uB2E4\uC74C");
                return;
            }

            if (normalizedStep.IndexOf("\uC0D8\uD50C", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uC774\uBBF8\uC9C0", "\uC5F4\uAE30", "\uB77C\uBCA8");
                return;
            }

            if (normalizedStep.IndexOf("\uB77C\uBCA8", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uADF8\uB9AC\uAE30", "\uC800\uC7A5", "\uB2E4\uC74C");
                return;
            }

            string combined = string.Join(
                " ",
                new[] { stepText, toolText, actionText }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (combined.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("\uD6C4\uBCF4", StringComparison.Ordinal) >= 0
                || combined.IndexOf("\uAC80\uD1A0", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uD655\uC778", "\uD655\uC815", "\uC2A4\uD0B5");
                return;
            }

            if (combined.IndexOf("\uCD94\uB860", StringComparison.Ordinal) >= 0
                || combined.IndexOf("\uAC80\uC0AC", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uAC80\uC0AC", "\uD655\uC778", "\uAC80\uD1A0");
                return;
            }

            if (combined.IndexOf("\uC800\uC7A5", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uD655\uC778", "\uC800\uC7A5", "\uB2E4\uC74C");
                return;
            }

            if (combined.IndexOf("\uC774\uBBF8\uC9C0 \uD050", StringComparison.Ordinal) >= 0
                || combined.IndexOf("\uC0D8\uD50C", StringComparison.Ordinal) >= 0)
            {
                SetCurrentLabelingTaskChecklist("\uC774\uBBF8\uC9C0", "\uC5F4\uAE30", "\uB77C\uBCA8");
                return;
            }

            SetCurrentLabelingTaskChecklist("\uADF8\uB9AC\uAE30", "\uC800\uC7A5", "\uB2E4\uC74C");
        }

        private void SetCurrentLabelingTaskChecklist(string first, string second, string third)
        {
            CurrentLabelingTaskChecklistFirstText = $"1  {first}";
            CurrentLabelingTaskChecklistSecondText = $"2  {second}";
            CurrentLabelingTaskChecklistThirdText = $"3  {third}";
            CurrentLabelingTaskChecklistSummaryText = $"\uD750\uB984: {first} > {second} > {third}";
        }

        public string ModeDetailText
        {
            get => modeDetailText;
            private set => SetProperty(ref modeDetailText, value ?? string.Empty);
        }

        public string StepDetailText
        {
            get => stepDetailText;
            private set => SetProperty(ref stepDetailText, value ?? string.Empty);
        }

        public string ToolDetailText
        {
            get => toolDetailText;
            private set => SetProperty(ref toolDetailText, value ?? string.Empty);
        }

        public int BrushSize
        {
            get => brushSize;
            set => SetProperty(ref brushSize, Math.Clamp(value, 2, 64));
        }

        public double MaskOpacity
        {
            get => maskOpacity;
            set
            {
                double normalized = Math.Clamp(value, 0.1, 1.0);
                if (SetProperty(ref maskOpacity, normalized))
                {
                    SetProperty(ref maskOpacityPercentText, $"{normalized:P0}", nameof(MaskOpacityPercentText));
                }
            }
        }

        private string maskOpacityPercentText = "66%";

        public string MaskOpacityPercentText
        {
            get => maskOpacityPercentText;
            private set => SetProperty(ref maskOpacityPercentText, value ?? string.Empty);
        }

        public WpfLearningModeItem SelectedDatasetPurposeMode
        {
            get => selectedDatasetPurposeMode;
            set
            {
                if (SetProperty(ref selectedDatasetPurposeMode, value))
                {
                    RefreshAnnotationToolScopeForMode(value?.Mode ?? WpfLearningMode.ObjectDetection);
                    RefreshLessonText();
                }
            }
        }

        public WpfLearningModeItem SelectedExternalYoloDatasetPurposeMode
        {
            get => selectedExternalYoloDatasetPurposeMode;
            set => SetProperty(ref selectedExternalYoloDatasetPurposeMode, value);
        }

        public WpfLearningModeItem SelectedMode
        {
            get => selectedMode;
            set
            {
                if (SetProperty(ref selectedMode, value))
                {
                    RefreshLessonText();
                }
            }
        }

        public WpfAnnotationToolItem SelectedTool
        {
            get => selectedTool;
            set
            {
                if (SetProperty(ref selectedTool, value))
                {
                    RefreshLessonText();
                }
            }
        }

        public WpfLearningStepItem SelectedStep
        {
            get => selectedStep;
            set
            {
                if (SetProperty(ref selectedStep, value))
                {
                    RefreshLessonText();
                }
            }
        }

        private void RefreshLessonText()
        {
            // Keep dataset-purpose UX copy in the ViewModel so the panel remains a display-only composition surface.
            DatasetPurposeSummaryText = ResolveReadableDatasetPurposeSummaryText(SelectedDatasetPurposeMode?.Mode ?? WpfLearningMode.ObjectDetection);

            DatasetPurposeToolSummaryText = SelectedDatasetPurposeMode?.Mode switch
            {
                WpfLearningMode.ObjectDetection => T("WpfLearningWorkflow.ToolSummary.ObjectDetection"),
                WpfLearningMode.Segmentation => T("WpfLearningWorkflow.ToolSummary.Segmentation"),
                WpfLearningMode.AnomalyDetection => T("WpfLearningWorkflow.ToolSummary.AnomalyDetection"),
                _ => T("WpfLearningWorkflow.ToolSummary.Default")
            };

            DatasetSetupActionText = SelectedDatasetPurposeMode?.Mode switch
            {
                WpfLearningMode.ObjectDetection => T("WpfLearningWorkflow.DatasetSetupAction"),
                WpfLearningMode.Segmentation => T("WpfLearningWorkflow.DatasetSetupAction"),
                WpfLearningMode.AnomalyDetection => T("WpfLearningWorkflow.DatasetSetupAction"),
                _ => T("WpfLearningWorkflow.DatasetSetupStatus.Before")
            };

            DatasetSetupFirstActionText = SelectedDatasetPurposeMode?.Mode switch
            {
                WpfLearningMode.ObjectDetection => T("WpfLearningWorkflow.FirstAction.ObjectDetection"),
                WpfLearningMode.Segmentation => T("WpfLearningWorkflow.FirstAction.Segmentation"),
                WpfLearningMode.AnomalyDetection => T("WpfLearningWorkflow.FirstAction.AnomalyDetection"),
                _ => T("WpfLearningWorkflow.FirstAction.Default")
            };

            ModeDetailText = ResolveReadableModeDetailText(SelectedMode?.Mode ?? WpfLearningMode.LabelingBasics);

            StepDetailText = ResolveReadableStepDetailText(
                SelectedStep?.Step,
                SelectedDatasetPurposeMode?.Mode ?? WpfLearningMode.ObjectDetection);

            CurrentWorkflowActionText = SelectedStep?.Step switch
            {
                WpfLearningStep.Sample => "\uB2E4\uC74C: \uC774\uBBF8\uC9C0 \uD3F4\uB354\uB97C \uC5F4\uACE0 \uCCAB \uC774\uBBF8\uC9C0\uB97C \uC120\uD0DD\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Label => SelectedDatasetPurposeMode?.Mode switch
                {
                    WpfLearningMode.Segmentation => "\uB2E4\uC74C: \uD3F4\uB9AC\uACE4/\uBE0C\uB7EC\uC2DC\uB85C \uB9C8\uC2A4\uD06C\uB97C \uB9CC\uB4E4\uACE0 \uC800\uC7A5\uD569\uB2C8\uB2E4.",
                    WpfLearningMode.AnomalyDetection => "다음: 이미지 전체를 정상(OK) 또는 이상(NG)으로 판정하고 다음 이미지로 이동합니다.",
                    _ => "\uB2E4\uC74C: \uBC15\uC2A4\uB97C \uADF8\uB9AC\uACE0 \uD074\uB798\uC2A4\uAC00 \uB9DE\uB294\uC9C0 \uD655\uC778\uD569\uB2C8\uB2E4."
                },
                WpfLearningStep.Infer => "\uB2E4\uC74C: \uCD94\uB860\uC744 \uC2E4\uD589\uD558\uACE0 AI \uD6C4\uBCF4\uB97C \uD655\uC778\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Review => "\uB2E4\uC74C: AI \uD6C4\uBCF4\uB97C \uD655\uC815\uD558\uAC70\uB098 \uC2A4\uD0B5\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Save => "\uB2E4\uC74C: \uB77C\uBCA8\uC744 \uC800\uC7A5\uD558\uACE0 \uB370\uC774\uD130\uC14B \uC810\uAC80\uC744 \uC2E4\uD589\uD569\uB2C8\uB2E4.",
                _ => string.Empty
            };

            ToolDetailText = ResolveReadableToolDetailText(SelectedTool?.Tool);
        }

        private void RefreshYoloDatasetStructureItems()
        {
            YoloDatasetStructureItems.Clear();
            YoloDatasetStructureItems.Add(new WpfYoloDatasetStructureItem(
                "data.yaml",
                T("WpfLearningWorkflow.Structure.DataYaml.Value"),
                T("WpfLearningWorkflow.Structure.DataYaml.Detail"),
                PackIconMaterialKind.FileCodeOutline));
            YoloDatasetStructureItems.Add(new WpfYoloDatasetStructureItem(
                "images",
                T("WpfLearningWorkflow.Structure.Images.Value"),
                T("WpfLearningWorkflow.Structure.Images.Detail"),
                PackIconMaterialKind.FolderImage));
            YoloDatasetStructureItems.Add(new WpfYoloDatasetStructureItem(
                "labels",
                T("WpfLearningWorkflow.Structure.Labels.Value"),
                T("WpfLearningWorkflow.Structure.Labels.Detail"),
                PackIconMaterialKind.FileDocumentOutline));
            YoloDatasetStructureItems.Add(new WpfYoloDatasetStructureItem(
                T("WpfLearningWorkflow.Structure.TxtLine.Title"),
                T("WpfLearningWorkflow.Structure.TxtLine.Value"),
                T("WpfLearningWorkflow.Structure.TxtLine.Detail"),
                PackIconMaterialKind.FormatListNumbered));
        }

        private static string ResolveReadableDatasetPurposeSummaryText(WpfLearningMode mode)
        {
            return mode switch
            {
                WpfLearningMode.ObjectDetection => T("WpfLearningWorkflow.DatasetPurpose.ObjectDetection"),
                WpfLearningMode.Segmentation => T("WpfLearningWorkflow.DatasetPurpose.Segmentation"),
                WpfLearningMode.AnomalyDetection => T("WpfLearningWorkflow.DatasetPurpose.AnomalyDetection"),
                WpfLearningMode.Train => T("WpfLearningWorkflow.DatasetPurpose.Train"),
                WpfLearningMode.Infer => T("WpfLearningWorkflow.DatasetPurpose.Infer"),
                WpfLearningMode.Review => T("WpfLearningWorkflow.DatasetPurpose.Review"),
                _ => T("WpfLearningWorkflow.DatasetPurpose.Default")
            };
        }

        private static string ResolveReadableModeDetailText(WpfLearningMode mode)
        {
            return mode switch
            {
                WpfLearningMode.ObjectDetection => "\uAC1D\uCCB4 \uD0D0\uC9C0: \uC774\uBBF8\uC9C0 \uC548\uC758 \uAC1D\uCCB4 \uC704\uCE58\uB97C \uBC15\uC2A4\uB85C \uCC3E\uACE0, YOLO \uB4F1 \uAC1D\uCCB4 \uD0D0\uC9C0 \uBAA8\uB378 \uD6C4\uBCF4\uB97C \uC815\uB2F5 \uB77C\uBCA8\uB85C \uD655\uC815\uD569\uB2C8\uB2E4.",
                WpfLearningMode.Segmentation => "\uC138\uADF8\uBA58\uD14C\uC774\uC158: \uD53D\uC140 \uB2E8\uC704 \uB9C8\uC2A4\uD06C\uB97C \uB9CC\uB4E4\uACE0, \uBAA8\uB378 \uD559\uC2B5/\uAC80\uC0AC\uB294 \uC5F0\uACB0\uB41C \uC138\uADF8\uBA58\uD14C\uC774\uC158 \uC2E4\uD589\uAE30\uC5D0\uC11C \uC9C4\uD589\uD569\uB2C8\uB2E4.",
                WpfLearningMode.AnomalyDetection => "이상 탐지: 이미지 전체의 정상/이상을 판정하고, 연결된 이미지 분류 실행기에서 학습과 검사를 진행합니다. 결함 위치를 그리는 작업은 객체탐지 또는 세그멘테이션을 사용하세요.",
                WpfLearningMode.Train => "\uD559\uC2B5: \uB77C\uBCA8\uACFC \uD074\uB798\uC2A4\uAC00 \uC900\uBE44\uB41C \uB4A4 \uB370\uC774\uD130\uC14B\uACFC \uD30C\uB77C\uBBF8\uD130\uB97C \uD3C9\uAC00\uD569\uB2C8\uB2E4.",
                WpfLearningMode.Infer => "\uCD94\uB860: \uD604\uC7AC \uC774\uBBF8\uC9C0 \uB610\uB294 \uC120\uD0DD \uC774\uBBF8\uC9C0\uB97C \uBA85\uC2DC\uC801\uC73C\uB85C \uAC80\uC0AC\uD569\uB2C8\uB2E4.",
                WpfLearningMode.Review => "\uAC80\uD1A0: AI \uD6C4\uBCF4\uB97C \uBCF4\uACE0 \uD655\uC815/\uC2A4\uD0B5\uD558\uBA70 \uC815\uB2F5 \uB77C\uBCA8\uB85C \uBC14\uAFC9\uB2C8\uB2E4.",
                _ => "\uB77C\uBCA8\uB9C1 \uD750\uB984\uC740 \uC815\uB2F5 \uC601\uC5ED\uC744 \uB9CC\uB4E4\uACE0 AI\uAC00 \uBC30\uC6B8 \uAE30\uC900\uC744 \uC900\uBE44\uD569\uB2C8\uB2E4."
            };
        }

        private static string ResolveReadableStepDetailText(WpfLearningStep? step, WpfLearningMode purposeMode)
        {
            return step switch
            {
                WpfLearningStep.Sample => "\uC0D8\uD50C \uC774\uBBF8\uC9C0\uB97C \uBD88\uB7EC\uC640 \uAE30\uC900 \uD654\uBA74\uC744 \uB9CC\uB4ED\uB2C8\uB2E4.",
                WpfLearningStep.Label when purposeMode == WpfLearningMode.AnomalyDetection => "이미지 전체를 정상(OK) 또는 이상(NG)으로 판정합니다. 박스나 마스크는 그리지 않습니다.",
                WpfLearningStep.Label => "\uC815\uB2F5 \uB77C\uBCA8\uC744 \uC9C1\uC811 \uB9CC\uB4E4\uACE0 \uD074\uB798\uC2A4\uC640 \uC704\uCE58\uB97C \uD655\uC778\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Infer => "AI \uD6C4\uBCF4\uB97C \uB9CC\uB4E0 \uB4A4 \uB77C\uBCA8\uACFC \uBE44\uAD50\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Review => "\uD6C4\uBCF4\uB97C \uD558\uB098\uC529 \uBCF4\uBA70 \uD655\uC815, \uC804\uCCB4 \uD655\uC815, \uC2A4\uD0B5\uC744 \uC120\uD0DD\uD569\uB2C8\uB2E4.",
                WpfLearningStep.Save when purposeMode == WpfLearningMode.AnomalyDetection => "현재 이미지의 OK/NG 판정을 이상탐지 검토 상태로 저장합니다.",
                WpfLearningStep.Save => "\uD604\uC7AC \uB77C\uBCA8\uC744 \uB370\uC774\uD130\uC14B \uC800\uC7A5 \uD3F4\uB354\uC758 \uD559\uC2B5 \uB77C\uBCA8 \uD30C\uC77C\uB85C \uC800\uC7A5\uD569\uB2C8\uB2E4.",
                _ => string.Empty
            };
        }

        private static string ResolveReadableToolDetailText(WpfAnnotationTool? tool)
        {
            return tool switch
            {
                WpfAnnotationTool.Rectangle => "\uBC15\uC2A4: \uAC1D\uCCB4 \uD0D0\uC9C0 \uD559\uC2B5\uC5D0\uC11C \uAC00\uC7A5 \uAE30\uBCF8\uC774 \uB418\uB294 \uC601\uC5ED\uC785\uB2C8\uB2E4.",
                WpfAnnotationTool.Ellipse => "\uC6D0/\uD0C0\uC6D0: \uC6D0\uD615 \uBD80\uC704\uB098 \uACB0\uD568\uC744 \uBE60\uB974\uAC8C \uC124\uBA85\uD558\uB294 \uBCF4\uC870 \uB3C4\uAD6C\uC785\uB2C8\uB2E4.",
                WpfAnnotationTool.Polygon => "\uD3F4\uB9AC\uACE4: \uC138\uADF8\uBA58\uD14C\uC774\uC158 \uACBD\uACC4\uB97C \uAF2D\uC9D3\uC810\uC73C\uB85C \uB9CC\uB4ED\uB2C8\uB2E4.",
                WpfAnnotationTool.Brush => "\uBE0C\uB7EC\uC2DC: \uB9C8\uC2A4\uD06C\uB97C \uCE60\uD574 \uD53D\uC140 \uB2E8\uC704 \uC815\uB2F5\uC744 \uB9CC\uB4ED\uB2C8\uB2E4.",
                WpfAnnotationTool.Eraser => "\uC9C0\uC6B0\uAC1C: \uB9C8\uC2A4\uD06C\uB098 \uC601\uC5ED \uC77C\uBD80\uB97C \uC81C\uAC70\uD569\uB2C8\uB2E4.",
                WpfAnnotationTool.PanZoom => "\uC774\uB3D9: \uB77C\uBCA8\uC744 \uB9CC\uB4E4\uAE30 \uC804\uC5D0 \uD654\uBA74 \uC704\uCE58\uB97C \uBE60\uB974\uAC8C \uC870\uC815\uD569\uB2C8\uB2E4.",
                WpfAnnotationTool.Delete => "\uC0AD\uC81C: \uC120\uD0DD\uD55C \uB77C\uBCA8\uC744 \uC81C\uAC70\uD569\uB2C8\uB2E4.",
                WpfAnnotationTool.Undo => "\uB418\uB3CC\uB9AC\uAE30: \uC9C1\uC804 \uD3B8\uC9D1\uC744 \uB418\uB3CC\uB9AC\uB294 \uBC84\uD2BC\uC785\uB2C8\uB2E4.",
                WpfAnnotationTool.Redo => "\uB2E4\uC2DC \uC801\uC6A9: \uB418\uB3CC\uB9B0 \uD3B8\uC9D1\uC744 \uB2E4\uC2DC \uC801\uC6A9\uD558\uB294 \uBC84\uD2BC\uC785\uB2C8\uB2E4.",
                _ => "\uC120\uD0DD: \uB9CC\uB4E0 \uB77C\uBCA8\uC744 \uACE0\uB974\uACE0 \uAC80\uC0AC\uD569\uB2C8\uB2E4."
            };
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            OpenVisionLanguageService.LanguageChanged -= OpenVisionLanguageService_LanguageChanged;
        }

        private void OpenVisionLanguageService_LanguageChanged(object sender, EventArgs e)
        {
            bool isDefaultDatasetSetupStatus = string.Equals(
                datasetSetupStatusText,
                "\uB370\uC774\uD130\uC14B \uC2DC\uC791 \uC804",
                StringComparison.Ordinal)
                || string.Equals(
                    datasetSetupStatusText,
                    "Before dataset start",
                    StringComparison.Ordinal);

            RefreshLessonText();
            RefreshYoloDatasetStructureItems();
            if (isDefaultDatasetSetupStatus)
            {
                DatasetSetupStatusText = T("WpfLearningWorkflow.DatasetSetupStatus.Before");
            }

            if (trainingChecklistLocalizationSnapshot != null)
            {
                SetTrainingChecklistLocalization(trainingChecklistLocalizationSnapshot);
            }

            if (trainingChecklistActionLocalizationSnapshot != null)
            {
                SetTrainingChecklistActionLocalization(trainingChecklistActionLocalizationSnapshot);
            }

            if (modelReplacementLocalizationSnapshot != null)
            {
                SetModelReplacementLocalization(modelReplacementLocalizationSnapshot);
            }

            if (trainingModelLifecycleLocalizationSnapshot != null)
            {
                SetTrainingModelLifecycleLocalization(trainingModelLifecycleLocalizationSnapshot);
            }

            if (trainingComparisonLocalizationSnapshot != null)
            {
                SetTrainingComparisonLocalization(trainingComparisonLocalizationSnapshot);
            }

            if (datasetDashboardLocalizationSnapshot != null)
            {
                DatasetDashboardStatusText = datasetDashboardLocalizationSnapshot.StatusText;
                DatasetDashboardSummaryText = datasetDashboardLocalizationSnapshot.SummaryText;
                string localizedActionText = datasetDashboardLocalizationSnapshot.ActionText;
                if (!string.IsNullOrWhiteSpace(localizedActionText))
                {
                    DatasetDashboardActionText = localizedActionText;
                }
                DatasetDashboardMetrics.Clear();
                foreach (WpfDatasetDashboardMetricItem metric in datasetDashboardLocalizationSnapshot.MetricItems)
                {
                    DatasetDashboardMetrics.Add(metric);
                }
                DatasetDashboardIssueItems.Clear();
                foreach (string issue in datasetDashboardLocalizationSnapshot.IssueItems)
                {
                    if (!string.IsNullOrWhiteSpace(issue))
                    {
                        DatasetDashboardIssueItems.Add(issue);
                    }
                }
            }

            ObjectDetectionMvpNextActionText = BuildObjectDetectionMvpNextActionText(DatasetDashboardActionText);

            // The remaining panel captions are expression-backed so one owner-level
            // notification refreshes them without a visual-tree string rewrite.
            OnPropertyChanged(string.Empty);
        }
    }
}
