import * as int from './integer';
import { StaxInt } from './integer';
import { last } from './types';
const two = int.make(2), three = int.make(3);

export function primeFactors(n: StaxInt): StaxInt[] {
    let result: StaxInt[] = [];

    n = int.abs(n);
    if (n.valueOf() <= 1) return result;
    for (let d of allPrimes()) {
        while (int.mod(n, d).valueOf() == 0) {
            result.push(d);
            n = int.div(n, d);
        }
        if (n.valueOf() == 1) return result;
        if (int.cmp(int.pow(d, two), n) > 0) {
            result.push(n);
            return result;
        }
    }
    throw new Error("Ran out of primes...?");
}

let primes: StaxInt[] = [two, three];

export function *allPrimes() {
    for (let p of primes) yield p;
    while (true) yield addPrime();
}

function addPrime(): StaxInt {
    for (let c = int.add(last(primes)!, two);; c = int.add(c, two)) {
        for (let p of allPrimes()) {
            if (int.mod(c, p). valueOf() == 0) break;
            if (int.cmp(int.pow(p, two), c) > 0) {
                primes.push(c);
                return c;
            }
        }
    }
}