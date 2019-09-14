import { last, floatify } from './types';
import * as int from './integer';
import { StaxInt } from './integer';

const Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";

const _10 = int.make(10);
const _46 = int.make(46);
const _93 = int.make(93);

export function uncram(str: string): StaxInt[] {
    let result: StaxInt[] = [];
    let continuing = false, sign = 0;

    for (let i = 0; i < str.length; i++) {
        let charValue = Symbols.indexOf(str[i]);
        if (charValue < 0) throw new Error("Bad character for uncram");
        if (continuing) {
            let toAdd = (charValue >> 1) * sign;
            result[result.length - 1] = int.add(int.mul(last(result)!, _46), int.make(toAdd));
        }
        else {
            sign = charValue % 4 >= 2 ? -1 : 1;
            result.push(int.make((charValue >> 2) * sign));
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
        for (; int.floatify(remain) > 22; remain = int.div(remain, _46), continuing = 1) {
            parts.unshift(int.floatify(int.mod(remain, _46)) * 2 + continuing);
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
    if (int.cmp(n, int.zero) < 0) return cramSingle(int.negate(n)) + "N";
    if (int.eq(n, int.make(10))) return "A";
    if (int.eq(n, int.make(256))) return "VB";
    if (int.eq(n, int.make(1000))) return "Vk";

    const sqrt = int.floorSqrt(n), isSquare = int.eq(n, int.mul(sqrt, sqrt));
    if (int.cmp(n, int.make(100)) >= 0 && isSquare) return cramSingle(sqrt) + "J";

    let best = n.toString();
    if (int.cmp(n, int.make(1e7)) < 0) return best;

    for (var scalarCrammed = ""; int.cmp(n, int.zero) > 0; n = int.div(n, _93)) {
        n = int.sub(n, int.one);
        scalarCrammed = Symbols[floatify(int.mod(n, _93))] + scalarCrammed;
    }
    scalarCrammed = `"${scalarCrammed}"%`;
    if (scalarCrammed.length < best.length) best = scalarCrammed;
    return best;
}

export function uncramSingle(s: string): StaxInt {
    let result = int.zero;
    for (let c of s) {
        result = int.add(int.mul(result, _93), int.make(Symbols.indexOf(c) + 1));
    }
    return result;
}

export function baseArrayCrammed(arr: StaxInt[]): string | null {
    if (arr.some(e => int.cmp(e, int.zero) < 0) || int.eq(arr[0], int.zero)) return null;
    const base = int.add(arr.reduce((a, b) => int.cmp(a, b) < 0 ? b : a), int.one);
    const all = arr.reduce((a, b) => int.add(int.mul(a, base), b));
    let best = cramSingle(all) + cramSingle(base) + "|E";

    if (arr.every(e => int.cmp(e, int.zero) >= 0 && int.cmp(e , _10) < 0) && int.cmp(arr[0], int.zero) > 0) {
        const all = arr.reduce((a, b) => int.add(int.mul(a, _10), b));
        const digited = cramSingle(all) + "E";
        if (digited.length < best.length) best = digited;
    }

    return best;
}
