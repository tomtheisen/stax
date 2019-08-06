import * as npm_bigInt from 'big-integer';

export type StaxInt = npm_bigInt.BigInteger | bigint;

const disableNativeBigInt = typeof process !== "undefined" && process.argv.includes("--nobigint");
// Chrome's bigint is *slower* than big-integer
// set this to false if you want to use npm big-integer everywhere
export const usingNativeBigInt = !disableNativeBigInt && typeof BigInt === "function";

export const make: (n: number | string) => StaxInt = usingNativeBigInt ? BigInt : npm_bigInt;
export const zero = make(0), one = make(1), minusOne = make(-1);
export const isInt: (n: any) => n is StaxInt = usingNativeBigInt 
    ? (n => typeof n === "bigint") as (n: any) => n is StaxInt
    : npm_bigInt.isInstance as (n: any) => n is StaxInt;
export const cmp: (a: StaxInt, b: StaxInt) => number = usingNativeBigInt 
    ? (a: bigint, b: bigint) => Number(a - b)
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.minus(b).valueOf();
export const eq: (a: StaxInt, b: StaxInt) => boolean = usingNativeBigInt
    ? (a, b) => a === b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.eq(b);
export const add: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a + b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.add(b);
export const sub: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a - b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.subtract(b);
export const mul: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a * b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.multiply(b);
export const div: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a / b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.divide(b);
export const mod: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a % b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.mod(b);
export const pow: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a ** b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.pow(b);
export const negate: (n: StaxInt) => StaxInt = usingNativeBigInt
    ? (n: bigint) => -n
    : (n: npm_bigInt.BigInteger) => n.negate();
export const abs: (n: StaxInt) => StaxInt = usingNativeBigInt
    ? (n: bigint) => (n < zero ? -n : n)
    : (n: npm_bigInt.BigInteger) => n.abs();
export const gcd: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => {
        if (a < zero) a = -a;
        if (b < zero) b = -b;
        if (b > a) [a, b] = [b, a];
        while (a !== zero) [a, b] = [b % a, a];
        return b;
    }
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => npm_bigInt.gcd(a, b);
export const bitand: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a & b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.and(b);
export const bitor: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a | b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.or(b);
export const bitxor: (a: StaxInt, b: StaxInt) => StaxInt = usingNativeBigInt
    ? (a: bigint, b: bigint) => a ^ b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.xor(b);
export const bitnot: (n: StaxInt) => StaxInt = usingNativeBigInt
    ? (n: bigint) => ~n
    : (n: npm_bigInt.BigInteger) => n.not();
export const floatify: (n: StaxInt) => number = usingNativeBigInt
    ? Number
    : (n: npm_bigInt.BigInteger) => n.valueOf();
export const floorSqrt: (n: StaxInt) => StaxInt = usingNativeBigInt
    ? (n: bigint) => {
        if (n < 0) throw Error("Can't sqrt negative");
        if (n === zero) return zero;
        const one = BigInt(1), two = BigInt(2), four = BigInt(4);
        for (var next = one, start = n; start > one; start /= four) next *= two;
        let last: bigint;
        do {
            [last, next] = [next, (next + n / next) / two];
        } while (next !== last && next !== last - one || next * next > n);
        return next;
    }
    : (n: npm_bigInt.BigInteger) => {
        if (n.lt(0)) throw Error("Can't sqrt negative");
        if (n.eq(0)) return zero;

        for (var next = npm_bigInt[1], start = n; start.gt(1); start = start.divide(4)) next = next.multiply(2);
        let last: npm_bigInt.BigInteger;
        do {
            last = next;
            next = last.add(n.divide(last)).divide(2);
        } while (next.neq(last) && next.neq(last.subtract(1)) || next.multiply(next).gt(n));
        return next;
    }