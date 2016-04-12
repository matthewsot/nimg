namespace Zoltar
{
    public class TrainingSet
    {
        public double[] Inputs { get; set; }
        public double[] Outputs { get; set; }
        public TrainingSet(double[] inputs, double[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }
    }
}
