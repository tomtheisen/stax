using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using StaxLang;
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
            RunProgram("n2*", "3", "6");
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
            RunProgram("n{ch1C3*I2C2%?ci}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("n{cXhx3*Ix2%?ci}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("n{cXhx3*Ix2%?ciw", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void MyTestMethod() {
            RunProgram("4Ri", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("nXiR{xs%!f", "12", "1", "2", "3", "4", "6");
            RunProgram("nXiR{xs%!fin", "12", "4");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("1{IXiRi{xs%!fn!{x}*xc20-wd", "", "2", "3", "5", "7", "11", "13", "17", "19");
            RunProgram("2Xd{xiRi{xs%!fn!{x}*xIX20-w", "", "2", "3", "5", "7", "11", "13", "17", "19");
            RunProgram("d2Zd{ziRi{zs%!fn!{z}*zIZx-w", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("nR{'**m", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1snR{*cmd", "5", "120");
            RunProgram("1snR{*F", "5", "120");
            RunProgram("1snX{*xiXxwd", "5", "120");
        }

        [TestMethod]
        public void ListifyTest() {
            RunProgram("1 2 3 L-", "", "3", "2", "1");
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
            RunProgram(@"nR{'\)m", "3", @"\", @" \", @"  \");
            RunProgram(@"nR{'\)PF", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"nXR{c'\)sxs-HI'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"nXR{c'\)Oxs-HI'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"nXR{'\)Ox_-HI'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"nXR{'\)x_-HI'/)+m", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"nXR{'\)x_i-H'/)+mxI'X)xR-{'/)x_i-H'\)+m", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }
    }
}
