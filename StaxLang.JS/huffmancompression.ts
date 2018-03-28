import * as bigInt from 'big-integer';
import englishData from './englishhuffman';
import { last } from './types';

type BigInteger = bigInt.BigInteger;
const one = bigInt.one, zero = bigInt.zero, minusOne = bigInt.minusOne;

class HuffmanNode {
    left: HuffmanNode;
    right: HuffmanNode;
    leafValue?: string;

    populate(path: string, leaf: string, pathIdx = 0) {
        if (pathIdx >= path.length) {
            this.leafValue = leaf;
            return;
        }

        this.left = this.left || new HuffmanNode;
        this.right = this.right || new HuffmanNode;
        (path[pathIdx] === '1' ? this.right : this.left).populate(path, leaf, pathIdx + 1);
    }

    traverse(path: string, idx: number): ({ result: string, newidx: number }) {
        if (this.leafValue) return { result: this.leafValue, newidx: idx };
        return ((++idx <= path.length && path[idx - 1] === '1') ? this.right : this.left).traverse(path, idx);
    }

    findPath(ch: string): string | null {
        if (this.leafValue) return (ch === this.leafValue) ? "" : null;

        let result = this.left.findPath(ch);
        if (typeof result === 'string') return '0' + result;
        result = this.right.findPath(ch);
        if (typeof result === 'string') return '1' + result;
        return null;
    }
}

const symbols = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";
let trees: {[key: string]: HuffmanNode} = {};

function setup() {
    for (let prefix in englishData) {
        trees[prefix] = new HuffmanNode;
        let treespec = englishData[prefix];

        let path = "";
        for (let i = 0; i < treespec.length; i += 2) {
            let ch = treespec[i];
            let zeroes = "0123456789abcdefghijklmnop".indexOf(treespec[i + 1]);

            if (i) {
                let idx = path.length - 1;
                while (path[idx] === '1') idx--;
                path = path.substr(0, idx) + '1';
            }
            path += '0'.repeat(zeroes);

            trees[prefix].populate(path, ch);
        }
    }
}
setup();

export function compress(input: string): string | null {
    input = '. ' + input;
    let path = '';

    for (let i = 2; i < input.length; i++) {
        let tree = trees[input.substr(i - 2, 2)], cpath = tree.findPath(input[i]);
        if (!cpath) return null;
        if (i === input.length - 1) {
            while (cpath.length >= 2 && cpath[cpath.length - 1] === '0') {
                cpath = cpath.substr(0, cpath.length - 1);
            }
        }
        path += cpath;
    }

    let big = one, result = "", symlen = bigInt(symbols.length);
    for (let i = 0; i < path.length; i++) {
        big = big.shiftLeft(1);
        big = big.add(path[i] === '1' ? one : zero);
    }
    while (big.isPositive()) {
        let { quotient, remainder } = big.divmod(symlen);
        result += symbols[remainder.valueOf()];
        big = quotient;
    }
    return result;
}

let memoizedDecompress: {[key: string]: string} = {};
export function decompress(compressed: string): string {
    if (compressed in memoizedDecompress) return memoizedDecompress[compressed];

    let big = zero;
    for (let ch of compressed.split("").reverse()) {
        big = big.multiply(symbols.length).add(symbols.indexOf(ch));
    }

    let path = '', result = '. ', pathIdx = 1;
    while (!big.isZero()) {
        path = (big.isOdd() ? '1' : '0') + path;
        big = big.divide(bigInt[2]);
    }

    while (pathIdx < path.length) {
        let tree = trees[result.substr(result.length - 2)];
        let { result: segment, newidx } = tree.traverse(path, pathIdx);
        result += segment;
        pathIdx = newidx;
    }
    return memoizedDecompress[compressed] = result.substr(2);
}
