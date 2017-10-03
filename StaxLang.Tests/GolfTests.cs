using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;

namespace StaxLang.Tests {
    [TestClass]
    public class GolfTests {
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
        public void SquaresTest() {
            RunProgram("5R{c*mS", "", "1", "4", "9", "16", "25");
            RunProgram("5R{c*PF", "", "1", "4", "9", "16", "25");
            RunProgram("5Fi^c*P", "", "1", "4", "9", "16", "25");
            RunProgram("5{c*mS", "", "1", "4", "9", "16", "25");
            RunProgram("5{c*PF", "", "1", "4", "9", "16", "25");
            RunProgram("5mc*", "", "1", "4", "9", "16", "25");
        }

        [TestMethod]
        public void CollatzTest() {
            RunProgram("nQwX2%x3*^xh?Qcv", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void DropFirstTest() {
            RunProgram("4RU)S", "", "2", "3", "4");
            RunProgram("4R1tS", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("nvR{xs%!fS", "12", "1", "2", "3", "4", "6");
            RunProgram("nv{xs%!fS", "12", "1", "2", "3", "4", "6");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("nR1]-{|f%1=fS", "15", "2", "3", "5", "7", "11", "13");
            RunProgram("n{|pfS", "15", "2", "3", "5", "7", "11", "13");
            RunProgram("nRf|p", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("nR{'**mS", "4", "*", "**", "***", "****");
            RunProgram("znf'*+Q", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1nR{*cmd", "5", "120");
            RunProgram("1nR{*F", "5", "120");
            RunProgram("1nX{*xvXxwd", "5", "120");
            RunProgram("d1xR{*F", "5", "120");
            RunProgram("1nF*", "5", "120");
        }

        [TestMethod]
        public void DiagonalTest() {
            RunProgram(@"nR{'\)mS", "3", @"\", @" \", @"  \");
            RunProgram(@"nR{'\)PF", "3", @"\", @" \", @"  \");
            RunProgram(@"nm'\)", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"eR{c'\)sxs-H^'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"eR{c'\)pxs-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"eR{'\)px_-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"eR{'\)x_-H^'/)+mS", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"em'\)x_-H^'/)+", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"eR{' xH*i'\&sN'/&TmS", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"eR{'\)x_v-H'/)+mSx^'X)PxRr{'/)x_v-H'\)+mS", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"eH^Xrr{d' x*i'\&_""/X""i_=@&TmS", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"e{xH^' *i'\&iNv'/&c~TP}*'Xx^)P{,rTP}x*", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"e{xH^' *i'\&iNv'/&c~TP}*'Xx^)PxF,rTP", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"n{'\)xi-H'/)+mcS'Xx^)Pr{rxH^)TmS", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"eH^Xrrm' x*i'\&_""/X""i_=@&T", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }

        [TestMethod]
        public void PairSpacingTest() {
            RunProgram("2/' *", "sequencespacingtest", "se qu en ce sp ac in gt es t");
            RunProgram("2/J", "sequencespacingtest", "se qu en ce sp ac in gt es t");
        }

        [TestMethod]
        public void PairSpacing2Test() {
            RunProgram("2/{t' s+m\"\"*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{t' s+mtP", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tmJ", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
        }

        [TestMethod]
        public void PairSpacing3Test() {
            RunProgram("' /{2/' *m' *", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("' Z/{2/z*mz*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("j{2/JmJ", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
        }

        [TestMethod]
        public void DigitTallyTest() {
            http://golf.shinh.org/p.rb?Digit+Tally
            RunProgram("d10r{$ys/%v$mP", "176093677603", "2102003301");
            RunProgram("Xd10r{$xs/%v$mP}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$[/%v$mPd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$[/%v$pF|Pd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$[/%vpF|Pd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$;s/%v$mP,}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("mAr{$[/%v$m", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$;s#$mP,}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Vd{;s#$mP,}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("mVd{[#$m", "27204322879364\n82330228112748", "1042201211", "1242100130");
        }

        [TestMethod]
        public void SmileyTest() {
            RunProgram("':P\":-\"Q{')+Q}n*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':Q'-+Q{')+Q}n*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("{\":-\"')i*+i^(P}n^^*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("n2+F\":-\"')i*+i^(P", "3", ":", ":-", ":-)", ":-))", ":-)))");
        }

        [TestMethod]
        public void DeleteBlanksTest() {
            RunProgram("L{fS", "1\n\n2\n\n\n3", "1", "2", "3");
            RunProgram("f", "1\n\n2\n\n\n3", "1", "2", "3");
        }

        [TestMethod]
        public void ScopeTest() {
            RunProgram("2R{d 9R{dF _PF", "", "1", "2");
            RunProgram("2R{d 9R{dF iPF", "", "0", "1");
        }

        [TestMethod]
        public void BaseConversionTest() {
            RunProgram("e16|b", "291", "123");
            RunProgram("4|b", "123", "27");
        }

        [TestMethod]
        public void ShiftingDigitsTest() {
            https://codegolf.stackexchange.com/questions/141225/shifting-digits

            RunProgram("seXd{VdVa+YsI^x%ys@]pF|P", "5f69\n16", "607a");
            RunProgram("seXd{]x|b^x%x|bpF|P", "5f69\n16", "607a");
            RunProgram("seXd{]x|b^x|b1)pF|P", "5f69\n16", "607a");
            RunProgram("deVw({]2*m$U)'0+ys|t", "5f69\n16", "607a");
            RunProgram("deVw(~y{;I^;s@m", "5f69\n16", "607a");
        }

        [TestMethod]
        public void FibTest() {
            RunProgram("e1s0s{b+Q}*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("1 0{b+Q}n*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01{QX+xs}n*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01nfQX+xs", "7", "1", "1", "2", "3", "5", "8", "13");
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
            RunProgram("ec*R{5R1]-{*{_/c_%!wF1=fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("ec*R{H|fU@6/!fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("ec*R{H|fH6<fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("ec*R{|f5R-!fx(S", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
        }

        [TestMethod]
        public void DeleteLastLineTest() {
            http://golf.shinh.org/p.rb?delete+last+line
            RunProgram("Lc%v(S", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("LU(S", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("P,,~}", "foo\nbar\nbaz", "foo", "bar");
        }

        [TestMethod]
        public void RotateLinesTest() {
            http://golf.shinh.org/p.rb?rotate+lines
            RunProgram("d{P|DwyP", "foo\nbar\nbaz", "bar", "baz", "foo");
        }

        [TestMethod]
        public void FizzBuzzTest() {
            RunProgram("eR{3%!\"Fizz\"*_5%!\"Buzz\"*+c!_$*+mS", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("nm3%!.N\"(.*_5%!.-D.*+c_?", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
        }

        [TestMethod]
        public void GCDTest() {
            http://golf.shinh.org/p.rb?Greatest+Common+Divisor
            RunProgram("ese|g", "42\n56", "14");
            RunProgram("nn|g", "42\n56", "14");
        }

        [TestMethod]
        public void LCMTest() {
            http://golf.shinh.org/p.rb?Least+Common+Multiple
            RunProgram("' /{emXE*xE|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("' /EeXseY*xy|g/P}", "195 548\n965 981", "106860", "946665");
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
            RunProgram("mX{xxi(r+m{cr=fh", "test\nNISIOISIN", "testset", "NISIOISIN");
            RunProgram("m{__i(r+ccr=!w", "test\nNISIOISIN", "testset", "NISIOISIN");
        }

        [TestMethod]
        public void BracketMatching() {
            http://golf.shinh.org/p.rb?Bracket+Matching
            RunProgramSingleInputs("mUZ~{\"[](){}\"|tX_={,_={}{iZd}?}{x~}?z^CFzU=.TO..2%IKrq`._zt+?",
                ")", "failed at: )",
                "()", "yes",
                "{()[]}", "yes",
                "()}()", "failed at: }()");
        }

        [TestMethod]
        public void GoogleTest() {
            RunProgram("e'o*'gs+\"gle\"+", "2", "google");
            RunProgram("e'o*'gs+\"gle\"+", "10", "goooooooooogle");
            RunProgram("'gpe'o*p\"gle\"P", "10", "goooooooooogle");
        }

        [TestMethod]
        public void SummationTest() {
            http://golf.shinh.org/p.rb?Summation
            RunProgram("{eR|+P|Dvw", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("e,~R|+P}", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("{eR|+Pcew", "1\n2\n3\n0", "1", "3", "6");
        }

        [TestMethod]
        public void Rule30Test() {
            http://golf.shinh.org/p.rb?Rule+30
            RunProgram("\"  \"s+X{2%Hxi^@2%xi^^@2%++\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("\"  \"+{c' s+}2*cLM{0s{2%+F\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("\"  \"+{c' s+}2*cLM{\" ## \"s{2%m|+@m", "##  #   #", "## #### ###");
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
            RunProgram("mU(", "abc\nzxcvb", "ab", "zxcv");
        }

        [TestMethod]
        public void OverlappingTriplesTest() {
            RunProgram("~;%R2R-{;(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("X%R2R-{x(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("%R2R-{y(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("2(y2N){+Q2)Fd", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("zs{+3)cm2tsdS", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("{zs+3)Zm2tS", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("3BS", "abcdefg", "abc", "bcd", "cde", "def", "efg");
        }

        [TestMethod]
        public void TwinPrimesTest() {
            http://golf.shinh.org/p.rb?Twin+primes
            RunProgram("eR1-{|f%_2+|f%*1=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("eR1-{c2+*|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("eR1-{^c*v|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("eR1-{^c*v|f%2={_p',p_2+P}*F", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("eR{|p_2+|p*f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
        }

        [TestMethod]
        public void BronspeakTest() {
            http://golf.shinh.org/p.rb?Bronspeak
            RunProgram("Va\"aeeiioouua\"c^+X-{]2*m$U)'b+c^+x+Z,\"\\w+\"{1(zr|t_U(U)x|t_1)z|t++}R", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
        }

        [TestMethod]
        public void CheckersPatternTest() {
            http://golf.shinh.org/p.rb?checkers+pattern

            string[] ThreeNineteenOutput = {
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1"};

            RunProgram("' /Ee~e{i2%{!cp' p};v*!P}*", "3 19", ThreeNineteenOutput);
            RunProgram("' /EeR{2%m' *se{Q1001$|t}*d", "3 19", ThreeNineteenOutput);
            RunProgram("nnv~{i2%{!cp' p};*!P}*", "3 19", ThreeNineteenOutput);
            RunProgram("nnv~Fi2%{!cp' p};*!P", "3 19", ThreeNineteenOutput);
            RunProgram("nnv~Fi|e{q!' p};*P", "3 19", ThreeNineteenOutput);
        }

        [TestMethod]
        public void IsFibTest() {
            https://codegolf.stackexchange.com/questions/126373/am-i-a-fibonacci-number
            RunProgram("nXU1{s[+cx<wx=P}", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
            RunProgram("meXU1{s[+cx<wx=", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
        }

        [Timeout(100)]
        [TestMethod]
        public void PandigitalDoublingTest() {
            https://codegolf.stackexchange.com/questions/142758/pandigital-doubling
            RunProgramSingleInputs("{cVds-{eH$0~1}0?w|D", "66833", "44", "617283945", "1");
        }

        [TestMethod]
        public void MersennePrimeTest() {
            https://codegolf.stackexchange.com/questions/104508/is-it-a-mersenne-prime
            RunProgramSingleInputs("ec^|&!x|f%1=*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("ec^|&!x|fx-!*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("ec^|&!x|p*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
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
            RunProgram("9R8Rr+{9s-' *'1_*ec*$+PF", "", expected);
            RunProgram("9R|p{R$9)|pTPF", "", expected);
        }

        [TestMethod]
        public void DizzyEnumeration() {
            https://codegolf.stackexchange.com/questions/142893/dizzy-integer-enumeration
            RunProgramSingleInputs("e^h{N}xh*", 
                "0", "0", 
                "1", "1",
                "2", "-1", 
                "3", "-2", 
                "4", "2", 
                "5", "3");
            RunProgramSingleInputs("e^hxhfN",
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
            RunProgramSingleInputs("1111n{c*4(e}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("'14*n{ec*4(}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("1n{c*$4*4(e}*", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
            RunProgramSingleInputs("1nfc*$4*4(e", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
        }

        [TestMethod]
        public void DownSlashesTest() {
            var slashOut = new string[] {
                @"\",
                @" \",
                @"  \",
                @"  /",
                @" /",
                @" \",
                @"  \",
                @"  /",
                @"  \",
                @"  /",
                @" /",
                @" \",
                @"  \",
                @"  /",
                @" /",
                @"/"
            };

            RunProgram(@"{|ex+X_])Px_2%-XF", @"\\\//\\/\//\\///", slashOut);
        }

        [TestMethod]
        public void TabulaRectaTest() {
            https://codegolf.stackexchange.com/questions/86986/print-a-tabula-recta
            var expected = new[] {
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "BCDEFGHIJKLMNOPQRSTUVWXYZA",
                "CDEFGHIJKLMNOPQRSTUVWXYZAB",
                "DEFGHIJKLMNOPQRSTUVWXYZABC",
                "EFGHIJKLMNOPQRSTUVWXYZABCD",
                "FGHIJKLMNOPQRSTUVWXYZABCDE",
                "GHIJKLMNOPQRSTUVWXYZABCDEF",
                "HIJKLMNOPQRSTUVWXYZABCDEFG",
                "IJKLMNOPQRSTUVWXYZABCDEFGH",
                "JKLMNOPQRSTUVWXYZABCDEFGHI",
                "KLMNOPQRSTUVWXYZABCDEFGHIJ",
                "LMNOPQRSTUVWXYZABCDEFGHIJK",
                "MNOPQRSTUVWXYZABCDEFGHIJKL",
                "NOPQRSTUVWXYZABCDEFGHIJKLM",
                "OPQRSTUVWXYZABCDEFGHIJKLMN",
                "PQRSTUVWXYZABCDEFGHIJKLMNO",
                "QRSTUVWXYZABCDEFGHIJKLMNOP",
                "RSTUVWXYZABCDEFGHIJKLMNOPQ",
                "STUVWXYZABCDEFGHIJKLMNOPQR",
                "TUVWXYZABCDEFGHIJKLMNOPQRS",
                "UVWXYZABCDEFGHIJKLMNOPQRST",
                "VWXYZABCDEFGHIJKLMNOPQRSTU",
                "WXYZABCDEFGHIJKLMNOPQRSTUV",
                "XYZABCDEFGHIJKLMNOPQRSTUVW",
                "YZABCDEFGHIJKLMNOPQRSTUVWX",
                "ZABCDEFGHIJKLMNOPQRSTUVWXY",
            };

            RunProgram("VA{Q2*U)26(}26*", "", expected);
            RunProgram("VA{Qch+U)}26*", "", expected);
            RunProgram("VA{Q|(}26*", "", expected);
            RunProgram("VA26fQ|(", "", expected);
        }

        [TestMethod]
        public void LphabetTest() {
            https://codegolf.stackexchange.com/questions/87064/print-output-the-l-phabet

            string[] expected = {
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "BBCDEFGHIJKLMNOPQRSTUVWXYZ",
                "CCCDEFGHIJKLMNOPQRSTUVWXYZ",
                "DDDDEFGHIJKLMNOPQRSTUVWXYZ",
                "EEEEEFGHIJKLMNOPQRSTUVWXYZ",
                "FFFFFFGHIJKLMNOPQRSTUVWXYZ",
                "GGGGGGGHIJKLMNOPQRSTUVWXYZ",
                "HHHHHHHHIJKLMNOPQRSTUVWXYZ",
                "IIIIIIIIIJKLMNOPQRSTUVWXYZ",
                "JJJJJJJJJJKLMNOPQRSTUVWXYZ",
                "KKKKKKKKKKKLMNOPQRSTUVWXYZ",
                "LLLLLLLLLLLLMNOPQRSTUVWXYZ",
                "MMMMMMMMMMMMMNOPQRSTUVWXYZ",
                "NNNNNNNNNNNNNNOPQRSTUVWXYZ",
                "OOOOOOOOOOOOOOOPQRSTUVWXYZ",
                "PPPPPPPPPPPPPPPPQRSTUVWXYZ",
                "QQQQQQQQQQQQQQQQQRSTUVWXYZ",
                "RRRRRRRRRRRRRRRRRRSTUVWXYZ",
                "SSSSSSSSSSSSSSSSSSSTUVWXYZ",
                "TTTTTTTTTTTTTTTTTTTTUVWXYZ",
                "UUUUUUUUUUUUUUUUUUUUUVWXYZ",
                "VVVVVVVVVVVVVVVVVVVVVVWXYZ",
                "WWWWWWWWWWWWWWWWWWWWWWWXYZ",
                "XXXXXXXXXXXXXXXXXXXXXXXXYZ",
                "YYYYYYYYYYYYYYYYYYYYYYYYYZ",
                "ZZZZZZZZZZZZZZZZZZZZZZZZZZ",
            };

            RunProgram("zVA{]Yi*]+{y+mFS", "", expected);
            RunProgram("26RZ{~z{;|M64+mPF", "", expected);
            RunProgram("VA{~VA{;|MmPF", "", expected);
            RunProgram("VA{VA{[s|MmPF", "", expected);
            RunProgram("VAQc2B{|tQF", "", expected);
        }

        [TestMethod]
        public void AlphabetsTriangleTest() {
            https://codegolf.stackexchange.com/questions/87496/alphabet-triangle?noredirect=1&lq=1
            string[] expected = {
                "A",
                "ABA",
                "ABCBA",
                "ABCDCBA",
                "ABCDEDCBA",
                "ABCDEFEDCBA",
                "ABCDEFGFEDCBA",
                "ABCDEFGHGFEDCBA",
                "ABCDEFGHIHGFEDCBA",
                "ABCDEFGHIJIHGFEDCBA",
                "ABCDEFGHIJKJIHGFEDCBA",
                "ABCDEFGHIJKLKJIHGFEDCBA",
                "ABCDEFGHIJKLMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWXWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWXYXWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZYXWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWXYXWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWXWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVWVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUVUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTUTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSTSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRSRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQRQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPQPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNOPONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNONMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMNMLKJIHGFEDCBA",
                "ABCDEFGHIJKLMLKJIHGFEDCBA",
                "ABCDEFGHIJKLKJIHGFEDCBA",
                "ABCDEFGHIJKJIHGFEDCBA",
                "ABCDEFGHIJIHGFEDCBA",
                "ABCDEFGHIHGFEDCBA",
                "ABCDEFGHGFEDCBA",
                "ABCDEFGFEDCBA",
                "ABCDEFEDCBA",
                "ABCDEDCBA",
                "ABCDCBA",
                "ABCBA",
                "ABA",
                "A",
            };

            RunProgram("VA{VAi^(crU)+mcrU)+S", "", expected);
            RunProgram("VA{VAi^(cr1t+mcr1t+S", "", expected);
            RunProgram("VA|[{|pm|pS", "", expected);
        }

        [TestMethod]
        public void LongestOneRunTest() {
            https://codegolf.stackexchange.com/questions/143000/calculate-the-longest-series-of-1s-in-an-integers-binary-value
            RunProgramSingleInputs("e{cch|&cwd2|b%v", "142", "1", "48", "4", "750", "5", "0", "0");
        }

        [TestMethod]
        public void SimplifyNotNegateTest() {
            https://codegolf.stackexchange.com/questions/142934/evaluate-an-expression-of-minus-and-tilde
            string[] expected = {
                "x", "x+0",
                "~x", "-x-1",
                "-~x", "x+1",
                "~-~x", "-x-2",
                "-~-~x", "x+2",
                "--~~x", "x+0",
                "~-x", "x-1",
                "-~-x", "-x+1",
            };
            RunProgramSingleInputs("%|e'-*'x+py{]'~=Ui^|**m|+cU>'+*pP", expected);
            RunProgramSingleInputs("%|e'-*'x+py{5%Ui^|**m|+cU>'+*pP", expected);
        }

        [TestMethod]
        public void BitflipAndNegateTest() {
            https://codegolf.stackexchange.com/questions/92598/bitflip-and-negate
            RunProgramSingleInputs("\"-~\"x*x0<T'0+",
                "-3", "~-~-~0",
                "-2", "~-~0",
                "-1", "~0",
                "0", "0",
                "1", "-~0",
                "2", "-~-~0",
                "3", "-~-~-~0");
        }

        [TestMethod]
        public void BinarySierpinskiTest() {
            https://codegolf.stackexchange.com/questions/67497/compute-the-binary-sierpinski-triangle-sequence
            RunProgramSingleInputs("1{cH|^}n*",
                "0", "1",
                "1", "3",
                "2", "5",
                "3", "15",
                "4", "17");
            RunProgramSingleInputs("1nfcH|^",
                "0", "1",
                "1", "3",
                "2", "5",
                "3", "15",
                "4", "17");
        }

        [TestMethod]
        public void CrossedSquareTest() {
            https://codegolf.stackexchange.com/questions/91068/creating-a-crossed-square
            RunProgram("'*x*QxvXvR{'*x('*+s'*& xvi-'*&PFP", "7",
                "*******",
                "**   **",
                "* * * *",
                "*  *  *",
                "* * * *",
                "**   **",
                "*******");

            RunProgram("'*Zx*]xh{z)xh(zs+|pm+|pS", "7",
                "*******",
                "**   **",
                "* * * *",
                "*  *  *",
                "* * * *",
                "**   **",
                "*******");
        }

        [TestMethod]
        public void NicomachussTest() {
            https://codegolf.stackexchange.com/questions/143216/visualize-nicomachuss-theorem
            string[] expected = {
                @"+//XXX\\\\+++++",
                @"/\\XXX\\\\+++++",
                @"/\\XXX////+++++",
                @"XXX+++////+++++",
                @"XXX+++////+++++",
                @"XXX+++////XXXXX",
                @"\\////\\\\XXXXX",
                @"\\////\\\\XXXXX",
                @"\\////\\\\XXXXX",
                @"\\////\\\\XXXXX",
                @"+++++XXXXX+++++",
                @"+++++XXXXX+++++",
                @"+++++XXXXX+++++",
                @"+++++XXXXX+++++",
                @"+++++XXXXX+++++",
            };
            RunProgram(@"znR{X""X/""i@]*x""+\""i@]*+x*xx^*h)~{;i@]x*+m;]x*+FS", "5", expected);
            RunProgram(@"znHR{hR|+_^hYih2%""/\""""+X""?*{O_*ihR|+ts(]y*+MFS", "5", expected);
        }

        [TestMethod]
        public void PalindromicBinaryTwist() {
            https://codegolf.stackexchange.com/questions/139254/palindromic-numbers-with-a-binary-twist
            string[] expected = {
                "1", "1",
                "6", "1",
                "9", "1",
                "10", "0",
                "12", "0",
                "13", "0",
                "14", "0",
                "33", "1",
                "44", "0",
                "1342177280", "0",
                "297515792", "1",
            };

            RunProgramSingleInputs("cr=xH{hc|ew2|bcr=*", expected);
            RunProgramSingleInputs("cr=x2|/2|bcr=*",expected);
            RunProgramSingleInputs("cr=x2|/|Bcr=*", expected);
        }

        [TestMethod]
        public void InsignificantArrayTest() {
            https://codegolf.stackexchange.com/questions/143278/am-i-an-insignificant-array
            RunProgramSingleInputs("nL2B{E-m2r-U-!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
            RunProgramSingleInputs("nL2B{E-^3/f!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
            RunProgramSingleInputs("e2B{E-^3/f!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
            RunProgramSingleInputs("e|-{^3/f!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
        }

        [TestMethod]
        public void CobolCommentStripTest() {
            https://codegolf.stackexchange.com/questions/140292/uncomment-a-cobol-program

            RunProgram("F6@4%C_7tP", 
                "000000 blah blah\n" +
                "000001* apples\n" +
                "000002 oranges ?\n" +
                "000003* yeah, oranges.\n" +
                "000*04 love me some oranges\n",
                "blah blah",
                "oranges ?",
                "love me some oranges");
        }

        [TestMethod]
        public void ReverseTheConsonantsTest() {
            https://codegolf.stackexchange.com/questions/83171/reverse-the-consonants
            RunProgram("Vc'y-M'|*Z{yz|fri@}R", "reverse the consonants", "setenne sne cohtosarvr");
        }

        [TestMethod]
        public void ReverseStringMaintainCapsTest() {
            https://codegolf.stackexchange.com/questions/84606/reverse-a-string-while-maintaining-the-capitalization-in-the-same-places
            RunProgramSingleInputs("vr{VAyi@I^_]^_?m",
                "Hello, Midnightas", "SathginDim ,olleh",
                ".Q", "q.");
        }

        [TestMethod]
        public void AlternatingHarmonicTest() {
            http://golf.shinh.org/p.rb?alternating+harmonic+series
            string[] expected = {
                "1/1",
                "1/2",
                "5/6",
                "7/12",
                "47/60",
                "37/60",
                "319/420",
                "533/840",
                "1879/2520",
                "1627/2520",
                "20417/27720",
                "18107/27720",
                "263111/360360",
                "237371/360360",
                "52279/72072",
                "95549/144144",
                "1768477/2450448",
                "1632341/2450448",
                "33464927/46558512",
                "155685007/232792560",
            };

            RunProgram("1YZ20mx_*yzNZ*-Xy_*Y|g~x;/p'/py,/", "", expected);
        }

        [TestMethod]
        public void AsciiStarsTest() {
            http://golf.shinh.org/p.rb?ASCII+Stars
            string[] expected = {
                "  *",
                " ***",
                "*****",
                " ***",
                "  *",
            };

            RunProgram("n{'**x)|pTm|pS", "3", expected);
        }

        [TestMethod]
        public void BinaryMultiplicationTest() {
            http://golf.shinh.org/p.rb?Binary+Multiplication
            RunProgramSingleInputs("',/{|BmE*|B8|z",
                "00000011,00000011", "00001001",
                "00101001,00000110", "11110110",
                "00001111,00001011", "10100101");
        }

        [TestMethod]
        public void IllustrateLCMTest() {
            https://codegolf.stackexchange.com/questions/143725/illustrate-the-least-common-multiple
            RunProgram("ec|l~mv'-*'|+;_/*", "[6 4]", "-----|-----|", "---|---|---|");
        }

        [TestMethod]
        public void SymmetricNotPalindromicTest() {
            https://codegolf.stackexchange.com/questions/142248/im-symmetric-not-palindromic
            RunProgramSingleInputs(@"r""()<>[]{}qpbd/\""Xcr+|t"" !`""'+*-.:=AHIMOTUVWXY^_ovwx|""x+|&_=",
                "()()", "1",
                "()()()", "1",
                "[A + A]", "1",
                "WOW ! WOW", "1",
                "OH-AH_wx'xw_HA-HO", "1",
                "(<<[[[T*T]]]>>)", "1",
                "(:)", "1",
                ")-(", "1",
                "())(()", "1",
                "qpqp", "1",
                "())(", "0",
                "((B))", "0",
                "11", "0",
                "+-*+-", "0",
                "WOW ! wow", "0",
                "(;)", "0",
                "qppq", "0");
        }

        [TestMethod]
        public void StringStairsTest() {
            https://codegolf.stackexchange.com/questions/143988/build-me-some-string-stairs

            string[] expected = {
                "Stap        sapr",
                "   hylo        ophy",
                "      cocc        ticu",
                "         us          s",
            };

            RunProgram("nXdj{x/{xvi*' *s+mmc{%m|Ms{[%-z]*_s+m{{%m|M_{[(mmMFJTP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("nXdccj~FiY;{%cvx/-' xv*y*_xy*tx(+(mJTc!CP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("scj~FiY;{%cvx/-' xv*y*_xy*tx(+(mJTc!CP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWiYdc{%cvx/-' xv*y*_xy*tx(+(mJTc!CP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWiYxv*z)[s{%cvx/-_xy*tx((mJTc!C+P", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWc{%cvx/-_x|i*tx((mJTc!Cixv*z)pP", "4\nStaphylococcus saprophyticus", expected);
        }

        [TestMethod]
        public void PalindromeRangeTest() {
            https://codegolf.stackexchange.com/questions/3532/enumerate-all-palindromic-numbers-in-decimal-between-0-and-n

            RunProgram("e^rf$cr=", "33", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "11", "22", "33");
        }

        [TestMethod]
        public void RemoveAmbigousPluralsTest() {
            https://codegolf.stackexchange.com/questions/144136/remove-ambiguous-plurals
            RunProgramSingleInputs(@"""(\b(an?|one|1) \S+)\(s\)""X""$1""Rx5)'sR",
                "one banana(s) two apple(s)", "one banana two apples",
                "1 banana(s) 11 apple(s)", "1 banana 11 apples");
        }

        [TestMethod]
        public void AlphabetSearchlightTest() {
            https://codegolf.stackexchange.com/questions/141725/make-an-alphabet-searchlight
            string[] expected = {
                "                         ZYXWVUTSRQPONMLKJIHGFEDCBA",
                "                        YXWVUTSRQPONMLKJIHGFEDCBA",
                "                       XWVUTSRQPONMLKJIHGFEDCBA",
                "                      WVUTSRQPONMLKJIHGFEDCBA",
                "                     VUTSRQPONMLKJIHGFEDCBA",
                "                    UTSRQPONMLKJIHGFEDCBA",
                "                   TSRQPONMLKJIHGFEDCBA",
                "                  SRQPONMLKJIHGFEDCBA",
                "                 RQPONMLKJIHGFEDCBA",
                "                QPONMLKJIHGFEDCBA",
                "               PONMLKJIHGFEDCBA",
                "              ONMLKJIHGFEDCBA",
                "             NMLKJIHGFEDCBA",
                "            MLKJIHGFEDCBA",
                "           LKJIHGFEDCBA",
                "          KJIHGFEDCBA",
                "         JIHGFEDCBA",
                "        IHGFEDCBA",
                "       HGFEDCBA",
                "      GFEDCBA",
                "     FEDCBA",
                "    EDCBA",
                "   DCBA",
                "  CBA",
                " BA",
                "A",
            };
            RunProgram("VAr|]mc%Hv)", "", expected);
        }

        [TestMethod]
        public void WordIntoAlphabetGrid() {
            https://codegolf.stackexchange.com/questions/141372/fit-a-word-into-an-alphabet-grid
            
            string[] expected = {
                "A            N     T      ",
                "        I                 ",
                "   D    I         S       ",
                "    E             ST      ",
                "AB         L              ",
                "        I         S       ",
                "       H    M             ",
                "    E        N     T      ",
                "A                R        ",
                "        I                 ",
                "A            N            ",
                "        I         S       ",
                "            M             ",
                "                          ",
            };

            RunProgram("wVA{[I' {1t_}?mQT", "ANTIDISESTABLISHMENTARIANISM", expected);
            RunProgram("wVA{[I' {B}?mQT", "ANTIDISESTABLISHMENTARIANISM", expected);
        }

        [TestMethod]
        public void IntegersContainANumberTest() {
            https://codegolf.stackexchange.com/questions/98470/how-many-integers-contain-a-number-in-a-specific-range

            RunProgramSingleInputs("e^|r{$[s#f%",
                "\"3\" 1 100", "19",
                "\"12\" -200 200", "24",
                "\"123\" 1 3", "0",
                "\"3\" 33 34", "2",
                "\"0\" -1 1", "1",
                "\"127\" -12 27", "0");

        }
    }
}
