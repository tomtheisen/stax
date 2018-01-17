import { StaxValue, areEqual } from './types';

export default class Multiset {
    private entries: {key: StaxValue, count: number}[] = [];

    constructor(values: StaxValue[] | undefined = undefined) {
        if (values) values.forEach(this.add.bind(this));
    }

    add(val: StaxValue) {
        for (let entry of this.entries) {
            if (areEqual(val, entry.key)) {
                ++entry.count;
                return;
            }
        }
        this.entries.push({ key: val, count: 1 });
    }

    remove(val: StaxValue) {
        for (let entry of this.entries) {
            if (areEqual(val, entry.key) && entry.count > 0) {
                --entry.count;
                return;
            }
        }
        throw new Error("can't remove element from multiset that doesn't exist");
    }

    addAll(...vals: StaxValue[]) {
        vals.forEach(this.add.bind(this));
    }

    get(val: StaxValue) {
        for (let entry of this.entries) {
            if (areEqual(val, entry.key)) return entry.count;
        }
        return 0;
    }

    contains(val: StaxValue) {
        return this.get(val) > 0;
    }

    keys() {
        return this.entries.map(e => e.key);
    }
}