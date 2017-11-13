import { StaxValue } from './types';

export default class IteratorPair {
    public item1: StaxValue;
    public item2: StaxValue;

    constructor(i1: StaxValue, i2: StaxValue) {
        this.item1 = i1;
        this.item2 = i2;
    }
}