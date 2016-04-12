using System.Collections.Generic;
using System.Drawing;
using Zoltar;

namespace NImg
{
    static class Loader
    {
        public static TrainingSet[] LoadTrainingSets(string[] paths, int inputPixels = 3, int maxTrainingSets = -1)
        {
            var trainingSets = new List<TrainingSet>();
            foreach (var path in paths)
            {
                // Load the unnormalized data in
                using (var image = new Bitmap(path))
                {
                    var sets = 0;
                    for (var yPixel = 0; yPixel < image.Height; yPixel++)
                    {
                        var input = new double[inputPixels * 3];
                        for (var xPixel = 0; xPixel < image.Width; xPixel++)
                        {
                            var pixel = image.GetPixel(xPixel, yPixel);
                            var output = new double[] { (double)pixel.R / 255, (double)pixel.G / 255, (double)pixel.B / 255 };

                            trainingSets.Add(new TrainingSet((double[])input.Clone(), output));

                            for (var i = 0; i < input.Length - 3; i++)
                            {
                                input[i] = input[i + 3];
                            }
                            input[input.Length - 3] = (double)pixel.R / 255;
                            input[input.Length - 2] = (double)pixel.G / 255;
                            input[input.Length - 1] = (double)pixel.B / 255;

                            sets++;
                            if (maxTrainingSets > 0 && sets >= maxTrainingSets)
                            {
                                break;
                            }
                        }
                        if (maxTrainingSets > 0 && sets >= maxTrainingSets)
                        {
                            break;
                        }
                    }
                }
            }
            return trainingSets.ToArray();
        }
    }
}
