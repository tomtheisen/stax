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
export function nthRoot(val: StaxInt, n: StaxInt): StaxInt {
    val = abs(val);
    if (cmp(n, one) < 0) n = one;
    let x = one;
    const two = make(2), shift = pow(two, n), n_1 = sub(n, one);
    for (let i = val; cmp(i, zero) > 0; i = div(i, shift)) x = mul(x, two);

    do {
        x = div(add(mul(n_1, x), div(val, pow(x, n_1))), n);
    } while (!(cmp(pow(x, n), val) <= 0 && cmp(val, pow(add(x, one), n)) < 0));
    return x;
}
export function floorSqrt(n: StaxInt) {
    return nthRoot(n, make(2));
}