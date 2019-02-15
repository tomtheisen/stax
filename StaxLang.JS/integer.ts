import * as npm_bigInt from 'big-integer';

export type StaxInt = npm_bigInt.BigInteger | bigint;
const nativeBigIntSupport = "BigInt" in window;
export const make: (n: number | string) => StaxInt = nativeBigIntSupport ? BigInt : npm_bigInt;
export const zero = make(0), one = make(1), minusOne = make(-1);
export const isInt: (n: any) => n is StaxInt = nativeBigIntSupport 
    ? (n => typeof n === "bigint") as (n: any) => n is StaxInt
    : npm_bigInt.isInstance as (n: any) => n is StaxInt;
export const compare: (a: StaxInt, b: StaxInt) => number = nativeBigIntSupport 
    ? (a: bigint, b: bigint) => Number(a - b)
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.minus(b).valueOf();
export const eq: (a: StaxInt, b: StaxInt) => boolean = nativeBigIntSupport
    ? (a, b) => a === b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.eq(b);
export const add: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a + b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.add(b);
export const sub: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a - b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.subtract(b);
export const mul: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a * b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.multiply(b);
export const div: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a / b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.divide(b);
export const mod: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a % b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.mod(b);
export const pow: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a ** b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.pow(b);
export const negate: (n: StaxInt) => StaxInt = nativeBigIntSupport
    ? (n: bigint) => -n
    : (n: npm_bigInt.BigInteger) => n.negate();
export const abs: (n: StaxInt) => StaxInt = nativeBigIntSupport
    ? (n: bigint) => (n < zero ? -n : n)
    : (n: npm_bigInt.BigInteger) => n.abs();
export const gcd: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => {
        if (a < zero) a = -a;
        if (b < zero) b = -b;
        if (b > a) [a, b] = [b, a];
        while (a !== zero) [a, b] = [b % a, a];
        return b;
    }
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => npm_bigInt.gcd(a, b);
export const bitand: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a & b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.and(b);
export const bitor: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a | b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.or(b);
export const bitxor: (a: StaxInt, b: StaxInt) => StaxInt = nativeBigIntSupport
    ? (a: bigint, b: bigint) => a ^ b
    : (a: npm_bigInt.BigInteger, b: npm_bigInt.BigInteger) => a.xor(b);
export const bitnot: (n: StaxInt) => StaxInt = nativeBigIntSupport
    ? (n: bigint) => ~n
    : (n: npm_bigInt.BigInteger) => n.not();

