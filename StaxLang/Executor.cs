using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

/* To add:
 *     FeatureTests for generators
 *     debugger
 */

namespace StaxLang {
    public class Executor {
        public const string VersionInfo = "Stax 0.0.0 - Tom Theisen - https://github.com/ttheisen/stax";

        private bool OutputWritten = false;
        public TextWriter Output { get; private set; }
        public bool Annotate { get; set; }
        public IReadOnlyList<string> Annotation { get; private set; } = null;
        private List<Block> GotoTargets;
        private int GotoCallDepth = 0;

        private static IReadOnlyDictionary<char, (object Value, string Name)> Constants = new Dictionary<char, (object, string)> {
            ['?'] = (S2A(VersionInfo), "version info"),
            ['0'] = (new Rational(0, 1), "0/1"),
            ['2'] = (0.5, "0.5"),
            ['3'] = (Math.Pow(2, 1.0 / 12), "semitone ratio in equal temperament"),
            ['/'] = (Math.PI / 3, "pi / 3"),
            ['%'] = (new List<object>{ BigInteger.Zero, BigInteger.Zero }, "[0 0]"),
            ['a'] = (S2A("abcdefghijklmnopqrstuvwxyz"), "lowercase alphabet"),
            ['A'] = (S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), "uppercase alphabet"),
            ['b'] = (S2A("()[]{}<>"), "matched brackets"),
            ['B'] = (new BigInteger(256), "256"),
            ['c'] = (S2A("bcdfghjklmnpqrstvwxyz"), "lowercase consonants"),
            ['C'] = (S2A("BCDFGHJKLMNPQRSTVWXYZ"), "uppercase consonants"),
            ['d'] = (S2A("0123456789"), "decimal digits"),
            ['D'] = (Math.Sqrt(2), "sqrt(2)"),
            ['e'] = (Math.E, "natural log base"),
            ['E'] = (Math.Sqrt(3), "sqrt(3)"),
            ['h'] = (S2A("0123456789abcdef"), "lowercase hex digits"),
            ['H'] = (S2A("0123456789ABCDEF"), "uppercase hex digits"),
            ['i'] = (double.NegativeInfinity, "negative infinity"),
            ['I'] = (double.PositiveInfinity, "positive infinity"),
            ['k'] = (new BigInteger(1000), "one thousand"),
            ['l'] = (S2A("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all letters"),
            ['L'] = (S2A("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all alphanumerics"),
            ['m'] = (new BigInteger(0x7fffffff), "0x7fffffff"),
            ['M'] = (new BigInteger(1000000), "one million"),
            ['n'] = (S2A("\n"), "newline"),  // also just A]
            ['p'] = (S2A(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"), "all printable ascii characters"),
            ['P'] = (Math.PI, "pi"),
            ['q'] = (Math.PI / 2, "pi/2"),
            ['s'] = (S2A(" \t\r\n\v"), "all ascii whitespace"),
            ['S'] = (Math.PI * 4 / 3, "4/3 pi"),
            ['t'] = (Math.PI * 2, "tau (2pi)"),
            ['T'] = (10.0, "10.0"),
            ['v'] = (S2A("aeiou"), "lowercase vowels"),
            ['V'] = (S2A("AEIOU"), "uppercase vowels"),
            ['w'] = (S2A("0123456789abcdefghijklmnopqrstuvwxyz"), "all digits and lowercase letters"),
            ['W'] = (S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all digits and uppercase letters"),
        };

        private BigInteger Index; // loop iteration
        private BigInteger IndexOuter; // outer loop iteration
        private dynamic X; // register - default to numeric value of first input
        private dynamic Y; // register - default to first input
        private dynamic _; // implicit iterator

        private Stack<dynamic> MainStack;
        private Stack<dynamic> InputStack;

        public Executor(TextWriter output = null) {
            Output = output ?? Console.Out;
        }

        /// <summary>
        /// run a stax program
        /// </summary>
        /// <param name="program"></param>
        /// <param name="input"></param>
        /// <returns>number of steps the program ran</returns>
        public int Run(byte[] programBytes, string[] input, TimeSpan? timeout = null) {
            Encoding e = StaxPacker.IsPacked(programBytes) ? StaxPacker.Encoding : Encoding.ASCII;
            return Run(e.GetString(programBytes), input, timeout);
        }

        /// <summary>
        /// run a stax program
        /// </summary>
        /// <param name="program"></param>
        /// <param name="input"></param>
        /// <returns>number of steps the program ran</returns>
        public int Run(string program, string[] input, TimeSpan? timeout = null) {
            if (StaxPacker.IsPacked(program)) program = StaxPacker.Unpack(program);
            var block = new Block(program);
            block.UnAnnotate();
            Initialize(block, input);
            int step = 0;
            try {
                var sw = Stopwatch.StartNew();
                foreach (var s in RunSteps(block)) {
                    if (s.Cancel) break;
                    if (sw.Elapsed > timeout) throw new StaxException("program is running too long");
                    ++step;
                }
                while (TotalStackSize > 0 && IsBlock(Peek())) {
                    foreach (var s in RunSteps(Pop())) {
                        if (s.Cancel) break;
                        if (sw.Elapsed > timeout) throw new StaxException("program is running too long");
                        ++step;
                    }
                }
            }
            catch (InvalidOperationException) { }
            if (!OutputWritten) {
                block.AddAmbient("top of stack implicitly printed");
                Print(Pop());
            }

            if (Annotate) Annotation = block.Annotate(); 
            return step;
        }

        private void Initialize(Block programBlock, string[] input) {
            int gotoTarget = 0;
            GotoTargets = new List<Block> { programBlock };
            do {
                ParseBlock(programBlock, ref gotoTarget, true);
                if (++gotoTarget < programBlock.Contents.Length) {
                    GotoTargets.Add(programBlock.SubBlock(gotoTarget));
                }
            } while (gotoTarget < programBlock.Contents.Length);

            input = input ?? Array.Empty<string>();

            IndexOuter = Index = 0;
            X = BigInteger.Zero;
            Y = S2A("");
            _ = S2A(string.Join("\n", input));

            if (input.Length > 0) {
                Y = S2A(input[0]);
            }

            var transformedInput = input
                .Reverse()
                .SkipWhile(s => s == "")
                .Select(S2A).ToArray();
            MainStack = new Stack<dynamic>();
            InputStack = new Stack<dynamic>(transformedInput);

            if (programBlock.Contents.StartsWith("i")) {
                programBlock.AddDesc("suppress single line eval; treat input as raw string");
            }
            else if (transformedInput.Length == 1) {
                if (!DoEval()) {
                    MainStack.Clear();
                    InputStack = new Stack<dynamic>(transformedInput);
                }
                else if (TotalStackSize == 0) {
                    InputStack = new Stack<dynamic>(transformedInput);
                }
                else {
                    programBlock.AddAmbient("program input is implicitly parsed");
                    programBlock.ImplicitEval = true;
                    X = MainStack.Last();
                    (MainStack, InputStack) = (InputStack, MainStack);
                }
            }
        }

        private dynamic Pop() => MainStack.Any() ? MainStack.Pop() : InputStack.Pop();

        private dynamic Peek() => MainStack.Any() ? MainStack.Peek() : InputStack.Peek();

        private void Push(dynamic arg) => MainStack.Push(arg);

        Stack<(dynamic _, BigInteger IndexOuter)> CallStackFrames = new Stack<(dynamic, BigInteger)>();

        private void PushStackFrame() {
            CallStackFrames.Push((_, IndexOuter));
            IndexOuter = Index;
            Index = 0;
        }

        private void PopStackFrame() {
            Index = IndexOuter;
            (_, IndexOuter) = CallStackFrames.Pop();
        }

        private int TotalStackSize => MainStack.Count + InputStack.Count;

        private void RunMacro(string program) {
            foreach (var s in RunSteps(new Block(program))) ;
        }

        private IEnumerable<ExecutionState> RunSteps(Block block) {
            var program = block.Contents;
            if (TotalStackSize > 0 && CallStackFrames.Count == 0 && !block.ImplicitEval) switch (program.FirstOrDefault()) {
                case 'm': // line-map
                case 'f': // line-filter
                case 'F': // line-for
                    DoListify();
                    break;
            }

            InstructionType type = 0;
            for (int ip = 0; ip < program.Length;) {
                type = InstructionType.Normal;
                block.InstrStartPtr = ip;

                yield return new ExecutionState();
                switch (program[ip]) {
                    case '0': case '1': case '2': case '3': case '4':
                    case '5': case '6': case '7': case '8': case '9':
                        Push(ParseNumber(program, ref ip));
                        --ip;
                        block.AddDesc(Peek().ToString());
                        type = InstructionType.Value;
                        break;
                    case ' ': case '\n': case '\r':
                        ++ip;
                        continue;
                    case '\t': // line comment
                        ip = program.IndexOf('\n', ip);
                        if (ip == -1) yield break;
                        break;
                    case ';': block.AddDesc("peek from input stack");
                        Push(InputStack.Peek());
                        break;
                    case ',': block.AddDesc("pop from input stack");
                        Push(InputStack.Pop());
                        break;
                    case '~': block.AddDesc("push to input stack");
                        InputStack.Push(Pop());
                        break;
                    case '#': 
                        {
                            // make sure number is on top
                            dynamic b = Pop(), a = Pop();
                            if (IsNumber(a)) (a, b) = (b, a);
                            Push(a);
                            Push(b);
                        }
                        if (IsArray(Peek())) {

                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "count number of times " + e + " is found as a substring");
                            else block.AddDesc("count number of times substring is found");
                            RunMacro("/%v");
                        }
                        else if (IsNumber(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "count number of times " + e + " is found in array");
                            else block.AddDesc("count number of times element appears in array");
                            RunMacro("]|&%");
                        }
                        break;
                    case '"': 
                        {
                            Push(ParseString(program, true, ref ip, out bool implicitEnd));
                            type = InstructionType.Value;
                            if (implicitEnd) {
                                block.AddDesc("print unclosed literal");
                                Print(Pop(), newline: false);
                            }
                            else block.AddDesc("literal");
                        }
                        break;
                    case '`': 
                        {
                            Push(ParseCompressedString(program, ref ip, out bool implitEnd));
                            type = InstructionType.Value;
                            if (implitEnd) {
                                block.AddDesc($"print unclosed compressed [{A2S(Peek())}]");
                                Print(Peek());
                            }
                            else block.AddDesc($"compressed [{A2S(Peek())}]");
                        }
                        break;
                    case '\'':
                        block.AddDesc("single character string literal");
                        type = InstructionType.Value;
                        Push(S2A(program.Substring(++ip, 1)));
                        break;
                    case '.':
                        block.AddDesc("two character string literal");
                        type = InstructionType.Value;
                        Push(S2A(program.Substring(ip + 1, 2)));
                        ip += 2;
                        break;
                    case '{':
                        block.AddDesc("code block");
                        type = InstructionType.Block;
                        ++ip;
                        Push(ParseBlock(block, ref ip, false));
                        break;
                    case '}':
                        block.AddDesc("end");
                        yield break;
                    case '!':
                        if (block.LastInstrType == InstructionType.Comparison) block.AmendDesc(e => "not " + e);
                        else block.AddDesc("not");
                        Push(IsTruthy(Pop()) ? BigInteger.Zero : BigInteger.One);
                        break;
                    case '+':
                        DoPlus(block);
                        break;
                    case '-':
                        DoMinus(block);
                        break;
                    case '*':
                        foreach (var s in DoStar(block)) yield return s;
                        break;
                    case '/':
                        foreach (var s in DoSlash(block)) yield return s;
                        break;
                    case '\\':
                        DoZipRepeat(block);
                        break;
                    case '%':
                        DoPercent(block);
                        break;
                    case '@': // read index
                        DoAt(block);
                        break;
                    case '&': // assign index
                        DoAssignIndex(block);
                        break;
                    case '$':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "convert " + e + " to string");
                        else block.AddDesc("convert to string");
                        Push(ToString(Pop()));
                        break;
                    case '<':
                        type = InstructionType.Comparison;
                        DoLessThan(block);
                        break;
                    case '>':
                        type = InstructionType.Comparison;
                        DoGreaterThan(block);
                        break;
                    case '=':
                        type = InstructionType.Comparison;
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "equals " + e);
                        else block.AddDesc("equal to");
                        Push(AreEqual(Pop(), Pop()) ? BigInteger.One : BigInteger.Zero);
                        break;
                    case 'v':
                        if (IsNumber(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " - 1");
                            else block.AddDesc("decrement");
                            Push(Pop() - 1);
                        }
                        else if (IsArray(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in lower case");
                            else block.AddDesc("to lower case");
                            Push(S2A(A2S(Pop()).ToLower()));
                        }
                        else throw new StaxException("Bad type for v");
                        break;
                    case '^':
                        if (IsNumber(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " + 1");
                            else block.AddDesc("increment");
                            Push(Pop() + 1);
                        }
                        else if (IsArray(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in upper case");
                            else block.AddDesc("to upper case");
                            Push(S2A(A2S(Pop()).ToUpper()));
                        }
                        else throw new StaxException("Bad type for ^");
                        break;
                    case '(':
                        foreach (var s in DoPadRight(block)) yield return s;
                        break;
                    case ')':
                        DoPadLeft(block);
                        break;
                    case '[':
                        block.AddDesc("duplicate element under top of stack");
                        RunMacro("ss~c,");
                        break;
                    case ']':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "singleton array of " + e);
                        else block.AddDesc("make singleton array");
                        Push(new List<object> { Pop() });
                        break;
                    case '?': // if
                        block.AddDesc("if; pop 3 elements - (bottom ? middle : top)");
                        foreach (var s in DoIf()) yield return s;
                        break;
                    case 'a': // alter stack
                        {
                            block.AddDesc("move 3rd element in stack to top");
                            dynamic c = Pop(), b = Pop(), a = Pop();
                            Push(b); Push(c); Push(a);
                        }
                        break;
                    case 'A':
                        block.AddDesc("10");
                        type = InstructionType.Value;
                        Push(new BigInteger(10));
                        break;
                    case 'b': 
                        {
                            block.AddDesc("copy top two values on stack");
                            dynamic b = Pop(), a = Peek();
                            Push(b); Push(a); Push(b);
                        }
                        break;
                    case 'B':
                        if (IsInt(Peek())) {
                            DoOverlappingBatch(block);
                        }
                        else if (IsArray(Peek())) {
                            block.AddDesc("uncons; remove first element from array and push both");
                            RunMacro("c1tsh");
                        }
                        else if (IsFrac(Peek())) {
                            block.AddDesc("properize fraction; push integer floor and remainder of fraction separately");
                            RunMacro("c@s1%");
                        }
                        else throw new StaxException("Bad type for B");
                        break;
                    case 'c':
                        block.AddDesc("copy of top element in stack");
                        type = InstructionType.Value;
                        Push(Peek());
                        break;
                    case 'C':
                        if (IsBlock(Peek())) {
                            foreach (var s in DoCollect(block)) yield return s;
                        }
                        else {
                            if (CallStackFrames.Any()) block.AddDesc("cancel iteration if true");
                            else block.AddDesc("terminate if true");
                            if (IsTruthy(Pop())) {
                                yield return ExecutionState.CancelState;
                                yield break;
                            }
                        }
                        break;
                    case 'd': 
                        block.AddDesc("pop and discard");
                        Pop();
                        break;
                    case 'D':
                        if (IsArray(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " with the first element removed");
                            else block.AddDesc("remove first element");
                            var result = new List<object>(Pop());
                            result.RemoveAt(0);
                            Push(result);
                        }
                        if (IsInt(Peek())) { // n times do
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " times do");
                            else block.AddDesc("n times do");
                            var n = Pop();
                            PushStackFrame();
                            for (Index = BigInteger.Zero; Index < n; Index++) {
                                _ = Index + 1;
                                foreach (var s in RunSteps(block.SubBlock(ip + 1))) yield return s;
                            }
                            PopStackFrame();
                            yield break;
                        }
                        break;
                    case 'e': 
                        if (IsArray(Peek())) {
                            block.AddDesc("eval - parse strings, arrays, and numbers");
                            if (!DoEval()) throw new StaxException("eval failed");
                        }
                        else if (IsFloat(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "ceiling of " + e);
                            else block.AddDesc("ceiling");
                            Push(new BigInteger(Math.Ceiling(Pop())));
                        }
                        else if (IsFrac(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "ceiling of " + e);
                            else block.AddDesc("ceiling");
                            Push(((Rational)Pop()).Ceil());
                        }
                        else if (IsBlock(Peek())) {
                            foreach (var s in DoExtremaBy(-1, block)) yield return s;
                        }
                        break;
                    case 'E': // explode (de-listify)
                        if (IsBlock(Peek())) {
                            foreach (var s in DoExtremaBy(1, block)) yield return s;
                        }
                        else DoExplode(block);
                        break;
                    case 'f': // block filter
                        { 
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoFilter(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'F': // for loop
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoFor(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'g': // generator
                        {
                            // shorthand is indicated by
                            //   no trailing block
                            //   OR trailing block with explicit close }, in which case it becomes a filter
                            bool shorthand = !IsBlock(Peek()) || (ip >= 1 && program[ip - 1] == '}');
                            foreach (var s in DoGenerator(block, shorthand, program[++ip], block.SubBlock(ip + 1))) {
                                yield return s;
                            }
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'G': // goto
                        { 
                            block.AddDesc("goto trailing outer block; return here when done");
                            var target = GotoTargets[Math.Min(++GotoCallDepth, GotoTargets.Count - 1)];
                            foreach (var s in RunSteps(target)) {
                                if (s.Cancel) break;
                                else yield return s;
                            }
                            GotoCallDepth--;
                        }
                        break;
                    case 'h':
                        if (IsInt(Peek()) || IsFloat(Peek())) {
                            block.AddDesc("half");
                            RunMacro("2/"); 
                        }
                        else if (IsFrac(Peek())) {
                            block.AddDesc("numerator");
                            Push(Pop().Num);
                        }
                        else if (IsArray(Peek())) {
                            block.AddDesc("first element");
                            Push(Pop()[0]); 
                        }
                        else if (IsBlock(Peek())) {
                            Block pred = Pop();
                            List<object> result = new List<object>(), arr = Pop();
                            block.AddDesc("keep matching elements from beginning of array");
                            bool cancelled = false;
                            PushStackFrame();
                            foreach (var e in arr) {
                                Push(_ = e);
                                foreach (var s in RunSteps(pred)) {
                                    if (cancelled = s.Cancel) break;
                                    yield return s;
                                }
                                if (cancelled || !IsTruthy(Pop())) break;
                                result.Add(e);
                                ++Index;
                            }
                            PopStackFrame();
                            Push(result);
                        }
                        break;
                    case 'H':
                        if (IsInt(Peek()) || IsFloat(Peek())) {
                            block.AddDesc("un-half (double)");
                            Push(Pop() * 2);
                        }
                        else if (IsFrac(Peek())) {
                            block.AddDesc("denominator");
                            Push(Pop().Den); 
                        }
                        else if (IsArray(Peek())) {
                            block.AddDesc("last element");
                            Push(Peek()[Pop().Count - 1]);
                        }
                        else if (IsBlock(Peek())) {
                            Block pred = Pop();
                            List<object> result = new List<object>(), arr = Pop();
                            arr.Reverse();
                            block.AddDesc("keep matching elements from end of array");
                            bool cancelled = false;
                            PushStackFrame();
                            foreach (var e in arr) {
                                Push(_ = e);
                                foreach (var s in RunSteps(pred)) {
                                    if (cancelled = s.Cancel) break;
                                    yield return s;
                                }
                                if (cancelled || !IsTruthy(Pop())) break;
                                result.Insert(0, e);
                                ++Index;
                            }
                            PopStackFrame();
                            Push(result);
                        }
                        break;
                    case 'i':
                        if (CallStackFrames.Any()) {
                            type = InstructionType.Value;
                            block.AddDesc("the iteration index");
                            Push(Index);
                        }
                        break;
                    case 'I':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "find first index of " + e);
                        else block.AddDesc("find first index");
                        foreach (var s in DoFindIndex()) yield return s;
                        break;
                    case 'j':
                        if (IsArray(Peek())) {
                            block.AddDesc("un-join (split) by spaces");
                            RunMacro("' /");
                        }
                        else if (IsInt(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "string format rounded to " + e + " decimal places");
                            else block.AddDesc("string format rounded to n decimal places");
                            BigInteger digits = Pop();
                            double num = (double)Pop();
                            Push(S2A(num.ToString($"F{digits}")));
                        }
                        else if (IsNumber(Peek())) {
                            block.AddDesc("round to nearest integer");
                            RunMacro("2u+@");
                        }
                        else if (IsBlock(Peek())) {
                            foreach (var s in DoFindFirst(block)) yield return s;
                        }
                        break;
                    case 'J':
                        if (IsArray(Peek())) {
                            block.AddDesc("join with spaces");
                            RunMacro("' *");
                        }
                        else if (IsNumber(Peek())) {
                            block.AddDesc("square");
                            Push(Peek() * Pop());
                        }
                        else if (IsBlock(Peek())) {
                            foreach (var s in DoFindFirst(block, true)) yield return s;
                        }
                        break;
                    case 'k': // reduce
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoReduce(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'K': // cross-map
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoCrossMap(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'l': // listify-n
                        DoListifyN(block);
                        break;
                    case 'L':
                        block.AddDesc("clear stacks; put contents in a single array");
                        DoListify();
                        break;
                    case 'm': // do map
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoMap(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'M':
                        foreach (var s in DoTransposeOrMaybe(block)) yield return s;
                        break;
                    case 'n': 
                        {
                            type = InstructionType.Value;
                            block.AddDesc("copy of 2nd value in stack");
                            dynamic top = Pop(), second = Peek();
                            Push(top);
                            Push(second);
                        }
                        break;
                    case 'N':
                        if (IsNumber(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "-" + e);
                            block.AddDesc("negate");
                            Push(-Pop()); 
                        }
                        else if (IsArray(Peek())) {
                            block.AddDesc("uncons-right; remove last element from array and push both");
                            RunMacro("c1TsH");
                        }
                        else throw new StaxException("Bad type for N");
                        break;
                    case 'o': // order
                        foreach (var s in DoOrder(block)) yield return s;
                        break;
                    case 'O':
                        block.AddDesc("push 1 under top element");
                        RunMacro("1s");
                        break;
                    case 'p':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "print " + e + " with no newline");
                        block.AddDesc("print with no newline");
                        Print(Pop(), false);
                        break;
                    case 'P':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "print " + e);
                        block.AddDesc("print");
                        Print(Pop());
                        break;
                    case 'q': 
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "peek print " + e + " with no newline");
                        block.AddDesc("peek print with no newline");
                        Print(Peek(), false);
                        break;
                    case 'Q':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "peek print " + e);
                        block.AddDesc("peek print");
                        Print(Peek());
                        break;
                    case 'r':
                        if (IsInt(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "0 ... " + e + "-1");
                            block.AddDesc("0 ... n");
                            Push(Range(0, Pop()));
                        }
                        else if (IsArray(Peek())) {
                            block.AddDesc("reverse");
                            var result = new List<object>(Pop());
                            result.Reverse();
                            Push(result);
                        }
                        else throw new StaxException("Bad type for r");
                        break;
                    case 'R':
                        if (IsInt(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "1 ... " + e);
                            block.AddDesc("1 ... n");
                            Push(Range(1, Pop()));
                        }
                        else {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "regex replace with " + e);
                            block.AddDesc("regex replace");
                            foreach (var s in DoRegexReplace()) yield return s;
                        }
                        break;
                    case 's': 
                        {
                            block.AddDesc("swap top two stack elements");
                            dynamic top = Pop(), bottom = Pop();
                            Push(top);
                            Push(bottom);
                        }
                        break;
                    case 'S':
                        DoPowerset(block);
                        break;
                    case 't': 
                        if (IsArray(Peek())) {
                            block.AddDesc("trim whitespace from left");
                            Push(S2A(A2S(Pop()).TrimStart()));
                        }
                        else if (IsInt(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "trim (remove) " + e + " from the left");
                            else block.AddDesc("trim (remove) n elements from the left");
                            RunMacro("ss~ c%,-0|M)");
                        }
                        else if (IsBlock(Peek())) {
                            Block pred = Pop();
                            List<object> result = new List<object>(Pop());
                            block.AddDesc("remove matching elements from beginning of array");
                            bool cancelled = false;
                            PushStackFrame();
                            while (result.Count > 0) {
                                Push(_ = result[0]);
                                foreach (var s in RunSteps(pred)) {
                                    if (cancelled = s.Cancel) break;
                                    yield return s;
                                }
                                if (cancelled || !IsTruthy(Pop())) break;
                                result.RemoveAt(0);
                                ++Index;
                            }
                            PopStackFrame();
                            Push(result);
                        }
                        else throw new StaxException("Bad types for trimleft");
                        break;
                    case 'T':
                        if (IsArray(Peek())) {
                            block.AddDesc("trim whitespace from right");
                            Push(S2A(A2S(Pop()).TrimEnd()));
                        }
                        else if (IsInt(Peek())) {
                            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "trim (remove) " + e + " from the right");
                            else block.AddDesc("trim (remove) n elements from the right");
                            RunMacro("ss~ c%,-0|M(");
                        }
                        else if (IsBlock(Peek())) {
                            Block pred = Pop();
                            List<object> result = new List<object>(Pop());
                            block.AddDesc("remove matching elements from end of array");
                            bool cancelled = false;
                            PushStackFrame();
                            while (result.Count > 0) {
                                Push(_ = result.Last());
                                foreach (var s in RunSteps(pred)) {
                                    if (cancelled = s.Cancel) break;
                                    yield return s;
                                }
                                if (cancelled || !IsTruthy(Pop())) break;
                                result.RemoveAt(result.Count - 1);
                                ++Index;
                            }
                            PopStackFrame();
                            Push(result);
                        }
                        else throw new StaxException("Bad types for trimright");
                        break;
                    case 'u': // unique
                        DoUnique(block);
                        break;
                    case 'U':
                        block.AddDesc("negative unit (-1)");
                        Push(BigInteger.MinusOne);
                        break;
                    case 'V': // constant value
                        Push(Constants[program[++ip]].Value);
                        block.AddDesc(Constants[program[ip]].Name);
                        break;
                    case 'w': // do-while
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoWhile(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'W':
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoPreCheckWhile(block, block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case '_':
                        {
                            type = InstructionType.Value;
                            if (_ is IteratorPair p) {
                                block.AddDesc("push outer and inner iteration values");
                                Push(p.Outer);
                                Push(p.Inner);
                            }
                            else if (CallStackFrames.Any()) {
                                block.AddDesc("current iteration value");
                                Push(_);
                            }
                            else {
                                block.AddDesc("entire standard input in one string");
                                Push(_);
                            }
                        }
                        break;
                    case 'x':
                        type = InstructionType.Value;
                        block.AddDesc("register x");
                        Push(X);
                        break;
                    case 'X': 
                        block.AddDesc("peek and store register x");
                        X = Peek();
                        break;
                    case 'y': 
                        type = InstructionType.Value;
                        block.AddDesc("register y");
                        Push(Y);
                        break;
                    case 'Y':
                        block.AddDesc("peek and store register y");
                        Y = Peek();
                        break;
                    case 'z': 
                        block.AddDesc("empty string/array");
                        type = InstructionType.Value;
                        Push(S2A(""));
                        break;
                    case 'Z':
                        block.AddDesc("push 0 under top element");
                        RunMacro("0s");
                        break;
                    case ':':
                        DoMacroAlias(block, program[++ip]);
                        break;
                    case '|': // extended operations
                        switch (program[++ip]) {
                            case ' ':
                                block.AddDesc("print single space; no newline");
                                Print(" ", false);
                                break;
                            case '`':
                                block.AddDesc("show debug state");
                                DoDump();
                                break;
                            case '%':
                                if (IsNumber(Peek())) {
                                    block.AddDesc("divmod; push a/b and a%b");
                                    RunMacro("ssb%~/,");
                                }
                                else if (IsArray(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "embed " + e + " in array at position");
                                    block.AddDesc("embed sub-array at position");
                                    dynamic c = Pop(), b = Pop(), a = Pop();
                                    int loc;

                                    List<object> result = new List<object>(a), payload;
                                    if (IsArray(c)) (payload, loc) = (c, (int)b);
                                    else (payload, loc) = (b, (int)c);

                                    if (loc < 0) {
                                        loc += result.Count;
                                        if (loc < 0) {
                                            result.InsertRange(0, Enumerable.Repeat(BigInteger.Zero as object, -loc));
                                            loc = 0;
                                        }
                                    }

                                    for (int i = 0; i < payload.Count; i++) {
                                        while (loc + i >= result.Count) result.Add(BigInteger.Zero);
                                        result[loc + i] = payload[i];
                                    }
                                    Push(result);
                                }
                                break;
                            case '+': 
                                block.AddDesc("sum of array");
                                RunMacro("Z{+F");
                                break;
                            case '-':
                                DoMultisetSubtract(block);
                                break;
                            case '!':
                                if (IsInt(Peek())) DoPartition(block);
                                else if (IsArray(Peek())) DoMultiAntiMode(block);
                                break;
                            case '@':
                                DoRemoveOrInsert(block);
                                break;
                            case '&':
                                if (IsArray(Peek())) {
                                    block.AddDesc("set intersection; keep all elements from left array that appear in right");
                                    List<object> b = Pop();
                                    var a = Pop();
                                    if (!IsArray(a)) a = new List<object> { a };
                                    var result = new List<object>();
                                    foreach (object e in a) {
                                        if (b.Contains(e, Comparer.Instance)) result.Add(e);
                                    }
                                    Push(result);
                                }
                                else {
                                    block.AddDesc("bitwise and");
                                    Push(Pop() & Pop()); 
                                }
                                break;
                            case '#':
                                if (IsArray(Peek())) {
                                    List<object> b = Pop(), a = Pop();
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "number of occurrences of " + e);
                                    else block.AddDesc("number of occurrences in array");
                                    Push(new BigInteger(a.Count(e => AreEqual(e, b))));
                                }
                                break;
                            case '|': 
                                block.AddDesc("bitwise or");
                                if (IsInt(Peek())) {
                                    Push(Pop() | Pop());
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("embed grid at co-ordinates");
                                    List<object> payload = Pop();
                                    int col = (int)Pop(), row = (int)Pop();
                                    var result = new List<object>(Pop());

                                    for (int r = 0; r < payload.Count; r++) {
                                        List<object> payline = IsArray(payload[r]) ? (List<object>)payload[r] : new List<object> { payload[r] };
                                        while (result.Count <= row + r) result.Add(new List<object>());
                                        if (!IsArray(result[row + r])) result[row + r] = new List<object> { result[row + r] };
                                        var resultline = (List<object>)result[row + r];

                                        for (int c = 0; c < payline.Count; c++) {
                                            while (resultline.Count <= col + c) resultline.Add(BigInteger.Zero);
                                            resultline[col + c] = payline[c];
                                        }
                                    }

                                    Push(result);
                                }
                                break;
                            case '^':
                                if (IsArray(Peek())) {
                                    block.AddDesc("symmetric array difference");
                                    RunMacro("s b-~ s-, +"); 
                                }
                                else if (IsInt(Peek())) {
                                    dynamic b = Pop();
                                    if (IsArray(Peek())) {
                                        block.AddDesc("tuples of specified size from array elements");
                                        List<object> els = Pop();

                                        var result = new List<object> { new List<object>() };
                                        for (int i = 0; i < b; i++) {
                                            // omg c# types get out of my way
                                            result = result
                                                .SelectMany(r => els.Select(e => ((List<object>)r).Concat(new[] { (object)e }).ToList() as object))
                                                .ToList();
                                        }
                                        Push(result);
                                    }
                                    else if (IsInt(Peek())) {
                                        block.AddDesc("bitwise xor");
                                        Push(Pop() ^ b);
                                    }
                                }
                                break;
                            case '*':
                                if (IsInt(Peek())) { 
                                    BigInteger b = Pop();
                                    if (IsInt(Peek())) { 
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "to the " + e + " power");
                                        else block.AddDesc("exponent");
                                        if (b < 0) Push(new Rational(1, BigInteger.Pow(Pop(), (int)-b)));
                                        else Push(BigInteger.Pow(Pop(), (int)b));
                                        break;
                                    }
                                    else if (IsFrac(Peek())) { 
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "fraction to the " + e + " power");
                                        else block.AddDesc("exponent");
                                        Rational a = Pop();
                                        if (b < 0) {
                                            b = -b;
                                            a = 1 / a;
                                        }
                                        var result = new Rational(1, 1);
                                        for (int i = 0; i < b; i++) result *= a;
                                        Push(result);
                                        break;
                                    }
                                    else if (IsArray(Peek())) {
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "repeat each element " + e + " times");
                                        else block.AddDesc("repeat each element n times");
                                        var result = new List<object>();
                                        foreach (var e in Pop()) result.AddRange(Enumerable.Repeat((object)e, (int)b));
                                        Push(result);
                                        break;
                                    }
                                    else {
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "to the " + e + " power");
                                        else block.AddDesc("exponent");
                                        Push(Math.Pow((double)Pop(), (double)b));
                                        break;
                                    }
                                }
                                else if (IsNumber(Peek())) {
                                    double b = (double)Pop(), a = (double)Pop();
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "to the " + e + " power");
                                    else block.AddDesc("exponent");
                                    Push(Math.Pow(a, b));
                                    break;
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("cross product; array of pairs");
                                    dynamic B = Pop(), A = Pop(); 
                                    var result = new List<object>();
                                    foreach (var a in A) foreach (var b in B) result.Add(new List<object> { a, b });
                                    Push(result);
                                    break;
                                }
                                throw new StaxException("Bad types for |*");
                            case '/': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "divide out " + e + " as many times as possible");
                                else block.AddDesc("divide by n until no longer a multiple");
                                RunMacro("ss~;*{;/c;%!w,d");
                                break;
                            case '\\':
                                if (IsArray(Peek())) {
                                    block.AddDesc("zip; truncate to shorter");
                                    RunMacro("b%s% |m~ ;(s,(s \\");
                                }
                                else {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "zip arrays using " + e + " for fill");
                                    else block.AddDesc("zip arrays using fill element");
                                    var fill = Pop();
                                    List<object> b = Pop(), a = Pop(), result = new List<object>();
                                    for (int i = 0; i < Math.Max(a.Count, b.Count); i++) {
                                        result.Add(new List<object> {
                                            a.ElementAtOrDefault(i) ?? fill,
                                            b.ElementAtOrDefault(i) ?? fill,
                                        });
                                    }
                                    Push(result);
                                }
                                break;
                            case ')': 
                                DoRotate(block, RotateDirection.Right);
                                break;
                            case '(': 
                                DoRotate(block, RotateDirection.Left);
                                break;
                            case '=':
                                if (IsArray(Peek())) {
                                    DoMultiMode(block);
                                }
                                break;
                            case '[': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "all prefixes of " + e);
                                else block.AddDesc("generate all prefixes");
                                RunMacro("~;%R{;s(m,d");
                                break;
                            case ']': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "all suffixes of " + e);
                                else block.AddDesc("generate all suffixes");
                                RunMacro("~;%R{;s)mr,d");
                                break;
                            case '<':
                                if (IsInt(Peek())) {
                                    block.AddDesc("bit shift left");
                                    RunMacro("|2*");
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("left-align lines");
                                    List<object> arr = Pop();
                                    int maxlen = 0;
                                    for (int i = 0; i < arr.Count; i++) {
                                        if (!IsArray(arr[i])) arr[i] = ToString(arr[i]);
                                        maxlen = Math.Max(maxlen, ((List<object>)arr[i]).Count);
                                    }
                                    var result = new List<object>();
                                    for (int i = 0; i < arr.Count; i++) {
                                        var line = new List<object>(Enumerable.Repeat((object)BigInteger.Zero, maxlen - ((List<object>)arr[i]).Count));
                                        line.InsertRange(0, (List<object>)arr[i]);
                                        result.Add(line);
                                    }
                                    Push(result);
                                }
                                break;
                            case '>':
                                if (IsInt(Peek())) {
                                    block.AddDesc("bit shift right");
                                    RunMacro("|2/"); 
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("right-align lines");
                                    List<object> arr = Pop();
                                    int maxlen = 0;
                                    for (int i = 0; i < arr.Count; i++) {
                                        if (!IsArray(arr[i])) arr[i] = ToString(arr[i]);
                                        maxlen = Math.Max(maxlen, ((List<object>)arr[i]).Count);
                                    }
                                    var result = new List<object>();
                                    for (int i = 0; i < arr.Count; i++) {
                                        var line = new List<object>(Enumerable.Repeat((object)BigInteger.Zero, maxlen - ((List<object>)arr[i]).Count));
                                        line.AddRange((List<object>)arr[i]);
                                        result.Add(line);
                                    }
                                    Push(result);
                                }
                                break;
                            case ';':
                                block.AddDesc("parity of iteration");
                                type = InstructionType.Value;
                                RunMacro("i2%");
                                break;
                            case '0':
                                if (IsArray(Peek())) {
                                    block.AddDesc("get index of first falsy element");
                                    BigInteger result = -1;
                                    int i = 0;
                                    foreach (var e in Pop()) {
                                        if (!IsTruthy(e)) {
                                            result = i;
                                            break;
                                        }
                                        ++i;
                                    }
                                    Push(result);
                                }
                                break;
                            case '1':
                                if (IsArray(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "get index of first truthy element in " + e);
                                    else block.AddDesc("get index of first truthy element");
                                    BigInteger result = -1;
                                    int i = 0;
                                    foreach (var e in Pop()) {
                                        if (IsTruthy(e)) {
                                            result = i;
                                            break;
                                        }
                                        ++i;
                                    }
                                    Push(result);
                                }
                                else if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "-1 to the power of " + e);
                                    else block.AddDesc("power of -1");
                                    RunMacro("2%U1?");
                                }
                                break;
                            case '2':
                                if (IsArray(Peek())) {
                                    block.AddDesc("diagonal of matrix");
                                    var result = new List<object>();
                                    int i = 0;
                                    foreach (var e in Pop()) {
                                        if (IsArray(e)) {
                                            if (e.Count > i) result.Add(e[i]);
                                            else result.Add(BigInteger.Zero);
                                        }
                                        else {
                                            result.Add(i == 0 ? e : BigInteger.Zero);
                                        }
                                        ++i;
                                    }
                                    Push(result);
                                }
                                else if (IsNumber(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "2 to the " + e);
                                    else block.AddDesc("power of 2");
                                    RunMacro("2s|*");
                                }
                                break;
                            case '3': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in base 36");
                                else block.AddDesc("base 36");
                                RunMacro("36|b");
                                break;
                            case '5':
                                block.AddDesc("0-indexed fibonacci number"); 
                                {
                                    BigInteger n = Pop(), a = 1, b = 1;
                                    for (int i = 0; i < n; i++) (a, b) = (b, a + b);
                                    Push(a);
                                }
                                break;
                            case '6':
                                block.AddDesc("0-indexed nth prime");
                                Push(PrimeHelper.AllPrimes().ElementAt((int)Pop()));
                                break;
                            case '7':
                                block.AddDesc("cosine in radians");
                                Push(Math.Cos((double)Pop()));
                                break;
                            case '8':
                                block.AddDesc("sine in radians");
                                Push(Math.Sin((double)Pop()));
                                break;
                            case '9':
                                block.AddDesc("tangent in radians");
                                Push(Math.Tan((double)Pop()));
                                break;
                            case 'a':
                                if (IsNumber(Peek())) {
                                    block.AddDesc("absolute value");
                                    if (IsInt(Peek())) Push(BigInteger.Abs(Pop()));
                                    else if (IsFloat(Peek())) Push(Math.Abs(Pop()));
                                    else if (IsFrac(Peek())) Push(((Rational)Pop()).AbsoluteValue());
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("any");
                                    BigInteger result = 0;
                                    foreach (var e in Pop()) {
                                        if (IsTruthy(e)) {
                                            result = 1;
                                            break;
                                        }
                                    }
                                    Push(result);
                                }
                                break;
                            case 'A':
                                if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "10 to the " + e);
                                    else block.AddDesc("power of 10");
                                    Push(BigInteger.Pow(10, (int)Pop()));
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("all");
                                    BigInteger result = 1;
                                    foreach (var e in Pop()) {
                                        if (!IsTruthy(e)) {
                                            result = 0;
                                            break;
                                        }
                                    }
                                    Push(result);
                                }
                                break;
                            case 'b':
                                if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "convert to base " + e);
                                    else block.AddDesc("convert base");
                                    DoBaseConvert();
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("keep the elements of a, no more than than their occurrences in b");
                                    List<object> b = Pop(), a = Pop(), result = new List<object>();
                                    foreach (var e in a) {
                                        for (int i = 0; i < b.Count; i++) {
                                            if (AreEqual(b[i], e)) {
                                                result.Add(e);
                                                b.RemoveAt(i);
                                                break;
                                            }
                                        }
                                    }
                                    Push(result);
                                }
                                break;
                            case 'B': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in binary");
                                else block.AddDesc("convert to binary");
                                RunMacro("2|b");
                                break;
                            case 'c':
                                block.AddDesc("contend; assert top of stack is truthy, don't pop");
                                if (!IsTruthy(Peek())) {
                                    Pop();
                                    yield return ExecutionState.CancelState;
                                    yield break;
                                }
                                break;
                            case 'C':
                                DoCenter(block);
                                break;
                            case 'd': 
                                block.AddDesc("depth of main stack");
                                Push(new BigInteger(MainStack.Count));
                                break;
                            case 'D': 
                                block.AddDesc("depth of main stack");
                                Push(new BigInteger(InputStack.Count));
                                break;
                            case 'e':
                                if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "is " + e + " even?");
                                    else block.AddDesc("is even?");
                                    Push(Pop() % 2 ^ 1);
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("string replace - first instance only");
                                    string to = A2S(Pop()), from = A2S(Pop()), original = A2S(Pop());
                                    var parts = original.Split(new[] { from }, 2, StringSplitOptions.None);
                                    Push(S2A(string.Join(to, parts)));
                                }
                                break;
                            case 'E':
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "generate array of digits in base " + e);
                                else block.AddDesc("generate array of digits in base n");
                                DoBaseConvert(false);
                                break;
                            case 'f':
                                if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "prime factorize " + e);
                                    else block.AddDesc("prime factorize");
                                    Push(PrimeFactors(Pop()));
                                }
                                else if (IsArray(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "regex find all " + e);
                                    else block.AddDesc("regex find all");
                                    DoRegexFind(); 
                                }
                                break;
                            case 'F':
                                if (IsInt(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "factorial of " + e);
                                    else block.AddDesc("factorial");
                                    var result = BigInteger.One;
                                    var n = Pop();
                                    for (int i = 1; i <= n; i++) result *= i;
                                    Push(result);
                                }
                                break;
                            case 'g':
                                block.AddDesc("greatest common denominator");
                                DoGCD();
                                break;
                            case 'H':
                                block.AddDesc("hexadecimal convert");
                                RunMacro("16|b");
                                break;
                            case 'i':
                                type = InstructionType.Value;
                                block.AddDesc("iteration index of outer loop");
                                Push(IndexOuter);
                                break;
                            case 'I':
                                block.AddDesc("find all indexes of");
                                foreach (var s in DoFindIndexAll()) yield return s;
                                break;
                            case 'J':
                                block.AddDesc("join with newlines");
                                RunMacro("Vn*");
                                break;
                            case 'l':
                                block.AddDesc("lowest common denominator");
                                if (IsArray(Peek())) RunMacro("1s{|lF");
                                else if (IsInt(Peek())) RunMacro("b|g~*,/");
                                else throw new StaxException("Bad type for lcm");
                                break;
                            case 'L': 
                                if (IsNumber(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "log base " + e);
                                    else block.AddDesc("log with base");
                                    double b = (double)Pop(), a = (double)Pop();
                                    Push(Math.Log(a, b));
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("combine elements from a and b, with each occurring the max of its occurrences from a and b");
                                    List<object> b = Pop(), a = Pop(), result = new List<object>();
                                    foreach (var e in a) {
                                        result.Add(e);
                                        for (int i = 0; i < b.Count; i++) {
                                            if (AreEqual(b[i], e)) {
                                                b.RemoveAt(i);
                                                break;
                                            }
                                        }
                                    }
                                    result.AddRange(b);
                                    Push(result);
                                }
                                break;
                            case 'm':
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "minimum of n and " + e);
                                else block.AddDesc("minimum of");
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) < 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("{|mk");
                                else throw new StaxException("Bad types for min");
                                break;
                            case 'M': // max
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "maximum of n and " + e);
                                else block.AddDesc("maximum of");
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) > 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("{|Mk");
                                else throw new StaxException("Bad types for max");
                                break;
                            case 'n': 
                                if (IsInt(Peek())) {
                                    block.AddDesc("exponenets of sequential primes in factorization");
                                    BigInteger target = BigInteger.Abs(Pop());
                                    
                                    var result = new List<object>();
                                    foreach (var p in PrimeHelper.AllPrimes()) {
                                        if (target <= 1) break;
                                        BigInteger exp = 0;
                                        while (target % p == 0) {
                                            target /= p;
                                            exp++;
                                        }
                                        result.Add(exp);
                                    }
                                    Push(result);
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("combine elements from a and b, removing common elements only as many times as they mutually occur");
                                    List<object> b = Pop(), a = Pop(), result = new List<object>();
                                    foreach (var e in a) {
                                        bool found = false;
                                        for (int i = 0; i < b.Count; i++) {
                                            if (AreEqual(b[i], e)) {
                                                found = true;
                                                b.RemoveAt(i);
                                                break;
                                            }
                                        }
                                        if (!found) result.Add(e);
                                    }
                                    result.AddRange(b);
                                    Push(result);
                                }
                                break;
                            case 'N':
                                if (IsArray(Peek())) {
                                    DoNextPerm(block);
                                }
                                break;
                            case 'o':
                                DoIndexWhenOrdered(block);
                                break;
                            case 'p':
                                if (IsInt(Peek())) {
                                    block.AddDesc("is prime?");
                                    RunMacro("|f%1=");
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("palindromize; drop last element, reverse, and concat to original");
                                    RunMacro("cr1t+");
                                }
                                break;
                            case 'P':
                                block.AddDesc("print blank newline");
                                Print("");
                                break;
                            case 'q':
                                if (IsNumber(Peek())) {
                                    block.AddDesc("floor square root");
                                    Push(new BigInteger(Math.Sqrt(Math.Abs((double)Pop()))));
                                }
                                else if (IsArray(Peek())) {
                                    block.AddDesc("get all indices of regex match");
                                    string pattern = A2S(Pop()), n = A2S(Pop());
                                    var result = Regex.Matches(n, pattern)
                                        .Cast<Match>()
                                        .Select(m => new BigInteger(m.Index) as object)
                                        .ToList();
                                    Push(result);
                                }
                                break;
                            case 'Q': 
                                if (IsNumber(Peek())) {
                                    block.AddDesc("square root");
                                    Push(Math.Sqrt(Math.Abs((double)Pop())));
                                }
                                else if (IsArray(Peek())) {
                                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "regex in " + e + " matches all of string");
                                    else block.AddDesc("regex matches all of string");
                                    List<object> b = Pop(), a = Pop();
                                    bool match = Regex.IsMatch(A2S(a), "^(" + A2S(b) + ")$");
                                    Push(match ? BigInteger.One : BigInteger.Zero);
                                }
                                break;
                            case 'r':
                                block.AddDesc("explicit range"); 
                                {
                                    dynamic end = Pop(), start = Pop();
                                    if (IsArray(end)) end = new BigInteger(end.Count);
                                    if (IsArray(start)) start = new BigInteger(-start.Count);
                                    Push(Range(start, end - start));
                                    break;
                                }
                            case 'R': 
                                if (IsInt(Peek())) { // start-end-stride range
                                    block.AddDesc("explicit range with stride");
                                    int stride = (int)Pop(), end = (int)Pop(), start = (int)Pop();
                                    Push(Enumerable.Range(0, end - start).Select(n => n * stride + start).TakeWhile(n => n < end).Select(n => new BigInteger(n) as object).ToList());
                                }
                                else if (IsArray(Peek())) { // RLE
                                    block.AddDesc("run length encode into [element count] pairs");
                                    Push(RunLength(Pop()));
                                }
                                break;
                            case 's': // regex split
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "regex split on " + e);
                                else block.AddDesc("regex split");
                                DoRegexSplit();
                                break;
                            case 'S': // surround with
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "surround with " + e + "; concat to start and end");
                                else block.AddDesc("surround with; concat to start and end");
                                DoSurround();
                                break;
                            case 't': // translate
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "translate using adjacent pairs in map string: " + e);
                                block.AddDesc("translate; replace using adjacent pairs in map string");
                                DoTranslate();
                                break;
                            case 'T':
                                DoPermutations(block);
                                break;
                            case 'w': // trim elements from start
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "remove all " + e + " from beginning of array");
                                else block.AddDesc("remove all instances from beginning of array");
                                DoTrimElementsStart(block);
                                break;
                            case 'W': // trim elements from end
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "remove all " + e + " from end of array");
                                else block.AddDesc("remove all instances from end of array");
                                DoTrimElementsEnd(block);
                                break;
                            case 'x': // decrement X, push
                                block.AddDesc("decrement x and push");
                                Push(--X);
                                break;
                            case 'X': // increment X, push
                                block.AddDesc("increment x and push");
                                Push(++X);
                                break;
                            case 'z': // zero-fill
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "zero-fill to " + e + " places");
                                else block.AddDesc("zero-fill");
                                RunMacro("ss ~; '0* s 2l$ ,)");
                                break;
                            case 'Z': // rectangularize using empty array
                                if (IsArray(Peek())) {
                                    block.AddDesc("rectangularize using empty array");
                                    List<object> arr = Pop();
                                    int maxlen = 0;
                                    for (int i = 0; i < arr.Count; i++) {
                                        if (!IsArray(arr[i])) arr[i] = ToString(arr[i]);
                                        maxlen = Math.Max(maxlen, ((List<object>)arr[i]).Count);
                                    }
                                    var result = new List<object>();
                                    for (int i = 0; i < arr.Count; i++) {
                                        var line = new List<object>(Enumerable.Repeat((object)new List<object>(), maxlen - ((List<object>)arr[i]).Count));
                                        line.InsertRange(0, (List<object>)arr[i]);
                                        result.Add(line);
                                    }
                                    Push(result);
                                }
                                break;

                            default: throw new StaxException($"Unknown extended character '{program[ip]}'");
                        }
                        break;
                    default: throw new StaxException($"Unknown character '{program[ip]}'");
                }
                block.LastInstrType = type;
                ++ip;
            }
            yield return new ExecutionState();
        }

        private IEnumerable<ExecutionState> DoFindFirst(Block block, bool reverse = false) {
            block.AddDesc("find first element matching predicate");

            Block pred = this.Pop();
            List<object> arr = this.Pop();
            if (reverse) arr.Reverse();

            PushStackFrame();
            foreach (var e in arr) {
                Push(_ = e);
                foreach (var s in RunSteps(pred)) {
                    if (s.Cancel) goto Cancel;
                    yield return s;
                }
                if (IsTruthy(Pop())) {
                    Push(e);
                    break;
                }

                Cancel:
                Index++;
            }
            PopStackFrame();
        }

        private IEnumerable<ExecutionState> DoExtremaBy(int direction, Block block) {
            block.AddDesc($"get elements that yield { (direction < 0 ? "minima" : "maxima") } when block is applied");

            Block project = Pop();
            List<object> arr = Pop(), result = new List<object>();
            object extreme = null;

            if (arr.Count == 0) {
                Push(arr);
                yield break;
            }

            PushStackFrame();
            foreach (var e in arr) {
                Push(_ = e);

                foreach (var s in RunSteps(project)) {
                    if (s.Cancel) goto Cancel;
                    yield return s;
                }

                var projected = Pop();
                if (extreme == null || Comparer.Instance.Compare(projected, extreme) * direction > 0) {
                    extreme = projected;
                    result.Clear();
                }
                if (AreEqual(projected, extreme)) result.Add(e);

                Cancel:
                ++Index;
            }
            PopStackFrame();

            Push(result);
        }

        private void DoMultiMode(Block block) {
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "get all modes of " + e);
            else block.AddDesc("get all tied modes of array");

            List<object> arr = Pop(), result = new List<object>();
            if (arr.Count > 0) {
                var multi = Multiset(arr);
                int max = multi.Values.Max();
                result.AddRange(multi.Where(kvp => kvp.Value == max).Select(kvp => kvp.Key));
                result.Sort();
            }
            Push(result);
        }

        private void DoMultiAntiMode(Block block) {
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "get all anti-modes of " + e);
            else block.AddDesc("get all tied anti-modes of array");

            List<object> arr = Pop(), result = new List<object>();
            if (arr.Count > 0) {
                var multi = Multiset(arr);
                int min = multi.Values.Min();
                result.AddRange(multi.Where(kvp => kvp.Value == min).Select(kvp => kvp.Key));
                result.Sort();
            }
            Push(result);
        }

        private IEnumerable<ExecutionState> DoCollect(Block block) {
            block.AddDesc("reduce and collect values");
            Block reduce = Pop();
            List<object> arr = Pop();

            if (arr.Count < 2) {
                Push(arr);
                yield break;
            }
            List<object> result = new List<object> { arr[0] };

            PushStackFrame();
            Push(arr[0]);
            foreach (var e in arr.Skip(1)) {
                Push(_ = e);
                foreach (var s in RunSteps(reduce)) yield return s;
                result.Add(Peek());
                Index++;
            }
            PopStackFrame();
            Pop();
            Push(result);
        }

        private void DoPartition(Block block) {
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "get partitions of size " + e);
            else block.AddDesc("get n-size partitions");

            int n = (int)Pop(), total;
            var arg = Pop();
            if (IsArray(arg)) total = arg.Count;
            else total = (int)arg;
            var list = arg as List<object>;

            var result = new List<object>();
            if (n > total) {
                Push(result);
                return;
            }

            var partition = Enumerable.Repeat(1, n - 1).ToList();
            partition.Add(total - n + 1);

            while (true) {
                if (list == null) {
                    result.Add(partition.Select(e => new BigInteger(e) as object).ToList());
                }
                else {
                    int added = 0;
                    var listpartition = new List<object>();
                    foreach (var psize in partition) {
                        listpartition.Add(list.Skip(added).Take(psize).ToList());
                        added += psize;
                    }
                    result.Add(listpartition);
                }

                int i;
                for (i = n - 1; i >= 0 && partition[i] == 1; i--) ;
                if (i <= 0) break;

                ++partition[i - 1];
                (partition[i], partition[n - 1]) = (partition[n - 1], --partition[i]);
            }

            Push(result);
        }

        private void DoNextPerm(Block block) {
            block.AddDesc("get next permutation of elements in lexicographic ordering");
            List<object> els = Pop(), result = new List<object>();
            els = new List<object>(els); // we need to mutate it, so copy

            int i = els.Count - 2;
            for (; i >= 0 && Comparer.Instance.Compare(els[i], els[i + 1]) >= 0; i--) ;
            if (i < 0) {
                result.AddRange(els);
                result.Reverse();
                Push(result);
                return;
            }

            result.AddRange(els.Take(i));
            els.RemoveRange(0, i);
            for (i = els.Count - 1; Comparer.Instance.Compare(els[i], els[0]) <= 0; i--) ;
            result.Add(els[i]);
            els.RemoveAt(i);
            els.Sort();
            result.AddRange(els);
            Push(result);
        }

        private void DoIndexWhenOrdered(Block block) {
            block.AddDesc("Get indices when ordered");
            List<object> a = Pop();
            var result = new object[a.Count];
            int i = 0;
            foreach (var t in Enumerable.Range(0, a.Count).OrderBy(j => a[j])) result[t] = i++;
            Push(result.ToList());
        }

        private void DoTrimElementsStart(Block block) {
            dynamic b = Pop();
            List<object> a = Pop(), bl = null;
            if (IsArray(b)) bl = b;

            int i = 0;
            for (; i < a.Count; i++) {
                if (bl != null) {
                    if (!bl.Contains(a[i], Comparer.Instance)) break;
                }
                else {
                    if (!AreEqual(a[i], b)) break;
                }
            }

            var result = new List<object>(a.Skip(i));
            Push(result);
        }

        private void DoTrimElementsEnd(Block block) {
            dynamic b = Pop();
            List<object> a = Pop(), bl = null;
            if (IsArray(b)) bl = b;

            int i = a.Count - 1;
            for (; i > 0; i--) {
                if (bl != null) {
                    if (!bl.Contains(a[i], Comparer.Instance)) break;
                }
                else {
                    if (!AreEqual(a[i], b)) break;
                }
            }

            var result = new List<object>(a.Take(i + 1));
            Push(result);
        }

        private void DoPermutations(Block block) {
            int targetSize = int.MaxValue;
            if (IsInt(Peek())) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "permutations of length " + e);
                else block.AddDesc("get permutations of specified size");
                targetSize = (int)Pop();
            }
            else {
                block.AddDesc("get all permutations");
            }
            List<object> els = Pop();
            targetSize = Math.Min(els.Count, targetSize);
            var result = new List<object>();

            // factoradic permutation decoder
            int totalPerms = 1, stride = 1;
            for (int i = 1; i <= els.Count; i++) totalPerms *= i;
            for (int i = 1; i <= els.Count - targetSize; i++) stride *= i;
            var idxs = new int[els.Count];
            for (int pi = 0; pi < totalPerms; pi += stride) {
                int n = pi;
                for (int i = 1; i <= els.Count; n /= i++) idxs[els.Count - i] = n % i;
                var dupe = new List<object>(els);
                result.Add(idxs.Take(targetSize).Select(i => {
                    try { return dupe[i]; } finally { dupe.RemoveAt(i); }
                }).ToList());
            }

            Push(result);
        }

        private void DoPowerset(Block block) {
            if (IsInt(Peek())) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "Combinations of length " + e);
                else block.AddDesc("Get all combinations of specified length");
                var len = (int)Pop();
                List<object> arr = Pop();
                var result = new List<object>();
                var idxs = Enumerable.Range(0, len).ToArray();
                while (len <= arr.Count) {
                    result.Add(idxs.Select(idx => arr[idx]).ToList());
                    int i;
                    for (i = len - 1; i >= 0 && idxs[i] == i + (arr.Count - len); i--) ;
                    if (i < 0) break;
                    idxs[i] += 1;
                    for (i++; i < len; i++) idxs[i] = idxs[i - 1] + 1;
                }
                Push(result);
            }
            else if (IsArray(Peek())) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "All combinations of " + e);
                else block.AddDesc("Get all combinations");

                List<object> arr = Pop();
                var result = new List<object>();
                foreach (var e in arr.AsEnumerable().Reverse()) {
                    var single = new List<object> { e };
                    result.AddRange(result.Select(r => single.Concat((List<object>)r).ToList()).ToList());
                    result.Add(single);
                }
                result.Reverse();
                Push(result);
            }
            else {
                throw new StaxException("Bad types for powerset");
            }
        }

        private void DoRemoveOrInsert(Block block) {
            dynamic b = Pop(), a = Pop();
            if (IsArray(a)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "remove at index " + e);
                else block.AddDesc("remove at index");
                var result = new List<object>(a);
                if (b < 0) b += result.Count;
                if (b >= 0 && b < result.Count) result.RemoveAt((int)b);
                Push(result);
            }
            else {
                dynamic arr = Pop();
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "insert " + e + "into array at index");
                else block.AddDesc("insert element at index");
                var result = new List<object>(arr);
                if (a < 0) a += result.Count;
                if (a < 0) {
                    result.InsertRange(0, Enumerable.Repeat((object)BigInteger.Zero, -(int)a));
                    result.Insert(0, b);
                }
                else if (a > result.Count) {
                    result.AddRange(Enumerable.Repeat((object)BigInteger.Zero, (int)a - result.Count));
                    result.Add(b);
                }
                else {
                    result.Insert((int)a, b);
                }
                Push(result);
            }
        }

        private void DoMultisetSubtract(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsArray(b)) {
                block.AddDesc("multiset subtraction");
            }
            else {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "remove first instance of " + e);
                else block.AddDesc("remove first instance of element");
                b = new List<object> { b };
            }

            var result = new List<object>();
            var bset = Multiset((List<object>)b);
            foreach (var e in a) {
                if (bset.ContainsKey(e)) {
                    bset[e] -= 1;
                    if (bset[e] == 0) bset.Remove(e);
                }
                else {
                    result.Add(e);
                }
            }
            Push(result);
        }

        private void DoCenter(Block block) {
            dynamic top = Pop();
            if (IsInt(top)) {
                if (IsArray(Peek())) {
                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "center in " + e + " spaces");
                    else block.AddDesc("center string in n spaces");
                    int size = (int)top;
                    var str = Pop();
                    var result = new List<object>(Enumerable.Repeat(BigInteger.Zero as object, (size - str.Count) / 2));
                    result.AddRange(str);
                    result.AddRange(Enumerable.Repeat(BigInteger.Zero as object, size - result.Count));
                    Push(result);
                }
                else if (IsInt(Peek())) {
                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "choose " + e);
                    else block.AddDesc("binomial coefficient");
                    BigInteger r = top, n = Pop(), result = 1;
                    if (n < 0 || r > n) result = 0;
                    for (int i = 0; i < r; i++) {
                        result *= n - i;
                        result /= i + 1;
                    }
                    Push(result);
                }
            }
            else if (IsArray(top)) {
                block.AddDesc("center lines");
                int maxLen = 0;
                foreach (var line in top) maxLen = Math.Max(maxLen, line.Count);
                var result = new List<object>();
                foreach (var line in top) {
                    var newLine = new List<object>(line);
                    newLine.InsertRange(0, Enumerable.Repeat(BigInteger.Zero as object, (maxLen - newLine.Count) / 2));
                    newLine.AddRange(Enumerable.Repeat(BigInteger.Zero as object, maxLen - newLine.Count));
                    result.Add(newLine);
                }
                Push(result);
            }
        }

        private void DoMacroAlias(Block block, char alias) {
            var typeTree = MacroTree.GetMacroTree(alias);
            var resPopped = new Stack<object>();
            // follow type tree as far as necessary
            while (typeTree.HasChildren) {
                resPopped.Push(Pop());
                char type = MacroTree.GetTypeChar(resPopped.Peek());
                typeTree = typeTree.Children[type];
            }
            // return inspected values to stack
            while (resPopped.Count > 0) Push(resPopped.Pop());

            block.AddDesc(typeTree.Description);
            // disable line modes
            RunMacro(' ' + typeTree.Code);
        }

        private void DoOverlappingBatch(Block block) {
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "overlapping batches of " + e);
            else block.AddDesc("get overlapping batches of specified length");

            int b = (int)Pop();
            List<object> a = Pop();
            var result = new List<object>();

            for (int i = 0; i < a.Count - b + 1; i++) {
                result.Add(a.Skip(i).Take(b).ToList());
            }

            Push(result);
        }

        private void DoSurround() {
            dynamic b = Pop(), a = Pop();

            if (!IsArray(b)) b = new List<object> { b };
            if (!IsArray(a)) a = new List<object> { a };
            var result = new List<object>(b);
            result.AddRange(a);
            result.AddRange(b);
            Push(result);
        }

        private IEnumerable<ExecutionState> DoGenerator(Block block, bool shorthand, char spec, Block rest) {
            char lowerSpec = char.ToLower(spec);
            bool stopOnDupe = lowerSpec == 'u' || lowerSpec == 'l',
                stopOnFilter = lowerSpec == 'f',
                stopOnCancel = lowerSpec == 'c',
                stopOnFixPoint = lowerSpec == 'i' || lowerSpec == 'p',
                stopOnTargetVal = lowerSpec == 't',
                scalarMode = lowerSpec == 's' || lowerSpec == 'e' || lowerSpec == 'p',
                keepOnlyLoop = lowerSpec == 'l',
                postPop = char.IsUpper(spec);
            Block genblock = shorthand ? rest : Pop();
            Block filter = null;
            dynamic targetVal = null;
            int? targetCount = null;

            if (IsBlock(Peek())) filter = Pop();
            else if (stopOnFilter) throw new StaxException("generator can't stop on filter failure when there is no filter");

            if (stopOnTargetVal) targetVal = Pop();

            bool hardCodedTargetCount = false;
            if (lowerSpec == 'n') targetCount = (int)Pop();
            else if (lowerSpec == 'e') targetCount = (int)Pop() + 1;
            else if (lowerSpec == 's') {
                targetCount = 1;
                hardCodedTargetCount = true;
            }
            else {
                int idx = "1234567890!@#$%^&*()".IndexOf(spec);
                if (idx >= 0) {
                    targetCount = idx % 10 + 1;
                    postPop = idx >= 10;
                    hardCodedTargetCount = true;
                }
            }

            if (!stopOnDupe && !stopOnFilter && !stopOnCancel && !stopOnFixPoint && !stopOnTargetVal && !targetCount.HasValue) {
                throw new StaxException("no end condition for generator");
            }

            string targetCountClause = 
                targetCount.HasValue  
                    ? hardCodedTargetCount 
                        ? "until " + targetCount + " element" + (targetCount == 1 ? " is" : "s are") + " found, " 
                        : "until specified number of elements are found, " 
                    : "";

            block.AddDesc(
                (scalarMode ? "generate values, keeping only the last, " : "generate and collect values ")
                + (filter != null ? "matching filter " : "")
                + (shorthand ? "from rest of program " : "")
                + (stopOnDupe ? "until a duplicate is found, " : "")
                + (keepOnlyLoop ? "keeping only the looped portion, " : "")
                + (stopOnFilter ? "until a value fails the filter, ": "")
                + (stopOnCancel ? "until cancelled, " : "")
                + (stopOnFixPoint ? "until the same value appears successively, " : "")
                + (stopOnTargetVal ? "until specified target value, " : "")
                + targetCountClause
                + (postPop ? "popping each value" : "including the initial value"));
            if (genblock.Contents == "") block.AddAmbient("generator block is empty; using increment");


            if (targetCount == 0) { // 0 elements requested ??
                Push(new List<object>()); 
                yield break;
            }

            PushStackFrame();
            var result = new List<object>();

            object lastGenerated = null;
            while (targetCount == null || result.Count < targetCount) {
                _ = Peek();

                if (Index > 0 || postPop) {
                    if (genblock.Contents != "") {
                        foreach (var s in RunSteps(genblock)) {
                            if (s.Cancel && stopOnCancel) goto GenComplete;
                            if (s.Cancel) goto Cancelled;
                            yield return s;
                        }
                    }
                    else { // empty gen block, use (^)
                        RunMacro("^");
                    }
                }
                object generated = Peek();

                bool passed = true;
                if (filter != null) {
                    _ = generated;
                    foreach (var s in RunSteps(filter)) {
                        if (s.Cancel && stopOnCancel) goto GenComplete;
                        if (s.Cancel) goto Cancelled;
                        yield return s;
                    }
                    passed = IsTruthy(Pop());
                    Push(generated); // put the generated element back
                    if (stopOnFilter && !passed) break;
                }

                if (postPop) Pop();
                if (passed) {
                    // dupe
                    if (stopOnDupe && result.Contains(generated, Comparer.Instance)) {
                        while (keepOnlyLoop && !AreEqual(result[0], generated)) result.RemoveAt(0);
                        break;
                    }
                    // successive equal values
                    if (stopOnFixPoint && AreEqual(generated, lastGenerated)) break;
                    result.Add(generated);
                    // got to target val
                    if (stopOnTargetVal && AreEqual(generated, targetVal)) break;
                }
                lastGenerated = generated;

                Cancelled:
                ++Index;
            }
            if (!postPop) {
                // Remove left-over value from pre-peek mode
                // It's kept on stack between iterations, but iterations are over now
                Pop(); 
            }

            GenComplete:
            PopStackFrame();

            if (shorthand) {
                if (scalarMode) Print(result.Last());
                else foreach (var e in result) Print(e);
            }
            else {
                if (scalarMode) Push(result.Last());
                else Push(result);
            }
        }

        enum RotateDirection { Left, Right };
        private void DoRotate(Block block, RotateDirection dir) {
            dynamic arr, distance = Pop();
            if (IsArray(distance)) {
                arr = distance;
                distance = BigInteger.One;
                block.AddDesc("rotate one position " + dir.ToString().ToLower());
            }
            else {
                arr = Pop();
            }
            
            if (IsArray(arr) && IsInt(distance)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "rotate " + e +" position " + dir.ToString().ToLower());
                else block.AddDesc("rotate n positions " + dir.ToString().ToLower());
                var result = new List<object>();
                distance = distance % arr.Count;
                if (distance < 0) distance += arr.Count;
                int cutPoint = dir == RotateDirection.Left ? (int)distance : arr.Count - (int)distance;
                for (int i = 0; i < arr.Count; i++) {
                    result.Add(arr[(i + cutPoint) % arr.Count]);
                }
                Push(result);
            }
            else {
                throw new StaxException("Bad types for rotate");
            }
        }

        private void DoListify() {
            var newList = new List<object>();
            while (TotalStackSize > 0) newList.Add(Pop());
            Push(newList);
        }

        private void DoListifyN(Block block) {
            var n = Pop();

            if (IsFrac(n)) {
                block.AddDesc("make a pair containing numerator and denominator");
                Push(new List<object> { n.Num, n.Den });
            }
            else if (IsInt(n)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "make array from top " + e + " values on stack");
                block.AddDesc("make array from top n values on stack");
                var result = new List<object>();
                for (int i = 0; i < n; i++) result.Insert(0, Pop());
                Push(result);
            }
            else {
                throw new StaxException("bad type for listify n");
            }
        }

        private void DoZipRepeat(Block block) {
            dynamic b = Pop(), a = Pop();

            if (!IsArray(a) && !IsArray(b)) {
                block.AddDesc("make array with last two values");
                Push(new List<object> { a, b });
                return;
            }

            if (!IsArray(a)) {
                if (b.Count == 0) a = new List<object>();
                else a = new List<object> { a };
            }
            if (!IsArray(b)) {
                if (a.Count == 0) b = new List<object>();
                else b = new List<object> { b };
            }

            block.AddDesc("zip two arrays; non-arrays are wrapped, and the shorter one is repeated");
            var result = new List<object>();
            int size = Math.Max(a.Count, b.Count);
            for (int i = 0; i < size; i++) {
                result.Add(new List<object> { a[i%a.Count], b[i%b.Count] });
            }
            Push(result);
        }

        // not an eval of stax code, but a json-like data parse
        private bool DoEval() {
            string arg = A2S(Pop());
            var activeArrays = new Stack<List<object>>();

            void NewValue(object val) {
                if (activeArrays.Count > 0) {
                    activeArrays.Peek().Add(val);
                }
                else {
                    Push(val);
                }
            }

            for (int i = 0; i < arg.Length; i++) {
                switch (arg[i]) {
                    case '[':
                        activeArrays.Push(new List<object>());
                        break;
                    case ']':
                        if (activeArrays.Count == 0) return false;
                        NewValue(activeArrays.Pop());
                        break;
                    case '"':
                        int finishPos = arg.IndexOf('"', i+1);
                        NewValue(S2A(arg.Substring(i + 1, finishPos - i - 1)));
                        i = finishPos;
                        break;
                    case '-':
                    case '0': case '1': case '2': case '3': case '4':
                    case '5': case '6': case '7': case '8': case '9':
                        var substring = arg.Substring(i);
                        var match = Regex.Match(substring, @"^-?\d+\.\d+");
                        if (match.Success) {
                            NewValue(double.Parse(match.Value));
                            i += match.Value.Length - 1;
                            break;
                        }

                        match = Regex.Match(substring, @"^(-?\d+)/(-?\d+)");
                        if (match.Success) {
                            var frac = new Rational(
                                BigInteger.Parse(match.Groups[1].Value),
                                BigInteger.Parse(match.Groups[2].Value));
                            NewValue(frac);
                            i += match.Value.Length - 1;
                            break;
                        }

                        match = Regex.Match(substring, @"^-?\d+");
                        if (match.Success) {
                            NewValue(BigInteger.Parse(match.Value));
                            i += match.Value.Length - 1;
                            break;
                        }
                        return false;

                    case ' ': case '\t': case '\r': case '\n': case ',':
                        break;
                    default: return false;
                }
            }
            return true;
        }

        private void DoRegexFind() {
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new StaxException("Bad types for find");
            string ts = A2S(text), ss = A2S(search);

            var result = new List<object>();
            foreach (Match m in Regex.Matches(ts, ss)) result.Add(S2A(m.Value));
            Push(result);
        }

        private void DoRegexSplit() {
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new StaxException("Bad types for replace");
            string ts = A2S(text), ss = A2S(search);

            Push(Regex.Split(ts, ss, RegexOptions.ECMAScript).Select(S2A).Cast<object>().ToList());
        }

        private IEnumerable<ExecutionState> DoRegexReplace() {
            var replace = Pop();
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new StaxException("Bad types for replace");
            string ts = A2S(text), ss = A2S(search);

            if (IsArray(replace)) {
                Push(S2A(Regex.Replace(ts, ss, A2S(replace))));
            }
            else if (IsBlock(replace)) {
                PushStackFrame();
                string result = "";
                var matches = Regex.Matches(ts, ss);
                int consumed = 0;
                foreach (Match match in matches) {
                    result += ts.Substring(consumed, match.Index - consumed);
                    Push(_ = S2A(match.Value));
                    foreach (var s in RunSteps((Block)replace)) yield return s;
                    Index++;
                    result += A2S(Pop());
                    consumed = match.Index + match.Length;
                }
                result += ts.Substring(consumed);
                Push(S2A(result));
                PopStackFrame();
                yield break;
            }
            else {
                throw new StaxException("Bad types for replace");
            }
        }

        private void DoTranslate() {
            var translation = Pop();
            var input = Pop();

            if (IsInt(input)) input = new List<object> { input };

            if (IsArray(input) && IsArray(translation)) {
                var result = new List<object>();
                var map = new Dictionary<BigInteger, BigInteger>();

                for (int i = 0; i < translation.Count; i += 2) map[translation[i]] = translation[i + 1];
                foreach (var e in input) result.Add(map.ContainsKey(e) ? map[e] : e);
                Push(result);
            }
            else {
                throw new StaxException("Bad types for translate");
            }
        }

        private void DoDump() {
            int i = 0;
            if (CallStackFrames.Any()) Output.WriteLine("i: {0}, _: {1}", Index, Format(_));
            Output.WriteLine("x: {0} y: {1}", Format(X), Format(Y));
            if (MainStack.Any()) {
                Output.WriteLine("Main:");
                foreach (var e in MainStack) Output.WriteLine("{0:##0}: {1}", i++, Format(e)); 
            }
            if (InputStack.Any()) {
                Output.WriteLine("Input:");
                foreach (var e in InputStack) Output.WriteLine("{0:##0}: {1}", i++, Format(e));
            }
            Output.WriteLine();
        }

        private void DoUnique(Block block) {
            var arg = Pop();

            if (IsArray(arg)) {
                block.AddDesc("eliminate duplicate elements; keep in order of first occurrence");
                var result = new List<object>();
                var seen = new HashSet<object>(Comparer.Instance);
                foreach (var e in arg) {
                    var key = IsArray(e) ? A2S(e) : e;
                    if (!seen.Contains(key)) {
                        result.Add(e);
                        seen.Add(key);
                    }
                }
                Push(result);
            }
            else if (IsInt(arg)) { // upside down
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "1/" + e);
                else block.AddDesc("1/n");
                Push(new Rational(1, arg));
            }
            else if (IsFrac(arg)) { // upside down
                block.AddDesc("invert fraction");
                Push(1 / arg);
            }
            else if (IsFloat(arg)) { // invert
                block.AddDesc("invert float");
                Push(1.0 / arg);
            }
            else {
                throw new StaxException("Bad type for unique");
            }
        }

        private void DoLessThan(Block block) {
            dynamic b = Pop(), a = Pop();
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "less than " + e);
            block.AddDesc("less than");
            Push(Comparer.Instance.Compare(a, b) < 0 ? BigInteger.One : BigInteger.Zero);
        }

        private void DoGreaterThan(Block block) {
            dynamic b = Pop(), a = Pop();
            if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "greater than " + e);
            block.AddDesc("greater than");
            Push(Comparer.Instance.Compare(a, b) > 0 ? BigInteger.One : BigInteger.Zero);
        }

        private IEnumerable<ExecutionState> DoOrder(Block block) {
            var arg = Pop();

            if (IsArray(arg)) {
                block.AddDesc("sort array");
                var result = new List<object>(arg);
                result.Sort(Comparer.Instance);
                Push(result);
            }
            else if (IsBlock(arg)) {
                block.AddDesc("sort array using projection");
                var list = Pop();
                var combined = new List<(object val, IComparable key)>();

                PushStackFrame();
                foreach (var e in list) {
                    _ = e;
                    Push(e);
                    foreach (var s in RunSteps((Block)arg)) yield return s;
                    combined.Add((e, Pop()));
                    ++Index;
                }
                PopStackFrame();

                Push(combined.OrderBy(e => e.key).Select(e => e.val).ToList());
            }
            else {
                throw new StaxException("Bad types for order");
            }
        }

        private void DoBaseConvert(bool stringRepresentation = true) {
            int @base = (int)Pop();
            var number = Pop();

            if (IsInt(number)) {
                var result = new List<object>();
                if (@base == 1) result.AddRange(Enumerable.Repeat(BigInteger.Zero, number));
                else do {
                    BigInteger digit = number % @base;
                    if (stringRepresentation) {
                        char d = "0123456789abcdefghijklmnopqrstuvwxyz"[(int)digit];
                        result.Insert(0, new BigInteger(d + 0));
                    }
                    else { //digit mode
                        result.Insert(0, digit);
                    }
                    number /= @base;
                } while (number > 0);

                Push(result);
            }
            else if (IsArray(number)) {
                BigInteger result = 0;
                if (stringRepresentation) {
                    string s = A2S(number).ToLower();
                    foreach (var c in s) {
                        int digit = "0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(c);
                        if (digit < 0) digit = c + 0;
                        result = result * @base + digit;
                    }
                }
                else {
                    foreach (var d in number) result = result * @base + d;
                }
                Push(result);
            }
            else {
                throw new StaxException("Bad types for base convert");
            }
        }

        private IEnumerable<ExecutionState> DoFindIndexAll() {
            dynamic target = Pop(), list = Pop();
            if (!IsArray(list)) throw new StaxException("Bad types for find index all");

            if (IsArray(target)) {
                string text = A2S(list), search = A2S(target);
                var result = new List<object>();
                int lastFound = -1;
                while ((lastFound = text.IndexOf(search, lastFound + 1)) >= 0) {
                    result.Add(new BigInteger(lastFound));
                }
                Push(result);
            }
            else if (IsBlock(target)) {
                PushStackFrame();
                var result = new List<object>();
                for (Index = 0; Index < list.Count; Index++) {
                    Push(_ = list[(int)Index]);
                    foreach (var s in RunSteps((Block)target)) yield return s;
                    if (IsTruthy(Pop())) result.Add(Index);
                }
                PopStackFrame();
                Push(result);
            }
            else {
                var result = new List<object>();
                for (int i = 0; i < list.Count; i++) {
                    if (AreEqual(list[(int)i], target)) result.Add(new BigInteger(i));
                }
                Push(result);
            }
        }

        private IEnumerable<ExecutionState> DoFindIndex() {
            dynamic element = Pop(), list = Pop();

            if (!IsArray(list)) (list, element) = (element, list);

            if (IsArray(list)) {
                for (int i = 0; i < list.Count; i++) {
                    if (IsArray(element)) {
                        if (i + element.Count > list.Count) {
                            Push(BigInteger.MinusOne);
                            yield break;
                        }
                        bool match = true;
                        for (int j = 0; j < element.Count; j++) {
                            if (!AreEqual(list[i + j], element[j])) {
                                match = false;
                                break;
                            }
                        }
                        if (match) {
                            Push((BigInteger)i);
                            yield break;
                        }
                    }
                    else if (IsBlock(element)) {
                        PushStackFrame();
                        Push(_ = list[i]);
                        Index = i;
                        foreach (var s in RunSteps((Block)element)) {
                            yield return s;
                            if (s.Cancel) goto Cancel;
                        }
                        if (IsTruthy(Pop())) {
                            Push((BigInteger)i);
                            PopStackFrame();
                            yield break;
                        }
                        Cancel:
                        PopStackFrame();
                    }
                    else if (AreEqual(element, list[i])) {
                        Push((BigInteger)i);
                        yield break;
                    }
                }
                Push(BigInteger.MinusOne);
                yield break;
            }
            else {
                throw new StaxException("Bad types for get-index");
            }
        }
        
        private void DoExplode(Block block) {
            var arg = Pop();

            if (IsArray(arg)) {
                block.AddDesc("explode array; push all items to stack individually");
                foreach (var item in arg) Push(item);
            }
            else if (IsFrac(arg)) {
                block.AddDesc("push numerator and denominator separately");
                Push(arg.Num);
                Push(arg.Den);
            }
            else if (IsInt(arg)) {
                block.AddDesc("produce array of decimal digits");
                var result = new List<object>();
                foreach (var c in (string)(BigInteger.Abs(arg).ToString())) {
                    result.Add(new BigInteger(c - '0'));
                }
                Push(result);
            }
        }

        private void DoAssignIndex(Block block) {
            dynamic element = Pop(), indexes = Pop(), list;

            if (IsInt(indexes)) {
                indexes = new List<object> { indexes };
                if (IsInt(Peek())) {
                    indexes = new List<object> { indexes };
                    while (IsInt(Peek())) indexes[0].Insert(0, Pop());
                }
                if (IsBlock(element)) block.AddDesc("modify array element at index");
                else if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "assign " + e + " at index to array");
                else block.AddDesc("assign element at index to array");
            }
            else {
                if (IsBlock(element)) block.AddDesc("modify array elements at indices");
                else if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "assign " + e + " to array at all indices");
                else block.AddDesc("assign element to array at all indices");
            }
            list = Pop();

            void DoFinalAssign(List<object> flatArr, int index) {
                if (index + 1 > flatArr.Count) {
                    flatArr.AddRange(Enumerable.Repeat((object)BigInteger.Zero, index + 1 - flatArr.Count));
                }

                if (IsBlock(element)) {
                    Push(flatArr[index]);
                    bool cancelled = false;
                    foreach (var s in RunSteps((Block)element)) cancelled = s.Cancel;
                    if (!cancelled) flatArr[index] = Pop();
                }
                else {
                    flatArr[index] = element;
                }
            }

            if (IsArray(list)) {
                var result = new List<object>(list);
                foreach (dynamic arg in indexes) {
                    if (IsArray(arg)) {
                        List<object> idxPath = arg, target = result;
                        int idx;
                        for (int i = 0; i < idxPath.Count - 1; i++) {
                            idx = (int)(BigInteger)idxPath[i];
                            while (target.Count <= idx) target.Add(new List<object>());
                            if (!IsArray(target[idx])) target[idx] = new List<object> { target[idx] };
                            target = (List<object>)target[idx];
                        }
                        idx = (int)(BigInteger)idxPath[idxPath.Count - 1];
                        DoFinalAssign(target, idx);
                    }
                    else if (IsInt(arg)) {
                        int index = (int)arg;
                        if (index < 0) {
                            index += result.Count;
                            if (index < 0) {
                                result.InsertRange(0, Enumerable.Repeat((object)BigInteger.Zero, -index));
                                index = 0;
                            }
                        }

                        DoFinalAssign(result, index);
                    }
                }
                Push(result);
            }
            else {
                throw new StaxException("Bad type for index assign");
            }

        }

        private void DoAt(Block block) {
            var top = Pop();

            if (IsFrac(top)) { // floor
                block.AddDesc("floor fraction to integer");
                Push(top.Floor());
                return;
            }

            if (IsFloat(top)) { // floor
                block.AddDesc("floor floating point to integer");
                Push(new BigInteger(Math.Floor(top)));
                return;
            }

            var list = Pop();

            dynamic ReadAt(List<object> arr, int idx) {
                idx %= arr.Count;
                if (idx < 0) idx += arr.Count;
                return arr[idx];
            }

            // read at index
            if (IsInt(list) && IsArray(top)) (list, top) = (top, list);
            if (IsArray(list)) {
                if (IsArray(top)) {
                    block.AddDesc("get elements at all indices");
                    var result = new List<object>();
                    foreach (var idx in top) result.Add(ReadAt(list, (int)idx));
                    Push(result);
                    return;
                }
                else if (IsInt(top)) {
                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "get element at index " + e);
                    else block.AddDesc("get element at index");
                    Push(ReadAt(list, (int)top));
                    return;
                }
            }
            throw new StaxException("Bad type for at");
        }

        private void DoPadLeft(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsArray(b) && IsInt(a)) (a, b) = (b, a);
            if (IsInt(a)) a = ToString(a);

            if (IsArray(a) && IsInt(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "left pad/truncate to " + e);
                else block.AddDesc("left pad/truncate with spaces");
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.InsertRange(0, Enumerable.Repeat(BigInteger.Zero as object, (int)b - a.Count));
                if (a.Count > b) a.RemoveRange(0, a.Count - (int)b);
                Push(a);
            }
            else if (IsArray(a) && IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "right-align string inside " + e);
                else block.AddDesc("right-align string inside string");
                var result = new List<object>();
                for (int i = 0; i < b.Count; i++) {
                    result.Add(a.Count - b.Count + i >= 0 ? a[a.Count - b.Count + i] : b[i]);
                }
                Push(result);
            }
            else {
                throw new StaxException("bad types for padleft");
            }
        }

        private IEnumerable<ExecutionState> DoPadRight(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsArray(b) && IsInt(a)) (a, b) = (b, a);
            if (IsInt(a)) a = ToString(a);

            if (IsArray(a) && IsInt(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "right pad/truncate to " + e);
                else block.AddDesc("right pad/truncate with spaces");
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.AddRange(Enumerable.Repeat(BigInteger.Zero as object, (int)b - a.Count));
                if (a.Count > b) a.RemoveRange((int)b, a.Count - (int)b);
                Push(a);
            }
            else if (IsArray(a) && IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "left-align string inside " + e);
                else block.AddDesc("left-align string inside string");
                var result = new List<object>();
                for (int i = 0; i < b.Count; i++) {
                    result.Add(i < a.Count ? a[i] : b[i]);
                }
                Push(result);
            }
            else if (IsArray(a) && IsBlock(b)) {
                List<object> result = new List<object>(), current = null;

                PushStackFrame();
                foreach (var e in a) {
                    Push(_ = e);

                    foreach (var s in RunSteps((Block)b)) {
                        if (s.Cancel) goto Cancel;
                        yield return s;
                    }

                    if (IsTruthy(Pop()) || current == null) {
                        if (current != null) result.Add(current);
                        current = new List<object>();
                    }
                    current.Add(e);

                    Cancel:
                    ++Index;
                }
                PopStackFrame();

                if (current != null) result.Add(current);
                Push(result);
            }
            else {
                throw new StaxException("bad types for padright");
            }
        }

        private IEnumerable<ExecutionState> DoPreCheckWhile(Block block, Block rest) {
            if (!IsBlock(Peek())) {
                block.AddDesc("loop rest of program until cancelled");
                PushStackFrame();
                while (true) {
                    foreach (var s in RunSteps(rest)) {
                        if (s.Cancel) {
                            PopStackFrame();
                            yield break;
                        }
                        yield return s;
                    }
                    ++Index;
                }
            }

            Block whileBlock = Pop();
            block.AddDesc("loop until cancelled");
            PushStackFrame();
                
            while (true) {
                foreach (var s in RunSteps(whileBlock)) {
                    if (s.Cancel) {
                        PopStackFrame();
                        yield break;
                    }
                    yield return s;
                }
                ++Index;
            }
        }

        private IEnumerable<ExecutionState> DoWhile(Block block, Block rest) {
            if (!IsBlock(Peek())) {
                block.AddDesc("while loop rest of program; pop condition at end");
                PushStackFrame();
                do {
                    foreach (var s in RunSteps(rest)) {
                        if (s.Cancel) {
                            PopStackFrame();
                            yield break;
                        }
                        yield return s;
                    }
                    ++Index;
                } while (IsTruthy(Pop()));
                PopStackFrame();
                yield break;
            }

            Block whileBlock = Pop();
            block.AddDesc("while loop; pop condition at end");
            PushStackFrame();
            do {
                foreach (var s in RunSteps(whileBlock)) {
                    if (s.Cancel) {
                        PopStackFrame();
                        yield break;
                    }
                    yield return s;
                }
                ++Index;
            } while (IsTruthy(Pop()));
        }

        private IEnumerable<ExecutionState> DoIf() {
            dynamic @else = Pop(), then = Pop(), condition = Pop();
            Push(IsTruthy(condition) ? then : @else);
            if (IsBlock(Peek())) {
                foreach (var s in RunSteps((Block)Pop())) yield return s;
            }
        }

        private void Print(object arg, bool newline = true) {
            OutputWritten = true;
            if (IsArray(arg)) {
                Print(A2S((List<object>)arg).Replace("\n", Environment.NewLine), newline);
                return;
            }

            if (newline) Output.WriteLine(arg);
            else Output.Write(arg);
        }

        private IEnumerable<ExecutionState> DoFilter(Block block, Block rest) {
            dynamic top = Pop(), a;
            bool shorthand = !(top is Block);
            Block pred;
            if (shorthand) {
                pred = rest;
                a = top;
            }
            else {
                pred = top;
                a = Pop();
            }

            if (IsInt(a)) a = Range(1, a);

            if (IsArray(a)) {
                if (shorthand) block.AddDesc("treat rest of program as filter and print the result");
                block.AddDesc("filter array by block");
                PushStackFrame();
                var result = new List<object>();
                foreach (var e in a) {
                    Push(_ = e);
                    foreach (var s in RunSteps(pred)) {
                        if (s.Cancel) goto Cancel;
                        yield return s;
                    }
                    if (IsTruthy(Pop())) {
                        if (shorthand) Print(e);
                        else result.Add(e);
                    }
                    Cancel:
                    Index++;
                }
                if (!shorthand) Push(result);
                PopStackFrame();
            }
            else {
                throw new StaxException("Bad types for filter");
            }
        }

        private IEnumerable<ExecutionState> DoReduce(Block block, Block rest) {
            bool shorthand = !IsBlock(this.Peek());
            Block combine = shorthand ? rest : this.Pop();
            dynamic a = Pop();
            if (IsInt(a)) {
                block.AddDesc("reduce range 1 to n using block");
                a = Range(1, a);
            }
            else {
                block.AddDesc("reduce using block");
                a = new List<object>(a);
            }
            if (IsArray(a)) {
                if (a.Count < 2) {
                    Push(a[0]);
                    yield break;
                }

                PushStackFrame();
                Push(a[0]);
                a.RemoveAt(0);
                foreach (var e in a) {
                    Push(_ = e);
                    foreach (var s in RunSteps(combine)) {
                        if (s.Cancel) {
                            PopStackFrame();
                            yield break;
                        }
                        yield return s;
                    }
                    Index++;
                }
                PopStackFrame();
                if (shorthand) Print(this.Pop(), false);
            }
            else {
                throw new StaxException("Bad types for reduce");
            }
        }

        private IEnumerable<ExecutionState> DoFor(Block block, Block rest) {
            bool implicitRange = false;
            if (IsInt(Peek())) {
                Push(Range(1, Pop()));
                implicitRange = true;
            }
            if (IsArray(Peek())) {
                if (implicitRange) block.AddDesc("for 1 to n, using rest of program");
                else block.AddDesc("foreach element, using rest of program");

                PushStackFrame();
                foreach (var e in Pop()) {
                    Push(_ = e);
                    foreach (var s in RunSteps(rest)) {
                        if (s.Cancel) break;
                        yield return s;
                    }
                    Index++;
                }
                PopStackFrame();
                yield break;
            }

            dynamic b = Pop(), a = Pop();
            if (IsInt(a) && IsBlock(b)) {
                a = Range(1, a);
                implicitRange = true;
            }
            if (IsArray(a) && IsBlock(b)) {
                if (implicitRange) block.AddDesc("for 1 to n, push and execute block");
                else block.AddDesc("foreach element, push and execute block");

                PushStackFrame();
                foreach (var e in a) {
                    Push(_ = e);
                    foreach (var s in RunSteps((Block)b)) {
                        if (s.Cancel) {
                            PopStackFrame();
                            yield break;
                        }
                        yield return s;
                    }
                    Index++;
                }
                PopStackFrame();
            }
            else {
                throw new StaxException("Bad types for for");
            }
        }

        private IEnumerable<ExecutionState> DoTransposeOrMaybe(Block block) {
            if (IsBlock(Peek())) {
                block.AddDesc("execute block if value is truthy");
                Block b = Pop();
                if (IsTruthy(Pop())) {
                    foreach (var s in RunSteps(b)) {
                        if (s.Cancel) yield break;
                        yield return s;
                    }
                }
                yield break;
            }

            block.AddDesc("transpose 2-d array; treats scalars as singletons and truncates to shortest");
            List<object> list = Pop();
            var result = new List<object>();

            if (list.Count > 0 && !IsArray(list[0])) list = new List<object> { list };

            int maxLen = 0;
            foreach (List<object> row in list) maxLen = Math.Max(maxLen, row.Count);

            foreach (List<object> line in list) {
                line.AddRange(Enumerable.Repeat(BigInteger.Zero as object, maxLen - line.Count));
            }

            for (int i = 0; i < maxLen; i++) {
                var column = new List<object>();
                foreach (dynamic row in list) column.Add(row[i]);
                result.Add(column);
            }

            Push(result);
        }

        private IEnumerable<ExecutionState> DoCrossMap(Block block, Block rest) {
            bool shorthand = false;
            Block map;
            if (IsBlock(Peek())) {
                block.AddDesc("cross-map arrays a and b into result; output[i,j] = f(a[i], b[i])");
                map = Pop();
            }
            else {
                block.AddDesc("cross-map arrays a and b, printing each resulting row");
                shorthand = true;
                map = rest;
            }

            dynamic inner = Pop(), outer = Pop();
            if (IsInt(inner)) inner = Range(1, inner);
            if (IsInt(outer)) outer = Range(1, outer);

            var result = new List<object>();
            PushStackFrame();
            foreach (var e in outer) {
                var row = new List<object>();
                PushStackFrame();
                foreach (var f in inner) {
                    Push(e);
                    Push(f);
                    _ = new IteratorPair(e, f);

                    try {
                        foreach (var s in RunSteps(map)) {
                            if (s.Cancel) goto Cancel;
                            yield return s;
                        }
                    }
                    finally { ++Index; }

                    row.Add(Pop());
                    Cancel:;
                }
                PopStackFrame();
                ++Index;
                if (shorthand) Print(row);
                else result.Add(row);
            }
            PopStackFrame();

            if (!shorthand) Push(result);
        }

        private IEnumerable<ExecutionState> DoMap(Block block, Block rest) {
            if (IsInt(Peek())) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "map range 1 to " + e + " using rest of program; print the results");
                else block.AddDesc("map range 1 to n using rest of program; print the results");
                var n = Pop();
                PushStackFrame();
                for (Index = BigInteger.Zero; Index < n; Index++) {
                    Push(_ = Index + 1);
                    foreach (var s in RunSteps(rest)) {
                        if (s.Cancel) goto NextIndex;
                        yield return s;
                    }
                    Print(Pop());
                    NextIndex:;
                }
                PopStackFrame();
                yield break;
            }
            else if (IsArray(Peek())) {
                block.AddDesc("map array using rest of program; print the results");
                PushStackFrame();
                foreach (var e in Pop()) {
                    Push(_ = e);
                    foreach (var s in RunSteps(rest)) {
                        if (s.Cancel) goto NextElement;
                        yield return s;
                    }
                    Print(Pop());
                    NextElement: Index++;
                }
                PopStackFrame();
                yield break;
            }

            dynamic b = Pop(), a = Pop();

            if (IsArray(b)) (a, b) = (b, a);
            if (IsInt(a) && IsBlock(b)) a = Range(1, a);

            if (IsArray(a) && IsBlock(b)) {
                block.AddDesc("map array");
                PushStackFrame();
                var result = new List<object>();
                foreach (var e in a) {
                    Push(_ = e);

                    foreach (var s in RunSteps((Block)b)) {
                        if (s.Cancel) goto NextElement;
                        yield return s;
                    }
                    result.Add(Pop());

                    NextElement: Index++;
                }
                Push(result);
                PopStackFrame();
            }
            else {
                throw new StaxException("bad type for map");
            }
        }

        private void DoPlus(Block block) {
            if (TotalStackSize < 2) return;
            dynamic b = Pop(), a = Pop();

            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "add " + e);
                else block.AddDesc("add");
                if (IsFloat(a) || IsFloat(b)) {
                    a = (double)a;
                    b = (double)b;
                }
                Push(a + b);
            }
            else if (IsArray(a) && IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "concatenate " + e);
                else block.AddDesc("concatenate");
                var result = new List<object>(a);
                result.AddRange(b);
                Push(result);
            }
            else if (IsArray(a)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "add " + e + " to end of array");
                else block.AddDesc("add element to end of array");
                Push(new List<object>(a) { b });
            }
            else if (IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "add element to beginning of " + e);
                else block.AddDesc("add element to beginning of array");
                var result = new List<object> { a };
                result.AddRange(b);
                Push(result);
            }
            else {
                throw new StaxException("Bad types for +");
            }
        }

        private void DoMinus(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsArray(a) && IsArray(b)) {
                block.AddDesc("remove all matching elements");
                a = new List<object>(a);
                var bl = (List<object>)b;
                a.RemoveAll((Predicate<object>)(e => bl.Contains(e, Comparer.Instance)));
                Push(a);
            }
            else if (IsArray(a)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "remove all occurrences of " + e);
                else block.AddDesc("remove all occurrences");
                a = new List<object>(a);
                a.RemoveAll((Predicate<object>)(e => AreEqual(e, b)));
                Push(a);
            }
            else if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "minus " + e);
                else block.AddDesc("subtract");
                if (IsFloat(a) || IsFloat(b)) {
                    a = (double)a;
                    b = (double)b;
                }
                Push(a - b);
            }
            else {
                throw new StaxException("Bad types for -");
            }
        }

        private IEnumerable<ExecutionState> DoSlash(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "floor division by " + e);
                else block.AddDesc("floor division");
                if (IsFloat(a) || IsFloat(b) || AreEqual(b, BigInteger.Zero)) {
                    a = (double)a;
                    b = (double)b;
                }
                if (IsNumber(a) && IsNumber(b) && a < 0) {
                    Push((a - b + 1) / b); // int division is floor always
                }
                else {
                    Push(a / b);
                }
            }
            else if (IsArray(a) && IsInt(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "split into groups of " + e);
                else block.AddDesc("split into groups");
                var result = new List<object>();
                for (int i = 0; i < a.Count; i += (int)b) {
                    result.Add(((IEnumerable<object>)a).Skip(i).Take((int)b).ToList());
                }
                Push(result);
            }
            else if (IsArray(a) && IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "split string by " + e);
                else block.AddDesc("string split");
                string[] strings = A2S(a).Split(new string[] { A2S(b) }, 0);
                Push(strings.Select(s => S2A(s) as object).ToList());
            }
            else if (IsArray(a) && IsBlock(b)) {
                block.AddDesc("partition into groups of adjacent elements that produce equal values after executing block");
                List<object> result = new List<object>(), currentPart = null;
                dynamic last = null;
                
                PushStackFrame();
                foreach (var e in a) {
                    Push(_ = e);
                    foreach (var s in RunSteps((Block)b)) {
                        if (s.Cancel) goto Cancel;
                        yield return s;
                    }
                    var current = Pop();
                    if (!AreEqual(current, last)) {
                        if (currentPart != null) result.Add(currentPart);
                        currentPart = new List<object>();
                    }
                    currentPart.Add(e);
                    last = current;

                    Cancel:
                    ++Index;
                }
                if (currentPart.Count > 0) result.Add(currentPart);
                PopStackFrame();
                Push(result);
            }
            else {
                throw new StaxException("Bad types for /");
            }
        }

        private void DoPercent(Block block) {
            var b = Pop();
            if (IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "length of " + e);
                else block.AddDesc("array length");
                Push((BigInteger)b.Count);
                return;
            }

            var a = Pop();
            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "modulo " + e);
                else block.AddDesc("modulus");
                if (IsFloat(a) || IsFloat(b)) {
                    a = (double)a;
                    b = (double)b;
                }
                if (IsFrac(a) || IsFrac(b)) {
                    a = (Rational)a;
                    b = (Rational)b;
                }
                var result = a % b;
                if (result < 0) result += b;
                Push(result);
            }
            else {
                throw new StaxException("Bad types for %");
            }
        }

        private IEnumerable<ExecutionState> DoStar(Block block) {
            if (TotalStackSize < 2) yield break;
            dynamic b = Pop(), a = Pop();

            if (IsInt(a)) (a, b) = (b, a);

            if (IsInt(b)) {
                if (IsArray(a)) {
                    if (b < 0) {
                        a.Reverse();
                        b *= -1;
                        block.AddDesc("repeat array - negative number reverses");
                    }
                    else if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "repeat array " + e + " times");
                    else block.AddDesc("repeat array");
                    var result = new List<object>();
                    for (int i = 0; i < b; i++) result.AddRange(a);
                    Push(result);
                    yield break;
                }
                else if (IsBlock(a)) {
                    if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "repeat " + e + " times");
                    else block.AddDesc("repeat n times");
                    PushStackFrame();
                    for (Index = 0; Index < b; Index++) {
                        foreach (var s in RunSteps((Block)a)) yield return s;
                    }
                    PopStackFrame();
                    yield break;
                }
            }

            if (IsArray(a) && IsArray(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "join with " + e);
                else block.AddDesc("string join");
                string result = "", joiner = A2S(b);
                bool subsequent = false;
                foreach (var e in a) {
                    if (subsequent) result += joiner;
                    result += IsArray(e) ? A2S(e): e;
                    subsequent = true;
                }
                Push(S2A(result));
                yield break;
            }

            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "times " + e);
                else block.AddDesc("multiply");
                if (IsFloat(a) || IsFloat(b)) {
                    a = (double)a;
                    b = (double)b;
                }
                Push(a * b);
                yield break;
            }

            throw new StaxException("Bad types for *");
        }

        private List<object> PrimeFactors(BigInteger n) {
            var result = new List<object>();
            n = BigInteger.Abs(n);
            if (n <= 1) return result;
            foreach (var d in PrimeHelper.AllPrimes()) {
                while (n % d == 0) {
                    result.Add(d);
                    n /= d;
                }
                if (n == 1) return result;
            }
            throw new Exception("Reality mismatch.  Ran out of primes.");
        }

        private void DoGCD() {
            var b = Pop();
            if (IsArray(b)) {
                BigInteger result = 0;
                foreach (BigInteger e in b) result = BigInteger.GreatestCommonDivisor(result, e);
                Push(result);
                return;
            }

            var a = Pop();
            if (IsInt(a) && IsInt(b)) {
                Push(BigInteger.GreatestCommonDivisor(a, b));
                return;
            }

            throw new StaxException("Bad types for GCD");
        }

        #region support
        private object ToNumber(dynamic arg) {
            if (IsArray(arg)) {
                return BigInteger.Parse(A2S(arg));
            }
            throw new StaxException("Bad type for ToNumber");
        }

        private List<object> ToString(dynamic arg) {
            if (IsNumber(arg)) {
                return S2A(arg.ToString());
            }
            else if (IsArray(arg)) {
                var result = new StringBuilder();
                foreach (var e in arg) result.Append(IsInt(e) ? e : A2S(e));
                return S2A(result.ToString());
            }
            throw new StaxException("Bad type for ToString");
        }

        private static bool AreEqual(dynamic a, dynamic b) => Comparer.Instance.Compare(a, b) == 0;

        private List<object> RunLength(List<object> arr) {
            if (arr.Count == 0) return arr;
            var result = new List<object>();
            object last = null;
            int run = 0;
            foreach (var e in arr) {
                if (AreEqual(e, last)) {
                    run += 1;
                }
                else {
                    if (run > 0) result.Add(new List<object> { last, new BigInteger(run) });
                    last = e;
                    run = 1;
                }
            }
            result.Add(new List<object> { last, new BigInteger(run) });
            return result;
        }

        private Dictionary<object, int> Multiset(List<object> arr) {
            var result = new Dictionary<object, int>(Comparer.Instance);
            foreach (var e in arr) {
                if (!result.ContainsKey(e)) result[e] = 0;
                result[e] += 1;
            }
            return result;
        }

        private static bool IsInt(object b) => b is BigInteger;
        private static bool IsFrac(object b) => b is Rational;
        private static bool IsFloat(object b) => b is double;
        private static bool IsNumber(object b) => IsInt(b) || IsFrac(b) || IsFloat(b);
        private static bool IsArray(object b) => b is List<object>;
        private static bool IsBlock(object b) => b is Block;
        private static bool IsTruthy(dynamic b) => (IsNumber(b) && b != 0) || (IsArray(b) && b.Count != 0);

        private static List<object> S2A(string arg) {
            var result = new List<object>();
            var e = StringInfo.GetTextElementEnumerator(arg);
            while (e.MoveNext()) {
                int codepoint = char.ConvertToUtf32((string)e.Current, 0);
                result.Add(new BigInteger(codepoint));
            }
            return result;
        }

        private static string A2S(List<object> arg) {
            string Convert(object e) {
                if (IsInt(e)) {
                    if (AreEqual(e, BigInteger.Zero)) return " ";
                    return char.ConvertFromUtf32((int)(BigInteger)e);
                }
                return A2S((List<object>)e);
            }
            return string.Concat(arg.Select(Convert));
        }

        private static List<object> Range(BigInteger start, BigInteger count) =>
            Enumerable.Range((int)start, (int)count).Select(n => new BigInteger(n) as object).ToList();

        private string Format(dynamic e) {
            if (IsArray(e)) {
                if (((List<object>)e).TrueForAll(ee => ee is BigInteger bi && (bi >= 32 && bi < 127 || bi == 0))) {
                    return '"' + A2S(e).Replace("\n", "\\n") + '"';
                }
                else {
                    var inner = ((IList<object>)e).Select(Format);
                    return '[' + string.Join(", ", inner) + ']';
                }
            }
            return e.ToString();
        }

        private object ParseNumber(string program, ref int ip) {
            var substring = program.Substring(ip);
            var match = Regex.Match(program.Substring(ip), @"^\d+!(\d*[1-9])?");
            if (match.Success) {
                ip += match.Value.Length;
                return double.Parse(match.Value.Replace('!', '.'));
            }

            match = Regex.Match(program.Substring(ip), @"^0|[1-9]\d*");
            if (match.Success) {
                if (match.Value == "10") { // 1 0 (ten is A)
                    ip += 1;
                    return BigInteger.One;
                }
                ip += match.Value.Length;
                return BigInteger.Parse(match.Value);
            }

            throw new InvalidOperationException("tried to parse a number, but there was only " + substring);
        }

        private List<object> ParseCompressedString(string program, ref int ip, out bool implicitEnd) {
            string compressed = "";
            while (ip < program.Length - 1 && program[++ip] != '`') compressed += program[ip];
            implicitEnd = ip == program.Length - 1;

            var decompressed = HuffmanCompressor.Decompress(compressed);
            return S2A(decompressed);
        }

        /// <summary>
        /// parse a string literal
        /// </summary>
        /// <param name="program"></param>
        /// <param name="doTemplates"></param>
        /// <param name="ip">
        /// input: index of first character in interior of literal
        /// output: index of closing quote or last character of string
        /// </param>
        /// <param name="implicitEnd"></param>
        /// <returns></returns>
        private List<object> ParseString(string program, bool doTemplates, ref int ip, out bool implicitEnd) {
            string result = "";
            while (ip < program.Length - 1 && program[++ip] != '"') {
                if (program[ip] == '`') {
                    switch (program[++ip]) {
                        case '0':
                            result += '\0';
                            break;
                        case '1':
                            result += '\n';
                            break;
                        case '2':
                            result += '\t';
                            break;
                        case '3':
                            result += '\r';
                            break;
                        case '4':
                            result += '\v';
                            break;
                        case '`':
                        case '"':
                            result += program[ip];
                            break;
                        default:
                            int instLen = ":|V".Contains(program[ip]) ? 2 : 1;
                            if (doTemplates) {
                                RunMacro(program.Substring(ip, instLen));
                                if (IsArray(Peek())) result += A2S(Pop());
                                else result += A2S(ToString(Pop()));
                            }
                            ip += instLen - 1;
                            break;
                    }
                }
                else {
                    result += program[ip];
                }
            }
            implicitEnd = program[ip] != '"';
            return S2A(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="block"></param>
        /// <param name="ip">
        /// input: index of first character in block interior
        /// output: index of } for those blocks, otherwise index of last character in block interior
        /// </param>
        /// <param name="entireProgram"></param>
        /// <returns></returns>
        private Block ParseBlock(Block block, ref int ip, bool entireProgram) {
            string contents = block.Contents;
            int depth = 1, start = ip;

            for(; ip < contents.Length; ip++) {
                if (contents[ip] == '|' || contents[ip] == ':' || contents[ip] == '\'' || contents[ip] == 'V') {
                    ip++; // 2-char tokens
                    continue;
                }

                if (contents[ip] == '.') {
                    ip += 2; // 2-char literal
                    continue;
                }

                if (contents[ip] == '\t') {
                    ip = contents.IndexOf('\n', ip);
                    if (ip < 0) break;
                    continue;
                }

                if (contents[ip] == '"') {
                    ParseString(contents, false, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '`') {
                    ParseCompressedString(contents, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '{') ++depth;
                if (contents[ip] == '}' && --depth == 0) return block.SubBlock(start, ip);

                // shortcut block terminators
                if (depth > 1 || !entireProgram) {
                    if ("wWmfFkKgo".Contains(contents[ip]) && --depth == 0) return block.SubBlock(start, ip--);
                }
            }
            ip = contents.Length - 1;
            return block.SubBlock(start);
        }

        class Comparer : IComparer<object>, IEqualityComparer<object> {
            public static readonly Comparer Instance = new Comparer();

            private Comparer() { }

            private int CompareScalars(dynamic a, dynamic b) {
                if (IsFloat(a) || IsFloat(b)) return ((double)a).CompareTo((double)b);
                if (IsFrac(a) || IsFrac(b)) return ((Rational)a).CompareTo((Rational)b);
                if (IsInt(a) || IsInt(b)) return ((BigInteger)a).CompareTo((BigInteger)b);
                throw new StaxException("what types even are they?");
            }

            public int Compare(dynamic a, dynamic b) {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;

                if (IsNumber(a)) {
                    if (IsNumber(b)) return CompareScalars(a, b);
                    if (IsArray(b)) {
                        if (b.Count == 0) return 1;
                        return Compare(a, b[0]);
                    }
                    return a.GetType().Name.CompareTo(b.GetType().Name);
                }
                if (IsNumber(b)) {
                    if (IsArray(a)) {
                        if (a.Count == 0) return -1;
                        return Compare(a[0], b);
                    }
                    return a.GetType().Name.CompareTo(b.GetType().Name);
                }
                if (IsArray(a) && IsArray(b)) {
                    IList<object> al = (IList<object>)a, bl = (IList<object>)b;
                    for (int i = 0; i < al.Count && i < bl.Count; i++) {
                        int ec = Compare(al[i], bl[i]);
                        if (ec != 0) return ec;
                    }
                    return al.Count.CompareTo(bl.Count);
                }
                return a.GetType().Name.CompareTo(b.GetType().Name);
            }

            public new bool Equals(object a, object b) => Compare(a, b) == 0;

            public int GetHashCode(object a) {
                if (IsArray(a)) {
                    int hash = 0;
                    foreach (var e in (IList<object>)a) {
                        hash *= 37;
                        hash ^= GetHashCode(e);
                    }
                    return hash;
                }
                return a.GetHashCode();
            }
        }
        #endregion
    }
}
