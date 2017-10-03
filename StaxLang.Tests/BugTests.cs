using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace StaxLang.Tests {
    [TestClass]
    public class BugTests {
        internal void RunProgram(string source, string expected, string input = null) {
            var writer = new StringWriter();
            new Executor(writer).Run(source, input == null ? Array.Empty<string>() : new[] { input });
            Assert.AreEqual(expected, writer.ToString().TrimEnd('\r', '\n'));
        }

        [TestMethod] public void NegativeDivision() => RunProgram("U2/", "-1");
        [TestMethod] public void NegativeMod() => RunProgram("U2%", "1");
        [TestMethod] public void RegexReplaceIndex() => RunProgram("'x'x+ 'x {i$} R", "01");
        [TestMethod] public void EmptyTranspose() => RunProgram("1p zMp", "1");
        [TestMethod] public void StringArrayDiff() => RunProgram("\"abc\"] \"abc\"] |^ %", "0");
        [TestMethod] public void ZeroFillNumber() => RunProgram("6 2 |z", "06");
        [TestMethod] public void UnfoundMulticharSubstringIndex() => RunProgram("\"abc\" \"xy\" I", "-1");
        [TestMethod] public void UnfoundMulticharSubstringOverlap() => RunProgram("\"abc\" \"cd\" I", "-1");
        [TestMethod] public void TooMuchTrim() => RunProgram("'a 3t", "");
        [TestMethod] public void ShorthandLoopIndex() => RunProgram("2F2Fip", "0101");
        [TestMethod] public void CompressedInBlock() => RunProgram("1{.O.F", "Thg");
        [TestMethod] public void MixedEqualityEmpty() => RunProgram("z 1 =", "0");
        [TestMethod] public void NegativeEval() => RunProgram("U$e", "-1");
    }
}
