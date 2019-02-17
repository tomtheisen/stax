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

export const zero = new Rational(int.zero, int.one);
export const one = new Rational(int.one, int.one);