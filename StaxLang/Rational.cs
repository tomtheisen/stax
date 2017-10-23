using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StaxLang {
    public class Rational : IComparable {
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

            if (Den < 0) {
                Num *= -1;
                Den *= -1;
            }
        }

        public Rational AbsoluteValue() => new Rational(BigInteger.Abs(Num), BigInteger.Abs(Den));

        public static Rational operator -(Rational a, Rational b) => new Rational(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);
        public static Rational operator +(Rational a, Rational b) => new Rational(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);
        public static Rational operator /(Rational a, Rational b)=> new Rational(a.Num * b.Den, a.Den * b.Num);
        public static Rational operator *(Rational a, Rational b)=> new Rational(a.Num * b.Num, a.Den * b.Den);
        public static Rational operator %(Rational a, Rational b) => a - (a / b).Floor() * b;
        public static Rational operator -(Rational a) => a * -1;
        public static bool operator ==(Rational a, Rational b) => a.Equals(b);
        public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
        public static bool operator <(Rational a, Rational b) => a.Num * b.Den < b.Num * a.Den;
        public static bool operator >(Rational a, Rational b) => a.Num * b.Den > b.Num * a.Den;
        public static bool operator <=(Rational a, Rational b) => a.Num * b.Den <= b.Num * a.Den;
        public static bool operator >=(Rational a, Rational b) => a.Num * b.Den >= b.Num * a.Den;

        public static implicit operator Rational(int n) => new Rational(n, 1);
        public static implicit operator Rational(BigInteger n) => new Rational(n, 1);
        public static explicit operator double(Rational r) => (double)r.Num / (double)r.Den;

        public BigInteger Floor() {
            if (Num < 0) return (Num - Den + 1) / Den; 
            else return Num / Den;
        }

        public BigInteger Ceil() {
            if (Num < 0) {
                return Num / Den; 
            }
            else {
                return (Num + Den - 1) / Den;
            }
        }

        public override string ToString() => $"{Num}/{Den}";
        public override bool Equals(object obj) => obj is Rational r && Num == r.Num && Den == r.Den;
        public override int GetHashCode() => Num.GetHashCode() ^ (Den.GetHashCode() * 37);

        public int CompareTo(object obj) {
            var r = (Rational)obj;
            if (this > r) return 1;
            if (this < r) return -1;
            if (this == r) return 0;
            throw new Exception("Rational compare sanity failed");
        }
    }
}
