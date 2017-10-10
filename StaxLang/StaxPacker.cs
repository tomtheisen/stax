using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace StaxLang {
    public static class StaxPacker {
        private const string CodePage = "ø☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■Δ";

        private static readonly Dictionary<char, byte> CodePageIndex = CodePage
            .Select((c, i) => (c, i))
            .ToDictionary(t => t.c, t => (byte)t.i);

        public static string Pack(string stax) {
            var bytes = PackBytes(stax);
            var result = string.Concat(bytes.Reverse().Select(b => CodePage[b]));
            return result;
        }

        public static byte[] PackBytes(string stax) {
            BigInteger big = 1;
            var result = new List<byte>();
            for (int i = stax.Length - 1; i >= 0; i--) big = big * 95 + stax[i] - ' ';
            while (big > 0) {
                byte b = (byte)(big % 0x100);
                if (big == b) {
                    if ((b & 0x80) == 0) {
                        b |= 0x80; // set leading bit for packing flag
                    }
                    else { // we need a whole nother byte to set the flag
                        result.Add(b);
                        b = 0x80; // so many wasted bits
                    }
                }
                result.Add(b);
                big /= 0x100;
            }
            return result.ToArray();
        }

        public static string Unpack(string packed) {
            var bytes = packed.Select(c => CodePageIndex[c]).ToArray();
            return Unpack(bytes);
        }

        public static string Unpack(byte[] bytes) {
            string result = "";
            BigInteger big = 0;
            bytes[0] &= 0x7f;
            for (int i = 0; i < bytes.Length; i++) big = big * 0x100 + bytes[i];
            while (big > 1) {
                result += (char)((int)(big % 95) + ' ');
                big /= 95;
            }
            return result;
        }

        public static bool IsPacked(string stax) => stax.Length > 0 && stax[0] >= 0x80;
    }
}
