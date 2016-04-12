using System;
using System.IO;
using Zoltar;

namespace NImg
{
    static class Writer
    {
        public static void WriteWeights(Network network, int inputPixels, int innerLayers, int neuronsPerLayer, int biasNeurons, string path = @"Output\weights.nn")
        {
            using (var writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                writer.Write(Convert.ToByte(inputPixels)); //Assuming inputPixels < 256
                writer.Write(Convert.ToByte(innerLayers));
                writer.Write(Convert.ToByte(neuronsPerLayer));
                writer.Write(Convert.ToByte(biasNeurons));

                for (var i = 0; i < network.Weights.Length; i++)
                {
                    for (var j = 0; j < network.Weights[i].Length; j++)
                    {
                        for (var k = 0; k < network.Weights[i][j].Length; k++)
                        {
                            writer.Write(network.Weights[i][j][k]);
                        }
                    }
                }
            }
        }
    }
}
