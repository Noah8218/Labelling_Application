using MvcVisionSystem.Yolo;
using System;

namespace MvcVisionSystem
{
    public class TrainingSettings
    {
        public int ImageSize { get; set; } = 320;

        public int Batch { get; set; } = 16;

        public int Epoch { get; set; } = 50;

        public string Cfg { get; set; } = YoloV5TrainingParameters.Cfg.yolov5x.ToString();

        public string Weight { get; set; } = YoloV5TrainingParameters.Weight.yolov5x.ToString();

        public void CopyFrom(YoloV5TrainingParameters trainingParam)
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

        public void ApplyTo(YoloV5TrainingParameters trainingParam)
        {
            if (trainingParam == null)
            {
                return;
            }

            trainingParam.imageSize = ImageSize;
            trainingParam.batch = Batch;
            trainingParam.epoch = Epoch;

            if (Enum.TryParse(Cfg, out YoloV5TrainingParameters.Cfg cfg))
            {
                trainingParam.cfg = cfg;
            }

            if (Enum.TryParse(Weight, out YoloV5TrainingParameters.Weight weight))
            {
                trainingParam.weight = weight;
            }
        }
    }
}
