using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StaxLang {
    public class Executor {
        public TextWriter Output { get; private set; }

        private long Index = 0L; // loop iteration
        private dynamic X = 0L; // register
        private dynamic Y = 0L; // register
        private dynamic Z = 0L; // register
        private dynamic _ = 0L; // implicit iterator

        public Executor(TextWriter output = null) {
            this.Output = output ?? Console.Out;
        }

        public void Run(string program, string[] input) {
            this.Z = input.LongLength;

            if (input.Length > 0) {
                this.Y = input[0];
                double d;
                if (double.TryParse(input[0], out d)) this.X = d;
                long l;
                if (long.TryParse(input[0], out l)) this.X = l;
            }

            var stack = new Stack<dynamic>(input.Reverse());
            try {
                Run(program, stack);
                stack.Reverse().ToList().ForEach(e => Print(e));
            } 
            catch (InvalidOperationException) { } 
            catch (ArgumentOutOfRangeException) { }
        }

        private void Run(string program, Stack<dynamic> stack) {
            int ip = 0;

            while (ip < program.Length) {
                if (char.IsDigit(program[ip])) {
                    stack.Push(ParseNumber(program, ref ip));
                } else if (char.IsWhiteSpace(program[ip])) {
                    ++ip;
                } else switch (program[ip]) {
                        case '`':
                            ++ip;
                            Debugger.Break();
                            break;
                        case '"': // "literal"
                            stack.Push(ParseString(program, ref ip));
                            break;
                        case '\'': // single char 'x
                            stack.Push(program.Substring(ip + 1, 1));
                            ip += 2;
                            break;
                        case '{': // block
                            stack.Push(ParseBlock(program, ref ip));
                            break;
                        case '!': // not
                            ++ip;
                            stack.Push(IsTruthy(stack.Pop()) ? 0L : 1L);
                            break;
                        case '+':
                            ++ip;
                            DoPlus(stack);
                            break;
                        case '-':
                            ++ip;
                            DoMinus(stack);
                            break;
                        case '*':
                            ++ip;
                            DoStar(stack);
                            break;
                        case '/':
                            ++ip;
                            DoSlash(stack);
                            break;
                        case '%':
                            ++ip;
                            DoPercent(stack);
                            break;
                        case '@': // read index
                            ++ip;
                            DoReadIndex(stack);
                            break;
                        case '&': // assign index
                            ++ip;
                            DoAssignIndex(stack);
                            break;
                        case ':': // copy 2nd
                            ++ip; 
                            {
                                var top = stack.Pop();
                                stack.Push(stack.Peek());
                                stack.Push(top);
                            }
                            break;
                        case '#': // to number
                            ++ip;
                            stack.Push(ToNumber(stack.Pop()));
                            break;
                        case '$': // to string
                            ++ip;
                            stack.Push(stack.Pop().ToString());
                            break;
                        case '=':
                            ++ip;
                            DoEqual(stack);
                            break;
                        case 'v':
                            ++ip;
                            if (IsNumber(stack.Peek())) stack.Push(stack.Pop() - 1); // decrement
                            else if (IsString(stack.Peek())) stack.Push("" + (char)(stack.Pop()[0] - 1)); // dec char
                            else if (IsArray(stack.Peek())) stack.Peek().RemoveAt(stack.Peek().Count - 1); // drop-last
                            break;
                        case '^':
                            ++ip;
                            if (IsNumber(stack.Peek())) stack.Push(stack.Pop() + 1); // increment
                            else if (IsString(stack.Peek())) stack.Push("" + (char)(stack.Pop()[0] + 1)); // inc char
                            else if (IsArray(stack.Peek())) stack.Peek().RemoveAt(0); // drop-first
                            break;
                        case '(':
                            ++ip;
                            PadRight(stack);
                            break;
                        case ')':
                            ++ip;
                            PadLeft(stack);
                            break;
                        case '?': // if
                            ++ip;
                            DoIf(stack);
                            break;
                        case 'a':
                            ++ip;
                            stack.Push("abcdefghijklmnopqrstuvwxyz");
                            break;
                        case 'A': // 10
                            ++ip;
                            stack.Push(10L);
                            break;
                        case 'c': // copy
                            ++ip;
                            stack.Push(stack.Peek());
                            break;
                        case 'C': // dig
                            ++ip;
                            stack.Push(stack.ElementAt((int)stack.Pop()));
                            break;
                        case 'd': // discard
                            ++ip;
                            stack.Pop();
                            break;
                        case 'D': // do-over
                            ip = 0;
                            break;
                        case 'e': // empty string
                            ++ip;
                            stack.Push("");
                            break;
                        case 'E': // explode (de-listify)
                            ++ip;
                            DoExplode(stack);
                            break;
                        case 'f': // filter
                            ++ip;
                            DoFilter(stack);
                            break;
                        case 'F': // for loop
                            ++ip;
                            DoFor(stack);
                            break;
                        case 'h': 
                            ++ip;
                            if (IsNumber(stack.Peek())) stack.Push(stack.Pop() / 2); // half
                            if (IsString(stack.Peek())) stack.Push(stack.Pop().ToLower()); // lower
                            if (IsArray(stack.Peek())) stack.Push(stack.Pop()[0]); // head
                            break;
                        case 'H': 
                            ++ip;
                            if (IsNumber(stack.Peek())) stack.Push(stack.Pop() * 2); // double
                            if (IsString(stack.Peek())) stack.Push(stack.Pop().ToUpper()); // upper
                            if (IsArray(stack.Peek())) stack.Push(stack.Peek()[stack.Pop().Count - 1]); // tail
                            break;
                        case 'i': // iteration index
                            ++ip;
                            stack.Push(this.Index);
                            break;
                        case 'I': // get index
                            ++ip;
                            DoGetIndex(stack);
                            break;
                        case 'l': // listify string or n elements
                            ++ip;
                            DoListify(stack);
                            break;
                        case 'L': // listify stack
                            ++ip;
                            var newList = stack.ToList();
                            stack.Clear();
                            stack.Push(newList);
                            break;
                        case 'n': // push newline
                            ++ip;
                            stack.Push("\n");
                            break;
                        case 'N': // print newline
                            ++ip;
                            Output.WriteLine();
                            break;
                        case 'm': // do map
                            ++ip;
                            DoMap(stack);
                            break;
                        case 'o': // shy output
                            ++ip;
                            Print(stack.Peek(), false);
                            break;
                        case 'O': // output
                            ++ip;
                            Print(stack.Pop(), false);
                            break;
                        case 'p': // shy print
                            ++ip;
                            Print(stack.Peek());
                            break;
                        case 'P': // print
                            ++ip;
                            Print(stack.Pop());
                            break;
                        case 'r': // 0 range
                            ++ip;
                            stack.Push(Enumerable.Range(0, (int)stack.Pop()).Select(Convert.ToInt64).Cast<object>().ToList());
                            break;
                        case 'R': // 1 range
                            ++ip;
                            stack.Push(Enumerable.Range(1, (int)stack.Pop()).Select(Convert.ToInt64).Cast<object>().ToList());
                            break;
                        case 's': // swap
                            ++ip; 
                            {
                                var top = stack.Pop();
                                var bottom = stack.Pop();
                                stack.Push(top);
                                stack.Push(bottom);
                            }
                            break;
                        case 'S': // space
                            ++ip;
                            stack.Push(" ");
                            break;
                        case 't': // trim left
                            ++ip;
                            stack.Push(((string)stack.Pop()).TrimStart());
                            break;
                        case 'T': // trim right
                            ++ip;
                            stack.Push(((string)stack.Pop()).TrimEnd());
                            break;
                        case 'w': // while
                            ++ip;
                            DoWhile(stack);
                            break;
                        case '_':
                            ++ip;
                            stack.Push(this._);
                            break;
                        case 'x': // read;
                            ++ip;
                            stack.Push(this.X);
                            break;
                        case 'X': // write
                            ++ip;
                            this.X = stack.Peek();
                            break;
                        case 'y': // read;
                            ++ip;
                            stack.Push(this.Y);
                            break;
                        case 'Y': // write
                            ++ip;
                            this.Y = stack.Peek();
                            break;
                        case 'z': // read;
                            ++ip;
                            stack.Push(this.Z);
                            break;
                        case 'Z': // write
                            ++ip;
                            this.Z = stack.Peek();
                            break;
                        default: throw new Exception($"Unknown character '{program[ip]}'");
                    }
            }
        }

        private void DoGetIndex(Stack<dynamic> stack) {
            var element = stack.Pop();
            var list = stack.Pop();

            if (IsString(list) || IsArray(list)) {
                stack.Push((long)list.IndexOf(element));
            }
            else {
                throw new Exception("Bad types for get-index");
            }
        }

        private void DoExplode(Stack<dynamic> stack) {
            var arg = stack.Pop();

            if (IsString(arg)) arg = String2Array(arg);

            if (IsArray(arg)) {
                foreach (var item in arg) stack.Push(item);
            }
        }

        private List<object> String2Array(string arg)  => arg.ToCharArray().Select(c => c + "" as object).ToList();

        private void DoAssignIndex(Stack<dynamic> stack) {
            var element = stack.Pop();
            var index = (int)stack.Pop();
            var list = stack.Pop();

            if (IsArray(list)) {
                var result = new List<object>(list);
                index = ((index % result.Count) + result.Count) % result.Count;
                result[index] = element;
                stack.Push(result);
            }
            else if (IsString(list)) {
                string toEmbed = element.ToString();
                index = ((index % list.Length) + list.Length) % list.Length;
                string result = list.Substring(0, index);
                result += toEmbed.Length + index > list.Length ? toEmbed.Substring(0, list.Length - index) : toEmbed;
                if (result.Length < list.Length) result += list.Substring(result.Length);
                stack.Push(result);
            }
            else {
                throw new Exception("Bad type for index assign");
            }

        }

        private void DoReadIndex(Stack<dynamic> stack) {
            int index = (int)stack.Pop();
            var list = stack.Pop();

            if (IsArray(list)) {
                stack.Push(list[index]);
            }
            else if (IsString(list)) {
                stack.Push(list[index].ToString());
            }
            else {
                throw new Exception("Bad type for index");
            }
        }

        private void DoListify(Stack<dynamic> stack) {
            var arg = stack.Pop();
            if (IsInt(arg)) {
                var list = new List<object>();
                long count = arg;
                for (int i = 0; i < count; i++) list.Insert(0, stack.Pop());
                stack.Push(list);
            }
            else if (IsString(arg)) {
                stack.Push(String2Array(arg));
            }
            else {
                throw new Exception("Bad type for listify");
            }
        }

        private void PadLeft(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a)) (a, b) = (b, a);

            if (IsString(a) && IsInt(b)) {
                var result = ((string)a).PadLeft((int)b);
                stack.Push(result.Substring(result.Length - (int)b));
                return;
            }

            if (IsArray(a) && IsInt(b)) {
                if (a.Count < b) a.InsertRange(0, Enumerable.Repeat((object)0L, b - a.Count));
                if (a.Count > b) a.RemoveRange(0, a.Count - (int)b);
                stack.Push(a);
                return;
            }

            throw new Exception("bad types for padleft");
        }

        private void PadRight(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a)) (a, b) = (b, a);

            if (IsString(a) && IsInt(b)) {
                var result = ((string)a).PadRight((int)b);
                stack.Push(result.Substring(0, (int)b));
                return;
            }

            if (IsArray(a) && IsInt(b)) {
                if (a.Count < b) a.AddRange(Enumerable.Repeat((object)0L, b - a.Count));
                if (a.Count > b) a.RemoveRange((int)b, a.Count - (int)b);
                stack.Push(a);
                return;
            }

            throw new Exception("bad types for padright");
        }

        private void DoWhile(Stack<dynamic> stack) {
            Block block = stack.Pop();
            do Run(block.Program, stack); while (IsTruthy(stack.Pop()));
        }

        private void DoIf(Stack<dynamic> stack) {
            bool condition = IsTruthy(stack.Pop());

            var els = stack.Pop();
            if (condition) {
                stack.Pop();
                stack.Push(els);
            }
        }

        private object ToNumber(dynamic arg) {
            if (IsString(arg)) {
                long n;
                if (long.TryParse(arg, out n)) return n;
                return double.Parse(arg);
            } else if (IsArray(arg)) {
                return (long)arg.Count;
            } else {
                throw new Exception("Bad type for ToNumber");
            }
        }

        private void Print(object arg, bool newline = true) {
            if (IsArray(arg)) {
                foreach (var e in (IEnumerable)arg) Print(e, newline);
            }
            else if (newline) {
                Output.WriteLine(arg);
            }
            else {
                Output.Write(arg);
            }
        }

        private void DoFilter(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(a) && IsBlock(b)) {
                var initial = (this._, this.Index);
                long i = 0;
                var result = new List<object>();
                foreach (var e in a) {
                    stack.Push(e);
                    this._ = e;
                    this.Index = i++;
                    Run(b.Program, stack);
                    if (IsTruthy(stack.Pop())) result.Add(e);
                }
                stack.Push(result);
                (this._, this.Index) = initial;
            } else {
                throw new Exception("Bad types for filter");
            }
        }

        private void DoFor(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsString(a)) a = String2Array(a);

            if (IsArray(a) && IsBlock(b)) {
                var initial = (this._, this.Index);
                long i = 0;
                foreach (var e in a) {
                    stack.Push(e);
                    this._ = e;
                    this.Index = i++;
                    Run(b.Program, stack);
                }
                (this._, this.Index) = initial;
            }
            else {
                throw new Exception("Bad types for for");
            }
        }

        private void DoMap(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(b)) (a, b) = (b, a);

            if (IsString(a)) a = String2Array(a);

            if (IsArray(a) && IsBlock(b)) {
                var initial = (this._, this.Index);
                long i = 0;
                var result = new List<object>();
                foreach (var e in a) {
                    stack.Push(e);
                    this._ = e;
                    this.Index = i++;
                    Run(b.Program, stack);
                    result.Add(stack.Pop());
                }
                stack.Push(result);
                (this._, this.Index) = initial;
            }
            else {
                throw new Exception("bad type for map");
            }
        }

        private void DoPlus(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a) && IsNumber(b)) {
                stack.Push(a + b);
            } else if (IsString(a) && IsString(b)) {
                stack.Push(a + b);
            } else if (IsArray(a) && IsArray(b)) {
                a.AddRange(b);
                stack.Push(a);
            } else {
                throw new Exception("Bad types for +");
            }
        }

        private void DoMinus(Stack<dynamic> stack) {
            var b = stack.Pop();
            if (IsString(b)) {
                string reversed = string.Concat(((string)b).Reverse());
                stack.Push(reversed);
                return;
            }

            if (IsArray(b)) {
                b.Reverse();
                stack.Push(b);
                return;
            }

            var a = stack.Pop();

            if (IsNumber(a) && IsNumber(b)) {
                stack.Push(a - b);
            } else {
                throw new Exception("Bad types for +");
            }
        }

        private void DoSlash(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a) && IsNumber(b)) {
                stack.Push(a / b);
            } else if (IsString(a) && IsInt(b)) {
                var result = new List<object>();
                for (int i = 0; i < a.Length; i += (int)b) result.Add(a.Substring(i, Math.Min((int)b, a.Length - i)));
                stack.Push(result);
            } else if (IsString(a) && IsString(b)) {
                stack.Push(((string)a).Split(new string[] { b }, 0).Cast<object>().ToList());
            } else {
                throw new Exception("Bad types for /");
            }
        }

        private void DoPercent(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a) && IsNumber(b)) {
                stack.Push(a % b);
            } else {
                throw new Exception("Bad types for %");
            }
        }

        private void DoStar(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsInt(a)) (a, b) = (b, a);

            if (IsInt(b)) {
                if (IsString(a)) {
                    stack.Push(string.Concat(Enumerable.Repeat(a, (int)b)));
                    return;
                } else if (IsArray(a)) {
                    var result = new List<object>();
                    for (int i = 0; i < b; i++) result.AddRange(a);
                    stack.Push(result);
                    return;
                } else if (IsBlock(a)) {
                    var initial = this.Index;
                    for (this.Index = 0; this.Index < b; this.Index++) Run(a.Program, stack);
                    this.Index = initial;
                    return;
                }
            }

            if (IsArray(a) && IsString(b)) {
                stack.Push(string.Join(b, a));
                return;
            }

            if (IsNumber(a) && IsNumber(b)) {
                stack.Push(a * b);
                return;
            }

            throw new Exception("Bad types for *");
        }

        private void DoEqual(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsNumber(a) && IsNumber(b) || IsString(a) && IsString(b)) {
                stack.Push(a == b ? 1L : 0L);
                return;
            }

            if (IsArray(a) && IsArray(b)) {
                stack.Push(Enumerable.SequenceEqual(a, b) ? 1L : 0L);
                return;
            }

            throw new Exception("Bad types for =");
        }

        private bool IsInt(object b) => b is long;
        private bool IsFloat(object b) => b is double;
        private bool IsNumber(object b) => b is long || b is double;
        private bool IsString(object b) => b is string;
        private bool IsArray(object b) => b is List<object>;
        private bool IsBlock(object b) => b is Block;
        private bool IsTruthy(dynamic b) => (!IsNumber(b) || b != 0) && (b as string)?.Length != 0 && (b as IList<object>)?.Count != 0;

        private object ParseNumber(string program, ref int ip) {
            long value = 0;

            while (ip < program.Length && char.IsDigit(program[ip]))
                value = value * 10 + program[ip++] - '0';

            if (ip < program.Length && program[ip] == '.') {
                ++ip;
                return double.Parse(value + "." + ParseNumber(program, ref ip));
            }
            return value;
        }

        private string ParseString(string program, ref int ip) {
            string result = "";
            while (ip < program.Length - 1 && program[++ip] != '"') {
                if (program[ip] == '\\') ++ip;
                result += program[ip];
            }
            if (ip < program.Length) ++ip; // final quote
            return result;
        }

        private Block ParseBlock(string program, ref int ip) {
            int depth = 0;
            int start = ip + 1;
            do {
                if (program[ip] == '{') ++depth;
                if (program[ip] == '}' && --depth == 0) return new Block(program.Substring(start, ip++ - start));

                // shortcut block terminators
                if ((program[ip] == 'w' || program[ip] == 'm' || program[ip] == 'f' || program[ip] == 'F') && --depth == 0) return new Block(program.Substring(start, ip - start));
            } while (++ip < program.Length);
            return new Block(program.Substring(start));
        }
    }
}
