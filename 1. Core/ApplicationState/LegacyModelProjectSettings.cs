namespace MvcVisionSystem
{
    /// <summary>
    /// Owns model-oriented state that still has to travel with a legacy
    /// labeling Recipe. The aggregate is intentionally not serialized as a
    /// new XML element; <see cref="LabelingProjectSettings"/> exposes the
    /// historical element names through compatibility proxy properties.
    /// </summary>
    public sealed class LegacyModelProjectSettings
    {
        public ExternalYoloDatasetSettings ExternalYoloDataset { get; set; } = new ExternalYoloDatasetSettings();

        public TrainingSettings Training { get; set; } = new TrainingSettings();

        public PythonModelSettings PythonModel { get; set; } = new PythonModelSettings();

        public YoloTrainingGuideHistory TrainingGuide { get; set; } = new YoloTrainingGuideHistory();

        public ModelRegistrySettings ModelRegistry { get; set; } = new ModelRegistrySettings();

        public AnomalyClassificationSettings AnomalyClassification { get; set; } = new AnomalyClassificationSettings();

        public void EnsureDefaults()
        {
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
