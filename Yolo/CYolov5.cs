using RJCodeUI_M1.RJControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Serialization;

namespace MvcVisionSystem.Yolo
{
    public class YamlData
    {
        public string train { get; set; }
        public string val { get; set; }
        public int nc { get; set; }
        public string names { get; set; }
    }

    public static class CYolov5
    {
        public static void CreateYaml(string trainImagesPath, string valImagesPath, List<string> classNames, string outputYamlPath)
        {
            // Convert the names list to a string in the desired format
            string names = "[" + string.Join(", ", classNames.Select(name => $"'{name}'")) + "]";

            // Create an object with the necessary information for the yaml file
            YamlData data = new YamlData
            {
                train = trainImagesPath,
                val = valImagesPath,
                nc = classNames.Count,
                names = names
            };

            // Create a new SerializerBuilder and serialize the data
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(data);

            // Write the yaml data to a file
            File.WriteAllText(outputYamlPath, yaml);
        }

        public static void CreateImageAndTxtFile(string imageFileName, Bitmap image, float[] coordinates, string basePath)
        {
            CreateImageAndTxtFile("train", imageFileName, image, coordinates, basePath);
            CreateImageAndTxtFile("valid", imageFileName, image, coordinates, basePath);
        }

        private static void CreateImageAndTxtFile(string mode, string imageFileName, Bitmap image, float[] coordinates, string basePath)
        {
            // Create directories if they do not exist
            string imageDir = Path.Combine(basePath, mode, "images");
            string labelDir = Path.Combine(basePath, mode, "labels");
            Directory.CreateDirectory(imageDir);
            Directory.CreateDirectory(labelDir);

            // Save the image file
            string imageFilePath = Path.Combine(imageDir, $"{imageFileName}.jpeg");
            if (!File.Exists(imageFilePath))
            {
                image.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            // Save the txt file
            string txtFilePath = Path.Combine(labelDir, $"{imageFileName}.txt");

            using (StreamWriter sw = File.AppendText(txtFilePath))
            {
                sw.WriteLine($"{string.Join(" ", coordinates)}");
            }
        }

        public static int GetComboBoxIndex(RJComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString() == value)
                {
                    return i;
                }
            }
            return -1; // return -1 if the item was not found
        }
    }
}
