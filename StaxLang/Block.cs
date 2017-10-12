using System;
using System.Collections.Generic;
using System.Linq;

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

        public string Contents { get; }
        // used to align the explanations
        public int InstrStartPtr { get; internal set; }
        // instrptr in absolute terms in original program
        public int RootInstrStartPtr => Start + InstrStartPtr;
        public InstructionType LastInstrType { get; internal set; }

        public Block(string program) {
            Root = this;
            Program = Contents = program;
            Start = 0;
            End = program.Length;

            Descs = new List<string> { };
            InstrDescLine = new int[program.Length];
            IndexDescribed = new bool[program.Length];

            // -1 means use last
            for (int i = 0; i < program.Length; i++) InstrDescLine[i] = -1;
        }

        public string[] Annotate() {
            if (Descs.Count == 0) return new [] { "Description not available" };

            var result = Descs.Select(_ => "").ToArray();
            int lastLine = 0;
            for (int i = 0; i < Program.Length; i++) {
                int line = InstrDescLine[i];
                if (line == -1) line = lastLine;
                result[line] = result[line].PadRight(i) + Contents[i];
                lastLine = line;
            }

            // rectangularize, add descriptions
            int maxlen = result.Max(r => r.Length);
            for (int i = 0; i < result.Length; i++) {
                result[i] = result[i].PadRight(maxlen) + '\t' + Descs[i];
            }
            return result;
        }

        private Block(Block parent, int start, int end) {
            Root = parent.Root;
            Program = parent.Program;
            Start = parent.Start + start;
            End = parent.Start + end;
            Contents = Program.Substring(Start, End - Start);
        }

        public Block SubBlock(int start, int end) => new Block(this, start, end);
        public Block SubBlock(int start) => new Block(this, start, Contents.Length);

        internal void AddAmbient(string text) {
            if (!Root.Ambients.Contains((RootInstrStartPtr, text))) {
                Root.Ambients.Add((RootInstrStartPtr, text));
                Root.Descs.Add(text);
            }
        }

        internal void AddDesc(string text) {
            if (Root.IndexDescribed[RootInstrStartPtr]) return; // already described
            Root.IndexDescribed[RootInstrStartPtr] = true;

            Root.Descs.Add(text);
            Root.InstrDescLine[RootInstrStartPtr] = Root.Descs.Count - 1;
        }

        internal void AmendDesc(Func<string, string> transform) {
            if (Root.IndexDescribed[RootInstrStartPtr]) return; // already described
            Root.IndexDescribed[RootInstrStartPtr] = true;

            int idx = Root.Descs.Count - 1;
            Root.Descs[idx] = transform(Root.Descs[idx]); 
        }
    }
}