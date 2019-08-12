import { StaxValue, StaxMap, StaxArray } from './types';

export default class Multiset {
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

    keys() {
        return [...this.entries.keys()];
    }
}