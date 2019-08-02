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
        public string ToString(IFormatProvider format) => Num.ToString(format) + "/" + Den.ToString(format);
        public override bool Equals(object obj) => obj is Rational r && Num == r.Num && Den == r.Den;
        public override int GetHashCode() => Num.GetHashCode() ^ (Den.GetHashCode() * 37);

        public int CompareTo(object obj) {
            var r = (Rational)obj;
            if (this > r) return 1;
            if (this < r) return -1;
            if (this == r) return 0;
            throw new Exception("Rational compare sanity failed");
        }

        public static Rational Rationalize(double arg) {
            const int SignificantBits = 50;

            if (arg < 0) return -Rationalize(-arg);
            if (arg % 1 == 0) return new Rational(new BigInteger(arg), 1);

            double epsilon = arg / Math.Pow(2, SignificantBits), bestError = double.PositiveInfinity;
            (BigInteger Num, BigInteger Den)
                left = (new BigInteger(Math.Floor(arg)), 1),
                right = (new BigInteger(Math.Ceiling(arg)), 1),
                best = default, mediant;
            BigInteger lastMove = 1;

            do { // Stern-Brocot binary search
                mediant = lastMove < 0
                    ? (-lastMove * left.Num + right.Num, -lastMove * left.Den + right.Den)
                    : (left.Num + lastMove * right.Num, left.Den + lastMove * right.Den);

                double error = (double)mediant.Num / (double)mediant.Den - arg;
                if (double.IsNaN(error)) error = 0;

                if (error > 0) {
                    if (lastMove < 0) {
                        right = mediant;
                        lastMove *= 2;
                    }
                    else if ((lastMove /= 2) == 0) lastMove = -1;
                }
                else {
                    if (lastMove > 0) {
                        left = mediant;
                        lastMove *= 2;
                    }
                    else if ((lastMove /= 2) == 0) lastMove = 1;
                }
                if (Math.Abs(error) < bestError) (best, bestError) = (mediant, Math.Abs(error));
            } while (bestError > epsilon);
            return new Rational(best.Num, best.Den);
        }
    }
}
