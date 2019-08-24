import englishData from './englishhuffman';
import * as int from './integer';

const _2 = int.make(2);

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
        let treespec = englishData[prefix], path = "";
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

/** e.g. "Hello, World" -> "`;Kp0TDt`" */
export function compressLiteral(input: string): string | null {
    let compressed = compress(input);
    if (!compressed) return null;

    const lowerInput = input.toLowerCase(), upperInput = input.toUpperCase();
    const titleInput = lowerInput.replace(/\b\w/g, m => m[0].toUpperCase());
    let caseTransform : string | null;
    switch (input) {
        case lowerInput: caseTransform = "v"; break;
        case upperInput: caseTransform = "^"; break;
        case titleInput: caseTransform = ":."; break;
        default: caseTransform = null;
    }

    const untransformed = '`' + compressed + '`';
    if (!caseTransform) return untransformed;
    const caseTransformed = '`' + compress(input, true) + '`' + caseTransform;
    return (caseTransformed.length < untransformed.length) ? caseTransformed : untransformed;
}

export function compress(input: string, flexcase = false): string | null {
    let path = '', big = int.one, result = "", symlen = int.make(symbols.length);
    
    if (flexcase) {
        let picked = '. ';
        for (let c of input) {
            let tree = trees[picked]; 
            let cpath1 = tree.findPath(c.toLowerCase()), cpath2 = tree.findPath(c.toUpperCase());
            if (!cpath1 || !cpath2) return null;
            if (cpath2.length < cpath1.length) {
                picked = picked.substr(1) + c.toUpperCase();
                path += cpath2;
            }
            else {
                picked = picked.substr(1) + c.toLowerCase();
                path += cpath1;
            }
        }
    }
    else {
        input = '. ' + input;
        for (let i = 2; i < input.length; i++) {
            let tree = trees[input.substr(i - 2, 2)], cpath = tree.findPath(input[i]);
            if (!cpath) return null;
            if (i === input.length - 1) cpath = cpath.replace(/0+$/, '') || '0';
            path += cpath;
        }
    }

    for (let i = 0; i < path.length; i++) {
        big = int.mul(big, _2);
        big = int.add(big, path[i] === '1' ? int.one : int.zero);
    }
    while (int.cmp(big, int.zero) > 0) {
        let [quotient, remainder] = [int.div(big, symlen), int.mod(big, symlen)];
        result += symbols[int.floatify(remainder)];
        big = quotient;
    }
    return result;
}

let memoizedDecompress: {[key: string]: string} = {};
export function decompress(compressed: string): string {
    if (compressed in memoizedDecompress) return memoizedDecompress[compressed];

    let big = int.zero;
    for (let ch of compressed.split("").reverse()) {
        big = int.add(int.mul(big, int.make(symbols.length)), int.make(symbols.indexOf(ch)));
    }

    let path = '', result = '. ', pathIdx = 1;
    while (int.floatify(big)) {
        path = int.bitand(big, int.one).toString() + path;
        big = int.div(big, _2);
    }

    while (pathIdx < path.length) {
        let tree = trees[result.substr(result.length - 2)];
        let { result: segment, newidx } = tree.traverse(path, pathIdx);
        result += segment;
        pathIdx = newidx;
    }
    return memoizedDecompress[compressed] = result.substr(2);
}
