import { StaxArray, StaxNumber, StaxValue, last } from './types';
import * as bigInt from 'big-integer';
type BigInteger = bigInt.BigInteger;

const Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";

export function uncram(str: string): BigInteger[] {
    let result: BigInteger[] = [];
    let continuing = false;

    for (let i = 0; i < str.length; i++) {
        let charValue = Symbols.indexOf(str[i]);
        if (charValue < 0) throw new Error("Bad character for uncram");
        if (continuing) {
            let toAdd = (charValue >> 1) * (last(result)!.isNegative() ? -1 : 1);
            result[result.length - 1] = last(result)!.multiply(46).add(toAdd);
        }
        else result.push(bigInt((charValue >> 2) * (charValue % 4 >= 2 ? -1 : 1)));
        continuing = charValue % 2 === 1;
    }

    if (continuing) { // offset mode
        for (let i = 1; i < result.length; i++) result[i] = result[i].add(result[i - 1]);
    }
    return result;
}

function encode(a: BigInteger[], offsetMode: boolean) {
    let result = "";
    if (offsetMode) {
        for (let i = a.length - 1; i > 0; i--) a[i] = a[i].subtract(a[i - 1]);
    }
    for (let i = 0; i < a.length; i++) {
        let parts: number[] = [], signBit = a[i].isNegative() ? 2 : 0;
        let continuing = (offsetMode && i === a.length - 1) ? 1 : 0, remain = a[i].abs();
        for (; remain.gt(23); remain = remain.divide(46), continuing = 1) {
            parts.unshift(remain.valueOf() % 46 * 2 + continuing);
        }
        parts.unshift(remain.valueOf() * 4 + signBit + continuing);
        result += parts.map(p => Symbols[p]).join("");
    }
    return result;
}

export function cram(arr: BigInteger[]): string {
    let flat = encode(arr, false), offset = encode(arr, true);
    return offset.length < flat.length ? offset : flat;
}