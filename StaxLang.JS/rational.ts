import { StaxInt } from './integer'
import * as int from './integer'
import { floatify } from './types';

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

export function rationalize(arg: number): Rational {
    const significantBits = 50, two = int.make(2), epsilon = arg / 2 ** significantBits;;

    if (arg < 0) return rationalize(-arg).negate();
    if (arg % 1 == 0) return new Rational(int.make(arg), int.one);

    type rat = [StaxInt, StaxInt];
    let
        left: rat = [int.make(Math.floor(arg)), int.one],
        right: rat = [int.make(Math.ceil(arg)), int.one],
        best: rat = [int.zero, int.one], mediant: rat;
    let bestError = Number.POSITIVE_INFINITY, lastMove = int.one;

    do { // Stern-Brocot binary search
        mediant = int.cmp(lastMove, int.zero) < 0
            ? [int.sub(right[0], int.mul(lastMove, left[0])), int.sub(right[1], int.mul(lastMove, left[1]))]
            : [int.add(left[0], int.mul(lastMove, right[0])), int.add(left[1], int.mul(lastMove, right[1]))];

        let error = floatify(mediant[0]) / floatify(mediant[1]) - arg;
        if (isNaN(error)) error = 0;

        if (error > 0) {
            if (int.cmp(lastMove, int.zero) < 0) {
                right = mediant;
                lastMove = int.mul(lastMove, two)
            }
            else {
                lastMove = int.div(lastMove, two);
                if (int.eq(lastMove, int.zero)) lastMove = int.minusOne;
            }
        }
        else {
            if (int.cmp(lastMove, int.zero) > 0) {
                left = mediant;
                lastMove = int.mul(lastMove, two)
            }
            else {
                lastMove = int.div(lastMove, two);
                if (int.eq(lastMove, int.zero)) lastMove = int.one;
            }
        }
        if (Math.abs(error) < bestError) [best, bestError] = [mediant, Math.abs(error)];
    } while (bestError > epsilon);
    return new Rational(best[0], best[1]);
}

export const zero = new Rational(int.zero, int.one);
export const one = new Rational(int.one, int.one);