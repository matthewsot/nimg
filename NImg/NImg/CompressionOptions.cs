using System.Reflection;

namespace NImg
{
    public class CompressionOptions
    {
        public object this[string property]
        {
            set
            {
                this.GetType().GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, value, null);
            }
        }

        public int ColorIndexBytes { get; set; }
        public int ColorIndexTolerance { get; set; }
        public int InnerLayers { get; set; }
        public int InputPixels { get; set; }
        public int MaxTrainingSets { get; set; }
        public int NeuronsPerLayer { get; set; }
        public int TrainingRounds { get; set; }
        public int TrainingSetRandomization { get; set; }
        public int WriteTolerance { get; set; }
    }
}
