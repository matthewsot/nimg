using System;
using System.Linq;

namespace Zoltar.Optimizers
{
    /// <summary>
    /// NOTE: This class ASSUMES the activation function is the SigmoidActivation in Layer.cs.
    /// Do not use this with other activation functions.
    /// </summary>
    // Highly adapted from https://www.youtube.com/watch?v=zpykfC4VnpM
    public static class BackPropOptimizer
    {
        /// <summary>
        /// Calculates the error for a given network, training set, and weights.
        /// Error = (1/2) sum (calculated - actual)^2
        /// </summary>
        /// <param name="network">The network to calculate the error on</param>
        /// <param name="set">The training set to use</param>
        /// <param name="weights">The weights to use</param>
        /// <returns>1/2 of the sum of the squares of the errors for each neuron</returns>
        public static double Error(this Network network, TrainingSet set, double[][][] weights)
        {
            var outputs = network.Pulse(set.Inputs, weights);

            double error = 0;
            for (var i = 0; i < outputs.Length; i++)
            {
                error += Math.Pow(outputs[i] - set.Outputs[i], 2);
            }
            return 0.5 * error;
        }

        /// <summary>
        /// Calculates the error for a given network, training sets, and weights.
        /// </summary>
        /// <param name="network">The network to calculate the error on</param>
        /// <param name="sets">The training set to use</param>
        /// <param name="weights">The weights to use</param>
        /// <returns>1/2 of the sum of the squares of the errors for each training set</returns>
        public static double Error(this Network network, TrainingSet[] sets, double[][][] weights)
        {
            var error = 0.0;
            for (var i = 0; i < sets.Length; i++)
            {
                error += network.Error(sets[i], weights);
            }
            return error;
        }

        /// <summary>
        /// Calculates the "delta value" for a specified neuron.
        /// For output neurons, delta = (calculated - actual)*(calculated - calculated^2)
        /// For hidden neurons in level l, delta = (calculated - calculated^2)* (sum n in neurons in l+1 [ delta((l+1)[n]) * weight(l[n] -> (l+1)[n]) ])
        /// </summary>
        /// <param name="network">The network to calculate the delta on</param>
        /// <param name="set">The training set to calculate the delta on</param>
        /// <param name="innerLayer">The inner layer index to calculate the training set on</param>
        /// <param name="neuron">The neuron index to calculate the training set on</param>
        /// <param name="deltas">The delta values for the L+1 layer</param>
        /// <returns>The delta value for the specified neuron</returns>
        public static double Delta(Network network, TrainingSet set, int innerLayer, int neuron, double[] deltas = null)
        {
            var isOutputLayer = innerLayer == network.Layers.Length - 1;
            if (isOutputLayer)
            {
                var output = network.Pulse(set.Inputs)[neuron];
                return (output - set.Outputs[neuron]) * (output - Math.Pow(output, 2));
            }
            else
            {
                var outputs = network.PulseDetailed(set.Inputs, false);
                var actualOutput = outputs[innerLayer][neuron];
                var summation = 0.0;
                for (var n = 0; n < network.Weights[innerLayer + 1].Length; n++)
                {
                    summation += deltas[n] * network.Weights[innerLayer + 1][n][neuron];
                }
                return (actualOutput - Math.Pow(actualOutput, 2)) * summation;
            }
        }

        /// <summary>
        /// Optimizes weights for a given training set
        /// </summary>
        /// <param name="network">The network to optimize</param>
        /// <param name="set">The set to optimize for</param>
        /// <param name="trainingFactor">The training factor (how large the changes should be)</param>
        /// <returns>The optimized weights</returns>
        public static double[][][] Optimize(Network network, TrainingSet set, double trainingFactor = 0.1)
        {
            var outputs = network.PulseDetailed(set.Inputs, true);
            var deltas = new double[network.Weights.Length][];
            for (var layer = network.Weights.Length - 1; layer >= 0; layer--)
            {
                deltas[layer] = new double[network.Weights[layer].Length];
                for (var neuron = 0; neuron < network.Weights[layer].Length; neuron++)
                {
                    if (layer == network.Weights.Length - 1)
                    {
                        deltas[layer][neuron] = Delta(network, set, layer, neuron);
                    }
                    else
                    {
                        deltas[layer][neuron] = Delta(network, set, layer, neuron, deltas[layer + 1]);
                    }

                    for (var input = 0; input < network.Weights[layer][neuron].Length; input++)
                    {
                        var delta = deltas[layer][neuron];

                        var errorPrime = 0.0;
                        if (input < outputs[layer].Length)
                        {
                            //No need for layer-1 since the addition of the inputs pushes all the layers +1
                            errorPrime = delta * outputs[layer/* - 1*/][input]; //Error prime = (d Error) / (d weight)
                        }
                        else
                        {
                            //Assume it's a bias neuron of value 1
                            errorPrime = delta * 1;
                        }

                        var deltaWeight = (-1.0) * trainingFactor * errorPrime;

                        var preError = network.Error(set, network.Weights);
                        var preErrorWeight = network.Weights[layer][neuron][input];

                        network.Weights[layer][neuron][input] += deltaWeight;

                        var postError = network.Error(set, network.Weights);
                        if (postError > preError)
                        {
                            network.Weights[layer][neuron][input] -= deltaWeight;
                        }
                    }
                }
            }
            return network.Weights;
        }

        /// <summary>
        /// Optimizes the weights for a network
        /// </summary>
        /// <param name="network">The network to optimize</param>
        /// <param name="sets">The training sets to use</param>
        /// <param name="trainingFactor">The training factor to use (directly related to the size of the weight changes)</param>
        /// <param name="rounds">The number of rounds to optimize for</param>
        /// <param name="ensureBetter">Ensure that the error has reduced before updating the weights</param>
        /// <returns>The optimized weights</returns>
        public static double[][][] Optimize(Network network, TrainingSet[] sets, double trainingFactor = 0.1, int rounds = 1, bool ensureBetter = false)
        {
            for (var r = 0; r < rounds; r++)
            {
                foreach (var set in sets)
                {
                    double[][][] preWeights = null;
                    var preError = 0.0;
                    if (ensureBetter)
                    {
                        preWeights = network.Weights.Select(layer => layer.Select(neuron => neuron.ToArray()).ToArray()).ToArray();
                        preError = network.Error(sets, network.Weights);
                    }

                    network.Weights = Optimize(network, set, trainingFactor);

                    if (ensureBetter)
                    {
                        var postError = network.Error(sets, network.Weights);
                        if (postError > preError)
                        {
                            network.Weights = preWeights;
                        }
                    }
                }
            }
            return network.Weights;
        }
    }
}
