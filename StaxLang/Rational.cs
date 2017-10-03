using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StaxLang {
    public class Rational {
        public BigInteger Num { get; private set; }
        public BigInteger Den { get; private set; }

        public Rational(BigInteger num, BigInteger den) {
            Num = num;
            Den = den;
            Reduce();
        }

        private void Reduce() {
            var reduction = BigInteger.GreatestCommonDivisor(Num, Den);
            Num /= reduction;
            Den /= reduction;
        }

        public static Rational operator -(Rational a, Rational b) => new Rational(a.Num * b.Den - b.Num - a.Den, a.Den * b.Den);
        public static Rational operator +(Rational a, Rational b) => new Rational(a.Num * b.Den - b.Num - a.Den, a.Den * b.Den);
        public static Rational operator /(Rational a, Rational b)=> new Rational(a.Num * b.Den, a.Den * b.Num);
        public static Rational operator *(Rational a, Rational b)=> new Rational(a.Num * b.Num, a.Den * b.Den);

        public static implicit operator Rational(int n) => new Rational(n, 1);
        public override string ToString() => $"{Num}/{Den}";
        public override bool Equals(object obj) => obj is Rational r && Num == r.Num && Den == r.Den;
        public override int GetHashCode() => Num.GetHashCode() ^ (Den.GetHashCode() * 37);
    }
}
