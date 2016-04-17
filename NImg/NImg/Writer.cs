using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static void WriteImageMetadata(this BinaryWriter writer, int imageWidth, int imageHeight, int colorIndexBytes)
        {
            writer.Write(imageWidth);
            writer.Write(imageHeight);
            writer.Write(Convert.ToByte(colorIndexBytes));
        }

        public static void WriteAIByte(this BinaryWriter writer, int inARow)
        {
            //NOTE: 11110000 -> inARow = 1, not 0, increase by 1
            // "Use AI" bytes are in the form 1111 + (number of pixels)
            writer.Write(Convert.ToByte("1111" + Convert.ToString(inARow - 1, 2).PadLeft(4, '0'), 2));
        }

        public static void WriteColorIndex(this BinaryWriter writer, int colorIndexBytes, int colorIndex)
        {
            switch (colorIndexBytes)
            {
                case 1:
                    writer.Write(Convert.ToByte(colorIndex));
                    break;
                case 2:
                    var byteStr = Convert.ToString(colorIndex, 2).PadLeft(16, '0');
                    writer.Write(Convert.ToByte(byteStr.Substring(0, 8), 2));
                    writer.Write(Convert.ToByte(byteStr.Substring(8), 2));
                    break;
            }
        }

        public static void WriteColor(this BinaryWriter writer, int colorIndexBytes, List<int[]> colorList, int[] colorToWrite, BinaryWriter colorsWriter, int colorTolerance)
        {
            if (colorIndexBytes > 0)
            {
                var existing = colorList.FirstOrDefault(color =>
                    Math.Abs(colorToWrite[0] - color[0]) < colorTolerance &&
                    Math.Abs(colorToWrite[1] - color[1]) < colorTolerance &&
                    Math.Abs(colorToWrite[2] - color[2]) < colorTolerance);

                var maxColorIndex = (colorIndexBytes == 1 ? 239 : 61440);

                if (existing != null)
                {
                    var index = colorList.IndexOf(existing);

                    writer.WriteColorIndex(colorIndexBytes, index);
                }
                else if (colorList.Count >= maxColorIndex)
                {
                    //Over 239/61440 the byte starts with "1111", which is interpreted as a "Use AI" byte.
                    //At this point, just assume that the AI will give a better approximation of the accurate color
                    Console.WriteLine("Hit max colors, defaulting to AI");
                    writer.WriteAIByte(1);
                }
                else
                {
                    //Add a new color to the dictionary
                    colorList.Add(colorToWrite);
                    colorsWriter.Write(Convert.ToByte(colorToWrite[0]));
                    colorsWriter.Write(Convert.ToByte(colorToWrite[1]));
                    colorsWriter.Write(Convert.ToByte(colorToWrite[2]));

                    writer.WriteColorIndex(colorIndexBytes, colorList.IndexOf(colorToWrite));
                }
            }
            else
            {
                //Write the color to the file
                for (var i = 0; i < colorToWrite.Length; i++)
                {
                    var binaryStr = Convert.ToString(colorToWrite[0], 2);

                    //Any color component starting with 1111 will be interpreted as a "Use AI" bit, this is a hacky way to get around that
                    //This will drop any component above 239 by 16 units
                    binaryStr = Regex.Replace(binaryStr, "^1111", "1110");

                    writer.Write(Convert.ToByte(binaryStr, 2));
                }
            }
        }
    }
}
