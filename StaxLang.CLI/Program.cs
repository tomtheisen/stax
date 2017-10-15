using System;
using System.IO;

namespace StaxLang.CLI {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 0) {
                ShowUsage();
                return;
            }

            if (args[0] == "-c") {
                string program = args[1];
                string[] input = null;
                if (args.Length >= 3) input = File.ReadAllLines(args[2]);
                new Executor().Run(program, input);
            }
            else {
                byte[] program = File.ReadAllBytes(args[0]);
                string[] input = null;
                if (args.Length >= 2) input = File.ReadAllLines(args[1]);
                new Executor().Run(program, input);
            }
        }

        private static void ShowUsage() {
            Console.WriteLine(Executor.Version);
            Console.WriteLine("Usage:");
            Console.WriteLine("Run a program from a source file:");
            Console.WriteLine("\tstax program.stax [inputfile]");
            Console.WriteLine("Run ad-hoc code:");
            Console.WriteLine("\tstax -c staxcode [inputfile]");
        }
    }
}
