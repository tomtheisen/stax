using System;
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
            BigInteger big = 1;
            for (int i = stax.Length - 1; i >= 0; i--) big = big * 95 + stax[i] - ' ';

            string result = "";
            while (big > 0) {
                byte b = (byte)(big % 0x100);
                if (big == b) {
                    if ((b & 0x80) == 0) {
                        b |= 0x80; // set leading bit for packing flag
                    }
                    else { // we need a whole nother byte to set the flag
                        result = CodePage[b] + result;
                        b = 0x80; // so many wasted bits
                    }
                }
                result = CodePage[b] + result;
                big /= 0x100;
            }
            return result;
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
