import { Block, Program, parseProgram } from './block';
import * as _ from 'lodash';
import * as bigInt from 'big-integer';
import { Rational } from './rational';

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
function isInt(n: any): n is bigInt.BigInteger {
    return bigInt.isInstance(n);
}
function isArray(n: StaxValue | string): n is StaxArray {
    return Array.isArray(n);
}
function isNumber(n: StaxValue): n is StaxNumber {
    return isInt(n) || isFloat(n) || n instanceof Rational;
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

function broadenNums(...nums: StaxNumber[]): StaxNumber[] {
    if (_.some(nums, isFloat)) {
        return _.map(nums, floatify);
    }
    if (_.some(nums, n => n instanceof Rational)) {
        return _.map(nums, n => n instanceof Rational ? n : new Rational(n as bigInt.BigInteger, minusOne));
    }
    return nums;
}

type StaxNumber = number | bigInt.BigInteger | Rational;
type StaxValue = StaxNumber | Block | StaxArray;
interface StaxArray extends Array<StaxValue> { }

export class Runtime {
    private lineOut: (line: string) => void;
    private outBuffer = ""; // unterminated line output
    private mainStack: StaxArray = [];
    private inputStack: StaxArray = [];
    private producedOutput = false;

    constructor(output: (line: string) => void) {
        this.lineOut = output;
    }

    private push(...vals: StaxValue[]) {
        vals.forEach(this.mainStack.push);
    }

    private peek(): StaxValue {
        return _.last(this.mainStack) || _.last(this.inputStack) || fail("peeked empty stacks");
    }

    private pop(): StaxValue {
        return this.mainStack.pop() || this.inputStack.pop() || fail("popped empty stacks");
    }

    private print(val: StaxValue | string, newline = true) {
        this.producedOutput = true;

        if (isInt(val) || typeof val === "number") val = val.toString();
        if (val instanceof Block) val = `Block: ${val.contents}`;
        if (isArray(val)) val = A2S(val);

        if (newline) {
            this.lineOut(this.outBuffer + val);
            this.outBuffer = "";
        }
        else {
            this.outBuffer += val;
        }
    }

    public *runProgram(program: string) {
        for (let s of this.runSteps(program)) yield s;

        if (this.outBuffer) this.print("");
        if (!this.producedOutput) this.print(this.pop());
    }

    private *runSteps(block: Block | string) {
        if (typeof block === "string") block = parseProgram(block);

        let ip = 0;
        for (let token of block.tokens) {
            yield new ExecutionState(ip);

            if (token instanceof Block) {
                this.push(token);
                continue;
            }
            else {
                if (!!token[0].match(/\d+!/)) this.push(parseFloat(token.replace("!", ".")));
                else if (!!token[0].match(/\d/)) this.push(bigInt(token));
                else if (token[0] === '"') this.evaluateStringToken(token);
                else if (token[0] === "'" || token[0] === ".") this.push(S2A(token.substr(1)));
                switch (token) {
                    case ' ':
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
                    case 'a':
                        {
                            let c = this.pop(), b = this.pop(), a = this.pop();
                            this.push(b, c, a);
                        }
                        break;
                    case 'b':
                        {
                            let b = this.pop(), a = this.peek();
                            this.push(b, a, b);
                        }
                        break;
                    case 'c':
                        this.push(this.peek());
                        break;
                    case 'd':
                        this.pop();
                        break;
                    case 'q':
                        this.print(this.peek(), false);
                        break;
                    case 'Q':
                        this.print(this.peek(), false);
                        break;
                    case 'p':
                        this.print(this.pop(), false);
                        break;
                    case 'P':
                        this.print(this.pop());
                        break;
                }
            }

            ip += token.length;
        }
    }

    private doPlus() {
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = broadenNums(a, b);
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
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = broadenNums(a, b);
            if (isFloat(a) && isFloat(b)) result = a - b;
            else if (a instanceof Rational && b instanceof Rational) result = a.subtract(b);
            else if (isInt(a) && isInt(b)) result = a.subtract(b);
            else throw "weird types or something; can't subtract?"
            this.push(result);
        }
    }

    private doStar() {
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = broadenNums(a, b);
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
    }

    private doSlash() {
        let b = this.pop(), a = this.pop();
        if (isNumber(a) && isNumber(b)) {
            let result: StaxNumber;
            [a, b] = broadenNums(a, b);
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

    private evaluateStringToken(token: string) {
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