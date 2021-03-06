﻿using System;
using System.IO;
using Newtonsoft.Json;
using Zoltar;
using Zoltar.Optimizers;

namespace NImg
{
    static class Optimizer
    {
        public static void Optimize(Network network, int inputPixels, TrainingSet[] trainingSets, int trainingRounds)
        {
            var weightsPath = "weights." + inputPixels + ".json"; // Disabled for now
            if (false && File.Exists(weightsPath))
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
                Console.WriteLine("Beginning Optimization");
                do
                {
                    network.Weights = BackPropOptimizer.Optimize(network, trainingSets, 0.5, 1);
                    var newError = BackPropOptimizer.Error(network, trainingSets, network.Weights);
                    deltaDrop = error - newError;
                    error = newError;
                    if (tries % 10 == 0)
                    {
                        Console.WriteLine("Error from last optimization attempt: " + error);
                    }
                    tries++;
                } while (tries < trainingRounds /*false || error > 10 || deltaDrop < 1*/);

                Console.WriteLine("Optimization complete!");
                using (var writer = new StreamWriter(weightsPath))
                {
                    writer.Write(JsonConvert.SerializeObject(network.Weights));
                    writer.Flush();
                }
            }
        }
    }
}
