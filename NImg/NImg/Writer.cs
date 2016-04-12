using System.IO;
using Zoltar;

namespace NImg
{
    static class Writer
    {
        public static void WriteWeights(Network network, string path = @"Output\weights.nn")
        {
            using (var writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
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
        }
    }
}
