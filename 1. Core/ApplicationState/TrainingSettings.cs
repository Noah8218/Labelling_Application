using MvcVisionSystem.Yolo;
using System;

namespace MvcVisionSystem
{
    public class TrainingSettings
    {
        public int ImageSize { get; set; } = 320;

        public int Batch { get; set; } = 16;

        public int Epoch { get; set; } = 50;

        public string Cfg { get; set; } = CYolov5TrainingParam.Cfg.yolov5x.ToString();

        public string Weight { get; set; } = CYolov5TrainingParam.Weight.yolov5x.ToString();

        public void CopyFrom(CYolov5TrainingParam trainingParam)
        {
            if (trainingParam == null)
            {
                return;
            }

            ImageSize = trainingParam.imageSize;
            Batch = trainingParam.batch;
            Epoch = trainingParam.epoch;
            Cfg = trainingParam.cfg.ToString();
            Weight = trainingParam.weight.ToString();
        }

        public void ApplyTo(CYolov5TrainingParam trainingParam)
        {
            if (trainingParam == null)
            {
                return;
            }

            trainingParam.imageSize = ImageSize;
            trainingParam.batch = Batch;
            trainingParam.epoch = Epoch;

            if (Enum.TryParse(Cfg, out CYolov5TrainingParam.Cfg cfg))
            {
                trainingParam.cfg = cfg;
            }

            if (Enum.TryParse(Weight, out CYolov5TrainingParam.Weight weight))
            {
                trainingParam.weight = weight;
            }
        }
    }
}
