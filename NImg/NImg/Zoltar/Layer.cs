using System;

namespace Zoltar
{
    public class Layer
    {
        /// <summary>
        /// The Sigmoid activation function, 1/(1+e^-x)
        /// </summary>
        /// <param name="input">The input for the activation function</param>
        /// <returns>A scaled value between 0 and 1</returns>
        public static double SigmoidActivation(double input)
        {
            return (1 / (1 + Math.Exp(-1 * input)));
        }

        /// <summary>
        /// Creates a layer with the specified properties
        /// </summary>
        /// <param name="neurons">The number of neurons</param>
        /// <param name="inputsPerNeuron">The number of inputs per neuron</param>
        /// <param name="activationFunction">The activation function to use</param>
        public Layer(int neurons, int inputsPerNeuron, Func<double, double> activationFunction = null)
        {
            Neurons = neurons;
            Inputs = inputsPerNeuron;
            ActivationFunction = activationFunction ?? SigmoidActivation;
        }

        /// <summary>
        /// The activation function used
        /// </summary>
        public Func<double, double> ActivationFunction { get; set; }
        /// <summary>
        /// The number of neurons
        /// </summary>
        public int Neurons { get; set; }
        /// <summary>
        /// The number of inputs per neuron
        /// </summary>
        public int Inputs { get; set; }

        /// <summary>
        /// Calculates the outputs of each neuron in the layer
        /// </summary>
        /// <param name="inputs">An array of inputs</param>
        /// <param name="weights">An array of weights to use</param>
        /// <returns>An array of the outputs of the neurons in the layer</returns>
        public double[] Calculate(double[] inputs, double[][] weights)
        {
            var result = new double[Neurons];

            for (var neuron = 0; neuron < Neurons; neuron++)
            {
                var input = 0.0;
                for (var i = 0; i < weights[neuron].Length; i++)
                {
                    if (i >= inputs.Length)
                    {
                        //There's no matching input neuron, assume it's a bias neuron of value 1
                        input += weights[neuron][i] * 1;
                        continue;
                    }

                    input += weights[neuron][i] * inputs[i];
                }
                result[neuron] = ActivationFunction(input);
            }
            return result;
        }
    }
}
