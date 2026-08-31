using System;
using System.Collections.Generic;
using System.Linq;

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

        public List<string> ObjectReviewTags { get; set; } = new List<string>();

        public YoloDatasetSettings YoloDataset { get; set; } = new YoloDatasetSettings();

        // Model-oriented settings remain reachable through the old public
        // property names for source and VISION.xml compatibility, but their
        // state is owned by one explicit compatibility aggregate.
        [System.Xml.Serialization.XmlIgnore]
        public LegacyModelProjectSettings LegacyModel { get; set; } = new LegacyModelProjectSettings();

        // Native YOLO data.yaml inputs stay separate from the recipe-owned export root.
        // Selecting one never rewrites the current labeling dataset paths or annotations.
        [System.Xml.Serialization.XmlElement("ExternalYoloDataset")]
        public ExternalYoloDatasetSettings ExternalYoloDataset
        {
            get => EnsureLegacyModel().ExternalYoloDataset;
            set => EnsureLegacyModel().ExternalYoloDataset = value ?? new ExternalYoloDatasetSettings();
        }

        [System.Xml.Serialization.XmlElement("Training")]
        public TrainingSettings Training
        {
            get => EnsureLegacyModel().Training;
            set => EnsureLegacyModel().Training = value ?? new TrainingSettings();
        }

        [System.Xml.Serialization.XmlElement("PythonModel")]
        public PythonModelSettings PythonModel
        {
            get => EnsureLegacyModel().PythonModel;
            set => EnsureLegacyModel().PythonModel = value ?? new PythonModelSettings();
        }

        // Canonical source-image folder for the labeling workflow. The legacy
        // PythonModel field remains synchronized so older Recipes stay readable.
        private string imageRootPath = string.Empty;

        public string ImageRootPath
        {
            get => imageRootPath;
            set
            {
                imageRootPath = value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(imageRootPath)
                    && PythonModel != null
                    && !string.Equals(PythonModel.ImageRootPath, imageRootPath, StringComparison.Ordinal))
                {
                    PythonModel.ImageRootPath = imageRootPath;
                }
            }
        }

        [System.Xml.Serialization.XmlElement("TrainingGuide")]
        public YoloTrainingGuideHistory TrainingGuide
        {
            get => EnsureLegacyModel().TrainingGuide;
            set => EnsureLegacyModel().TrainingGuide = value ?? new YoloTrainingGuideHistory();
        }

        [System.Xml.Serialization.XmlElement("ModelRegistry")]
        public ModelRegistrySettings ModelRegistry
        {
            get => EnsureLegacyModel().ModelRegistry;
            set => EnsureLegacyModel().ModelRegistry = value ?? new ModelRegistrySettings();
        }

        [System.Xml.Serialization.XmlElement("AnomalyClassification")]
        public AnomalyClassificationSettings AnomalyClassification
        {
            get => EnsureLegacyModel().AnomalyClassification;
            set => EnsureLegacyModel().AnomalyClassification = value ?? new AnomalyClassificationSettings();
        }

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

            ObjectReviewTags = (ObjectReviewTags ?? new List<string>())
                .Select(tag => tag?.Trim() ?? string.Empty)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Length <= 32 ? tag : tag.Substring(0, 32))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(16)
                .ToList();

            YoloDataset ??= new YoloDatasetSettings();
            LegacyModelProjectSettings legacyModel = EnsureLegacyModel();
            legacyModel.EnsureDefaults();
            if (!string.IsNullOrWhiteSpace(ImageRootPath)
                && !string.Equals(PythonModel.ImageRootPath, ImageRootPath, StringComparison.Ordinal))
            {
                PythonModel.ImageRootPath = ImageRootPath;
            }
        }

        public string ResolveImageRootPath()
        {
            return !string.IsNullOrWhiteSpace(ImageRootPath)
                ? ImageRootPath
                : PythonModel?.ImageRootPath ?? string.Empty;
        }

        private LegacyModelProjectSettings EnsureLegacyModel()
        {
            return LegacyModel ??= new LegacyModelProjectSettings();
        }
    }
}
