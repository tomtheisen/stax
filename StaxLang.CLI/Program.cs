using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StaxLang.CLI {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 0) {
                ShowUsage();
                return;
            }

            bool @throw = args.Contains("-throw");
            if (args[0] == "-tests") {
                DoTests(args[1], @throw);
            }
            else if (args[0] == "-test") {
                DoTest(args[1], @throw);
            }
            else if (args[0] == "-c") {
                string program = args[1];
                string[] input = null;
                if (args.Length >= 3) input = File.ReadAllLines(args[2]);
                new Executor(args.Skip(3).ToArray()).Run(program, input);
            }
            else if (args[0] == "-u") {
                string program = File.ReadAllText(args[1], Encoding.UTF8);
                program = program.TrimEnd('\n', '\r');
                string[] input;
                if (args.Length >= 3) input = File.ReadAllLines(args[2], Encoding.UTF8);
                else input = Console.In.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                new Executor(args.Skip(3).ToArray()).Run(program, input);
            }
            else {
                byte[] program = File.ReadAllBytes(args[0]);
                string[] input;
                if (args.Length >= 2) input = File.ReadAllLines(args[1]);
                else input = Console.In.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                new Executor(args.Skip(2).ToArray()).Run(program, input);
            }
        }

        private static void Overwrite(string msg) => Console.Write(msg.PadRight(Console.BufferWidth));

        private static void DoTests(string path, bool @throw) {
            var sw = Stopwatch.StartNew();
            var canon = Path.GetFullPath(path);
            var files = Directory.GetFiles(canon, "*.staxtest", SearchOption.AllDirectories);
            int i = 0;
            Console.WriteLine();
            foreach (var file in files) {
                var name = Path.GetFileNameWithoutExtension(file);
                string msg = string.Format("[{0}/{1}] {2}", ++i, files.Length, name);
                Overwrite(msg);
                Console.CursorTop -= 1;

                DoTest(file, @throw);
            }
            Overwrite(string.Format("[{0}/{0}] specifications complete", files.Length));
            Console.WriteLine("{0} programs executed", ProgramsExecuted);
            Console.WriteLine(sw.Elapsed);
        }

        private static int ProgramsExecuted = 0;
        private enum ReadMode { Input = 1, Expected, Code }
        private static void DoTest(string file, bool @throw) {
            string name = Path.GetFileNameWithoutExtension(file);
            var cases = new List<(List<string> Input, List<string> Expected)>();
            bool executed = true;
            var lastInput = new List<string>();
            var lastExpected = new List<string>();
            ReadMode mode = 0;

            void NewCaseSet() {
                lastInput.Clear();
                lastExpected.Clear();
                cases.Clear();
                executed = false;
            }

            int i = 0;
            foreach (var line in File.ReadLines(file)) {
                if (line.StartsWith("\tname:")) {
                    name = line.Split(new[] { ':' }, 2)[1];
                }
                else if (line.StartsWith("\tin")) {
                    if (executed) {
                        NewCaseSet();
                    }
                    else {
                        cases.Add((lastInput, lastExpected));
                        lastInput = new List<string>();
                    }
                    mode = ReadMode.Input;
                }
                else if (line.StartsWith("\tout")) {
                    if (executed) NewCaseSet();
                    lastExpected = new List<string>();
                    mode = ReadMode.Expected;
                }
                else if (line.StartsWith("\tstax")) {
                    if (!executed) {
                        cases.Add((lastInput, lastExpected));
                        lastInput = new List<string>();
                        lastExpected = new List<string>();
                    }
                    executed = true;
                    mode = ReadMode.Code;
                }
                else if (line.StartsWith("\t#")) {
                    // comment
                }
                else {
                    switch (mode) {
                        case ReadMode.Input:
                            lastInput.Add(line);
                            break;
                        case ReadMode.Expected:
                            lastExpected.Add(line);
                            break;
                        case ReadMode.Code:
                            string fileLocation = string.Format("{0}:{1}", file, i+1);
                            string stax = line;
                            foreach (var c in cases) {
                                ++ProgramsExecuted;
                                ExecuteCase(fileLocation, name, stax, c.Input, c.Expected, @throw);
                            }
                            break;
                    }
                }
                ++i;
            }
        }

        private static void ExecuteCase(string fileSpecifier, string name, string stax, List<string> input, List<string> expected, bool @throw) {
            var writer = new StringWriter();
            var executor = new Executor(Array.Empty<string>(), writer);

            try {
                executor.Run(stax, input.ToArray(), TimeSpan.FromSeconds(2));
                var outLines = writer.ToString()
                    .TrimEnd('\n', '\r')
                    .Split(new[] { Environment.NewLine }, int.MaxValue, StringSplitOptions.None);
                if (!outLines.SequenceEqual(expected)) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Overwrite(string.Format("Error in {0}", name));
                    Console.WriteLine(fileSpecifier);
                    Console.WriteLine("Expected: ");
                    foreach (var e in expected) {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine("Actual: ");
                    foreach (var a in outLines) {
                        Console.WriteLine(a);
                    }
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }
            catch (Exception ex) when (!@throw) {
                Console.ForegroundColor = ConsoleColor.Red;
                Overwrite(string.Format("Error in {0}", name));
                Console.WriteLine(fileSpecifier);
                Console.WriteLine("Input:");
                input.ForEach(Console.WriteLine);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void ShowUsage() {
            Console.WriteLine(Executor.VersionInfo);
            Console.WriteLine("Usage:");
            Console.WriteLine("Run a program from a source file:");
            Console.WriteLine("\tstax [-u] program.stax [inputfile]");
            Console.WriteLine("-u reads the program using utf-8, rather than stax encoding");
            Console.WriteLine();
            Console.WriteLine("Run ad-hoc code:");
            Console.WriteLine("\tstax -c staxcode [inputfile]");
        }
    }
}
