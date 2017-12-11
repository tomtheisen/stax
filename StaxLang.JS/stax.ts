import { StaxArray, StaxNumber, StaxValue, isArray, isFloat, isInt, isNumber, isTruthy, A2S, S2A, floatify, constants, widenNumbers, areEqual, compare, stringFormat } from './types';
import { Block, Program, parseProgram } from './block';
import * as _ from 'lodash';
import * as bigInt from 'big-integer';
import { Rational } from './rational';
import IteratorPair from './iteratorpair';
import Multiset from './multiset';
import { primeFactors } from './primehelper';
import { compress, decompress } from './huffmancompression';
import { macroTrees, getTypeChar } from './macrotree';
type BigInteger = bigInt.BigInteger;
const one = bigInt.one, zero = bigInt.zero, minusOne = bigInt.minusOne;

export class ExecutionState {
    public ip: number;
    public cancel: boolean;

    constructor(ip: number, cancel = false) {
        this.ip = ip;
        this.cancel = cancel;
    }
}

function fail(msg: string): never {
    throw new Error(msg);
}

function range(start: number | BigInteger, end: number | BigInteger): StaxArray {
    return _.range(start.valueOf(), end.valueOf()).map(n => bigInt(n));
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

    constructor(output: (line: string) => void) {
        this.lineOut = output;
    }

    private push(...vals: StaxValue[]) {
        vals.forEach(e => this.mainStack.push(e));
    }

    private peek(): StaxValue {
        return _.last(this.mainStack) || _.last(this.inputStack) || fail("peeked empty stacks");
    }

    private pop(): StaxValue {
        return this.mainStack.pop() || this.inputStack.pop() || fail("popped empty stacks");
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

        if (isInt(val) || typeof val === "number") val = val.toString();
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
            if (activeArrays.length) _.last(activeArrays)!.push(val);
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
                    newValue(S2A(arg.substring(i + 1, finishPos)));
                    i = finishPos;
                    break;
                case '-':
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    let substring = arg.substr(i);
                    let match = substring.match(/-?\d+\.\d+/);
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
        while (stdin[0] === "") stdin.shift();
        this.inputStack = _.reverse(stdin).map(S2A);
        this.y = _.last(this.inputStack) || [];
        this._ = S2A(stdin.join("\n"));

        if (stdin.length === 1) {
            if (!this.doEval()) {
                this.mainStack = [];
                this.inputStack = _.reverse(stdin).map(S2A);
            }
            else if (this.totalSize() === 0) {
                this.inputStack = _.reverse(stdin).map(S2A);
            }
            else {
                this.x = this.mainStack[0];
                [this.mainStack, this.inputStack] = [this.inputStack, this.mainStack];
            }
        }

        for (let s of this.runSteps(program)) yield s;

        if (this.outBuffer) this.print("");
        if (!this.producedOutput) this.print(this.pop());
    }

    private *runSteps(block: Block | string): IterableIterator<ExecutionState> {
        if (typeof block === "string") block = this.program = parseProgram(block);

        let ip = 0;

        for (let token of block.tokens) {
            const getRest = () => (block as Block).contents.substr(ip + token.length);

            yield new ExecutionState(ip);

            if (token instanceof Block) {
                this.push(token);
                continue;
            }
            else {
                if (!!token[0].match(/\d+!/)) this.push(parseFloat(token.replace("!", ".")));
                else if (!!token[0].match(/\d/)) this.push(bigInt(token));
                else if (token[0] === '"') this.doEvaluateStringToken(token);
                else if (token[0] === '`') {
                    let compressed = token.replace(/^`|`$/g, '');
                    this.push(S2A(decompress(compressed)));
                    if (token[token.length - 1] === '`') this.print(this.peek());
                }
                else if (token[0] === "'" || token[0] === ".") this.push(S2A(token.substr(1)));
                else if (token[0] === 'V') this.push(constants[token[1]]);
                else if (token[0] === ':') this.doMacroAlias(token[1]);
                else switch (token) {
                    case ' ':
                        break;
                    case '}':
                        return;
                    case '~':
                        this.inputStack.push(this.pop());
                        break;
                    case ';':
                        this.inputStack.length || fail("stack empty");
                        this.push(_.last(this.inputStack)!);
                        break;
                    case ',':
                        this.inputStack.length || fail("stack empty");
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
                        this.push(isTruthy(this.pop()) ? zero : one);
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
                        this.doAssignIndex();
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
                    case '$':
                        this.push(stringFormat(this.pop()));
                        break;
                    case '(':
                        for (let s of this.doPadRight()) yield s;
                        break;
                    case ')':
                        this.doPadLeft();
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
                        else if (isArray(this.pop())) this.runMacro("c1tsh"); // uncons
                        else if (this.peek() instanceof Rational) this.runMacro("c@s1%"); // properize
                        else throw new Error("bad type for B");
                        break;
                    case 'c':
                        this.push(this.peek());
                        break;
                    case 'C':
                        if (isTruthy(this.pop())) {
                            yield new ExecutionState(ip, true);
                            return;
                        }
                        break;
                    case 'd':
                        this.pop();
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
                    case 'f': {
                        let shorthand = !(this.peek() instanceof Block);
                        for (let s of this.doFilter(getRest())) yield s;
                        if (shorthand) return;
                        break;
                    }
                    case 'F':{
                        let shorthand = !(this.peek() instanceof Block);
                        for (let s of this.doFor(getRest())) ;
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
                        else if (isArray(this.peek())) this.push((this.pop() as StaxArray)[0]);
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
                        else if (isArray(this.peek())) this.push(_.last(this.pop() as StaxArray) || fail("empty array has no last element"));
                        else if (this.peek() instanceof Block) {
                            let pred = this.pop() as Block, result: StaxArray = [], arr = this.pop(), cancelled = false;
                            if (!isArray(arr)) throw new Error("bad types for take-while");
                            arr = _.clone(arr); 
                            arr.reverse();
                            
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
                        this.push(this.index);
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
                        for (let s of this.doReduce(getRest())) ;
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
                            _.times(a.valueOf(), () => result.unshift(this.pop()));
                            this.push(result);
                        }
                        else throw new Error("bad types for l");
                        break;
                    }
                    case 'L':
                        this.mainStack = [[..._.reverse(this.mainStack), ..._.reverse(this.inputStack)]];
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
                        this.print(this.peek(), false);
                        break;
                    case 'r':
                        if (isInt(this.peek())) this.push(range(0, this.popInt()));
                        else if (isArray(this.peek())) this.push(_.reverse(this.popArray()));
                        break;
                    case 'R':
                        if (isInt(this.peek())) this.push(range(1, (this.pop() as BigInteger).add(1)));
                        else this.doRegexReplace();
                        break;
                    case 's':
                        this.push(this.pop(), this.pop());
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
                            result = _.clone(result);

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
                            result = _.clone(result);

                            this.pushStackFrame();
                            while (result.length) {
                                this.push(this._ = _.last(result)!);
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
                        let shorthand = !(this.peek() instanceof Block);
                        for (let s of this.doWhile(getRest())) {
                            if (s.cancel) return;
                            yield s;
                        }
                        if (shorthand) return;
                        break;
                    }
                    case 'W':
                        let shorthand = !(this.peek() instanceof Block);
                        for (let s of this.doUnconditionalWhile(getRest())) {
                            if (s.cancel) return;
                            yield s;
                        }
                        if (shorthand) return;
                        break;
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
                    case '| ':
                        this.print(' ', false);
                        break;
                    case '|;':
                        this.push(this.index.isEven() ? zero : one);
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
                    case '|+':
                        this.runMacro('Z{+F');
                        break;
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
                                for (let e of a) result.push(...Array(b.valueOf()).fill(e));
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
                            for (let a_ of a) for (let b_ of b) result.push([a, b]);
                            this.push(result);
                        }
                        break;
                    }
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
                    case '|i':
                        this.push(this.indexOuter);
                        break;
                    case '|I':
                        for (let s of this.doFindIndexAll()) yield s;
                        break;
                    case '|m': {
                        let top = this.pop();
                        if (isNumber(top)) {
                            let next = this.pop();
                            this.push(compare(next, top) < 0 ? next : top);
                        }
                        else if (isArray(top)) this.runMacro("{|mk");
                        break;
                    }
                    case '|M': {
                        let top = this.pop();
                        if (isNumber(top)) {
                            let next = this.pop();
                            this.push(compare(next, top) > 0 ? next : top);
                        }
                        else if (isArray(top)) this.runMacro("{|Mk");
                        break;
                    }
                    case '|P':
                        this.print('');
                        break;
                    case '|q': {
                        let b = this.pop();
                        if (isNumber(b)) {
                            this.push(bigInt(Math.floor(Math.sqrt(b.valueOf()))));
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
                            this.push(Math.sqrt(b.valueOf()));
                        }
                        else if (isArray(b)) {
                            let a = this.popArray();
                            let match = RegExp(`^(${ A2S(b) })$`).exec(A2S(a));
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
                    case '|s': {
                        let search = A2S(this.popArray()), text = A2S(this.popArray());
                        this.push(text.split(new RegExp(search)).map(S2A));
                        break;
                    }
                    case '|t':
                        this.doTranslate();
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
                    case '|z':
                        this.runMacro("ss ~; '0* s 2l$ ,)"); // zero fill
                        break;
                    default:
                        throw new Error(`unknown token ${token}`);
                }
            }

            ip += token.length;
        }
    }

    private runMacro(macro: string) {
        for (let s of this.runSteps(macro)) ;
    }

    private doPlus() {
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
        if (this.totalSize() === 1) return;
        let b = this.pop(), a = this.pop();
        if (isArray(a) && isArray(b)) {
            let bArr = b;
            let result = a.filter(a_ => !_.some(bArr, b_ => areEqual(a_, b_)));
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
        if (this.totalSize() === 1) return;
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
            for (var i = 0; i < count; i++) result = result.concat(b);
            this.push(result);
        }
        else if (isArray(a) && isInt(b)) {
            let result: StaxArray = [];
            let count = b.valueOf();
            for (var i = 0; i < count; i++) result = result.concat(a);
            this.push(result);
        }
        else if (isArray(a) && isArray(b)) {
            this.push(S2A(a.map(e => isArray(e) ? A2S(e) : e.toString()).join(A2S(b))));
        }
        else if (a instanceof Block && isInt(b)) {
            let block = a, times = b.valueOf();
            for (let i = 0; i < times; i++) {
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break; 
                    yield s;
                }
            }
        }
        else if (isInt(a) && b instanceof Block) {
            let block = b, times = a.valueOf();
            for (let i = 0; i < times; i++) {
                for (let s of this.runSteps(block)) {
                    if (s.cancel) break; 
                    yield s;
                }
            }
        }
    }

    private *doSlash() {
        if (this.totalSize() === 1) return;
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
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
                result.push(_.slice(a, i, i + b.valueOf()));
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

    private doAssignIndex() {
        let element = this.pop(), indexes = this.pop();

        if (isInt(indexes)) {
            indexes = [indexes];
            while (isInt(this.peek())) indexes.unshift(this.popInt());
        }
        if (!isArray(indexes)) throw new Error("unknown index type for assign-index");
        
        function doFinalAssign(flatArr: StaxArray, index: number) {
            if (index >= flatArr.length) {
                flatArr.push(...Array(index + 1 - flatArr.length).fill(zero));
            }

            if (element instanceof Block) {
                this.push(flatArr[index]);
                let cancelled = false;
                for (let s of this.runSteps(element)) cancelled = s.cancel;
                if (!cancelled) flatArr[index] = this.pop();
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
                idx = _.last(idxPath)!.valueOf() as number;
                doFinalAssign(target, idx);
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

                doFinalAssign(result, index);
            }
        }
        this.push(result);
    }

    private doPadLeft() {
        let b = this.pop(), a = this.pop();

        if (isArray(b) && isInt(a)) [a, b] = [b, a];
        if (isInt(a)) a = stringFormat(a);

        if (isArray(a) && isInt(b)) {
            a = _.clone(a);
            let bval = b.valueOf();
            if (bval < 0) b = b.add(a.length);
            if (a.length < bval) a.unshift(..._.fill(Array(bval - a.length), zero));
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
        else throw new Error("bad types for padleft");
    }

    private *doPadRight() {
        let b = this.pop(), a = this.pop();
        
        if (isArray(b) && isInt(a)) [a, b] = [b, a];
        if (isInt(a)) a = stringFormat(a);

        if (isArray(a) && isInt(b)) {
            a = _.clone(a);
            let bval = b.valueOf();
            if (bval < 0) b = b.add(a.length);
            if (a.length < bval) a.push(..._.fill(Array(bval - a.length), zero));
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
                result.push(...data);
                result.push(...Array(top.valueOf() - result.length).fill(zero));
                this.push(result);
            }
            else if (isInt(data)) {
                // binomial coefficient
                let r = top, n = data, result = one;
                if (n.isNegative() || r.gt(n)) result = zero;
                for (let i = one; i.leq(r); i = i.add(one)) {
                    result = result.multiply(n.subtract(i)).divide(i);
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
        for (let i = 0; i < end; i++) result.push(_.slice(a, i, i + bv));
        this.push(result);
    }

    private doTrimElementsFromStart() {
        let b = this.pop(), a = this.popArray(), i = 0;

        for (; i < a.length; i++) {
            if (isArray(b)) {
                if (!_.some(b, e => areEqual(e, a[i]))) break;
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
                if (!_.some(b, e => areEqual(e, a[i]))) break;
            }
            else {
                if (!areEqual(a[i], b)) break;
            }
        }

        let result = a.slice(0, i + 1);
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
        let result = _.slice(arr, cutpoint).concat(_.slice(arr, 0, cutpoint));
        this.push(result);
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
                let match = true;
                for (let j = 0; j < target.length; j++) {
                    if (!areEqual(arr[i + j], target[j])) {
                        match = false;
                        break;
                    }
                }
                if (match) {
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
    }

    private *doFindFirst(reverse = false) {
        let pred = this.pop(), arr = this.pop(), cancelled = false;
        if (!(pred instanceof Block) || !isArray(arr)) throw new Error("bad types for find-first");

        if (reverse) {
            arr = _.clone(arr);
            arr.reverse();
        }

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

        if (!isArray(top)) throw new Error("bad types for transpose/maybe");

        if (top.length > 0 && !isArray(top[0])) top = [top];
        let result: StaxArray = [];
        let maxlen = _.max(top.map(e => (e as StaxArray).length))!;

        for (let line of top) {
            line = line as StaxArray;
            line.push(..._.fill(Array(maxlen - line.length), zero));
        }

        for (let i = 0; i < maxlen; i++) {
            let column: StaxArray = [];
            for (let row of top) column.push((row as StaxArray)[i]);
            result.push(column);
        }

        this.push(result);
    }

    private *doOrder() {
        let top = this.pop();
        if (isArray(top)) {
            this.push(_.sortBy((n: StaxValue) => n));
            return;
        }
        if (top instanceof Block) {
            let arr = this.pop();
            if (!isArray(arr)) throw new Error("expected array for order");
            let combined: {val: StaxValue, key: StaxValue}[] = [];

            this.pushStackFrame();
            for (let e of arr) {
                this.push(this._ = e);
                for (let s of this.runSteps(top)) yield s;
                combined.push({val: e, key: this.pop()});
                this.index = this.index.add(one);
            }
            this.popStackFrame();

            let result: StaxArray = _.sortBy(combined, t => t.key).map(t => t.val);
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

    private doRegexReplace() {
        let replace = this.pop(), search = this.pop(), text = this.pop();
        if (!isArray(text) || !isArray(search)) throw new Error("bad types for replace");
        let ts = A2S(text), ss = RegExp(A2S(search), "g");
        
        if (isArray(replace)) {
            let pattern
            this.push(S2A(ts.replace(ss, A2S(replace))));
        }
        else if (replace instanceof Block) {
            let replaceBlock = replace;
            this.pushStackFrame();
            let result = ts.replace(ss, m => {
                this.push(this._ = S2A(m));
                for (let s of this.runSteps(replaceBlock)) ; // todo: yield the execution states
                this.index = this.index.add(one);
                let out = this.pop();
                if (!isArray(out)) throw new Error("regex replace block didn't yield string");
                return A2S(out);
            });
            this.popStackFrame();
            this.push(S2A(result));
        }
        else throw new Error("bad types for replace");
    }

    private *doFor(rest: string) {
        if (isInt(this.peek())) {
            this.push(range(1, (this.pop() as BigInteger).add(one)));
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

    private *doUnconditionalWhile(rest: string) {
        let cancelled = false;
        let body: (Block | string) = (this.peek() instanceof Block) ? this.pop() as Block : rest;
    
        this.pushStackFrame();
        do {
            for (let s of this.runSteps(body)) {
                if (cancelled = s.cancel) break;
                yield s;
            }
        } while (!cancelled);
        this.popStackFrame();
    }

    private *doWhile(rest: string) {
        let cancelled = false;
        let body: (Block | string) = (this.peek() instanceof Block) ? this.pop() as Block : rest;
    
        this.pushStackFrame();
        do {
            for (let s of this.runSteps(body)) {
                if (cancelled = s.cancel) break;
                yield s;
            }
        } while (!cancelled && isTruthy(this.pop()));
        this.popStackFrame();
    }

    private *doReduce(rest: string) {
        let top = this.pop(), shorthand = !(top instanceof Block);
        let block: Block | string, arr: StaxValue;

        if (top instanceof Block) [block, arr] = [top, this.pop()];
        else [block, arr] = [rest, top];

        if (isInt(arr)) arr = range(one, arr);
        else if (isArray(arr)) arr = _.clone(arr);

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

    private *doCrossMap(rest: string) {
        let top = this.pop(), 
            shorthand = !(top instanceof Block), 
            map: Block | string = shorthand ? rest : top as Block;
        
        let inner = shorthand ? top : this.pop(), outer =  this.pop();
        if (isInt(inner)) inner = range(one, inner);
        if (isInt(outer)) outer = range(one, outer);
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

    private *doFilter(rest: string) {
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
        else if (isArray(this.peek())) {
            let data = this.pop() as StaxArray, cancelled = false;

            this.pushStackFrame();
            for (let e of data) {
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
        else throw new Error("bad types in filter");
    }

    private *doMap(rest: string) {
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

    private doMacroAlias(alias: string) {
        let typeTree = macroTrees[alias];
        let resPopped: StaxArray = [];
        // follow type tree as far as necessary
        while (typeTree.hasChildren()) {
            resPopped.push(this.pop());
            let type = getTypeChar(_.last(resPopped)!);
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