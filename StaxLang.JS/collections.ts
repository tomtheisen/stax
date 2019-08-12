import { StaxValue, StaxArray, floatify, isFloat, isArray, S2A, areEqual } from './types';
import { isInt } from './integer';
import { Rational } from './rational';
import { Block } from './block';
import * as int from './integer'

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
