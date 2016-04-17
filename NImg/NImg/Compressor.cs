using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Zoltar;

namespace NImg
{
    static class Compressor
    {
        public static void Compress(string file = @"Data\TrainingImages\train.png")
        {
            Directory.CreateDirectory("Output");
            var files = new string[] { file };

            var options = Loader.LoadOptions();

            var trainingSets = Loader.LoadTrainingSets(files, options);
            
            Network network = new Network(options.InputPixels * 3, options.InnerLayers, options.NeuronsPerLayer, 3);
            var biasNeurons = 1;
            network.AddBiasNeuron(0);

            Optimizer.Optimize(network, options.InputPixels, trainingSets, options.TrainingRounds);

            Writer.WriteWeights(network, options.InputPixels, options.InnerLayers, options.NeuronsPerLayer, biasNeurons);

            using (var originalImage = new Bitmap(file))
            {
                using (var imageWriter = new BinaryWriter(new FileStream(@"Output\image.data", FileMode.Create)))
                {
                    imageWriter.WriteImageMetadata(originalImage.Width, originalImage.Height, options.ColorIndexBytes);

                    using (var colorsWriter = new BinaryWriter(new FileStream(@"Output\colors.list", FileMode.Create)))
                    {
                        var colors = new List<int[]>();
                        
                        var hits = 0;
                        var misses = 0;

                        for (var yPixel = 0; yPixel < originalImage.Height; yPixel++)
                        {
                            var input = new double[options.InputPixels * 3];
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

                                var good = true;
                                for (var i = 0; i < 3; i++)
                                {
                                    if (Math.Abs(actualColors[i] - output[i]) < options.WriteTolerance)
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
                                var roundedToUse = new int[] { (int)Math.Round(toUse[0]), (int)Math.Round(toUse[1]), (int)Math.Round(toUse[2]) };

                                if (good && inARow < 15 && xPixel < originalImage.Width - 1)
                                {
                                    inARow++;
                                }
                                else if (good && (inARow == 15 || xPixel == originalImage.Width - 1))
                                {
                                    inARow++;
                                    imageWriter.WriteAIByte(inARow);
                                    inARow = 0;
                                }
                                else
                                {
                                    if (inARow > 0)
                                    {
                                        imageWriter.WriteAIByte(inARow);
                                        inARow = 0;
                                    }

                                    imageWriter.WriteColor(options.ColorIndexBytes, colors, roundedToUse, colorsWriter, options.ColorIndexTolerance);
                                }

                                for (var i = 0; i < input.Length - 3; i++)
                                {
                                    input[i] = input[i + 3];
                                }
                                input[input.Length - 3] = toUse[0] / 255;
                                input[input.Length - 2] = toUse[1] / 255;
                                input[input.Length - 1] = toUse[2] / 255;
                            }
                        }
                        Console.WriteLine("Hits: " + hits + " Misses: " + misses);
                        Console.WriteLine((double)(((double)hits / (double)(hits + misses)) * 100) + "%");
                    }
                }
            }

            var ext = file.Split('.').Last();
            var saveAs = file.Replace("." + ext, ".nimg");
            if (File.Exists(saveAs)) File.Delete(saveAs);

            ZipFile.CreateFromDirectory("Output", saveAs, CompressionLevel.Optimal, false);
            Directory.Delete("Output", true);

            var originalSize = new FileInfo(file).Length;
            var newSize = new FileInfo(saveAs).Length;

            Console.WriteLine("Original Size: " + originalSize + " New Size: " + newSize);
            Console.WriteLine("Reduction: " + ((double)100 - ((newSize * (double)100) / originalSize)) + "%");
        }
    }
}
