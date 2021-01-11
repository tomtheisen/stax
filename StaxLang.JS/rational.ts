import { StaxInt } from './integer'
import * as int from './integer'
import { floatify } from './types';

export class Rational {
    public numerator: StaxInt;
    public denominator: StaxInt;

    public valueOf(): number {
        return Number(Number(this.numerator) / Number(this.denominator));
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
            this.numerator * other.denominator + other.numerator * this.denominator,
            this.denominator * other.denominator);
    }

    subtract(other: Rational) {
        return new Rational(
            this.numerator * other.denominator - other.numerator * this.denominator,
            this.denominator * other.denominator);
    }

    multiply(other: Rational): Rational {
        return new Rational (
            this.numerator * other.numerator,
            this.denominator * other.denominator);
    }

    divide(other: Rational) {
        return new Rational (
            this.numerator * other.denominator,
            this.denominator * other.numerator);
    }

    negate() {
        return new Rational(-this.numerator, this.denominator);
    }

    abs() {
        return new Rational(int.abs(this.numerator), int.abs(this.denominator));
    }

    floor() {
        if (int.cmp(this.numerator, 0n) < 0) {
            return (this.numerator - this.denominator + 1n) / this.denominator;
        }
        return this.numerator / this.denominator;
    }

    ceiling() {
        return -this.negate().floor();
    }

    mod(other: Rational) {
        other = other.abs();
        let intPart = this.divide(other).floor();
        return this.subtract(other.multiply(new Rational(intPart, 1n)));
    }

    equals(other: Rational) {
        return this.numerator === other.numerator && this.denominator === other.denominator;
    }

    private reduce() {
        if (this.denominator === 0n) throw new Error("rational divide by zero");
        let gcd = int.gcd(this.numerator, this.denominator);
        this.numerator /= gcd;
        this.denominator /= gcd;
        if (int.cmp(this.denominator, 0n) < 0) {
            this.numerator = -this.numerator;
            this.denominator = -this.denominator;
        }
    }
}

export function rationalize(arg: number): Rational {
    const significantBits = 50, epsilon = arg / 2 ** significantBits;;

    if (arg < 0) return rationalize(-arg).negate();
    if (arg % 1 == 0) return new Rational(BigInt(arg), 1n);

    type rat = [StaxInt, StaxInt];
    let
        left: rat = [BigInt(Math.floor(arg)), 1n],
        right: rat = [BigInt(Math.ceil(arg)), 1n],
        best: rat = [0n, 1n], mediant: rat;
    let bestError = Number.POSITIVE_INFINITY, lastMove = 1n;

    do { // Stern-Brocot binary search
        mediant = int.cmp(lastMove, 0n) < 0
            ? [right[0] - lastMove * left[0], right[1] - lastMove * left[1]]
            : [left[0] + lastMove * right[0], left[1] + lastMove * right[1]];

        let error = floatify(mediant[0]) / floatify(mediant[1]) - arg;
        if (isNaN(error)) error = 0;

        if (error > 0) {
            if (int.cmp(lastMove, 0n) < 0) {
                right = mediant;
                lastMove *= 2n;
            }
            else {
                lastMove /= 2n;
                if (lastMove === 0n) lastMove = -1n;
            }
        }
        else {
            if (int.cmp(lastMove, 0n) > 0) {
                left = mediant;
                lastMove *= 2n;
            }
            else {
                lastMove /= 2n;
                if (lastMove === 0n) lastMove = 1n;
            }
        }
        if (Math.abs(error) < bestError) [best, bestError] = [mediant, Math.abs(error)];
    } while (bestError > epsilon);
    return new Rational(best[0], best[1]);
}

export const zero = new Rational(0n, 1n);
export const one = new Rational(1n, 1n);