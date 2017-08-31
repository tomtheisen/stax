namespace StaxLang {
    internal class Block {
        public string Program { get; }

        public Block(string program) {
            Program = program;
        }

        public override string ToString() => $"Block {{{Program}}}";
    }
}