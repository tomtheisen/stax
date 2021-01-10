import * as int from './integer';
import { StaxInt } from './integer';
import { last } from './types';

export function primeFactors(n: StaxInt): StaxInt[] {
    let result: StaxInt[] = [];

    n = int.abs(n);
    if (n.valueOf() <= 1) return result;
    for (let d of allPrimes()) {
        while (int.mod(n, d) === 0n) {
            result.push(d);
            n = int.div(n, d);
        }
        if (n === 1n) return result;
        if (int.cmp(int.mul(d, d), n) > 0) {
            result.push(n);
            return result;
        }
    }
    throw new Error("Ran out of primes...?");
}

let primes: StaxInt[] = [2n, 3n];

export function *allPrimes() {
    for (let p of primes) yield p;
    while (true) yield addPrime();
}

function addPrime(): StaxInt {
    for (let c = int.add(last(primes)!, 2n);; c = int.add(c, 2n)) {
        for (let p of allPrimes()) {
            if (int.mod(c, p) === 0n) break;
            if (int.cmp(int.pow(p, 2n), c) > 0) {
                primes.push(c);
                return c;
            }
        }
    }
}

export function indexOfPrime(p: StaxInt): number {
    if (int.cmp(p, last(primes)!) <= 0) {
        // binary search
        for (let lo = 0, hi = primes.length;lo < hi; ) {
            let mid = lo + hi >> 1;
            let cmp = int.cmp(p, primes[mid]);
            if (cmp === 0) return mid;
            if (cmp < 0) hi = mid; else lo = mid + 1;
        }
        return -1;
    }
    else for (let i = primes.length; ; i++) {
        const cmp = int.cmp(p, addPrime());
        if (cmp < 0) return -1;
        if (cmp === 0) return i;
    }
}
