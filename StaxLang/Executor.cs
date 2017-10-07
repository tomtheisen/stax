using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace StaxLang {
    // available chars
    //  .:DGKnoSZ
    /* To add:
     *     find-index-all by regex
     *     running "total" / reduce-collect
     *     flatten
     *     map-many
     *     zip-short
     *     log
     *     trig
     *     floats
     *     sqrt float 
     *     string interpolate
     *     repeat-to-length
     *     increase-to-multiple
     *     non-regex replace
     *     replace first only
     *     compare / sign (c|a/)
     *     eval fractions and floats
     *     uneval
     *     entire array ref inside for/filter/map 
     *     rectangularize (center/center-trim/left/right align, fill el)
     *     multidimensional array index assign / 2-dimensional ascii art grid assign mode
     *     copy 2nd
     *     CLI STDIN / STDOUT
     *     string starts-with / ends-with
     *     combinatorics: powerset, permutations
     *     Rotate chars (like translate on a ring)
     *     call into next line
     *     between
     *     clamp
     *     FeatureTests for generators
     *     RLE
     *     popcount (2|E|+)
     *     version string
     *     
     *     code explainer
     *     debugger
     *     docs
     *     tests in portable files
     */

    public class Executor {
        private bool OutputWritten = false;
        public TextWriter Output { get; private set; }
        public bool Annotate { get; set; }
        public IReadOnlyList<string> Annotation { get; private set; } = null;

        private static IReadOnlyDictionary<char, object> Constants = new Dictionary<char, object> {
            ['0'] = new Rational(0, 1),
            ['1'] = new Rational(1, 1),
            ['2'] = new Rational(1, 2),
            ['A'] = S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
            ['a'] = S2A("abcdefghijklmnopqrstuvwxyz"),
            ['C'] = S2A("BCDFGHJKLMNPQRSTVWXYZ"),
            ['c'] = S2A("bcdfghjklmnpqrstvwxyz"),
            ['d'] = S2A("0123456789"),
            ['V'] = S2A("AEIOU"),
            ['v'] = S2A("aeiou"),
            ['W'] = S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
            ['w'] = S2A("0123456789abcdefghijklmnopqrstuvwxyz"),
            ['s'] = S2A(" \t\r\n\v"),
            ['n'] = S2A("\n"),  // also just A]
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
        /// <returns>number of steps it took</returns>
        public int Run(string program, string[] input) {
            Initialize(program, input);
            var block = new Block(program);
            int step = 0;
            try {
                foreach (var s in RunSteps(block)) {
                    if (s.Cancel) break;
                    if (++step > 100000) throw new Exception("program is running too long");
                }
            }
            catch (InvalidOperationException) { }
            if (!OutputWritten) Print(Pop());

            if (Annotate) Annotation = block.Annotate(); 
            return step;
        }

        private void Initialize(string program, string[] input) {
            IndexOuter = Index = 0;
            X = BigInteger.Zero;
            Y = S2A("");
            _ = S2A(string.Join("\n", input));

            if (input.Length > 0) {
                Y = S2A(input[0]);
                if (BigInteger.TryParse(input[0], out var d)) X = d;
            }

            MainStack = new Stack<dynamic>();
            InputStack = new Stack<dynamic>(input.Reverse().Select(S2A));

            if (program.FirstOrDefault() == 'e') {
                // if first instruction is 'e', eval all lines and put back on stack in same order
                RunMacro("L{eFw~|d");
            }
            else if (input.Length == 1 & program.FirstOrDefault() != 'i') {
                try {
                    DoEval();
                    if (TotalStackSize == 0) {
                        InputStack = new Stack<dynamic>(input.Reverse().Select(S2A));
                    }
                    else {
                        (MainStack, InputStack) = (InputStack, MainStack);
                    }
                }
                catch {
                    MainStack.Clear();
                    InputStack = new Stack<dynamic>(input.Reverse().Select(S2A));
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
            if (program.Length > 0) switch (program[0]) {
                case 'm': // line-map
                case 'f': // line-filter
                case 'F': // line-for
                    if (TotalStackSize > 0 && !IsInt(Peek())) DoListify();
                    break;
            }

            for (int ip = 0; ip < program.Length;) {
                int ipstart = ip;
                void AddExplanation(string text) {
                    block.AddDesc(ipstart, text);
                }

                yield return new ExecutionState();
                switch (program[ip]) {
                    case '0':
                        Push(BigInteger.Zero);
                        break;
                    case '1': case '2': case '3': case '4': case '5':
                    case '6': case '7': case '8': case '9':
                        AddExplanation("number");
                        Push(ParseNumber(program, ref ip));
                        break;
                    case ' ': case '\n': case '\r':
                        break;
                    case '\t': // line comment
                        ip = program.IndexOf('\n', ip);
                        if (ip == -1) yield break;
                        break;
                    case ';': // peek from input stack
                        Push(InputStack.Peek());
                        break;
                    case ',': // pop from input stack
                        Push(InputStack.Pop());
                        break;
                    case '~': // push to input stack
                        InputStack.Push(Pop());
                        break;
                    case '#': // count number
                        if (IsArray(Peek())) RunMacro("/%v");
                        else if (IsNumber(Peek())) RunMacro("]|&%");
                        break;
                    case '"': // "literal"
                        {
                            Push(ParseString(program, ref ip, out bool implicitEnd));
                            if (implicitEnd) Print(Peek()); 
                        }
                        break;
                    case '`': // compressed `5Is1%`
                        {
                            Push(ParseCompressedString(program, ref ip, out bool implitEnd));
                            if (implitEnd) Print(Peek()); 
                        }
                        break;
                    case '\'': // single char 'x
                        Push(S2A(program.Substring(++ip, 1)));
                        break;
                    case '{': // block
                        Push(ParseBlock(block, ref ip));
                        break;
                    case '}': // do-over (or block end)
                        ip = -1;
                        break;
                    case '!': // not
                        Push(IsTruthy(Pop()) ? BigInteger.Zero : BigInteger.One);
                        break;
                    case '+':
                        AddExplanation("addition");
                        DoPlus();
                        break;
                    case '-':
                        DoMinus();
                        break;
                    case '*':
                        foreach (var s in DoStar()) yield return s;
                        break;
                    case '/':
                        DoSlash();
                        break;
                    case '\\':
                        DoZipRepeat();
                        break;
                    case '%':
                        DoPercent();
                        break;
                    case '@': // read index
                        DoAt();
                        break;
                    case '&': // assign index
                        DoAssignIndex();
                        break;
                    case '$': // to string
                        Push(ToString(Pop()));
                        break;
                    case '<':
                        DoLessThan();
                        break;
                    case '>':
                        DoGreaterThan();
                        break;
                    case '=':
                        Push(AreEqual(Pop(), Pop()) ? BigInteger.One : BigInteger.Zero);
                        break;
                    case 'v':
                        if (IsNumber(Peek())) Push(Pop() - 1); // decrement
                        else if (IsArray(Peek())) Push(S2A(A2S(Pop()).ToLower())); // lower
                        else throw new Exception("Bad type for v");
                        break;
                    case '^':
                        if (IsNumber(Peek())) Push(Pop() + 1); // increment
                        else if (IsArray(Peek())) Push(S2A(A2S(Pop()).ToUpper())); // uppper
                        else throw new Exception("Bad type for ^");
                        break;
                    case '(':
                        DoPadRight();
                        break;
                    case ')':
                        DoPadLeft();
                        break;
                    case '[': // copy outer
                        RunMacro("ss~c,");
                        break;
                    case ']': // singleton
                        Push(new List<object> { Pop() });
                        break;
                    case '?': // if
                        foreach (var s in DoIf()) yield return s;
                        break;
                    case 'a': // alter stack
                        {
                            dynamic c = Pop(), b = Pop(), a = Pop();
                            Push(b); Push(c); Push(a);
                        }
                        break;
                    case 'A': // 10 (0xA)
                        Push(new BigInteger(10));
                        break;
                    case 'b': // both copy
                        {
                            dynamic b = Pop(), a = Peek();
                            Push(b); Push(a); Push(b);
                        }
                        break;
                    case 'B':
                        if (IsInt(Peek())) RunMacro("ss ~ c;v( [s;vN) {+;)cm sdsd ,d"); // batch
                        else if (IsArray(Peek())) RunMacro("c1tsh"); // uncons-right
                        else throw new Exception("Bad type for B");
                        break;
                    case 'c': // copy
                        Push(Peek());
                        break;
                    case 'C':
                        if (IsTruthy(Pop())) yield return ExecutionState.CancelState;
                        break;
                    case 'd': // discard
                        Pop();
                        break;
                    case 'e': // eval, but only when not at the very beginning of the program
                        if (CallStackFrames.Any() || ip > 0) DoEval();
                        break;
                    case 'E': // explode (de-listify)
                        DoExplode();
                        break;
                    case 'f': // block filter
                        { 
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoFilter(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'F': // for loop
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoFor(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'g': // generator
                        {
                            // shorthand is indicated by
                            //   no trailing block
                            //   OR trailing block with explicit close }, in which case it becomes a filter
                            bool shorthand = !IsBlock(Peek()) || (ip >= 1 && program[ip - 1] == '}');
                            foreach (var s in DoGenerator(shorthand, program[++ip], block.SubBlock(ip + 1))) {
                                yield return s;
                            }
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'h':
                        if (IsInt(Peek())) RunMacro("2/"); // half
                        else if (IsFrac(Peek())) Push(Pop().Num); // numerator
                        else if (IsArray(Peek())) Push(Pop()[0]); // head
                        break;
                    case 'H':
                        if (IsInt(Peek())) Push(Pop() * 2); // un-half
                        else if (IsFrac(Peek())) Push(Pop().Den); // denominator
                        else if (IsArray(Peek())) Push(Peek()[Pop().Count - 1]); // last
                        break;
                    case 'i': // iteration index
                        if (CallStackFrames.Any()) Push(Index);
                        break;
                    case 'I': // get index
                        DoFindIndex();
                        break;
                    case 'j': 
                        if (IsArray(Peek())) RunMacro("' /"); // un-join with spaces
                        break;
                    case 'J':
                        if (IsArray(Peek())) RunMacro("' *"); // join with spaces
                        else if (IsNumber(Peek())) Push(Peek() * Pop()); // square
                        break;
                    case 'k': // reduce
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoReduce(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'l': // listify-n
                        DoListifyN();
                        break;
                    case 'L': // listify stack
                        DoListify();
                        break;
                    case 'm': // do map
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoMap(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'M': // transpose
                        DoTranspose();
                        break;
                    case 'N':
                        if (IsNumber(Peek())) Push(-Pop()); // negate
                        else if (IsArray(Peek())) RunMacro("c1TsH"); // uncons
                        else throw new Exception("Bad type for N");
                        break;
                    case 'O': // order
                        foreach (var s in DoOrder()) yield return s;
                        break;
                    case 'p': // print inline
                        Print(Pop(), false);
                        break;
                    case 'P': // print
                        Print(Pop());
                        break;
                    case 'q': // shy print inline
                        Print(Peek(), false);
                        break;
                    case 'Q': // print
                        Print(Peek());
                        break;
                    case 'r': // 0 range
                        if (IsInt(Peek())) Push(Range(0, Pop()));
                        else if (IsArray(Peek())) {
                            var result = new List<object>(Pop());
                            result.Reverse();
                            Push(result);
                        }
                        else throw new Exception("Bad type for r");
                        break;
                    case 'R': // 1 range
                        if (IsInt(Peek())) Push(Range(1, Pop()));
                        else { // regex replace
                            foreach (var s in DoRegexReplace()) yield return s;
                        }
                        break;
                    case 's': // swap
                        {
                            dynamic top = Pop(), bottom = Pop();
                            Push(top);
                            Push(bottom);
                        }
                        break;
                    case 't': // trim left
                        if (IsArray(Peek())) Push(S2A(A2S(Pop()).TrimStart()));
                        else if (IsInt(Peek())) RunMacro("ss~ c%,-0|M)");
                        else throw new Exception("Bad types for trimleft");
                        break;
                    case 'T': // trim right
                        if (IsArray(Peek())) Push(S2A(A2S(Pop()).TrimEnd()));
                        else if (IsInt(Peek())) RunMacro("ss~ c%,-0|M(");
                        else throw new Exception("Bad types for trimright");
                        break;
                    case 'u': // unique
                        DoUnique();
                        break;
                    case 'U': // negative Unit
                        Push(BigInteger.MinusOne);
                        break;
                    case 'V': // constant value
                        Push(Constants[program[++ip]]);
                        break;
                    case 'w': // do-while
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoWhile(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case 'W':
                        {
                            bool shorthand = !IsBlock(Peek());
                            foreach (var s in DoPreCheckWhile(block.SubBlock(ip + 1))) yield return s;
                            if (shorthand) ip = program.Length;
                        }
                        break;
                    case '_':
                        Push(_);
                        break;
                    case 'x': // read;
                        Push(X);
                        break;
                    case 'X': // write
                        X = Peek();
                        break;
                    case 'y': // read;
                        Push(Y);
                        break;
                    case 'Y': // write
                        Y = Peek();
                        break;
                    case 'z': // zero-length;
                        Push(S2A(""));
                        break;
                    case '|': // extended operations
                        switch (program[++ip]) {
                            case ' ':
                                Print(" ", false);
                                break;
                            case '`':
                                DoDump();
                                break;
                            case '%': // div mod
                                RunMacro("ssb%~/,");
                                break;
                            case '+': // sum
                                RunMacro("0s{+F");
                                break;
                            case '-': // deltas
                                RunMacro("2B{Es-m");
                                break;
                            case '~': // bitwise not
                                Push(~Pop());
                                break;
                            case '&': 
                                if (IsArray(Peek())) RunMacro("ss~ {;sIU>f ,d"); // intersection
                                else Push(Pop() & Pop()); // bitwise and
                                break;
                            case '|': // bitwise or
                                Push(Pop() | Pop());
                                break;
                            case '^': 
                                if (IsArray(Peek())) RunMacro("s b-~ s-, +"); // symmetric diff
                                else Push(Pop() ^ Pop()); // bitwise xor
                                break;
                            case '*':
                                if (IsInt(Peek())) { 
                                    dynamic b = Pop();
                                    if (IsInt(Peek())) { // exponent
                                        Push(BigInteger.Pow(Pop(), (int)b));
                                        break;
                                    }
                                    else if (IsFrac(Peek())) { // fraction power
                                        dynamic a = Pop();
                                        var result = new Rational(1, 1);
                                        for (int i = 0; i < b; i++) result *= a;
                                        Push(result);
                                        break;
                                    }
                                    else if (IsArray(Peek())) { // repeat element
                                        var result = new List<object>();
                                        foreach (var e in Pop()) result.AddRange(Enumerable.Repeat((object)e, (int)b));
                                        Push(result);
                                        break;
                                    }
                                }
                                else if (IsArray(Peek())) {
                                    dynamic B = Pop(), A = Pop(); // cross product
                                    var result = new List<object>();
                                    foreach (var a in A) foreach (var b in B) result.Add(new List<object> { a, b });
                                    Push(result);
                                    break;
                                }
                                throw new Exception("Bad types for |*");
                            case '/': // repeated divide
                                RunMacro("ss~;*{;/c;%!w,d");
                                break;
                            case ')': // rotate right
                                DoRotate(RotateDirection.Right);
                                break;
                            case '(': // rotate left
                                DoRotate(RotateDirection.Left);
                                break;
                            case '[': // prefixes
                                RunMacro("~;%R{;s(m,d");
                                break;
                            case ']': // suffixes
                                RunMacro("~;%R{;s)mr,d");
                                break;
                            case '<': // shift left
                                RunMacro("|2*");
                                break;
                            case '>': // shift right
                                RunMacro("|2/");
                                break;
                            case '1': // -1-power
                                RunMacro("2%U1?");
                                break;
                            case '2': // 2-power
                                RunMacro("2s|*");
                                break;
                            case '3': // base 36
                                RunMacro("36|b");
                                break;
                            case 'a': // absolute value
                                Push(BigInteger.Abs(Pop()));
                                break;
                            case 'A': // 10 ** x
                                Push(BigInteger.Pow(10, (int)Pop()));
                                break;
                            case 'b': // base convert
                                DoBaseConvert();
                                break;
                            case 'B': // binary convert
                                RunMacro("2|b");
                                break;
                            case 'd': // depth of stack
                                Push(new BigInteger(MainStack.Count));
                                break;
                            case 'D': // depth of side stack
                                Push(new BigInteger(InputStack.Count));
                                break;
                            case 'e': // is even
                                Push(Pop() % 2 ^ 1);
                                break;
                            case 'E': // base digital explode
                                DoBaseConvert(false);
                                break;
                            case 'f':
                                if (IsInt(Peek())) Push(PrimeFactors(Pop())); // prime factorize
                                else if (IsArray(Peek())) DoRegexFind(); // regex find all matches
                                break;
                            case 'g': // gcd
                                DoGCD();
                                break;
                            case 'H': // hex convert
                                RunMacro("16|b");
                                break;
                            case 'i': // outer loop index
                                Push(IndexOuter);
                                break;
                            case 'I': // find all indexes
                                foreach (var s in DoFindIndexAll()) yield return s;
                                break;
                            case 'l': // lcm
                                if (IsArray(Peek())) RunMacro("1s{|lF");
                                else if (IsInt(Peek())) RunMacro("b|g~*,/");
                                else throw new Exception("Bad type for lcm");
                                break;
                            case 'J': // join with newlines
                                RunMacro("Vn*");
                                break;
                            case 'm': // min
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) < 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("chs{|mF");
                                else throw new Exception("Bad types for min");
                                break;
                            case 'M': // max
                                if (IsNumber(Peek())) {
                                    dynamic b = Pop(), a = Pop();
                                    Push(Comparer.Instance.Compare(a, b) > 0 ? a : b);
                                }
                                else if (IsArray(Peek())) RunMacro("chs{|MF");
                                else throw new Exception("Bad types for max");
                                break;
                            case 'p':
                                if (IsInt(Peek())) RunMacro("|f%1="); // is prime
                                else if (IsArray(Peek())) RunMacro("cr1t+"); // palindromize
                                break;
                            case 'P': // print blank newline
                                Print("");
                                break;
                            case 'r': // start-end range
                                {
                                    dynamic end = Pop(), start = Pop();
                                    if (IsArray(end)) end = new BigInteger(end.Count);
                                    if (IsArray(start)) start = new BigInteger(-start.Count);
                                    Push(Range(start, end - start));
                                    break;
                                }
                            case 'R': // start-end-stride range
                                {
                                    int stride = (int)Pop(), end = (int)Pop(), start = (int)Pop();
                                    Push(Enumerable.Range(0, end - start).Select(n => n * stride + start).TakeWhile(n => n < end).Select(n => new BigInteger(n) as object).ToList());
                                    break;
                                }
                            case 's': // regex split
                                DoRegexSplit();
                                break;
                            case 'S': // surround with
                                DoSurround();
                                break;
                            case 'q': // int square root
                                Push(new BigInteger(Math.Sqrt(Math.Abs((double)Pop()))));
                                break;
                            case 't': // translate
                                DoTranslate();
                                break;
                            case 'x': // decrement X, push
                                Push(--X);
                                break;
                            case 'X': // increment X, push
                                Push(++X);
                                break;
                            case 'z': // zero-fill
                                RunMacro("ss ~; '0* s 2l$ ,)");
                                break;
                            default: throw new Exception($"Unknown extended character '{program[ip]}'");
                        }
                        break;
                    default: throw new Exception($"Unknown character '{program[ip]}'");
                }
                ++ip;
            }
            yield return new ExecutionState();
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

        private IEnumerable<ExecutionState> DoGenerator(bool shorthand, char spec, Block rest) {
            /*
             *  End condition
	         *      duplicate -    u
	         *      n reached -    n
	         *      filter false - f
	         *      cancelled -	   c
             *      invariant pt - i
             *      target value - t
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
             *  0{filter}{project}gn
             *   {filter}{project}g9
             *  t{filter}{project}gt
	         *           {project}gu
	         *           {project}gi
	         *           {project}gc
	         *  0        {project}gn
	         *           {project}g9
	         *  t        {project}gt
             *   {filter}{project}gU
             *   {filter}{project}gI
             *   {filter}{project}gF
             *   {filter}{project}gC
             *  0{filter}{project}gN
             *   {filter}{project}g(
             *  t{filter}{project}gT
	         *           {project}gU
	         *           {project}gI
	         *           {project}gC
	         *  0        {project}gN
	         *           {project}g(
	         *  t        {project}gT
             *           
             *           gu project
             *           gi project
             *           gc project
             *         0 gn project
             *           g9 project
             *         t gt project
             *           gU project
             *           gI project
             *           gC project
             *         0 gN project
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
                postPop = char.IsUpper(spec);
            Block genblock = shorthand ? rest : Pop(), 
                filter = null;
            dynamic targetVal = null;
            int? targetCount = null;

            if (IsBlock(Peek())) filter = Pop();
            else if (stopOnFilter) throw new Exception("generator can't stop on filter failure when there is no filter");

            if (stopOnTargetVal) targetVal = Pop();

            if (char.ToLower(spec) == 'n') {
                targetCount = (int)Pop();
            }
            else {
                int idx = "1234567890!@#$%^&*()".IndexOf(spec);
                if (idx >= 0) targetCount = idx % 10 + 1;
                postPop = idx >= 10;
            }

            if (!stopOnDupe && !stopOnFilter && !stopOnCancel && !stopOnFixPoint && !stopOnTargetVal && !targetCount.HasValue) {
                throw new Exception("no end condition for generator");
            } 

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
                    foreach (var s in RunSteps(genblock)) {
                        if (s.Cancel && stopOnCancel) goto GenComplete;
                        if (s.Cancel) goto Cancelled;
                        yield return s;
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
                if (passed) { // check for dupe
                    if (stopOnDupe && result.Contains(generated, Comparer.Instance)) break;
                    if (stopOnFixPoint && AreEqual(generated, lastGenerated)) break;
                    result.Add(generated);
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

            if (shorthand) foreach (var e in result) Print(e);
            else Push(result);
        }

        enum RotateDirection { Left, Right };
        private void DoRotate(RotateDirection dir) {
            dynamic arr, distance = Pop();
            if (IsArray(distance)) {
                arr = distance;
                distance = BigInteger.One;
            }
            else {
                arr = Pop();
            }

            if (IsArray(arr) && IsInt(distance)) {
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
                throw new Exception("Bad types for rotate");
            }
        }

        private void DoListify() {
            var newList = new List<object>();
            while (TotalStackSize > 0) newList.Add(Pop());
            Push(newList);
        }

        private void DoListifyN() {
            var n = Pop();

            if (IsFrac(n)) {
                Push(new List<object> { n.Num, n.Den });
            }
            else if (IsInt(n)) {
                var result = new List<object>();
                for (int i = 0; i < n; i++) result.Insert(0, Pop());
                Push(result);
            }
            else {
                throw new Exception("bad type for listify n");
            }
        }

        private void DoZipRepeat() {
            dynamic b = Pop(), a = Pop();

            if (!IsArray(a) && !IsArray(b)) {
                Push(new List<object> { a, b });
                return;
            }

            if (!IsArray(a)) a = new List<object> { a };
            if (!IsArray(b)) b = new List<object> { b };

            var result = new List<object>();
            int size = Math.Max(a.Count, b.Count);
            for (int i = 0; i < size; i++) {
                result.Add(new List<object> { a[i%a.Count], b[i%b.Count] });
            }
            Push(result);
        }

        // not an eval of stax code, but a json-like data parse
        private void DoEval() {
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
                        int endPos;
                        for (endPos = i + 1; endPos < arg.Length && char.IsDigit(arg[endPos]); endPos++);
                        NewValue(BigInteger.Parse(arg.Substring(i, endPos - i)));
                        i = endPos - 1;
                        break;
                    case ' ': case '\t': case '\r': case '\n': case ',':
                        break;
                    default: throw new Exception($"Bad char {arg[i]} during eval");
                }
            }
        }

        private void DoRegexFind() {
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new Exception("Bad types for find");
            string ts = A2S(text), ss = A2S(search);

            var result = new List<object>();
            foreach (Match m in Regex.Matches(ts, ss)) result.Add(S2A(m.Value));
            Push(result);
        }

        private void DoRegexSplit() {
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new Exception("Bad types for replace");
            string ts = A2S(text), ss = A2S(search);

            Push(Regex.Split(ts, ss, RegexOptions.ECMAScript).Select(S2A).Cast<object>().ToList());
        }

        private IEnumerable<ExecutionState> DoRegexReplace() {
            var replace = Pop();
            var search = Pop();
            var text = Pop();

            if (!IsArray(text) || !IsArray(search)) throw new Exception("Bad types for replace");
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
                throw new Exception("Bad types for replace");
            }
        }

        private void DoTranslate() {
            var translation = Pop();
            var input = Pop();

            if (IsInt(input)) input = new List<object> { input };

            if (IsArray(input) && IsArray(translation)) {
                var result = new List<object>();
                var map = new Dictionary<char, char>();
                var ts = A2S(translation);

                for (int i = 0; i < ts.Length; i += 2) map[ts[i]] = ts[i + 1];
                foreach (var e in A2S(input)) result.AddRange(S2A("" + (map.ContainsKey(e) ? map[e] : e)));
                Push(result);
            }
            else {
                throw new Exception("Bad types for translate");
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

        private void DoUnique() {
            var arg = Pop();

            if (IsArray(arg)) {
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
                Push(new Rational(1, arg));
            }
            else if (IsFrac(arg)) { // upside down
                Push(1 / arg);
            }
            else {
                throw new Exception("Bad type for unique");
            }
        }

        private void DoLessThan() {
            dynamic b = Pop(), a = Pop();
            Push(Comparer.Instance.Compare(a, b) < 0 ? BigInteger.One : BigInteger.Zero);
        }

        private void DoGreaterThan() {
            dynamic b = Pop(), a = Pop();
            Push(Comparer.Instance.Compare(a, b) > 0 ? BigInteger.One : BigInteger.Zero);
        }

        private IEnumerable<ExecutionState> DoOrder() {
            var arg = Pop();

            if (IsArray(arg)) {
                var result = new List<object>(arg);
                result.Sort(Comparer.Instance);
                Push(result);
            }
            else if (IsBlock(arg)) {
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
                throw new Exception("Bad types for order");
            }
        }

        private void DoBaseConvert(bool stringRepresentation = true) {
            int @base = (int)Pop();
            var number = Pop();

            if (IsInt(number)) {
                long n = (long)number;
                var result = new List<object>();
                do {
                    BigInteger digit = n % @base;
                    if (stringRepresentation) {
                        char d = "0123456789abcdefghijklmnopqrstuvwxyz"[(int)digit];
                        result.Insert(0, new BigInteger(d + 0));
                    }
                    else { //digit mode
                        result.Insert(0, digit);
                    }
                    n /= @base;
                } while (n > 0);

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
                throw new Exception("Bad types for base convert");
            }
        }

        private IEnumerable<ExecutionState> DoFindIndexAll() {
            dynamic target = Pop(), list = Pop();
            if (!IsArray(list)) throw new Exception("Bad types for find index all");

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
                throw new Exception("Bad types for get-index");
            }
        }

        private void DoExplode() {
            var arg = Pop();

            if (IsArray(arg)) {
                foreach (var item in arg) Push(item);
            }
            else if (IsFrac(arg)) {
                Push(arg.Num);
                Push(arg.Den);
            }
            else if (IsInt(arg)) {
                var result = new List<object>();
                foreach (var c in (string)(BigInteger.Abs(arg).ToString())) {
                    result.Add(new BigInteger(c - '0'));
                }
                Push(result);
            }
        }

        private void DoAssignIndex() {
            dynamic element = Pop(), indexes = Pop(), list = Pop();

            if (IsInt(indexes)) indexes = new List<object> { indexes };

            if (IsArray(list)) {
                var result = new List<object>(list);
                foreach (int index in indexes) {
                    result[((index % result.Count) + result.Count) % result.Count] = element;
                }
                Push(result);
            }
            else {
                throw new Exception("Bad type for index assign");
            }

        }

        private void DoAt() {
            var top = Pop();

            if (IsFrac(top)) { // floor
                Push(top.Floor());
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
                    var result = new List<object>();
                    foreach (var idx in top) result.Add(ReadAt(list, (int)idx));
                    Push(result);
                    return;
                }
                else if (IsInt(top)) {
                    Push(ReadAt(list, (int)top));
                    return;
                }
            }
            throw new Exception("Bad type for at");
        }

        private void DoPadLeft() {
            dynamic b = Pop(), a = Pop();

            if (IsInt(a)) (a, b) = (b, a);

            if (IsArray(a) && IsInt(b)) {
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.InsertRange(0, Enumerable.Repeat((object)new BigInteger(32), (int)b - a.Count));
                if (a.Count > b) a.RemoveRange(0, a.Count - (int)b);
                Push(a);
            }
            else {
                throw new Exception("bad types for padleft");
            }
        }

        private void DoPadRight() {
            dynamic b = Pop(), a = Pop();

            if (IsArray(b)) (a, b) = (b, a);

            if (IsInt(a)) a = ToString(a);

            if (IsArray(a) && IsInt(b)) {
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.AddRange(Enumerable.Repeat((object)new BigInteger(32), (int)b - a.Count));
                if (a.Count > b) a.RemoveRange((int)b, a.Count - (int)b);
                Push(a);
            }
            else {
                throw new Exception("bad types for padright");
            }
        }

        private IEnumerable<ExecutionState> DoPreCheckWhile(Block rest) {
            if (!IsBlock(Peek())) {
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

            Block block = Pop();
            PushStackFrame();
                
            while (true) {
                foreach (var s in RunSteps(block)) {
                    if (s.Cancel) {
                        PopStackFrame();
                        yield break;
                    }
                    yield return s;
                }
                ++Index;
            }
        }

        private IEnumerable<ExecutionState> DoWhile(Block rest) {
            if (!IsBlock(Peek())) {
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

            Block block = Pop();
            PushStackFrame();
            do {
                foreach (var s in RunSteps(block)) {
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

        private IEnumerable<ExecutionState> DoFilter(Block rest) {
            if (IsInt(Peek())) { // n times do
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
                throw new Exception("Bad types for filter");
            }
        }

        private IEnumerable<ExecutionState> DoReduce(Block rest) {
            dynamic b = Pop(), a = Pop();
            if (IsInt(a) && IsBlock(b)) a = Range(1, a);
            if (IsArray(a) && IsBlock(b)) {
                if (a.Count < 2) {
                    Push(a);
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
                throw new Exception("Bad types for reduce");
            }
        }

        private IEnumerable<ExecutionState> DoFor(Block rest) {
            if (IsInt(Peek())) Push(Range(1, Pop()));
            if (IsArray(Peek())) {
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
            if (IsInt(a) && IsBlock(b)) a = Range(1, a);
            if (IsArray(a) && IsBlock(b)) {
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
                throw new Exception("Bad types for for");
            }
        }

        private void DoTranspose() {
            var list = Pop();
            var result = new List<object>();

            if (list.Count > 0 && !IsArray(list[0])) list = new List<object> { list };

            int? count = null;
            foreach (var series in list) count = Math.Min(count ?? int.MaxValue, series.Count);

            for (int i = 0; i < (count ?? 0); i++) {
                var tuple = new List<object>();
                foreach (var series in list) tuple.Add(series[i]);
                result.Add(tuple);
            }

            Push(result);
        }

        private IEnumerable<ExecutionState> DoMap(Block rest) {
            if (IsInt(Peek())) {
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
                throw new Exception("bad type for map");
            }
        }

        private void DoPlus() {
            if (TotalStackSize < 2) return;
            dynamic b = Pop(), a = Pop();

            if (IsNumber(a) && IsNumber(b)) {
                Push(a + b);
            }
            else if (IsArray(a) && IsArray(b)) {
                var result = new List<object>(a);
                result.AddRange(b);
                Push(result);
            }
            else if (IsArray(a)) {
                Push(new List<object>(a) { b });
            }
            else if (IsArray(b)) {
                var result = new List<object> { a };
                result.AddRange(b);
                Push(result);
            }
            else {
                throw new Exception("Bad types for +");
            }
        }

        private void DoMinus() {
            dynamic b = Pop(), a = Pop();

            if (IsArray(a) && IsArray(b)) {
                a = new List<object>(a);
                var bl = (List<object>)b;
                a.RemoveAll((Predicate<object>)(e => bl.Contains(e, Comparer.Instance)));
                Push(a);
            }
            else if (IsArray(a)) {
                a = new List<object>(a);
                a.RemoveAll((Predicate<object>)(e => AreEqual(e, b)));
                Push(a);
            }
            else if (IsNumber(a) && IsNumber(b)) {
                Push(a - b);
            }
            else {
                throw new Exception("Bad types for -");
            }
        }

        private void DoSlash() {
            dynamic b = Pop(), a = Pop();

            if (IsNumber(a) && IsNumber(b)) {
                if (IsInt(a) && IsInt(b) && a < 0) {
                    Push((a - b + 1) / b); // int division is floor always
                }
                else {
                    Push(a / b);
                }
            }
            else if (IsArray(a) && IsInt(b)) {
                var result = new List<object>();
                for (int i = 0; i < a.Count; i += (int)b) {
                    result.Add(((IEnumerable<object>)a).Skip(i).Take((int)b).ToList());
                }
                Push(result);
            }
            else if (IsArray(a) && IsArray(b)) {
                string[] strings = A2S(a).Split(new string[] { A2S(b) }, 0);
                Push(strings.Select(s => S2A(s) as object).ToList());
            }
            else {
                throw new Exception("Bad types for /");
            }
        }

        private void DoPercent() {
            var b = Pop();
            if (IsArray(b)) {
                Push((BigInteger)b.Count);
                return;
            }

            var a = Pop();
            if (IsInt(a) && IsInt(b)) {
                BigInteger result = a % b;
                if (result < 0) result += b;
                Push(result);
            }
            else {
                throw new Exception("Bad types for %");
            }
        }

        private IEnumerable<ExecutionState> DoStar() {
            if (TotalStackSize < 2) yield break;
            dynamic b = Pop(), a = Pop();

            if (IsInt(a)) (a, b) = (b, a);

            if (IsInt(b)) {
                if (IsArray(a)) {
                    if (b < 0) {
                        a.Reverse();
                        b *= -1;
                    }
                    var result = new List<object>();
                    for (int i = 0; i < b; i++) result.AddRange(a);
                    Push(result);
                    yield break;
                }
                else if (IsBlock(a)) {
                    PushStackFrame();
                    for (Index = 0; Index < b; Index++) {
                        foreach (var s in RunSteps((Block)a)) yield return s;
                    }
                    PopStackFrame();
                    yield break;
                }
            }

            if (IsArray(a) && IsArray(b)) {
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
                Push(a * b);
                yield break;
            }

            throw new Exception("Bad types for *");
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

            throw new Exception("Bad types for GCD");
        }

        #region support
        private object ToNumber(dynamic arg) {
            if (IsArray(arg)) {
                return BigInteger.Parse(A2S(arg));
            }
            throw new Exception("Bad type for ToNumber");
        }

        private List<object> ToString(dynamic arg) {
            if (IsInt(arg)) {
                return S2A(arg.ToString());
            }
            else if (IsArray(arg)) {
                var result = new StringBuilder();
                foreach (var e in arg) result.Append(IsInt(e) ? e : A2S(e));
                return S2A(result.ToString());
            }
            throw new Exception("Bad type for ToString");
        }

        private bool AreEqual(dynamic a, dynamic b) => Comparer.Instance.Compare(a, b) == 0;

        private static bool IsInt(object b) => b is BigInteger;
        private static bool IsFrac(object b) => b is Rational;
        private static bool IsNumber(object b) => IsInt(b) || IsFrac(b);
        private static bool IsArray(object b) => b is List<object>;
        private static bool IsBlock(object b) => b is Block;
        private static bool IsTruthy(dynamic b) => (IsNumber(b) && b != 0) || (IsArray(b) && b.Count != 0);

        private static List<object> S2A(string arg) => arg.ToCharArray().Select(c => (BigInteger)(int)c as object).ToList();
        private static string A2S(List<object> arg) {
            return string.Concat(arg.Select(e => IsInt(e)
                ? ((char)(int)((BigInteger)e & ushort.MaxValue)).ToString()
                : A2S((List<object>)e)));
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
            BigInteger value = 0;

            while (ip < program.Length && char.IsDigit(program[ip]))
                value = value * 10 + program[ip++] - '0';

            if (ip < program.Length && program[ip] == '.') {
                ++ip;
                return double.Parse(value + "." + ParseNumber(program, ref ip));
            }
            --ip;
            return value;
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
            implicitEnd = ip == program.Length - 1;
            return S2A(result);
        }

        private Block ParseBlock(Block block, ref int ip) {
            string contents = block.Contents;
            int depth = 0;
            int start = ip + 1;
            do {
                if (contents[ip] == '|' || contents[ip] == '\'' || contents[ip] == 'V') {
                    ip++; // 2-char tokens
                    continue;
                }

                if (contents[ip] == '"') {
                    ParseString(contents, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '.') {
                    ParseCompressedString(contents, ref ip, out bool implicitEnd);
                    continue;
                }

                if (contents[ip] == '{') ++depth;
                if (contents[ip] == '}' && --depth == 0) return block.SubBlock(start, ip);

                // shortcut block terminators
                if ("wWmfFkgO".Contains(contents[ip]) && --depth == 0) return block.SubBlock(start, ip--);
            } while (++ip < contents.Length);
            --ip;
            return block.SubBlock(start);
        }

        class Comparer : IComparer<object>, IEqualityComparer<object> {
            public static readonly Comparer Instance = new Comparer();

            private Comparer() { }

            public int Compare(dynamic a, dynamic b) {
                if (a == null || b == null) return object.ReferenceEquals(a, b);
                if (IsNumber(a)) {
                    while (IsArray(b) && b.Count > 0) b = ((IList<object>)b)[0];
                    if (IsNumber(b)) return ((IComparable)a).CompareTo(b);
                    return a.GetType().Name.CompareTo(b.GetType().Name);
                }
                if (IsNumber(b)) {
                    while (IsArray(a) && a.Count > 0) a = ((IList<object>)a)[0];
                    if (IsNumber(b)) return ((IComparable)a).CompareTo(b);
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
