using System;
using System.Text;

namespace StaxLang {
    public class PackedStaxEncoding : Encoding {
        // ø☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■Δ

        private Encoding CP437 = Encoding.GetEncoding(437);

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
            for (int i = 0; i < chars.Length; i++) {
                // NUL is unprintable and uncopyable
                if (chars[i] == 'ø') chars[i] = '\0';

                // nbsp - replaced because it doesn't survive round trip through browser and clipboard
                if (chars[i] == 'Δ') chars[i] = (char)0xa0; 
            }
            return CP437.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            int written = CP437.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            for (int i = 0; i < written; i++) {
                // these encode properly, but don't decode.  https://en.wikipedia.org/wiki/Code_page_437#Character_set
                if (chars[i] < 0x20) chars[i] = "ø☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼"[chars[i]];
                if (chars[i] == 0x7f) chars[i] = '⌂';

                // nbsp - replaced because it doesn't survive round trip through browser and clipboard
                if (chars[i] == 0xa0) chars[i] = 'Δ';
            }
            return written;
        }

        public override int GetByteCount(char[] chars, int index, int count) => count;
        public override int GetCharCount(byte[] bytes, int index, int count) => count;
        public override int GetMaxByteCount(int count) => count;
        public override int GetMaxCharCount(int count) => count;
    }
}
