using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StaxLang {
    static class PrimeHelper {
        private static List<BigInteger> Primes = new List<BigInteger> { 2, 3 };

        public static bool IsPrime(BigInteger n) {
            while (n > Primes.Last()) AddPrime();
            return Primes.BinarySearch(n) >= 0;
        }

        public static IEnumerable<BigInteger> AllPrimes() {
            foreach (var p in Primes) yield return p;
            while (true) yield return AddPrime();
        }

        private static BigInteger AddPrime() {
            for (var c = Primes.Last() + 2;; c += 2) {
                foreach (var p in AllPrimes()) {
                    if (c % p == 0) break;
                    if (p * p > c) {
                        Primes.Add(c);
                        return c;
                    }
                }
            }
        }
    }
}
