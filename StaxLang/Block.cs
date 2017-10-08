using System;
using System.Collections.Generic;
using System.Linq;

namespace StaxLang {
    public enum InstructionType { Normal = 0, Value, Block, Comparison, }

    class Block {
        private Block Root;
        private string Program;
        private int Start;
        private int End;
        private List<string> Descs;
        private int[] InstrDescLine;
        private bool[] IndexDescribed;

        public string Contents { get; }
        // used to align the explanations
        public int InstrStartPtr { get; internal set; }
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

        public List<string> Annotate() {
            if (InstrDescLine.Max() < 0) return new List<string> { "Description not available" };

            var result = new List<string>();
            int lastLine = 0;
            for (int i = 0; i < Program.Length; i++) {
                int line = InstrDescLine[i];
                if (line == -1) line = lastLine;

                if (line < result.Count) { // use existing
                    result[line] = result[line].PadRight(i) + Contents[i];
                }
                else if (line >= result.Count) { // make new
                    while (result.Count < line) result.Add("");
                    result.Add("".PadLeft(i) + Contents[i]);
                }
                else throw new Exception("description line index skipped");
                lastLine = line;
            }

            // rectangularize, add descriptions
            int maxlen = result.Max(r => r.Length);
            for (int i = 0; i < result.Count; i++) {
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

        internal void AddDesc(string text) {
            int progptr = Start + InstrStartPtr; // index in original program
            if (Root.IndexDescribed[progptr]) return; // already described
            Root.IndexDescribed[progptr] = true;

            Root.Descs.Add(text);
            Root.InstrDescLine[progptr] = Root.Descs.Count - 1;
        }

        internal void AmendDesc(Func<string, string> transform) {
            int progptr = Start + InstrStartPtr; // index in original program
            if (Root.IndexDescribed[progptr]) return; // already described
            Root.IndexDescribed[progptr] = true;

            int idx = Root.Descs.Count - 1;
            Root.Descs[idx] = transform(Root.Descs[idx]); 
        }
    }
}