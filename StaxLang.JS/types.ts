import { Block } from './block';
import { StaxInt } from './integer';
import * as int from './integer'
import { Rational, one as rationalOne } from './rational';
import * as rat from './rational';
import { IntRange } from './collections';
import { codePage } from './packer';

export type StaxNumber = number | Rational | StaxInt;
export type StaxValue = StaxNumber | Block | StaxArray;
export interface MaterializedStaxArray extends ReadonlyArray<StaxValue> { }
export type StaxArray = MaterializedStaxArray | IntRange;

export function S2A(s: string): StaxInt[] {
    let result: StaxInt[] = [];
    for (let c of s) result.push(BigInt(c.codePointAt(0)!));
    return result;
}

export function A2S(a: StaxArray): string {
    let result = "";
    for (let e of a) {
        if (typeof e === 'bigint') result += e === 0n ? ' ' : String.fromCodePoint(int.floatify(e));
        else if (isArray(e)) result += A2S(e);
        else throw new Error(`can't convert ${e} to string`);
    }
    return result;
}

export function floatify(num: StaxNumber): number {
    return Number(num.valueOf());
}

export function isTruthy(a: StaxValue): boolean {
    if (isNumber(a)) return floatify(a) !== 0;
    return isArray(a) && a.length > 0;
}

export function isFloat(n: any): n is number {
    return typeof n === "number";
}
export function isArray(n: StaxValue | string): n is StaxArray {
    return Array.isArray(n) || n instanceof IntRange;
}
export function isMatrix(n: StaxArray): n is StaxArray[] {
    return n.length > 0 && n.every(isArray);
}
export function isNumber(n: StaxValue): n is StaxNumber {
    return typeof n === 'bigint' || isFloat(n) || n instanceof Rational;
}

export function materialize(arr: StaxArray): MaterializedStaxArray {
    return Array.isArray(arr) ? arr : [...arr];
}

export function last<T>(arr: ReadonlyArray<T>): T | undefined;
export function last(arr: IntRange): StaxInt | number | undefined;
export function last(arr: StaxArray): StaxValue | undefined;
export function last<T>(arr: ReadonlyArray<T> | IntRange): T | StaxInt | number | undefined {
    if (arr instanceof IntRange) {
        if (arr.end == null) return Number.POSITIVE_INFINITY;
        return (arr.length > 0) ? int.sub(arr.end, 1n) : undefined;
    }
    return arr[arr.length - 1];
}

export function widenNumbers(...nums: StaxNumber[]): StaxNumber[] {
    if (nums.some(isFloat)) return nums.map(floatify);
    if (nums.some(n => n instanceof Rational)) {
        return nums.map(n => n instanceof Rational ? n : new Rational(n as StaxInt, 1n));
    }
    return nums;
}

export function pow(a: StaxNumber, b: StaxNumber): StaxNumber {
    if (typeof b === 'bigint') {
        if (typeof a === 'bigint') {
            if (b.valueOf() < 0) return new Rational(1n, int.pow(a, -b));
            else return int.pow(a, b);
        }
        else if (a instanceof Rational) {
            if (b.valueOf() < 0) {
                b = -b;
                a = a.invert();
            }
            let result = rationalOne;
            for (let i = 0; i < b.valueOf(); i++) result = result.multiply(a);
            return result;
        }
    }
    return Math.pow(floatify(a), floatify(b));
}

export function runLength(arr: StaxArray): StaxArray {
    if (arr.length === 0) return arr;
    let result: [StaxValue, StaxInt][] = [], last: StaxValue | null = null, run = 0n;
    for (let e of arr) {
        if (last != null && areEqual(e, last)) ++run;
        else {
            if (run > 0n) result.push([last!, run]);
            [last, run] = [e, 1n];
        }
    }
    result.push([last!, run]);
    return result;
}

export function areEqual(a: StaxValue, b: StaxValue): boolean {
    if (isArray(a) && isArray(b)) {
        if (a.length != b.length) return false;
        b = materialize(b);
        let i = 0;
        for (let e of a) {
            if (!areEqual(e, b[i++])) return false;
        }
        return true;
    }
    if (isArray(a)) [a] = a;
    if (isArray(b)) [b] = b;
    if (isNumber(a) && isNumber(b)) {
        [a, b] = widenNumbers(a, b);
        if (typeof a === 'bigint') return a === (b as StaxInt);
        if (typeof a === "number") return a === b;
        if (a instanceof Rational) return a.equals(b as Rational);
    }
    return false;
}

export function indexOf(arr: StaxArray, val: StaxValue): number {
    if (arr instanceof IntRange) {
        if (typeof val !== 'bigint') return -1;
        if (int.cmp(val, arr.start) >= 0 && (arr.end == null || int.cmp(val, arr.end) < 0)) {
            return floatify(val) - floatify(arr.start);
        }
        else return -1;
    }
    return arr.findIndex(e => areEqual(e, val));
}

export function compare(a: StaxValue, b: StaxValue): number {
    if (isNumber(a)) {
        if (isNumber(b)) {
            if (typeof a === "number" || typeof b === "number") return floatify(a) - floatify(b);
            if (a instanceof Rational || b instanceof Rational) {
                if (typeof a === 'bigint') a = new Rational(a, 1n);
                if (typeof b === 'bigint') b = new Rational(b, 1n);
                return (a instanceof Rational ? a : new Rational(a, 1n))
                    .subtract(b instanceof Rational ? b : new Rational(b, 1n)).valueOf();
            }
            return int.cmp(a, b);
        }
        if (isArray(b)) {
            if (b.length === 0) return 1;
            let [bs] = b;
            return compare(a, bs);
        }
    }
    else if (isNumber(b)) {
        if (isArray(a)) {
            if (a.length === 0) return -1;
            let [as] = a;
            return compare(as, b);
        }
    }
    else if (isArray(a) && isArray(b)) {
        a = materialize(a); b = materialize(b);
        for (let i = 0; i < a.length && i < b.length; i++) {
            let ec = compare(a[i], b[i]);
            if (ec) return ec;
        }
        return compare(a.length, b.length);
    }
    return a.toString() > b.toString() ? 1 : -1;
}

export function stringFormatFloat(arg: number) {
    let result = arg.toPrecision(15).replace("Infinity", "∞");
    if (result.indexOf('.') >= 0) result = result
        .replace(/\.0+(e|E|$)/, '$1')
        .replace(/\.(\d+?)0+($|e)/, '.$1$2');
    return result.replace('e', 'E');
}

export function stringFormat(arg: StaxValue): StaxArray {
    function flatten(arr: StaxArray): StaxNumber[] {
        let result: StaxNumber[] = [];
        for (let e of arr) {
            if (isNumber(e)) result.push(e);
            else if (isArray(e)) result = result.concat(flatten(e));
        }
        return result;
    }

    if (isFloat(arg)) return S2A(stringFormatFloat(arg));
    if (isNumber(arg)) return S2A(arg.toString());
    if (isArray(arg)) {
        let result: StaxArray = [];
        for (let e of arg) {
            if (isArray(e)) result = result.concat(flatten(e));
            else result = result.concat(S2A(e.toString()));
        }
        return result;
    }
    throw new Error("bad type for stringFormat");
}

export function unEval(arr: StaxArray): string {
    let mapped = arr.map(e => isArray(e) ? unEval(e) : e.toString());
    return "[" + mapped.join(", ") + "]";
}

export function literalFor(arg: StaxInt): string {
    if (int.cmp(arg, 0n) < 0) return literalFor(-arg) + 'N';
    if (arg === 10n) return 'A';
    return arg.toString();
}

const versionInfo = "Stax 1.1.11 - Tom Theisen - https://github.com/tomtheisen/stax"

export const constants: {[key: string]: StaxValue} = {
    '?': S2A(versionInfo),
    '%': [0n, 0n],
	'!': S2A("[a-z]"),
	'@': S2A("[A-Z]"),
	'#': S2A("[a-zA-Z]"),
	'$': S2A("[a-z]+"),
	')': S2A("[A-Z]+"),
	'^': S2A("[a-zA-Z]+"),
	'&': S2A("[a-z]*"),
	'*': S2A("[A-Z]*"),
	'(': S2A("[a-zA-Z]*"),
	':': S2A("http://"),
    ';': S2A("https://"),
    '0': rat.zero,
    '2': 0.5,
    '3': Math.pow(2, 1.0 / 12),
    '6': S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"),
    '/': Math.PI / 3,
    'a': S2A("abcdefghijklmnopqrstuvwxyz"),
    'A': S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'b': S2A("()[]{}<>"),
    'B': 256n,
    'c': S2A("bcdfghjklmnpqrstvwxyz"),
    'C': S2A("BCDFGHJKLMNPQRSTVWXYZ"),
    'd': S2A("0123456789"),
    'D': Math.sqrt(2),
    'e': Math.E,
    'E': Math.sqrt(3),
    'h': S2A("0123456789abcdef"),
    'H': S2A("0123456789ABCDEF"),
    'i': Number.NEGATIVE_INFINITY,
    'I': Number.POSITIVE_INFINITY,
    'k': 1000n,
    'l': S2A("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'L': S2A("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'm': 0x7fffffffn,
    'M': 1_000_000n,
    'n': S2A("\n"),
    'N': Number.NaN,
    'p': S2A(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"),
    'P': Math.PI,
    'q': Math.PI / 2,
    's': S2A(" \t\r\n\v"),
    'S': Math.PI * 4 / 3,
    't': Math.PI * 2,
    'T': 10.0,
    'u': 4294967296n,
    'v': S2A("aeiou"),
    'V': S2A("AEIOU"),
    'x': S2A(codePage),
    'w': S2A("0123456789abcdefghijklmnopqrstuvwxyz"),
    'W': S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'y': S2A("aeiouy"),
    'Y': S2A("AEIOUY"),
    'z': S2A("bcdfghjklmnpqrstvwxz"),
    'Z': S2A("BCDFGHJKLMNPQRSTVWXZ"),
};