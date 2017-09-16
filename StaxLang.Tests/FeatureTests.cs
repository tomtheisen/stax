using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StaxLang.Tests {
    [TestClass]
    class FeatureTests {
        internal void RunProgram(string source, string expected, string input = null) {
            var writer = new StringWriter();
            new Executor(writer).Run(source, input == null ? Array.Empty<string>() : new[] { input });
            Assert.AreEqual(expected, writer.ToString());
        }

        // Numerics
        [TestMethod] public void IntLiteral() => RunProgram("123", "123");
        [TestMethod] public void BigIntLiteral() => RunProgram("999999999999999999999999999999999999999", "999999999999999999999999999999999999999");
        [TestMethod] public void FloatLiteral() => RunProgram("1.23", "1.23");
        [TestMethod] public void AdditionTest() => RunProgram("2 3+", "5");
        [TestMethod] public void ModulusTest() => RunProgram("11 4%", "3");

        // String
        [TestMethod] public void CharTest() => RunProgram("'a", "a");
        [TestMethod] public void StringLiteral() => RunProgram("\"hello\"", "hello");
        [TestMethod] public void UnterminatedStringLiteral() => RunProgram("\"hello", "hello");
        [TestMethod] public void EscapedString() => RunProgram("\"a`\"b``c", "", "a\"b`c");
        [TestMethod] public void ConcatTest() => RunProgram("\"hello\" \"world\"+", "helloworld");

        // Array
        [TestMethod] public void ZeroRangeTest() => RunProgram("5r',*", "0,1,2,3,4");
        [TestMethod] public void OneRangeTest() => RunProgram("5R',*", "1,2,3,4,5");

    }
}
