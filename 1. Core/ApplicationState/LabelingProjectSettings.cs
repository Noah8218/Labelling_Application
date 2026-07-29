using System;

namespace MvcVisionSystem
{
    public enum LabelingDatasetPurpose
    {
        ObjectDetection,
        Segmentation,
        AnomalyDetection
    }

    public enum LabelingBoxDrawingMethod
    {
        TwoPointDrag,
        FourPointExtreme
    }

    public class LabelingProjectSettings
    {
        public LabelingDatasetPurpose DatasetPurpose { get; set; } = LabelingDatasetPurpose.ObjectDetection;

        public bool SmartMaskAutoContourEnabled { get; set; }

        public LabelingBoxDrawingMethod BoxDrawingMethod { get; set; } = LabelingBoxDrawingMethod.TwoPointDrag;

        public YoloDatasetSettings YoloDataset { get; set; } = new YoloDatasetSettings();

        // Native YOLO data.yaml inputs stay separate from the recipe-owned export root.
        // Selecting one never rewrites the current labeling dataset paths or annotations.
        public ExternalYoloDatasetSettings ExternalYoloDataset { get; set; } = new ExternalYoloDatasetSettings();

        public TrainingSettings Training { get; set; } = new TrainingSettings();

        public PythonModelSettings PythonModel { get; set; } = new PythonModelSettings();

        public YoloTrainingGuideHistory TrainingGuide { get; set; } = new YoloTrainingGuideHistory();

        public ModelRegistrySettings ModelRegistry { get; set; } = new ModelRegistrySettings();

        public AnomalyClassificationSettings AnomalyClassification { get; set; } = new AnomalyClassificationSettings();

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(LabelingDatasetPurpose), DatasetPurpose))
            {
                DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            }

            if (!Enum.IsDefined(typeof(LabelingBoxDrawingMethod), BoxDrawingMethod))
            {
                BoxDrawingMethod = LabelingBoxDrawingMethod.TwoPointDrag;
            }

            YoloDataset ??= new YoloDatasetSettings();
            ExternalYoloDataset ??= new ExternalYoloDatasetSettings();
            Training ??= new TrainingSettings();
            PythonModel ??= new PythonModelSettings();
            TrainingGuide ??= new YoloTrainingGuideHistory();
            ModelRegistry ??= new ModelRegistrySettings();
            AnomalyClassification ??= new AnomalyClassificationSettings();
            TrainingGuide.EnsureDefaults();
            ModelRegistry.EnsureDefaults();
            PythonModel.EnsureDefaults();
            AnomalyClassification.EnsureDefaults();
            ExternalYoloDataset.EnsureDefaults();
        }
    }
}
