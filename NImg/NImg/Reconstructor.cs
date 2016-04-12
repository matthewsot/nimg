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
        public static void Reconstruct(string path)
        {
            var folderPath = path.Replace(".nimg", ".nimg-temp");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
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
            using (var reader = new BinaryReader(new FileStream(folderPath + "colors.list", FileMode.Open)))
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


                for (var yPixel = 0; yPixel < height; yPixel++)
                {
                    var input = new double[inputPixels * 3];
                    var inARow = 0;
                    for (var xPixel = 0; xPixel < width; xPixel++)
                    {

                    }
                }
            }
        }
    }
}