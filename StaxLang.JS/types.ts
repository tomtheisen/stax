import { Block } from './block';
import { StaxInt, isInt } from './integer';
import * as int from './integer'
import { Rational, one as rationalOne } from './rational';
import * as rat from './rational';

export type StaxNumber = number | Rational | StaxInt;
export type StaxValue = StaxNumber | Block | StaxArray;
export interface StaxArray extends Array<StaxValue> { }

const hashMemo = new WeakMap<Exclude<Exclude<StaxValue, number>, BigInt>, number>();
const buf = new ArrayBuffer(8), intView = new Int32Array(buf), floatView = new Float64Array(buf);
const HASHMAX_BIG = int.make(0x8000_0000);
/** returns a value hash in the signed 32-bit range */
function getHashCode(val: StaxValue): number {
    if (typeof val === "bigint") {
        return floatify(int.mod(val, HASHMAX_BIG));
    }
    if (isFloat(val)) {
        if (val === 0) return 0;  // normalize -0
        if (isNaN(val)) val = Number.NaN; // normalize exotic nans
        if (val % 1 === 0) return Math.abs(val) % 0x8000_0000; // can be equal to ints
        floatView[0] = val;
        let hash = intView[0] ^ intView[1];
        return hash;
    }
    if (hashMemo.has(val)) return hashMemo.get(val)!;
    if (isInt(val)) {
        let hash = floatify(int.mod(val, HASHMAX_BIG));
        hashMemo.set(val, hash);
        return hash;
    }
    if (val instanceof Rational) {
        if (int.eq(val.denominator, int.one)) return getHashCode(val.numerator); // can be equal to ints
        let hash = getHashCode(val.numerator) ^ getHashCode(val.denominator);
        hashMemo.set(val, hash);
        return hash;
    }
    if (isArray(val)) {
        let hash = 0xA22A1;
        for (let el of val) {
            hash *= 37;
            hash ^= getHashCode(el);
        }
        hashMemo.set(val, hash);
        return hash;
    }
    if (val instanceof Block) {
        let hash = getHashCode(S2A(val.contents));
        hashMemo.set(val, hash);
        return hash;
    }
    else throw new Error("Can't compute hash for " + val);
}

export class StaxSet {
    private contents = new Map<number, StaxValue[]>();

    constructor(items?: StaxArray) {
        if (items) this.add(...items);
    }

    has(val: StaxValue): boolean {
        const hash = getHashCode(val);
        if (!this.contents.has(hash)) return false;
        return this.contents.get(hash)!.some(el => areEqual(el, val));
    }
    
    add(...vals: StaxValue[]): StaxSet {
        for (let val of vals) {
            const hash = getHashCode(val);
            const arr = this.contents.get(hash);
            if (arr != undefined) {
                if (!arr.some(el => areEqual(el, val))) arr.push(val);
            }
            else this.contents.set(hash, [val]);
        }
        return this;
    }
    
    remove(...vals: StaxValue[]): StaxSet {
        for (let val of vals) {
            const hash = getHashCode(val);
            if (!this.contents.has(hash)) continue;
            const arr = this.contents.get(hash)!;
            let idx = arr.findIndex(el => areEqual(el, val));
            if (idx >= 0) arr.splice(idx, 1);
        }
        return this;
    }

    *entries(): IterableIterator<StaxValue> {
        for (let rec of this.contents.values()) {
            for (let el of rec) yield el;
        }
    }
}

export class StaxMap<TValue = StaxValue> {
    private contents = new Map<number, {key: StaxValue, val: TValue}[]>();

    has(key: StaxValue): boolean {
        const hash = getHashCode(key);
        if (!this.contents.has(hash)) return false;
        return this.contents.get(hash)!.some(el => areEqual(el.key, key));
    }

    set(key: StaxValue, val: TValue): StaxMap<TValue> {
        const hash = getHashCode(key);
        if (!this.contents.has(hash)) {
            this.contents.set(hash, [{ key, val }]);
        }
        else {
            const arr = this.contents.get(hash)!;
            const found = arr.find(el => areEqual(el.key, key));
            if (found) found.val = val;
            else arr.push({key, val});
        }
        return this;
    }

    get(key: StaxValue): TValue | undefined {
        const hash = getHashCode(key);
        const arr = this.contents.get(hash);
        if (!arr) return undefined;
        const found = arr.find(el => areEqual(el.key, key));
        if (!found) return undefined;
        return found.val;
    }

    remove(key: StaxValue): StaxMap<TValue> {
        const hash = getHashCode(key);
        const arr = this.contents.get(hash);
        if (!arr) return this;
        const idx = arr.findIndex(el => areEqual(el.key, key));
        if (idx < 0) return this;
        arr.splice(idx, 1);
        return this;
    }

    *keys(): IterableIterator<StaxValue> {
        for (let rec of this.contents.values()) {
            for (let el of rec) yield el.key;
        }
    }
}

export function S2A(s: string): StaxInt[] {
    let result: StaxInt[] = [];
    for (let i = 0; i < s.length; i++) {
        let code = s.codePointAt(i)!;
        result.push(int.make(code));
        if (code > 0x10000) i++;
    }
    return result;
}

export function A2S(a: StaxArray): string {
    let result = "";
    for (let e of a) {
        if (isInt(e)) result += e.valueOf() == 0 ? ' ' : String.fromCodePoint(int.floatify(e));
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
    return Array.isArray(n);
}
export function isMatrix(n: StaxArray): n is StaxArray[] {
    return n.length > 0 && n.every(isArray);
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
        return nums.map(n => n instanceof Rational ? n : new Rational(n as StaxInt, int.one));
    }
    return nums;
}

export function pow(a: StaxNumber, b: StaxNumber): StaxNumber {
    if (isInt(b)) {
        if (isInt(a)) {
            if (b.valueOf() < 0) return new Rational(int.one, int.pow(a, int.negate(b)));
            else return int.pow(a, b);
        }
        else if (a instanceof Rational) {
            if (b.valueOf() < 0) {
                b = int.negate(b);
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
    let result: StaxArray = [], last: StaxValue | null = null, run = 0;
    for (let e of arr) {
        if (last != null && areEqual(e, last)) {
            run += 1;
        }
        else {
            if (run > 0) result.push([last!, int.make(run)]);
            [last, run] = [e, 1];
        }
    }
    result.push([last!, int.make(run)]);
    return result;
}

export function areEqual(a: StaxValue, b: StaxValue): boolean {
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
        if (isInt(a)) return int.eq(a, b as StaxInt);
        if (typeof a === "number") return a === b;
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
        if (isNumber(b)) {
            if (typeof a === "number" || typeof b === "number") return floatify(a) - floatify(b);
            if (a instanceof Rational || b instanceof Rational) {
                if (isInt(a)) a = new Rational(a, int.one);
                if (isInt(b)) b = new Rational(b, int.one);
                return (a instanceof Rational ? a : new Rational(a, int.one))
                    .subtract(b instanceof Rational ? b : new Rational(b, int.one)).valueOf();
            }
            return int.cmp(a, b);
        }
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

export function stringFormatFloat(arg: number) {
    let result = arg.toPrecision(15).replace("Infinity", "∞");
    if (result.indexOf('.') >= 0) result = result.replace(/\.?0+$/, '');
    return result;
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

const versionInfo = "Stax 1.1.7 - Tom Theisen - https://github.com/tomtheisen/stax"

export const constants: {[key: string]: StaxValue} = {
    '?': S2A(versionInfo),
    '%': [int.zero, int.zero],
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
    '/': Math.PI / 3,
    'a': S2A("abcdefghijklmnopqrstuvwxyz"),
    'A': S2A("ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'b': S2A("()[]{}<>"),
    'B': int.make(256),
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
    'k': int.make(1000),
    'l': S2A("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'L': S2A("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    'm': int.make(0x7fffffff),
    'M': int.make(1000000),
    'n': S2A("\n"),
    'N': Number.NaN,
    'p': S2A(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"),
    'P': Math.PI,
    'q': Math.PI / 2,
    's': S2A(" \t\r\n\v"),
    'S': Math.PI * 4 / 3,
    't': Math.PI * 2,
    'T': 10.0,
    'u': int.make(4294967296),
    'v': S2A("aeiou"),
    'V': S2A("AEIOU"),
    'w': S2A("0123456789abcdefghijklmnopqrstuvwxyz"),
    'W': S2A("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
};