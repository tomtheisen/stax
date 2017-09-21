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
    }
}
