namespace Zoltar
{
    public class Network
    {
        public Layer[] Layers { get; set; }
        public double[][][] Weights { get; set; } //[layer][neuron][inputNeuron]

        /// <summary>
        /// Creates a new Network with the specified properties
        /// </summary>
        /// <param name="inputs">The number of inputs</param>
        /// <param name="innerLayers">The number of hidden inner layers</param>
        /// <param name="neuronsPerLayer">The number of neurons per hidden inner layer</param>
        /// <param name="outputNeurons">The number of outputs</param>
        public Network(int inputs, int innerLayers, int neuronsPerLayer, int outputNeurons)
        {
            Layers = new Layer[innerLayers + 1];
            InitializeWeights(inputs, innerLayers, neuronsPerLayer, outputNeurons);

            Layers[0] = new Layer(neuronsPerLayer, inputs);

            for (var i = 1; i <= innerLayers; i++)
            {
                Layers[i] = new Layer(neuronsPerLayer, neuronsPerLayer);
            }
            Layers[innerLayers] = new Layer(outputNeurons, neuronsPerLayer);
        }

        /// <summary>
        /// Addes a bias neuron to the specified layer
        /// </summary>
        /// <param name="layer">The layer to add the bias neuron to, starting with input layer = 0</param>
        public void AddBiasNeuron(int layer)
        {
            layer++; //Adding the bias neuron to layer L means adding extra weights to each neuron in layer L+1
            for (var neuron = 0; neuron < Weights[layer].Length; neuron++)
            {
                var newWeights = new double[Weights[layer][neuron].Length + 1];
                Weights[layer][neuron].CopyTo(newWeights, 0);

                newWeights[Weights[layer][neuron].Length] = BruteOptimizer.GetRandomNumber(-10, 10);

                Weights[layer][neuron] = newWeights;
            }
        }

        /// <summary>
        /// Initializes the weights to random values between -10 and 10
        /// </summary>
        /// <param name="inputs">The number of input neurons</param>
        /// <param name="innerLayers">The number of hidden inner layers (not input or output)</param>
        /// <param name="neuronsPerLayer">The number of neurons per hidden inner layer</param>
        /// <param name="outputNeurons">The number of output neurons</param>
        public void InitializeWeights(int inputs, int innerLayers, int neuronsPerLayer, int outputNeurons)
        {
            Weights = new double[innerLayers + 1][][];
            Weights[0] = new double[neuronsPerLayer][];

            for (var neuronIndex = 0; neuronIndex < neuronsPerLayer; neuronIndex++)
            {
                Weights[0][neuronIndex] = new double[inputs];
                for (var i = 0; i < inputs; i++)
                {
                    Weights[0][neuronIndex][i] = BruteOptimizer.GetRandomNumber(-10, 10);
                }
            }
            
            for (var layerIndex = 1; layerIndex < innerLayers; layerIndex++)
            {
                Weights[layerIndex] = new double[neuronsPerLayer][];

                for (var neuronIndex = 0; neuronIndex < neuronsPerLayer; neuronIndex++)
                {
                    Weights[layerIndex][neuronIndex] = new double[neuronsPerLayer];
                    for (var i = 0; i < neuronsPerLayer; i++)
                    {
                        Weights[layerIndex][neuronIndex][i] = BruteOptimizer.GetRandomNumber(-10, 10);
                    }
                }
            }

            Weights[innerLayers] = new double[outputNeurons][];
            for (var neuronIndex = 0; neuronIndex < outputNeurons; neuronIndex++)
            {
                Weights[innerLayers][neuronIndex] = new double[neuronsPerLayer];
                for (var i = 0; i < neuronsPerLayer; i++)
                {
                    Weights[innerLayers][neuronIndex][i] = BruteOptimizer.GetRandomNumber(-10, 10);
                }
            }
        }

        /// <summary>
        /// Randomizes all weights.
        /// </summary>
        public void RandomizeWeights()
        {
            foreach (var layer in Weights)
            {
                foreach (var neuron in layer)
                {
                    for (var input = 0; input < neuron.Length; input++)
                    {
                        neuron[input] = BruteOptimizer.GetRandomNumber(-10, 10);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the neural network.
        /// </summary>
        /// <param name="inputs">The values of the input neurons</param>
        /// <param name="temporaryWeights">The weights to use (can differ from network.Weights)</param>
        /// <returns>The values of the output neurons</returns>
        public double[] Pulse(double[] inputs, double[][][] temporaryWeights)
        {
            double[] lastLayerOutputs = inputs;

            for (var i = 0; i < Layers.Length; i++)
            {
                lastLayerOutputs = Layers[i].Calculate(lastLayerOutputs, temporaryWeights[i]);
            }

            return lastLayerOutputs;
        }

        /// <summary>
        /// Calculates the neural network.
        /// </summary>
        /// <param name="inputs">The values of the input neurons</param>
        /// <returns>The values of the output neurons</returns>
        public double[] Pulse(double[] inputs)
        {
            return Pulse(inputs, Weights);
        }

        /// <summary>
        /// Calculates the neural network and saves the results of each layer.
        /// </summary>
        /// <param name="inputs">The values of the input neurons</param>
        /// <param name="includeInputLayer">Whether to include the input layer in the output</param>
        /// <returns>An array of what each neuron in each layer returned</returns>
        public double[][] PulseDetailed(double[] inputs, bool includeInputLayer = false)
        {
            double[] lastLayerOutputs = inputs;
            double[][] layerOutputs = new double[Layers.Length + (includeInputLayer ? 1 : 0)][];
            if (includeInputLayer)
            {
                layerOutputs[0] = inputs;
            }

            for (var i = 0; i < Layers.Length; i++)
            {
                lastLayerOutputs = Layers[i].Calculate(lastLayerOutputs, Weights[i]);
                layerOutputs[i + (includeInputLayer ? 1 : 0)] = lastLayerOutputs;
            }

            return layerOutputs;
        }
    }
}
