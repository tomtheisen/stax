using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;

namespace StaxLang.Tests {
    [TestClass]
    public class Tests {
        internal static string[] MultiLineStrip(string arg) {
            var result = arg.Trim().Split('\n')
                .Select(line => line.TrimStart())
                .ToArray();

            if (result[0] == "") return Array.Empty<string>();
            return result;
        }

        internal void RunProgram(string source, string input = "", params string[] expected) {
            var writer = new StringWriter();
            new Executor(writer).Run(source, MultiLineStrip(input));
            string actual = writer.ToString();
            
            var expectedJoined = string.Concat(expected.Select(e => e + Environment.NewLine));
            Assert.AreEqual(expectedJoined, actual);
        }

        internal void RunProgramSingleInputs(string source, params string[] inputOutputs) {
            if (inputOutputs.Length % 2 != 0) throw new ArgumentException(nameof(inputOutputs));

            for (int i = 0; i < inputOutputs.Length; i+=2) {
                var input = inputOutputs[i];
                var expected = inputOutputs[i + 1] + Environment.NewLine;
                var writer = new StringWriter();
                new Executor(writer).Run(source, new[] { input });
                string actual = writer.ToString();

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void IntLiteral() {
            RunProgram("123", "", "123");
            RunProgram("999999999999999999999999999999999999999", "", "999999999999999999999999999999999999999");
        }

        [TestMethod]
        public void DoubleLiteral() {
            RunProgram("1.23", "", "1.23");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TooManyDecimals() {
            RunProgram("1.23.45", "");
        }

        [TestMethod]
        public void StringLiteral() {
            RunProgram("\"hello\"", "", "hello");
        }

        [TestMethod]
        public void UnterminatedString() {
            RunProgram("\"asdf", "", "asdf");
        }

        [TestMethod]
        public void EscapedString() {
            RunProgram("\"a`\"b\"", "", "a\"b");
        }

        [TestMethod]
        public void DigTest() {
            RunProgram("'a'b'c'd 2C LrS", "", "a", "b", "c", "d", "b");
        }

        [TestMethod]
        public void AdditionTest() {
            RunProgram("2 3+", "", "5");
        }

        [TestMethod]
        public void ConcatTest() {
            RunProgram("\"hello\" \"world\"+", "", "helloworld");
        }

        [TestMethod]
        public void RangeTest() {
            RunProgram("5rS", "", "0", "1", "2", "3", "4");
            RunProgram("5RS", "", "1", "2", "3", "4", "5");
        }

        [TestMethod]
        public void StarOverloadsTest() {
            RunProgram("2r3*S", "", "0", "1", "0", "1", "0", "1");
            RunProgram("4\"abc\"*", "", "abcabcabcabc");
            RunProgram("{1P}3*", "", "1", "1", "1");
        }

        [TestMethod]
        public void CopyTest() {
            RunProgram("1QP", "", "1", "1");
        }

        [TestMethod]
        public void InputTest() {
            RunProgram("n2*", "3", "6");
        }

        [TestMethod]
        public void SquaresTest() {
            RunProgram("5R{c*mS", "", "1", "4", "9", "16", "25");
            RunProgram("5R{c*PF", "", "1", "4", "9", "16", "25");
        }

        [TestMethod]
        public void WhileTest() {
            RunProgram("3{Qvcw", "", "3", "2", "1");
        }

        [TestMethod]
        public void CollatzTest() {
            RunProgram("n{QXhx3*^2lx@cvwP", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void DropFirstTest() {
            RunProgram("4RU)S", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("nvR{xs%!fS", "12", "1", "2", "3", "4", "6");
            RunProgram("nvR{xs%!f%v", "12", "4");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("nR1]-{|f%1=fS", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("nR{'**mS", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1nR{*cmd", "5", "120");
            RunProgram("1nR{*F", "5", "120");
            RunProgram("1nX{*xvXxwd", "5", "120");
            RunProgram("d1xR{*F", "5", "120");
        }

        [TestMethod]
        public void ListifyTest() {
            RunProgram("1 2 3 LrS", "", "1", "2", "3");
        }

        [TestMethod]
        public void ReverseTest() {
            RunProgram("r", "asdf", "fdsa");
        }

        [TestMethod]
        public void EqualTest() {
            RunProgram("1 2=", "", "0");
            RunProgram("1 1=", "", "1");
        }

        [TestMethod]
        public void DiagonalTest() {
            RunProgram(@"nR{'\)mS", "3", @"\", @" \", @"  \");
            RunProgram(@"nR{'\)PF", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"#R{c'\)sxs-H^'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#R{c'\)pxs-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#R{'\)px_-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#R{'\)x_-H^'/)+mS", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#R{' xH*i'\&sN'/&TmS", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void IndexAssignMethod() {
            RunProgram("3'x&", "12345", "123x5");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"#R{'\)x_v-H'/)+mSx^'X)PxRr{'/)x_v-H'\)+mS", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"#H^Xrr{d' x*i'\&_""/X""i_=@&TmS", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"#{xH^' *i'\&iNv'/&c[TP}*'Xx^)P{,rTP}x*", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }

        [TestMethod]
        public void SplitTest() {
            RunProgram("{]mS", "asdf\nxxx", "a", "s", "d", "f");
            RunProgram("1/S", "asdf\nxxx", "a", "s", "d", "f");
        }

        [TestMethod]
        public void PairSpacingTest() {
            RunProgram("2/' *", "sequencespacingtest", "se qu en ce sp ac in gt es t");
        }

        [TestMethod]
        public void PairSpacing2Test() {
            RunProgram("2/{t' s+m\"\"*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{t' s+mtP", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tm' *", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
        }

        [TestMethod]
        public void PairSpacing3Test() {
            RunProgram("' /{2/' *m' *", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("' Z/{2/z*mz*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
        }

        [TestMethod]
        public void DigitTallyTest() {
            RunProgram("d10r{$ys/%v$mP", "176093677603", "2102003301");
            RunProgram("L{Xd10r{$xs/%v$mPF", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Xd10r{$xs/%v$mP}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%v$mPd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%v$pF|Pd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%vpF|Pd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$;s/%v$mP,}", "27204322879364\n82330228112748", "1042201211", "1242100130");
        }

        [TestMethod]
        public void SmileyTest() {
            RunProgram("':P\":-\"Q{')+Q}n*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':Q'-+Q{')+Q}n*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("{\":-\"')i*+i^(P}n^^*", "3", ":", ":-", ":-)", ":-))", ":-)))");
        }

        [TestMethod]
        public void DeleteBlanksTest() {
            RunProgram("L{fS", "1\n\n2\n\n\n3", "1", "2", "3");
        }

        [TestMethod]
        public void ScopeTest() {
            RunProgram("2R{d 9R{dF _PF", "", "1", "2");
            RunProgram("2R{d 9R{dF iPF", "", "0", "1");
        }

        [TestMethod]
        public void AllDigitsTest() {
            RunProgram("ArE'A{ch^1l}25*Lr$", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("Ar26r{65+]m+$", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("36r{36|b^mP", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("36r{48+c58/7*+m", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("43r{48+m7r{58+m-", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("91r48r-7r{58+m-", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("91r48r-65r58r--", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("VdVa^+", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("VW", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        [TestMethod]
        public void FindIndexTest() {
            RunProgram("' Va+sIP}", "d\nz\n_", "4", "26", "-1");
        }

        [TestMethod]
        public void BaseConversionTest() {
            RunProgram("#16|b", "291", "123");
            RunProgram("4|b", "123", "27");
        }

        [TestMethod]
        public void ShiftingDigitsTest() {
            https://codegolf.stackexchange.com/questions/141225/shifting-digits

            RunProgram("s#Xd{VdVa+YsI^x%ys@]pF|P", "5f69\n16", "607a");
            RunProgram("s#Xd{]x|b^x%x|bpF|P", "5f69\n16", "607a");
            RunProgram("s#Xd{]x|b^x|b1)pF|P", "5f69\n16", "607a");
            RunProgram("d#Ar$Va+([y{;I^;@m", "5f69\n16", "607a");
            RunProgram("d#Vw({]2*m$U)'0+ys|t", "5f69\n16", "607a");
            RunProgram("d#Vw([y{;I^;@m", "5f69\n16", "607a");
        }

        [TestMethod]
        public void FibTest() {
            RunProgram("#1s0s{c2C+Q}*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("1 0{c2C+Q}n*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01{cQ2C+}n*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01{QX+xs}n*", "7", "1", "1", "2", "3", "5", "8", "13");
        }

        [TestMethod]
        public void EvenLinesTest() {
            http://golf.shinh.org/p.rb?even+lines
            RunProgram("dP}", "qw\nas\nzx\nwe", "as", "we");
        }

        [TestMethod]
        public void SortCharsTest() {
            http://golf.shinh.org/p.rb?sort+characters
            RunProgram("O", "Hello, world!", " !,Hdellloorw");
        }

        [TestMethod]
        public void RegularTest() {
            http://golf.shinh.org/p.rb?Hamming+Numbers
            RunProgram("20R{5R1]-{*{_/c_%!wF1=fS", "", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12", "15", "16", "18", "20");
            RunProgram("#c*R{5R1]-{*{_/c_%!wF1=fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{H|fU@6/!fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{H|fH6<fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{|f5R-!fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
        }

        [TestMethod]
        public void DeleteLastLineTest() {
            http://golf.shinh.org/p.rb?delete+last+line
            RunProgram("Lc%v(S", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("LU(S", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("P,,[}", "foo\nbar\nbaz", "foo", "bar");
        }

        [TestMethod]
        public void RotateLinesTest() {
            http://golf.shinh.org/p.rb?rotate+lines
            RunProgram("d{P|DwyP", "foo\nbar\nbaz", "bar", "baz", "foo");
        }

        [TestMethod]
        public void FizzBuzzTest() {
            RunProgram("#R{3%!\"Fizz\"*_5%!\"Buzz\"*+c!_$*+mS", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("#R{_3%!\"Fizz\"*_5%!\"Buzz\"*+c?mS", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("#R{_3%!\"Fizz\"*_5%!\"Buzz\"*+c?PF", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
        }

        [TestMethod]
        public void GCDTest() {
            http://golf.shinh.org/p.rb?Greatest+Common+Divisor
            RunProgram("#s#|g", "42\n56", "14");
            RunProgram("nn|g", "42\n56", "14");
        }

        [TestMethod]
        public void LCMTest() {
            http://golf.shinh.org/p.rb?Least+Common+Multiple
            RunProgram("' /{#mXE*xE|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("' /E#Xs#Y*xy|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("nXnY*xy|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("nn|lP}", "195 548\n965 981", "106860", "946665");
        }

        [TestMethod]
        public void DeleteDupesTest() {
            http://golf.shinh.org/p.rb?delete+duplicate+lines
            RunProgram("LuS", "a\nb\na\nc", "a", "b", "c");
        }

        [TestMethod]
        public void PalindromizeTest() {
            http://golf.shinh.org/p.rb?palindromize
            RunProgram("X{dxxi(r+m{cr=fhP}", "test\nNISIOISIN", "testset", "NISIOISIN");
        }

        [TestMethod]
        public void BracketMatching() {
            http://golf.shinh.org/p.rb?Bracket+Matching
            // x - bracket type
            // y - input
            // z - temp storage for outer i
            RunProgramSingleInputs(",0[{{\"_)}]\",@=!{\"failed at: \"pyPzh}*}{dx[}\"({[\"3CI^X?yU)YdF\"yes", 
                ")", "failed at: )", 
                "()", "yes", 
                "{()[]}", "yes", 
                "()}()", "failed at: }()");
        }

        [TestMethod]
        public void GoogleTest() {
            RunProgram("#'o*'gs+\"gle\"+", "2", "google");
            RunProgram("#'o*'gs+\"gle\"+", "10", "goooooooooogle");
            RunProgram("'gp#'o*p\"gle\"P", "10", "goooooooooogle");
        }

        [TestMethod]
        public void SummationTest() {
            http://golf.shinh.org/p.rb?Summation
            RunProgram("1Cd#c^*hP}", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("1Cd#R|sP}", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("{#R|sP|Dvw", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("#,[R|sP}", "1\n2\n3\n0", "1", "3", "6");
        }

        [TestMethod]
        public void Rule30Test() {
            http://golf.shinh.org/p.rb?Rule+30
            RunProgram("\"  \"s+X{2%Hxi^@2%xi^^@2%++\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("\"  \"+{c' s+}2*cLM{0s{2%+F\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("\"  \"+{c' s+}2*cLM{\" ## \"s{2%m|s@m", "##  #   #", "## #### ###");
        }

        [TestMethod]
        public void TransposeTest() {
            RunProgram("LMS", "abc\ndef\nghi", "adg", "beh", "cfi");
        }

        [TestMethod]
        public void NegateTest() {
            RunProgramSingleInputs("nN", "13", "-13", "-14", "14");
        }

        [TestMethod]
        public void NegativePadTest() {
            RunProgram("U(P}", "abc\nzxcvb", "ab", "zxcv");
        }

        [TestMethod]
        public void OverlappingTriplesTest() {
            RunProgram("[;%R2R-{;(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("X%R2R-{x(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("%R2R-{y(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("2(y2N){+Q2)Fd", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("zs{+3)cm2N)sdS", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("{zs+3)Zm2N)S", "abcdefg", "abc", "bcd", "cde", "def", "efg");
        }

        [TestMethod]
        public void TwinPrimesTest() {
            http://golf.shinh.org/p.rb?Twin+primes
            RunProgram("#R1]-{|f%_2+|f%*1=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R1]-{c2+*|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R1]-{^c*v|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R1]-{^c*v|f%2={_p',p_2+P}*F", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R1]-{c2+*|f%2=f{c2+2l',*mS", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
        }

        [TestMethod]
        public void RegexTest() {
            RunProgram("\"x+\"{%$}|r", "axbxxcxxxd", "a1b2c3d");
        }

        [TestMethod]
        public void BronspeakTest() {
            http://golf.shinh.org/p.rb?Bronspeak
            RunProgram("Va\"aeeiioouua\"c^+X-{]2*m$U)'b+c^+x+Z,\"\\w+\"{1(zr|t_U(U)x|t_1)z|t++}|r", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
        }

        [TestMethod]
        public void CheckersPatternTest() {
            http://golf.shinh.org/p.rb?checkers+pattern

            string[] ThreeNineteenOutput = {
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1"};

            RunProgram("' /E#[#{;{R}{r}i2%?{2%m' *P}*", "3 19", ThreeNineteenOutput);
            RunProgram("' /E#[#{i2%{!cp' p};v*!P}*", "3 19", ThreeNineteenOutput);
            RunProgram("' /E#R{2%m' *s#{Q1001$|t}*d", "3 19", ThreeNineteenOutput);
            RunProgram("nnv[{i2%{!cp' p};*!P}*", "3 19", ThreeNineteenOutput);
        }

        [TestMethod]
        public void IsFibTest() {
            https://codegolf.stackexchange.com/questions/126373/am-i-a-fibonacci-number
            RunProgram("nXU1{s1C+cx<wx=P}", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
        }

        [TestMethod]
        public void PandigitalDoublingTest() {
            https://codegolf.stackexchange.com/questions/142758/pandigital-doubling

            RunProgramSingleInputs("{0{#H$0[1}Vd3C-?w|D", "66833", "44", "617283945", "1");
        }

        [TestMethod]
        public void MersennePrimeTest() {
            https://codegolf.stackexchange.com/questions/104508/is-it-a-mersenne-prime
            RunProgramSingleInputs("#c^|&!x|f%1=*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("#c^|&!x|fx-!*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("#c^|&!x|p*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
        }

        [TestMethod]
        public void PrintDiamondTest() {
            https://codegolf.stackexchange.com/questions/8696/print-this-diamond
            string[] expected = {
                "        1",
                "       121",
                "      12321",
                "     1234321",
                "    123454321",
                "   12345654321",
                "  1234567654321",
                " 123456787654321",
                "12345678987654321",
                " 123456787654321",
                "  1234567654321",
                "   12345654321",
                "    123454321",
                "     1234321",
                "      12321",
                "       121",
                "        1" };
            RunProgram("9R8Rr+{R$9)_vRr$+PF", "", expected);
            RunProgram("9R8Rr+{9s-' *_R_vRr+$+PF", "", expected);
            RunProgram("9R8Rr+{9s-' *_|A9/c*$+PF", "", expected);
            RunProgram("9R8Rr+{9s-' *'1_*#c*$+PF", "", expected);
        }

        [TestMethod]
        public void DizzyEnumeration() {
            https://codegolf.stackexchange.com/questions/142893/dizzy-integer-enumeration
            RunProgramSingleInputs("#^h{N}xh*", 
                "0", "0", 
                "1", "1",
                "2", "-1", 
                "3", "-2", 
                "4", "2", 
                "5", "3");
        }

        [TestMethod]
        public void SquaringSequnceTest() {
            https://codegolf.stackexchange.com/questions/101961/the-squaring-sequence
            RunProgramSingleInputs("1111n{c*4(#}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("'14*n{#c*4(}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("1n{c*$4*4(#}*", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
        }
    }
}
