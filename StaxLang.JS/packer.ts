import * as int from './integer';

export const codePage = "\u00f8\u263a\u263b\u2665\u2666\u2663\u2660\u2022\u25d8\u25cb\u25d9\u2642\u2640\u266a\u266b\u263c\u25ba\u25c4\u2195\u203c\u00b6\u00a7\u25ac\u21a8\u2191\u2193\u2192\u2190\u221f\u2194\u25b2\u25bc !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u2302\u00c7\u00fc\u00e9\u00e2\u00e4\u00e0\u00e5\u00e7\u00ea\u00eb\u00e8\u00ef\u00ee\u00ec\u00c4\u00c5\u00c9\u00e6\u00c6\u00f4\u00f6\u00f2\u00fb\u00f9\u00ff\u00d6\u00dc\u00a2\u00a3\u00a5\u20a7\u0192\u00e1\u00ed\u00f3\u00fa\u00f1\u00d1\u00aa\u00ba\u00bf\u2310\u00ac\u00bd\u00bc\u00a1\u00ab\u00bb\u2591\u2592\u2593\u2502\u2524\u2561\u2562\u2556\u2555\u2563\u2551\u2557\u255d\u255c\u255b\u2510\u2514\u2534\u252c\u251c\u2500\u253c\u255e\u255f\u255a\u2554\u2569\u2566\u2560\u2550\u256c\u2567\u2568\u2564\u2565\u2559\u2558\u2552\u2553\u256b\u256a\u2518\u250c\u2588\u2584\u258c\u2590\u2580\u03b1\u00df\u0393\u03c0\u03a3\u03c3\u00b5\u03c4\u03a6\u0398\u03a9\u03b4\u221e\u03c6\u03b5\u2229\u2261\u00b1\u2265\u2264\u2320\u2321\u00f7\u2248\u00b0\u2219\u00b7\u221a\u207f\u00b2\u25a0\u0394";
let CodePageIndex: {[char: string]: number} = {};
for (let i = 0; i < 256; i++) CodePageIndex[codePage[i]] = i;

export function pack(asciiStax: string): string {
    let bytes = packBytes(asciiStax);
    let chars = bytes.reverse().map(b => codePage[b]);
    return chars.join("");
}

export function packBytes(asciiStax: string): number[] {
    // move trailing spaces to front
    asciiStax = asciiStax.replace(/^(.+?)( +)$/, "$2$1");

    let big = 0n;
    let result: number[] = [];
    for (let i = asciiStax.length - 1; i >= 0; i--) {
        big = int.add(int.mul(big, 95n), BigInt(asciiStax.charCodeAt(i) - 32));
    }
    while (int.floatify(big) > 0) {
        let b = int.mod(big, 0x100n);
        if (big === b) {
            if (int.bitand(b, 0x80n) === 0n) {
                b = int.bitor(b, 0x80n); // set leading bit for packing flag
            }
            else { // we need a whole nother byte to set the flag
                result.push(int.floatify(b));
                b = 0x80n;
            }
        }
        result.push(int.floatify(b));
        big = int.div(big, 0x100n);
    }
    return result;
}

export function staxDecode(packedStax: string): Uint8Array | undefined {
    let bytes: number[] = [];
    for (let c of packedStax) {
        let byte = CodePageIndex[c];
        if (byte == null || bytes.length === 0 && byte < 0x80) return undefined;
        bytes.push(byte);
    }
    return new Uint8Array(bytes);
}

export function staxEncode(bytes: number[] | Uint8Array): string {
    let result = "";
    for (let b of bytes) result += codePage[b];
    return result;
}

export function unpack(packedStax: string): string {
    const decoded = staxDecode(packedStax);
    if (decoded == null) throw new Error("not a packed program");
    return unpackBytes(decoded);
}

export function unpackBytes(bytes: number[] | Uint8Array): string {
    let result = "";
    let big = 0n;
    bytes[0] &= 0x7f;
    for (let i = 0; i < bytes.length; i++) {
        big = int.add(int.mul(big, 0x100n), BigInt(bytes[i]));
    }
    while (int.floatify(big) > 0) {
        result += String.fromCharCode(int.floatify(int.mod(big, 95n)) + 32);
        big = int.div(big, 95n);
    }

    // move leading spaces to end
    result = result.replace(/^( +)(.+)$/, "$2$1");
    return result;
}

export function isPacked(stax: string | number[] | Uint8Array): boolean {
    if (typeof stax === 'string') return staxDecode(stax) != null;
    return stax[0] >= 0x80;
}