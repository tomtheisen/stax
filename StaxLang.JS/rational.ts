import * as bigInt from 'big-integer';    

export class Rational {
    public numerator: bigInt.BigInteger;
    public denominator: bigInt.BigInteger;

    public valueOf(): number {
        return this.numerator.valueOf() / this.denominator.valueOf();
    }

    public toString(): string {
        return `${this.numerator}/${this.denominator}`;
    }

    constructor(num: bigInt.BigInteger, den: bigInt.BigInteger) {
        this.numerator = num;
        this.denominator = den;
        this.reduce();
    }

    invert() {
        return new Rational(this.denominator, this.numerator);
    }

    add(other: Rational): Rational {
        return new Rational(
            this.numerator.multiply(other.denominator).add(other.numerator.multiply(this.denominator)),
            this.denominator.multiply(other.denominator));
    }

    subtract(other: Rational) {
        return new Rational(
            this.numerator.multiply(other.denominator).subtract(other.numerator.multiply(this.denominator)),
            this.denominator.multiply(other.denominator));
    }

    multiply(other: Rational): Rational {
        return new Rational (
            this.numerator.multiply(other.numerator),
            this.denominator.multiply(other.denominator));
    }

    divide(other: Rational) {
        return new Rational (
            this.numerator.multiply(other.denominator),
            this.denominator.multiply(other.numerator));
    }

    negate() {
        return new Rational(this.numerator.negate(), this.denominator);
    }

    abs() {
        return new Rational(this.numerator.abs(), this.denominator.abs());
    }

    floor() {
        if (this.numerator.isNegative()) return this.numerator.subtract(this.denominator).add(bigInt.one).divide(this.denominator);
        return this.numerator.divide(this.denominator);
    }

    ceiling() {
        return this.negate().floor().negate();
    }

    mod(other: Rational) {
        let intPart = this.divide(other).floor();
        return this.subtract(other.multiply(new Rational(intPart, bigInt.one)));
    }

    equals(other: Rational) {
        return this.numerator.eq(other.numerator) && this.denominator.eq(other.denominator);
    }

    private reduce() {
        if (this.denominator.isZero()) throw "rational divide by zero";
        let gcd = bigInt.gcd(this.numerator, this.denominator);
        this.numerator = this.numerator.divide(gcd);
        this.denominator = this.denominator.divide(gcd);
    }
}

export const zero = new Rational(bigInt.zero, bigInt.one);
export const one = new Rational(bigInt.one, bigInt.one);