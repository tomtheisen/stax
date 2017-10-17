using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

// available chars
//  DGS
/* To add:
 *     find-index-all by regex
 *     running "total" / reduce-collect
 *     log
 *     trig
 *     string interpolate
 *     uneval
 *     multidimensional array index assign / 2-dimensional ascii art grid assign mode
 *     combinatorics: powerset, permutations
 *     Rotate chars (like translate on a ring)
 *     call into trailing }
 *     FeatureTests for generators
 *     RLE
 *     RLE prime factorization [(prime, exp)]
 *     data-driven macro namespace ':' - 
 *          compare / sign (c{c|a/}0?)
 *          replace first only
 *     while loops continue to next
 *     hypotenuse type operation
 *     get indices of truthy elements
 *     get index of first truthy element
 *     get indices of falsy elements
 *     get index of first falsy element
 *     get first truthy element
 *     squarify array
 *     get indices of maxes
 *     get indices of mins
 *     array mul {*k
 *     all {!f!
 *     any {f
 *     sorted indices by value
 *     all combinations
 *     n-combinations
 *     permutations
 *     log 2/10/n
 *     mirror cr+
 *     trim element(s)
 *     absolute difference -|a
 *     multiplicity 0~{b%C caa / s ,^~ Wdd,
 *     custom base encoding
 *     all factors c%{[%!fsd
 *     distinct prime factors |fu
 *     distinct prime factor count |fu%
 *     all factor count
 *     prime factorization exponents
 *     nth prime
 *     next prime >=
 *     last prime <
 *     nth fibonacci element
 *     totient c{[|g1=f%sd
 *     mode
 *     centered range |aNcN^|r
 *     multiset intersection
 *     multiset subtract
 *     multiset xor
 *     multiset union
 *     split once bI~;^ {n;(aa %,+t 2l} {d],d}?
 *     insert at
 *     if (no else) like {^D
 *     all sub-arrays |]{|[m{+k
 *     copy thrice ccc
 *     hasupper VA |&
 *     haslower Va |&
 *     hasletter Vl |&
 *     2-char string literal
 *     assign to array using predicate instead of index
 *     is subset
 *     is superset
 *     sign c{c|a/}{d0}?
 *     rot13
 *     squarify array
 *     
 *     debugger
 */

namespace StaxLang {
    public class Executor {
        public const string VersionInfo = "Stax 0.0.0 - Tom Theisen - https://github.com/ttheisen/stax";

        private bool OutputWritten = false;
        public TextWriter Output { get; private set; }
        public bool Annotate { get; set; }
        public IReadOnlyList<string> Annotation { get; private set; } = null;

        private static IReadOnlyDictionary<char, (object Value, string Name)> Constants = new Dictionary<char, (object, string)> {
            ['?'] = (S2A(VersionInfo), "version info"),
            ['0'] = (new Rational(0, 1), "0/1"),
            ['A'] = (S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), "uppercase alphabet"),
            ['a'] = (S2A("abcdefghijklmnopqrstuvwxyz"), "lowercase alphabet"),
            ['B'] = (new BigInteger(256), "256"),
            ['C'] = (S2A("BCDFGHJKLMNPQRSTVWXYZ"), "uppercase consonants"),
            ['c'] = (S2A("bcdfghjklmnpqrstvwxyz"), "lowercase consonants"),
            ['d'] = (S2A("0123456789"), "decimal digits"),
            ['H'] = (S2A("0123456789ABCDEF"), "uppercase hex digits"),
            ['h'] = (S2A("0123456789abcdef"), "lowercase hex digits"),
            ['k'] = (new BigInteger(1000), "one thousand"),
            ['l'] = (S2A("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all letters"),
            ['L'] = (S2A("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all alphanumerics"),
            ['M'] = (new BigInteger(1000000), "one million"),
            ['p'] = (S2A(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"), "all printable ascii characters"),
            ['P'] = (Math.PI, "pi"),
            ['V'] = (S2A("AEIOU"), "uppercase vowels"),
            ['v'] = (S2A("aeiou"), "lowercase vowels"),
            ['W'] = (S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"), "all digits and uppercase letters"),
            ['w'] = (S2A("0123456789abcdefghijklmnopqrstuvwxyz"), "all digits and lowercase letters"),
            ['s'] = (S2A(" \t\r\n\v"), "all ascii whitespace"),
            ['n'] = (S2A("\n"), "newline"),  // also just A]
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
            input = input ?? Array.Empty<string>();

            IndexOuter = Index = 0;
            X = BigInteger.Zero;
            Y = S2A("");
            _ = S2A(string.Join("\n", input));

            if (input.Length > 0) {
                Y = S2A(input[0]);
                if (BigInteger.TryParse(input[0], out var d)) X = d;
            }

            var transformedInput = input
                .Reverse()
                .SkipWhile(s => s == "")
                .Select(S2A).ToArray();
            MainStack = new Stack<dynamic>();
            InputStack = new Stack<dynamic>(transformedInput);

            if (programBlock.Contents.StartsWith("e")) {
                // if first instruction is 'e', eval all lines and put back on stack in same order
                RunMacro("L{eFw~|d");
                programBlock.AddDesc("eval line mode; parse each line - push all values to input stack");
            }
            else if (programBlock.Contents.StartsWith("i")) {
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
            if (TotalStackSize > 0 && !IsInt(Peek())) switch (program.FirstOrDefault()) {
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
                    case '0':
                        Push(BigInteger.Zero);
                        block.AddDesc("0");
                        type = InstructionType.Value;
                        break;
                    case '1': case '2': case '3': case '4': case '5':
                    case '6': case '7': case '8': case '9':
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
                            Push(ParseString(program, ref ip, out bool implicitEnd));
                            type = InstructionType.Value;
                            if (implicitEnd) {
                                Print(Peek());
                                block.AddDesc("print unclosed literal");
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
                        Push(ParseBlock(block, ref ip));
                        break;
                    case '}': 
                        block.AddDesc("start over");
                        ip = -1;
                        break;
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
                        DoSlash(block);
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
                        DoPadRight(block);
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
                        else throw new StaxException("Bad type for B");
                        break;
                    case 'c': 
                        block.AddDesc("copy of top element in stack");
                        type = InstructionType.Value;
                        Push(Peek());
                        break;
                    case 'C':
                        if (CallStackFrames.Any()) block.AddDesc("cancel iteration if true");
                        else block.AddDesc("terminate if true");
                        if (IsTruthy(Pop())) yield return ExecutionState.CancelState;
                        break;
                    case 'd': 
                        block.AddDesc("pop and discard");
                        Pop();
                        break;
                    case 'e': 
                        if (CallStackFrames.Any() || ip > 0) {
                            block.AddDesc("eval - parse strings, arrays, and numbers");
                            if (!DoEval()) throw new StaxException("eval failed");
                        }
                        break;
                    case 'E': // explode (de-listify)
                        DoExplode(block);
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
                    case 'h':
                        if (IsInt(Peek())) {
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
                        break;
                    case 'H':
                        if (IsInt(Peek())) {
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
                        break;
                    case 'i':
                        if (CallStackFrames.Any()) {
                            type = InstructionType.Value;
                            block.AddDesc("iteration index");
                            Push(Index);
                        }
                        break;
                    case 'I':
                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "find first index of " + e);
                        else block.AddDesc("find first index");
                        DoFindIndex();
                        break;
                    case 'j':
                        if (IsArray(Peek())) {
                            block.AddDesc("un-join (split) by spaces");
                            RunMacro("' /");
                        }
                        else if (IsInt(Peek())) {
                            BigInteger digits = Pop();
                            double num = Pop();
                            Push(S2A(Math.Round(num, (int)digits).ToString()));
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
                        block.AddDesc("transpose 2-d array; treats scalars as singletons and truncates to shortest");
                        DoTranspose();
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
                            if (CallStackFrames.Any()) {
                                block.AddDesc("current iteration value");
                                Push(_);
                            }
                            else if (_ is IteratorPair p) {
                                block.AddDesc("push outer and inner iteration values");
                                Push(p.Outer);
                                Push(p.Inner);
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
                                block.AddDesc("divmod; push a/b and a%b");
                                RunMacro("ssb%~/,");
                                break;
                            case '+': 
                                block.AddDesc("sum of array");
                                RunMacro("0s{+F");
                                break;
                            case '-':
                                block.AddDesc("pairwise difference of array");
                                RunMacro("2B{Es-m");
                                break;
                            case '~':
                                block.AddDesc("bitwise not");
                                Push(~Pop());
                                break;
                            case '&':
                                if (IsArray(Peek())) {
                                    block.AddDesc("set intersection; keep all elements from left array that appear in right");
                                    RunMacro("ss~ {;sIU>f ,d"); 
                                }
                                else {
                                    block.AddDesc("bitwise and");
                                    Push(Pop() & Pop()); 
                                }
                                break;
                            case '|': 
                                block.AddDesc("bitwise or");
                                Push(Pop() | Pop());
                                break;
                            case '^':
                                if (IsArray(Peek())) {
                                    block.AddDesc("symmetric array difference");
                                    RunMacro("s b-~ s-, +"); 
                                }
                                else {
                                    block.AddDesc("bitwise xor");
                                    Push(Pop() ^ Pop());
                                }
                                break;
                            case '*':
                                if (IsInt(Peek())) { 
                                    dynamic b = Pop();
                                    if (IsInt(Peek())) { 
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "to the " + e + " power");
                                        else block.AddDesc("exponent");
                                        Push(BigInteger.Pow(Pop(), (int)b));
                                        break;
                                    }
                                    else if (IsFrac(Peek())) { 
                                        if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "fraction to the " + e + " power");
                                        else block.AddDesc("exponent");
                                        dynamic a = Pop();
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
                                block.AddDesc("zip; truncate to shorter");
                                RunMacro("b%s% |m~ ;(s,(s \\");
                                break;
                            case ')': 
                                DoRotate(block, RotateDirection.Right);
                                break;
                            case '(': 
                                DoRotate(block, RotateDirection.Left);
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
                                block.AddDesc("bit shift left");
                                RunMacro("|2*");
                                break;
                            case '>': 
                                block.AddDesc("bit shift right");
                                RunMacro("|2/");
                                break;
                            case '1': 
                                block.AddDesc("power of -1");
                                RunMacro("2%U1?");
                                break;
                            case '2': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "2 to the " + e);
                                else block.AddDesc("power of 2");
                                RunMacro("2s|*");
                                break;
                            case '3': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in base 36");
                                else block.AddDesc("base 36");
                                RunMacro("36|b");
                                break;
                            case 'a': 
                                block.AddDesc("absolute value");
                                Push(BigInteger.Abs(Pop()));
                                break;
                            case 'A': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "10 to the " + e);
                                else block.AddDesc("power of 10");
                                Push(BigInteger.Pow(10, (int)Pop()));
                                break;
                            case 'b': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "convert to base " + e);
                                else block.AddDesc("convert base");
                                DoBaseConvert();
                                break;
                            case 'B': 
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " in binary");
                                else block.AddDesc("convert to binary");
                                RunMacro("2|b");
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
                                block.AddDesc("is even?");
                                Push(Pop() % 2 ^ 1);
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
                            case 'l':
                                block.AddDesc("lowest common denominator");
                                if (IsArray(Peek())) RunMacro("1s{|lF");
                                else if (IsInt(Peek())) RunMacro("b|g~*,/");
                                else throw new StaxException("Bad type for lcm");
                                break;
                            case 'J':
                                block.AddDesc("join with newlines");
                                RunMacro("Vn*");
                                break;
                            case 'm':
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "minimum of n and " + e);
                                else block.AddDesc("minimum of");
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) < 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("chs{|mF");
                                else throw new StaxException("Bad types for min");
                                break;
                            case 'M': // max
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "maximum of n and " + e);
                                else block.AddDesc("maximum of");
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) > 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("chs{|MF");
                                else throw new StaxException("Bad types for max");
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
                            case 'r':
                                block.AddDesc("explicit range");
                                {
                                    dynamic end = Pop(), start = Pop();
                                    if (IsArray(end)) end = new BigInteger(end.Count);
                                    if (IsArray(start)) start = new BigInteger(-start.Count);
                                    Push(Range(start, end - start));
                                    break;
                                }
                            case 'R': // start-end-stride range
                                block.AddDesc("explicit range with stride"); 
                                {
                                    int stride = (int)Pop(), end = (int)Pop(), start = (int)Pop();
                                    Push(Enumerable.Range(0, end - start).Select(n => n * stride + start).TakeWhile(n => n < end).Select(n => new BigInteger(n) as object).ToList());
                                    break;
                                }
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
                            case 'q': // int square root
                                block.AddDesc("floor square root");
                                Push(new BigInteger(Math.Sqrt(Math.Abs((double)Pop()))));
                                break;
                            case 'Q': // float square root
                                block.AddDesc("square root");
                                Push(Math.Sqrt(Math.Abs((double)Pop())));
                                break;
                            case 't': // translate
                                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "translate using adjacent pairs in map string: " + e);
                                block.AddDesc("translate; replace using adjacent pairs in map string");
                                DoTranslate();
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

        private void DoCenter(Block block) {
            dynamic top = Pop();
            if (IsInt(top)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "center in " + e + " spaces");
                else block.AddDesc("center string in n spaces");
                int size = (int)top;
                var str = Pop();
                var result = new List<object>(Enumerable.Repeat(BigInteger.Zero as object, (size - str.Count) / 2));
                result.AddRange(str);
                result.AddRange(Enumerable.Repeat(BigInteger.Zero as object, size - result.Count));
                Push(result);
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
            /*
             *  End condition
	         *      duplicate -     u
	         *      n reached -     n
	         *      filter false -  f
	         *      cancelled -	    c
             *      invariant pt -  i
             *      target value -  t
             *      first as scalar s
             *      element@ index  e
             *
             *  Collection type
	         *      pre-peek - lower case
	         *      post-pop - upper case
             *
             *  Filter
	         *      yes
	         *      no
             *
             *   {filter}{project}gu
             *   {filter}{project}gi
             *   {filter}{project}gf
             *   {filter}{project}gc
             *   {filter}{project}gs
             *  0{filter}{project}gn
             *  0{filter}{project}ge
             *   {filter}{project}g9
             *  t{filter}{project}gt
	         *           {project}gu
	         *           {project}gi
	         *           {project}gc
	         *  0        {project}gn
	         *  0        {project}ge
	         *           {project}g9
	         *  t        {project}gt
             *   {filter}{project}gU
             *   {filter}{project}gI
             *   {filter}{project}gF
             *   {filter}{project}gC
             *   {filter}{project}gS
             *  0{filter}{project}gN
             *  0{filter}{project}gE
             *   {filter}{project}g(
             *  t{filter}{project}gT
	         *           {project}gU
	         *           {project}gI
	         *           {project}gC
	         *  0        {project}gN
	         *  0        {project}gE
	         *           {project}g(
	         *  t        {project}gT
             *           
             *           gu project
             *           gi project
             *           gc project
             *           gs project
             *         0 gn project
             *         0 ge project
             *           g9 project
             *         t gt project
             *           gU project
             *           gI project
             *           gC project
             *           gS project
             *         0 gN project
             *         0 gE project
             *           g( project
             *         t gT project
             *
             */

            char lowerSpec = char.ToLower(spec);
            bool stopOnDupe = lowerSpec == 'u',
                stopOnFilter = lowerSpec == 'f',
                stopOnCancel = lowerSpec == 'c',
                stopOnFixPoint = lowerSpec == 'i',
                stopOnTargetVal = lowerSpec == 't',
                scalarMode = lowerSpec == 's' || lowerSpec == 'e',
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
                if (idx >= 0) targetCount = idx % 10 + 1;
                postPop = idx >= 10;
                hardCodedTargetCount = true;
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
                (scalarMode ? "generate values, keeping only the last,  " : "generate and collect values ")
                + (filter != null ? "matching filter " : "")
                + (shorthand ? "from rest of program " : "")
                + (stopOnDupe ? "until a duplicate is found, " : "")
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
                    if (stopOnDupe && result.Contains(generated, Comparer.Instance)) break;
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

            if (!IsArray(a)) a = new List<object> { a };
            if (!IsArray(b)) b = new List<object> { b };

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
                        var match = Regex.Match(substring, @"-?\d+\.\d+");
                        if (match.Success) {
                            NewValue(double.Parse(match.Value));
                            i += match.Value.Length - 1;
                            break;
                        }

                        match = Regex.Match(substring, @"(-?\d+)/(-?\d+)");
                        if (match.Success) {
                            var frac = new Rational(
                                BigInteger.Parse(match.Groups[1].Value),
                                BigInteger.Parse(match.Groups[2].Value));
                            NewValue(frac);
                            i += match.Value.Length - 1;
                            break;
                        }

                        match = Regex.Match(substring, @"-?\d+");
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
                yield break;
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
                do {
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
                string s = A2S(number).ToLower();
                BigInteger result = 0;
                foreach (var c in s) {
                    int digit = "0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(c);
                    if (digit < 0) digit = c + 0;
                    result = result * @base + digit;
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

        private void DoFindIndex() {
            dynamic element = Pop(), list = Pop();

            if (!IsArray(list)) (list, element) = (element, list);

            if (IsArray(list)) {
                for (int i = 0; i < list.Count; i++) {
                    if (IsArray(element)) {
                        if (i + element.Count > list.Count) {
                            Push(BigInteger.MinusOne);
                            return;
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
                            return;
                        }
                    }
                    else if (AreEqual(element, list[i])) {
                        Push((BigInteger)i);
                        return;
                    }
                }
                Push(BigInteger.MinusOne);
                return;
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
            dynamic element = Pop(), indexes = Pop(), list = Pop();

            if (IsInt(indexes)) {
                indexes = new List<object> { indexes };
                if (IsBlock(element)) block.AddDesc("modify array element at index");
                else if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "assign " + e + " at index to array");
                else block.AddDesc("assign element at index to array");
            }
            else {
                if (IsBlock(element)) block.AddDesc("modify array elements at indices");
                else if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "assign " + e + " to array at all indices");
                else block.AddDesc("assign element to array at all indices");
            }

            if (IsArray(list)) {
                var result = new List<object>(list);
                foreach (int arg in indexes) {
                    int index = arg;
                    if (index < 0) {
                        index += result.Count;
                        if (index < 0) {
                            result.InsertRange(0, Enumerable.Repeat((object)BigInteger.Zero, -index));
                            index = 0;
                        }
                    }
                    else if (index + 1 > result.Count) {
                        result.AddRange(Enumerable.Repeat((object)BigInteger.Zero, index + 1 - result.Count));
                    }

                    if (IsBlock(element)) {
                        Push(result[index]);
                        bool cancelled = false;
                        foreach (var s in RunSteps((Block)element)) cancelled = s.Cancel;
                        if (!cancelled) result[index] = Pop();
                    }
                    else {
                        result[index] = element;
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
            else {
                throw new StaxException("bad types for padleft");
            }
        }

        private void DoPadRight(Block block) {
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
            if (IsInt(Peek())) { // n times do
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => e + " times do");
                else block.AddDesc("n times do");
                var n = Pop();
                PushStackFrame();
                for (Index = BigInteger.Zero; Index < n; Index++) {
                    _ = Index + 1;
                    foreach (var s in RunSteps(rest)) yield return s;
                }
                PopStackFrame();
                yield break;
            }
            else if (IsArray(Peek())) { // filter shorthand
                block.AddDesc("treat rest of program as filter and print the result");
                PushStackFrame();
                foreach (var e in Pop()) {
                    Push(_ = e);
                    foreach (var s in RunSteps(rest)) yield return s;
                    if (IsTruthy(Pop())) Print(e);
                    Index++;
                }
                PopStackFrame();
                yield break;
            }

            dynamic b = Pop(), a = Pop();

            if (IsInt(a) && IsBlock(b)) a = Range(1, a);

            if (IsArray(a) && IsBlock(b)) {
                block.AddDesc("filter array by block");
                PushStackFrame();
                var result = new List<object>();
                foreach (var e in a) {
                    Push(_ = e);
                    foreach (var s in RunSteps((Block)b)) yield return s;
                    Index++;
                    if (IsTruthy(Pop())) result.Add(e);
                }
                Push(result);
                PopStackFrame();
            }
            else {
                throw new StaxException("Bad types for filter");
            }
        }

        private IEnumerable<ExecutionState> DoReduce(Block block, Block rest) {
            dynamic b = Pop(), a = Pop();
            if (IsInt(a) && IsBlock(b)) {
                a = Range(1, a);
                block.AddDesc("reduce range 1 to n using block");
            }
            else {
                block.AddDesc("reduce using block");
            }
            if (IsArray(a) && IsBlock(b)) {
                if (a.Count < 2) {
                    Push(a[0]);
                    yield break;
                }

                PushStackFrame();
                Push(a[0]);
                a.RemoveAt(0);
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

        private void DoTranspose() {
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
            if (IsInt(outer)) outer = Range(1, inner);

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
                            if (s.Cancel) continue;
                            yield return s;
                        }
                    }
                    finally { ++Index; }

                    row.Add(Pop());
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
                else block.AddDesc("substract");
                Push(a - b);
            }
            else {
                throw new StaxException("Bad types for -");
            }
        }

        private void DoSlash(Block block) {
            dynamic b = Pop(), a = Pop();

            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "floor division by " + e);
                else block.AddDesc("floor division");
                if (IsInt(a) && IsInt(b) && a < 0) {
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
            else {
                throw new StaxException("Bad types for /");
            }
        }

        private void DoPercent(Block block) {
            var b = Pop();
            if (IsArray(b)) {
                block.AddDesc("array length");
                Push((BigInteger)b.Count);
                return;
            }

            var a = Pop();
            if (IsInt(a) && IsInt(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "modulo " + e);
                else block.AddDesc("modulus");
                BigInteger result = a % b;
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
                string result = "";
                string joiner = A2S(b);
                foreach (var e in a) {
                    if (result != "") result += joiner;
                    result += IsInt(e) ? e : A2S(e);
                }
                Push(S2A(result));
                yield break;
            }

            if (IsNumber(a) && IsNumber(b)) {
                if (block.LastInstrType == InstructionType.Value) block.AmendDesc(e => "times " + e);
                else block.AddDesc("multiply");
                Push(a * b);
                yield break;
            }

            throw new StaxException("Bad types for *");
        }

        private List<object> PrimeFactors(BigInteger n) {
            var result = new List<object>();
            BigInteger d = 2;
            while (n > 1) {
                while (n % d == 0) {
                    result.Add(d);
                    n /= d;
                }
                ++d;
            }
            return result;
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
                var formatted = e;
                formatted = '"' + A2S(e).Replace("\n", "\\n") + '"';
                if (((string)formatted).Any(char.IsControl) || ((IList<object>)e).Any(IsArray)) {
                    var inner = ((IList<object>)e).Select(Format);
                    return '[' + string.Join(", ", inner) + ']';
                }
                return formatted;
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

        private List<object> ParseString(string program, ref int ip, out bool implicitEnd) {
            string result = "";
            while (ip < program.Length - 1 && program[++ip] != '"') {
                if (program[ip] == '`') ++ip;
                result += program[ip];
            }
            implicitEnd = program[ip] != '"';
            return S2A(result);
        }

        private Block ParseBlock(Block block, ref int ip) {
            string contents = block.Contents;
            int depth = 0;
            int start = ip + 1;
            do {
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
                    if (ip == -1) {
                        ip = contents.Length;
                        break;
                    }
                    continue;
                }

                if (contents[ip] == '"') {
                    ParseString(contents, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '`') {
                    ParseCompressedString(contents, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '{') ++depth;
                if (contents[ip] == '}' && --depth == 0) return block.SubBlock(start, ip);

                // shortcut block terminators
                if ("wWmfFkKgo".Contains(contents[ip]) && --depth == 0) return block.SubBlock(start, ip--);
            } while (++ip < contents.Length);
            --ip;
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
                if (a == null || b == null) return object.ReferenceEquals(a, b);
                if (IsNumber(a)) {
                    while (IsArray(b) && b.Count > 0) b = ((IList<object>)b)[0];
                    if (IsNumber(b)) return CompareScalars(a, b);
                    return a.GetType().Name.CompareTo(b.GetType().Name);
                }
                if (IsNumber(b)) {
                    while (IsArray(a) && a.Count > 0) a = ((IList<object>)a)[0];
                    if (IsNumber(b)) return CompareScalars(a, b);
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

            public new bool Equals(object a, object b) {
                if (IsArray(a) && IsArray(b)) {
                    IList<object> al = (IList<object>)a, bl = (IList<object>)b;
                    for (int i = 0; i < al.Count && i < bl.Count; i++) {
                        if (!Equals(al[i], bl[i])) return false;
                    }
                    return true;
                }
                return a.Equals(b);
            }

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
