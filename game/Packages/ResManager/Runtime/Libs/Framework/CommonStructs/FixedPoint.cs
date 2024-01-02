using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngineEx
{
    public struct FixedPoint : IComparable, IComparable<FixedPoint>, IComparable<Double>, IConvertible, IEquatable<FixedPoint>, IEquatable<Double>, IFormattable
    {
        public static bool OverflowLikeInteger = false;
        public static bool HandleNaNAndInf = false;

        public long Raw; // 32 -20 and 1 bit sign
        public const int ValBits = 52;
        public const int FBits = 20;
        public const long MULTIPLERAW = 1L << FBits;
        public const double MULTIPLED = (double)MULTIPLERAW;
        public const long MaxRaw = (1L << 52) - 1;
        public const long MinRaw = -(MaxRaw + 1);
        public const double MaxD = ((double)MaxRaw) / MULTIPLED;
        public const double MinD = ((double)MinRaw) / MULTIPLED;
        public const long EpsRaw = 1L;
        public const double EpsD = ((double)EpsRaw) / MULTIPLED;

        public static readonly FixedPoint MaxValue = new FixedPoint(MaxRaw, 0);
        public static readonly FixedPoint MinValue = new FixedPoint(MinRaw, 0);
        public static readonly FixedPoint Epsilon = new FixedPoint(EpsRaw, 0);

        public long FractionPart
        {
            get
            {
                return Raw & (MULTIPLERAW - 1);
            }
        }
        public double ShiftedFractionPart
        {
            get
            {
                return ((double)FractionPart) / MULTIPLED;
            }
        }
        public long IntegerPart
        {
            get
            {
                return Raw & ~(MULTIPLERAW - 1);
            }
        }
        public long ShiftedIntegerPart
        {
            get
            {
                return IntegerPart >> FBits;
            }
        }

        public FixedPoint(long raw, int reserved)
        {
            Raw = raw;
        }
        public FixedPoint(double d)
        {
            if (HandleNaNAndInf)
            {
                if (double.IsNaN(d))
                {
                    Raw = NaNRaw;
                    return;
                }
                if (double.IsPositiveInfinity(d))
                {
                    Raw = PInfRaw;
                    return;
                }
                if (double.IsNegativeInfinity(d))
                {
                    Raw = NInfRaw;
                    return;
                }
            }
            Raw = (long)(d * MULTIPLED);
        }
        public FixedPoint(FixedPoint fp)
        {
            Raw = fp.Raw;
        }
        public double ToDouble()
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(this))
                {
                    return double.NaN;
                }
                if (IsPositiveInfinity(this))
                {
                    return double.PositiveInfinity;
                }
                if (IsNegativeInfinity(this))
                {
                    return double.NegativeInfinity;
                }
            }
            return ((double)Raw) / MULTIPLED;
        }

        #region Wrap and Unwrap
        public static FixedPoint Wrap(double d)
        {
            return new FixedPoint(d);
        }
        public static FixedPoint Wrap(FixedPoint fp)
        {
            return fp;
        }
        public static double Unwrap(FixedPoint fp)
        {
            return fp.ToDouble();
        }
        public static double Unwrap(double d)
        {
            return new FixedPoint(d).ToDouble();
        }
        #endregion

        #region NaN and Inf
        public const long NaNRaw = 3L << 62;
        public const long PInfRaw = long.MaxValue;
        public const long NInfRaw = long.MinValue + 1;
        public static bool IsNaN(FixedPoint fp)
        {
            return (fp.Raw & NaNRaw) == NaNRaw;
        }
        public bool IsNaN()
        {
            return IsNaN(this);
        }
        public static readonly FixedPoint NaN = new FixedPoint(NaNRaw, 0);
        public static bool IsPositiveInfinity(FixedPoint fp)
        {
            return fp.Raw == PInfRaw;
        }
        public bool IsPositiveInfinity()
        {
            return IsPositiveInfinity(this);
        }
        public static readonly FixedPoint PositiveInfinity = new FixedPoint(PInfRaw, 0);
        public static bool IsNegativeInfinity(FixedPoint fp)
        {
            return fp.Raw == NInfRaw;
        }
        public bool IsNegativeInfinity()
        {
            return IsNegativeInfinity(this);
        }
        public static readonly FixedPoint NegativeInfinity = new FixedPoint(NInfRaw, 0);
        public static bool IsInfinity(FixedPoint fp)
        {
            return fp.Raw == PInfRaw || fp.Raw == NInfRaw;
        }
        public bool IsInfinity()
        {
            return IsInfinity(this);
        }
        // In order to call func on both fixpoints and doubles, we should wrap the same function for double
        public static bool IsNaN(double d)
        {
            return double.IsNaN(d);
        }
        public static bool IsPositiveInfinity(double d)
        {
            return double.IsPositiveInfinity(d);
        }
        public static bool IsNegativeInfinity(double d)
        {
            return double.IsNegativeInfinity(d);
        }
        public static bool IsInfinity(double d)
        {
            return double.IsInfinity(d);
        }
        #endregion

        public static implicit operator FixedPoint(double d)
        {
            return new FixedPoint(d);
        }
        public static implicit operator double(FixedPoint fp)
        {
            return fp.ToDouble();
        }

        public override string ToString()
        {
            return ToDouble().ToString();
        }
        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (!(obj is FixedPoint))
            {
                return false;
            }
            return ((FixedPoint)obj).Raw == Raw;
        }

        public static FixedPoint Parse(string s)
        {
            return new FixedPoint(double.Parse(s));
        }
        public static bool TryParse(string s, out FixedPoint fp)
        {
            double d;
            if (double.TryParse(s, out d))
            {
                fp = new FixedPoint(d);
                return true;
            }
            else
            {
                fp = default(FixedPoint);
                return false;
            }
        }

        public static int Compare(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1))
                {
                    if (IsNaN(fp2))
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (IsNaN(fp2))
                {
                    return 1;
                }
            }
            return fp1.Raw.CompareTo(fp2.Raw);
        }
        public static int Compare(FixedPoint fp1, double d2)
        {
            return Compare(fp1, new FixedPoint(d2));
        }
        public static int Compare(double d1, FixedPoint fp2)
        {
            return Compare(new FixedPoint(d1), fp2);
        }
        public static int Compare(double d1, double d2)
        {
            return Compare(new FixedPoint(d1), new FixedPoint(d2));
        }

        #region Interfaces
        public int CompareTo(FixedPoint fp2)
        {
            return Compare(this, fp2);
        }
        public int CompareTo(double d2)
        {
            return Compare(this, d2);
        }
        public int CompareTo(object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (value is FixedPoint)
            {
                return Compare(this, (FixedPoint)value);
            }
            else if (value is double)
            {
                return Compare(this, (double)value);
            }
            throw new ArgumentException("Must be a FixPoint or double.");
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Int64;
        }
        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Raw != 0;
        }
        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return (byte)ShiftedIntegerPart;
        }
        char IConvertible.ToChar(IFormatProvider provider)
        {
            return (char)ShiftedIntegerPart;
        }
        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(ToDouble(), provider);
        }
        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return (decimal)ToDouble();
        }
        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return ToDouble();
        }
        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return (short)ShiftedIntegerPart;
        }
        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return (int)ShiftedIntegerPart;
        }
        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return (long)ShiftedIntegerPart;
        }
        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return (sbyte)ShiftedIntegerPart;
        }
        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return (float)ToDouble();
        }
        public string ToString(IFormatProvider provider)
        {
            return ToDouble().ToString(provider);
        }
        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(ToDouble(), conversionType, provider);
        }
        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return (ushort)ShiftedIntegerPart;
        }
        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return (uint)ShiftedIntegerPart;
        }
        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return (ulong)ShiftedIntegerPart;
        }

        public bool Equals(FixedPoint fp2)
        {
            return this == fp2;
        }
        public bool Equals(double d2)
        {
            return this == d2;
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return ToDouble().ToString(format, provider);
        }
        #endregion

        private static long CheckOverflow(long raw)
        {
            //if (HandleNaNAndInf)
            //{
            //    if (raw > MaxRaw)
            //    {
            //        return PInfRaw;
            //    }
            //    if (raw < MinRaw)
            //    {
            //        return NInfRaw;
            //    }
            //    return raw;
            //}
            //else
            {
                return CheckOverflow(raw, MaxRaw, MinRaw);
            }
        }
        private static long CheckOverflow(long raw, long max, long min)
        {
            if (OverflowLikeInteger)
            {
                if (raw > max)
                {
                    raw |= min;
                }
                else if (raw < min)
                {
                    raw &= max;
                }
                return raw;
            }
            else
            {
                if (raw > max)
                {
                    raw = max;
                }
                else if (raw < min)
                {
                    raw = min;
                }
                return raw;
            }
        }

        #region operators
        public static bool operator ==(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return false;
                }
            }
            return fp1.Raw == fp2.Raw;
        }
        public static bool operator !=(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return true;
                }
            }
            return fp1.Raw != fp2.Raw;
        }
        public static bool operator ==(FixedPoint fp1, double d2)
        {
            return fp1 == new FixedPoint(d2);
        }
        public static bool operator !=(FixedPoint fp1, double d2)
        {
            return fp1 != new FixedPoint(d2);
        }
        public static bool operator ==(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) == fp2;
        }
        public static bool operator !=(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) != fp2;
        }

        public static bool operator >(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return false;
                }
            }
            return fp1.Raw > fp2.Raw;
        }
        public static bool operator <(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return false;
                }
            }
            return fp1.Raw < fp2.Raw;
        }
        public static bool operator >(FixedPoint fp1, double d2)
        {
            return fp1 > new FixedPoint(d2);
        }
        public static bool operator <(FixedPoint fp1, double d2)
        {
            return fp1 < new FixedPoint(d2);
        }
        public static bool operator >(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) > fp2;
        }
        public static bool operator <(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) < fp2;
        }

        public static bool operator >=(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return false;
                }
            }
            return fp1.Raw >= fp2.Raw;
        }
        public static bool operator <=(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return false;
                }
            }
            return fp1.Raw <= fp2.Raw;
        }
        public static bool operator >=(FixedPoint fp1, double d2)
        {
            return fp1 >= new FixedPoint(d2);
        }
        public static bool operator <=(FixedPoint fp1, double d2)
        {
            return fp1 <= new FixedPoint(d2);
        }
        public static bool operator >=(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) >= fp2;
        }
        public static bool operator <=(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) <= fp2;
        }

        public static FixedPoint operator +(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return NaN;
                }
                else if (IsPositiveInfinity(fp1))
                {
                    if (IsNegativeInfinity(fp2))
                    {
                        return NaN;
                    }
                    else
                    {
                        return fp1;
                    }
                }
                else if (IsNegativeInfinity(fp1))
                {
                    if (IsPositiveInfinity(fp2))
                    {
                        return NaN;
                    }
                    else
                    {
                        return fp1;
                    }
                }
                else
                {
                    if (IsInfinity(fp2))
                    {
                        return fp2;
                    }
                }
            }
            var newRaw = fp1.Raw + fp2.Raw;
            newRaw = CheckOverflow(newRaw);
            return new FixedPoint(newRaw, 0);
        }
        public static FixedPoint operator +(FixedPoint fp1, double d2)
        {
            return fp1 + new FixedPoint(d2);
        }
        public static FixedPoint operator +(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) + fp2;
        }

        public static FixedPoint operator -(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return NaN;
                }
                else if (IsPositiveInfinity(fp1))
                {
                    if (IsPositiveInfinity(fp2))
                    {
                        return NaN;
                    }
                    else
                    {
                        return fp1;
                    }
                }
                else if (IsNegativeInfinity(fp1))
                {
                    if (IsNegativeInfinity(fp2))
                    {
                        return NaN;
                    }
                    else
                    {
                        return fp1;
                    }
                }
                else
                {
                    if (IsInfinity(fp2))
                    {
                        return new FixedPoint(-fp2.Raw, 0);
                    }
                }
            }
            var newRaw = fp1.Raw - fp2.Raw;
            newRaw = CheckOverflow(newRaw);
            return new FixedPoint(newRaw, 0);
        }
        public static FixedPoint operator -(FixedPoint fp1, double d2)
        {
            return fp1 - new FixedPoint(d2);
        }
        public static FixedPoint operator -(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) - fp2;
        }

        public static FixedPoint operator +(FixedPoint fp)
        {
            return fp;
        }
        public static FixedPoint operator -(FixedPoint fp)
        {
            if (fp.Raw == MinRaw)
            {
                //if (HandleNaNAndInf)
                //{
                //    return PositiveInfinity;
                //}
                if (OverflowLikeInteger)
                {
                    return fp;
                }
                else
                {
                    return MaxRaw;
                }
            }
            return new FixedPoint(-fp.Raw, 0);
        }

        private const int HalfBits = ValBits / 2;
        private const long LowMask = (1L << HalfBits) - 1;
        private const long HHMax = (int.MaxValue >> 2);
        private const long HHMin = (int.MinValue >> 2);
        public static FixedPoint operator *(FixedPoint fp1, FixedPoint fp2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return NaN;
                }
                else if (IsInfinity(fp1))
                {
                    var raw2 = fp2.Raw;
                    if (raw2 == 0)
                    {
                        return NaN;
                    }
                    else if (raw2 > 0)
                    {
                        return fp1;
                    }
                    else
                    {
                        return new FixedPoint(-fp1.Raw, 0);
                    }
                }
                else if (IsInfinity(fp2))
                {
                    var raw1 = fp1.Raw;
                    if (raw1 == 0)
                    {
                        return NaN;
                    }
                    else if (raw1 > 0)
                    {
                        return fp2;
                    }
                    else
                    {
                        return new FixedPoint(-fp2.Raw, 0);
                    }
                }
            }

            var h1 = fp1.Raw >> HalfBits;
            var l1 = fp1.Raw & LowMask;
            var h2 = fp2.Raw >> HalfBits;
            var l2 = fp2.Raw & LowMask;

            var hh = h1 * h2;
            hh = CheckOverflow(hh, HHMax, HHMin);
            hh <<= (ValBits - FBits);

            var hl = h1 * l2;
            var lh = l1 * h2;
            var shl = hl + lh;
            shl <<= (HalfBits - FBits);

            var ll = l1 * l2;
            ll >>= (FBits);

            var newraw = hh + shl + ll;
            newraw = CheckOverflow(newraw);
            return new FixedPoint(newraw, 0);
        }
        public static FixedPoint operator *(FixedPoint fp1, double d2)
        {
            return fp1 * new FixedPoint(d2);
        }
        public static FixedPoint operator *(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) * fp2;
        }

        private const int HalfFBits = FBits / 2;
        private const long RHQMax = (1L << (64 - HalfFBits - 4)) - 1;
        private const long RHQMin = -(RHQMax + 1);
        public static FixedPoint operator /(FixedPoint fp1, FixedPoint fp2)
        {
            var raw1 = fp1.Raw;
            var raw2 = fp2.Raw;

            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return NaN;
                }
                else if (IsInfinity(fp1))
                {
                    if (IsInfinity(fp2))
                    {
                        return NaN;
                    }
                    if (raw2 < 0)
                    {
                        return new FixedPoint(-raw1, 0);
                    }
                    else
                    {
                        return fp1;
                    }
                }
                else if (IsInfinity(fp2))
                {
                    return new FixedPoint(0, 0);
                }
                else if (raw2 == 0)
                {
                    if (raw1 == 0)
                    {
                        return NaN;
                    }
                    else if (raw1 > 0)
                    {
                        return PositiveInfinity;
                    }
                    else
                    {
                        return NegativeInfinity;
                    }
                }
            }

            var mainr = raw1 << HalfFBits;

            var rhq = mainr / raw2;
            var rhr = mainr % raw2;

            rhq = CheckOverflow(rhq, RHQMax, RHQMin);
            rhq <<= HalfFBits;
            rhr <<= HalfFBits;

            var rlq = rhr / raw2;

            var newraw = rhq + rlq;
            newraw = CheckOverflow(newraw);
            return new FixedPoint(newraw, 0);
        }
        public static FixedPoint operator /(FixedPoint fp1, double d2)
        {
            return fp1 / new FixedPoint(d2);
        }
        public static FixedPoint operator /(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) / fp2;
        }

        public static FixedPoint operator %(FixedPoint fp1, FixedPoint fp2)
        {
            var raw1 = fp1.Raw;
            var raw2 = fp2.Raw;

            if (HandleNaNAndInf)
            {
                if (IsNaN(fp1) || IsNaN(fp2))
                {
                    return NaN;
                }
                else if (IsInfinity(fp1))
                {
                    return NaN;
                }
                else if (IsInfinity(fp2))
                {
                    return fp1;
                }
                else if (raw2 == 0)
                {
                    return NaN;
                }
            }

            var newraw = raw1 % raw2;
            newraw = CheckOverflow(newraw);
            return new FixedPoint(newraw, 0);
        }
        public static FixedPoint operator %(FixedPoint fp1, double d2)
        {
            return fp1 % new FixedPoint(d2);
        }
        public static FixedPoint operator %(double d1, FixedPoint fp2)
        {
            return new FixedPoint(d1) % fp2;
        }
        #endregion

        #region Math Functions (Deterministic)
        private const int HalfHalfFBits = HalfFBits / 2;
        private static long FloorSqrt(long x)
        {
            // Base Cases
            if (x == 0 || x == 1)
                return x;

            // Do Binary Search 
            // for floor(sqrt(x))
            long start = 1, end = x, ans = 0;
            while (start <= end)
            {
                long mid = (start + end) / 2;

                // If x is a 
                // perfect square
                if (mid * mid == x)
                    return mid;

                // Since we need floor, we 
                // update answer when mid * 
                // mid is smaller than x, 
                // and move closer to sqrt(x)
                if (mid * mid < x)
                {
                    start = mid + 1;
                    ans = mid;
                }

                // If mid*mid is 
                // greater than x
                else
                    end = mid - 1;
            }
            return ans;
        }
        public static FixedPoint Sqrt(FixedPoint fp)
        {
            var raw = fp.Raw;

            if (HandleNaNAndInf)
            {
                if (IsNaN(fp))
                {
                    return NaN;
                }
                if (raw < 0)
                {
                    return NaN;
                }
                if (IsPositiveInfinity(fp))
                {
                    return fp;
                }
            }

            if (raw < 0)
            {
                throw new ArithmeticException("Cannot find sqrt of negative number.");
            }
            raw <<= HalfFBits;
            var mainraw = FloorSqrt(raw);
            mainraw <<= HalfHalfFBits;

            var main = new FixedPoint(mainraw, 0);
            //var diff = fp - main * main;
            //var r = diff / main;
            var r = fp / main;
            var rraw = r.Raw;
            rraw -= mainraw;
            rraw /= 2;

            return new FixedPoint(mainraw + rraw, 0);
        }
        public static FixedPoint Sqrt(double d)
        {
            return Sqrt(new FixedPoint(d));
        }

        public static FixedPoint Abs(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp))
                {
                    return fp;
                }
            }
            var raw = fp.Raw;
            if (raw == MinRaw)
            {
                //if (HandleNaNAndInf)
                //{
                //    return PositiveInfinity;
                //}
                if (OverflowLikeInteger)
                {
                    return fp;
                }
                else
                {
                    return MaxRaw;
                }
            }
            return new FixedPoint(Math.Abs(raw), 0);
        }
        public static FixedPoint Abs(double d)
        {
            return Abs(new FixedPoint(d));
        }

        public static FixedPoint Ceiling(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp) || IsInfinity(fp))
                {
                    return fp;
                }
            }
            var i = fp.IntegerPart;
            var f = fp.FractionPart;
            if (f != 0)
            {
                i += MULTIPLERAW;
            }
            var newraw = CheckOverflow(i);
            return new FixedPoint(newraw, 0);
        }
        public static FixedPoint Ceiling(double d)
        {
            return Ceiling(new FixedPoint(d));
        }
        public static FixedPoint Floor(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp) || IsInfinity(fp))
                {
                    return fp;
                }
            }
            var i = fp.IntegerPart;
            return new FixedPoint(i, 0);
        }
        public static FixedPoint Floor(double d)
        {
            return Floor(new FixedPoint(d));
        }

        public static FixedPoint Max(FixedPoint val1, FixedPoint val2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(val1) || IsNaN(val2))
                {
                    return NaN;
                }
            }
            var maxraw = Math.Max(val1.Raw, val2.Raw);
            return new FixedPoint(maxraw, 0);
        }
        public static FixedPoint Max(FixedPoint val1, double val2)
        {
            return Max(val1, new FixedPoint(val2));
        }
        public static FixedPoint Max(double val1, FixedPoint val2)
        {
            return Max(new FixedPoint(val1), val2);
        }
        public static FixedPoint Max(double val1, double val2)
        {
            return Max(new FixedPoint(val1), new FixedPoint(val2));
        }

        public static FixedPoint Min(FixedPoint val1, FixedPoint val2)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(val1) || IsNaN(val2))
                {
                    return NaN;
                }
            }
            var minraw = Math.Min(val1.Raw, val2.Raw);
            return new FixedPoint(minraw, 0);
        }
        public static FixedPoint Min(FixedPoint val1, double val2)
        {
            return Min(val1, new FixedPoint(val2));
        }
        public static FixedPoint Min(double val1, FixedPoint val2)
        {
            return Min(new FixedPoint(val1), val2);
        }
        public static FixedPoint Min(double val1, double val2)
        {
            return Min(new FixedPoint(val1), new FixedPoint(val2));
        }

        private const long HalfMaxFrac = MULTIPLERAW >> 1;
        public static FixedPoint Round(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp) || IsInfinity(fp))
                {
                    return fp;
                }
            }
            var i = fp.IntegerPart;
            var f = fp.FractionPart;
            if (f != 0)
            {
                if (i < 0)
                {
                    if (f > HalfMaxFrac)
                    {
                        i += MULTIPLERAW;
                    }
                }
                else
                {
                    if (f >= HalfMaxFrac)
                    {
                        i += MULTIPLERAW;
                    }
                }
            }
            var newraw = CheckOverflow(i);
            return new FixedPoint(newraw, 0);
        }
        public static FixedPoint Round(double d)
        {
            return Round(new FixedPoint(d));
        }

        public static int Sign(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp))
                {
                    throw new ArithmeticException("Cannot get sign of a NaN.");
                }
            }
            var raw = fp.Raw;
            if (raw == 0)
            {
                return 0;
            }
            else if (raw > 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
        public static int Sign(double d)
        {
            return Sign(new FixedPoint(d));
        }

        public static FixedPoint Truncate(FixedPoint fp)
        {
            if (HandleNaNAndInf)
            {
                if (IsNaN(fp) || IsInfinity(fp))
                {
                    return fp;
                }
            }
            var i = fp.IntegerPart;
            var f = fp.FractionPart;
            if (i < 0 && f != 0)
            {
                i += MULTIPLERAW;
            }
            return new FixedPoint(i, 0);
        }
        public static FixedPoint Truncate(double d)
        {
            return Truncate(new FixedPoint(d));
        }
        #endregion

        #region Math Functions (Directly using the Math.XXX(double) functions)
        public static readonly FixedPoint E = new FixedPoint(Math.E);
        public static readonly FixedPoint PI = new FixedPoint(Math.PI);

        public static FixedPoint Acos(FixedPoint fp)
        {
            return new FixedPoint(Math.Acos(fp.ToDouble()));
        }
        public static FixedPoint Acos(double d)
        {
            return Acos(new FixedPoint(d));
        }
        public static FixedPoint Asin(FixedPoint fp)
        {
            return new FixedPoint(Math.Asin(fp.ToDouble()));
        }
        public static FixedPoint Asin(double d)
        {
            return Asin(new FixedPoint(d));
        }
        public static FixedPoint Atan(FixedPoint fp)
        {
            return new FixedPoint(Math.Atan(fp.ToDouble()));
        }
        public static FixedPoint Atan(double d)
        {
            return Atan(new FixedPoint(d));
        }
        public static FixedPoint Atan2(FixedPoint y, FixedPoint x)
        {
            return new FixedPoint(Math.Atan2(y.ToDouble(), x.ToDouble()));
        }
        public static FixedPoint Atan2(FixedPoint y, double x)
        {
            return Atan2(y, new FixedPoint(x));
        }
        public static FixedPoint Atan2(double y, FixedPoint x)
        {
            return Atan2(new FixedPoint(y), x);
        }
        public static FixedPoint Atan2(double y, double x)
        {
            return Atan2(new FixedPoint(y), new FixedPoint(x));
        }
        public static FixedPoint Cos(FixedPoint fp)
        {
            return new FixedPoint(Math.Cos(fp.ToDouble()));
        }
        public static FixedPoint Cos(double d)
        {
            return Cos(new FixedPoint(d));
        }
        public static FixedPoint Cosh(FixedPoint fp)
        {
            return new FixedPoint(Math.Cosh(fp.ToDouble()));
        }
        public static FixedPoint Cosh(double d)
        {
            return Cosh(new FixedPoint(d));
        }
        public static FixedPoint Exp(FixedPoint fp)
        {
            return new FixedPoint(Math.Exp(fp.ToDouble()));
        }
        public static FixedPoint Exp(double d)
        {
            return Exp(new FixedPoint(d));
        }
        public static FixedPoint Log(FixedPoint fp)
        {
            return new FixedPoint(Math.Log(fp.ToDouble()));
        }
        public static FixedPoint Log(double d)
        {
            return Log(new FixedPoint(d));
        }
        public static FixedPoint Log10(FixedPoint fp)
        {
            return new FixedPoint(Math.Log10(fp.ToDouble()));
        }
        public static FixedPoint Log10(double d)
        {
            return Log10(new FixedPoint(d));
        }
        public static FixedPoint Log(FixedPoint a, FixedPoint newBase)
        {
            return new FixedPoint(Math.Log(a.ToDouble(), newBase.ToDouble()));
        }
        public static FixedPoint Log(FixedPoint a, double newBase)
        {
            return Log(a, new FixedPoint(newBase));
        }
        public static FixedPoint Log(double a, FixedPoint newBase)
        {
            return Log(new FixedPoint(a), newBase);
        }
        public static FixedPoint Log(double a, double newBase)
        {
            return Log(new FixedPoint(a), new FixedPoint(newBase));
        }
        public static FixedPoint Pow(FixedPoint x, FixedPoint y)
        {
            return new FixedPoint(Math.Pow(x.ToDouble(), y.ToDouble()));
        }
        public static FixedPoint Pow(FixedPoint x, double y)
        {
            return Pow(x, new FixedPoint(y));
        }
        public static FixedPoint Pow(double x, FixedPoint y)
        {
            return Pow(new FixedPoint(x), y);
        }
        public static FixedPoint Pow(double x, double y)
        {
            return Pow(new FixedPoint(x), new FixedPoint(y));
        }
        public static FixedPoint Sin(FixedPoint fp)
        {
            return new FixedPoint(Math.Sin(fp.ToDouble()));
        }
        public static FixedPoint Sin(double d)
        {
            return Sin(new FixedPoint(d));
        }
        public static FixedPoint Sinh(FixedPoint fp)
        {
            return new FixedPoint(Math.Sinh(fp.ToDouble()));
        }
        public static FixedPoint Sinh(double d)
        {
            return Sinh(new FixedPoint(d));
        }
        public static FixedPoint Tan(FixedPoint fp)
        {
            return new FixedPoint(Math.Tan(fp.ToDouble()));
        }
        public static FixedPoint Tan(double d)
        {
            return Tan(new FixedPoint(d));
        }
        public static FixedPoint Tanh(FixedPoint fp)
        {
            return new FixedPoint(Math.Tanh(fp.ToDouble()));
        }
        public static FixedPoint Tanh(double d)
        {
            return Tanh(new FixedPoint(d));
        }
        #endregion
    }
}