import * as bigInt from 'big-integer';

export class Rational {
    public numerator: bigInt.BigInteger;
    public denominator: bigInt.BigInteger;

    public valueOf(): number {
        return this.numerator.valueOf() / this.denominator.valueOf();
    }

    constructor(num: bigInt.BigInteger, den: bigInt.BigInteger) {
        this.numerator = num;
        this.denominator = den;
        this.reduce;
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

    private reduce() {
        if (this.denominator.isZero()) throw "rational divide by zero";
        let gcd = bigInt.gcd(this.numerator, this.denominator);
        this.numerator = this.numerator.divide(gcd);
        this.denominator = this.denominator.divide(gcd);
    }
}

export const zero = new Rational(bigInt.zero, bigInt.one);
export const one = new Rational(bigInt.one, bigInt.one);