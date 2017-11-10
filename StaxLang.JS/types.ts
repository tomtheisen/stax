import * as bigInt from 'big-integer';
import * as _ from 'lodash';
import { Rational } from './rational';
import { Block } from './block';
type BigInteger = bigInt.BigInteger;

export type StaxNumber = number | BigInteger | Rational;
export type StaxValue = StaxNumber | Block | StaxArray;
export interface StaxArray extends Array<StaxValue> { }


export function S2A(s: string): StaxArray {
    let result: StaxArray = [];
    for (let i = 0; i < s.length; i++) result.push(bigInt(s.charCodeAt(i)));
    return result;
}

export function A2S(a: StaxArray): string {
    let result = "";
    for (let e of a) {
        if (isInt(e)) result += String.fromCodePoint(e.valueOf());
        else if (isArray(e)) result += A2S(e);
        else throw `can't convert ${e} to string`;
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

export function widenNumbers(...nums: StaxNumber[]): StaxNumber[] {
    if (_.some(nums, isFloat)) {
        return _.map(nums, floatify);
    }
    if (_.some(nums, n => n instanceof Rational)) {
        return _.map(nums, n => n instanceof Rational ? n : new Rational(n as BigInteger, bigInt.one));
    }
    return nums;
}

export function areEqual(a: StaxValue, b: StaxValue) {
    if (isArray(a) && isArray(b)) return _.isEqual(a, b);
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

const versionInfo = "Stax 0.0.0 (typescript) - Tom Theisen - https://github.com/ttheisen/stax "

export const constants: {[key: string]: StaxValue} = {
    '?': S2A(versionInfo),
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