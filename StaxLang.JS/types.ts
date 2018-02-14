import * as bigInt from 'big-integer';
import { Rational } from './rational';
import { Block } from './block';
type BigInteger = bigInt.BigInteger;

export type StaxNumber = number | BigInteger | Rational;
export type StaxValue = StaxNumber | Block | StaxArray;
export interface StaxArray extends Array<StaxValue> { }

export function S2A(s: string): StaxArray {
    let result: StaxArray = [];
    for (let i = 0; i < s.length; i++) {
        let code = s.codePointAt(i)!;
        result.push(bigInt(code));
        if (code > 0x10000) i++;
    }
    return result;
}

export function A2S(a: StaxArray): string {
    let result = "";
    for (let e of a) {
        if (isInt(e)) result += e.isZero() ? ' ' : String.fromCodePoint(e.valueOf());
        else if (isArray(e)) result += A2S(e);
        else throw new Error(`can't convert ${e} to string`);
    }
    return result;
}

export function floatify(num: StaxNumber): number {
    if (isInt(num)) return num.valueOf();
    if (num instanceof Rational) return num.valueOf();
    return num;
}

export function isTruthy(a: StaxValue): boolean {
    if (isNumber(a)) return floatify(a) !== 0;
    return isArray(a) && a.length > 0;
}

export function isFloat(n: any): n is number {
    return typeof n === "number";
}
export function isInt(n: any): n is BigInteger {
    return bigInt.isInstance(n);
}
export function isArray(n: StaxValue | string): n is StaxArray {
    return Array.isArray(n);
}
export function isNumber(n: StaxValue): n is StaxNumber {
    return isInt(n) || isFloat(n) || n instanceof Rational;
}

export function last<T>(arr: T[]): T | undefined {
    return arr[arr.length - 1];
}

export function widenNumbers(...nums: StaxNumber[]): StaxNumber[] {
    if (nums.some(isFloat)) {
        return nums.map(floatify);
    }
    if (nums.some(n => n instanceof Rational)) {
        return nums.map(n => n instanceof Rational ? n : new Rational(n as BigInteger, bigInt.one));
    }
    return nums;
}

export function runLength(arr: StaxArray): StaxArray {
    if (arr.length === 0) return arr;
    let result: StaxArray = [], last: StaxValue | null = null, run = 0;
    for (let e of arr) {
        if (last != null && areEqual(e, last)) {
            run += 1;
        }
        else {
            if (run > 0) result.push([last!, bigInt(run)]);
            [last, run] = [e, 1];
        }
    }
    result.push([last!, bigInt(run)]);
    return result;
}

export function areEqual(a: StaxValue, b: StaxValue) {
    if (isArray(a) && isArray(b)) {
        if (a.length != b.length) return false;
        for (let i = 0; i < a.length; i++) {
            if (!areEqual(a[i], b[i])) return false;
        }
        return true;
    }
    if (isArray(a)) a = a[0];
    if (isArray(b)) b = b[0];
    if (isNumber(a) && isNumber(b)) {
        [a, b] = widenNumbers(a, b);
        if (typeof a === "number") return a === b;
        if (isInt(a)) return a.equals(b as BigInteger);
        if (a instanceof Rational) return a.equals(b as Rational);
    }
    return false;
}

export function indexOf(arr: StaxArray, val: StaxValue): number {
    for (let i = 0; i  < arr.length; i++) {
        if (areEqual(arr[i], val)) return i;
    }
    return -1;
}

export function compare(a: StaxValue, b: StaxValue): number {
    if (isNumber(a)) {
        if (isNumber(b)) return floatify(a) - floatify(b);
        if (isArray(b)) {
            if (b.length === 0) return 1;
            return compare(a, b[0]);
        }
    }
    else if (isNumber(b)) {
        if (isArray(a)) {
            if (a.length === 0) return -1;
            return compare(a[0], b);
        }
    }
    else if (isArray(a) && isArray(b)) {
        for (let i = 0; i < a.length && i < b.length; i++) {
            let ec = compare(a[i], b[i]);
            if (ec) return ec;
        }
        return compare(a.length, b.length);
    }
    return a.toString() > b.toString() ? 1 : -1;
}

export function stringFormat(arg: StaxValue): StaxArray {
    if (isNumber(arg)) return S2A(arg.toString());
    if (isArray(arg)) {
        let result = "";
        for (let e of arg) {
            if (isArray(e)) result += A2S(e);
            else result += e.toString();
        }
        return S2A(result);
    }
    throw new Error("bad type for stringFormat");
}

const versionInfo = "Stax 1.0.2 - Tom Theisen - https://github.com/ttheisen/stax"

export const constants: {[key: string]: StaxValue} = {
    '?': S2A(versionInfo),
	'!': S2A("[a-z]"),
	'@': S2A("[A-Z]"),
	'#': S2A("[a-zA-Z]"),
	'$': S2A("[a-z]+"),
	'%': S2A("[A-Z]+"),
	'^': S2A("[a-zA-Z]+"),
	'&': S2A("[a-z]*"),
	'*': S2A("[A-Z]*"),
	'(': S2A("[a-zA-Z]*"),
	':': S2A("http://"),
    ';': S2A("https://"),
    '0': new Rational(bigInt.zero, bigInt.one),
    '2': 0.5,
    '3': Math.pow(2, 1.0 / 12),
    '/': Math.PI / 3,
    'a': S2A("abcdefghijklmnopqrstuvwxyz"),
    'A': S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'b': S2A("()[]{}<>"),
    'B': bigInt[256],
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
    'k': bigInt(1000),
    'l': S2A("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'L': S2A("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'm': bigInt(0x7fffffff),
    'M': bigInt(1000000),
    'n': S2A("\n"),
    'p': S2A(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"),
    'P': Math.PI,
    'q': Math.PI / 2,
    's': S2A(" \t\r\n\v"),
    'S': Math.PI * 4 / 3,
    't': Math.PI * 2,
    'T': 10.0,
    'v': S2A("aeiou"),
    'V': S2A("AEIOU"),
    'w': S2A("0123456789abcdefghijklmnopqrstuvwxyz"),
    'W': S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
};