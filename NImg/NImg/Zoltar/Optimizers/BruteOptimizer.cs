using System;

namespace Zoltar
{
    static class BruteOptimizer
    {
        public static Random Random = new Random(123);
        public static double Error(this Network network, TrainingSet trainingSet, double[][][] weights)
        {
            var outputs = network.Pulse(trainingSet.Inputs, weights);

            double error = 0;
            for (var i = 0; i < outputs.Length; i++)
            {
                error += Math.Abs(outputs[i] - trainingSet.Outputs[i]);
            }
            return error;
        }

        public static double Error(this Network network, TrainingSet[] trainingSets, double[][][] weights)
        {
            double error = 0;
            for (var i = 0; i < trainingSets.Length; i++)
            {
                error += network.Error(trainingSets[i], weights);
            }
            return error;
        }

        //Thanks! https://stackoverflow.com/questions/1064901/random-number-between-2-double-numbers
        public static double GetRandomNumber(double minimum, double maximum)
        {
            return Random.NextDouble() * (maximum - minimum) + minimum;
        }
        public static double[][][] OptimizeSingle(Network network, TrainingSet[] trainingSets, double[][][] weights, int layerIndex, int neuronIndex, int inputIndex, double delta = 10, int recursions = 10)
        {
            //Console.WriteLine("Optimizing: [" + layerIndex + ", " + neuronIndex + ", " + inputIndex + "]...");

            var bestError = network.Error(trainingSets, weights);

            for (var r = 0; r < recursions; r++)
            {
                //Console.WriteLine("START: " + delta + " " + weights[layerIndex][neuronIndex][inputIndex]);
                if (bestError == 0) break;

                weights[layerIndex][neuronIndex][inputIndex] = weights[layerIndex][neuronIndex][inputIndex] + delta;
                var positiveDeltaError = network.Error(trainingSets, weights);
                weights[layerIndex][neuronIndex][inputIndex] = weights[layerIndex][neuronIndex][inputIndex] - (2.0 * delta);
                var negativeDeltaError = network.Error(trainingSets, weights);

                if (Math.Min(negativeDeltaError, positiveDeltaError) < bestError)
                {
                    bestError = Math.Min(negativeDeltaError, positiveDeltaError);
                    if (negativeDeltaError < positiveDeltaError)
                    {
                    }
                    else
                    {
                        weights[layerIndex][neuronIndex][inputIndex] = weights[layerIndex][neuronIndex][inputIndex] + (2.0 * delta);
                    }
                }
                else
                {
                    weights[layerIndex][neuronIndex][inputIndex] = weights[layerIndex][neuronIndex][inputIndex] + delta;
                    delta = delta * 0.5;
                }
                //Console.WriteLine("END: " + delta + " " + weights[layerIndex][neuronIndex][inputIndex]);
            }
            Console.WriteLine("Current best error: " + bestError);
            return weights;
        }

        public static double[][][] OptimizeMulti(Network network, TrainingSet[] trainingSets, int optimizerDelta = 10, int optimizerRecursions = 100)
        {
            //Console.WriteLine("Starting optimization...");
            var weights = network.Weights;
            for (var layerIndex = 0; layerIndex < network.Layers.Length; layerIndex++)
            {
                var layer = network.Layers[layerIndex];
                for (var neuronIndex = 0; neuronIndex < layer.Neurons; neuronIndex++)
                {
                    for (var inputIndex = 0; inputIndex < layer.Inputs; inputIndex++)
                    {
                        weights = OptimizeSingle(network, trainingSets, weights, layerIndex, neuronIndex, inputIndex, optimizerDelta, optimizerRecursions);
                    }
                }
            }
            return weights;
        }
    }
}
