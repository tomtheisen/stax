import { last, floatify } from './types';
import * as int from './integer';
import { StaxInt } from './integer';

const Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";

export function uncram(str: string): StaxInt[] {
    let result: StaxInt[] = [];
    let continuing = false, sign = 0;

    for (let i = 0; i < str.length; i++) {
        let charValue = Symbols.indexOf(str[i]);
        if (charValue < 0) throw new Error("Bad character for uncram");
        if (continuing) {
            let toAdd = (charValue >> 1) * sign;
            result[result.length - 1] = int.add(int.mul(last(result)!, 46n), BigInt(toAdd));
        }
        else {
            sign = charValue % 4 >= 2 ? -1 : 1;
            result.push(BigInt((charValue >> 2) * sign));
        }
        continuing = charValue % 2 === 1;
    }

    if (continuing) { // offset mode
        for (let i = 1; i < result.length; i++) result[i] = int.add(result[i], result[i - 1]);
    }
    return result;
}

function encode(a: StaxInt[], offsetMode: boolean) {
    let result = "";
    if (offsetMode) {
        for (let i = a.length - 1; i > 0; i--) a[i] = int.sub(a[i], a[i - 1]);
    }
    for (let i = 0; i < a.length; i++) {
        let parts: number[] = [], signBit = int.floatify(a[i]) < 0 ? 2 : 0;
        let continuing = (offsetMode && i === a.length - 1) ? 1 : 0, remain = int.abs(a[i]);
        for (; int.floatify(remain) > 22; remain = int.div(remain, 46n), continuing = 1) {
            parts.unshift(int.floatify(int.mod(remain, 46n)) * 2 + continuing);
        }
        parts.unshift(int.floatify(remain) * 4 + signBit + continuing);
        result += parts.map(p => Symbols[p]).join("");
    }
    return result;
}

export function cram(arr: StaxInt[]): string {
    let flat = encode(arr.slice(), false), offset = encode(arr.slice(), true);
    return `"${offset.length < flat.length ? offset : flat}"!`;
}

export function cramSingle(n: StaxInt): string {
    if (int.eq(n, -1n)) return "U";
    if (int.cmp(n, 0n) < 0) return cramSingle(int.negate(n)) + "N";
    if (int.eq(n, 10n)) return "A";
    if (int.eq(n, 256n)) return "VB";
    if (int.eq(n, 1_000n)) return "Vk";
    if (int.eq(n, 1_000_000n)) return "VM";

    const sqrt = int.floorSqrt(n), isSquare = int.eq(n, int.mul(sqrt, sqrt));
    if (int.cmp(n, 100n) >= 0 && isSquare) return cramSingle(sqrt) + "J";

    let best = n.toString();
    if (int.cmp(n, BigInt(1e7)) < 0) return best;

    for (var scalarCrammed = ""; int.cmp(n, 0n) > 0; n = int.div(n, 93n)) {
        n = int.sub(n, 1n);
        scalarCrammed = Symbols[floatify(int.mod(n, 93n))] + scalarCrammed;
    }
    scalarCrammed = `"${scalarCrammed}"%`;
    if (scalarCrammed.length < best.length) best = scalarCrammed;
    return best;
}

export function uncramSingle(s: string): StaxInt {
    let result = 0n;
    for (let c of s) {
        result = int.add(int.mul(result, 93n), BigInt(Symbols.indexOf(c) + 1));
    }
    return result;
}

export function compressIntAray(ints: int.StaxInt[]): string {
    let best : string | undefined = undefined;
    [cram, baseArrayCrammed, shortenRepeatedArray, shortenPair, asciiShorten].forEach(strat => {
        const out = strat(ints);
        if (out && (!best || out.length < best.length)) best = out;
    });
    return best!;
}

function baseArrayCrammed(arr: int.StaxInt[]): string | null {
    if (arr.length === 0) return "z";
    if (arr.some(e => int.cmp(e, 0n) < 0)) return null;

    let leadingZeroes = 0, anyNonZero = false;
    for (let e of arr) {
        if (int.eq(e, 0n)) leadingZeroes += 1; 
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
    
    const base = int.add(arr.reduce((a, b) => int.cmp(a, b) < 0 ? b : a), 1n);
    const all = arr.reduce((a, b) => int.add(int.mul(a, base), b));
    const part1 = cramSingle(all);
    let part2: string;
    if (int.eq(base, 2n)) part2 = ":B";
    else if (int.eq(base, 10n)) part2 = "E";
    else part2 = cramSingle(base) + "|E";

    let best = /\d$/.test(part1) && /^\d/.test(part2) 
        ? part1 + ' ' + part2
        : part1 + part2;

    // check for all single digits
    if (arr.every(e => int.cmp(e, 0n) >= 0 && int.cmp(e , 10n) < 0) && int.cmp(arr[0], 0n) > 0) {
        const all = arr.reduce((a, b) => int.add(int.mul(a, 10n), b));
        const digited = cramSingle(all) + "E";
        if (digited.length < best.length) best = digited;
    }

    return best;
}

function shortenRepeatedArray(arr: int.StaxInt[]): string | null {
    if (arr.length < 2) return null;
    if (arr.some(e => !int.eq(e, arr[0]))) return null;
    if (int.cmp(arr[0], 32n) >= 0 && int.cmp(arr[0], 127n) < 0) {
        return "'" + String.fromCharCode(floatify(arr[0])) + cramSingle(BigInt(arr.length)) + '*';
    }
    else return cramSingle(arr[0]) + ']' + cramSingle(BigInt(arr.length)) + '*';
}

 function shortenPair(arr: int.StaxInt[]): string | null {
    if (arr.length !== 2) return null;
    let a = cramSingle(arr[0]), b = cramSingle(arr[1]);
    if (a === b) b = 'c';
    return /\d$/.test(a) && /^\d/.test(b)
        ? a + ' ' + b + '\\'
        : a + b + '\\';
}

function asciiShorten(arr: int.StaxInt[]): string | null {
    if (arr.some(e => int.cmp(e, 32n) < 0 || int.cmp(e, 127n) >= 0)) return null;
    const codes = arr.map(floatify);
    const content = String.fromCharCode(...codes);
    switch (content.length) {
        case 0: return null;
        case 1: return "'" + content;
        case 2: return "." + content;
        default: return '"' + content.replace(/"/g, '\\"') + '"';
    }
}