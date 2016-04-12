using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Zoltar;
using Zoltar.Optimizers;

namespace NImg
{
    class Program
    {
        static TrainingSet[] RawTrainingSets = null;
        static TrainingSet[] LoadTrainingSets(int inputPixels = 3, string path = @"Data\TrainingImages\train.png")
        {
            var trainingSets = new List<TrainingSet>();
            // Load the unnormalized data in
            using (var image = new Bitmap(path))
            {
                for (var yPixel = 0; yPixel < image.Height; yPixel++)
                {
                    var input = new double[inputPixels * 3];
                    for (var xPixel = 0; xPixel < image.Width; xPixel++)
                    {
                        var pixel = image.GetPixel(xPixel, yPixel);
                        var output = new double[] { (pixel.R / 255), (pixel.G / 255), (pixel.B / 255) };

                        trainingSets.Add(new TrainingSet((double[])input.Clone(), output));

                        for (var i = 0; i < input.Length - 3; i++)
                        {
                            input[i] = input[i + 3];
                        }
                        input[input.Length - 3] = pixel.R / 255;
                        input[input.Length - 2] = pixel.G / 255;
                        input[input.Length - 1] = pixel.B / 255;
                    }
                }
            }
            return trainingSets.ToArray();
        }

        static void Main(string[] args)
        {

            var file = @"Data\TrainingImages\train.png";
            var inputPixels = 3;
            var trainingInputPixels = 3;
            var trainingSets = LoadTrainingSets(trainingInputPixels, file);

            Network network = new Network(inputPixels * 3, 1, 9, 3);
            network.AddBiasNeuron(0);
            //network.AddBiasNeuron(1);
            var weightsPath = "weights." + trainingInputPixels + ".json";
            if (File.Exists(weightsPath))
            {
                Console.WriteLine("Found existing weights!");
                using (var reader = new StreamReader(weightsPath))
                {
                    network.Weights = JsonConvert.DeserializeObject<double[][][]>(reader.ReadToEnd());
                }
            }
            else
            {
                double error = 0;
                double deltaDrop = 0;
                int tries = 0;
                do
                {
                    Console.WriteLine("Optimizing...");
                    network.Weights = BackPropOptimizer.Optimize(network, trainingSets, 0.5, 1);
                    var newError = BackPropOptimizer.Error(network, trainingSets, network.Weights);
                    deltaDrop = error - newError;
                    error = newError;
                    Console.WriteLine("Error from last optimization attempt: " + error);
                    tries++;
                } while (error > 5 && tries < 25 /*false || error > 10 || deltaDrop < 1*/);

                Console.WriteLine("Optimization complete!");
                using (var writer = new StreamWriter(weightsPath))
                {
                    writer.Write(JsonConvert.SerializeObject(network.Weights));
                    writer.Flush();
                }
            }

            var tolerance = 25;

            using (var writer = new BinaryWriter(new FileStream(@"Output\weights.nn", FileMode.Create)))
            {
                for (var i = 0; i < network.Weights.Length; i++)
                {
                    if (i != 0)
                    {
                        writer.Write("\n");
                    }
                    for (var j = 0; j < network.Weights[i].Length; j++)
                    {
                        if (j != 0)
                        {
                            writer.Write(">");
                        }
                        for (var k = 0; k < network.Weights[i][j].Length; k++)
                        {
                            if (k != 0)
                            {
                                writer.Write("|");
                            }
                            writer.Write(network.Weights[i][j][k]);
                        }
                    }
                }
            }

            using (var originalImage = new Bitmap(file))
            {
                using (var writer = new BinaryWriter(new FileStream(@"Output\image.aimg", FileMode.Create)))
                {
                    var colorsWriter = new BinaryWriter(new FileStream(@"Output\colors.colors", FileMode.Create));
                    var colors = new List<int[]>();
                    writer.Write(originalImage.Width);
                    //writer.Write("X");
                    writer.Write(originalImage.Height);
                    writer.Write("|");

                    var reconstructedImage = new Bitmap(originalImage.Width, originalImage.Height);
                    var hits = 0;
                    var misses = 0;

                    for (var yPixel = 0; yPixel < originalImage.Height; yPixel++)
                    {
                        var input = new double[inputPixels * 3];
                        var inARow = 0;
                        for (var xPixel = 0; xPixel < originalImage.Width; xPixel++)
                        {
                            var output = network.Pulse(input);
                            output[0] = output[0] * 255;
                            output[1] = output[1] * 255;
                            output[2] = output[2] * 255;
                            var actualPixel = originalImage.GetPixel(xPixel, yPixel);
                            var toUse = new double[3];

                            var good = true;
                            if (Math.Abs(actualPixel.R - output[0]) < tolerance)
                            {
                                hits++;
                                toUse[0] = output[0];
                            }
                            else
                            {
                                good = false;
                                misses++;
                                toUse[0] = actualPixel.R;
                            }

                            if (Math.Abs(actualPixel.G - output[1]) < tolerance)
                            {
                                hits++;
                                toUse[1] = output[1];
                            }
                            else
                            {
                                good = false;
                                misses++;
                                toUse[1] = actualPixel.G;
                            }

                            if (Math.Abs(actualPixel.B - output[2]) < tolerance)
                            {
                                hits++;
                                toUse[2] = output[2];
                            }
                            else
                            {
                                good = false;
                                misses++;
                                toUse[2] = actualPixel.B;
                            }

                            if (good && inARow < 16)
                            {
                                inARow++;
                            }
                            else
                            {
                                if (inARow > 0)
                                {
                                    Console.WriteLine(inARow);
                                    writer.Write(Convert.ToByte("1111" + Convert.ToString(inARow, 2).PadLeft(8, '0').Substring(4), 2));
                                    inARow = 0;
                                }

                                for (var i = 0; i < toUse.Length; i++)
                                {
                                    if (Convert.ToString((int)Math.Round(toUse[i]), 2).PadLeft(8, '0').Substring(4) == "1111")
                                    {
                                        var bits = Convert.ToString((int)Math.Round(toUse[i]), 2).PadLeft(8, '0').ToCharArray();
                                        bits[3] = '0';

                                        toUse[i] = Convert.ToInt32(new string(bits), 2);
                                    }
                                }

                                // find the color
                                var rounded = new int[] { (int)Math.Round(toUse[0]), (int)Math.Round(toUse[1]), (int)Math.Round(toUse[2]) };
                                var colorTolerance = 3;
                                var existing = colors.FirstOrDefault(color =>
                                    Math.Abs(rounded[0] - color[0]) < colorTolerance &&
                                    Math.Abs(rounded[0] - color[0]) < colorTolerance &&
                                    Math.Abs(rounded[0] - color[0]) < colorTolerance);

                                if (existing != null)
                                {
                                    writer.Write(Convert.ToByte(colors.IndexOf(existing)));
                                }
                                else
                                {
                                    colors.Add(rounded);
                                    colorsWriter.Write(Convert.ToByte(rounded[0]));
                                    colorsWriter.Write(Convert.ToByte(rounded[1]));
                                    colorsWriter.Write(Convert.ToByte(rounded[2]));
                                    colorsWriter.Flush();
                                    writer.Write(Convert.ToByte((colors.Count() - 1)));
                                }
                                Console.WriteLine(colors.Count());
                            }

                            for (var i = 0; i < input.Length - 3; i++)
                            {
                                input[i] = input[i + 3];
                            }
                            input[input.Length - 3] = toUse[0] / 255;
                            input[input.Length - 2] = toUse[1] / 255;
                            input[input.Length - 1] = toUse[2] / 255;

                            var colorToUse = Color.FromArgb((int)Math.Round(toUse[0]), (int)Math.Round(toUse[1]), (int)Math.Round(toUse[2]));
                            reconstructedImage.SetPixel(xPixel, yPixel, colorToUse);
                            //reconstructedImage.SetPixel(xPixel, yPixel, originalImage.GetPixel(xPixel, yPixel));
                        }
                    }
                    Console.WriteLine("Hits: " + hits + " Misses: " + misses);
                    Console.WriteLine((double)(((double)hits / (double)(hits + misses)) * 100) + "%");

                    reconstructedImage.Save("reconstructed.png", System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            Console.ReadLine();
        }
    }
}
