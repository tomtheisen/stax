import { StaxArray, StaxNumber, StaxValue, last } from './types';
import * as int from './integer';
import { StaxInt } from './integer';

const Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}";
const _46 = int.make(46);

export function uncram(str: string): StaxInt[] {
    let result: StaxInt[] = [];
    let continuing = false, sign = 0;

    for (let i = 0; i < str.length; i++) {
        let charValue = Symbols.indexOf(str[i]);
        if (charValue < 0) throw new Error("Bad character for uncram");
        if (continuing) {
            let toAdd = (charValue >> 1) * sign;
            result[result.length - 1] = int.add(int.mul(last(result)!, _46), int.make(toAdd));
        }
        else {
            sign = charValue % 4 >= 2 ? -1 : 1;
            result.push(int.make((charValue >> 2) * sign));
        }
        continuing = charValue % 2 === 1;
    }

    if (continuing) { // offset mode
        for (let i = 1; i < result.length; i++) result[i] = int.add(result[i], result[i - 1]);
    }
    return result;
}

function encode(a: StaxInt[], offsetMode: boolean) {
    let result = "";
    if (offsetMode) {
        for (let i = a.length - 1; i > 0; i--) a[i] = int.sub(a[i], a[i - 1]);
    }
    for (let i = 0; i < a.length; i++) {
        let parts: number[] = [], signBit = int.floatify(a[i]) < 0 ? 2 : 0;
        let continuing = (offsetMode && i === a.length - 1) ? 1 : 0, remain = int.abs(a[i]);
        for (; int.floatify(remain) > 22; remain = int.div(remain, _46), continuing = 1) {
            parts.unshift(int.floatify(int.mod(remain, _46)) * 2 + continuing);
        }
        parts.unshift(int.floatify(remain) * 4 + signBit + continuing);
        result += parts.map(p => Symbols[p]).join("");
    }
    return result;
}

export function cram(arr: StaxInt[]): string {
    let flat = encode(arr, false), offset = encode(arr, true);
    return offset.length < flat.length ? offset : flat;
}