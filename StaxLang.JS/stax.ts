import { Block, Program, parseProgram } from './block';
import * as _ from 'lodash';
import * as bigInt from 'big-integer';
import { Rational } from './rational';
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

function isFloat(n: any): n is number {
    return typeof n === "number";
}
function isInt(n: any): n is BigInteger {
    return bigInt.isInstance(n);
}
function isArray(n: StaxValue | string): n is StaxArray {
    return Array.isArray(n);
}
function isNumber(n: StaxValue): n is StaxNumber {
    return isInt(n) || isFloat(n) || n instanceof Rational;
}

function range(start: number | BigInteger, end: number | BigInteger): StaxArray {
    return _.range(start.valueOf(), end.valueOf()).map(bigInt);
}

function S2A(s: string): StaxArray {
    let result: StaxArray = [];
    for (let i = 0; i < s.length; i++) result.push(bigInt(s.charCodeAt(i)));
    return result;
}

function A2S(a: StaxArray): string {
    let result = "";
    for (let e of a) {
        if (isInt(e)) result += String.fromCodePoint(e.valueOf());
        else if (isArray(e)) result += A2S(e);
        else throw `can't convert ${e} to string`;
    }
    return result;
}

function isTruthy(a: StaxValue): boolean {
    if (isNumber(a)) return floatify(a) !== 0;
    return isArray(a) && a.length > 0;
}

function floatify(num: StaxNumber): number {
    if (isInt(num)) return num.valueOf();
    if (num instanceof Rational) return num.valueOf();
    return num;
}

function widenNumbers(...nums: StaxNumber[]): StaxNumber[] {
    if (_.some(nums, isFloat)) {
        return _.map(nums, floatify);
    }
    if (_.some(nums, n => n instanceof Rational)) {
        return _.map(nums, n => n instanceof Rational ? n : new Rational(n as BigInteger, minusOne));
    }
    return nums;
}

type StaxNumber = number | BigInteger | Rational;
type StaxValue = StaxNumber | Block | StaxArray;
interface StaxArray extends Array<StaxValue> { }

export class Runtime {
    private lineOut: (line: string) => void;
    private outBuffer = ""; // unterminated line output
    private mainStack: StaxArray = [];
    private inputStack: StaxArray = [];
    private producedOutput = false;

    private callStackFrames: {_: StaxValue, indexOuter: BigInteger}[] = [];
    private _: StaxValue;
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
            this.lineOut(this.outBuffer + val);
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
                    newValue(S2A(arg.substring(i + 1, finishPos - i - 1)));
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

    private *runSteps(block: Block | string) {
        if (typeof block === "string") block = parseProgram(block);

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
                else if (token[0] === "'" || token[0] === ".") this.push(S2A(token.substr(1)));
                else switch (token) {
                    case ' ':
                        break;
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
                        this.doStar();
                        break;
                    case '/':
                        this.doSlash();
                        break;
                    case '[': {
                        let b = this.pop(), a = this.peek();
                        this.push(a, b);
                        break;
                    }
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
                    case 'F':
                        for (let s of this.doFor(getRest())) ;
                        break;
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
                    case 'n':
                        this.push(this.pop(), this.peek());
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
                    case 's':
                        this.push(this.pop(), this.pop());
                        break;
                    case 'U':
                        this.push(bigInt[-1]);
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
                    case '|+':
                        this.runMacro('Z{+F');
                        break;
                    case '|d':
                        this.push(bigInt(this.mainStack.length));
                        break;
                    case '|D':
                        this.push(bigInt(this.inputStack.length));
                        break;
                    case '|P':
                        this.print('');
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
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = widenNumbers(a, b);
            if (isFloat(a) && isFloat(b)) result = a - b;
            else if (a instanceof Rational && b instanceof Rational) result = a.subtract(b);
            else if (isInt(a) && isInt(b)) result = a.subtract(b);
            else throw "weird types or something; can't subtract?"
            this.push(result);
        }
    }

    private doStar() {
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
    }

    private doSlash() {
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
    }

    private *doFor(rest: string) {
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
        else throw new Error("bad types in for")
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
                        for (let s of this.runSteps(token[i]));
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