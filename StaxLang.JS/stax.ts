import { StaxArray, StaxNumber, StaxValue,
    isArray, isFloat, isNumber, isTruthy, isMatrix,
    last, A2S, S2A, floatify, constants, widenNumbers, runLength,
    areEqual, indexOf, compare,
    stringFormat, unEval, stringFormatFloat, pow, materialize } from './types';
import { Block, Program, parseProgram } from './block';
import { unpack, isPacked } from './packer';
import * as int from './integer';
import { isInt, StaxInt, zero, one, minusOne } from './integer';
import { Rational, zero as ratZero, rationalize } from './rational';
import IteratorPair from './iteratorpair';
import { Multiset, StaxSet, StaxMap, IntRange } from './collections';
import { primeFactors, allPrimes } from './primehelper';
import { decompress } from './huffmancompression';
import { uncram, uncramSingle } from './crammer';
import { macroTrees, getTypeChar } from './macrotree';
import { ensureStableSort } from './stable-sort';

export class ExecutionState {
    public ip: number;
    public cancel: boolean;
    public break: boolean;

    constructor(ip: number, cancel = false, break_ = false) {
        this.ip = ip;
        this.cancel = cancel;
        this.break = break_;
    }
}

function fail(msg: string): never {
    throw new Error(msg);
}

function range(start: number | StaxInt, end: number | StaxInt): IntRange {
    return new IntRange(
        typeof start === "number" ? int.make(start) : start,
        typeof end === "number" ? int.make(end) : end);
}

class EarlyTerminate extends Error {
    constructor(msg: string) {
        super(msg);
    }
}

type FormatOptions = { zeroString?: string };
export class Runtime {
    private standardOut: (line: string) => void;
    private infoOut?: (line: string) => void;
    private program: Program;
    private mainStack: StaxValue[] = [];
    private inputStack: StaxValue[] = [];
    private producedOutput = false;
    private warnedInstructions: string[] = [];

    private gotoCallDepth = 0;
    private callStackFrames: {_: StaxValue | IteratorPair, indexOuter: StaxInt}[] = [];
    private _: StaxValue | IteratorPair;
    private index = zero;
    private indexOuter = zero;
    private x: StaxValue = zero;
    private y: StaxValue;
    private implicitEval = false;

    constructor(partialOutput: (content: string) => void, info?: (line: string) => void) {
        this.standardOut = partialOutput;
        this.infoOut = info;
        ensureStableSort();
    }

    private format(arg: (StaxValue | IteratorPair), options?: FormatOptions): string {
        const _10 = int.make(10), _32 = int.make(32), _127 = int.make(127);
        const nul = options && options.zeroString || "\\0";
        if (arg instanceof IteratorPair) return `(${ this.format(arg.item1) }, ${ this.format(arg.item2) })`;
        if (isNumber(arg)) return arg.toString();
        if (arg instanceof Block) return `Block ${ arg.contents }`;

        if (arg.every(e => isInt(e) && int.eq(e, zero))) {
            return '[' + Array(arg.length).fill("0").join(", ") + ']';
        }
        if (arg.every(e => isInt(e) && (int.eq(e, zero) || int.eq(e, _10) || int.cmp(e, _32) >= 0 && int.cmp(e, _127) < 0))) {
            return JSON.stringify(String.fromCharCode(...arg.map(e => floatify(e as StaxInt)))).replace(/\\u0000/g, nul);
        }
        if (arg instanceof IntRange && arg.length >= 3) {
            return `[${ arg.start } .. ${ arg.end != null ? int.sub(arg.end, one) : '' }]`;
        }

        return '[' + arg.map(val => this.format(val, options)).join(", ") + ']';
    }

    public getDebugState(options?: FormatOptions) {
        return {
            implicitEval: this.implicitEval,
            x: this.format(this.x, options),
            y: this.format(this.y, options),
            index: this.index,
            _: this.format(this._),
            main: this.mainStack.map(val => this.format(val, options)).reverse(),
            input: this.inputStack.map(val => this.format(val, options)).reverse(),
        };
    }

    private push(...vals: StaxValue[]) {
        vals.forEach(e => this.mainStack.push(e));
    }

    private peek(): StaxValue {
        if (this.mainStack.length) return last(this.mainStack)!;
        if (this.inputStack.length) return last(this.inputStack)!;
        throw new EarlyTerminate("peeked empty stack");
    }

    private pop(): StaxValue {
        if (this.mainStack.length) return this.mainStack.pop()!;
        if (this.inputStack.length) return this.inputStack.pop()!;
        throw new EarlyTerminate("popped empty stack");
    }

    private popArray(): StaxArray {
        let p = this.pop();
        return isArray(p) ? p : fail("expected array");
    }

    private popInt(): StaxInt {
        let p = this.pop();
        return isInt(p) ? p : fail("expected int");
    }

    private totalSize() {
        return this.mainStack.length + this.inputStack.length;
    }

    private pushStackFrame() {
        this.callStackFrames.push({_: this._, indexOuter: this.indexOuter});
        [this.indexOuter, this.index] = [this.index, zero];
    }
    private popStackFrame() {
        this.index = this.indexOuter;
        let popped = this.callStackFrames.pop();
        if (!popped) throw new Error("tried to pop a stack frame; wasn't one");
        this._ = popped._;
        this.indexOuter = popped.indexOuter;
    }

    private lastNewline = false;
    private print(val: StaxValue | string, newline = true) {
        this.producedOutput = true;

        if (isFloat(val)) val = stringFormatFloat(val);
        if (isInt(val)) val = val.toString();
        if (val instanceof Block) val = `Block: ${val.contents}`;
        if (isArray(val)) val = A2S(val);
        if (val instanceof Rational) val = val.toString();

        this.standardOut(val + (newline ? "\n" : ""));
        this.lastNewline = newline;
    }

    private doEval(): boolean {
        let a = this.pop();
        if (!isArray(a)) throw new Error("tried to eval a non-array");
        let arg = A2S(a);
        let activeArrays: StaxValue[][] = [];

        const newValue = (val: StaxValue) => {
            if (activeArrays.length) last(activeArrays)!.push(val);
            else this.push(val);
        };

        for (let i = 0; i < arg.length; i++) {
            switch (arg[i]) {
                case '[':
                    activeArrays.push([]);
                    break;
                case ']':
                    if (!activeArrays.length) return false;
                    newValue(activeArrays.pop()!);
                    break;
                case '"':
                    let strliteral = /^"([^\\"]|\\.)*"/.exec(arg.substr(i));
                    if (!strliteral) return false;
                    let finishPos = i + strliteral[0].length - 1;
                    let str = arg.substring(i + 1, finishPos);
                    str = str.replace(/\\n/g, "\n");
                    str = str.replace(/\\"/g, '"');
                    str = str.replace(/\\\\/g, "\\");
                    str = str.replace(/\\x[0-9a-f]{2}/ig,
                        s => String.fromCharCode(parseInt(s.substr(2), 16)));
                    newValue(S2A(str));
                    i = finishPos;
                    break;
                case '\u221E':
                    newValue(Number.POSITIVE_INFINITY);
                    break;
                case '-':
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    let substring = arg.substr(i);
                    if (substring.substr(0, 2) === "-\u221E") {
                        newValue(Number.NEGATIVE_INFINITY);
                        i += 1;
                        break;
                    }

                    let match = substring.match(/^-?\d+(\.\d+([eE]-?\d+)?|[eE]-?\d+)/);
                    if (match) {
                        newValue(parseFloat(match[0]));
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^(-?\d+)\/(-?\d+)/);
                    if (match) {
                        newValue(new Rational(int.make(match[1]), int.make(match[2])));
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^-?0x([0-9a-f]+)/i);
                    if (match) {
                        const _16 = int.make(16);
                        let hex = [...match[1].toLowerCase()].reduce(
                            (acc, dig) => int.add(
                                int.mul(acc, _16), 
                                int.make("0123456789abcdef".indexOf(dig))
                            ), 
                            int.zero);
                        if (substring.startsWith("-")) hex = int.negate(hex);
                        newValue(hex);
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^-?0b([01]+)/i);
                    if (match) {
                        const _2 = int.make(2);
                        let hex = [...match[1].toLowerCase()].reduce(
                            (acc, dig) => int.add(
                                int.mul(acc, _2), 
                                int.make("01".indexOf(dig))
                            ), 
                            int.zero);
                        if (substring.startsWith("-")) hex = int.negate(hex);
                        newValue(hex);
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^-?\d+/);
                    if (match) {
                        newValue(int.make(match[0]));
                        i += match[0].length - 1;
                        break;
                    }
                    return false;

                case ' ': case '\t': case '\r': case '\n': case ',':
                    break;

                default: return false;
            }
        }
        return true;
    }

    public *runProgram(program: string, stdin: string[]) {
        if (isPacked(program)) program = unpack(program);
        this._ = S2A(stdin.join("\n"));
        stdin = [...stdin].reverse(); // copy for mutations
        while (stdin[0] === "") stdin.shift();
        this.inputStack = stdin.map(S2A);
        this.y = last(this.inputStack) || [];
        this.implicitEval = false;

        // starting 'i' suppresses eval
        if (program.match(/^( |\t.*\n)*i/)) {}
        else if (stdin.length === 1) {
            if (!this.doEval()) {
                this.mainStack = [];
                this.inputStack = stdin.reverse().map(S2A);
            }
            else if (this.totalSize() === 0) {
                this.inputStack = stdin.reverse().map(S2A);
            }
            else {
                this.implicitEval = true;
                this.x = this.mainStack[0];
                [this.mainStack, this.inputStack] = [this.inputStack, this.mainStack];
            }
        }
        else if (stdin.length >= 2 && stdin[0] === '"""' && last(stdin) === '"""') {
            this.inputStack = [S2A(stdin.slice(1, stdin.length - 1).reverse().join("\n"))];
        }

        if (this.inputStack.length > 0 && !this.implicitEval) switch (program[0]) {
            case 'm': // line-map
            case 'f': // line-filter
            case 'F': // line-for
                this.runMacro("L");
                break;
        }

        let block = this.program = parseProgram(program);
        try {
            for (let s of this.runSteps(block)) yield s;
            while (this.totalSize() && this.peek() instanceof Block) {
                for (let _ of this.runSteps(this.pop() as Block)) { }
            }
        }
        catch (e) {
            if (e instanceof EarlyTerminate) {} // proceed
            else throw e;
        }

        if (!this.producedOutput && this.totalSize()) this.print(this.pop());
        if (!this.lastNewline) this.print("");
    }

    private *runSteps(block: Block): IterableIterator<ExecutionState> {
        let ip = block.offset, i = 0;
        for (let token of block.tokens) {
            i += 1;
            const getRest = () => new Block(
                block.contents.substr(ip + token.length),
                block.tokens.slice(i),
                ip + token.length,
                false);

            if (token === '|`') {
                yield new ExecutionState(ip += 2, false, true);
                continue;
            }
            // don't step on a no-op
            else if (typeof token !== 'string' || !token.match(/^[ \n]/)) yield new ExecutionState(ip);

            if (token instanceof Block) {
                this.push(token);
                ip += token.contents.length;
                continue;
            }
            else if (!!token.match(/^\d+!/)) this.push(parseFloat(token.replace("!", ".")));
            else if (!!token[0].match(/^\d/)) this.push(int.make(token));
            else if (token.startsWith('"')) this.doEvaluateStringToken(token);
            else if (token.startsWith('`')) {
                let compressed = token.replace(/^`|`$/g, '');
                this.push(S2A(decompress(compressed)));
                if (token[token.length - 1] !== '`') this.print(this.peek());
            }
            else if (token.startsWith("'") || token.startsWith(".")) this.push(S2A(token.substr(1)));
            else if (token.startsWith('V')) this.push(constants[token[1]]);
            else if (token.startsWith(':')) this.doMacroAlias(token[1]);
            else if (token.startsWith('g')) { // generator
                // shorthand is indicated by
                //  no trailing block
                //  OR trailing block with explicit close }, in which case it's a filter
                let peeked = this.peek();
                let shorthand = !(peeked instanceof Block)
                    || (block as Block).contents[ip - 1] == '}';
                for (let s of this.doGenerator(shorthand, token[1], getRest())) {
                    yield s;
                }
                if (shorthand) break;
            }
            else if (token.startsWith('\t')) {} // tab starts comment
            else if (token.match(/^\s+$/)) {} // noop
            else switch (token) {
                case '}':
                    return;
                case '~':
                    this.inputStack.push(this.pop());
                    break;
                case ';':
                    if(!this.inputStack.length) throw new EarlyTerminate("input stack empty");
                    this.push(last(this.inputStack)!);
                    break;
                case ',':
                    if(!this.inputStack.length) throw new EarlyTerminate("input stack empty");
                    this.push(this.inputStack.pop()!);
                    break;
                case '#': {
                    let b = this.pop(), a = this.pop();
                    if (isNumber(a) && isNumber(b)) {
                        this.push(pow(a, b));
                    }
                    else {
                        if (isNumber(a)) [a, b] = [b, a];
                        this.push(a, b);
                        if (isArray(this.peek())) this.runMacro("/%v");
                        else if (isNumber(this.peek())) this.runMacro("]|&%");
                    }
                    break;
                }
                case '_':
                    if (this._ instanceof IteratorPair) this.push(this._.item1, this._.item2);
                    else this.push(this._);
                    break;
                case '!':
                    if (this.peek() instanceof Block) {
                        for (let s of this.runSteps(this.pop() as Block)) {
                            if (s.cancel) break;
                            yield s;
                        }
                    }
                    else this.push(isTruthy(this.pop()) ? zero : one);
                    break;
                case '+':
                    this.doPlus();
                    break;
                case '-':
                    this.doMinus();
                    break;
                case '*':
                    for (let s of this.doStar()) yield s;
                    break;
                case '/':
                    for (let s of this.doSlash()) yield s;
                    break;
                case '\\':
                    this.doZipRepeat();
                    break;
                case '%':
                    this.doPercent();
                    break;
                case '&':
                    for (let s of this.doAssignIndex()) yield s;
                    break;
                case '=':
                    this.push(areEqual(this.pop(), this.pop()) ? one : zero);
                    break;
                case '<': {
                    let b = this.pop(), a = this.pop();
                    this.push(compare(a, b) < 0 ? one : zero);
                    break;
                }
                case '>': {
                    let b = this.pop(), a = this.pop();
                    this.push(compare(a, b) > 0 ? one : zero);
                    break;
                }
                case '?': {
                    let b = this.pop(), a = this.pop();
                    let result = isTruthy(this.pop()) ? a : b;
                    if (result instanceof Block) {
                        for (let s of this.runSteps(result)) {
                            if (s.cancel) break;
                            yield s;
                        }
                    }
                    else this.push(result);
                    break;
                }
                case '@':
                    this.doAt();
                    break;
                case '$':
                    this.push(stringFormat(this.pop()));
                    break;
                case '(':
                    for (let s of this.doPadRight()) yield s;
                    break;
                case ')':
                    for (let s of this.doPadLeft()) yield s;
                    break;
                case '[': {
                    let b = this.pop(), a = this.peek();
                    this.push(a, b);
                    break;
                }
                case ']':
                    this.push([this.pop()]);
                    break;
                case 'a': {
                    let c = this.pop(), b = this.pop(), a = this.pop();
                    this.push(b, c, a);
                    break;
                }
                case 'A':
                    this.push(int.make(10))
                    break;
                case 'b': {
                    let b = this.pop(), a = this.peek();
                    this.push(b, a, b);
                    break;
                }
                case 'B':
                    if (isInt(this.peek())) this.doOverlappingBatch();
                    else if (isArray(this.peek())) this.runMacro("c1tsh"); // uncons
                    else if (this.peek() instanceof Rational) this.runMacro("c@s1%"); // properize
                    else if (typeof this.peek() === "number") {
                        let buf = new ArrayBuffer(8), view = new DataView(buf);
                        view.setFloat64(0, this.pop() as number);
                        let result: StaxInt[] = [];
                        for (let i = 0; i < 8; i++) {
                            for (let j = 7; j >= 0; j--) {
                                result.push(view.getUint8(i) >> j & 1 ? one : zero);
                            }
                        }
                        this.push(result);
                    }
                    else if (this.peek() instanceof Block) {
                        let b = this.pop() as Block;
                        for (let i = 0; i < 3; i++) {
                            for (let s of this.runSteps(b)) yield s;
                        }
                    }
                    else fail("bad type for B");
                    break;
                case 'c':
                    this.push(this.peek());
                    break;
                case 'C':
                    if (this.peek() instanceof Block) {
                        for (let s of this.doCollect()) yield s;
                    }
                    else {
                        if (isTruthy(this.pop())) {
                            yield new ExecutionState(ip, true);
                            return;
                        }
                    }
                    break;
                case 'd':
                    this.pop();
                    break;
                case 'D':
                    if (isArray(this.peek())) { // remove first element
                        let result = [...this.popArray()];
                        result.shift();
                        this.push(result);
                    }
                    else if (isInt(this.peek())) { // n times do
                        let n = this.popInt();
                        this.pushStackFrame();
                        for (this.index = zero; int.cmp(this.index, n) < 0; this.index = int.add(this.index, one)) {
                            this._ = int.add(this.index, one);
                            for (let s of this.runSteps(getRest())) yield s;
                        }
                        this.popStackFrame();
                        return;
                    }
                    else if (isNumber(this.peek())) {
                        this.runMacro("1%"); // get fractional part
                    }
                    else if (this.peek() instanceof Block) {
                        let b = this.pop() as Block;
                        for (let i = 0; i < 2; i++) {
                            for (let s of this.runSteps(b)) yield s;
                        }
                    }
                    break;
                case 'e':
                    if (isArray(this.peek())) {
                        if (!this.doEval()) throw new Error("eval failed");
                    }
                    else if (typeof this.peek() === "number") {
                        this.push(int.make(Math.ceil(this.pop() as number)));
                    }
                    else if (this.peek() instanceof Rational) {
                        this.push((this.pop() as Rational).ceiling())
                    }
                    else if (this.peek() instanceof Block) {
                        for (let s of this.doExtremaBy(-1)) yield s;
                    }
                    break;
                case 'E':
                    if (this.peek() instanceof Block) {
                        for (let s of this.doExtremaBy(1)) yield s;
                    }
                    else this.doExplode();
                    break;
                case 'f': {
                    let shorthand = !(this.peek() instanceof Block);
                    for (let s of this.doFilter(getRest())) yield s;
                    if (shorthand) return;
                    break;
                }
                case 'F': {
                    let shorthand = !(this.peek() instanceof Block);
                    for (let s of this.doFor(getRest())) yield s;
                    if (shorthand) return;
                    break;
                }
                case 'G': {
                    let target = this.program.getGotoTarget(++this.gotoCallDepth);
                    for (let s of this.runSteps(target)) {
                        if (s.cancel) break;
                        yield s;
                    }
                    --this.gotoCallDepth;
                    break;
                }
                case 'h':
                    if (isNumber(this.peek())) this.runMacro("2/");
                    else if (isArray(this.peek())) {
                        let arr = this.popArray();
                        if (arr.length === 0) return;
                        this.push(...arr.slice(0, 1));
                    }
                    else if (this.peek() instanceof Block) {
                        let pred = this.pop() as Block, result: StaxValue[] = [], arr = this.pop(), cancelled = false;
                        if (!isArray(arr)) throw new Error("bad types for take-while");

                        this.pushStackFrame();
                        for (let e of arr) {
                            this.push(this._ = e);
                            for (let s of this.runSteps(pred)) {
                                if (cancelled = s.cancel) break;
                                yield s;
                            }
                            if (cancelled || !isTruthy(this.pop())) break;
                            result.push(e);
                            this.index = int.add(this.index, one);
                        }
                        this.popStackFrame();
                        this.push(result);
                    }
                    break;
                case 'H':
                    if (isNumber(this.peek())) this.runMacro("2*");
                    else if (isArray(this.peek())) {
                        let arr = this.popArray();
                        if (arr.length === 0) return;
                        this.push(last(arr)!);
                    }
                    else if (this.peek() instanceof Block) {
                        let pred = this.pop() as Block, result: StaxValue[] = [], arr = this.pop(), cancelled = false;
                        if (!isArray(arr)) throw new Error("bad types for take-while");
                        arr = [...arr].reverse();

                        this.pushStackFrame();
                        for (let e of arr) {
                            this.push(this._ = e);
                            for (let s of this.runSteps(pred)) {
                                if (cancelled = s.cancel) break;
                                yield s;
                            }
                            if (cancelled || !isTruthy(this.pop())) break;
                            result.unshift(e);
                            this.index = int.add(this.index, one);
                        }
                        this.popStackFrame();
                        this.push(result);
                    }
                    break;
                case 'i':
                    // leading i suppresses eval, which is already taken care of
                    if (this.callStackFrames.length) this.push(this.index);
                    break;
                case 'I':
                    for (let s of this.doIndexOfOrAnd()) yield s;
                    break;
                case 'j':
                    if (isArray(this.peek())) this.runMacro("' /");
                    else if (isInt(this.peek())) {
                        let digits = this.popInt(), num = this.pop();
                        num = isNumber(num) ? floatify(num) : fail("can't round a non-number");
                        this.push(S2A(num.toFixed(int.floatify(digits))));
                    }
                    else if (isNumber(this.peek())) {
                        this.runMacro("2u+@");
                    }
                    else if (this.peek() instanceof Block) {
                        for (let s of this.doFindFirst()) yield s;
                    }
                    else throw new Error("unknown type for j");
                    break;
                case 'J':
                    if (isArray(this.peek())) {
                        this.runMacro("0]*");
                    }
                    else if (isNumber(this.peek())) {
                        this.runMacro("c*");
                    }
                    else if (this.peek() instanceof Block) {
                        for (let s of this.doFindFirst(true)) yield s;
                    }
                    else throw new Error("unknown type for J");
                    break;
                case 'k': {
                    let shorthand = !(this.peek() instanceof Block);
                    for (let s of this.doReduce(getRest())) yield s;
                    if (shorthand) return;
                    break;
                }
                case 'K': {
                    let shorthand = !(this.peek() instanceof Block);
                    for (let s of this.doCrossMap(getRest())) yield s;
                    if (shorthand) return;
                    break;
                }
                case 'l': {
                    let a = this.pop();
                    if (a instanceof Rational) this.push([a.numerator, a.denominator]);
                    else if (isInt(a)) {
                        let result: StaxValue[] = [];
                        for (let i = 0; i < a.valueOf(); i++) result.unshift(this.pop());
                        this.push(result);
                    }
                    else throw new Error("bad types for l");
                    break;
                }
                case 'L':
                    this.mainStack = [[...this.mainStack.reverse(), ...this.inputStack.reverse()]];
                    this.inputStack = [];
                    break;
                case 'm': {
                    let shorthand = !(this.peek() instanceof Block);
                    for (let s of this.doMap(getRest())) yield s;
                    if (shorthand) return;
                    break;
                }
                case 'M':
                    for (let s of this.doTransposeOrMaybe()) yield s;
                    break;
                case 'n':
                    this.push(this.pop(), this.peek());
                    break;
                case 'N':
                    if (isNumber(this.peek())) this.runMacro("U*");
                    else if (isArray(this.peek())) this.runMacro("c1TsH");
                    else if (this.peek() instanceof Block) {
                        let block = this.pop() as Block, n = this.inputStack.pop();
                        if (isInt(n)) {
                            for (this.pushStackFrame(); int.cmp(this.index, n) < 0; this.index = int.add(this.index, one)) {
                                for (let s of this.runSteps(block)) {
                                    if (!s.cancel) yield s;
                                }
                            }
                            this.popStackFrame();
                        }
                    }
                    break;
                case 'o':
                    for (let s of this.doOrder()) yield s;
                    break;
                case 'O':
                    this.runMacro("1s");
                    break;
                case 'p':
                    this.print(this.pop(), false);
                    break;
                case 'P':
                    this.print(this.pop());
                    break;
                case 'q':
                    this.print(this.peek(), false);
                    break;
                case 'Q':
                    this.print(this.peek());
                    break;
                case 'r': {
                    let top = this.pop();
                    if (isInt(top)) this.push(range(0, top));
                    else if (isArray(top)) this.push([...top].reverse());
                    else if (top instanceof Rational) this.push(top.numerator);
                    break;
                }
                case 'R':
                    if (isInt(this.peek())) this.push(range(1, int.add(this.popInt(), one)));
                    else if (this.peek() instanceof Rational) this.push((this.pop() as Rational).denominator);
                    else for (let s of this.doRegexReplace()) yield s;
                    break;
                case 's':
                    this.push(this.pop(), this.pop());
                    break;
                case 'S':
                    this.doPowersetOrXor();
                    break;
                case 't':
                    if (isArray(this.peek())) {
                        this.push(S2A(A2S(this.pop() as StaxArray).replace(/^\s+/, "")))
                    }
                    else if (this.peek() instanceof Block) {
                        let pred = this.pop() as Block, arr = this.pop(), cancelled = false;
                        if (!isArray(arr)) throw new Error("bad types for trim");
                        let result = [...arr];

                        this.pushStackFrame();
                        while (result.length) {
                            this.push(this._ = result[0]);
                            for (let s of this.runSteps(pred)) {
                                if (cancelled = s.cancel) break;
                                yield s;
                            }
                            if (cancelled || !isTruthy(this.pop())) break;
                            result.shift();
                            this.index = int.add(this.index, one);
                        }
                        this.popStackFrame();
                        this.push(result);
                    }
                    else {
                        if (this.totalSize() < 2) {
                            this.push(this.pop());
                            break;
                        }
                        let top = this.pop(), next = this.pop();
                        if (isNumber(top) && isNumber(next)) {
                            this.push(compare(next, top) < 0 ? next : top);
                        }
                        else {
                            this.inputStack.push(top);
                            this.push(next);
                            this.runMacro("c%,-0T)");
                        }
                    }
                    break;
                case 'T':
                    if (isArray(this.peek())) {
                        this.push(S2A(A2S(this.pop() as StaxArray).replace(/\s+$/, "")))
                    }
                    else if (this.peek() instanceof Block) {
                        let pred = this.pop() as Block, arr = this.pop(), cancelled = false;
                        if (!isArray(arr)) throw new Error("bad types for trim");
                        let result = [...arr];

                        this.pushStackFrame();
                        while (result.length) {
                            this.push(this._ = last(result)!);
                            for (let s of this.runSteps(pred)) {
                                if (cancelled = s.cancel) break;
                                yield s;
                            }
                            if (cancelled || !isTruthy(this.pop())) break;
                            result.pop();
                            this.index = int.add(this.index, one);
                        }
                        this.popStackFrame();
                        this.push(result);
                    }
                    else {
                        if (this.totalSize() < 2) {
                            this.push(this.pop());
                            break;
                        }
                        let top = this.pop(), next = this.pop();
                        if (isNumber(top) && isNumber(next)) {
                            this.push(compare(next, top) > 0 ? next : top);
                        }
                        else {
                            this.inputStack.push(top);
                            this.push(next);
                            this.runMacro("c%,-0T(");
                        }
                    }
                    break;
                case 'u': {
                        let arg = this.pop();
                        if (isArray(arg)) {
                            let set = new StaxSet, result: StaxValue[] = [];
                            for (let el of arg) {
                                if (set.has(el)) continue;
                                result.push(el);
                                set.add(el);
                            }
                            this.push(result);
                        }
                        else if (isInt(arg)) this.push(new Rational(one, arg));
                        else if (arg instanceof Rational) this.push(arg.invert());
                        else if (typeof arg === "number") this.push(1 / arg);
                        else fail("bad type for u");
                        break;
                    }
                case 'U':
                    this.push(minusOne);
                    break;
                case 'v':
                    if (isNumber(this.peek())) this.runMacro("1-");
                    else if (isArray(this.peek())) this.push(S2A(A2S(this.pop() as StaxArray).toLowerCase()));
                    else throw new Error("unknown type for ^");
                    break;
                case '^':
                    if (isNumber(this.peek())) this.runMacro("1+");
                    else if (isArray(this.peek())) this.push(S2A(A2S(this.pop() as StaxArray).toUpperCase()));
                    else throw new Error("unknown type for ^");
                    break;
                case 'w': {
                    let shorthand = this.totalSize() === 0 || !(this.peek() instanceof Block);
                    for (let s of this.doWhile(getRest())) {
                        if (s.cancel) return;
                        yield s;
                    }
                    if (shorthand) return;
                    break;
                }
                case 'W': {
                    let shorthand = this.totalSize() === 0 || !(this.peek() instanceof Block);
                    for (let s of this.doUnconditionalWhile(getRest())) {
                        if (s.cancel) return;
                        yield s;
                    }
                    if (shorthand) return;
                    break;
                }
                case 'x':
                    this.push(this.x);
                    break;
                case 'X':
                    this.x = this.peek();
                    break;
                case 'y':
                    this.push(this.y);
                    break;
                case 'Y':
                    this.y = this.peek();
                    break;
                case 'z':
                    this.push([]);
                    break;
                case 'Z':
                    this.runMacro('0s');
                    break;
                case '|?':
                    if (block instanceof Block) this.push(S2A(block.contents));
                    else this.push(S2A(block));
                    break;
                case '| ':
                    this.print(' ', false);
                    break;
                case '|;':
                    this.push(int.mod(this.index, int.make(2)));
                    break;
                case '|~':
                    this.doLastIndexOf();
                    break;
                case '|@':
                    this.doRemoveOrInsert();
                    break;
                case '|&':
                    if (isArray(this.peek())) { // set intersection
                        let b = this.popArray(), a = this.pop();
                        if (!isArray(a)) a = [a];
                        let result: StaxValue[] = [];
                        const bSet = new StaxSet(b);
                        for (let e of a) if (bSet.has(e)) result.push(e);
                        this.push(result);
                    }
                    else {
                        if (this.infoOut) this.infoOut("<code>|&</code> is deprecated for bitwise and.  Use <code>I</code> instead.");
                        if (this.totalSize() < 2) break;
                        this.push(int.bitand(this.popInt(), this.popInt()));
                    }
                    break;
                case '|#':
                    if (isArray(this.peek())) { // number of occurrences in array
                        let b = this.popArray(), a = this.popArray();
                        this.push(int.make(a.filter(e => areEqual(e, b)).length));
                    }
                    break;
                case '|$': {
                    let b = this.pop(), a = this.pop(), cmp = compare(a, b);
                    if (cmp > 0) this.push(one);
                    else if (cmp < 0) this.push(minusOne);
                    else this.push(zero);
                    break;
                }
                case '||':
                    if (isInt(this.peek())) {
                        if (this.infoOut) this.infoOut("<code>||</code> is deprecated for bitwise or.  Use <code>M</code> instead.");
                        if (this.totalSize() < 2) break;
                        this.push(int.bitor(this.popInt(), this.popInt()));
                    }
                    else if (isArray(this.peek())) {
                        // embed grid at coords
                        let payload = this.popArray();
                        let col = int.floatify(this.popInt()), row = int.floatify(this.popInt());
                        let result = [...this.popArray()];

                        let r = -1;
                        for (let payline of payload) {
                            ++r;
                            payline = isArray(payline) ? [...payline] : [payline];
                            while (result.length <= row + r) result.push([]);
                            if (!isArray(result[row + r])) result[row + r] = [result[row + r]];

                            let resultline = (result[row + r] as StaxArray).map(c => c);
                            for (let c = 0; c < payline.length; c++) {
                                while (resultline.length <= col + c) resultline.push(zero);
                                resultline[col + c] = payline[c];
                            }
                            result[row + r] = resultline;
                        }

                        this.push(result);
                    }
                    break;
                case '|(':
                    this.doRotate(-1);
                    break;
                case '|)':
                    this.doRotate(1);
                    break;
                case '|[':
                    this.runMacro("~;%R{;(m,d"); // all prefixes
                    break;
                case '|]':
                    this.runMacro("~;%R{;)mr,d"); // all suffixes
                    break;
                case '|{':
                    this.push(new StaxSet(this.popArray()).eq(new StaxSet(this.popArray())) ? one : zero);
                    break;
                case '|}':
                    this.push(new Multiset(this.popArray()).eq(new Multiset(this.popArray())) ? one : zero);
                    break;
                case '|^':
                    if (isArray(this.peek())) {
                        this.runMacro("s b-~ s-, +"); // symmetric array difference
                    }
                    else if (isInt(this.peek())) { // tuples of specified size from array elements
                        if (this.totalSize() < 2) break;
                        let b = this.popInt(), a = this.pop();
                        if (isArray(a)) {
                            let result: StaxValue[] = [[]], els = a, _b = b.valueOf();
                            for (let i = 0; i < _b; i++) {
                                result = ([] as StaxArray).concat(
                                    ...result.map((r: StaxArray) => els.map(e => [...r, e])))
                            }
                            this.push(result);
                        }
                        else if (isInt(a)) { // xor
                            if (this.infoOut) this.infoOut("<code>|^</code> is deprecated for bitwise xor.  Use <code>S</code> instead.");
                            this.push(int.bitxor(a, b));
                        }
                    }
                    break;
                case '|<':
                    if (isInt(this.peek())) this.runMacro('cU>{|2*}{N|2/}?');
                    else if (isArray(this.peek())) {
                        let arr = [...this.popArray()], maxlen = 0, result = [];
                        for (let i = 0; i < arr.length; i++) {
                            if (!isArray(arr[i])) arr[i] = stringFormat(arr[i]);
                            maxlen = Math.max(maxlen, (arr[i] as StaxArray).length);
                        }
                        for (let i = 0; i < arr.length; i++) {
                            let line = Array(maxlen - (arr[i] as StaxArray).length).fill(zero);
                            line.unshift(...arr[i] as StaxArray);
                            result.push(line);
                        }
                        this.push(result);
                    }
                    break;
                case '|>':
                    if (isInt(this.peek())) this.runMacro('cU>{|2/}{N|2*}?');
                    else if (isArray(this.peek())) {
                        let arr = [...this.popArray()], maxlen = 0, result = [];
                        for (let i = 0; i < arr.length; i++) {
                            if (!isArray(arr[i])) arr[i] = stringFormat(arr[i]);
                            maxlen = Math.max(maxlen, (arr[i] as StaxArray).length);
                        }
                        for (let i = 0; i < arr.length; i++) {
                            let line = Array(maxlen - (arr[i] as StaxArray).length).fill(zero);
                            line.push(...arr[i] as StaxArray);
                            result.push(line);
                        }
                        this.push(result);
                    }
                    break;
                case '|=':
                    if (isArray(this.peek())) { // multi-mode
                        let arr = this.popArray(), result: StaxValue[] = [];
                        if (arr instanceof IntRange) this.push(arr);
                        else {
                            if (arr.length > 0) {
                                let multi = new Multiset(arr), keys = multi.keys();
                                let max = Math.max(...keys.map(k => multi.get(k)));
                                result = keys.filter(k => multi.get(k) === max);
                                result.sort(compare);
                            }
                            this.push(result);
                        }
                    }
                    break;
                case '|!':
                    if (isInt(this.peek())) this.doPartition();
                    else if (isArray(this.peek())) this.doMultiAntiMode();
                    break;
                case '|+':
                    if (isNumber(this.peek())) this.runMacro("c^*h");
                    else if (this.peek() instanceof IntRange) {
                        const range = this.popArray() as IntRange;
                        if (range.end == null) this.push(Number.POSITIVE_INFINITY);
                        // gauss sum
                        else this.push(int.div(int.mul(int.sub(range.end, range.start), int.sub(int.add(range.start, range.end), int.one)), int.make(2)));
                    }
                    else this.runMacro('Z{+F');
                    break;
                case '|-': { // multiset subtract
                    let b = this.pop(), a = this.popArray();
                    if (!isArray(b)) b = [b];
                    let result = [], bset = new Multiset(b);
                    for (let e of a) {
                        if (bset.contains(e)) bset.remove(e);
                        else result.push(e);
                    }
                    this.push(result);
                    break;
                }
                case '|*': {
                    let b = this.pop(), a = this.pop();
                    if (isNumber(a) && isNumber(b)) {
                        if (this.warnedInstructions.indexOf(token) < 0) {
                            this.warnedInstructions.push(token);
                            if (this.infoOut) this.infoOut("<code>|*</code> for exponent is deprecated.  Use <code>#</code> instead.");
                        }
                        this.push(pow(a, b));
                    }
                    else if (isInt(b) && isArray(a)) {
                        let result = [];
                        for (let e of a) result.push(...Array(Math.abs(int.floatify(b))).fill(e));
                        this.push(result);
                    }
                    else if (isArray(b)) {
                        if (!isArray(a)) throw new Error('tried to cross-product non-array');
                        let result = [];
                        for (let a_ of a) for (let b_ of b) result.push([a_, b_]);
                        this.push(result);
                    }
                    break;
                }
                case '|/': {
                    let b = this.pop(), a = this.pop();
                    if (isInt(a) && isInt(b)) {
                        if (!int.eq(a, zero) && int.abs(b).valueOf() > 1) {
                            while (int.eq(zero, int.mod(a, b)) && !int.eq(b, one)) a = int.div(a, b);
                        }
                        this.push(a);
                    }
                    else if (isArray(a) && isArray(b)) {
                        b = materialize(b);
                        let result: StaxValue[] = [];
                        for (let i = 0, offset = 0; offset < a.length; i++) {
                            let size = b[i % b.length];
                            if (isNumber(size)) {
                                result.push(a.slice(offset, offset += Math.floor(Number(size.valueOf()))));
                            }
                            else fail("can't multi-chunk by non-number");
                        }
                        this.push(result);
                    }
                    break;
                }
                case '|\\':
                    if (isArray(this.peek())) {
                        this.runMacro("b%s% t~ ;(s,(s \\"); // zip; truncate to shorter
                    }
                    else { // zip arrays using fill element
                        let fill = this.pop(), b = materialize(this.popArray()), a = materialize(this.popArray()), result = [];
                        for (let i = 0; i < Math.max(a.length, b.length); i++) {
                            result.push([
                                i < a.length ? a[i] : fill,
                                i < b.length ? b[i] : fill]);
                        }
                        this.push(result);
                    }
                    break;
                case '|%':
                    if (isNumber(this.peek())) { // divmod
                        this.runMacro("ssb%~/1u*@,");
                    }
                    else if (isArray(this.peek())) { // embed sub-array
                        let c = this.popArray(), b = this.pop(), a = this.popArray();
                        let result = [...a], payload = materialize(c), loc = floatify(b as StaxNumber);

                        if (loc < 0) {
                            loc += result.length;
                            if (loc < 0) {
                                result.unshift(...new Array(-loc).fill(zero));
                                loc = 0;
                            }
                        }

                        for (let i = 0; i < payload.length; i++) {
                            while (loc + i >= result.length) result.push(zero);
                            result[loc + i] = payload[i];
                        }
                        this.push(result);
                    }
                    break;
                case '|0':
                    if (isArray(this.peek())) {
                        let result = minusOne, i = zero;
                        for (let e of this.popArray()) {
                            if (!isTruthy(e)) {
                                result = i;
                                break;
                            }
                            i = int.add(i, one);
                        }
                        this.push(result);
                    }
                    break;
                case '|1':
                    if (isArray(this.peek())) { // index of 1st truthy
                        let result = minusOne, i = 0;
                        for (let e of this.popArray()) {
                            if (isTruthy(e)) {
                                result = int.make(i);
                                break;
                            }
                            ++i;
                        }
                        this.push(result);
                    }
                    else if (isInt(this.peek())) {
                        this.runMacro("2%U1?"); // power of -1
                    }
                    break;
                case '|2':
                    if (isArray(this.peek())) { // diagonal of matrix
                        let result = [], i = 0;
                        for (let e of this.popArray()) {
                            if (isArray(e)) {
                                if (e.length > i) result.push(materialize(e)[i]);
                                else result.push(zero);
                            }
                            else {
                                result.push(i == 0 ? e : zero);
                            }
                            ++i;
                        }
                        this.push(result);
                    }
                    else if (isNumber(this.peek())) this.runMacro("2s#"); // power of 2
                    break;
                case '|3':
                    this.runMacro("36|b"); // base 36
                    break;
                case '|4':
                    this.push(isArray(this.pop()) ? one : zero);
                    break;
                case '|5': { // 0-indexed fibonacci number
                    let n = this.popInt().valueOf(), a = one, b = one;
                    if (n >= 0) for (let i = 0; i < n; i++) [a, b] = [b, int.add(a, b)];
                    else for (let i = 0; i > n; i--) [a, b] = [int.sub(b, a), a];
                    this.push(a);
                    break;
                }
                case '|6': { // 0-indexed nth prime
                    let i = 0, n = floatify(this.popInt());
                    for (let p of allPrimes()) {
                        if (i++ === n) {
                            this.push(p);
                            break;
                        }
                    }
                    break;
                }
                case '|7':
                    this.push(Math.cos(Number((this.pop() as StaxNumber).valueOf())));
                    break;
                case '|8':
                    this.push(Math.sin(Number((this.pop() as StaxNumber).valueOf())));
                    break;
                case '|9':
                    this.push(Math.tan(Number((this.pop() as StaxNumber).valueOf())));
                    break;
                case '|a':
                    if (isNumber(this.peek())) { // absolute value
                        let num = this.pop();
                        if (isInt(num)) this.push(int.abs(num));
                        else if (isFloat(num)) this.push(Math.abs(num));
                        else if (num instanceof Rational) this.push(num.abs());
                        else fail("number but not a number in abs");
                    }
                    else if (isArray(this.peek())) { // any
                        let result = zero;
                        for (let e of this.popArray()) {
                            if (isTruthy(e)) {
                                result = one;
                                break;
                            }
                        }
                        this.push(result);
                    }
                    break;
                case '|A':
                    if (isInt(this.peek())) this.runMacro("As#");
                    else if (isArray(this.peek())) {
                        let result = one;
                        for (let e of this.popArray()) {
                            if (!isTruthy(e)) {
                                result = zero;
                                break;
                            }
                        }
                        this.push(result);
                    }
                    break;
                case '|b':
                    if (isInt(this.peek())) {
                        this.doBaseConvert();
                    }
                    else if (isArray(this.peek())) {
                        // keep elements of a, no more than their occurrences in b
                        let b = [...this.popArray()], a = this.popArray(), result = [];
                        for (let e of a) {
                            for (let i = 0; i < b.length; i++) {
                                if (areEqual(b[i], e)) {
                                    result.push(e);
                                    b.splice(i, 1);
                                    break;
                                }
                            }
                        }
                        this.push(result);
                    }
                    break;
                case '|B':
                    this.runMacro("2|b");
                    break;
                case '|c':
                    if (!isTruthy(this.peek())) {
                        this.pop();
                        yield new ExecutionState(ip, true);
                        return;
                    }
                    break;
                case '|C':
                    this.doCenter();
                    break;
                case '|d':
                    this.push(int.make(this.mainStack.length));
                    break;
                case '|D':
                    this.push(int.make(this.inputStack.length));
                    break;
                case '|e':
                    if (isInt(this.peek())) {
                        this.push(int.eq(zero, int.mod(this.pop() as StaxInt, int.make(2))) ? one : zero);
                    }
                    else if (isArray(this.peek())) {
                        let to = A2S(this.pop() as StaxArray), from = A2S(this.pop() as StaxArray), original = A2S(this.pop() as StaxArray);
                        this.push(S2A(original.replace(from, to)));
                    }
                    break;
                case '|E':
                    this.doBaseConvert(false);
                    break;
                case '|f': {
                    let t = this.pop();
                    if (isInt(t)) this.push(primeFactors(t));
                    else if (isArray(t)) {
                        let text = A2S(this.popArray()), pattern = A2S(t), regex = RegExp(pattern, 'g');
                        let result = [], match: string[] | null;
                        do {
                            match = regex.exec(text);
                            if (match) result.push(S2A(match[0]));
                        } while (match);
                        this.push(result);
                    }
                    break;
                }
                case '|F':
                    if (isInt(this.peek())) { // factorial
                        let result = one, n = this.popInt();
                        for (let i = one; i <= n.valueOf(); i = int.add(i, one)) {
                            result = int.mul(result, i);
                        }
                        this.push(result);
                    }
                    else if (isArray(this.peek())) { // all regex matches
                        let re = new RegExp(A2S(this.popArray()), "g");
                        let input = A2S(this.popArray()), result = [], m;
                        while (m = re.exec(input)) result.push(S2A(m[0]));
                        this.push(result);
                    }
                    break;
                case '|g':
                    this.doGCD();
                    break;
                case '|G': { // round-robin flatten
                    let arr = this.popArray(), result: StaxValue[] = [];
                    let maxlen = Math.max(...arr.map(e => (e as StaxArray).length));
                    const grid = arr.map(e => isArray(e) ? materialize(e) : fail("can't flatten array of non-arrays"));
                    for (let i = 0; i < maxlen; i++) {
                        for (let e of grid) {
                            if (e.length > i) result.push(e[i]);
                        }
                    }
                    this.push(result);
                    break;
                }
                case '|H':
                    this.runMacro("16|b");
                    break;
                case '|i':
                    this.push(this.indexOuter);
                    break;
                case '|I':
                    for (let s of this.doFindIndexAll()) yield s;
                    break;
                case '|j':
                    if (isArray(this.peek())) {
                        this.runMacro("Vn/"); // split on newlines
                    }
                    else if (isInt(this.peek())) {
                        this.runMacro("1u*");
                    }
                    else if (isFloat(this.peek())) {
                        this.push(rationalize(this.pop() as number));
                    }
                    break;
                case '|J':
                    this.runMacro("Vn*"); // join with newlines
                    break;
                case '|k': {
                    const str = A2S(this.popArray()), enc = new TextEncoder;
                    this.push([...enc.encode(str)].map(e => int.make(e)));
                    break;
                }
                case '|K': {
                    const bytes = Uint8Array.from(this.popArray().map(floatify)), dec = new TextDecoder;
                    this.push(S2A(dec.decode(bytes.buffer)));
                    break;
                }
                case '|l': // lcm
                    if (isArray(this.peek())) this.runMacro("O{|lF");
                    else if (isInt(this.peek())) this.runMacro("sb|g~*,n{/}{d}?");
                    else fail("bad types for lcm");
                    break;
                case '|L': {
                    let b = this.pop(), a = this.pop();
                    if (isNumber(b) && isNumber(a)) { // log with base
                        if (isInt(a) && isInt(b)) {
                            // check for exact power
                            let num = a, multiplicity = 0;
                            while (int.mod(num, b).valueOf() == 0 && num.valueOf() > 1) {
                                num = int.div(num, b);
                                multiplicity += 1;
                            }
                            if (num.valueOf() == 1) {
                                this.push(multiplicity);
                                break;
                            }
                        }
                        let result = Math.log(floatify(a)) / Math.log(floatify(b));
                        this.push(result);
                    }
                    else if (isArray(b) && isArray(a)) {
                        // combine elements from a and b, with each occurring the max of its occurrences from a and b
                        let result: StaxValue[] = [], b_ = [...b];
                        for (let e of a) {
                            result.push(e);
                            for (let i = 0; i < b_.length; i++) {
                                if (areEqual(b_[i], e)) {
                                    b_.splice(i, 1);
                                    break;
                                }
                            }
                        }
                        this.push(result.concat(b_));
                    }
                    break;
                }
                case '|m': {
                    if (isNumber(this.peek())) {
                        if (this.warnedInstructions.indexOf(token) < 0) {
                            this.warnedInstructions.push(token);
                            if (this.infoOut) this.infoOut("<code>|m</code> for minimum of scalars is deprecated.  Use <code>t</code> instead.");
                        }
                        if (this.totalSize() < 2) break;
                        let top = this.pop(), next = this.pop();
                        this.push(compare(next, top) < 0 ? next : top);
                    }
                    else if (isArray(this.peek())) {
                        let arr = this.popArray(), result: StaxValue = Number.POSITIVE_INFINITY;
                        if (arr.length === 0) this.push(result);
                        else if (arr instanceof IntRange) this.push(arr.start);
                        else {
                            for (let e of arr) if (compare(e, result) < 0) result = e;
                            this.push(result);
                        }
                    }
                    break;
                }
                case '|M': {
                    if (isNumber(this.peek())) {
                        if (this.warnedInstructions.indexOf(token) < 0) {
                            this.warnedInstructions.push(token);
                            if (this.infoOut) this.infoOut("<code>|M</code> for maximum of scalars is deprecated.  Use <code>T</code> instead.");
                        }
                        if (this.totalSize() < 2) break;
                        let top = this.pop(), next = this.pop();
                        this.push(compare(next, top) > 0 ? next : top);
                    }
                    else if (isArray(this.peek())) {
                        let arr = this.popArray(), result: StaxValue = Number.NEGATIVE_INFINITY;
                        if (arr.length === 0) this.push(result);
                        else if (arr instanceof IntRange) this.push(isInt(arr.end) ? arr.end : Number.POSITIVE_INFINITY);
                        else {
                            for (let e of arr) if (compare(e, result) > 0) result = e;
                            this.push(result);
                        }
                    }
                    break;
                }
                case '|n':
                    if (isInt(this.peek())) { // exponents of sequential primes in factorization
                        let target = int.abs(this.popInt()), result: StaxValue[] = [];
                        let singleRemaining = false, zeroes = 0;
                        if (target.valueOf() > 1) for (let p of allPrimes()) {
                            if (singleRemaining) {
                                if (int.eq(p, target)) {
                                    result = result.concat(Array(zeroes).fill(zero));
                                    result.push(one);
                                    break;
                                }
                                zeroes += 1;
                                continue;
                            }
                            let exp = zero;
                            while (int.eq(int.mod(target, p), zero)) {
                                target = int.div(target, p);
                                exp = int.add(exp, one);
                            }
                            result.push(exp);
                            if (target.valueOf() <= 1) break;
                            singleRemaining = int.cmp(int.mul(p, p), target) > 0;
                        }
                        this.push(result);
                    }
                    else if (isArray(this.peek())) {
                        // combine elements from a and b, removing common elements as many times as they mutually occur
                        let b = [...this.popArray()], a = this.popArray(), result: StaxValue[] = [];
                        for (let e of a) {
                            let found = false;
                            for (let i = 0; i < b.length; i++) {
                                if (areEqual(b[i], e)) {
                                    found = true;
                                    b.splice(i, 1);
                                    break;
                                }
                            }
                            if (!found) result.push(e);
                        }
                        this.push(result.concat(b));
                    }
                    break;
                case '|N':
                    if (isArray(this.peek())) { // next permutation in lexicographic order
                        let els = [...this.popArray()], result: StaxValue[] = [], i = els.length - 2;
                        for (; i >= 0 &&compare(els[i], els[i + 1]) >= 0; i--) ;
                        if (i < 0) {
                            this.push(els.reverse());
                            break;
                        }

                        result.push(...els.splice(0, i));
                        for (i = els.length - 1; compare(els[i], els[0]) <= 0; i--) ;
                        result.push(...els.splice(i, 1));
                        result.push(...els.sort(compare));
                        this.push(result);
                    }
                    else if (isInt(this.peek())) {
                        let b = this.popInt(), a = this.popInt();
                        this.push(int.nthRoot(a, b));
                    }
                    break;
                case '|o': { // get indices of elements when ordered
                    let a = this.popArray(), result: StaxValue[] = [], i = 0;
                    if (a instanceof IntRange) this.push(range(0, a.length));
                    else {
                        let loca = a;
                        let idxs = [...range(0, loca.length)].sort((x: StaxInt, y: StaxInt) => compare(loca[floatify(x)], loca[floatify(y)]));
                        for (let t of idxs) result[floatify(t as StaxInt)] = int.make(i++);
                        this.push(result);
                    }
                    break;
                }
                case '|p':
                    if (isInt(this.peek())) {
                        this.runMacro("|f%1="); // is prime?
                    }
                    else if (isArray(this.peek())) {
                        this.runMacro("cr1t+"); // palindromize
                    }
                    break;
                case '|P':
                    if (this.warnedInstructions.indexOf(token) < 0) {
                        this.warnedInstructions.push(token);
                        if (this.infoOut) this.infoOut("<code>|P</code> is deprecated.  Use <code>zP</code> instead.");
                    }
                    this.print('');
                    break;
                case '|q': {
                    let b = this.pop();
                    if (isInt(b)) {
                        this.push(int.floorSqrt(int.abs(b)));
                    }
                    else if (isNumber(b)) {
                        this.push(int.make(Math.floor(Math.sqrt(Math.abs(Number(b.valueOf()))))));
                    }
                    else if (isArray(b)) {
                        let pattern = new RegExp(A2S(b), "g"), text = A2S(this.popArray());
                        let match: RegExpExecArray | null;
                        let result: StaxValue[] = [];
                        while (match = pattern.exec(text)) result.push(int.make(match.index));
                        this.push(result);
                    }
                    break;
                }
                case '|Q': {
                    let b = this.pop();
                    if (isNumber(b)) {
                        this.push(Math.sqrt(Math.abs(Number(b.valueOf()))));
                    }
                    else if (isArray(b)) {
                        let a = this.popArray();
                        let match = RegExp(`^(?:${ A2S(b) })$`).exec(A2S(a));
                        this.push(match ? one : zero);
                    }
                    break;
                }
                case '|r': {
                    // explicit range
                    let end = this.pop(), start = this.pop();
                    if (isArray(end)) end = int.make(end.length);
                    if (isArray(start)) start = int.make(start.length);
                    if (isInt(start) && isInt(end)) this.push(range(start, end));
                    else fail("bad types for |r");
                    break;
                }
                case '|R': {
                    if (isInt(this.peek())) { // start-end-stride with range
                        let stride = this.popInt(), end = this.popInt(), start = this.popInt();
                        let result = range(0, int.sub(end, start))
                            .map((n: StaxInt) => int.add(int.mul(n, stride), start))
                            .filter(n => int.cmp(n, end) < 0);
                        this.push(result);
                    }
                    else if (isArray(this.peek())) { // RLE
                        this.push(runLength(this.popArray()));
                    }
                    break;
                }
                case '|s': {
                    let search = A2S(this.popArray()), text = A2S(this.popArray());
                    this.push(text.split(new RegExp(search)).filter(p => typeof p === "string").map(S2A));
                    break;
                }
                case '|S': { // surround
                    let b = this.pop(), a = this.pop();
                    if (!isArray(a)) a = [a];
                    if (!isArray(b)) b = [b];
                    this.push([...b, ...a, ...b]);
                    break;
                }
                case '|t':
                    this.doTranslate();
                    break;
                case '|T':
                    this.doPermutations();
                    break;
                case '|u':
                    this.push(S2A(unEval(this.popArray())));
                    break;
                case '|V':
                    this.push([]); // command line args
                    break;
                case '|w':
                    this.doTrimElementsFromStart();
                    break;
                case '|W':
                    this.doTrimElementsFromEnd();
                    break;
                case '|x':
                    this.push(this.x = int.sub(isInt(this.x) ? this.x : zero, one));
                    break;
                case '|X':
                    this.push(this.x = int.add(isInt(this.x) ? this.x : zero, one));
                    break;
                case '|y':
                    this.push(this.y = int.sub(isInt(this.y) ? this.y : zero, one));
                    break;
                case '|Y':
                    this.push(this.y = int.add(isInt(this.y) ? this.y : zero, one));
                    break;
                case '|z':
                    this.runMacro("ss ~; '0* s 2l$ ,)"); // zero fill
                    break;
                case '|Z':
                    if (isArray(this.peek())) { // rectangularize using empty array
                        let arr = [...this.popArray()], maxlen = 0;
                        for (let i = 0; i < arr.length; i ++) {
                            if (!isArray(arr[i])) arr[i] = stringFormat(arr[i]);
                            maxlen = Math.max(maxlen, (arr[i] as StaxArray).length);
                        }
                        let result: StaxValue[] = [];
                        for (let i = 0; i < arr.length; i++) {
                            let orig = arr[i] as StaxArray;
                            let line = new Array(maxlen - orig.length).fill([]);
                            line.unshift(...orig);
                            result.push(line);
                        }
                        this.push(result);
                    }
                    break;
                default:
                    throw new Error(`unknown token ${token}`);
            }

            ip += token.length;
        }

        yield new ExecutionState(ip);
    }

    private runMacro(macro: string) {
        for (let _ of this.runSteps(parseProgram(macro))) { }
    }

    private doPlus() {
        if (this.totalSize() < 2) {
            this.push(this.pop());
            return;
        }
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a + b;
            else if (a instanceof Rational && b instanceof Rational) result = a.add(b);
            else if (isInt(a) && isInt(b)) result = int.add(a, b);
            else throw "weird types or something; can't add?"
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) this.push([...a, ...b]);
        else if (isArray(a)) this.push([...a, b]);
        else if (isArray(b)) this.push([a, ...b]);
    }

    private doMinus() {
        let b = this.pop(), a = this.pop();
        if (isArray(a) && isArray(b)) {
            if (b.length * a.length === 0) this.push(a);
            else {
                const bSet = new StaxSet(b);
                let result = a.filter(a_ => !bSet.has(a_) && (!isArray(a_) || a_.length == 0 || !bSet.has(materialize(a_.slice(0, 1))[0]))); //omg wtf
                this.push(result);
            }
        }
        else if (isArray(a)) {
            this.push(a.filter(a_ => !areEqual(a_, b)));
        }
        else if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a - b;
            else if (a instanceof Rational && b instanceof Rational) result = a.subtract(b);
            else if (isInt(a) && isInt(b)) result = int.sub(a, b);
            else throw "weird types or something; can't subtract?"
            this.push(result);
        }
        else throw new Error('bad types for -');
    }

    private *doStar() {
        if (this.totalSize() < 2) {
            this.push(this.pop());
            return;
        }
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a * b;
            else if (a instanceof Rational && b instanceof Rational) result = a.multiply(b);
            else if (isInt(a) && isInt(b)) result = int.mul(a, b);
            else throw "weird types or something; can't multiply?"
            this.push(result);
        }
        else if (isInt(a) && isArray(b)) {
            let count = a.valueOf();
            if (count < 0) [b, count] = [[...b].reverse(), -count];
            if (count === 1) this.push(b);
            else {
                let result: StaxValue[] = [];
                for (let i = 0; i < count; i++) result = [...result, ...b];
                this.push(result);
            }
        }
        else if (isArray(a) && isInt(b)) {
            let count = b.valueOf();
            if (count < 0) [a, count] = [[...a].reverse(), -count];
            if (count === 1) this.push(a);
            else {
                let result: StaxValue[] = [];
                for (let i = 0; i < count; i++) result = [...result, ...a];
                this.push(result);
            }
        }
        else if (isArray(a) && isArray(b)) {
            if (isMatrix(a) && isMatrix(b)) {
                this.push(a, b); // matrix multiplication
                this.runMacro("M~{;{n|\\{:*m|+msdm,d");
            }
            else {
                let result: StaxValue[] = [];
                a.forEach((e, i) => {
                    if (i) result = result.concat(b);
                    if (isArray(e)) result = result.concat(e);
                    else result = result.concat(S2A(e.toString()));
                });
                this.push(result);
            }
        }
        else if (a instanceof Block && isInt(b)) {
            let block = a, times = b.valueOf();
            this.pushStackFrame();
            for (this.index = zero; this.index.valueOf() < times; this.index = int.add(this.index, one)) {
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break;
                    yield s;
                }
            }
            this.popStackFrame();
        }
        else if (isInt(a) && b instanceof Block) {
            let block = b, times = a.valueOf();
            this.pushStackFrame();
            for (this.index = zero; this.index.valueOf() < times; this.index = int.add(this.index, one)) {
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break;
                    yield s;
                }
            }
            this.popStackFrame();
        }
    }

    private *doSlash() {
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isInt(b) && int.eq(b, zero) || b instanceof Rational && int.eq(b.numerator, zero)) {
                [a, b] = [floatify(a), 0];
            }
            if (isFloat(a) && isFloat(b)) result = a / b;
            else if (a instanceof Rational && b instanceof Rational) result = a.divide(b);
            else if (isInt(a) && isInt(b)) {
                if (b.valueOf() < 0) {
                    [a, b] = [int.negate(a), int.negate(b)];
                }
                if (a.valueOf() < 0) a = int.add(int.sub(a, b), one);
                result = int.div(a, b);
            }
            else throw "weird types or something; can't divide?"
            this.push(result);
        }
        else if (isArray(a) && isInt(b)) {
            let result = [];
            if (b.valueOf() < 0) {
                a = [...a].reverse();
                b = int.negate(b);
            }
            let _b = int.floatify(b);
            if (b.valueOf() > 0) {
                for (let i = 0; i < a.length; i += _b) {
                    result.push(a.slice(i, i + _b));
                }
            }
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) {
            let strings = A2S(a).split(A2S(b));
            this.push(strings.map(s => S2A(s)));
        }
        else if (isArray(a) && b instanceof Block) {
            let result: StaxValue[] = [], currentPart: StaxValue[] | null = null, last = null, cancelled = false;

            this.pushStackFrame();
            for (let e of a) {
                this.push(this._ = e);
                for (let s of this.runSteps(b)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                if (!cancelled) {
                    let current = this.pop();
                    if (last == null || !areEqual(current, last)) {
                        if (currentPart != null) result.push(currentPart);
                        currentPart = [];
                    }
                    currentPart!.push(e);
                    last = current;
                }
                this.index = int.add(this.index, one);
            }
            if (currentPart) result.push(currentPart);
            this.popStackFrame();
            this.push(result);
        }
        else throw new Error("bad types for /");
    }

    private doZipRepeat() {
        let b = this.pop(), a = this.pop();

        if (!isArray(a) && !isArray(b)) {
            this.push([a, b]);
            return;
        }

        if (!isArray(a) && isArray(b)) a = b.length ? [a] : [];
        else if (isArray(a) && !isArray(b)) b = a.length ? [b] : [];

        a = materialize(a as StaxArray);
        b = materialize(b as StaxArray);

        let result: StaxValue[] = [], size = Math.max(a.length, b.length);
        if (a.length && b.length) for (let i = 0 ; i < size; i++) {
            result.push([ a[i % a.length], b[i % b.length] ]);
        }
        this.push(result);
    }

    private doPercent() {
        let b = this.pop();
        if (isArray(b)) {
            this.push(isFinite(b.length) ? int.make(b.length) : b.length);
            return;
        }
        let a = this.pop();
        if (isArray(a) && isInt(b)) {
            let b_ = int.floatify(b);
            if (b_ < -a.length) b_ = -a.length;
            if (b_ > a.length) b_ = a.length;
            if (b_ < 0) b_ += a.length;
            this.push(a.slice(0, b_), a.slice(b_));
        }
        else if (isNumber(a) && isNumber(b)) {
            [a, b] = widenNumbers(a, b);
            let result: StaxNumber;
            if (typeof a === "number" && typeof b === "number") {
                if (b === 0) result = a;
                else {
                    result = a % b;
                    if (result < 0) result += Math.abs(b);
                }
            }
            else if (isInt(a) && isInt(b)) {
                if (int.eq(b, zero)) result = a;
                else {
                    result = int.mod(a, b);
                    if (result.valueOf() < 0) result = int.add(result, b);
                }
            }
            else if (a instanceof Rational && b instanceof Rational) {
                result = b.equals(ratZero) ? a : a.mod(b);
            }
            else throw new Error("bad types for %");
            this.push(result);
        }
        else throw new Error("bad types for %");
    }

    private doExplode() {
        let arg = this.pop();

        if (isArray(arg)) {
            for (let e of arg) this.push(e); // explode array; push items to stack individually
        }
        else if (arg instanceof Rational) {
            this.push(arg.numerator, arg.denominator);
        }
        else if (isInt(arg)) {
            let result = [];  // array of decimal digits
            for (let c of int.abs(arg).toString()) result.push(int.make(c));
            this.push(result);
        }
    }

    private doPowersetOrXor() {
        let b = this.pop();
        if (isInt(b)) {
            if (this.totalSize() === 0) this.push(b);
            else if (isInt(this.peek())) {
                this.push(int.bitxor(b, this.popInt()));
            }
            else {
                let len = int.floatify(b), arr = materialize(this.popArray()), result: StaxValue[] = [];
                let idxs = range(0, b).map(i => int.floatify((i as StaxInt)));
                while (len <= arr.length) {
                    result.push(idxs.map(idx => arr[idx]));
                    let i: number;
                    for (i = len - 1; i >= 0 && idxs[i] == i + (arr.length - len); i--) ;
                    if (i < 0) break;
                    idxs[i] += 1;
                    for (i++; i < len; i++) idxs[i] = idxs[i - 1] + 1;
                }
                this.push(result);
            }
        }
        else if (isArray(b)) {
            let result: StaxValue[] = [];
            for (let e of [...b].reverse()) {
                result = result.concat(result.map(r => [e, ...r as StaxArray]));
                result.push([e]);
            }
            this.push(result.reverse());
        }
    }

    private doPermutations() {
        let targetSize = isInt(this.peek()) ? int.floatify(this.popInt()) : Number.MAX_SAFE_INTEGER;
        let els = this.popArray(), result: StaxValue[] = [];
        targetSize = Math.min(els.length, targetSize);

        // factoradic permutation decoder
        let totalPerms = one, stride = one;
        for (let i = 1; i <= els.length; i++) totalPerms = int.mul(totalPerms, int.make(i));
        for (let i = 1; i <= els.length - targetSize; i++) stride = int.mul(stride, int.make(i));
        let idxs = els.map(_ => 0);
        for (let pi = zero; int.cmp(pi, totalPerms) < 0; pi = int.add(pi, stride)) {
            let n = pi;
            for (let i = 1; i <= els.length; n = int.div(n, int.make(i++))) {
                idxs[els.length - i] = int.floatify(int.mod(n, int.make(i)));
            }
            let dupe = [...els];
            result.push(idxs.slice(0, targetSize).map(i => {
                try { return dupe[i]; }
                finally { dupe.splice(i, 1); }
            }));
        }

        this.push(result);
    }

    private doAt() {
        let top = this.pop();

        if (top instanceof Rational) {
            this.push(top.floor());
            return;
        }
        if (isFloat(top)) {
            this.push(int.make(Math.floor(top)));
            return;
        }

        let list = this.pop();

        function readAt(arr: StaxArray, idx: StaxNumber) {
            if (arr.length === 0) throw new EarlyTerminate("Can't index into empty array");
            idx = Math.floor(Number(idx.valueOf())) % arr.length;
            if (idx < 0) idx += arr.length;
            return materialize(arr.slice(idx,idx + 1))[0];
        }

        // read at index
        if (isInt(list) && isArray(top)) [list, top] = [top, list];
        if (isArray(list) && isArray(top)) {
            let result = [];
            for (let idx of top) {
                if (isNumber(idx)) result.push(readAt(list, idx));
                else fail("couldn't index at non-number");
            }
            this.push(result);
            return;
        }
        else if (isInt(top)) {
            let indices = [ top ];

            this.push(list);
            while (this.totalSize() > 0 && isInt(this.peek())) indices.unshift(this.popInt());
            list = this.popArray();

            for(let idx of indices) list = readAt(list as StaxArray, idx.valueOf());
            this.push(list);
            return;
        }
        fail("bad type for @");
    }

    private *doAssignIndex() {
        let element = this.pop(), indexes = this.pop();

        if (isInt(indexes)) {
            let buildindexes: StaxValue[] = [indexes];
            if (isInt(this.peek())) {
                while (isInt(this.peek())) buildindexes.unshift(this.popInt());
                buildindexes = [buildindexes];
            }
            indexes = buildindexes;
        }
        if (!isArray(indexes)) throw new Error("unknown index type for assign-index");

        const self = this;
        function *doFinalAssign (flatArr: StaxValue[], index: number) {
            if (index >= flatArr.length) {
                flatArr.push(...Array(index + 1 - flatArr.length).fill(zero));
            }

            if (element instanceof Block) {
                self.pushStackFrame();
                self.index = int.make(index);
                self.push(self._ = flatArr[index]);
                let cancelled = false;
                for (let s of self.runSteps(element)) {
                    yield s;
                    cancelled = s.cancel;
                }
                if (!cancelled) flatArr[index] = self.pop();
                self.popStackFrame();
            }
            else flatArr[index] = element;
        }

        let list = this.popArray(), result: StaxValue[] = [...list];
        for (let arg of indexes) {
            if (isArray(arg)) {
                // path to deep target element
                let idxPath = materialize(arg), target = result, idx: number;
                for (let i = 0; i < idxPath.length - 1; i++) {
                    idx = floatify(idxPath[i] as StaxInt);
                    while (target.length <= idx) target.push([]);
                    if (isArray(target[idx])) {
                        target = target[idx] = [...(target[idx] as StaxArray)];
                    }
                    else target = target[idx] = [ target[idx] ];
                }
                idx = floatify(last(idxPath) as StaxInt);
                for (let s of doFinalAssign(target, idx)) yield s;
            }
            else if (isInt(arg)) {
                // multiple top indices to assign
                let index = int.floatify(arg);
                if (index < 0) {
                    index += result.length;
                    if (index < 0) {
                        result.unshift(...Array(-index).fill(zero));
                        index = 0;
                    }
                }
                for (let s of doFinalAssign(result, index)) yield s;
            }
        }
        this.push(result);
    }

    private doRemoveOrInsert() {
        let b = this.pop(), a = this.pop();
        if (isArray(a)) { // remove at index
            if (isInt(b)) {
                let b_ = int.floatify(b), result = [...a];
                if (b_ < 0) b_ += result.length;
                if (b_ >= 0 && b_ < result.length) result.splice(b_, 1);
                this.push(result);
            }
            else fail("need integer index for remove");
        }
        else if (isInt(a)) { // insert element at index
            let arr = this.popArray(), result = [...arr], a_ = int.floatify(a);
            if (a_ < 0) a_ += result.length;
            if (a_ < 0) result.unshift(b, ...new Array(-a_).fill(zero));
            else if (a_ > result.length) result.push(...new Array(a_ - result.length).fill(zero), b);
            else result.splice(a_, 0, b);
            this.push(result);
        }
    }

    private doBaseConvert(stringRepresentation = true) {
        let base = this.popInt(), number = this.pop();

        if (isInt(number)) {
            let result = [], negative = number.valueOf() < 0;
            number = int.abs(number);

            if (base.valueOf() == 1) result = new Array(number).fill(zero);
            else do {
                let digit = int.mod(number, base);
                if (stringRepresentation) {
                    let d = "0123456789abcdefghijklmnopqrstuvwxyz".charCodeAt(int.floatify(digit));
                    result.unshift(int.make(d));
                }
                else { // digit mode
                    result.unshift(digit);
                }
                number = int.div(number, base);
            } while (number.valueOf() > 0);
            if (negative && stringRepresentation) result.unshift(int.make("-".charCodeAt(0)));

            this.push(result);
        }
        else if (isArray(number)) {
            let result = zero;
            if (stringRepresentation) {
                let s = A2S(number).toLowerCase();
                let negative = s.startsWith("-");
                s = s.replace(/^-/, "");
                for (let c of s) {
                    let digit = "0123456789abcdefghijklmnopqrstuvwxyz".indexOf(c);
                    if (digit < 0) digit = c.charCodeAt(0);
                    result = int.add(int.mul(result, base), int.make(digit));
                }
                if (negative) result = int.negate(result);
            }
            else {
                for (let d of number) {
                    if (!isInt(d)) throw new Error("digits list contains a non-integer");
                    result = int.add(int.mul(result, base), d);
                }
            }
            this.push(result);
        }
        else fail("bad types for base conversion");
    }

    private doGCD() {
        let b = this.pop();
        if (isArray(b)) {
            let result = zero;
            for (let e of b) result = int.gcd(result, e as StaxInt);
            this.push(result);
            return;
        }

        let a = this.pop();
        if (isInt(a) && isInt(b)) {
            let gcd = int.gcd(a, b);
            this.push(gcd);
            return;
        }

        fail("bad types for gcd");
    }

    private *doPadLeft() {
        let b = this.pop(), a = this.pop();

        if (isArray(b) && isInt(a)) [a, b] = [b, a];
        if (isInt(a)) a = stringFormat(a);

        if (isArray(a) && isInt(b)) {
            let bval = int.floatify(b);
            if (bval < 0) bval += a.length;
            if (bval <= a.length) this.push(a.slice(a.length - bval));
            else this.push([...Array(bval - a.length).fill(zero), ...a]);
        }
        else if (isArray(a) && isArray(b)) {
            if (a.length >= b.length) this.push(a.slice(a.length - b.length));
            else this.push([...b.slice(0, b.length - a.length), ...a]);
        }
        else if (isArray(a) && b instanceof Block) {
            // partition where the block produces a truthy for the pair of values surrounding the boundary
            let result: StaxValue[] = [], current = [];
            a = materialize(a);
            if (a.length > 0) current.push(a[0]);

            this.pushStackFrame();
            for (let i = 1; i < a.length; i++) {
                this._ = new IteratorPair(a[i-1], a[i]);
                this.push(a[i-1], a[i]);

                let cancelled = false;
                for (let s of this.runSteps(b)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                if (!cancelled) {
                    if (isTruthy(this.pop())) {
                        result.push(current);
                        current = [];
                    }
                    current.push(a[i]);
                }
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();

            result.push(current);
            this.push(result);
        }
        else fail("bad types for padleft");
    }

    private *doPadRight() {
        let b = this.pop(), a = this.pop();

        if (isArray(b) && isInt(a)) [a, b] = [b, a];
        if (isInt(a)) a = stringFormat(a);

        if (isArray(a) && isInt(b)) {
            let bval = int.floatify(b);
            if (bval < 0) bval += a.length;
            if (bval <= a.length) this.push(a.slice(0, bval));
            else this.push([...a, ...Array(bval - a.length).fill(zero)]);
        }
        else if (isArray(a) && isArray(b)) {
            if (a.length >= b.length) this.push(a.slice(0, b.length));
            else this.push([...a, ...b.slice(a.length)]);
        }
        else if (isArray(a) && b instanceof Block) {
            let result = [], current: StaxValue[] | null = null;

            this.pushStackFrame();
            for (let e of a) {
                let cancelled = false;
                this.push(this._ = e);

                for (let s of this.runSteps(b)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                if (!cancelled) {
                    if (isTruthy(this.pop()) || current == null) {
                        if (current) result.push(current);
                        current = [];
                    }
                    current.push(e);
                }

                this.index = int.add(this.index, one);
            }
            this.popStackFrame();

            if (current) result.push(current);
            this.push(result);
        }
        else throw new Error("bad types for padright")
    }

    private doCenter() {
        let top = this.pop();
        if (isInt(top)) {
            let data = this.pop();
            if (isArray(data)) {
                let result = Array((int.floatify(top) - data.length) >> 1).fill(zero);
                result = result.concat(data);
                result = result.concat(Array(int.floatify(top) - result.length).fill(zero));
                this.push(result);
            }
            else if (isInt(data)) { // binomial coefficient
                let r = top, n = data, result = one;
                if (n.valueOf() < 0 || int.cmp(r, n) > 0) result = zero;
                for (let i = one; int.cmp(i, r) <= 0; i = int.add(i, one)) {
                    result = int.div(int.mul(result, int.add(int.sub(n, i), one)), i);
                }
                this.push(result);
            }
        }
        else if (isArray(top)) {
            let maxLen = 0, result = [];
            for (let line of top) maxLen = isArray(line) ? Math.max(maxLen, line.length) : fail('tried to center a non-array');
            for (let line of top) {
                if (!isArray(line)) throw new Error('tried to center a non-array');
                let newLine = [...line];
                newLine.unshift(...Array((maxLen - newLine.length) >> 1).fill(zero));
                newLine.push(...Array(maxLen - newLine.length).fill(zero));
                result.push(newLine);
            }
            this.push(result);
        }
    }

    private doTranslate() {
        let translation = this.pop(), input = this.pop();
        if (isInt(input)) input = [input];

        if (isArray(input) && isArray(translation)) {
            let result = [], map = new StaxMap;
            translation = materialize(translation);
            for (let i = 0; i < translation.length; i += 2) {
                map.set(translation[i], translation[i + 1]);
            }
            for (let e of input) {
                let mapped = map.get(e);
                result.push(mapped == null ? e : mapped);
            }
            this.push(result);
        }
        else fail("bad types for translate");
    }

    private doOverlappingBatch() {
        let b = this.popInt(), a = this.popArray(), result = [];
        let bv = int.floatify(b), end = a.length - bv + 1;
        for (let i = 0; i < end; i++) result.push(a.slice(i, i + bv));
        this.push(result);
    }

    private doTrimElementsFromStart() {
        let b = this.pop(), a = materialize(this.popArray()), i = 0;

        for (; i < a.length; i++) {
            if (isArray(b)) {
                if (!b.some(e => areEqual(e, a[i]))) break;
            }
            else if (!areEqual(a[i], b)) break;
        }

        let result = a.slice(i);
        this.push(result);
    }

    private doTrimElementsFromEnd() {
        let b = this.pop(), a = materialize(this.popArray()), i = a.length - 1;

        for (; i >= 0; i--) {
            if (isArray(b)) {
                if (!b.some(e => areEqual(e, a[i]))) break;
            }
            else if (!areEqual(a[i], b)) break;
        }

        let result = a.slice(0, i + 1);
        this.push(result);
    }

    private doPartition() {
        let n = int.floatify(this.popInt()), arg = this.pop();
        let total = isArray(arg) ? arg.length : floatify(arg as StaxNumber);

        let result: StaxValue[] = [];
        if (n > total) {
            this.push(result);
            return;
        }

        let partition = new Array(n - 1).fill(1);
        partition.push(total - n + 1);

        while (true) {
            if (isArray(arg)) {
                let added = 0, listPartition = [];
                for (let psize of partition) {
                    listPartition.push(arg.slice(added, added += psize));
                }
                result.push(listPartition);
            }
            else result.push(partition.map(v => int.make(v)));

            let i: number;
            for (i = n - 1; i >= 0 && partition[i] === 1; i --) ;
            if (i <= 0) break;

            ++partition[i - 1];
            --partition[i];
            [partition[i], partition[n - 1]] = [partition[n - 1], partition[i]];
        }
        this.push(result);
    }

    private doMultiAntiMode() {
        let arr = this.popArray(), result: StaxValue[] = [];
        if (arr instanceof IntRange) this.push(arr);
        else {
            if (arr.length > 0) {
                let multi = new Multiset(arr), keys = multi.keys();
                let min = Math.min(...keys.map(k => multi.get(k)));
                result = keys.filter(k => multi.get(k) === min);
                result.sort(compare);
            }
            this.push(result);
        }
    }

    private doRotate(direction: number) {
        let distance = this.pop(), arr: ReadonlyArray<StaxValue>;

        if (isArray(distance)) {
            arr = materialize(distance);
            distance = one;
        }
        else {
            let popped = this.pop();
            arr = isArray(popped) ? materialize(popped) : fail("bad types for rotate");
        }

        if (!isInt(distance)) throw new Error("bad rotation distance");

        if (arr.length) {
            distance = int.mod(distance, int.make(arr.length));
            if (distance.valueOf() < 0) distance = int.add(distance, int.make(arr.length));
            let cutpoint = direction < 0 ? int.floatify(distance) : (arr.length - int.floatify(distance));
            let result = arr.slice(cutpoint).concat(arr.slice(0, cutpoint));
            this.push(result);
        }
        else this.push(arr);
    }

    private doLastIndexOf() {
        let target = this.popArray(), arr = this.popArray();
        for (let i = arr.length - target.length; i >= 0; i--) {
            if (areEqual(target, arr.slice(i, i + target.length))) {
                this.push(int.make(i));
                return;
            }
        }
        this.push(minusOne);
    }

    private *doIndexOfOrAnd() {
        if (this.totalSize() === 1) {
            this.push(this.pop());
            return;
        }
        let target = this.pop(), arr = this.pop();
        if (isInt(target) && isInt(arr)) {
            this.push(int.bitand(target, arr));
            return;
        }
        if (!isArray(arr)) [arr, target] = [target, arr];
        if (!isArray(arr)) throw new Error("bad types for index-of");

        let i = -1;
        for (let el of arr) {
            ++i;
            if (isArray(target)) {
                if (i + target.length > arr.length) {
                    this.push(minusOne);
                    return;
                }
                if (areEqual(target, arr.slice(i, i + target.length))) {
                    this.push(int.make(i));
                    return;
                }
            }
            else if (target instanceof Block) {
                this.pushStackFrame();
                this.push(this._ = el);
                this.index = int.make(i);
                let cancelled = false;
                for (let s of this.runSteps(target)) {
                    yield s;
                    if (cancelled = s.cancel) break;
                }
                if (!cancelled && isTruthy(this.pop())) {
                    this.push(int.make(i));
                    this.popStackFrame();
                    return;
                }
                this.popStackFrame();
            }
            else if (areEqual(target, el)) {
                this.push(int.make(i));
                return;
            }
        }
        this.push(minusOne); // all else failed
    }

    private *doFindFirst(reverse = false) {
        let pred = this.pop(), arr = this.pop(), cancelled = false;
        if (!(pred instanceof Block) || !isArray(arr)) throw new Error("bad types for find-first");

        if (reverse) arr = [...arr].reverse();

        this.pushStackFrame();
        for (let e of arr) {
            this.push(this._ = e);
            for (let s of this.runSteps(pred)) {
                if (cancelled = s.cancel) break;
                yield s;
            }
            if (!cancelled && isTruthy(this.pop())) {
                this.push(e);
                break;
            }
            this.index = int.add(this.index, one);
        }
        this.popStackFrame();
    }

    private *doTransposeOrMaybe() {
        let top = this.pop();
        if (top instanceof Block) {
            if (isTruthy(this.pop())) {
                for (let s of this.runSteps(top)) {
                    if (s.cancel) return;
                    yield s;
                }
            }
            return;
        }
        else if (isInt(top)) { // split array into number of equalish-sized chunks
            if (this.totalSize() === 0) {
                this.push(top);
            }
            if (isInt(this.peek())) {
                this.push(int.bitor(top, this.popInt()));
            }
            else {
                let chunks = int.floatify(top), consumed = 0, arr = this.popArray(), result: StaxValue[] = [];

                for (; chunks > 0; chunks--) {
                    let toTake = Math.ceil((arr.length - consumed) / chunks);
                    result.push(arr.slice(consumed, consumed + toTake));
                    consumed += toTake;
                }

                this.push(result);
            }
        }
        else if (isArray(top)) {
            top = materialize(top);
            if (top.length > 0 && !isArray(top[0])) top = [top];
            const copied = top.map((line) => [...line as StaxValue[]]); // prevent mutations
            let result: StaxValue[] = [];
            let maxlen = Math.max(...copied.map(e => (e as StaxArray).length));

            for (let line of copied) {
                line.push(...Array(maxlen - line.length).fill(zero));
            }

            for (let i = 0; i < maxlen; i++) {
                let column: StaxValue[] = [];
                for (let row of copied) column.push(row[i]);
                result.push(column);
            }

            this.push(result);
        }
        else fail("bad types for transpose/maybe");
    }

    private *doOrder() {
        let top = this.pop();
        if (isArray(top)) {
            this.push(top.map(e => e).sort(compare));
            return;
        }
        if (top instanceof Block) {
            let arr = this.pop(), i = 0;
            if (!isArray(arr)) throw new Error("expected array for order");
            let combined: {val: StaxValue, key: StaxValue, idx: number}[] = [];

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(top)) yield s;
                combined.push({val: e, key: this.pop(), idx: i++});
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();

            let result = combined
                .sort((a, b) => compare(a.key, b.key) || a.idx - b.idx)
                .map(t => t.val);
            this.push(result);
        }
        else throw new Error("bad types for order");
    }

    private *doExtremaBy(direction: number) {
        let project = this.pop(), arr = this.pop(), result: StaxValue[] = [], extreme: StaxValue | null = null;
        if (!(project instanceof Block) || !isArray(arr)) throw new Error("bad types for extrema");

        if (arr.length === 0) {
            this.push(arr);
            return;
        }

        this.pushStackFrame();
        for (let e of arr) {
            this.push(this._ = e);

            let cancelled = false
            for (let s of this.runSteps(project)) {
                if (cancelled = s.cancel) break;
                yield s;
            }

            if (!cancelled) {
                let projected = this.pop();
                if (extreme == null || compare(projected, extreme) * direction > 0) {
                    extreme = projected;
                    result.splice(0);
                }
                if (areEqual(projected, extreme)) result.push(e);
            }

            this.index = int.add(this.index, one);
        }
        this.popStackFrame();

        this.push(result);
    }

    private *doFindIndexAll() {
        let target = this.pop(), arr = this.pop();
        if (!isArray(arr)) throw new Error("bad types for find index all");

        if (isArray(target)) {
            let text = A2S(arr), search = A2S(target), result = [], lastFound = -1;
            while ((lastFound = text.indexOf(search, lastFound + 1)) >= 0) {
                result.push(int.make(lastFound));
            }
            this.push(result);
        }
        else if (target instanceof Block) {
            this.pushStackFrame();
            let result = [];
            for (let el of arr) {
                this.push(this._ = el);
                for (let s of this.runSteps(target)) yield s;
                if (isTruthy(this.pop())) result.push(this.index);
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();
            this.push(result);
        }
        else {
            let result = [], i = 0;
            for (let el of arr) {
                if (areEqual(el, target)) result.push(int.make(i));
                i++;
            }
            this.push(result);
        }
    }

    private *doRegexReplace() {
        let replace = this.pop(), search = this.pop(), text = this.pop();
        if (!isArray(text) || !isArray(search)) throw new Error("bad types for replace");
        let ts = A2S(text);

        if (isArray(replace)) {
            let ss = RegExp(A2S(search), "g");
            this.push(S2A(ts.replace(ss, A2S(replace))));
        }
        else if (replace instanceof Block) {
            let ss = RegExp(A2S(search), "g");
            let result = "";
            let lastEnd = 0;
            let match: RegExpMatchArray | null;

            this.pushStackFrame();
            while (match = ss.exec(ts)) {
                result += ts.substring(lastEnd, match.index!);

                this.push(this._ = S2A(match[0]));
                for (let s of this.runSteps(replace)) yield s;
                const replaced = this.pop();
                result += A2S(isArray(replaced) ? replaced : stringFormat(replaced));
                lastEnd = match.index! + match[0].length;

                this.index = int.add(this.index, one);
                if (!match[0]) ss.lastIndex += 1;
            }
            result += ts.substr(lastEnd);

            this.popStackFrame();
            this.push(S2A(result));
        }
        else throw new Error("bad types for replace");
    }

    private *doFor(rest: Block) {
        if (this.peek() instanceof Block) {
            let block = this.pop() as Block, data = this.pop();
            if (isInt(data)) data = range(1, int.add(data, one));
            if (!isArray(data)) throw Error("block-for operates on ints and arrays, not this garbage. get out of here.");

            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();
        }
        else if (isArray(this.peek()) || isInt(this.peek())) {
            let data = this.pop() as StaxArray | StaxInt, arr = isArray(data) ? data : range(one, int.add(data, one));

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();
        }
        else throw new Error("bad types in for");
    }

    private *doUnconditionalWhile(rest: Block) {
        let cancelled = false;
        let body: (Block | string) = (this.totalSize() && this.peek() instanceof Block) ? this.pop() as Block : rest;

        this.pushStackFrame();
        do {
            for (let s of this.runSteps(body)) {
                if (cancelled = s.cancel) break;
                yield s;
            }
            this.index = int.add(this.index, one);
        } while (!cancelled);
        this.popStackFrame();
    }

    private *doWhile(rest: Block) {
        let cancelled = false;
        let body: (Block | string) = (this.totalSize() && this.peek() instanceof Block) ? this.pop() as Block : rest;

        this.pushStackFrame();
        do {
            for (let s of this.runSteps(body)) {
                if (cancelled = s.cancel) break;
                yield s;
            }
            this.index = int.add(this.index, one);
        } while (!cancelled && isTruthy(this.pop()));
        this.popStackFrame();
    }

    private *doReduce(rest: Block) {
        let top = this.pop(), shorthand = !(top instanceof Block);
        let block: Block | string, arr: StaxValue;

        if (top instanceof Block) [block, arr] = [top, this.pop()];
        else [block, arr] = [rest, top];

        let reduceArr: StaxArray | null = null;
        if (isInt(arr)) reduceArr = range(one, int.add(arr, one));
        else if (isArray(arr)) reduceArr = [...arr];

        if (reduceArr) {
            if (reduceArr.length === 0) throw new Error("tried to reduce empty array");
            if (reduceArr.length === 1) {
                this.push(...reduceArr);
                return;
            }

            this.pushStackFrame();
            this.push(...reduceArr.slice(0, 1));
            reduceArr = reduceArr.slice(1);
            for (let e of reduceArr) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (s.cancel) {
                        this.popStackFrame();
                        return;
                    }
                    yield s;
                }
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();

            if (shorthand) this.print(this.pop());
        }
        else throw new Error("bad types for reduce");
    }

    private *doCrossMap(rest: Block) {
        let top = this.pop(),
            shorthand = !(top instanceof Block),
            map: Block | string = shorthand ? rest : top as Block;

        let inner = shorthand ? top : this.pop(), outer =  this.pop();
        if (isInt(inner)) inner = range(one, int.add(inner, one));
        if (isInt(outer)) outer = range(one, int.add(outer, one));
        if (!isArray(outer) || !isArray(inner)) throw new Error("need arrays or integers for crossmap");

        let result: StaxValue[] = [];
        this.pushStackFrame();
        for (let e of outer) {
            let row = [];
            this.pushStackFrame();
            for (let f of inner) {
                this.push(e);
                this.push(f);
                this._ = new IteratorPair(e, f);

                let cancelled = false;
                for (let s of this.runSteps(map)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                if (!cancelled) {
                    if (shorthand) this.print([this.pop()], false);
                    else row.push(this.pop());
                }
                this.index = int.add(this.index, one);
            }
            this.popStackFrame();
            this.index = int.add(this.index, one);
            if (shorthand) this.print("");
            else result.push(row);
        }
        this.popStackFrame();

        if (!shorthand) this.push(result);
    }

    private *doFilter(rest: Block) {
        if (this.peek() instanceof Block) {
            let block = this.pop() as Block, data = this.pop(), result: StaxValue[] = [], cancelled = false;
            let arr: Iterable<StaxValue>;
            if (isArray(data)) arr = data;
            else if (isInt(data)) arr = range(one, int.add(data, one));
            else throw Error("block-filter operates on ints and arrays, not this garbage. get out of here.");

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
                if (!cancelled && isTruthy(this.pop())) result.push(e);
            }
            this.popStackFrame();
            this.push(result);
        }
        else {
            let data = this.pop(), cancelled = false;
            if (data === Number.POSITIVE_INFINITY) data = new IntRange(one);
            let arr = isArray(data) ? data
                : isInt(data) ? range(1, int.add(data, one))
                : fail("bad type for shorthand filter data")

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
                if (!cancelled && isTruthy(this.pop())) this.print(e);
            }
            this.popStackFrame();
        }
    }

    private *doMap(rest: Block) {
        let top = this.pop();
        if (top === Number.POSITIVE_INFINITY) top = new IntRange(one);
        if (top instanceof Block) {
            let block = top, data = this.pop(), result: StaxValue[] = [], cancelled = false;
            if (isInt(data)) data = range(1, int.add(data, one));
            if (!isArray(data)) throw Error("block-map operates on ints and arrays, not this garbage. get out of here.");

            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
                if (!cancelled) result.push(this.pop());
            }
            this.popStackFrame();
            this.push(result);
        }
        else if (isArray(top) || isInt(top)) {
            let data = isArray(top) ? top : range(one, int.add(top, one)), cancelled = false;

            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = int.add(this.index, one);
                if (!cancelled) this.print(this.pop());
            }
            this.popStackFrame();
        }
        else throw new Error("bad types in map");
    }

    private *doCollect() { // reduce and collect
        let reduce = this.pop() as Block, arr = this.popArray();

        if (arr.length < 2) {
            this.push(arr);
            return;
        }

        let result = [...arr.slice(0, 1)];
        this.pushStackFrame();
        this.push(result[0]);
        for (let e of arr.slice(1)) {
            this.push(this._ = e);
            for (let s of this.runSteps(reduce)) yield s;
            result.push(this.peek());
            this.index = int.add(this.index, one);
        }
        this.popStackFrame();
        this.pop();
        this.push(result);
    }

    private *doGenerator(shorthand: boolean, spec: string, rest: Block) {
        const lowerSpec = spec.toLowerCase();
        const stopOnDupe = lowerSpec === 'u' || lowerSpec === 'l';
        const stopOnFilter = lowerSpec === 'f';
        const stopOnCancel = lowerSpec ==='c';
        const stopOnFixPoint = lowerSpec === 'i' || lowerSpec === 'p';
        const stopOnTargetVal = lowerSpec === 't';
        const scalarMode = lowerSpec === 's' || lowerSpec === 'e' || lowerSpec === 'p';
        const keepOnlyLoop = lowerSpec === 'l';
        let postPop = spec !== lowerSpec;

        let genBlock = rest;
        if (!shorthand) {
            let popped = this.pop();
            if (popped instanceof Block) {
                genBlock = popped;
            } else throw new Error("generator block isn't a block");
        }
        let filter: Block | null = null;
        let targetVal: StaxValue | null = null;
        let targetCount: number | null = null;

        if (this.peek() instanceof Block) filter = this.pop() as Block;
        else if (stopOnFilter) throw new Error("generator can't stop on filter failure when there is no filter");

        if (stopOnTargetVal) targetVal = this.pop();

        if (lowerSpec === 'n') targetCount = int.floatify(this.popInt());
        else if (lowerSpec === 'e') targetCount = int.floatify(this.popInt()) + 1;
        else if (lowerSpec === 's') targetCount = 1;
        else {
            let idx = "1234567890!@#$%^&*()".indexOf(spec);
            if (idx >= 0) {
                targetCount = idx % 10 + 1;
                postPop = idx >= 10;
            }
        }

        if (!stopOnDupe && !stopOnFilter && !stopOnCancel && !stopOnFixPoint && !stopOnTargetVal && targetCount == null) {
            throw new Error("no end condition for generator");
        }

        if (targetCount === 0) { // 0 elements requested ??
            this.push([]);
            return;
        }

        this.pushStackFrame();
        var result: StaxValue[] = [];

        let lastGenerated : StaxValue | null = null;
        let genComplete = false, cancelled = false;
        let emptyGenBlock = genBlock.isEmpty();
        while (targetCount == null || result.length < targetCount) {
            this._ = this.peek();

            if (this.index.valueOf() > 0 || postPop) {
                if (!emptyGenBlock) {
                    for (let s of this.runSteps(genBlock)) {
                        if (s.cancel && stopOnCancel) genComplete = cancelled = true;
                        if (s.cancel) {
                            cancelled = true;
                            break;
                        }
                        yield s;
                    }
                }
                else { // empty gen block, use ^
                    this.runMacro("^");
                }
            }

            if (!cancelled) {
                let generated = this.peek();
                let passed = true;
                if (filter) {
                    this._ = generated;
                    for (let s of this.runSteps(filter)) {
                        if (s.cancel && stopOnCancel) genComplete = cancelled = true;
                        if (s.cancel) {
                            cancelled = true;
                            break;
                        }
                        yield s;
                    }

                    if (!cancelled) {
                        passed = isTruthy(this.pop());
                        this.push(generated); // put the generated element back
                        if (stopOnFilter && !passed) break;
                    }
                }

                if (!cancelled) {
                    if (postPop) this.pop();
                    if (passed) {
                        // dupe
                        if (stopOnDupe && indexOf(result, generated) >= 0) {
                            while (keepOnlyLoop && !areEqual(result[0], generated)) result.shift();
                            break;
                        }
                        // successive equal values
                        if (stopOnFixPoint && lastGenerated != null && areEqual(generated, lastGenerated)) break;
                        result.push(generated);
                        // got to target val
                        if (stopOnTargetVal && targetVal != null && areEqual(generated, targetVal)) break;
                    }
                    lastGenerated = generated;
                }
            }
            if (genComplete) break;
            this.index = int.add(this.index, one);
        }
        if (!postPop && !genComplete) {
            // Remove left-over value from pre-peek mode
            // It's kept on stack between iterations, but iterations are over now
            this.pop();
        }

        this.popStackFrame();

        if (shorthand) {
            if (scalarMode) this.push(last(result)!);
            else for (let e of result) this.print(e);
        }
        else {
            if (scalarMode) this.push(last(result)!);
            else this.push(result);
        }
    }

    private doMacroAlias(alias: string) {
        let typeTree = macroTrees[alias] || fail(`macro not found for types in :${ alias }`);
        let resPopped: StaxValue[] = [];
        // follow type tree as far as necessary
        while (typeTree.hasChildren()) {
            resPopped.push(this.pop());
            let type = getTypeChar(last(resPopped)!);
            typeTree = typeTree.children![type] || fail(`macro not found for types in :${ alias }`);
        }
        // return inspected values to stack
        this.push(...resPopped.reverse());

        if (typeTree.deprecation && this.warnedInstructions.indexOf(':' + alias) < 0) {
            if (this.infoOut) this.infoOut(typeTree.deprecation);
            this.warnedInstructions.push(':' + alias);
        }

        // disable line modes
        this.runMacro(' ' + typeTree.code);
    }

    private doEvaluateStringToken(token: string) {
        let unescaped = "";
        let terminated = false;
        for (var i = 1; i < token.length; i++) {
            if (token[i] == "`") {
                switch(token[++i]) {
                    case "`":
                    case '"': unescaped += token[i]; break;
                    case '0': unescaped += "\0"; break;
                    case '1': unescaped += "\n"; break;
                    case '2': unescaped += "\t"; break;
                    case '3': unescaped += "\r"; break;
                    case '4': unescaped += "\v"; break;
                    default:
                        let instruction = token[i];
                        if (instruction === ":" || instruction === "|" || instruction === "V") instruction += token[++i];
                        this.runMacro(instruction);;
                        let popped = this.pop();
                        if (isArray(popped)) unescaped += A2S(popped);
                        else unescaped += popped.toString();
                }
            }
            else if (token[i] == '"') {
                terminated = true;
                ++i;
                if (token[i] === '!') { // uncram integer array
                    let uncrammed = uncram(unescaped);
                    this.push(uncrammed);
                    return;
                }
                if (token[i] === '%') { // uncram scalar integer
                    let uncrammed = uncramSingle(unescaped);
                    this.push(uncrammed);
                    return;
                }
                break;
            }
            else {
                unescaped += token[i];
            }
        }
        if (terminated) this.push(S2A(unescaped));
        else this.print(unescaped, false);
    }
}