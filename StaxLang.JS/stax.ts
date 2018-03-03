import { StaxArray, StaxNumber, StaxValue, isArray, isFloat, isInt, isNumber, isTruthy, last, A2S, S2A, floatify, constants, widenNumbers, runLength, areEqual, indexOf, compare, stringFormat } from './types';
import { Block, Program, parseProgram } from './block';
import { unpack, unpackBytes, isPacked } from './packer';
import * as bigInt from 'big-integer';
import { Rational } from './rational';
import IteratorPair from './iteratorpair';
import Multiset from './multiset';
import { primeFactors, allPrimes } from './primehelper';
import { compress, decompress } from './huffmancompression';
import { macroTrees, getTypeChar } from './macrotree';
import { isBoolean, error } from 'util';
type BigInteger = bigInt.BigInteger;
const one = bigInt.one, zero = bigInt.zero, minusOne = bigInt.minusOne;

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

function range(start: number | BigInteger, end: number | BigInteger): StaxArray {
    let result: StaxArray = [];
    for (let e = start.valueOf(); e < end.valueOf(); e++) result.push(bigInt(e));
    return result;
}

class EarlyTerminate extends Error {
    constructor(msg: string) {
        super(msg);
    }
}

export class Runtime {
    private lineOut: (line: string) => void;
    private outBuffer = ""; // unterminated line output
    private program: Program;
    private mainStack: StaxArray = [];
    private inputStack: StaxArray = [];
    private producedOutput = false;

    private gotoCallDepth = 0;
    private callStackFrames: {_: StaxValue | IteratorPair, indexOuter: BigInteger}[] = [];
    private _: StaxValue | IteratorPair;
    private index = zero;
    private indexOuter = zero;
    private x: StaxValue = zero;
    private y: StaxValue;
    private implicitEval = false;

    constructor(output: (line: string) => void) {
        this.lineOut = output;
    }

    public getDebugState() {
        function format(arg: StaxValue | IteratorPair): string {
            if (arg instanceof IteratorPair) return `(${ format(arg.item1) }, ${ format(arg.item2) })`;
            if (isNumber(arg)) return arg.toString();
            if (arg instanceof Block) return `Block ${ arg.contents }`;
        
            if (arg.every(e => isInt(e) && (e.isZero() || e.eq(10) || e.greaterOrEquals(32) && e.lt(128)))) {
                return JSON.stringify(String.fromCharCode(...arg.map(e => (e as BigInteger).valueOf())));
            }
        
            return '[' + arg.map(format).join(", ") + ']';
        }
        
        return {
            implicitEval: this.implicitEval,
            x: format(this.x),
            y: format(this.y),
            index: this.index.valueOf(),
            _: format(this._),
            main: this.mainStack.map(format).reverse(),
            input: this.inputStack.map(format).reverse(),
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

    private popInt(): BigInteger {
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

    private print(val: StaxValue | string, newline = true) {
        this.producedOutput = true;

        if (isFloat(val)) {
            val = val.toPrecision(15).replace("Infinity", "∞");
            if (val.indexOf('.') >= 0) val = val.replace(/\.?0+$/, '');
        }
        if (isInt(val)) {
            val = val.toString();
        }
        if (val instanceof Block) val = `Block: ${val.contents}`;
        if (isArray(val)) val = A2S(val);
        if (val instanceof Rational) val = val.toString();

        if (newline) {
            (this.outBuffer + val).split("\n").forEach(l => this.lineOut(l));
            this.outBuffer = "";
        }
        else {
            this.outBuffer += val;
        }
    }

    private doEval(): boolean {
        let a = this.pop();
        if (!isArray(a)) throw new Error("tried to eval a non-array");
        let arg = A2S(a);
        let activeArrays: StaxArray[] = [];

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
                    let finishPos = arg.indexOf('"', i + 1);
                    if (finishPos < 0) return false;
                    newValue(S2A(arg.substring(i + 1, finishPos).replace("\\n", "\n")));
                    i = finishPos;
                    break;
                case '-':
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    let substring = arg.substr(i);
                    let match = substring.match(/^-?\d+\.\d+/);
                    if (match) {
                        newValue(parseFloat(match[0]));
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^(-?\d+)\/(-?\d+)/);
                    if (match) {
                        newValue(new Rational(bigInt(match[1]), bigInt(match[2])));
                        i += match[0].length - 1;
                        break;
                    }

                    match = substring.match(/^-?\d+/);
                    if (match) {
                        newValue(bigInt(match[0]));
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
        if (stdin.length === 1 && !program.startsWith('i')) {
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
                for (let s of this.runSteps(this.pop() as Block)) { }
            }
        }
        catch (e) {
            if (e instanceof EarlyTerminate) {} // proceed 
            else throw e;
        }

        if (this.outBuffer) this.print("");
        if (!this.producedOutput && this.totalSize()) this.print(this.pop());
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
                ip += 2;
                yield new ExecutionState(ip, false, true);
                continue;
            }
            yield new ExecutionState(ip);

            if (token instanceof Block) {
                this.push(token);
                ip += token.contents.length;
                continue;
            }
            else {
                if (!!token.match(/^\d+!/)) this.push(parseFloat(token.replace("!", ".")));
                else if (!!token[0].match(/^\d/)) this.push(bigInt(token));
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
                        if (isNumber(a)) [a, b] = [b, a];
                        this.push(a, b);
                        
                        if (isArray(this.peek())) this.runMacro("/%v");
                        else if (isNumber(this.peek())) this.runMacro("]|&%");
                        break;
                    }
                    case '_':
                        if (this._ instanceof IteratorPair) this.push(this._.item1, this._.item2);
                        else this.push(this._);
                        break;
                    case '!':
                        if (this.peek() instanceof Block) {
                            for (let s of this.runSteps(this.pop() as Block)) yield s;
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
                        else {
                            this.push(result);
                        }
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
                        this.push(bigInt[10])
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
                            for (this.index = zero; this.index.lt(n); this.index = this.index.add(one)) {
                                this._ = this.index.add(one);
                                for (let s of this.runSteps(getRest())) yield s;
                            }
                            this.popStackFrame();
                            return;
                        }
                        else if (isNumber(this.peek())) {
                            this.runMacro("1%"); // get fractional part
                        }
                        break;
                    case 'e':
                        if (isArray(this.peek())) {
                            if (!this.doEval()) throw new Error("eval failed");
                        }
                        else if (typeof this.peek() === "number") {
                            this.push(bigInt(Math.ceil(this.pop() as number)));
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
                            if (arr.length === 0) fail("empty array has no first element");
                            this.push(arr[0]);
                        }
                        else if (this.peek() instanceof Block) {
                            let pred = this.pop() as Block, result: StaxArray = [], arr = this.pop(), cancelled = false;
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
                                this.index = this.index.add(one);
                            }
                            this.popStackFrame();
                            this.push(result);
                        }
                        break;
                    case 'H':
                        if (isNumber(this.peek())) this.runMacro("2*");
                        else if (isArray(this.peek())) {
                            let arr = this.popArray();
                            if (arr.length === 0) fail("empty array has no last element");
                            this.push(last(arr)!);
                        }
                        else if (this.peek() instanceof Block) {
                            let pred = this.pop() as Block, result: StaxArray = [], arr = this.pop(), cancelled = false;
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
                                this.index = this.index.add(one);
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
                        for (let s of this.doIndexOf()) yield s;
                        break;
                    case 'j':
                        if (isArray(this.peek())) this.runMacro("' /");
                        else if (isInt(this.peek())) {
                            let digits = this.pop() as BigInteger, num = this.pop();
                            num = isNumber(num) && floatify(num) || fail("can't round a non-number");
                            this.push(S2A(num.toFixed(digits.valueOf())));
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
                            this.runMacro("' *");
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
                        if (a instanceof Rational) {
                            this.push([a.numerator, a.denominator]);
                        }
                        else if (isInt(a)) {
                            let result: StaxArray = [];
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
                        else if (isArray(top)) this.push(top.slice().reverse());
                        else if (top instanceof Rational) this.push(top.numerator);
                        break;
                    }
                    case 'R': 
                        if (isInt(this.peek())) this.push(range(1, this.popInt().add(one)));
                        else if (this.peek() instanceof Rational) this.push((this.pop() as Rational).denominator);
                        else for (let s of this.doRegexReplace()) yield s;
                        break;
                    case 's':
                        this.push(this.pop(), this.pop());
                        break;
                    case 'S':
                        this.doPowerset();
                        break;
                    case 't':
                        if (isArray(this.peek())) {
                            this.push(S2A(A2S(this.pop() as StaxArray).replace(/^\s+/, "")))
                        }
                        else if (isInt(this.peek())) {
                            this.runMacro("ss~ c%,-0|M)");
                        }
                        else if (this.peek() instanceof Block) {
                            let pred = this.pop() as Block, result = this.pop(), cancelled = false;
                            if (!isArray(result)) throw new Error("bad types for trim");
                            result = [...result];

                            this.pushStackFrame();
                            while (result.length) {
                                this.push(this._ = result[0]);
                                for (let s of this.runSteps(pred)) {
                                    if (cancelled = s.cancel) break;
                                    yield s;
                                }
                                if (cancelled || !isTruthy(this.pop())) break;
                                result.shift();
                                this.index = this.index.add(one);
                            }
                            this.popStackFrame();
                            this.push(result);
                        }
                        break;
                    case 'T':
                        if (isArray(this.peek())) {
                            this.push(S2A(A2S(this.pop() as StaxArray).replace(/\s+$/, "")))
                        }
                        else if (isInt(this.peek())) {
                            this.runMacro("ss~ c%,-0|M(");
                        }
                        else if (this.peek() instanceof Block) {
                            let pred = this.pop() as Block, result = this.pop(), cancelled = false;
                            if (!isArray(result)) throw new Error("bad types for trim");
                            result = [...result];

                            this.pushStackFrame();
                            while (result.length) {
                                this.push(this._ = last(result)!);
                                for (let s of this.runSteps(pred)) {
                                    if (cancelled = s.cancel) break;
                                    yield s;
                                }
                                if (cancelled || !isTruthy(this.pop())) break;
                                result.pop();
                                this.index = this.index.add(one);
                            }
                            this.popStackFrame();
                            this.push(result);
                        }
                        break;
                    case 'u': {
                            let arg = this.pop();
                            if (isArray(arg)) this.push(new Multiset(arg).keys());
                            else if (isInt(arg)) this.push(new Rational(one, arg));
                            else if (arg instanceof Rational) this.push(arg.invert());
                            else if (typeof arg === "number") this.push(1 / arg);
                            else fail("bad type for u");
                            break;
                        }
                    case 'U':
                        this.push(bigInt[-1]);
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
                        this.push(this.index.isEven() ? zero : one);
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
                            let result: StaxArray = [];
                            for (let e of a) {
                                if (indexOf(b, e) >= 0) result.push(e);
                            }
                            this.push(result);
                        }
                        else {
                            this.push(this.popInt().and(this.popInt()));
                        }
                        break;
                    case '|#':
                        if (isArray(this.peek())) { // number of occurrences in array
                            let b = this.popArray(), a = this.popArray();
                            this.push(bigInt(a.filter(e => areEqual(e, b)).length));
                        }
                        break;
                    case '||':
                        if (isInt(this.peek())) {
                            this.push(this.popInt().or(this.popInt()));
                        }
                        else if (isArray(this.peek())) {
                            // embed grid at coords
                            let payload = this.popArray(), col = this.popInt().valueOf(), row = this.popInt().valueOf();
                            let result = this.popArray().slice();

                            for (let r = 0; r < payload.length; r++) {
                                let payline = payload[r];
                                if (!isArray(payline)) payline = [payline];
                                while (result.length <= row + r) result.push([]);
                                if (!isArray(result[row + r])) result[row + r] = [result[row + r]];
                                let resultline = result[row + r] as StaxArray;

                                for (let c = 0; c < payline.length; c++) {
                                    while (resultline.length <= col + c) resultline.push(zero);
                                    resultline[col + c] = payline[c];
                                }
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
                    case '||':
                        if (isArray(this.peek())) {
                            this.push(this.popInt().or(this.popInt()));
                        }
                        else if (isArray(this.peek())) {
                            let payload = this.popArray(), col = this.popInt(), row = this.popInt();
                            let result = this.popArray().slice();

                            for (let r = 0; r < payload.length; r++) {
                                throw Error('nie');
                            }
                        }
                    case '|^':
                        if (isArray(this.peek())) {
                            this.runMacro("s b-~ s-, +"); // symmetric array difference
                        }
                        else if (isInt(this.peek())) { // tuples of specified size from array elements
                            let b = this.popInt(), a = this.pop();
                            if (isArray(a)) {
                                let result: StaxArray = [[]], els = a;
                                for (let i = 0; b.gt(i); i++) {
                                    result = ([] as StaxArray).concat(
                                        ...result.map((r: StaxArray) => els.map(e => [...r, e])))
                                }
                                this.push(result);
                            }
                            else if (isInt(a)) { // xor
                                this.push(a.xor(b));
                            }
                        }
                        break;
                    case '|<':
                        if (isInt(this.peek())) this.runMacro('|2*');
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
                        if (isInt(this.peek())) this.runMacro('|2/');
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
                            let arr = this.popArray(), result: StaxArray = [];
                            if (arr.length > 0) {
                                let multi = new Multiset(arr), keys = multi.keys();
                                let max = Math.max(...keys.map(k => multi.get(k)));
                                result = keys.filter(k => multi.get(k) === max);
                                result.sort(compare);
                            }
                            this.push(result);
                        }
                        break;
                    case '|!':
                        if (isInt(this.peek())) this.doPartition();
                        else if (isArray(this.peek())) this.doMultiAntiMode();
                        break;
                    case '|+':
                        this.runMacro('Z{+F');
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
                        if (isInt(b)) {
                            if (isInt(a)) {
                                if (b.isNegative()) this.push(new Rational(one, a.pow(b.negate())));
                                else this.push(a.pow(b));
                            }
                            else if (a instanceof Rational) {
                                if (b.isNegative()) {
                                    b = b.negate();
                                    a = a.invert();
                                }
                                let result = new Rational(one, one);
                                for (let i = 0; i < b.valueOf(); i++) result = result.multiply(a);
                                this.push(result);
                            }
                            else if (isArray(a)) {
                                let result = [];
                                for (let e of a) result.push(...Array(Math.abs(b.valueOf())).fill(e));
                                this.push(result);
                            }
                            else {
                                this.push(Math.pow(a.valueOf() as number, b.valueOf()));
                            }
                        }
                        else if (isNumber(b)) {
                            this.push(Math.pow(a.valueOf() as number, b.valueOf()));
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
                            this.push(a, b);
                            this.runMacro("~;*{;/c;%!w,d");
                        }
                        else if (isArray(a) && isArray(b)) {
                            let result: StaxArray = [];
                            for (let i = 0, offset = 0; offset < a.length; i++) {
                                let size = b[i % b.length];
                                if (isNumber(size)) {
                                    result.push(a.slice(offset, offset += Math.floor(size.valueOf())));
                                }
                                else fail("can't multi-chunk by non-number");
                            }
                            this.push(result);
                        }
                        break;
                    }
                    case '|\\':
                        if (isArray(this.peek())) {
                            this.runMacro("b%s% |m~ ;(s,(s \\"); // zip; truncate to shorter
                        }
                        else { // zip arrays using fill element
                            let fill = this.pop(), b = this.popArray(), a = this.popArray(), result = [];
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
                            this.runMacro("ssb%~/,");
                        }
                        else if (isArray(this.peek())) { // embed sub-array
                            let c = this.pop(), b = this.pop(), a = this.popArray();
                            let result = a.slice(), loc: number, payload: StaxArray;
                            if (isArray(c)) [payload, loc] = [c, b.valueOf() as number];
                            else [payload, loc] = [b as StaxArray, c.valueOf() as number];

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
                                i = i.add(one);
                            }
                            this.push(result);
                        }
                        break;
                    case '|1':
                        if (isArray(this.peek())) { // index of 1st truthy
                            let result = minusOne, i = 0;
                            for (let e of this.popArray()) {
                                if (isTruthy(e)) {
                                    result = bigInt(i);
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
                                    if (e.length > i) result.push(e[i]);
                                    else result.push(zero);
                                }
                                else {
                                    result.push(i == 0 ? e : zero);
                                }
                                ++i;
                            }
                            this.push(result);
                        }
                        else if (isNumber(this.peek())) {
                            this.runMacro("2s|*"); // power of 2
                        }
                        break;
                    case '|3':
                        this.runMacro("36|b"); // base 36
                        break;
                    case '|4':
                        this.push(isArray(this.pop()) ? one : zero);
                        break;
                    case '|5': { // 0-indexed fibonacci number
                        let n = this.popInt().valueOf(), a = one, b = one;
                        for (let i = 0; i < n; i++) [a, b] = [b, a.plus(b)];
                        this.push(a);
                        break;
                    }
                    case '|6': { // 0-indexed nth prime
                        let i = 0, n = this.popInt().valueOf();
                        for (let p of allPrimes()) {
                            if (i++ === n) {
                                this.push(p);
                                break;
                            }
                        }
                        break;
                    }
                    case '|7':
                        this.push(Math.cos((this.pop() as StaxNumber).valueOf()));
                        break;
                    case '|8':
                        this.push(Math.sin((this.pop() as StaxNumber).valueOf()));
                        break;
                    case '|9':
                        this.push(Math.tan((this.pop() as StaxNumber).valueOf()));
                        break;
                    case '|a':
                        if (isNumber(this.peek())) { // absolute value
                            let num = this.pop();
                            if (isInt(num)) this.push(num.abs());
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
                        if (isInt(this.peek())) {
                            this.push(bigInt[10].pow(this.popInt().valueOf()));
                        }
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
                            let b = this.popArray(), a = this.popArray(), result = [];
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
                        this.push(bigInt(this.mainStack.length));
                        break;
                    case '|D':
                        this.push(bigInt(this.inputStack.length));
                        break;
                    case '|e':
                        if (isInt(this.peek())) {
                            this.push((this.pop() as BigInteger).isEven() ? one : zero);
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
                                if (match) {
                                    result.push(S2A(match[0]));
                                }
                            } while (match);
                            this.push(result);
                        }
                        break;
                    }
                    case '|F':
                        if (isInt(this.peek())) { // factorial
                            let result = one, n = this.popInt();
                            for (let i = one; i.lesserOrEquals(n); i = i.add(one)) {
                                result = result.multiply(i);
                            }
                            this.push(result);
                        }
                        else if (isArray(this.peek())) { // all regex matches
                            let re = new RegExp(A2S(this.popArray()), "g");
                            let input = A2S(this.popArray()), result = [], m;
                            while (m = re.exec(input)) {
                                result.push(S2A(m[0]));
                            }
                            this.push(result);
                        }
                        break;
                    case '|g':
                        this.doGCD();
                        break;
                    case '|G': { // round-robin flatten
                        let arr = this.popArray(), result: StaxArray = [];
                        let maxlen = Math.max(...arr.map(e => (e as StaxArray).length));
                        for (let i = 0; i < maxlen; i++) {
                            for (let e of arr) {
                                let line = e as StaxArray;
                                if (line.length > i) result.push(line[i]);
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
                    case '|J':
                        this.runMacro("Vn*"); // join with newlines
                        break;
                    case '|l': // lcm
                        if (isArray(this.peek())) this.runMacro("1s{|lF");
                        else if (isInt(this.peek())) this.runMacro("b|g~*,/");
                        else fail("bad types for lcm");
                        break;
                    case '|L': {
                        let b = this.pop(), a = this.pop();
                        if (isNumber(b)) { // log with base
                            let result = Math.log(a.valueOf() as number) / Math.log(b.valueOf());
                            this.push(result);
                        }
                        else if (isArray(b) && isArray(a)) { 
                            // combine elements from a and b, with each occurring the max of its occurrences from a and b
                            let result: StaxArray = [];
                            b = [...b];
                            for (let e of a) {
                                result.push(e);
                                for (let i = 0; i < b.length; i++) {
                                    if (areEqual(b[i], e)) {
                                        b.splice(i, 1);
                                        break;
                                    }
                                }
                            }
                            this.push(result.concat(b));
                        }
                        break;
                    }
                    case '|m': {
                        if (isNumber(this.peek())) {
                            if (this.totalSize() < 2) break;
                            let top = this.pop(), next = this.pop();
                            this.push(compare(next, top) < 0 ? next : top);
                        }
                        else if (isArray(this.peek())) {
                            let arr = this.popArray();
                            let result = arr[0];
                            for (let e of arr.slice(1)) {
                                if (compare(e, result) < 0) result = e; 
                            }
                            this.push(result);
                        }
                        break;
                    }
                    case '|M': {
                        if (isNumber(this.peek())) {
                            if (this.totalSize() < 2) break;
                            let top = this.pop(), next = this.pop();
                            this.push(compare(next, top) > 0 ? next : top);
                        }
                        else if (isArray(this.peek())) {
                            let arr = this.popArray();
                            let result = arr[0];
                            for (let e of arr.slice(1)) {
                                if (compare(e, result) > 0) result = e; 
                            }
                            this.push(result);
                        }
                        break;
                    }
                    case '|n': 
                        if (isInt(this.peek())) { // exponents of sequential primes in factorization
                            let target = this.popInt().abs(), result: StaxArray = [];
                            for (let p of allPrimes()) {
                                if (target.lesserOrEquals(one)) break;
                                let exp = zero;
                                while (target.mod(p).isZero()) {
                                    target = target.divide(p);
                                    exp = exp.add(one);
                                }
                                result.push(exp);
                            }
                            this.push(result);
                        }
                        else if (isArray(this.peek())) {
                            // combine elements from a and b, removing common elements as many times as they mutually occur
                            let b = this.popArray(), a = this.popArray(), result: StaxArray = [];
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
                            let els = [...this.popArray()], result: StaxArray = [], i = els.length - 2;
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
                        break;
                    case '|o': { // get indices of elements when ordered
                        let a = this.popArray(), result: StaxArray = [], i = 0;
                        let idxs = range(0, a.length).sort((x, y) => compare(a[x.valueOf() as number], a[y.valueOf()  as number]));
                        for (let t of idxs) result[t.valueOf() as number] = bigInt(i++);
                        this.push(result);
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
                        this.print('');
                        break;
                    case '|q': {
                        let b = this.pop();
                        if (isNumber(b)) {
                            this.push(bigInt(Math.floor(Math.sqrt(Math.abs(b.valueOf())))));
                        }
                        else if (isArray(b)) {
                            let pattern = new RegExp(A2S(b), "g"), text = A2S(this.popArray());
                            let match: RegExpExecArray | null;
                            let result: StaxArray = [];
                            while (match = pattern.exec(text)) {
                                result.push(bigInt(match.index));
                            }
                            this.push(result);
                        }
                        break;
                    }
                    case '|Q': {
                        let b = this.pop();
                        if (isNumber(b)) {
                            this.push(Math.sqrt(Math.abs(b.valueOf())));
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
                        if (isArray(end)) end = bigInt(end.length);
                        if (isArray(start)) start = bigInt(start.length);
                        if (isInt(start) && isInt(end)) this.push(range(start, end));
                        else fail("bad types for |r");
                        break;
                    }
                    case '|R': {
                        if (isInt(this.peek())) { // start-end-stride with range
                            let stride = this.popInt(), end = this.popInt(), start = this.popInt();
                            let result = range(0, end.minus(start))
                                .map((n: BigInteger) => n.multiply(stride).add(start))
                                .filter(n => n.lt(end));
                            this.push(result);
                        }
                        else if (isArray(this.peek())) { // RLE
                            this.push(runLength(this.popArray()));
                        }
                        break;
                    }
                    case '|s': {
                        let search = A2S(this.popArray()), text = A2S(this.popArray());
                        this.push(text.split(new RegExp(search)).map(S2A));
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
                        this.push(this.x = (isInt(this.x) ? this.x : zero).subtract(one));
                        break;
                    case '|X':
                        this.push(this.x = (isInt(this.x) ? this.x : zero).add(one));
                        break;
                    case '|y':
                        this.push(this.y = (isInt(this.y) ? this.y : zero).subtract(one));
                        break;
                    case '|Y':
                        this.push(this.y = (isInt(this.y) ? this.y : zero).add(one));
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
                            let result: StaxArray = [];
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
            }

            ip += token.length;
        }

        yield new ExecutionState(ip);
    }

    private runMacro(macro: string) {
        for (let s of this.runSteps(parseProgram(macro))) { }
    }

    private doPlus() {
        if (this.totalSize() < 2) return; 
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a + b;
            else if (a instanceof Rational && b instanceof Rational) result = a.add(b);
            else if (isInt(a) && isInt(b)) result = a.add(b);
            else throw "weird types or something; can't add?"
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) {
            this.push([...a, ...b]);
        }
        else if (isArray(a)) {
            this.push([...a, b]);
        }
        else if (isArray(b)) {
            this.push([a, ...b]);
        }
    }

    private doMinus() {
        let b = this.pop(), a = this.pop();
        if (isArray(a) && isArray(b)) {
            let bArr = b;
            let result = a.filter(a_ => !bArr.some(b_ => areEqual(a_, b_)));
            this.push(result);
        }
        else if (isArray(a)) {
            this.push(a.filter(a_ => !areEqual(a_, b)));
        }
        else if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a - b;
            else if (a instanceof Rational && b instanceof Rational) result = a.subtract(b);
            else if (isInt(a) && isInt(b)) result = a.subtract(b);
            else throw "weird types or something; can't subtract?"
            this.push(result);
        }
        else throw new Error('bad types for -');
    }

    private *doStar() {
        if (this.totalSize() < 2) return;
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a * b;
            else if (a instanceof Rational && b instanceof Rational) result = a.multiply(b);
            else if (isInt(a) && isInt(b)) result = a.multiply(b);
            else throw "weird types or something; can't multiply?"
            this.push(result);
        }
        else if (isInt(a) && isArray(b)) {
            let result: StaxArray = [];
            let count = a.valueOf();
            if (count < 0) [b, count] = [b.reverse(), -count];
            for (let i = 0; i < count; i++) result = result.concat(b);
            this.push(result);
        }
        else if (isArray(a) && isInt(b)) {
            let result: StaxArray = [];
            let count = b.valueOf();
            if (count < 0) [a, count] = [a.reverse(), -count];
            for (let i = 0; i < count; i++) result = result.concat(a);
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) {
            this.push(S2A(a.map(e => isArray(e) ? A2S(e) : e.toString()).join(A2S(b))));
        }
        else if (a instanceof Block && isInt(b)) {
            let block = a, times = b.valueOf();
            this.pushStackFrame();
            for (this.index = zero; this.index.lt(times); this.index = this.index.add(one)) {
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
            for (this.index = zero; this.index.lt(times); this.index = this.index.add(one)) {
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
            if (isInt(b) && b.isZero() || b instanceof Rational && b.numerator.isZero()) {
                [a, b] = [a.valueOf(), b.valueOf()];
            }
            if (isFloat(a) && isFloat(b)) result = a / b;
            else if (a instanceof Rational && b instanceof Rational) result = a.divide(b);
            else if (isInt(a) && isInt(b)) {
                if (b.isNegative()) {
                    a = a.negate();
                    b = b.negate();
                }
                if (a.isNegative()) a = a.subtract(b).add(one);
                result = a.divide(b);
            }
            else throw "weird types or something; can't divide?"
            this.push(result);
        }
        else if (isArray(a) && isInt(b)) {
            let result = [];
            for (let i = 0; i < a.length; i += b.valueOf()) {
                result.push(a.slice(i, i + b.valueOf()));
            }
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) {
            let strings = A2S(a).split(A2S(b));
            this.push(strings.map(s => S2A(s)));
        }
        else if (isArray(a) && b instanceof Block) {
            let result: StaxArray = [], currentPart: StaxArray | null = null, last = null, cancelled = false;

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
                this.index = this.index.add(one);
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

        a = a as StaxArray;
        b = b as StaxArray;

        let result: StaxArray = [], size = Math.max(a.length, b.length);
        for (let i = 0 ; i < size; i++) {
            result.push([ a[i % a.length], b[i % b.length] ]);
        }
        this.push(result);
    }

    private doPercent() {
        let b = this.pop();
        if (isArray(b)) {
            this.push(bigInt(b.length));
            return;
        }
        let a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            [a, b] = widenNumbers(a, b);
            let result: StaxNumber;
            if (typeof a === "number" && typeof b === "number") result = a % b;
            else if (isInt(a) && isInt(b)) {
                result = a.mod(b);
                if (result.isNegative()) result = result.add(b);
            }
            else if (a instanceof Rational && b instanceof Rational) result = a.mod(b);
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
            for (let c of arg.abs().toString()) {
                result.push(bigInt(c));
            }
            this.push(result);
        }
    }

    private doPowerset() {
        let b = this.pop();
        if (isInt(b)) {
            let len = b.valueOf(), arr = this.popArray(), result: StaxArray = []; 
            let idxs = range(0, b).map(i => (i as BigInteger).valueOf());
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
        else if (isArray(b)) {
            let result: StaxArray = [];
            for (let e of b.slice().reverse()) {
                result = result.concat(result.map(r => [e, ...r as StaxArray]));
                result.push([e]);
            }
            this.push(result.reverse());
        }
    }

    private doPermutations() {
        let targetSize = isInt(this.peek()) ? this.popInt().valueOf() : Number.MAX_SAFE_INTEGER;
        let els = this.popArray(), result: StaxArray = [];
        targetSize = Math.min(els.length, targetSize);

        // factoradic permutation decoder
        let totalPerms = 1, stride = 1;
        for (let i = 1; i <= els.length; i++) totalPerms *= i;
        for (let i = 1; i <= els.length - targetSize; i++) stride *= i;
        let idxs = els.map(_ => 0);
        for (let pi = 0; pi < totalPerms; pi += stride) {
            let n = pi;
            for (let i = 1; i <= els.length; n = n / i++ | 0) idxs[els.length - i] = n % i;
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
            this.push(bigInt(Math.floor(top)));
            return;
        }

        let list = this.pop();

        function readAt(arr: StaxArray, idx: number) {
            idx %= arr.length;
            if (idx < 0) idx += arr.length;
            return arr[idx];
        }

        // read at index
        if (isInt(list) && isArray(top)) [list, top] = [top, list];
        if (isArray(list) && isArray(top)) {
            let result = [];
            for (let idx of top) result.push(readAt(list, idx as number));
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
            indexes = [indexes];
            if (isInt(this.peek())) {
                while (isInt(this.peek())) indexes.unshift(this.popInt());
                indexes = [indexes];
            }
        }
        if (!isArray(indexes)) throw new Error("unknown index type for assign-index");
        
        const self = this;
        function *doFinalAssign (flatArr: StaxArray, index: number) {
            if (index >= flatArr.length) {
                flatArr.push(...Array(index + 1 - flatArr.length).fill(zero));
            }

            if (element instanceof Block) {
                self.push(flatArr[index]);
                let cancelled = false;
                for (let s of self.runSteps(element)) {
                    yield s;
                    cancelled = s.cancel;
                }
                if (!cancelled) flatArr[index] = self.pop();
            }
            else {
                flatArr[index] = element;
            }
        }

        let list = this.popArray(), result: StaxArray = [...list];
        for (let arg of indexes) {
            if (isArray(arg)) {
                let idxPath = arg, target = result, idx: number;
                for (let i = 0; i < idxPath.length - 1; i++) {
                    idx = idxPath[i].valueOf() as number;
                    while (target.length <= idx) target.push([]);
                    if (!isArray(target[idx])) target[idx] = [ target[idx] ];
                    target = target[idx] as StaxArray;
                }
                idx = last(idxPath)!.valueOf() as number;
                for (let s of doFinalAssign(target, idx)) yield s;
            }
            else if (isInt(arg)) {
                let index = arg.valueOf();
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
            if (!isInt(b)) fail("need integer index for remove");
            let b_ = b.valueOf() as number, result = [...a];
            if (b < 0) b_ += result.length;
            if (b >= 0 && b < result.length) result.splice(b_, 1);
            this.push(result);
        }
        else { // insert element at index
            let arr = this.popArray(), result = [...arr], a_ = a.valueOf() as number;
            if (a_ < 0) a_ += result.length;
            if (a_ < 0) {
                result.unshift(b, ...new Array(-a_).fill(zero));
            }
            else if (a > result.length) {
                result.push(...new Array(a_ - result.length).fill(zero), b);
            }
            else {
                result.splice(a_, 0, b);
            }
            this.push(result);
        }
    }

    private doBaseConvert(stringRepresentation = true) {
        let base = this.popInt().valueOf(), number = this.pop();

        if (isInt(number)) {
            let result = [];
            if (base === 1) result = new Array(number).fill(zero);
            else do {
                let digit = number.mod(base);
                if (stringRepresentation) {
                    let d = "0123456789abcdefghijklmnopqrstuvwxyz".charCodeAt(digit.valueOf());
                    result.unshift(bigInt(d));
                }
                else { // digit mode
                    result.unshift(digit);
                }
                number = number.divide(base);
            } while (number.isPositive());

            this.push(result);
        }
        else if (isArray(number)) {
            let result = zero;
            if (stringRepresentation) {
                let s = A2S(number).toLowerCase();
                for (let c of s) {
                    let digit = "0123456789abcdefghijklmnopqrstuvwxyz".indexOf(c);
                    if (digit < 0) digit = c.charCodeAt(0);
                    result = result.multiply(base).add(digit);
                }
            }
            else {
                for (let d of number) {
                    if (!isInt(d)) throw new Error("digits list contains a non-integer");
                    result = result.multiply(base).add(d);
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
            for (let e of b) result = bigInt.gcd(result, e as BigInteger);
            this.push(result);
            return;
        }

        let a = this.pop();
        if (isInt(a) && isInt(b)) {
            let gcd = bigInt.gcd(a, b);
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
            a = [...a];
            let bval = b.valueOf();
            if (bval < 0) bval += a.length;
            if (a.length < bval) a.unshift(...Array(bval - a.length).fill(zero));
            if (a.length > bval) a.splice(0, a.length - bval);
            this.push(a);
        }
        else if (isArray(a) && isArray(b)) {
            let result = [];
            for (let i = 0; i < b.length; i++) {
                result.push(a.length - b.length + i >= 0 ? a[a.length - b.length + i] : b[i]);
            }
            this.push(result);
        }
        else if (isArray(a) && b instanceof Block) {
            // partition where the block produces a truthy for the pair of values surrounding the boundary
            let result: StaxArray = [], current = [];
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
                this.index = this.index.add(one);
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
            a = [...a];
            let bval = b.valueOf();
            if (bval < 0) bval += a.length;
            if (a.length < bval) a.push(...Array(bval - a.length).fill(zero));
            if (a.length > bval) a.splice(bval);
            this.push(a);
        }
        else if (isArray(a) && isArray(b)) {
            let result = [];
            for (let i = 0; i < b.length; i++) {
                result.push(i < a.length ? a[i] : b[i]);
            }
            this.push(result);
        }
        else if (isArray(a) && b instanceof Block) {
            let result = [], current: StaxArray | null = null;

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

                this.index = this.index.add(one);
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
                let result = Array((top.valueOf() - data.length) >> 1).fill(zero);
                result = result.concat(data);
                result = result.concat(Array(top.valueOf() - result.length).fill(zero));
                this.push(result);
            }
            else if (isInt(data)) { // binomial coefficient
                let r = top, n = data, result = one;
                if (n.isNegative() || r.gt(n)) result = zero;
                for (let i = one; i.leq(r); i = i.add(one)) {
                    result = result.multiply(n.subtract(i).add(one)).divide(i);
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
            let result = [];
            let map: {[key: string]: StaxValue} = {};

            for (let i = 0; i < translation.length; i += 2) map[translation[i].toString()] = translation[i + 1];
            for (let e of input) {
                let mapped = map[e.toString()];
                result.push(mapped == null ? e : mapped);
            }
            this.push(result);
        }
        else fail("bad types for translate");
    }

    private doOverlappingBatch() {
        let b = this.pop(), a = this.pop(), result = [];
        if (!isInt(b) || !isArray(a)) throw new Error("bad types for overlapping-batch");

        let bv = b.valueOf(), end = a.length - bv + 1;
        for (let i = 0; i < end; i++) result.push(a.slice(i, i + bv));
        this.push(result);
    }

    private doTrimElementsFromStart() {
        let b = this.pop(), a = this.popArray(), i = 0;

        for (; i < a.length; i++) {
            if (isArray(b)) {
                if (!b.some(e => areEqual(e, a[i]))) break;
            } 
            else {
                if (!areEqual(a[i], b)) break;
            }
        }

        let result = a.slice(i);
        this.push(result);
    }

    private doTrimElementsFromEnd() {
        let b = this.pop(), a = this.popArray(), i = a.length - 1;
        
        for (; i >= 0; i--) {
            if (isArray(b)) {
                if (!b.some(e => areEqual(e, a[i]))) break;
            }
            else {
                if (!areEqual(a[i], b)) break;
            }
        }

        let result = a.slice(0, i + 1);
        this.push(result);
    }

    private doPartition() {
        let n = this.popInt().valueOf(), arg = this.pop();
        let total = isArray(arg) ? arg.length : arg.valueOf() as number;

        let result: StaxArray = [];
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
            else {
                let mapped = partition.map(v => bigInt(v));
                result.push(mapped);
            }

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
        let arr = this.popArray(), result: StaxArray = [];
        if (arr.length > 0) {
            let multi = new Multiset(arr), keys = multi.keys();
            let min = Math.min(...keys.map(k => multi.get(k)));
            result = keys.filter(k => multi.get(k) === min);
            result.sort(compare);
        }
        this.push(result);
    }

    private doRotate(direction: number) {
        let distance = this.pop(), arr: StaxArray;

        if (isArray(distance)) {
            arr = distance;
            distance = one;
        }
        else {
            let popped = this.pop();
            arr = isArray(popped) ? popped : fail("bad types for rotate");
        }

        if (!isInt(distance)) throw new Error("bad rotation distance");

        distance = distance.mod(arr.length);
        if (distance.isNegative()) distance = distance.add(arr.length);
        let cutpoint = direction < 0 ? distance.valueOf() : (arr.length - distance.valueOf());
        let result = arr.slice(cutpoint).concat(arr.slice(0, cutpoint));
        this.push(result);
    }

    private doLastIndexOf() {
        let target = this.popArray(), arr = this.popArray();
        for (let i = arr.length - 1 - target.length; i >= 0; i--) {
            if (areEqual(target, arr.slice(i, i + target.length))) {
                this.push(bigInt(i));
                return;
            }
        }
        this.push(minusOne);
    }

    private *doIndexOf() {
        let target = this.pop(), arr = this.pop();
        if (!isArray(arr)) [arr, target] = [target, arr];
        if (!isArray(arr)) throw new Error("bad types for index-of");

        for (let i = 0; i < arr.length; i++) {
            let el = arr[i];
            if (isArray(target)) {
                if (i + target.length > arr.length) {
                    this.push(minusOne);
                    return;
                }
                if (areEqual(target, arr.slice(i, i + target.length))) {
                    this.push(bigInt(i));
                    return;
                }
            }
            else if (target instanceof Block) {
                this.pushStackFrame();
                this.push(this._ = el);
                this.index = bigInt(i);
                let cancelled = false;
                for (let s of this.runSteps(target)) {
                    yield s;
                    if (cancelled = s.cancel) break;
                }
                if (!cancelled && isTruthy(this.pop())) {
                    this.push(bigInt(i));
                    this.popStackFrame();
                    return;
                }
                this.popStackFrame();
            }
            else if (areEqual(target, el)) {
                this.push(bigInt(i));
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
            this.index = this.index.add(one);
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
            let chunks = top.valueOf(), consumed = 0, arr = this.popArray(), result: StaxArray = [];

            for (; chunks > 0; chunks--) {
                let toTake = Math.ceil((arr.length - consumed) / chunks);
                result.push(arr.slice(consumed, consumed + toTake));
                consumed += toTake;
            }

            this.push(result);
        }
        else if (isArray(top)) {
            if (top.length > 0 && !isArray(top[0])) top = [top];
            top = top.map((line) => [...line as StaxArray]); // prevent mutations
            let result: StaxArray = [];
            let maxlen = Math.max(...top.map(e => (e as StaxArray).length));

            for (let line of top) {
                line = line as StaxArray;
                line.push(...Array(maxlen - line.length).fill(zero));
            }

            for (let i = 0; i < maxlen; i++) {
                let column: StaxArray = [];
                for (let row of top) column.push((row as StaxArray)[i]);
                result.push(column);
            }

            this.push(result);
        }
        else fail("bad types for transpose/maybe");
    }

    private *doOrder() {
        let top = this.pop();
        if (isArray(top)) {
            let result = top.map((val, idx) => ({ val, idx }))
                .sort((a, b) => compare(a.val, b.val) || a.idx - b.idx)
                .map(t => t.val);
            this.push(result);
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
                this.index = this.index.add(one);
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
        let project = this.pop(), arr = this.pop(), result: StaxArray = [], extreme: StaxValue | null = null;
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

            this.index = this.index.add(one);
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
                result.push(bigInt(lastFound));
            }
            this.push(result);
        }
        else if (target instanceof Block) {
            this.pushStackFrame();
            let result = [];
            for (let i = 0; i < arr.length; i++) {
                this.push(this._ = arr[i]);
                this.index = bigInt(i);
                for (let s of this.runSteps(target)) yield s;
                if (isTruthy(this.pop())) result.push(this.index);
            }
            this.popStackFrame();
            this.push(result);
        }
        else {
            let result = [];
            for (let i = 0; i < arr.length; i++) {
                if (areEqual(arr[i], target)) result.push(bigInt(i));
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
            let ss = RegExp(A2S(search));
            let replaceBlock = replace;
            
            let result = "";
            let charsUsed = 0;
            let match: RegExpMatchArray | null;

            this.pushStackFrame();
            while ((match = ts.substr(charsUsed).match(ss))) {
                result += ts.substring(charsUsed, charsUsed + match.index!);
                
                this.push(this._ = S2A(match[0]));
                for (let s of this.runSteps(replaceBlock)) yield s;
                result += A2S(this.popArray());
                charsUsed += match.index! + match[0].length;
                
                this.index = this.index.add(one);
            }
            result += ts.substr(charsUsed);

            this.popStackFrame();
            this.push(S2A(result));
        }
        else throw new Error("bad types for replace");
    }

    private *doFor(rest: Block) {
        if (isInt(this.peek())) {
            this.push(range(1, this.popInt().add(one)));
        }

        if (this.peek() instanceof Block) {
            let block = this.pop() as Block, data = this.pop();
            if (isInt(data)) data = range(1, data.add(one));
            if (!isArray(data)) throw Error("block-for operates on ints and arrays, not this garbage. get out of here.");
            
            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
            }
            this.popStackFrame();
        }
        else if (isArray(this.peek())) {
            let data = this.pop() as StaxArray;

            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
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
            this.index = this.index.add(one);
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
            this.index = this.index.add(one);
        } while (!cancelled && isTruthy(this.pop()));
        this.popStackFrame();
    }

    private *doReduce(rest: Block) {
        let top = this.pop(), shorthand = !(top instanceof Block);
        let block: Block | string, arr: StaxValue;

        if (top instanceof Block) [block, arr] = [top, this.pop()];
        else [block, arr] = [rest, top];

        if (isInt(arr)) arr = range(one, arr.add(one));
        else if (isArray(arr)) arr = [...arr];

        if (isArray(arr)) {
            if (arr.length === 0) throw new Error("tried to reduce empty array");
            if (arr.length === 1) {
                this.push(arr[0]);
                return;
            }

            this.pushStackFrame();
            this.push(arr.shift()!);
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (s.cancel) {
                        this.popStackFrame();
                        return;
                    }
                    yield s;
                }
                this.index = this.index.add(one);
            }
            this.popStackFrame();

            if (shorthand) this.print(this.pop());
        }
        else {
            throw new Error("bad types for reduce");
        }
    }

    private *doCrossMap(rest: Block) {
        let top = this.pop(), 
            shorthand = !(top instanceof Block), 
            map: Block | string = shorthand ? rest : top as Block;
        
        let inner = shorthand ? top : this.pop(), outer =  this.pop();
        if (isInt(inner)) inner = range(one, inner.add(one));
        if (isInt(outer)) outer = range(one, outer.add(one));
        if (!isArray(outer) || !isArray(inner)) throw new Error("need arrays or integers for crossmap");

        let result: StaxArray = [];
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
                if (!cancelled) row.push(this.pop());
                this.index = this.index.add(one);
            }
            this.popStackFrame();
            this.index = this.index.add(one);
            if (shorthand) this.print(row);
            else result.push(row);
        }
        this.popStackFrame();

        if (!shorthand) this.push(result);
    }

    private *doFilter(rest: Block) {
        if (this.peek() instanceof Block) {
            let block = this.pop() as Block, data = this.pop(), result: StaxArray = [], cancelled = false;
            if (isInt(data)) data = range(1, data.add(one));
            if (!isArray(data)) throw Error("block-filter operates on ints and arrays, not this garbage. get out of here.");
            
            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
                if (!cancelled && isTruthy(this.pop())) result.push(e);
            }
            this.popStackFrame();
            this.push(result);
        }
        else {
            let data = this.pop(), cancelled = false;
            let arr = isArray(data) ? data 
                : isInt(data) ? range(1, data.add(one))
                : fail("bad type for shorthand filter data")

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
                if (!cancelled && isTruthy(this.pop())) this.print(e);
            }
            this.popStackFrame();
        }
    }

    private *doMap(rest: Block) {
        let top = this.pop();
        if (top instanceof Block) {
            let block = top, data = this.pop(), result: StaxArray = [], cancelled = false;
            if (isInt(data)) data = range(1, data.add(one));
            if (!isArray(data)) throw Error("block-map operates on ints and arrays, not this garbage. get out of here.");
            
            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(block)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
                if (!cancelled) result.push(this.pop());
            }
            this.popStackFrame();
            this.push(result);
        }
        else if (isArray(top) || isInt(top)) {
            let data = isArray(top) ? top : range(one, top.add(one)), cancelled = false;

            this.pushStackFrame();
            for (let e of data) {
                this.push(this._ = e);
                for (let s of this.runSteps(rest)) {
                    if (cancelled = s.cancel) break;
                    yield s;
                }
                this.index = this.index.add(one);
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

        let result = [arr[0]];
        this.pushStackFrame();
        this.push(arr[0]);
        for (let e of arr.slice(1)) {
            this.push(this._ = e);
            for (let s of this.runSteps(reduce)) yield s;
            result.push(this.peek());
            this.index = this.index.add(one);
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

        let hardCodedTargetCount = false;
        if (lowerSpec === 'n') targetCount = this.popInt().valueOf();
        else if (lowerSpec === 'e') targetCount = this.popInt().valueOf() + 1;
        else if (lowerSpec === 's') [targetCount, hardCodedTargetCount] = [1, true];
        else {
            let idx = "1234567890!@#$%^&*()".indexOf(spec);
            if (idx >= 0) {
                targetCount = idx % 10 + 1;
                postPop = idx >= 10;
                hardCodedTargetCount = true;
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
        var result: StaxArray = [];

        let lastGenerated : StaxValue | null = null;
        let genComplete = false, cancelled = false;
        while (targetCount == null || result.length < targetCount) {
            this._ = this.peek();

            if (this.index.isPositive() || postPop) {
                if (genBlock.contents !== "" && genBlock.contents !== "{") {
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
            this.index = this.index.add(one); 
        }
        if (!postPop && !genComplete) {
            // Remove left-over value from pre-peek mode
            // It's kept on stack between iterations, but iterations are over now
            this.pop();
        }
        
        this.popStackFrame();

        if (shorthand) {
            if (scalarMode) this.print(last(result)!);
            else for (let e of result) this.print(e);
        }
        else {
            if (scalarMode) this.push(last(result)!);
            else this.push(result);
        }
    }

    private doMacroAlias(alias: string) {
        let typeTree = macroTrees[alias];
        let resPopped: StaxArray = [];
        // follow type tree as far as necessary
        while (typeTree.hasChildren()) {
            resPopped.push(this.pop());
            let type = getTypeChar(last(resPopped)!);
            typeTree = typeTree.children![type];
        }
        // return inspected values to stack
        this.push(...resPopped.reverse());

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