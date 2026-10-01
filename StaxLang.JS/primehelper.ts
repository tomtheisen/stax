import { abs } from './integer';
import { compare } from './types';

export function primeFactors(n: bigint): bigint[] {
    let result: bigint[] = [];

    n = abs(n);
    if (n.valueOf() <= 1) return result;
    let primeIndex = 0;
    while (n > 1) {
        if (primes.length <= primeIndex) {
            addPrime();
        }

        const prime = primes[primeIndex];
        while (n % prime === 0n) {
            result.push(prime);
            n /= prime;
        }

        if (n === 1n) return result;

        if (prime * prime > n) {
            result.push(n);
            return result;
        }

        primeIndex += 1
    }

    return result
}

let primes: bigint[] = [2n, 3n, 5n, 7n, 11n, 13n, 17n, 19n, 23n, 29n, 31n,
    37n, 41n, 43n, 47n, 53n, 59n, 61n, 67n, 71n, 73n, 79n, 83n, 89n, 97n,
    101n, 103n, 107n, 109n, 113n, 127n, 131n, 137n, 139n, 149n, 151n, 157n,
    163n, 167n, 173n, 179n, 181n, 191n, 193n, 197n, 199n, 211n, 223n, 227n,
    229n, 233n, 239n, 241n, 251n, 257n, 263n, 269n, 271n];

export function *allPrimes() {
    for (let p of primes) yield p;
    while (true) yield addPrime();
}

function addPrime(): bigint {
    for (let c = primes[primes.length - 1]! + 2n; ; c += 2n) {
        for (const p of primes) {
            if (c % p === 0n) break;
            if (p * p > c) {
                primes.push(c);
                return c;
            }
        }
    }
}

export function indexOfPrime(p: bigint): number {
    if (p <= primes[primes.length - 1]!) {
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
