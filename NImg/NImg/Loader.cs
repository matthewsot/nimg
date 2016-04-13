using System;
using System.Collections.Generic;
using System.Drawing;
using Zoltar;

namespace NImg
{
    static class Loader
    {
        public static TrainingSet[] LoadTrainingSets(string[] paths, int inputPixels = 3, int maxTrainingSets = -1, int trainingSetRandomization = 1)
        {
            var trainingSets = new List<TrainingSet>();
            foreach (var path in paths)
            {
                // Load the unnormalized data in
                using (var image = new Bitmap(path))
                {
                    var sets = 0;
                    if (maxTrainingSets != -1 && trainingSetRandomization == 1)
                    {
                        for (var i = 0; i < maxTrainingSets; i++)
                        {
                            var xPixel = BruteOptimizer.Random.Next(0, image.Width);
                            var yPixel = BruteOptimizer.Random.Next(0, image.Height);
                            var pixel = image.GetPixel(xPixel, yPixel);

                            var output = new double[] { (double)pixel.R / 255, (double)pixel.G / 255, (double)pixel.B / 255 };

                            var input = new double[inputPixels * 3];

                            for (var x = (xPixel - 1); x >= Math.Max(0, xPixel - inputPixels); x--)
                            {
                                var thisPixel = image.GetPixel(x, yPixel);
                                var inputOffsetPixels = (xPixel - 1) - x;
                                input[input.Length - (inputOffsetPixels * 3) - 3] = (double)thisPixel.R / 255;
                                input[input.Length - (inputOffsetPixels * 3) - 2] = (double)thisPixel.G / 255;
                                input[input.Length - (inputOffsetPixels * 3) - 1] = (double)thisPixel.B / 255;
                            }

                            trainingSets.Add(new TrainingSet(input, output));
                        }
                        continue;
                    }

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
