export const abs: (n: bigint) => bigint = (n: bigint) => (n < 0n ? -n : n);
export function gcd(a: bigint, b: bigint): bigint {
    if (a < 0n) a = -a;
    if (b < 0n) b = -b;
    // Strip powers of two when either operand is even
    if (a !== 0n && b !== 0n &&
        ((a & 1n) === 0n || (b & 1n) === 0n)) {
        const aTwos = a & -a, bTwos = b & -b;
        a /= aTwos;
        b /= bTwos;
        while (a !== 0n) [a, b] = [b % a, a];
        return b * (aTwos < bTwos ? aTwos : bTwos);
    }
    while (a !== 0n) [a, b] = [b % a, a];
    return b;
}

export function nthRoot(val: bigint, n: bigint): bigint {
    val = abs(val);
    if (n < 1n) n = 1n;
    let x = 1n;
    const shift = 2n ** n;
    for (let i = val; i > 0n; i /= shift) x *= 2n;

    do x = ((n - 1n) * x + val / x ** (n - 1n)) / n;
    while (!(x ** n <= val && val < (x + 1n) ** n));
    return x;
}
export function floorSqrt(n: bigint) {
    return nthRoot(n, 2n);
}
