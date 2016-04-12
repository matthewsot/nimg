using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Zoltar;

namespace NImg
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"Data\TrainingImages\train.png";
            var files = new string[] { file };

            var inputPixels = 3;

            var trainingSets = Loader.LoadTrainingSets(files, inputPixels);

            Network network = new Network(inputPixels * 3, 1, 9, 3);
            network.AddBiasNeuron(0);

            Optimizer.Optimize(network, inputPixels, trainingSets);

            var tolerance = 10;

            Writer.WriteWeights(network);

            using (var originalImage = new Bitmap(file))
            {
                using (var writer = new BinaryWriter(new FileStream(@"Output\image.data", FileMode.Create)))
                {
                    using (var colorsWriter = new BinaryWriter(new FileStream(@"Output\colors.colors", FileMode.Create)))
                    {
                        var colors = new List<int[]>();
                        writer.Write(originalImage.Width);
                        writer.Write("X");
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

                                var actualColors = new int[] { actualPixel.R, actualPixel.G, actualPixel.B };
                                var roundedOutput = new int[] { (int)Math.Round(toUse[0]), (int)Math.Round(toUse[1]), (int)Math.Round(toUse[2]) };

                                var good = true;
                                for (var i = 0; i < 3; i++)
                                {
                                    if (Math.Abs(actualColors[i] - output[i]) < tolerance)
                                    {
                                        hits++;
                                        toUse[i] = output[i];
                                    }
                                    else
                                    {
                                        good = false;
                                        misses++;
                                        toUse[i] = actualColors[i];
                                    }
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
                                        // "Use AI" bytes start are in the form 1111 + (number of pixels)
                                        writer.Write(Convert.ToByte("1111" + Convert.ToString(inARow, 2).PadLeft(8, '0').Substring(4), 2));
                                        inARow = 0;
                                    }

                                    // find the color
                                    var colorTolerance = 3;
                                    var existing = colors.FirstOrDefault(color =>
                                        Math.Abs(roundedOutput[0] - color[0]) < colorTolerance &&
                                        Math.Abs(roundedOutput[0] - color[0]) < colorTolerance &&
                                        Math.Abs(roundedOutput[0] - color[0]) < colorTolerance);

                                    if (existing != null)
                                    {
                                        writer.Write(Convert.ToByte(colors.IndexOf(existing)));
                                    }
                                    else if (colors.Count >= 256)
                                    {
                                        writer.Write(Convert.ToByte(255));
                                        Console.WriteLine("Hit 256 colors");
                                    }
                                    else
                                    {
                                        colors.Add(roundedOutput);
                                        colorsWriter.Write(Convert.ToByte(roundedOutput[0]));
                                        colorsWriter.Write(Convert.ToByte(roundedOutput[1]));
                                        colorsWriter.Write(Convert.ToByte(roundedOutput[2]));

                                        colorsWriter.Flush();

                                        writer.Write(Convert.ToByte((colors.Count() - 1)));
                                    }
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
                            }
                        }
                        Console.WriteLine("Hits: " + hits + " Misses: " + misses);
                        Console.WriteLine((double)(((double)hits / (double)(hits + misses)) * 100) + "%");

                        reconstructedImage.Save("reconstructed.png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }

            if (File.Exists("output.nimg")) File.Delete("output.nimg");

            ZipFile.CreateFromDirectory("Output", "output.nimg", CompressionLevel.Optimal, false);

            var originalSize = new FileInfo(file).Length;
            var newSize = new FileInfo("output.nimg").Length;

            Console.WriteLine("Original Size: " + originalSize + " New Size: " + newSize);
            Console.WriteLine("Reduction: " + ((double)100 - ((newSize * (double)100) / originalSize)) + "%");

            Console.ReadLine();
        }
    }
}
