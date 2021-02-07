import { StaxValue, StaxArray, floatify, isFloat, isArray, S2A, areEqual } from './types';
import { Rational } from './rational';
import { Block } from './block';

export class Multiset {
    private entries = new StaxMap<number>();

    constructor(values?: StaxArray) {
        if (values) values.forEach(this.add.bind(this));
    }

    add(val: StaxValue) {
        const current = this.entries.get(val);
        this.entries.set(val, current == undefined ? 1 : current + 1);
    }

    remove(val: StaxValue) {
        const current = this.entries.get(val);
        if (!current) throw new Error("can't remove element from multiset that doesn't exist");
        if (current <= 1) this.entries.remove(val);
        else this.entries.set(val, current - 1);
    }

    addAll(...vals: StaxValue[]) {
        vals.forEach(this.add.bind(this));
    }

    get(val: StaxValue) {
        return this.entries.get(val) || 0;
    }

    contains(val: StaxValue) {
        return this.get(val) > 0;
    }

    eq(other: Multiset) {
        const otherkeys = other.keys();
        if (otherkeys.length !== this.keys().length) return false;
        for (let key of otherkeys) if (this.get(key) !== other.get(key)) return false;
        return true;
    }

    keys() {
        return [...this.entries.keys()];
    }
}

const hashMemo = new WeakMap<Exclude<Exclude<StaxValue, number>, BigInt>, number>();
const buf = new ArrayBuffer(8), intView = new Int32Array(buf), floatView = new Float64Array(buf);
const HASHMAX_BIG = 0x8000_0000n;
/** returns a value hash in the signed 32-bit range */
function getHashCode(val: StaxValue): number {
    if (typeof val === "bigint") {
        return floatify(val % HASHMAX_BIG);
    }
    if (isFloat(val)) {
        if (val === 0) return 0;  // normalize -0
        if (isNaN(val)) val = Number.NaN; // normalize exotic nans
        if (val % 1 === 0) return Math.abs(val) % 0x8000_0000; // can be equal to ints
        floatView[0] = val;
        return intView[0] ^ intView[1];
    }
    if (hashMemo.has(val)) return hashMemo.get(val)!;
    if (typeof val === 'bigint') {
        let hash = floatify(val % HASHMAX_BIG);
        hashMemo.set(val, hash);
        return hash;
    }
    if (val instanceof Rational) {
        if (val.denominator === 1n) return getHashCode(val.numerator); // can be equal to ints
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
    throw new Error("Can't compute hash for " + val);
}

export class StaxSet {
    private contents = new Map<number, StaxValue[]>();

    constructor(items?: StaxArray) {
        if (items) items.forEach(e => this.add(e));
    }

    has(val: StaxValue): boolean {
        const hash = getHashCode(val);
        if (!this.contents.has(hash)) return false;
        return this.contents.get(hash)!.some(el => areEqual(el, val));
    }
    
    add(...vals: StaxValue[]): StaxSet {
        for (let val of vals) {
            Object.freeze(val);
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

    eq(other: StaxSet) {
        if (other.size !== this.size) return false;
        for (let val of this.entries()) if (!other.has(val)) return false;
        return true;
    }

    *entries(): IterableIterator<StaxValue> {
        for (let rec of this.contents.values()) {
            for (let el of rec) yield el;
        }
    }

    get size() {
        let result = 0;
        for (let rec of this.contents.values()) result += rec.length;
        return result;
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
        Object.freeze(val);
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

export class IntRange {
    readonly start: bigint;
    readonly end?: bigint;

    constructor(start: bigint, end?: bigint) {
        if (end != null && start > end) throw new Error("Attempted to create range with [start] > [end]");
        this.start = start;
        this.end = end;
    }

    get length() {
        return typeof this.end === 'bigint' ? floatify(this.end) - floatify(this.start) : Number.POSITIVE_INFINITY;
    }

    includes(val: StaxValue) {
        return typeof val === 'bigint' && val >= this.start && (this.end == null || val < this.end);
    }

    *[Symbol.iterator]() {
        for (let i = this.start; this.end == null || i < this.end; ++i) {
            yield i;
        }
    }

    every(predicate: (el: StaxValue, i: number) => boolean): boolean {
        let i = 0;
        for(let e of this) if (!predicate(e, i++)) return false;
        return true;
    }

    some(predicate: (el: StaxValue, i: number) => boolean): boolean {
        let i = 0;
        for(let e of this) if (predicate(e, i++)) return true;
        return false;
    }

    map<T>(callbackfn: (value: StaxValue, index: number, array: readonly StaxValue[]) => T, thisArg?: any): T[] {
        callbackfn = callbackfn.bind(thisArg);
        let result = [], i = 0;
        for(let e of this) result.push(callbackfn(e, i++, this as any /* dragons here */));
        return result;
    }

    filter(callbackfn: (value: StaxValue, index: number, array: readonly StaxValue[]) => boolean, thisArg?: any): StaxArray {
        let i = 0, allPass = true, filtered: StaxValue[] = [];
        for (let e of this) {
            const pass = callbackfn(e, i, this as any /* dragons here */);
            if (allPass && !pass) {
                filtered = [...this.slice(0, i)];
                allPass = false;
            }
            else if (!allPass && pass) filtered.push(e);
            i++;
        }
        return allPass ? this : filtered;
    }

    forEach(callbackfn: (value: StaxValue, index: number, array: readonly StaxValue[]) => void, thisArg?: any): void {
        callbackfn = callbackfn.bind(thisArg);
        let i = 0;
        for(let e of this) callbackfn(e, i++, this as any /* dragons here */);
    }

    slice(start = 0, end?: number): StaxArray {
        start = Math.max(start, 0);
        if (start === 0) {
            if (end == null || end >= this.length) return this;
            return new IntRange(this.start, this.start + BigInt(end));
        }
        if (start > this.length) return [];
        if (end == null || end >= this.length) {
            return new IntRange((this.start + BigInt(start)), this.end);
        }
        return new IntRange((this.start + BigInt(start)), (this.start + BigInt(end)));
    }

    reverse() {
        return [...this].reverse();
    }

    concat(...others: StaxValue[]): StaxValue[] {
        return [...this, ...others];
    }
}