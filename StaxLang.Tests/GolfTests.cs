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

            for (int i = 0; i < inputOutputs.Length; i += 2) {
                var input = inputOutputs[i];
                var expected = inputOutputs[i + 1] + Environment.NewLine;
                var writer = new StringWriter();
                new Executor(writer).Run(source, new[] { input });
                string actual = writer.ToString();

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod] public void LineModeEval() {
            // leading e runs on each line of input
            RunProgram("e-", "5\n8", "3");
        }

        [TestMethod]
        public void SquaresTest() {
            RunProgram("5mJ", "", "1", "4", "9", "16", "25");
        }

        [TestMethod]
        public void CollatzTest() {
            RunProgram("QwX2%x3*^xh?Qcv", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("1gth_3*^\\_@", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("guh_3*^\\_@", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void DropFirstTest() {
            RunProgram("4RU)m", "", "2", "3", "4");
            RunProgram("4R1tm", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("vR{xs%!fm", "12", "1", "2", "3", "4", "6");
            RunProgram("v{xs%!fm", "12", "1", "2", "3", "4", "6");
            RunProgram("vmxs%C_", "12", "1", "2", "3", "4", "6");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("R1]-{|f%1=fm", "15", "2", "3", "5", "7", "11", "13");
            RunProgram("{|pfm", "15", "2", "3", "5", "7", "11", "13");
            RunProgram("Rf|p", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("R{'**mm", "4", "*", "**", "***", "****");
            RunProgram("z,f'*+Q", "4", "*", "**", "***", "****");
            RunProgram("m'**", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1,R{*cmd", "5", "120");
            RunProgram("1,R{*F", "5", "120");
            RunProgram("1,X{*xvXxwd", "5", "120");
            RunProgram("d1xR{*F", "5", "120");
            RunProgram("1,F*", "5", "120");
            RunProgram("F*", "5", "120");
        }

        [TestMethod]
        public void DiagonalTest() {
            RunProgram(@"R{'\)mm", "3", @"\", @" \", @"  \");
            RunProgram(@"R{'\)PF", "3", @"\", @" \", @"  \");
            RunProgram(@"m'\)", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"R{c'\)sxs-H^'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"R{c'\)pxs-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"R{'\)px_-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"R{'\)x_-H^'/)+mm", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"m'\)x_-H^'/)+", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"m'\)|xH^'/)+", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"H^Xrr{d' x*i'\&_""/X""i_=@&Tmm", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"{xH^' *i'\&iNv'/&c~TP}*'Xx^)P{,rTP}x*", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"{xH^' *i'\&iNv'/&c~TP}*'Xx^)PxF,rTP", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"H^Xrrm' x*i'\&_""/X""i_=@&T", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }

        [TestMethod]
        public void PairSpacingTest() {
            http://golf.shinh.org/p.rb?Pair+Spacing+1
            RunProgram("2/' *", "sequencespacingtest", "se qu en ce sp ac in gt es t");
            RunProgram("2/J", "sequencespacingtest", "se qu en ce sp ac in gt es t");
        }

        [TestMethod]
        public void PairSpacing2Test() {
            http://golf.shinh.org/p.rb?Pair+Spacing+2
            RunProgram("2/{t' s+m\"\"*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{t' s+mtP", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tmJ", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
        }

        [TestMethod]
        public void PairSpacing3Test() {
            http://golf.shinh.org/p.rb?Pair+Spacing+3
            RunProgram("' /{2/' *m' *", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("' X/{2/x*mx*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("j{2/JmJ", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
        }

        [TestMethod]
        public void DigitTallyTest() {
            http://golf.shinh.org/p.rb?Digit+Tally
            RunProgram("dAr{$ys/%v$mP", "176093677603", "2102003301");
            RunProgram("XdAr{$xs/%v$mP}", "27204322879364\n82330228112748", "1042201211", "1242100130");
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
            RunProgram("':P\":-\"Q{')+Q},*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':Q'-+Q{')+Q},*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("{\":-\"')i*+i^(P},^^*", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("2+F\":-\"')i*+i^(P", "3", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':Q'-+Q,f')+Q", "3", ":", ":-", ":-)", ":-))", ":-)))");
        }

        [TestMethod]
        public void DeleteBlanksTest() {
            http://golf.shinh.org/p.rb?delete+blank+lines
            RunProgram("L{fm", "1\n\n2\n\n\n3", "1", "2", "3");
            RunProgram("f", "1\n\n2\n\n\n3", "1", "2", "3");
        }

        [TestMethod]
        public void ShiftingDigitsTest() {
            https://codegolf.stackexchange.com/questions/141225/shifting-digits

            RunProgram("seXd{VdVa+YsI^x%ys@]pF|P", "5f69\n16", "607a");
            RunProgram("seXd{]x|b^x%x|bpF|P", "5f69\n16", "607a");
            RunProgram("seXd{]x|b^x|b1)pF|P", "5f69\n16", "607a");
            RunProgram("deVw({]2*m$U)'0+ys|t", "5f69\n16", "607a");
            RunProgram("deVw(~y{;I^;s@m", "5f69\n16", "607a");
            RunProgram("{];e|b^;e|bHm", "5f69\n16", "607a");
            RunProgram("seVw(2*o|(|t", "5f69\n16", "607a");
        }

        [TestMethod]
        public void FibTest() {
            RunProgram("1 0a{b+Q}*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("1 0{b+Q},*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01{QX+xs},*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01,fQX+xs", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("01,fQb+", "7", "1", "1", "2", "3", "5", "8", "13");
        }

        [TestMethod]
        public void EvenLinesTest() {
            http://golf.shinh.org/p.rb?even+lines
            RunProgram("dP}", "qw\nas\nzx\nwe", "as", "we");
        }

        [TestMethod]
        public void SortCharsTest() {
            http://golf.shinh.org/p.rb?sort+characters
            RunProgram("o", "Hello, world!", " !,Hdellloorw");
        }

        [TestMethod]
        public void RegularTest() {
            http://golf.shinh.org/p.rb?Hamming+Numbers
            RunProgram("20R{5R1]-{*{_/c_%!wF1=fm", "", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12", "15", "16", "18", "20");
            RunProgram("c*R{5R1]-{*{_/c_%!wF1=fx(m", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("c*R{H|fU@6/!fx(m", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("c*R{H|fH6<fx(m", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("c*R{|f5R-!fx(m", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("J{|f5R-!fx(m", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
        }

        [TestMethod]
        public void DeleteLastLineTest() {
            http://golf.shinh.org/p.rb?delete+last+line
            RunProgram("Lc%v(m", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("LU(m", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("L1Tm", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("P,,~}", "foo\nbar\nbaz", "foo", "bar");
        }

        [TestMethod]
        public void RotateLinesTest() {
            http://golf.shinh.org/p.rb?rotate+lines
            RunProgram("d{P|DwyP", "foo\nbar\nbaz", "bar", "baz", "foo");
            RunProgram("L|(m", "foo\nbar\nbaz", "bar", "baz", "foo");
        }

        [TestMethod]
        public void FizzBuzzTest() {
            RunProgram("R{3%!\"Fizz\"*_5%!\"Buzz\"*+c!_$*+mm", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("m3%!`M\"(`*_5 %!`-C`*+c_?", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("─22¶←P¿√L√£L◘≈║ätÅ↔0", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
        }

        [TestMethod]
        public void GCDTest() {
            http://golf.shinh.org/p.rb?Greatest+Common+Divisor
            RunProgram("e|g", "42\n56", "14");
        }

        [TestMethod]
        public void LCMTest() {
            http://golf.shinh.org/p.rb?Least+Common+Multiple
            RunProgram("' /{emXE*xE|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("' /EeXseY*xy|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("me|l", "195 548\n965 981", "106860", "946665");
        }

        [TestMethod]
        public void DeleteDupesTest() {
            http://golf.shinh.org/p.rb?delete+duplicate+lines
            RunProgram("Lum", "a\nb\na\nc", "a", "b", "c");
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
            RunProgramSingleInputs("mUY~{\"[](){}\"|tX_={,_={}{iYd}?}{x~}?y^CFyU=`SN``1%HJrq_`_yt+?",
                ")", "failed at: )",
                "()", "yes",
                "{()[]}", "yes",
                "()}()", "failed at: }()");
        }

        [TestMethod]
        public void GoogleTest() {
            http://golf.shinh.org/p.rb?google
            RunProgram("'o*'gs+\"gle\"+", "2", "google");
            RunProgram("'o*'gs+\"gle\"+", "10", "goooooooooogle");
            RunProgram("'gp'o*p\"gle\"P", "10", "goooooooooogle");
            RunProgram("'gp'o*p\"gle", "10", "goooooooooogle");
        }

        [TestMethod]
        public void SummationTest() {
            http://golf.shinh.org/p.rb?Summation
            RunProgram("{eR|+P|Dvw", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("ewR|+P;", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("{eR|+Pcew", "1\n2\n3\n0", "1", "3", "6");
            RunProgram("mec!CR|+", "1\n2\n3\n0", "1", "3", "6");
        }

        [TestMethod]
        public void Rule30Test() {
            http://golf.shinh.org/p.rb?Rule+30
            RunProgram("\"  \"s+X{2%Hxi^@2%xi^^@2%++\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("\"  \"Xs+x+3B{\" 0#1\"|t|Bv4/' '#?m", "##  #   #", "## #### ###");
            RunProgram("\"  \"Xs+x+3B{{2%$m|Bv4/' '#?m", "##  #   #", "## #### ###");
            RunProgram("\"  \"Xs+x+3B{h_+'##vh' '#?m", "##  #   #", "## #### ###");
            RunProgram("\"  \"Xs+x+3B{E|M+67='#' ?m", "##  #   #", "## #### ###");
        }

        [TestMethod]
        public void TransposeTest() {
            RunProgram("LMm", "abc\ndef\nghi", "adg", "beh", "cfi");
        }

        [TestMethod]
        public void NegateTest() {
            RunProgramSingleInputs("N", "13", "-13", "-14", "14");
        }

        [TestMethod]
        public void OverlappingTriplesTest() {
            RunProgram("~;%R2R-{;(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("X%R2R-{x(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("%R2R-{y(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("2(y2N){+Q2)Fd", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("zs{+3)cm2tsdm", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("3Bm", "abcdefg", "abc", "bcd", "cde", "def", "efg");
        }

        [TestMethod]
        public void TwinPrimesTest() {
            http://golf.shinh.org/p.rb?Twin+primes
            RunProgram("R1-{|f%_2+|f%*1=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("R1-{c2+*|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("R1-{^c*v|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("R1-{^c*v|f%2={_p',p_2+P}*F", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("{|p_2+|p*fFp',p_2+P", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("{c*v|f%2=fmvq',p2+", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("mJv|f%2-Ciq',p2+", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
        }

        [TestMethod]
        public void BronspeakTest() {
            http://golf.shinh.org/p.rb?Bronspeak
            RunProgram("Va\"aeeiioouua\"c^+X-{]2*m$U)'b+c^+x+Y,\"\\w+\"{1(yr|t_U(U)x|t_1)y|t++}R", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
            RunProgram("VaVv2*o|(c^+X-{]2*m$U)'b+c^+x+Y,\"\\w+\"{1(yr|t_U(U)x|t_1)y|t++}R", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
            RunProgram("\"\\w+\"{1(Vc{]2*m$U)'b+c^+Vv2*{o|(c^+X+Yr|t_1t1Tx|t_1)y|t++}R", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
        }

        [TestMethod]
        public void CheckersPatternTest() {
            http://golf.shinh.org/p.rb?checkers+pattern

            string[] ThreeNineteenOutput = {
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1"};

            RunProgram("ssv~{i2%{!cp' p};*!P}*", "3 19", ThreeNineteenOutput);
            RunProgram("ssv~Fi2%{!cp' p};*!P", "3 19", ThreeNineteenOutput);
            RunProgram("ssv~Fi|e{q!' p};*P", "3 19", ThreeNineteenOutput);
            RunProgram("vXdmi|exfq!' p", "3 19", ThreeNineteenOutput);
            RunProgram("vXdm2%xfq!' p", "3 19", ThreeNineteenOutput);
            RunProgram("vXdm2%xfq!| ", "3 19", ThreeNineteenOutput);
        }

        [TestMethod]
        public void IsFibTest() {
            https://codegolf.stackexchange.com/questions/126373/am-i-a-fibonacci-number
            RunProgram(",eXU1{s[+cx<wx=P}", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
            RunProgram("meXU1{s[+cx<wx=", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
            RunProgram("meXU1{b+cx<wx=", "0\n3\n4\n13\n14", "1", "1", "0", "1", "0");
        }

        [TestMethod]
        public void PandigitalDoublingTest() {
            https://codegolf.stackexchange.com/questions/142758/pandigital-doubling
            RunProgramSingleInputs("${cVds-{eH$0~1}0?w|D", "66833", "44", "617283945", "1", "1234567890", "0");
            RunProgramSingleInputs("0,{c$u%A=Cs^sHWd", "66833", "44", "617283945", "1", "1234567890", "0");
            RunProgramSingleInputs(",{i~c$u%A=CHW,", "66833", "44", "617283945", "1", "1234567890", "0");
            RunProgramSingleInputs("{$u%A<}{Hgf%", "66833", "44", "617283945", "1", "1234567890", "0");
            RunProgramSingleInputs("wiVdxi|<$-", "66833", "44", "617283945", "1", "1234567890", "0");
        }

        [TestMethod]
        public void MersennePrimeTest() {
            https://codegolf.stackexchange.com/questions/104508/is-it-a-mersenne-prime
            RunProgramSingleInputs("c^|&!x|f%1=*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("c^|&!x|fx-!*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
            RunProgramSingleInputs("c^|&!x|p*", "5", "0", "6", "0", "7", "1", "15", "0", "8191", "1");
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
            RunProgram("9R|pmR$9)|pT", "", expected);
        }

        [TestMethod]
        public void DizzyEnumeration() {
            https://codegolf.stackexchange.com/questions/142893/dizzy-integer-enumeration
            RunProgramSingleInputs("^h{N}xh*",
                "0", "0",
                "1", "1",
                "2", "-1",
                "3", "-2",
                "4", "2",
                "5", "3");
            RunProgramSingleInputs("^hxhfN",
                "0", "0",
                "1", "1",
                "2", "-1",
                "3", "-2",
                "4", "2",
                "5", "3");
        }

        [TestMethod]
        public void SquaringSequenceTest() {
            https://codegolf.stackexchange.com/questions/101961/the-squaring-sequence
            RunProgramSingleInputs("1111,{c*4(e}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("'14*,{ec*4(}*", "0", "1111", "7", "6840", "14", "7584", "19", "1425", "79", "4717");
            RunProgramSingleInputs("1,{c*$4*4(e}*", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
            RunProgramSingleInputs("1,fJ$4*4(e", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
            RunProgramSingleInputs("OfJ$4*4(e", "1", "1111", "8", "6840", "15", "7584", "20", "1425", "80", "4717");
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
            RunProgram("VA{|(gum", "", expected);
            RunProgram("VAgu|(", "", expected);
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

            RunProgram("zVA{]Yi*]+{y+mFm", "", expected);
            RunProgram("26RX{~x{;|M64+mPF", "", expected);
            RunProgram("VA{~VA{;|MmPF", "", expected);
            RunProgram("VA{VA{[s|MmPF", "", expected);
            RunProgram("VAQc2B{|tQF", "", expected);
            RunProgram("VAQc2BF|tQ", "", expected);
            RunProgram("VAmVA{[|Mm", "", expected);
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

            RunProgram("VA{VAi^(crU)+mcrU)+m", "", expected);
            RunProgram("VA{VAi^(cr1t+mcr1t+m", "", expected);
            RunProgram("VA|[{|pm|pm", "", expected);
        }

        [TestMethod]
        public void LongestOneRunTest() {
            https://codegolf.stackexchange.com/questions/143000/calculate-the-longest-series-of-1s-in-an-integers-binary-value
            RunProgramSingleInputs("{cch|&cwd|B%v", "142", "1", "48", "4", "750", "5", "0", "0");
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
            RunProgramSingleInputs("1{cH|^},*",
                "0", "1",
                "1", "3",
                "2", "5",
                "3", "15",
                "4", "17");
            RunProgramSingleInputs("OfcH|^",
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

            RunProgram("'*Y*]xh{y)xh(ys+|pm+|pm", "7",
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
            RunProgram(@"z,R{X""X/""i@]*x""+\""i@]*+x*xx^*h)~{;i@]x*+m;]x*+Fm", "5", expected);
            RunProgram(@"z,HR{hR|+_^hYih2%""/\""""+X""?*{o_*ihR|+ts(]y*+MFm", "5", expected);
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

            RunProgramSingleInputs("$cr=xH{hc|ew2|bcr=*", expected);
            RunProgramSingleInputs("$cr=x2|/2|bcr=*", expected);
            RunProgramSingleInputs("$cr=x2|/|Bcr=*", expected);
            RunProgramSingleInputs("2|/|Bcr=ycr=*", expected);
        }

        [TestMethod]
        public void InsignificantArrayTest() {
            https://codegolf.stackexchange.com/questions/143278/am-i-an-insignificant-array
            RunProgramSingleInputs("2B{E-^3/f!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
            RunProgramSingleInputs("|-{^3/f!",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 4]", "1",
                "[1, 2, 3, 4, 3, 4, 5, 5, 5, 3]", "0");
        }

        [TestMethod]
        public void CobolCommentStripTest() {
            https://codegolf.stackexchange.com/questions/140292/uncomment-a-cobol-program

            RunProgram("m6@4%C_7t",
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
            RunProgram("Vc'y-M'|*X{yx|fri@}R", "reverse the consonants", "setenne sne cohtosarvr");
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

            RunProgram("20FUi|*su*+Q", "", expected);
            RunProgram("20F|1N_u*+Q", "", expected);
            RunProgram("20Fui|1*+Q", "", expected);
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

            RunProgram("{'**x)|pTm|pm", "3", expected);
        }

        [TestMethod]
        public void BinaryMultiplicationTest() {
            http://golf.shinh.org/p.rb?Binary+Multiplication
            RunProgramSingleInputs("_',/{|BmE*|B8|z",
                "00000011,00000011", "00001001",
                "00101001,00000110", "11110110",
                "00001111,00001011", "10100101");
        }

        [TestMethod]
        public void IllustrateLCMTest() {
            https://codegolf.stackexchange.com/questions/143725/illustrate-the-least-common-multiple
            RunProgram(",c|l~mv'-*'|+;_/*", "[6 4]", "-----|-----|", "---|---|---|");
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

            RunProgram("scj~FiY;{%cvx/-' xv*y*_xy*tx(+(mJTc!CP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWiYdc{%cvx/-' xv*y*_xy*tx(+(mJTc!CP", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWiYxv*z)[s{%cvx/-_xy*tx((mJTc!C+P", "4\nStaphylococcus saprophyticus", expected);
            RunProgram("djWc{%cvx/-_x|i*tx((mJTc!Cixv*z)pP", "4\nStaphylococcus saprophyticus", expected);
        }

        [TestMethod]
        public void PalindromeRangeTest() {
            https://codegolf.stackexchange.com/questions/3532/enumerate-all-palindromic-numbers-in-decimal-between-0-and-n

            RunProgram("^rf$cr=", "33", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "11", "22", "33");
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

            RunProgramSingleInputs("^|r{$[s#f%",
                "\"3\" 1 100", "19",
                "\"12\" -200 200", "24",
                "\"123\" 1 3", "0",
                "\"3\" 33 34", "2",
                "\"0\" -1 1", "1",
                "\"127\" -12 27", "0");

        }

        [TestMethod]
        public void NarcissisticArrayElementsTest() {
            https://codegolf.stackexchange.com/questions/144358/narcissistic-array-elements
            RunProgram("XfHxiv@xi^@+>!", "[6 9 4 10 16 18 13]", "6", "4", "10");
        }

        [TestMethod]
        public void AdditivePersistence() {
            https://codegolf.stackexchange.com/questions/1775/additive-persistence
            RunProgram("mq' p{i~c%1=C{]em|+$W,", "74621\n39\n2677889\n0",
                "74621 2",
                "39 2",
                "2677889 3",
                "0 0");
            RunProgram("mq' pe{9>}{${]em|+gf%", "74621\n39\n2677889\n0",
                "74621 2",
                "39 2",
                "2677889 3",
                "0 0");
            RunProgram("mq| e{9>}{E|+gf%", "74621\n39\n2677889\n0",
                "74621 2",
                "39 2",
                "2677889 3",
                "0 0");
        }

        [TestMethod]
        public void KaprekarsMythicalTest() {
            https://codegolf.stackexchange.com/questions/2762/code-golf-6174-kaprekars-mythical-constant
            RunProgram(
                "i{{oXrqi~\" - \"pxp\" = \"pexe-$4|zQc6174$=!w`W8fhc0`p,^p\".",
                "2607",
                "7620 - 0267 = 7353",
                "7533 - 3357 = 4176",
                "7641 - 1467 = 6174",
                "Iterations: 3.");

            RunProgram(
                "i{{oXrqi~\" - \"pxp\" = \"pexe-$4|zQc6174$=!w`W8fhc0`p,^p\".",
                "1211",
                "2111 - 1112 = 0999",
                "9990 - 0999 = 8991",
                "9981 - 1899 = 8082",
                "8820 - 0288 = 8532",
                "8532 - 2358 = 6174",
                "Iterations: 5.");
        }

        [TestMethod]
        public void ExtractLocalMaximaTest() {
            https://codegolf.stackexchange.com/questions/132451/extract-local-maxima
            RunProgram("0|S3Bm|M_1@X-Cx", "[4,2,6,12,4,5,4,3]", "4", "12", "5");
        }

        [TestMethod]
        public void PlotCircleTest() {
            http://golf.shinh.org/p.rb?Plot+Circle
            string[] expected = {
                "          *          ",
                "      *********      ",
                "    *************    ",
                "   ***************   ",
                "  *****************  ",
                "  *****************  ",
                " ******************* ",
                " ******************* ",
                " ******************* ",
                " ******************* ",
                "*********************",
                " ******************* ",
                " ******************* ",
                " ******************* ",
                " ******************* ",
                "  *****************  ",
                "  *****************  ",
                "   ***************   ",
                "    *************    ",
                "      *********      ",
                "          *          ",
            };

            RunProgram("Nx^|rYmc*y{[c*+xx*>' '*?m", "10", expected);
            RunProgram("Nx^|rmc*xx*-|q^'**x^)|p", "10", expected);
            RunProgram("Nx^|rmJxJ-|q^'**x^)|p", "10", expected);
        }

        [TestMethod]
        public void YTest() {
            http://golf.shinh.org/p.rb?Y
            RunProgram("_m89-Ci",
                "ZZXZYXXYZZXZZYZXYZZZYXZZZZZYYXZXXXXZZX",
                "4",
                "7",
                "13",
                "16",
                "20",
                "27",
                "28");
        }

        [TestMethod]
        public void ZTest() {
            http://golf.shinh.org/p.rb?Z
            RunProgram("'Z*Qx{^'Z)FL2tm", "1", "Z");
            RunProgram("'Z*Qx{^'Z)FL2tm", "5",
                "ZZZZZ",
                "   Z",
                "  Z",
                " Z",
                "ZZZZZ");
        }

        [TestMethod]
        public void RationalPrimeFactorialTest() {
            https://codegolf.stackexchange.com/questions/144571/writing-rational-numbers-as-ratio-of-factorials-of-primes
            RunProgram(
                "u*~zXYd{cl{|f1s+HmcEy+Ydx+Xd{{/FuFcvwdyxW1-JP",
                    "10,9",
                    "2 5",
                    "3 3 3");
            RunProgram(
                "u*~zXYd{cl{|fO+HmcEy+Ydx+Xd{{/FuFcvwdyxW1-JP",
                    "6,1",
                    "3",
                    "");
            RunProgram(
                "â-ô►π╠#JP┘H*⌠gY>σô¡}U1$◙å╢º┌∟<íR∩k╗╫☺",
                    "6,1",
                    "3",
                    "");
            
        }

        [TestMethod]
        public void ExpandComparisonChainsTest() {
            https://codegolf.stackexchange.com/questions/144700/expand-comparison-chains
            RunProgramSingleInputs(@"""(\D+)""|s3B{i|ef"" && ""*",
                "3<4<5", "3<4 && 4<5",
                "3<4<5<6<7<8<9", "3<4 && 4<5 && 5<6 && 6<7 && 7<8 && 8<9",
                "3<5==6<19", "3<5 && 5==6 && 6<19",
                "10>=5<7!=20", "10>=5 && 5<7 && 7!=20",
                "15==15==15==15==15", "15==15 && 15==15 && 15==15 && 15==15");
        }

        [TestMethod]
        public void FloorLog2Test() {
            http://golf.shinh.org/p.rb?floor+log2
            RunProgram("me|B%v", "1\n2\n3\n4\n5\n6\n7\n8",
                "0", "1", "1", "2", "2", "2", "2", "3");
        }

        [TestMethod]
        public void AlphabetRain() {
            https://codegolf.stackexchange.com/questions/144868/make-some-alphabet-rain
            RunProgram("Q{]Va_]vI:mmMm", "abc !@ ABC",
                "abc !@ ABC",
                " bc     BC",
                "  c      C");
        }

        [Ignore]
        [TestMethod]
        public void SingerLettersTest() {
            https://codegolf.stackexchange.com/questions/144848/26-singers-26-letters
            RunProgramSingleInputs("m|+98%40%c27 31:b18*-]\" 7`\"9#;$<&=\"{40%m|th65+]",
                "Aretha Franklin", "V",
                "Ray Charles", "S",
                "Elvis Presley", "N",
                "Sam Cooke", "R",
                "John Lennon", "L",
                "Marvin Gaye", "X",
                "Bob Dylan", "J",
                "Otis Redding", "M",
                "Stevie Wonder", "F",
                "James Brown", "K",
                "Paul McCartney", "W",
                "Little Richard", "B",
                "Roy Orbison", "A",
                "Al Green", "Q",
                "Robert Plant", "H",
                "Mick Jagger", "P",
                "Tina Turner", "I",
                "Freddie Mercury", "O",
                "Bob Marley", "D",
                "Smokey Robinson", "U",
                "Johnny Cash", "Z",
                "Etta James", "E",
                "David Bowie", "C",
                "Van Morrison", "G",
                "Michael Jackson", "Y",
                "Jackie Wilson", "T");
        }
    }
}
