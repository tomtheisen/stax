import { abs } from './integer';
import { compare, last } from './types';

export function primeFactors(n: bigint): bigint[] {
    let result: bigint[] = [];

    n = abs(n);
    if (n.valueOf() <= 1) return result;
    for (let d of allPrimes()) {
        while (n % d === 0n) {
            result.push(d);
            n /= d;
        }
        if (n === 1n) return result;
        if (d * d > n) {
            result.push(n);
            return result;
        }
    }
    throw new Error("Ran out of primes...?");
}

let primes: bigint[] = [2n, 3n];

export function *allPrimes() {
    for (let p of primes) yield p;
    while (true) yield addPrime();
}

function addPrime(): bigint {
    for (let c = last(primes)! + 2n;; c += 2n) {
        for (let p of allPrimes()) {
            if (c % p === 0n) break;
            if (p ** 2n > c) {
                primes.push(c);
                return c;
            }
        }
    }
}

export function indexOfPrime(p: bigint): number {
    if (p <= last(primes)!) {
        // binary search
        for (let lo = 0, hi = primes.length;lo < hi; ) {
            const mid = lo + hi >> 1;
            const cmp = compare(p, primes[mid]);
            if (cmp === 0) return mid;
            if (cmp < 0) hi = mid; else lo = mid + 1;
        }
        return -1;
    }
    else for (let i = primes.length; ; i++) {
        const cmp = compare(p, addPrime());
        if (cmp < 0) return -1;
        if (cmp === 0) return i;
    }
}
