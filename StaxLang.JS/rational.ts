import { StaxInt } from './integer'
import * as int from './integer'

export class Rational {
    public numerator: StaxInt;
    public denominator: StaxInt;

    public valueOf(): number {
        return Number(int.floatify(this.numerator) / int.floatify(this.denominator));
    }

    public toString(): string {
        return `${this.numerator}/${this.denominator}`;
    }

    constructor(num: StaxInt, den: StaxInt) {
        this.numerator = num;
        this.denominator = den;
        this.reduce();
    }

    invert() {
        return new Rational(this.denominator, this.numerator);
    }

    add(other: Rational): Rational {
        return new Rational(
            int.add(int.mul(this.numerator, other.denominator), int.mul(other.numerator, this.denominator)),
            int.mul(this.denominator, other.denominator));
    }

    subtract(other: Rational) {
        return new Rational(
            int.sub(int.mul(this.numerator, other.denominator), int.mul(other.numerator, this.denominator)),
            int.mul(this.denominator, other.denominator));
    }

    multiply(other: Rational): Rational {
        return new Rational (
            int.mul(this.numerator, other.numerator),
            int.mul(this.denominator, other.denominator));
    }

    divide(other: Rational) {
        return new Rational (
            int.mul(this.numerator, other.denominator),
            int.mul(this.denominator, other.numerator));
    }

    negate() {
        return new Rational(int.negate(this.numerator), this.denominator);
    }

    abs() {
        return new Rational(int.abs(this.numerator), int.abs(this.denominator));
    }

    floor() {
        if (int.cmp(this.numerator, int.zero) < 0) {
            return int.div(int.add(int.sub(this.numerator, this.denominator), int.one), this.denominator);
        }
        return int.div(this.numerator, this.denominator);
    }

    ceiling() {
        return int.negate(this.negate().floor());
    }

    mod(other: Rational) {
        other = other.abs();
        let intPart = this.divide(other).floor();
        return this.subtract(other.multiply(new Rational(intPart, int.one)));
    }

    equals(other: Rational) {
        return int.eq(this.numerator, other.numerator) && int.eq(this.denominator, other.denominator);
    }

    private reduce() {
        if (int.eq(this.denominator, int.zero)) throw new Error("rational divide by zero");
        let gcd = int.gcd(this.numerator, this.denominator);
        this.numerator = int.div(this.numerator, gcd);
        this.denominator = int.div(this.denominator, gcd);
        if (int.cmp(this.denominator, int.zero) < 0) {
            this.numerator = int.negate(this.numerator);
            this.denominator = int.negate(this.denominator);
        }
    }
}

class BoundedFloat {
    value: number;
    error: number;
    get min() { return this.value - this.error; }
    get max() { return this.value + this.error; }
    get sound() {
        if (isNaN(this.value)) return false;
        if (!Number.isFinite(this.error)) return false;
        if (this.error < 0) return false;
        if (this.error === 0) return true;
        if (this.error >= Math.abs(this.value)) return false;
        return true;
    }

    static readonly catastrophicLoss = new BoundedFloat(Number.NaN, Number.POSITIVE_INFINITY);

    constructor(value: number, error: number) {
        if (error < 0) throw new Error("invalid negative error bound");
        this.value = value;
        this.error = error;
    }

    public static estimate(arg: number, sigbits: number) {
        return new BoundedFloat(arg, arg * Math.pow(0.5, sigbits));
    }
    
    sub(other: BoundedFloat) {
        return new BoundedFloat(this.value - other.value, this.error + other.error);
    }
    mul(num: number) {
        return new BoundedFloat(this.value * num, Math.abs(num) * this.error);
    }
    div(other: BoundedFloat) {
        return new BoundedFloat(this.value/other.value,
            (Math.abs(this.value) * other.error + Math.abs(other.value) * this.error) / (other.value * other.min));
    }
    mod(other: BoundedFloat) {
        const div = this.div(other);
        if (Math.floor(div.max) != Math.floor(div.min)) return BoundedFloat.catastrophicLoss;
        return this.sub(other.mul(Math.floor(div.value)));
    }
}

export function rationalize(arg: number): Rational {
    const round = (d: number) => Math.floor(d + 0.5);

    if (arg == 0) return zero;
    if (arg < 0) return rationalize(-arg).negate();

    let a = new BoundedFloat(1, 0), b = BoundedFloat.estimate(arg, 56);
    let bestn = 0, bestd = 1, besterr = Number.POSITIVE_INFINITY;
    do {
        var oldb = b;
        [a, b] = [b.mod(a), a];
        let denlo = round(1 / b.max), denhi = round(1 / b.min);
        for (let den = denlo; den <= denhi; den++) {
            let num = round(arg * den), err = Math.abs(arg - num / den);
            if (err < besterr) {
                [besterr, bestn, bestd] = [err, num, den];
                if (err == 0) return new Rational(int.make(bestn), int.make(bestd));
                let newError = 1 / den - 1 / (den + 1);
                if (newError < b.error) {
                    b = new BoundedFloat(1 / den, newError);
                    a = oldb.mod(b);
                }
            }
        }
    } while (a.sound);
    return new Rational(int.make(bestn), int.make(bestd));
}

export const zero = new Rational(int.zero, int.one);
export const one = new Rational(int.one, int.one);