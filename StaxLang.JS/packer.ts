import * as bigInt from 'big-integer';

const CodePage = "ø☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■Δ";

let CodePageIndex: {[char: string]: number} = {};
for (let i = 0; i < 256; i++) CodePageIndex[CodePage[i]] = i;

export function pack(asciiStax: string): string {
    let bytes = packBytes(asciiStax);
    let chars = bytes.reverse().map(b => CodePage[b]);
    return chars.join("");
}

export function packBytes(asciiStax: string): number[] {
    let big = bigInt.zero;
    let result: number[] = [];
    for (let i = asciiStax.length - 1; i >= 0; i--) {
        big = big.multiply(95).add(asciiStax.charCodeAt(i) - 32);
    }
    while (big.isPositive()) {
        let b = big.mod(bigInt[0x100]);
        if (big.eq(b)) {
            if (b.and(bigInt[0x80]).isZero()) {
                b = b.or(bigInt[0x80]); // set leading bit for packing flag
            }
            else { // we need a whole nother byte to set the flag
                result.push(b.valueOf());
                b = bigInt[0x80];
            }
        }
        result.push(b.valueOf());
        big = big.divide(bigInt[0x100]);
    }
    return result;
}

export function unpack(packedStax: string): string {
    let bytes = packedStax.split('').map(c => CodePageIndex[c]);
    return unpackBytes(bytes);
}

export function unpackBytes(bytes: number[]) {
    let result = "";
    let big = bigInt.zero;
    bytes[0] &= 0x7f;
    for (let i = 0; i < bytes.length; i++) {
        big = big.multiply(0x100).add(bytes[i]);
    }
    while (big.isPositive()) {
        result += String.fromCharCode(big.mod(bigInt[95]).valueOf() + 32);
        big = big.divide(bigInt[95]);
    }
    return result;
}

export function isPacked(stax: string | number[]) {
    if (typeof stax === 'string') return stax.charCodeAt(0) >= 0x80;
    return stax[0] >= 0x80;
}