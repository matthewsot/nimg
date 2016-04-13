using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using Zoltar;

namespace NImg
{
    static class Reconstructor
    {
        public static void Reconstruct(string path, bool demoMode)
        {
            var folderPath = path.Replace(".nimg", ".nimg-temp");
            if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
            Directory.CreateDirectory(folderPath);
            ZipFile.ExtractToDirectory(path, folderPath);
            folderPath += "\\";

            //Read the colors
            var colors = new List<Color>();
            using (var reader = new BinaryReader(new FileStream(folderPath + "colors.list", FileMode.Open)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    var R = Convert.ToInt32(reader.ReadByte());
                    var G = Convert.ToInt32(reader.ReadByte());
                    var B = Convert.ToInt32(reader.ReadByte());
                    colors.Add(Color.FromArgb(R, G, B));
                }
            }

            //Load the network
            Network network;
            var inputPixels = 0;
            using (var reader = new BinaryReader(new FileStream(folderPath + "weights.nn", FileMode.Open)))
            {
                inputPixels = Convert.ToInt32(reader.ReadByte());
                var innerLayers = Convert.ToInt32(reader.ReadByte());
                var neuronsPerLayer = Convert.ToInt32(reader.ReadByte());
                var biasNeurons = Convert.ToInt32(reader.ReadByte());
                network = new Network(inputPixels * 3, innerLayers, neuronsPerLayer, 3);
                network.RandomizeWeights();
                for (var i = 0; i < biasNeurons; i++)
                {
                    network.AddBiasNeuron(i);
                }

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    for (var i = 0; i < network.Weights.Length; i++)
                    {
                        for (var j = 0; j < network.Weights[i].Length; j++)
                        {
                            for (var k = 0; k < network.Weights[i][j].Length; k++)
                            {
                                network.Weights[i][j][k] = reader.ReadDouble();
                            }
                        }
                    }
                }
            }

            //Construct the image
            using (var reader = new BinaryReader(new FileStream(folderPath + "image.data", FileMode.Open)))
            {
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                var colorIndexBytes = Convert.ToInt32(reader.ReadByte());

                using (var reconstructedImage = new Bitmap(width, height))
                {
                    for (var yPixel = 0; yPixel < height; yPixel++)
                    {
                        var input = new double[inputPixels * 3];
                        for (var xPixel = 0; xPixel < width; xPixel++)
                        {
                            var byteStr = Convert.ToString(reader.ReadByte(), 2).PadLeft(8, '0');
                            if (byteStr.StartsWith("1111"))
                            {
                                var inARow = Convert.ToInt32(byteStr.Substring(4).PadLeft(8, '0'), 2) + 1; // 11110000 -> 1, not 0
                                for (var i = 0; i < inARow; i++)
                                {
                                    if (demoMode)
                                    {
                                        reconstructedImage.SetPixel(xPixel, yPixel,
                                            Color.FromArgb(255, 0, 0));
                                        xPixel++;
                                        continue;
                                    }

                                    var output = network.Pulse(input);
                                    var roundedOutput = new int[] {
                                            (int)Math.Round(output[0] * 255),
                                            (int)Math.Round(output[1] * 255),
                                            (int)Math.Round(output[2] * 255) };

                                    reconstructedImage.SetPixel(xPixel, yPixel,
                                        Color.FromArgb(roundedOutput[0], roundedOutput[1], roundedOutput[2]));
                                
                                    for (var j = 0; j < input.Length - 3; j++)
                                    {
                                        input[j] = input[j + 3];
                                    }
                                    input[input.Length - 3] = output[0];
                                    input[input.Length - 2] = output[1];
                                    input[input.Length - 1] = output[2];

                                    xPixel++;
                                }
                                xPixel--;
                            }
                            else
                            {
                                Color colorToUse;
                                if (colorIndexBytes == 0)
                                {
                                    colorToUse = Color.FromArgb(
                                        Convert.ToInt32(byteStr, 2),
                                        Convert.ToInt32(reader.ReadByte()),
                                        Convert.ToInt32(reader.ReadByte())
                                    );
                                }
                                else
                                {
                                    var colorIndex = Convert.ToInt32(byteStr, 2);
                                    if (colorIndexBytes == 2)
                                    {
                                        var secondByteStr = Convert.ToString(reader.ReadByte(), 2).PadLeft(8, '0');
                                        colorIndex = Convert.ToInt32(byteStr + secondByteStr, 2);
                                    }
                                    colorToUse = colors[colorIndex];
                                }
                                
                                reconstructedImage.SetPixel(xPixel, yPixel, colorToUse);
                                for (var j = 0; j < input.Length - 3; j++)
                                {
                                    input[j] = input[j + 3];
                                }
                                input[input.Length - 3] = (double)colorToUse.R / 255;
                                input[input.Length - 2] = (double)colorToUse.G / 255;
                                input[input.Length - 1] = (double)colorToUse.B / 255;
                            }
                        }
                    }
                    reconstructedImage.Save(path.Replace(".nimg", ".reconstructed.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            Directory.Delete(folderPath, true);
        }
    }
}