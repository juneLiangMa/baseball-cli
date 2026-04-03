using System;

namespace BaseballCli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Baseball CLI!");
            
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string command = args[0];
            switch (command)
            {
                case "--help":
                case "-h":
                    PrintUsage();
                    break;
                case "--version":
                case "-v":
                    Console.WriteLine("baseball-cli version 1.0.0");
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    PrintUsage();
                    break;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: baseball-cli [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  --help, -h       Show this help message");
            Console.WriteLine("  --version, -v    Show version information");
        }
    }
}
