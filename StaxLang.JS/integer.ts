export type StaxInt = bigint;

export const cmp: (a: StaxInt, b: StaxInt) => number = (a: bigint, b: bigint) => Number(a - b);
export const add: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a + b;
export const sub: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a - b;
export const mul: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a * b;
export const div: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a / b;
export const mod: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a % b;
export const pow: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a ** b;
export const abs: (n: StaxInt) => StaxInt = (n: bigint) => (n < 0n ? -n : n);
export const gcd: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => {
        if (a < 0n) a = -a;
        if (b < 0n) b = -b;
        if (b > a) [a, b] = [b, a];
        while (a !== 0n) [a, b] = [b % a, a];
        return b;
    };
export const bitand: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a & b;
export const bitor: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a | b;
export const bitxor: (a: StaxInt, b: StaxInt) => StaxInt = (a: bigint, b: bigint) => a ^ b;
export const bitnot: (n: StaxInt) => StaxInt = (n: bigint) => ~n;
export const floatify: (n: StaxInt) => number = Number;
export function nthRoot(val: StaxInt, n: StaxInt): StaxInt {
    val = abs(val);
    if (cmp(n, 1n) < 0) n = 1n;
    let x = 1n;
    const two = 2n, shift = pow(two, n), n_1 = sub(n, 1n);
    for (let i = val; cmp(i, 0n) > 0; i = div(i, shift)) x = mul(x, two);

    do x = div(add(mul(n_1, x), div(val, pow(x, n_1))), n);
    while (!(cmp(pow(x, n), val) <= 0 && cmp(val, pow(add(x, 1n), n)) < 0));
    return x;
}
export function floorSqrt(n: StaxInt) {
    return nthRoot(n, 2n);
}
