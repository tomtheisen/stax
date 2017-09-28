using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StaxLang.Tests {
    [TestClass]
    public class FeatureTests {
        internal void RunProgram(string source, string expected, string input = null) {
            var writer = new StringWriter();
            new Executor(writer).Run(source, input == null ? Array.Empty<string>() : new[] { input });
            Assert.AreEqual(expected, writer.ToString().TrimEnd('\r', '\n'));
        }

        // Numerics
        [TestMethod] public void IntLiteral() => RunProgram("123", "123");
        [TestMethod] public void BigIntLiteral() => RunProgram("999999999999999999999999999999999999999", "999999999999999999999999999999999999999");
        [TestMethod] public void FloatLiteral() => RunProgram("1.23", "1.23");
        [TestMethod] public void Addition() => RunProgram("2 3+", "5");
        [TestMethod] public void Subtraction() => RunProgram("2 3-", "-1");
        [TestMethod] public void Multiplication() => RunProgram("2 3*", "6");
        [TestMethod] public void Division() => RunProgram("14 3/", "4");
        [TestMethod] public void Modulus() => RunProgram("11 4%", "3");
        [TestMethod] public void Increment() => RunProgram("7^", "8");
        [TestMethod] public void Decrement() => RunProgram("7v", "6");
        [TestMethod] public void LeadingZero() => RunProgram("04*", "0");
        [TestMethod] public void NumberToString() => RunProgram("3$4$+", "34");
        [TestMethod] public void Halve() => RunProgram("13h", "6");
        [TestMethod] public void Unhalve() => RunProgram("6H", "12");
        [TestMethod] public void Negate() => RunProgram("4N", "-4");
        [TestMethod] public void Exponent() => RunProgram("3 4|*", "81");
        [TestMethod] public void PowerOfTen() => RunProgram("3 |A", "1000");
        [TestMethod] public void BaseConvert() => RunProgram("255 16 |b", "ff");
        [TestMethod] public void BaseUnconvert() => RunProgram("\"ff\" 16 |b", "255");
        [TestMethod] public void BinaryConvert() => RunProgram("5 |B", "101");
        [TestMethod] public void BinaryUnConvert() => RunProgram("\"101\" |B", "5");
        [TestMethod] public void HexConvert() => RunProgram("255 |H", "ff");
        [TestMethod] public void HexUnconvert() => RunProgram("\"ff\" |H", "255");
        [TestMethod] public void PrimeFactorize() => RunProgram("12 |f ',*", "2,2,3");
        [TestMethod] public void GCD() => RunProgram("12 18 |g", "6");
        [TestMethod] public void LCM() => RunProgram("12 18 |l", "36");
        [TestMethod] public void PrimeTest() => RunProgram("97 |p", "1");
        [TestMethod] public void IsEven() => RunProgram("3 |e", "0");
        [TestMethod] public void IncDecXTest() => RunProgram("|X|X|X|x Lr $", "1232");
        [TestMethod] public void Minimum() => RunProgram("3 7 |m", "3");
        [TestMethod] public void Maximum() => RunProgram("3 7 |M", "7");
        [TestMethod] public void AbsoluteValue() => RunProgram("5|a", "5");
        [TestMethod] public void NegativeAbsoluteValue() => RunProgram("5N|a", "5");
        [TestMethod] public void RepeatedDivide() => RunProgram("12 2 |/", "3");

        // Bitwise
        [TestMethod] public void BitwiseNot() => RunProgram("3|~", "-4");
        [TestMethod] public void BitwiseAnd() => RunProgram("3 5|&", "1");
        [TestMethod] public void BitwiseOr() => RunProgram("3 5||", "7");
        [TestMethod] public void BitwiseXor() => RunProgram("3 5|^", "6");

        // Logic
        [TestMethod] public void Equality() => RunProgram("3 3=", "1");
        [TestMethod] public void Inequality() => RunProgram("3 4=", "0");
        [TestMethod] public void LessThan() => RunProgram("1 2<", "1");
        [TestMethod] public void GreaterThan() => RunProgram("1 2>", "0");
        [TestMethod] public void NotTrue() => RunProgram("7!", "0");
        [TestMethod] public void NotFalse() => RunProgram("0!", "1");
        [TestMethod] public void If() => RunProgram("\"not equal\"\"equal\" 1 2=?", "not equal");

        // String
        [TestMethod] public void Char() => RunProgram("'a", "a");
        [TestMethod] public void StringLiteral() => RunProgram("\"hello\"", "hello");
        [TestMethod] public void UnterminatedStringLiteral() => RunProgram("\"hello", "hello");
        [TestMethod] public void EscapedString() => RunProgram("\"a`\"b``c", "a\"b`c");
        [TestMethod] public void Concat() => RunProgram("\"hello\" \"world\"+", "helloworld");
        [TestMethod] public void RepeatString() => RunProgram("\"abc\"4*", "abcabcabcabc");
        [TestMethod] public void StringSubtraction() => RunProgram("\"hello world\" \"ol\" -", "he wrd");
        [TestMethod] public void StringJoin() => RunProgram("'a'b'c'd L '-*", "d-c-b-a");
        [TestMethod] public void CharacterInterleave() => RunProgram("\"abcd\"M '- *", "a-b-c-d");
        [TestMethod] public void Upper() => RunProgram("\"Hello\"^", "HELLO");
        [TestMethod] public void Lower() => RunProgram("\"Hello\"v", "hello");
        [TestMethod] public void TruncateRight() => RunProgram("\"Hello\" 3(", "Hel");
        [TestMethod] public void PadRight() => RunProgram("\"Hello\" 8(", "Hello   ");
        [TestMethod] public void TruncateLeft() => RunProgram("\"Hello\" 3)", "llo");
        [TestMethod] public void PadLeft() => RunProgram("\"Hello\" 8)", "   Hello");
        [TestMethod] public void FindIndex() => RunProgram("\"Hello World\" \"Wo\" I", "6");
        [TestMethod] public void UnfoundIndex() => RunProgram("\"Hello World\" \"Wr\" I", "-1");
        [TestMethod] public void ChunkString() => RunProgram("\"abcdefgh\" 3/ ',*", "abc,def,gh");
        [TestMethod] public void Transpose() => RunProgram("\"abcdefgh\" 3/ M ',*", "adg,beh");
        [TestMethod] public void TrimLeft() => RunProgram("\"  abc  \" t", "abc  ");
        [TestMethod] public void TrimRight() => RunProgram("\"  abc  \" T", "  abc");
        [TestMethod] public void TrimLeftBy() => RunProgram("\"hello world\" 2 t", "llo world");
        [TestMethod] public void TrimRightBy() => RunProgram("\"hello world\" 2 T", "hello wor");
        [TestMethod] public void Unique() => RunProgram("\"Hello World\" u", "Helo Wrd");
        [TestMethod] public void RegexReplace() => RunProgram("\"axbxxcxxxd\" \"x+\" 'z R", "azbzczd");
        [TestMethod] public void Translate() => RunProgram("\"Hello World\" \"e3o0\" |t", "H3ll0 W0rld");
        [TestMethod] public void Batch() => RunProgram("\"hello\" 3B ',*", "hel,ell,llo");
        [TestMethod] public void RotateRight() => RunProgram("\"asdf\" |)", "fasd");
        [TestMethod] public void RotateLeft() => RunProgram("\"asdf\" |(", "sdfa");
        [TestMethod] public void RegexFind() => RunProgram("\"Hello. Good to see you.\" \"o+\" |f ', *", "o,oo,o,o");
        [TestMethod] public void RegexSplit() => RunProgram("\"Hello. Good to see you.\" \"o+\" |s ', *", "Hell,. G,d t, see y,u.");
        [TestMethod] public void Prefixes() => RunProgram("\"abc\" |[ ',*", "a,ab,abc");
        [TestMethod] public void Suffixes() => RunProgram("\"abc\" |] ',*", "abc,bc,c");
        [TestMethod] public void ZeroFill() => RunProgram("\"abc\" 5 |z", "00abc");
        [TestMethod] public void CompressedLiterals() => RunProgram(".6Js2%", "literal");
        [TestMethod] public void SubstringOccurrences() => RunProgram("\"drab cab\" \"ab\" #", "2");

        // Array
        [TestMethod] public void ZeroRange() => RunProgram("5r',*", "0,1,2,3,4");
        [TestMethod] public void OneRange() => RunProgram("5R',*", "1,2,3,4,5");
        [TestMethod] public void ReverseArray() => RunProgram("5R r ',*", "5,4,3,2,1");
        [TestMethod] public void ConcatArray() => RunProgram("3R 4R + ',*", "1,2,3,1,2,3,4");
        [TestMethod] public void ConcatArrayElement() => RunProgram("3R 7 + ',*", "1,2,3,7");
        [TestMethod] public void RepeatArray() => RunProgram("3R 2* ',*", "1,2,3,1,2,3");
        [TestMethod] public void RepeatArrayBackwards() => RunProgram("3R 2N* ',*", "3,2,1,3,2,1");
        [TestMethod] public void Explode() => RunProgram("5R E +", "9");
        [TestMethod] public void StringArrayEquivalence() => RunProgram("\"abc\" ',*", "97,98,99");
        [TestMethod] public void ArrayLength() => RunProgram("5R%", "5");
        [TestMethod] public void ReadIndex() => RunProgram("5R 2@", "3");
        [TestMethod] public void ReadIndexes() => RunProgram("5R 2]1N]+ @ ',*", "3,5");
        [TestMethod] public void AssignIndex() => RunProgram("5R 1 8& ',*", "1,8,3,4,5");
        [TestMethod] public void ArrayToString() => RunProgram("5R$", "12345");
        [TestMethod] public void SingletonWrap() => RunProgram("0]%", "1");
        [TestMethod] public void Head() => RunProgram("5R h", "1");
        [TestMethod] public void Tail() => RunProgram("5R H", "5");
        [TestMethod] public void ShowArray() => RunProgram("'x]S", "x");
        [TestMethod] public void Sum() => RunProgram("5R |+", "15");
        [TestMethod] public void MinimumArray() => RunProgram("5R Oh", "1");
        [TestMethod] public void MaximumArray() => RunProgram("5R OH", "5");
        [TestMethod] public void Delta() => RunProgram("1] 1]+ 3]+ 8]+ |- ',*", "0,2,5");
        [TestMethod] public void JoinWithNewlines() => RunProgram("\"abcd\" 2/ |J r", "dc\r\nba");
        [TestMethod] public void Palindromize() => RunProgram("\"abcb\" |p", "abcbcba");
        [TestMethod] public void ZipRep() => RunProgram("\"abcde\" \"xy\" \\ ',*", "ax,by,cx,dy,ex");
        [TestMethod] public void Union() => RunProgram("1]2]+2]+3]+3]+  3r |& ',*", "1,2,2");
        [TestMethod] public void SymmetricDiff() => RunProgram("1]2]+2]+3]+3]+  3r |^ ',*", "3,3,0");
        [TestMethod] public void CountInTest() => RunProgram("1]2]+2]+3]+3]+ 3 #", "2");

        // Constants
        [TestMethod] public void Ten() => RunProgram("A", "10");
        [TestMethod] public void MinusUnit() => RunProgram("U", "-1");
        [TestMethod] public void UpperAlpha() => RunProgram("VA", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        [TestMethod] public void LowerAlpha() => RunProgram("Va", "abcdefghijklmnopqrstuvwxyz");
        [TestMethod] public void UpperConsonants() => RunProgram("VC", "BCDFGHJKLMNPQRSTVWXYZ");
        [TestMethod] public void LowerConsonants() => RunProgram("Vc", "bcdfghjklmnpqrstvwxyz");
        [TestMethod] public void Digits() => RunProgram("Vd", "0123456789");
        [TestMethod] public void UpperVowels() => RunProgram("VV", "AEIOU");
        [TestMethod] public void LowerVowels() => RunProgram("Vv", "aeiou");
        [TestMethod] public void UpperWord() => RunProgram("VW", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        [TestMethod] public void LowerWord() => RunProgram("Vw", "0123456789abcdefghijklmnopqrstuvwxyz");
        [TestMethod] public void Whitespace() => RunProgram("Vs", " \t\r" + Environment.NewLine + "\v");
        [TestMethod] public void Newline() => RunProgram("Vn 'x+", Environment.NewLine + "x");

        // I/O
        [TestMethod] public void DefaultOutput() => RunProgram("1 2 3", "3");
        [TestMethod] public void Print() => RunProgram("1 2P 3", "2");
        [TestMethod] public void SuppressedNewline() => RunProgram("1p 2P 3", "12");
        [TestMethod] public void TokenizeNumberInput() => RunProgram("nn+", "7", "3 4");
        [TestMethod] public void PeekPrint() => RunProgram("7qqQ", "777");
        [TestMethod] public void PrintNewline() => RunProgram("|P", "");
        [TestMethod] public void Eval() => RunProgram("e|+", "6", "[1, 2, 3]");

        // Blocks
        [TestMethod] public void RepeatBlock() => RunProgram("{1p}3*", "111");
        [TestMethod] public void ShorthandRepeatBlock() => RunProgram("3F1p", "111");
        [TestMethod] public void DoWhile() => RunProgram("3{q^c8-w", "34567");
        [TestMethod] public void DoWhileShorthand() => RunProgram("3wq^c8-", "34567");
        [TestMethod] public void While() => RunProgram("3{q^c8=C'-pW", "3-4-5-6-7");
        [TestMethod] public void WhileShorthand() => RunProgram("3Wq^c8=C'-p", "3-4-5-6-7");
        [TestMethod] public void IfBlocks() => RunProgram(" \"equal\" {\"not \"s+} {} 1 2=?", "not equal");
        [TestMethod] public void Filter() => RunProgram("5R {2%f ',*", "1,3,5");
        [TestMethod] public void ForEach() => RunProgram("5R {3+pF", "45678");
        [TestMethod] public void Map() => RunProgram("5R {c*m ',*", "1,4,9,16,25");
        [TestMethod] public void OrderBy() => RunProgram("5R {c*5%O ',*", "5,1,4,2,3");
        [TestMethod] public void IterationIndex() => RunProgram("'x]4* {p ':p ip ' p F", "x:0 x:1 x:2 x:3 ");
        [TestMethod] public void IteratingVariable() => RunProgram("3R {$_*pF", "122333");
        [TestMethod] public void RegexReplaceBlock() => RunProgram("\"axbxxcxxxd\" \"x+\"{%$}R", "a1b2c3d");
        [TestMethod] public void ConditionalCancel() => RunProgram("12p 1C 34p", "12");


        // Stack operations
        [TestMethod] public void Copy() => RunProgram("1c+", "2");
        [TestMethod] public void Dig() => RunProgram("'a'b'c'd 2D", "b");
        [TestMethod] public void ListifyStack() => RunProgram("1 2 3 4 5 L ',*", "5,4,3,2,1");
        [TestMethod] public void ListifyN() => RunProgram("1 2 3 4 5 3l ',*", "3,4,5");
        [TestMethod] public void SideStack() => RunProgram("1 2 3 4 ~~p;p,ppp", "23314");
        [TestMethod] public void Discard() => RunProgram("11 22 33 d", "22");
        [TestMethod] public void Swap() => RunProgram("1 2 s pp", "12");
        [TestMethod] public void StackDepth() => RunProgram("1 1 1 1 |d", "4");
        [TestMethod] public void SideStackDepth() => RunProgram("1 1 1 1 ~~~ |D", "3");
    }
}

