using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaxLang {
    public enum InstructionType { Normal = 0, Value, Block, Comparison, }

    class Block {
        private Block Root;
        private string Program;
        // absolute offset from root
        private int Start;
        // absolute offset from root
        private int End;
        private List<string> Descs;
        private int[] InstrDescLine;
        private bool[] IndexDescribed;
        private HashSet<(int InstrPtr, string AmbientDesc)> Ambients = new HashSet<(int, string)>();
        private int NestingDepth;

        // implicit eval happened
        public bool ImplicitEval { get; internal set; }

        public string Contents { get; private set; }
        // used to align the explanations
        public int InstrStartPtr { get; internal set; }
        // instrptr in absolute terms in original program
        public int RootInstrStartPtr => Start + InstrStartPtr;
        public InstructionType LastInstrType { get; internal set; }

        public Block(string program) {
            Root = this;
            NestingDepth = Start = 0;
            Program = Contents = program;
            End = program.Length;

            Descs = new List<string> { };
            InstrDescLine = new int[program.Length];
            IndexDescribed = new bool[program.Length];

            // -1 means use last
            for (int i = 0; i < program.Length; i++) InstrDescLine[i] = -1;
        }

        public bool IsEmpty() {
            for (int i = 0; i < this.Contents.Length; i++) {
                char ch = this.Contents[i];
                if (!char.IsWhiteSpace(ch)) return false;
                if (ch == '\t') {
                    i = this.Contents.IndexOf('\n', i);
                    if (i < 0) i = this.Contents.Length;
                }
            }
            return true;
        }

        public string[] Annotate() {
            if (Descs.Count == 0) return new [] { "Description not available" };
            string linear = Contents.Replace('\n', ' ').Replace('\r', ' ');

            var result = Descs.Select(_ => "").ToList();
            int lastLine = 0;
            for (int i = 0; i < Program.Length; i++) {
                int line = InstrDescLine[i];
                if (line == -1) line = lastLine;
                result[line] = result[line].PadRight(i) + linear[i];
                lastLine = line;
            }

            // rectangularize, add descriptions
            int maxlen = result.Max(r => r.Length);
            for (int i = 0; i < result.Count; i++) {
                result[i] = result[i].PadRight(maxlen) + '\t' + Descs[i];
            }
            result.Insert(0, "\t" + Executor.VersionInfo);
            return result.ToArray();
        }

        private Block(Block parent, int start, int end) {
            Root = parent.Root;
            NestingDepth = parent.NestingDepth + 1;
            Program = parent.Program;
            Start = parent.Start + start;
            End = parent.Start + end;
            Contents = Program.Substring(Start, End - Start);
        }

        public Block SubBlock(int start, int end) => new Block(this, start, end);
        public Block SubBlock(int start) => new Block(this, start, Contents.Length);

        private string IndentSpaces => new string(' ', NestingDepth * 2);

        internal void AddAmbient(string text) {
            if (!Root.Ambients.Contains((RootInstrStartPtr, text))) {
                Root.Ambients.Add((RootInstrStartPtr, text));
                Root.Descs.Add(IndentSpaces + text);
            }
        }

        internal void AddDesc(string text) {
            if (Root.IndexDescribed[RootInstrStartPtr]) return; // already described
            Root.IndexDescribed[RootInstrStartPtr] = true;

            Root.Descs.Add(IndentSpaces + text);
            Root.InstrDescLine[RootInstrStartPtr] = Root.Descs.Count - 1;
        }

        internal void AmendDesc(Func<string, string> transform) {
            if (Root.IndexDescribed[RootInstrStartPtr]) return; // already described
            Root.IndexDescribed[RootInstrStartPtr] = true;

            int idx = Root.Descs.Count - 1;
            Root.Descs[idx] = IndentSpaces + transform(Root.Descs[idx].TrimStart()); 
        }

        internal void UnAnnotate() {
            if (this != Root || !Contents.StartsWith("\tstax")) return; // not annotated
            var lines = Contents
                .Split('\r', '\n')
                .Select(l => l.Split(new[] { '\t' }, 2)[0])
                .ToArray();
            int maxlen = lines.Max(l => l.Length);
            char[] result = new char[maxlen];

            for (int i = 0; i < maxlen; i++) {
                result[i] = lines.Max(l => l.ElementAtOrDefault(i));
            }

            Program = Contents = new string(result);
        }
    }
}