import { abs, gcd } from './integer';
import { floatify } from './types';

export class Rational {
    public numerator: bigint;
    public denominator: bigint;

    public valueOf(): number {
        return Number(Number(this.numerator) / Number(this.denominator));
    }

    public toString(): string {
        return `${this.numerator}/${this.denominator}`;
    }

    constructor(num: bigint, den: bigint) {
        this.numerator = num;
        this.denominator = den;
        this.reduce();
    }

    invert() {
        if (this.numerator === 0n) throw new Error("rational divide by zero");
        return this.numerator < 0n
            ? Rational.fromReduced(-this.denominator, -this.numerator)
            : Rational.fromReduced(this.denominator, this.numerator);
    }

    add(other: Rational): Rational {
        return this.combine(other.numerator, other.denominator);
    }

    subtract(other: Rational): Rational {
        return this.combine(-other.numerator, other.denominator);
    }

    multiply(other: Rational): Rational {
        return new Rational(
            this.numerator * other.numerator,
            this.denominator * other.denominator);
    }

    divide(other: Rational) {
        return new Rational(
            this.numerator * other.denominator,
            this.denominator * other.numerator);
    }

    negate() {
        return Rational.fromReduced(-this.numerator, this.denominator);
    }

    abs() {
        return Rational.fromReduced(abs(this.numerator), this.denominator);
    }

    floor() {
        if (this.numerator < 0n) {
            return (this.numerator - this.denominator + 1n) / this.denominator;
        }
        return this.numerator / this.denominator;
    }

    ceiling() {
        if (this.numerator > 0n) {
            return (this.numerator + this.denominator - 1n) / this.denominator;
        }
        return this.numerator / this.denominator;
    }

    mod(other: Rational) {
        other = other.abs();
        let intPart = this.divide(other).floor();
        return this.subtract(other.multiply(new Rational(intPart, 1n)));
    }

    equals(other: Rational) {
        return this.numerator === other.numerator && this.denominator === other.denominator;
    }

    private combine(otherNumerator: bigint, otherDenominator: bigint): Rational {
        if (this.denominator === otherDenominator) {
            return new Rational(
                this.numerator + otherNumerator, this.denominator);
        }
        // Adding or subtracting an integer cannot introduce a common factor
        if (otherDenominator === 1n) {
            return Rational.fromReduced(
                this.numerator + otherNumerator * this.denominator,
                this.denominator);
        }
        if (this.denominator === 1n) {
            return Rational.fromReduced(
                this.numerator * otherDenominator + otherNumerator,
                otherDenominator);
        }
        // Use denominator GCDs for large fractions. Direct reduction is
        // cheaper for small operands and near cancellation
        if (this.denominator > 0xffffffffffffffffn) {
            const numerator = this.numerator * otherDenominator +
                otherNumerator * this.denominator;
            return abs(numerator) < this.denominator
                ? new Rational(
                    numerator, this.denominator * otherDenominator)
                : Rational.fromLargeSum(
                    numerator, this.denominator, otherDenominator);
        }
        return new Rational(
            this.numerator * otherDenominator +
            otherNumerator * this.denominator,
            this.denominator * otherDenominator);
    }

    // Skip reduction only for rationals that we know have
    // coprime values with a positive denominator
    private static fromReduced(num: bigint, den: bigint): Rational {
        const result = new Rational(num, 1n);
        result.denominator = den;
        return result;
    }

    private static fromLargeSum(
        numerator: bigint, den: bigint, otherDen: bigint): Rational {
        const common = gcd(den, otherDen);
        if (common === 1n) {
            return Rational.fromReduced(numerator, den * otherDen);
        }
        const num = numerator / common, divisor = gcd(num, common);
        return Rational.fromReduced(
            num / divisor, (den / common) * (otherDen / divisor));
    }

    private reduce() {
        if (this.denominator === 1n) return;
        if (this.denominator === 0n) throw new Error("rational divide by zero");
        if (this.numerator === 0n) {
            this.denominator = 1n;
            return;
        }
        const divisor = gcd(this.numerator, this.denominator);
        this.numerator /= divisor;
        this.denominator /= divisor;
        if (this.denominator < 0n) {
            this.numerator = -this.numerator;
            this.denominator = -this.denominator;
        }
    }
}

export function rationalize(arg: number): Rational {
    const significantBits = 50, epsilon = arg / 2 ** significantBits;;

    if (arg < 0) return rationalize(-arg).negate();
    if (arg % 1 == 0) return new Rational(BigInt(arg), 1n);

    type rat = [bigint, bigint];
    let
        left: rat = [BigInt(Math.floor(arg)), 1n],
        right: rat = [BigInt(Math.ceil(arg)), 1n],
        best: rat = [0n, 1n], mediant: rat;
    let bestError = Number.POSITIVE_INFINITY, lastMove = 1n;

    do { // Stern-Brocot binary search
        mediant = lastMove < 0n
            ? [right[0] - lastMove * left[0], right[1] - lastMove * left[1]]
            : [left[0] + lastMove * right[0], left[1] + lastMove * right[1]];

        let error = floatify(mediant[0]) / floatify(mediant[1]) - arg;
        if (isNaN(error)) error = 0;

        if (error > 0) {
            if (lastMove < 0n) {
                right = mediant;
                lastMove *= 2n;
            }
            else {
                lastMove /= 2n;
                if (lastMove === 0n) lastMove = -1n;
            }
        }
        else {
            if (lastMove > 0n) {
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