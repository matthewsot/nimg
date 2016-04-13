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

            var inputPixels = 3;
            var innerLayers = 2;
            var neuronsPerLayer = 5;
            var colorIndexBytes = 2; // Currently only supporting 0-2 here
            var writeTolerance = 10;
            var colorIndexTolerance = 5;
            var trainingRounds = 5;
            var maxTrainingSets = -1;

            if (File.Exists("nimg.config"))
            {
                using (var reader = new StreamReader("nimg.config"))
                {
                    while (reader.Peek() > -1)
                    {
                        var line = reader.ReadLine();
                        var parts = line.Split(' ');
                        switch (parts[0])
                        {
                            case "inputPixels":
                                inputPixels = int.Parse(parts[1]);
                                break;
                            case "innerLayers":
                                innerLayers = int.Parse(parts[1]);
                                break;
                            case "neuronsPerLayer":
                                neuronsPerLayer = int.Parse(parts[1]);
                                break;
                            case "colorIndexBytes":
                                colorIndexBytes = int.Parse(parts[1]);
                                break;
                            case "writeTolerance":
                                writeTolerance = int.Parse(parts[1]);
                                break;
                            case "colorIndexTolerance":
                                colorIndexTolerance = int.Parse(parts[1]);
                                break;
                            case "trainingRounds":
                                trainingRounds = int.Parse(parts[1]);
                                break;
                            case "maxTrainingSets":
                                maxTrainingSets = int.Parse(parts[1]);
                                break;
                        }
                    }
                }
            }

            var trainingSets = Loader.LoadTrainingSets(files, inputPixels, maxTrainingSets);
            
            Network network = new Network(inputPixels * 3, innerLayers, neuronsPerLayer, 3);
            var biasNeurons = 1;
            network.AddBiasNeuron(0);

            Optimizer.Optimize(network, inputPixels, trainingSets, trainingRounds);

            Writer.WriteWeights(network, inputPixels, innerLayers, neuronsPerLayer, biasNeurons);

            using (var originalImage = new Bitmap(file))
            { 
                using (var writer = new BinaryWriter(new FileStream(@"Output\image.data", FileMode.Create)))
                {
                    using (var colorsWriter = new BinaryWriter(new FileStream(@"Output\colors.list", FileMode.Create)))
                    {
                        var colors = new List<int[]>();
                        writer.Write(originalImage.Width);
                        writer.Write(originalImage.Height);
                        writer.Write(Convert.ToByte(colorIndexBytes));
                        
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

                                var good = true;
                                for (var i = 0; i < 3; i++)
                                {
                                    if (Math.Abs(actualColors[i] - output[i]) < writeTolerance)
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
                                    //NOTE: 11110000 -> inARow = 1, not 0, increase by 1
                                    inARow++;
                                    Console.WriteLine(inARow);
                                    // "Use AI" bytes start are in the form 1111 + (number of pixels)
                                    writer.Write(Convert.ToByte("1111" + Convert.ToString(inARow - 1, 2).PadLeft(4, '0'), 2));
                                    inARow = 0;
                                }
                                else
                                {
                                    if (inARow > 0)
                                    {
                                        //NOTE: 11110000 -> inARow = 1, not 0, increase by 1
                                        Console.WriteLine(inARow);
                                        // "Use AI" bytes start are in the form 1111 + (number of pixels)
                                        writer.Write(Convert.ToByte("1111" + Convert.ToString(inARow - 1, 2).PadLeft(8, '0').Substring(4), 2));
                                        inARow = 0;
                                    }

                                    if (colorIndexBytes > 0)
                                    {
                                        // find the color
                                        var existing = colors.FirstOrDefault(color =>
                                            Math.Abs(roundedToUse[0] - color[0]) < colorIndexTolerance &&
                                            Math.Abs(roundedToUse[1] - color[1]) < colorIndexTolerance &&
                                            Math.Abs(roundedToUse[2] - color[2]) < colorIndexTolerance);

                                        var maxColorIndex = (colorIndexBytes == 1 ? 239 : 61440);

                                        if (existing != null)
                                        {
                                            var index = colors.IndexOf(existing);
                                            switch (colorIndexBytes)
                                            {
                                                case 1:
                                                    writer.Write(Convert.ToByte(index));
                                                    break;
                                                case 2:
                                                    var bytesStr = Convert.ToString(index, 2).PadLeft(16, '0');
                                                    writer.Write(Convert.ToByte(bytesStr.Substring(0, 8), 2));
                                                    writer.Write(Convert.ToByte(bytesStr.Substring(8), 2));
                                                    break;
                                            }
                                        }
                                        else if (colors.Count >= maxColorIndex) //Over 239/61440 the byte starts with "1111", which is interpreted as a "Use AI" byte
                                        {
                                            Console.WriteLine("Hit max colors");
                                            switch (colorIndexBytes)
                                            {
                                                case 1:
                                                    writer.Write(Convert.ToByte(maxColorIndex - 1));
                                                    break;
                                                case 2:
                                                    var bytesStr = Convert.ToString(maxColorIndex - 1, 2);
                                                    writer.Write(Convert.ToByte(bytesStr.Substring(0, 8), 2));
                                                    writer.Write(Convert.ToByte(bytesStr.Substring(8), 2));
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            colors.Add(roundedToUse);
                                            colorsWriter.Write(Convert.ToByte(roundedToUse[0]));
                                            colorsWriter.Write(Convert.ToByte(roundedToUse[1]));
                                            colorsWriter.Write(Convert.ToByte(roundedToUse[2]));

                                            switch (colorIndexBytes)
                                            {
                                                case 1:
                                                    writer.Write(Convert.ToByte(colors.Count() - 1));
                                                    break;
                                                case 2:
                                                    var byteStr = Convert.ToString(colors.Count() - 1, 2).PadLeft(16, '0');
                                                    writer.Write(Convert.ToByte(byteStr.Substring(0, 8), 2));
                                                    writer.Write(Convert.ToByte(byteStr.Substring(8), 2));
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var RStr = Convert.ToString(roundedToUse[0], 2);
                                        var GStr = Convert.ToString(roundedToUse[1], 2);
                                        var BStr = Convert.ToString(roundedToUse[2], 2);

                                        //TODO: handle 11111111
                                        if (RStr.StartsWith("1111")) RStr = RStr.Replace("1111", "1110");
                                        if (GStr.StartsWith("1111")) GStr = GStr.Replace("1111", "1110");
                                        if (BStr.StartsWith("1111")) BStr = BStr.Replace("1111", "1110");

                                        writer.Write(Convert.ToByte(RStr, 2));
                                        writer.Write(Convert.ToByte(GStr, 2));
                                        writer.Write(Convert.ToByte(BStr, 2));
                                    }
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
