using System;

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
                    //Reconstructor.Reconstruct(args[1].Replace(".png", ".nimg"), true);
                    break;
                case "reconstruct":
                    if (args.Length >= 3 && args[2] == "demo")
                    {
                        Console.WriteLine("You've enabled demo mode, which will output the image with the network-predicted pixels highlighted in red.");
                        Reconstructor.Reconstruct(args[1], true);
                        break;
                    }
                    Reconstructor.Reconstruct(args[1], false);
                    break;
            }
            Console.WriteLine("Completed, press Enter to exit.");
            Console.ReadLine();
        }
    }
}
