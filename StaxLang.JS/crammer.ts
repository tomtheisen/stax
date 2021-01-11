import { abs, floorSqrt } from './integer';
import { last, floatify } from './types';

const Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";

export function uncram(str: string): bigint[] {
    let result: bigint[] = [];
    let continuing = false, sign = 0;

    for (let i = 0; i < str.length; i++) {
        let charValue = Symbols.indexOf(str[i]);
        if (charValue < 0) throw new Error("Bad character for uncram");
        if (continuing) {
            let toAdd = (charValue >> 1) * sign;
            result[result.length - 1] = last(result)! * 46n + BigInt(toAdd);
        }
        else {
            sign = charValue % 4 >= 2 ? -1 : 1;
            result.push(BigInt((charValue >> 2) * sign));
        }
        continuing = charValue % 2 === 1;
    }

    if (continuing) { // offset mode
        for (let i = 1; i < result.length; i++) result[i] = result[i] + result[i - 1];
    }
    return result;
}

function encode(a: bigint[], offsetMode: boolean) {
    let result = "";
    if (offsetMode) {
        for (let i = a.length - 1; i > 0; i--) a[i] = a[i] - a[i - 1];
    }
    for (let i = 0; i < a.length; i++) {
        let parts: number[] = [], signBit = Number(a[i]) < 0 ? 2 : 0;
        let continuing = (offsetMode && i === a.length - 1) ? 1 : 0, remain = abs(a[i]);
        for (; Number(remain) > 22; remain = remain / 46n, continuing = 1) {
            parts.unshift(Number(remain % 46n) * 2 + continuing);
        }
        parts.unshift(Number(remain) * 4 + signBit + continuing);
        result += parts.map(p => Symbols[p]).join("");
    }
    return result;
}

export function cram(arr: bigint[]): string {
    let flat = encode(arr.slice(), false), offset = encode(arr.slice(), true);
    return `"${offset.length < flat.length ? offset : flat}"!`;
}

export function cramSingle(n: bigint): string {
    if (n === -1n) return "U";
    if (n < 0n) return cramSingle(-n) + "N";
    if (n === 10n) return "A";
    if (n === 256n) return "VB";
    if (n === 1_000n) return "Vk";
    if (n === 1_000_000n) return "VM";

    const sqrt = floorSqrt(n), isSquare = n === sqrt * sqrt;
    if (n >= 100n && isSquare) return cramSingle(sqrt) + "J";

    let best = n.toString();
    if (n < 10_000_000n) return best;

    for (var scalarCrammed = ""; n > 0n; n /= 93n) {
        scalarCrammed = Symbols[floatify(--n % 93n)] + scalarCrammed;
    }
    scalarCrammed = `"${scalarCrammed}"%`;
    if (scalarCrammed.length < best.length) best = scalarCrammed;
    return best;
}

export function uncramSingle(s: string): bigint {
    let result = 0n;
    for (let c of s) {
        result = result * 93n + BigInt(Symbols.indexOf(c) + 1);
    }
    return result;
}

export function compressIntAray(ints: bigint[]): string {
    let best : string | undefined = undefined;
    [cram, baseArrayCrammed, shortenRepeatedArray, shortenPair, asciiShorten].forEach(strat => {
        const out = strat(ints);
        if (out && (!best || out.length < best.length)) best = out;
    });
    return best!;
}

function baseArrayCrammed(arr: bigint[]): string | null {
    if (arr.length === 0) return "z";
    if (arr.some(e => e < 0n)) return null;

    let leadingZeroes = 0, anyNonZero = false;
    for (let e of arr) {
        if (e === 0n) leadingZeroes += 1; 
        else { 
            anyNonZero = true; 
            break; 
        }
    }
    if (!anyNonZero) {
        switch (arr.length) {
            case 1: return "0]";
            case 2: return "V%";
            default: return "0]" + cramSingle(BigInt(arr.length)) + "*";
        }
    }
    else switch (leadingZeroes) {
        case 0: break;
        case 1: return baseArrayCrammed(arr.slice(1)) + "Z+";
        case 2: return "V%" + baseArrayCrammed(arr.slice(2)) + "+";
        default:
            const zeroes = baseArrayCrammed(arr.slice(0, leadingZeroes));
            const rest = baseArrayCrammed(arr.slice(leadingZeroes));
            if (zeroes && rest) return zeroes + rest + "+";
            return null;
    }
    
    const base = arr.reduce((a, b) => a < b ? b : a) + 1n;
    const all = arr.reduce((a, b) => a * base + b);
    const part1 = cramSingle(all);
    let part2: string;
    if (base === 2n) part2 = ":B";
    else if (base === 10n) part2 = "E";
    else part2 = cramSingle(base) + "|E";

    let best = /\d$/.test(part1) && /^\d/.test(part2) 
        ? part1 + ' ' + part2
        : part1 + part2;

    // check for all single digits
    if (arr.every(e => e >= 0n && e < 10n) && arr[0] > 0n) {
        const all = arr.reduce((a, b) => a * 10n + b);
        const digited = cramSingle(all) + "E";
        if (digited.length < best.length) best = digited;
    }

    return best;
}

function shortenRepeatedArray(arr: bigint[]): string | null {
    if (arr.length < 2) return null;
    if (arr.some(e => e !== arr[0])) return null;
    if (arr[0] >= 32n && arr[0] < 127n) {
        return "'" + String.fromCharCode(floatify(arr[0])) + cramSingle(BigInt(arr.length)) + '*';
    }
    else return cramSingle(arr[0]) + ']' + cramSingle(BigInt(arr.length)) + '*';
}

 function shortenPair(arr: bigint[]): string | null {
    if (arr.length !== 2) return null;
    let a = cramSingle(arr[0]), b = cramSingle(arr[1]);
    if (a === b) b = 'c';
    return /\d$/.test(a) && /^\d/.test(b)
        ? a + ' ' + b + '\\'
        : a + b + '\\';
}

function asciiShorten(arr: bigint[]): string | null {
    if (arr.some(e => e < 32n || e >= 127n)) return null;
    const codes = arr.map(floatify);
    const content = String.fromCharCode(...codes);
    switch (content.length) {
        case 0: return null;
        case 1: return "'" + content;
        case 2: return "." + content;
        default: return '"' + content.replace(/"/g, '\\"') + '"';
    }
}