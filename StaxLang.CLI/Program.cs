using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
                new Executor().Run(program, input);
            }
            else {
                byte[] program = File.ReadAllBytes(args[0]);
                string[] input = null;
                if (args.Length >= 2) input = File.ReadAllLines(args[1]);
                new Executor().Run(program, input);
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
            var input = new List<string>();
            var expected = new List<string>();
            ReadMode mode = 0;

            int i = 0;
            foreach (var line in File.ReadLines(file)) {
                if (line.StartsWith("\tname:")) {
                    name = line.Split(new[] { ':' }, 2)[1];
                }
                else if (line.StartsWith("\tin")) {
                    input.Clear();
                    mode = ReadMode.Input;
                }
                else if (line.StartsWith("\tout")) {
                    expected.Clear();
                    mode = ReadMode.Expected;
                }
                else if (line.StartsWith("\tstax")) {
                    mode = ReadMode.Code;
                }
                else if (line.StartsWith("\t#")) {
                    // comment
                }
                else {
                    switch (mode) {
                        case ReadMode.Input:
                            input.Add(line);
                            break;
                        case ReadMode.Expected:
                            expected.Add(line);
                            break;
                        case ReadMode.Code:
                            ++ProgramsExecuted;
                            var writer = new StringWriter();
                            var executor = new Executor(writer);
                            try {
                                executor.Run(line, input.ToArray(), TimeSpan.FromSeconds(2));
                                var outLines = writer.ToString()
                                    .TrimEnd('\n', '\r')
                                    .Split(new[] { Environment.NewLine }, int.MaxValue, StringSplitOptions.None);
                                if (!outLines.SequenceEqual(expected)) {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Overwrite(string.Format("Error in {0}", name));
                                    Console.WriteLine("{0}:{1}", file, i + 1);
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
                                Console.WriteLine("{0}:{1}", file, i + 1);
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                                Console.WriteLine();
                                Console.ResetColor();
                            }
                            break;
                    }
                }
                ++i;
            }
        }

        private static void ShowUsage() {
            Console.WriteLine(Executor.VersionInfo);
            Console.WriteLine("Usage:");
            Console.WriteLine("Run a program from a source file:");
            Console.WriteLine("\tstax program.stax [inputfile]");
            Console.WriteLine("Run ad-hoc code:");
            Console.WriteLine("\tstax -c staxcode [inputfile]");
        }
    }
}
