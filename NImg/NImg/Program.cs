namespace NImg
{
    class Program
    {
        static void Main(string[] args)
        {
            switch (args[0])
            {
                case "compress":
                    Compressor.Compress(args[1]);
                    break;
                case "reconstruct":
                    Reconstructor.Reconstruct(args[1]);
                    break;
            }
        }
    }
}
