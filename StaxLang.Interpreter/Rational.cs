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

        private struct BoundedDouble {
            public double Value { get; private set; }
            public double Error { get; private set; }
            public double Min => Value - Error;
            public double Max => Value + Error;

            public static readonly BoundedDouble CatastrophicLoss = new BoundedDouble(double.NaN, double.PositiveInfinity);

            public override string ToString() => $"{ Value } ± { Error }";

            public bool Sound {
                get {
                    if (double.IsNaN(Value)) return false;
                    if (double.IsInfinity(Error)) return false;
                    if (Error < 0) return false;
                    if (Error == 0) return true;
                    if (Error >= Math.Abs(Value)) return false;
                    return true;
                }
            }

            public BoundedDouble(double value, double error) {
                if (error < 0) throw new ArgumentOutOfRangeException("error");
                this.Value = value;
                this.Error = error;
            }

            public static BoundedDouble Estimate(double arg, int sigbits) => new BoundedDouble(arg, arg * Math.Pow(0.5, sigbits));

            public static BoundedDouble operator +(BoundedDouble a, BoundedDouble b) => new BoundedDouble(a.Value + b.Value, a.Error + b.Error);
            public static BoundedDouble operator +(BoundedDouble a, double b) => new BoundedDouble(a.Value + b, a.Error);
            public static BoundedDouble operator +(double a, BoundedDouble b) => new BoundedDouble(a + b.Value, b.Error);
            public static BoundedDouble operator -(BoundedDouble a, BoundedDouble b) => new BoundedDouble(a.Value - b.Value, a.Error + b.Error);
            public static BoundedDouble operator -(BoundedDouble a, double b) => new BoundedDouble(a.Value - b, a.Error);
            public static BoundedDouble operator -(double a, BoundedDouble b) => new BoundedDouble(a - b.Value, b.Error);
            public static BoundedDouble operator *(BoundedDouble a, BoundedDouble b) => new BoundedDouble(a.Value * b.Value, Math.Abs(a.Value) * b.Error + Math.Abs(b.Value) * a.Error);
            public static BoundedDouble operator *(BoundedDouble a, double b) => new BoundedDouble(a.Value * b, Math.Abs(b) * a.Error);
            public static BoundedDouble operator *(double a, BoundedDouble b) => b * a;
            public static BoundedDouble operator /(BoundedDouble a, BoundedDouble b) => new BoundedDouble(
                    a.Value / b.Value,
                    (Math.Abs(a.Value) * b.Error + Math.Abs(b.Value) * a.Error) / (b.Value * b.Min));
            public static BoundedDouble operator %(BoundedDouble a, BoundedDouble b) {
                var div = a / b;
                if (Math.Floor(div.Max) != Math.Floor(div.Min)) return CatastrophicLoss;
                return a - b * Math.Floor(div.Value);
            }
        }
        public static Rational Rationalize(double arg) {
            double Round(double d) => Math.Floor(d + 0.5);

            if (arg == 0) return new Rational(0, 1);
            if (arg < 0) return -Rationalize(-arg);

            BoundedDouble a = new BoundedDouble(1, 0), b = BoundedDouble.Estimate(arg, 56);
            double bestn = 0, bestd = 1, besterr = double.PositiveInfinity;
            do {
                var oldb = b;
                (a, b) = (b % a, a);
                double denlo = Round(1 / b.Max), denhi = Round(1 / b.Min);
                for (double den = denlo; den <= denhi; den++) {
                    double num = Round(arg * den), err = Math.Abs(arg - num / den);
                    if (err < besterr) {
                        (besterr, bestn, bestd) = (err, num, den);
                        if (err == 0) goto done;
                        double newError = 1 / den - 1 / (den + 1);
                        if (newError < b.Error) {
                            b = new BoundedDouble(1 / den, newError);
                            a = oldb % b;
                        }
                    }
                }
            } while (a.Sound);

        done: 
            return new Rational(new BigInteger(bestn), new BigInteger(bestd));
        }
    }
}
