using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using StaxLang;
using System.IO;

namespace StaxLang.Tests {
    [TestClass]
    public class Tests {
        internal static string[] MultiLineStrip(string arg) {
            var result = arg.Trim().Split(new[] { Environment.NewLine }, 0)
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

        [TestMethod]
        public void IntLiteral() {
            RunProgram("123", "", "123");
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
        public void MultipleOutputs() {
            RunProgram("123\"hello\"", "", "123", "hello");
        }

        [TestMethod]
        public void UnterminatedString() {
            RunProgram("\"asdf", "", "asdf");
        }

        [TestMethod]
        public void EscapedString() {
            RunProgram("\"x\\\"x\"", "", "x\"x");
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
        [ExpectedException(typeof(Exception))]
        public void BadPlusTest() {
            RunProgram("\"abc\"123+", "");
        }

        [TestMethod]
        public void RangeTest() {
            RunProgram("5r", "", "0", "1", "2", "3", "4");
            RunProgram("5R", "", "1", "2", "3", "4", "5");
        }

        [TestMethod]
        public void StarOverloadsTest() {
            RunProgram("2r3*", "", "0", "1", "0", "1", "0", "1");
            RunProgram("4\"abc\"*", "", "abcabcabcabc");
            RunProgram("{1}3*", "", "1", "1", "1");
        }

        [TestMethod]
        public void CopyTest() {
            RunProgram("1c", "", "1", "1");
        }

        [TestMethod]
        public void InputTest() {
            RunProgram("#2*", "3", "6");
        }

        [TestMethod]
        public void SquaresTest() {
            RunProgram("5R{c*}m", "", "1", "4", "9", "16", "25");
        }

        [TestMethod]
        public void WhileTest() {
            RunProgram("3{cic}w", "", "3", "2", "1", "0");
        }

        [TestMethod]
        public void CollatzTest() {
            RunProgram("#{ch1C3*I2C2%?ci}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("#{cXhx3*Ix2%?ci}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("#{cXhx3*Ix2%?ciw", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void MyTestMethod() {
            RunProgram("4Ri", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("#XiR{xs%!f", "12", "1", "2", "3", "4", "6");
            RunProgram("#XiR{xs%!fi#", "12", "4");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("1{IXiRi{xs%!f#!{x}*xc20-wd", "", "2", "3", "5", "7", "11", "13", "17", "19");
            RunProgram("2Xd{xiRi{xs%!f#!{x}*xIX20-w", "", "2", "3", "5", "7", "11", "13", "17", "19");
            RunProgram("d2Zd{ziRi{zs%!f#!{z}*zIZx-w", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("#R{'**m", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1s#R{*cmd", "5", "120");
            RunProgram("1s#R{*F", "5", "120");
            RunProgram("1s#X{*xiXxwd", "5", "120");
        }

        [TestMethod]
        public void ListifyTest() {
            RunProgram("1 2 3 L-", "", "1", "2", "3");
        }

        [TestMethod]
        public void ReverseTest() {
            RunProgram("-", "asdf", "fdsa");
        }

        [TestMethod]
        public void EqualTest() {
            RunProgram("1 2=", "", "0");
            RunProgram("1 1=", "", "1");
        }

        [TestMethod]
        public void DiagonalTest() {
            RunProgram(@"#R{'\)m", "3", @"\", @" \", @"  \");
            RunProgram(@"#R{'\)PF", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"#XR{c'\)sxs-HI'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{c'\)Oxs-HI'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{'\)Ox_-HI'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{'\)x_-HI'/)+m", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"#XR{'\)x_i-H'/)+mxI'X)xR-{'/)x_i-H'\)+m", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }

        [TestMethod]
        public void SplitTest() {
            RunProgram("l", "asdf", "a", "s", "d", "f");
        }

        [TestMethod]
        public void PairSpacingTest() {
            RunProgram("2/' *", "sequencespacingtest", "se qu en ce sp ac in gt es t");
        }

        [TestMethod]
        public void PairSpacing2Test() {
            RunProgram("2/{t' s+m\"\"*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{t' s+me*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tm' *", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
        }

        [TestMethod]
        public void PairSpacing3Test() {
            RunProgram("' /{2/' *m' *", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("' Z/{2/z*mz*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("S/{2/S*mS*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
        }

        [TestMethod]
        public void DigitTallyTest() {
            RunProgram("d10r{$ys/#i$me*", "176093677603", "2102003301");
            RunProgram("L{Xd10r{$xs/#i$me*F", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
            RunProgram("Xd10r{$xs/#i$me*PD", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/#i$me*PdD", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/#i$OFNdD", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/#iOFNdD", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$:/#iOFNdD", "27204322879364" + Environment.NewLine + "82330228112748", "1042201211", "1242100130");
        }

        [TestMethod]
        public void SmileyTest() {
            RunProgram("':\":-\"{c')+}3*", "", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':c'-+{c')+}3*", "", ":", ":-", ":-)", ":-))", ":-)))");
        }

        [TestMethod]
        public void DeleteBlanksTest() {
            RunProgram("L{f", "1" + Environment.NewLine + Environment.NewLine + "2" + Environment.NewLine + Environment.NewLine + Environment.NewLine + "3", "1", "2", "3");
        }
    }
}
