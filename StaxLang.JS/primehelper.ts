import * as _ from 'lodash';
import * as bigInt from 'big-integer';
type BigInteger = bigInt.BigInteger;
const one = bigInt.one, zero = bigInt.zero, minusOne = bigInt.minusOne;

export function primeFactors(n: BigInteger): BigInteger[] {
    let result: BigInteger[] = [];

    n = n.abs();
    if (n.leq(one)) return result;
    for (let d of allPrimes()) {
        while (n.isDivisibleBy(d)) {
            result.push(d);
            n = n.divide(d);
        }
        if (n.equals(one)) return result;
    }
    throw new Error("Ran out of primes...?");
}

let primes: BigInteger[] = [bigInt[2], bigInt[3]];

export function *allPrimes() {
    for (let p of primes) yield p;
    while (true) yield addPrime();
}

function addPrime(): BigInteger {
    for (let c = _.last(primes)!.add(bigInt[2]);; c = c.add(bigInt[2])) {
        for (let p of allPrimes()) {
            if (c.isDivisibleBy(p)) break;
            if (p.square().greater(c)) {
                primes.push(c);
                return c;
            }
        }
    }
}