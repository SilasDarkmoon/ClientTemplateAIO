using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using uobj = UnityEngine.Object;
#endif

#if (!UNITY_ENGINE && !UNITY_5_3_OR_NEWER && NETSTANDARD) || NET_STANDARD_2_0
#else
using ITuple = System.Runtime.CompilerServices.ITuple;
#endif

namespace UnityEngineEx
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct BOOL
    {
        public BOOL(bool v)
        {
            val = v ? 1 : 0;
        }
        public BOOL(int v)
        {
            val = v;
        }

        public int val;
        public static implicit operator bool(BOOL v)
        {
            return v.val != 0;
        }
        public static implicit operator BOOL(bool v)
        {
            var v2 = new BOOL();
            v2.val = v ? 1 : 0;
            return v2;
        }
        public static bool operator ==(BOOL v1, BOOL v2)
        {
            return (v1.val == 0) == (v2.val == 0);
        }
        public static bool operator !=(BOOL v1, BOOL v2)
        {
            return (v1.val == 0) != (v2.val == 0);
        }
        public static bool operator ==(BOOL v1, bool v2)
        {
            return (v1.val != 0) == v2;
        }
        public static bool operator !=(BOOL v1, bool v2)
        {
            return (v1.val != 0) != v2;
        }
        public static bool operator ==(bool v1, BOOL v2)
        {
            return v1 == (v2.val != 0);
        }
        public static bool operator !=(bool v1, BOOL v2)
        {
            return v1 != (v2.val != 0);
        }

        public override bool Equals(object obj)
        {
            bool v1 = (val != 0);
            if (obj is bool)
            {
                return v1 == (bool)obj;
            }
            else if (obj is BOOL)
            {
                return v1 == (((BOOL)obj).val != 0);
            }
            return false;
        }
        public override int GetHashCode()
        {
            bool v1 = (val != 0);
            return v1.GetHashCode();
        }
        public override string ToString()
        {
            bool v1 = (val != 0);
            return v1.ToString();
        }
    }

#if (!UNITY_ENGINE && !UNITY_5_3_OR_NEWER && NETSTANDARD) || NET_STANDARD_2_0
    public interface ITuple
    {
        object this[int index] { get; }
        int Length { get; }
    }
#endif
    public interface IWritableTuple
    {
        object this[int index] { get; set; }
    }

    internal static class PackUtils
    {
        public static int GetHashCode<T>(T t)
        {
            if (typeof(T).IsValueType)
            {
                return t.GetHashCode();
            }
            else
            {
                if (t == null)
                {
                    return 0;
                }
                else
                {
                    return t.GetHashCode();
                }
            }
        }
        public static string ToString<T>(T t)
        {
            if (typeof(T).IsValueType)
            {
                return t.ToString();
            }
            else
            {
                if (t == null)
                {
                    //return "(" + typeof(T).ToString() + ")null";
                    return "null";
                }
                else
                {
                    return t.ToString();
                }
            }
        }
    }
    // TODO: Pack's ITuple, IWritableTuple, To ValueTuple and From Tuple, ==, !=
    public struct Pack<T1, T2> : IEquatable<Pack<T1, T2>>
    {
        public T1 t1;
        public T2 t2;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;

        public Pack(T1 p1, T2 p2)
        {
            t1 = p1;
            t2 = p2;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1) ^ PackUtils.GetHashCode(t2);
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2>)
            {
                return Equals((Pack<T1, T2>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3> : IEquatable<Pack<T1, T2, T3>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;

        public Pack(T1 p1, T2 p2, T3 p3)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3>)
            {
                return Equals((Pack<T1, T2, T3>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4> : IEquatable<Pack<T1, T2, T3, T4>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4>)
            {
                return Equals((Pack<T1, T2, T3, T4>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5> : IEquatable<Pack<T1, T2, T3, T4, T5>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6> : IEquatable<Pack<T1, T2, T3, T4, T5, T6>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;
        private static EqualityComparer<T6> c6 = EqualityComparer<T6>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ^ PackUtils.GetHashCode(t6)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5, T6>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5, T6>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5, T6> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                && c6.Equals(t6, other.t6)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t6));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7> : IEquatable<Pack<T1, T2, T3, T4, T5, T6, T7>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;
        private static EqualityComparer<T6> c6 = EqualityComparer<T6>.Default;
        private static EqualityComparer<T7> c7 = EqualityComparer<T7>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ^ PackUtils.GetHashCode(t6)
                ^ PackUtils.GetHashCode(t7)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5, T6, T7>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5, T6, T7>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5, T6, T7> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                && c6.Equals(t6, other.t6)
                && c7.Equals(t7, other.t7)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t6));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t7));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8> : IEquatable<Pack<T1, T2, T3, T4, T5, T6, T7, T8>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;
        private static EqualityComparer<T6> c6 = EqualityComparer<T6>.Default;
        private static EqualityComparer<T7> c7 = EqualityComparer<T7>.Default;
        private static EqualityComparer<T8> c8 = EqualityComparer<T8>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ^ PackUtils.GetHashCode(t6)
                ^ PackUtils.GetHashCode(t7)
                ^ PackUtils.GetHashCode(t8)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5, T6, T7, T8>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5, T6, T7, T8>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5, T6, T7, T8> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                && c6.Equals(t6, other.t6)
                && c7.Equals(t7, other.t7)
                && c8.Equals(t8, other.t8)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t6));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t7));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t8));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IEquatable<Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;
        public T9 t9;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;
        private static EqualityComparer<T6> c6 = EqualityComparer<T6>.Default;
        private static EqualityComparer<T7> c7 = EqualityComparer<T7>.Default;
        private static EqualityComparer<T8> c8 = EqualityComparer<T8>.Default;
        private static EqualityComparer<T9> c9 = EqualityComparer<T9>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
            t9 = p9;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ^ PackUtils.GetHashCode(t6)
                ^ PackUtils.GetHashCode(t7)
                ^ PackUtils.GetHashCode(t8)
                ^ PackUtils.GetHashCode(t9)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                && c6.Equals(t6, other.t6)
                && c7.Equals(t7, other.t7)
                && c8.Equals(t8, other.t8)
                && c9.Equals(t9, other.t9)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t6));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t7));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t8));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t9));
            sb.Append(")");
            return sb.ToString();
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IEquatable<Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;
        public T9 t9;
        public T10 t10;

        private static EqualityComparer<T1> c1 = EqualityComparer<T1>.Default;
        private static EqualityComparer<T2> c2 = EqualityComparer<T2>.Default;
        private static EqualityComparer<T3> c3 = EqualityComparer<T3>.Default;
        private static EqualityComparer<T4> c4 = EqualityComparer<T4>.Default;
        private static EqualityComparer<T5> c5 = EqualityComparer<T5>.Default;
        private static EqualityComparer<T6> c6 = EqualityComparer<T6>.Default;
        private static EqualityComparer<T7> c7 = EqualityComparer<T7>.Default;
        private static EqualityComparer<T8> c8 = EqualityComparer<T8>.Default;
        private static EqualityComparer<T9> c9 = EqualityComparer<T9>.Default;
        private static EqualityComparer<T10> c10 = EqualityComparer<T10>.Default;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9, T10 p10)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
            t9 = p9;
            t10 = p10;
        }

        public override int GetHashCode()
        {
            return PackUtils.GetHashCode(t1)
                ^ PackUtils.GetHashCode(t2)
                ^ PackUtils.GetHashCode(t3)
                ^ PackUtils.GetHashCode(t4)
                ^ PackUtils.GetHashCode(t5)
                ^ PackUtils.GetHashCode(t6)
                ^ PackUtils.GetHashCode(t7)
                ^ PackUtils.GetHashCode(t8)
                ^ PackUtils.GetHashCode(t9)
                ^ PackUtils.GetHashCode(t10)
                ;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>)
            {
                return Equals((Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>)obj);
            }
            return false;
        }
        public bool Equals(Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other)
        {
            return c1.Equals(t1, other.t1)
                && c2.Equals(t2, other.t2)
                && c3.Equals(t3, other.t3)
                && c4.Equals(t4, other.t4)
                && c5.Equals(t5, other.t5)
                && c6.Equals(t6, other.t6)
                && c7.Equals(t7, other.t7)
                && c8.Equals(t8, other.t8)
                && c9.Equals(t9, other.t9)
                && c10.Equals(t10, other.t10)
                ;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("(");
            sb.Append(PackUtils.ToString(t1));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t2));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t3));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t4));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t5));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t6));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t7));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t8));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t9));
            sb.Append(", ");
            sb.Append(PackUtils.ToString(t10));
            sb.Append(")");
            return sb.ToString();
        }
    }

    #region ValueArray
    public interface IValueArray : ITuple, IWritableTuple//, IList // TODO: IList
    {
        //int Length { get; }
        //object this[int index] { get; set; }
        Type ElementType { get; }
    }
    public interface IValueArray<T> : IValueArray//, IList<T> // TODO: IList
    {
        new T this[int index] { get; set; }
    }

    public struct ValueArray : IValueArray
    {
        public object this[int index]
        {
            get { throw new IndexOutOfRangeException(); }
            set { throw new IndexOutOfRangeException(); }
        }
        public int Length { get { return 0; } }
        public Type ElementType { get { return typeof(object); } }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray(ValueTuple t)
        {
            return new ValueArray();
        }
        public static implicit operator ValueTuple(ValueArray t)
        {
            return new ValueTuple();
        }
#endif

        public static ValueArray0<T> Arr<T>()
        {
            return new ValueArray0<T>();
        }
        public static ValueArray1<T> Arr<T>(T i0)
        {
            return new ValueArray1<T>(i0);
        }
        public static ValueArray2<T> Arr<T>(T i0, T i1)
        {
            return new ValueArray2<T>(i0, i1);
        }
        public static ValueArray3<T> Arr<T>(T i0, T i1, T i2)
        {
            return new ValueArray3<T>(i0, i1, i2);
        }
        public static ValueArray4<T> Arr<T>(T i0, T i1, T i2, T i3)
        {
            return new ValueArray4<T>(i0, i1, i2, i3);
        }
        public static ValueArray5<T> Arr<T>(T i0, T i1, T i2, T i3, T i4)
        {
            return new ValueArray5<T>(i0, i1, i2, i3, i4);
        }
        public static ValueArray6<T> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5)
        {
            return new ValueArray6<T>(i0, i1, i2, i3, i4, i5);
        }
        public static ValueArray7<T> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6)
        {
            return new ValueArray7<T>(i0, i1, i2, i3, i4, i5, i6);
        }
        public static ValueArray8<T> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7)
        {
            return new ValueArray8<T>(i0, i1, i2, i3, i4, i5, i6, i7);
        }
        public static ValueArrayEx<T, ValueArray1<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8)
        {
            return new ValueArrayEx<T, ValueArray1<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray1<T>(i8));
        }
        public static ValueArrayEx<T, ValueArray2<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9)
        {
            return new ValueArrayEx<T, ValueArray2<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray2<T>(i8, i9));
        }
        public static ValueArrayEx<T, ValueArray3<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10)
        {
            return new ValueArrayEx<T, ValueArray3<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray3<T>(i8, i9, i10));
        }
        public static ValueArrayEx<T, ValueArray4<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11)
        {
            return new ValueArrayEx<T, ValueArray4<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray4<T>(i8, i9, i10, i11));
        }
        public static ValueArrayEx<T, ValueArray5<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11, T i12)
        {
            return new ValueArrayEx<T, ValueArray5<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray5<T>(i8, i9, i10, i11, i12));
        }
        public static ValueArrayEx<T, ValueArray6<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11, T i12, T i13)
        {
            return new ValueArrayEx<T, ValueArray6<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray6<T>(i8, i9, i10, i11, i12, i13));
        }
        public static ValueArrayEx<T, ValueArray7<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11, T i12, T i13, T i14)
        {
            return new ValueArrayEx<T, ValueArray7<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray7<T>(i8, i9, i10, i11, i12, i13, i14));
        }
        public static ValueArrayEx<T, ValueArray8<T>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11, T i12, T i13, T i14, T i15)
        {
            return new ValueArrayEx<T, ValueArray8<T>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArray8<T>(i8, i9, i10, i11, i12, i13, i14, i15));
        }
        public static ValueArrayEx<T, ValueArrayEx<T, ValueArray1<T>>> Arr<T>(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, T i8, T i9, T i10, T i11, T i12, T i13, T i14, T i15, T i16)
        {
            return new ValueArrayEx<T, ValueArrayEx<T, ValueArray1<T>>>(i0, i1, i2, i3, i4, i5, i6, i7, new ValueArrayEx<T, ValueArray1<T>>(i8, i9, i10, i11, i12, i13, i14, i15, new ValueArray1<T>(i16)));
        }
    }
    public struct ValueArrayIndexAccessor<T, TValueArray> where TValueArray : struct, IValueArray<T>
    {
        public delegate T DelGetter(ref TValueArray thiz);
        public delegate void DelSetter(ref TValueArray thiz, T val);

        public DelGetter Getter;
        public DelSetter Setter;
    }
    public class ValueArrayIndexAccessorList<T, TValueArray> : List<ValueArrayIndexAccessor<T, TValueArray>> where TValueArray : struct, IValueArray<T>
    {
        public void Add(ValueArrayIndexAccessor<T, TValueArray>.DelGetter getter, ValueArrayIndexAccessor<T, TValueArray>.DelSetter setter)
        {
            Add(new ValueArrayIndexAccessor<T, TValueArray>() { Getter = getter, Setter = setter });
        }
        public T GetItem(ref TValueArray thiz, int index)
        {
            return this[index].Getter(ref thiz);
        }
        public void SetItem(ref TValueArray thiz, int index, T val)
        {
            this[index].Setter(ref thiz, val);
        }
    }
    // TODO: ValueArray's Equals, IEquatable<>, GetHashCode, ToString, ==, !=
    public struct ValueArray0<T> : IValueArray<T>
    {
        public T this[int index]
        {
            get { throw new IndexOutOfRangeException(); }
            set { throw new IndexOutOfRangeException(); }
        }
        public int Length { get { return 0; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { throw new IndexOutOfRangeException(); }
        }
        object IWritableTuple.this[int index]
        {
            get { throw new IndexOutOfRangeException(); }
            set { throw new IndexOutOfRangeException(); }
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray0<T>(ValueTuple t)
        {
            return new ValueArray0<T>();
        }
        public static implicit operator ValueTuple(ValueArray0<T> t)
        {
            return new ValueTuple();
        }
#endif
    }
    public struct ValueArray1<T> : IValueArray<T>
    {
        public T Item0;

        private static ValueArrayIndexAccessorList<T, ValueArray1<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray1<T>>()
        {
            { (ref ValueArray1<T> thiz) => thiz.Item0, (ref ValueArray1<T> thiz, T val) => thiz.Item0 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 1; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray1(T i0)
        {
            Item0 = i0;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray1<T>(ValueTuple<T> t)
        {
            return new ValueArray1<T>(t.Item1);
        }
        public static implicit operator ValueTuple<T>(ValueArray1<T> t)
        {
            return new ValueTuple<T>(t.Item0);
        }
#endif
    }
    public struct ValueArray2<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;

        private static ValueArrayIndexAccessorList<T, ValueArray2<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray2<T>>()
        {
            { (ref ValueArray2<T> thiz) => thiz.Item0, (ref ValueArray2<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray2<T> thiz) => thiz.Item1, (ref ValueArray2<T> thiz, T val) => thiz.Item1 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 2; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray2(T i0, T i1)
        {
            Item0 = i0;
            Item1 = i1;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray2<T>(ValueTuple<T, T> t)
        {
            return new ValueArray2<T>(t.Item1, t.Item2);
        }
        public static implicit operator ValueTuple<T, T>(ValueArray2<T> t)
        {
            return new ValueTuple<T, T>(t.Item0, t.Item1);
        }
#endif
    }
    public struct ValueArray3<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;

        private static ValueArrayIndexAccessorList<T, ValueArray3<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray3<T>>()
        {
            { (ref ValueArray3<T> thiz) => thiz.Item0, (ref ValueArray3<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray3<T> thiz) => thiz.Item1, (ref ValueArray3<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray3<T> thiz) => thiz.Item2, (ref ValueArray3<T> thiz, T val) => thiz.Item2 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 3; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray3(T i0, T i1, T i2)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray3<T>(ValueTuple<T, T, T> t)
        {
            return new ValueArray3<T>(t.Item1, t.Item2, t.Item3);
        }
        public static implicit operator ValueTuple<T, T, T>(ValueArray3<T> t)
        {
            return new ValueTuple<T, T, T>(t.Item0, t.Item1, t.Item2);
        }
#endif
    }
    public struct ValueArray4<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;

        private static ValueArrayIndexAccessorList<T, ValueArray4<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray4<T>>()
        {
            { (ref ValueArray4<T> thiz) => thiz.Item0, (ref ValueArray4<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray4<T> thiz) => thiz.Item1, (ref ValueArray4<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray4<T> thiz) => thiz.Item2, (ref ValueArray4<T> thiz, T val) => thiz.Item2 = val },
            { (ref ValueArray4<T> thiz) => thiz.Item3, (ref ValueArray4<T> thiz, T val) => thiz.Item3 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 4; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray4(T i0, T i1, T i2, T i3)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
            Item3 = i3;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray4<T>(ValueTuple<T, T, T, T> t)
        {
            return new ValueArray4<T>(t.Item1, t.Item2, t.Item3, t.Item4);
        }
        public static implicit operator ValueTuple<T, T, T, T>(ValueArray4<T> t)
        {
            return new ValueTuple<T, T, T, T>(t.Item0, t.Item1, t.Item2, t.Item3);
        }
#endif
    }
    public struct ValueArray5<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;
        public T Item4;

        private static ValueArrayIndexAccessorList<T, ValueArray5<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray5<T>>()
        {
            { (ref ValueArray5<T> thiz) => thiz.Item0, (ref ValueArray5<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray5<T> thiz) => thiz.Item1, (ref ValueArray5<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray5<T> thiz) => thiz.Item2, (ref ValueArray5<T> thiz, T val) => thiz.Item2 = val },
            { (ref ValueArray5<T> thiz) => thiz.Item3, (ref ValueArray5<T> thiz, T val) => thiz.Item3 = val },
            { (ref ValueArray5<T> thiz) => thiz.Item4, (ref ValueArray5<T> thiz, T val) => thiz.Item4 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 5; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray5(T i0, T i1, T i2, T i3, T i4)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
            Item3 = i3;
            Item4 = i4;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray5<T>(ValueTuple<T, T, T, T, T> t)
        {
            return new ValueArray5<T>(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5);
        }
        public static implicit operator ValueTuple<T, T, T, T, T>(ValueArray5<T> t)
        {
            return new ValueTuple<T, T, T, T, T>(t.Item0, t.Item1, t.Item2, t.Item3, t.Item4);
        }
#endif
    }
    public struct ValueArray6<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;
        public T Item4;
        public T Item5;

        private static ValueArrayIndexAccessorList<T, ValueArray6<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray6<T>>()
        {
            { (ref ValueArray6<T> thiz) => thiz.Item0, (ref ValueArray6<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray6<T> thiz) => thiz.Item1, (ref ValueArray6<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray6<T> thiz) => thiz.Item2, (ref ValueArray6<T> thiz, T val) => thiz.Item2 = val },
            { (ref ValueArray6<T> thiz) => thiz.Item3, (ref ValueArray6<T> thiz, T val) => thiz.Item3 = val },
            { (ref ValueArray6<T> thiz) => thiz.Item4, (ref ValueArray6<T> thiz, T val) => thiz.Item4 = val },
            { (ref ValueArray6<T> thiz) => thiz.Item5, (ref ValueArray6<T> thiz, T val) => thiz.Item5 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 6; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray6(T i0, T i1, T i2, T i3, T i4, T i5)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
            Item3 = i3;
            Item4 = i4;
            Item5 = i5;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray6<T>(ValueTuple<T, T, T, T, T, T> t)
        {
            return new ValueArray6<T>(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6);
        }
        public static implicit operator ValueTuple<T, T, T, T, T, T>(ValueArray6<T> t)
        {
            return new ValueTuple<T, T, T, T, T, T>(t.Item0, t.Item1, t.Item2, t.Item3, t.Item4, t.Item5);
        }
#endif
    }
    public struct ValueArray7<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;
        public T Item4;
        public T Item5;
        public T Item6;

        private static ValueArrayIndexAccessorList<T, ValueArray7<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray7<T>>()
        {
            { (ref ValueArray7<T> thiz) => thiz.Item0, (ref ValueArray7<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item1, (ref ValueArray7<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item2, (ref ValueArray7<T> thiz, T val) => thiz.Item2 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item3, (ref ValueArray7<T> thiz, T val) => thiz.Item3 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item4, (ref ValueArray7<T> thiz, T val) => thiz.Item4 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item5, (ref ValueArray7<T> thiz, T val) => thiz.Item5 = val },
            { (ref ValueArray7<T> thiz) => thiz.Item6, (ref ValueArray7<T> thiz, T val) => thiz.Item6 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 7; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray7(T i0, T i1, T i2, T i3, T i4, T i5, T i6)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
            Item3 = i3;
            Item4 = i4;
            Item5 = i5;
            Item6 = i6;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray7<T>(ValueTuple<T, T, T, T, T, T, T> t)
        {
            return new ValueArray7<T>(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7);
        }
        public static implicit operator ValueTuple<T, T, T, T, T, T, T>(ValueArray7<T> t)
        {
            return new ValueTuple<T, T, T, T, T, T, T>(t.Item0, t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6);
        }
#endif
    }
    public struct ValueArray8<T> : IValueArray<T>
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;
        public T Item4;
        public T Item5;
        public T Item6;
        public T Item7;

        private static ValueArrayIndexAccessorList<T, ValueArray8<T>> _IndexAccessors = new ValueArrayIndexAccessorList<T, ValueArray8<T>>()
        {
            { (ref ValueArray8<T> thiz) => thiz.Item0, (ref ValueArray8<T> thiz, T val) => thiz.Item0 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item1, (ref ValueArray8<T> thiz, T val) => thiz.Item1 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item2, (ref ValueArray8<T> thiz, T val) => thiz.Item2 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item3, (ref ValueArray8<T> thiz, T val) => thiz.Item3 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item4, (ref ValueArray8<T> thiz, T val) => thiz.Item4 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item5, (ref ValueArray8<T> thiz, T val) => thiz.Item5 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item6, (ref ValueArray8<T> thiz, T val) => thiz.Item6 = val },
            { (ref ValueArray8<T> thiz) => thiz.Item7, (ref ValueArray8<T> thiz, T val) => thiz.Item7 = val },
        };

        public T this[int index]
        {
            get { return _IndexAccessors.GetItem(ref this, index); }
            set { _IndexAccessors.SetItem(ref this, index, value); }
        }
        public int Length { get { return 8; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArray8(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7)
        {
            Item0 = i0;
            Item1 = i1;
            Item2 = i2;
            Item3 = i3;
            Item4 = i4;
            Item5 = i5;
            Item6 = i6;
            Item7 = i7;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        public static implicit operator ValueArray8<T>((T, T, T, T, T, T, T, T) t)
        {
            return new ValueArray8<T>(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7, t.Item8);
        }
        public static implicit operator (T, T, T, T, T, T, T, T)(ValueArray8<T> t)
        {
            return (t.Item0, t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7);
        }
#endif
    }
    public struct ValueArrayEx<T, TRest> : IValueArray<T> where TRest : struct, IValueArray<T>
    {
        public ValueArray8<T> ItemsLow;
        public TRest ItemsRest;

        public T this[int index]
        {
            get
            {
                if (index >= 0)
                {
                    if (index < ItemsLow.Length)
                    {
                        return ItemsLow[index];
                    }
                    else
                    {
                        return ItemsRest[index - ItemsLow.Length];
                    }
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index >= 0)
                {
                    if (index < ItemsLow.Length)
                    {
                        ItemsLow[index] = value;
                        return;
                    }
                    else
                    {
                        ItemsRest[index - ItemsLow.Length] = value;
                        return;
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }
        public int Length { get { return ItemsLow.Length + ItemsRest.Length; } }
        public Type ElementType { get { return typeof(T); } }
        object ITuple.this[int index]
        {
            get { return this[index]; }
        }
        object IWritableTuple.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public ValueArrayEx(T i0, T i1, T i2, T i3, T i4, T i5, T i6, T i7, TRest rest)
        {
            ItemsLow = new ValueArray8<T>(i0, i1, i2, i3, i4, i5, i6, i7);
            ItemsRest = rest;
        }
    }
    #endregion

    public struct ValueList<T> : IList<T>, IEquatable<ValueList<T>>
    {
        private T t0;
        private T t1;
        private T t2;
        private T t3;
        private T t4;
        private T t5;
        private T t6;
        private T t7;
        private T t8;
        private T t9;
        private List<T> tx;

        private int _cnt;

#region static funcs for set and get
        private delegate T GetTDel(ref ValueList<T> list);
        private delegate void SetTDel(ref ValueList<T> list, T val);

        private static T GetT0(ref ValueList<T> list) { return list.t0; }
        private static T GetT1(ref ValueList<T> list) { return list.t1; }
        private static T GetT2(ref ValueList<T> list) { return list.t2; }
        private static T GetT3(ref ValueList<T> list) { return list.t3; }
        private static T GetT4(ref ValueList<T> list) { return list.t4; }
        private static T GetT5(ref ValueList<T> list) { return list.t5; }
        private static T GetT6(ref ValueList<T> list) { return list.t6; }
        private static T GetT7(ref ValueList<T> list) { return list.t7; }
        private static T GetT8(ref ValueList<T> list) { return list.t8; }
        private static T GetT9(ref ValueList<T> list) { return list.t9; }

        private static void SetT0(ref ValueList<T> list, T val) { list.t0 = val; }
        private static void SetT1(ref ValueList<T> list, T val) { list.t1 = val; }
        private static void SetT2(ref ValueList<T> list, T val) { list.t2 = val; }
        private static void SetT3(ref ValueList<T> list, T val) { list.t3 = val; }
        private static void SetT4(ref ValueList<T> list, T val) { list.t4 = val; }
        private static void SetT5(ref ValueList<T> list, T val) { list.t5 = val; }
        private static void SetT6(ref ValueList<T> list, T val) { list.t6 = val; }
        private static void SetT7(ref ValueList<T> list, T val) { list.t7 = val; }
        private static void SetT8(ref ValueList<T> list, T val) { list.t8 = val; }
        private static void SetT9(ref ValueList<T> list, T val) { list.t9 = val; }

        private static GetTDel[] GetTFuncs = new GetTDel[]
        {
            GetT0,
            GetT1,
            GetT2,
            GetT3,
            GetT4,
            GetT5,
            GetT6,
            GetT7,
            GetT8,
            GetT9,
        };
        private static SetTDel[] SetTFuncs = new SetTDel[]
        {
            SetT0,
            SetT1,
            SetT2,
            SetT3,
            SetT4,
            SetT5,
            SetT6,
            SetT7,
            SetT8,
            SetT9,
        };
#endregion

#region IList<T>
        public int IndexOf(T item)
        {
            for (int i = 0; i < _cnt; ++i)
            {
                if (object.Equals(this[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index >= 0 && index <= _cnt)
            {
                this.Add(default(T));
                for (int i = _cnt - 1; i > index; --i)
                {
                    this[i] = this[i - 1];
                }
                this[index] = item;
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _cnt)
            {
                for (int i = index + 1; i < _cnt; ++i)
                {
                    this[i - 1] = this[i];
                }
                this[_cnt - 1] = default(T);
                --_cnt;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < GetTFuncs.Length)
                    {
                        return GetTFuncs[index](ref this);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - GetTFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                return tx[pindex];
                            }
                        }
                    }
                }
                return default(T);
            }
            set
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < SetTFuncs.Length)
                    {
                        SetTFuncs[index](ref this, value);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - SetTFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                tx[pindex] = value;
                            }
                        }
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_cnt < SetTFuncs.Length)
            {
                this[_cnt++] = item;
            }
            else
            {
                ++_cnt;
                if (tx == null)
                {
                    tx = new List<T>(8);
                }
                tx.Add(item);
            }
        }
        public void AddRange<ET>(ET list) where ET : IEnumerable<T>
        {
            foreach (var value in list)
            {
                Add(value);
            }
        }
        public void AddRange(IEnumerable<T> list)
        {
            foreach (var value in list)
            {
                Add(value);
            }
        }
        public void AddRange<ET>(ET list, int start, int count) where ET : IList<T>
        {
            for (int i = 0; i < count; ++i)
            {
                Add(list[start + i]);
            }
        }
        public void AddRange<ET>(ET list, int start) where ET : IList<T>
        {
            for (int i = start; i < list.Count; ++i)
            {
                Add(list[i]);
            }
        }
        public void Merge<ET>(ET list) where ET : IList<T>
        {
            for (int i = 0; i < list.Count; ++i)
            {
                Add(list[i]);
            }
        }
        public void AddRange(IList<T> list, int start, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                Add(list[start + i]);
            }
        }
        public void AddRange(IList<T> list, int start)
        {
            for (int i = start; i < list.Count; ++i)
            {
                Add(list[i]);
            }
        }
        public void Merge(IList<T> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                Add(list[i]);
            }
        }

        public void Clear()
        {
            _cnt = 0;
            t0 = default(T);
            t1 = default(T);
            t2 = default(T);
            t3 = default(T);
            t4 = default(T);
            t5 = default(T);
            t6 = default(T);
            t7 = default(T);
            t8 = default(T);
            t9 = default(T);
            tx = null;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex >= 0)
            {
                for (int i = 0; i < _cnt && i + arrayIndex < array.Length; ++i)
                {
                    array[arrayIndex + i] = this[i];
                }
            }
        }

        public int Count
        {
            get { return _cnt; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0 && index < _cnt)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _cnt; ++i)
            {
                yield return this[i];
            }
        }
#endregion

        public T[] ToArray()
        {
            T[] arr = new T[_cnt];
            CopyTo(arr, 0);
            return arr;
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueList<T>)
            {
                ValueList<T> types2 = (ValueList<T>)obj;
                if (types2._cnt == _cnt)
                {
                    for (int i = 0; i < _cnt; ++i)
                    {
                        if (!object.Equals(this[i], types2[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        private static IEqualityComparer<T> _Comparer = EqualityComparer<T>.Default;
        public bool Equals(ValueList<T> other)
        {
            if (other._cnt == _cnt)
            {
                for (int i = 0; i < _cnt; ++i)
                {
                    if (!_Comparer.Equals(this[i], other[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        internal static bool OpEquals(ValueList<T> source, ValueList<T> other)
        {
            return source.Equals(other);
        }
        public static bool operator ==(ValueList<T> source, ValueList<T> other)
        {
            return OpEquals(source, other);
        }
        public static bool operator !=(ValueList<T> source, ValueList<T> other)
        {
            return !OpEquals(source, other);
        }

        public override int GetHashCode()
        {
            int code = 0;
            for (int i = 0; i < Count; ++i)
            {
                code <<= 1;
                var type = this[i];
                if (type != null)
                {
                    code += type.GetHashCode();
                }
            }
            return code;
        }
    }

    public static class EnumUtils
    { // TODO: use ByRefUtils.dll to do the convert quickly
        public static T ConvertToEnum<T>(ulong val) where T : struct
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && (!NET_4_6 && !NET_STANDARD_2_0 || !NET_EX_LIB_UNSAFE) && (!UNITY_2021_1_OR_NEWER && !NET_UNITY_4_8) || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER && !NET && !NETCOREAPP
            return (T)Enum.ToObject(typeof(T), val);
#else
            Span<ulong> span = stackalloc[] { val };
            var tspan = System.Runtime.InteropServices.MemoryMarshal.Cast<ulong, T>(span);

            if (BitConverter.IsLittleEndian)
            {
                return tspan[0];
            }
            else
            {
                return tspan[8 / System.Runtime.InteropServices.Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))) - 1];
            }
#endif
        }
        public static ulong ConvertFromEnum<T>(T val) where T : struct
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && (!NET_4_6 && !NET_STANDARD_2_0 || !NET_EX_LIB_UNSAFE) && (!UNITY_2021_1_OR_NEWER && !NET_UNITY_4_8) || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER && !NET && !NETCOREAPP
            return Convert.ToUInt64(val);
#else
            Span<ulong> span = stackalloc ulong[1];
            var tspan = System.Runtime.InteropServices.MemoryMarshal.Cast<ulong, T>(span);

            if (BitConverter.IsLittleEndian)
            {
                tspan[0] = val;
            }
            else
            {
                tspan[8 / System.Runtime.InteropServices.Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))) - 1] = val;
            }
            return span[0];
#endif
        }
        public static bool TryConvertStrToEnum<T>(this string str, out T val)
        {
            return EnumConverter<T>.TryConvertStrToEnum(str, out val);
        }
        public static T ConvertStrToEnum<T>(this string str)
        {
            return EnumConverter<T>.ConvertStrToEnum(str);
        }
        public static string ConvertEnumToStr<T>(this T val)
        {
            return EnumConverter<T>.ConvertEnumToStr(val);
        }

        private enum Enum8 : byte { }
        private enum Enum16 : short { }
        private enum Enum32 : int { }
        private enum Enum64 : long { }
        [UnityPreserve]
        private static void AOTCompileEnumConverter()
        {
            {
                var funcTo = EnumConverter<Enum8>.ConvertToEnum;
                var funcFrom = EnumConverter<Enum8>.ConvertFromEnum;
                funcTo = ConvertToEnum<Enum8>;
                funcFrom = ConvertFromEnum<Enum8>;
            }
            {
                var funcTo = EnumConverter<Enum16>.ConvertToEnum;
                var funcFrom = EnumConverter<Enum16>.ConvertFromEnum;
                funcTo = ConvertToEnum<Enum16>;
                funcFrom = ConvertFromEnum<Enum16>;
            }
            {
                var funcTo = EnumConverter<Enum32>.ConvertToEnum;
                var funcFrom = EnumConverter<Enum32>.ConvertFromEnum;
                funcTo = ConvertToEnum<Enum32>;
                funcFrom = ConvertFromEnum<Enum32>;
            }
            {
                var funcTo = EnumConverter<Enum64>.ConvertToEnum;
                var funcFrom = EnumConverter<Enum64>.ConvertFromEnum;
                funcTo = ConvertToEnum<Enum64>;
                funcFrom = ConvertFromEnum<Enum64>;
            }
        }
        internal static class EnumConverter<T>
        {
            public static readonly Func<ulong, T> ConvertToEnum;
            public static readonly Func<T, ulong> ConvertFromEnum;
            private static readonly Dictionary<T, string> _EnumToNameCache = new Dictionary<T, string>();
            private static readonly Dictionary<string, T> _NameToEnumCache = new Dictionary<string, T>();

            static EnumConverter()
            {
                Func<ulong, System.TypeCode> templateTo = ConvertToEnum<System.TypeCode>;
                var templateToMethod = templateTo.Method;
                var toMethod = templateToMethod.GetGenericMethodDefinition().MakeGenericMethod(typeof(T));
                ConvertToEnum = (Func<ulong, T>)toMethod.CreateDelegate(typeof(Func<ulong, T>));

                Func<System.TypeCode, ulong> templateFrom = ConvertFromEnum<System.TypeCode>;
                var templateFromMethod = templateFrom.Method;
                var fromMethod = templateFromMethod.GetGenericMethodDefinition().MakeGenericMethod(typeof(T));
                ConvertFromEnum = (Func<T, ulong>)fromMethod.CreateDelegate(typeof(Func<T, ulong>));
            }

            public static T ConvertStrToEnum(string str)
            {
                T val;
                TryConvertStrToEnum(str, out val);
                return val;
            }
            public static bool TryConvertStrToEnum(string str, out T val)
            {
                if (string.IsNullOrEmpty(str))
                {
                    val = default(T);
                    return false;
                }
                if (!_NameToEnumCache.TryGetValue(str, out val))
                {
                    try
                    {
                        val = (T)Enum.Parse(typeof(T), str);
                    }
                    catch
                    {
                        val = default(T);
                        return false;
                    }
                    _NameToEnumCache[str] = val;
                    _EnumToNameCache[val] = str;
                }
                return true;
            }
            public static string ConvertEnumToStr(T val)
            {
                string name;
                if (!_EnumToNameCache.TryGetValue(val, out name))
                {
                    name = val.ToString();
                    _EnumToNameCache[val] = name;
                    _NameToEnumCache[name] = val;
                }
                return name;
            }
        }
        public static T ConvertToEnumForcibly<T>(ulong val)
        {
            return EnumConverter<T>.ConvertToEnum(val);
        }
        public static ulong ConvertFromEnumForcibly<T>(T val)
        {
            return EnumConverter<T>.ConvertFromEnum(val);
        }
    }
    public interface IConvertibleDictionary
    {
        T Get<T>(string key);
        void Set<T>(string key, T val); 
    }

    public static class ConvertUtils
    {
        public static T As<T>(this object val)
        {
            return val is T ? (T)val : default(T);
        }

        private static HashSet<Type> NumericTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };
        private static HashSet<Type> ConvertibleTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),

            typeof(char),
            typeof(string),
            typeof(IntPtr),
        };

        public interface ITypedConverter<T>
        {
            T Convert(object obj);
        }
        public class TypedConverter
        {
            protected Type _ToType;
            public Type ToType
            {
                get { return _ToType; }
            }

            public Func<object, object> ConvertRawFunc;
            public virtual object ConvertRaw(object obj)
            {
                var func = ConvertRawFunc;
                if (func != null)
                {
                    return func(obj);
                }
                return null;
            }
        }
        public static bool ToBoolean(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is bool)
            {
                return (bool)obj;
            }
            if (obj is string)
            {
                var str = (string)obj;
                float f;
                if (float.TryParse(str, out f))
                {
                    return f != 0.0f;
                }
                str = str.ToLower().Trim();
                if (str == "" || str == "n" || str == "no" || str == "f" || str == "false")
                {
                    return false;
                }
                return true;
            }
            else if (obj is IntPtr)
            {
                return ((IntPtr)obj) != IntPtr.Zero;
            }
            else if (obj is UIntPtr)
            {
                return ((UIntPtr)obj) != UIntPtr.Zero;
            }
            if (PlatDependant.IsObjIConvertible(obj))
            {
                try
                {
                    return System.Convert.ToBoolean(obj);
                }
                catch { }
            }
            return true;
        }
        public class TypedConverter<T> : TypedConverter, ITypedConverter<T>
        { // TODO: unmanaged value type converter using ByRefUtils
            public TypedConverter()
            {
                _ToType = typeof(T);
            }
            public TypedConverter(Func<object, T> convertFunc)
                : this()
            {
                ConvertFunc = convertFunc;
            }

            public Func<object, T> ConvertFunc;
            public T Convert(object obj)
            {
                var func = ConvertFunc;
                if (func != null)
                {
                    return func(obj);
                }
                else
                {
                    var funcraw = ConvertRawFunc;
                    if (funcraw != null)
                    {
                        return (T)funcraw(obj);
                    }
                }
                return default(T);
            }
            public override object ConvertRaw(object obj)
            {
                var func = ConvertFunc;
                if (func != null)
                {
                    return func(obj);
                }
                else
                {
                    var funcraw = ConvertRawFunc;
                    if (funcraw != null)
                    {
                        return funcraw(obj);
                    }
                }
                return default(T);
            }
        }
        public class TypedValueTypeConverter<T> : TypedConverter<T>, ITypedConverter<T?> where T : struct
        {
            public TypedValueTypeConverter(Func<object, T> convertFunc) : base(convertFunc)
            { }
            public TypedValueTypeConverter() : base() { }
            T? ITypedConverter<T?>.Convert(object obj)
            {
                if (obj == null)
                {
                    return null;
                }
                return Convert(obj);
            }
        }
        public static readonly Dictionary<Type, TypedConverter> _TypedConverters = new Dictionary<Type, TypedConverter>()
        {
            { typeof(bool), new TypedValueTypeConverter<bool>(ToBoolean) },
            { typeof(string), new TypedConverter<string>(
                obj =>
                {
                    if (obj == null)
                    {
                        return null;
                    }
                    if (obj is string)
                    {
                        return (string)obj;
                    }
                    else if (obj is byte[])
                    {
                        return System.Text.Encoding.UTF8.GetString(obj as byte[]);
                    }
                    return obj.ToString();
                })
            },
            { typeof(byte[]), new TypedConverter<byte[]>(
                obj =>
                {
                    if (obj == null)
                    {
                        return null;
                    }
                    if (obj is byte[])
                    {
                        return (byte[])obj;
                    }
                    else if (obj is string)
                    {
                        return System.Text.Encoding.UTF8.GetBytes(obj as string);
                    }
                    return null;
                })
            },
            { typeof(byte), new TypedValueTypeConverter<byte>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is byte)
                    {
                        return (byte)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        byte rv;
                        byte.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (byte)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (byte)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToByte(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(sbyte), new TypedValueTypeConverter<sbyte>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is sbyte)
                    {
                        return (sbyte)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        sbyte rv;
                        sbyte.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (sbyte)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (sbyte)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToSByte(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(short), new TypedValueTypeConverter<short>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is short)
                    {
                        return (short)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        short rv;
                        short.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (short)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (short)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt16(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(ushort), new TypedValueTypeConverter<ushort>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is ushort)
                    {
                        return (ushort)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ushort rv;
                        ushort.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (ushort)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (ushort)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt16(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(int), new TypedValueTypeConverter<int>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is int)
                    {
                        return (int)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        int rv;
                        int.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (int)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (int)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt32(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(uint), new TypedValueTypeConverter<uint>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is uint)
                    {
                        return (uint)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        uint rv;
                        uint.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (uint)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (uint)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt32(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(long), new TypedValueTypeConverter<long>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is long)
                    {
                        return (long)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        long rv;
                        long.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (long)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (long)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt64(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(ulong), new TypedValueTypeConverter<ulong>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is ulong)
                    {
                        return (ulong)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (ulong)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (ulong)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(char), new TypedValueTypeConverter<char>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(char);
                    }
                    if (obj is char)
                    {
                        return (char)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        char rv;
                        char.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (char)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (char)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToChar(obj);
                        }
                        catch { }
                    }
                    return default(char);
                })
            },
            { typeof(IntPtr), new TypedValueTypeConverter<IntPtr>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(IntPtr);
                    }
                    if (obj is IntPtr)
                    {
                        return (IntPtr)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return (IntPtr)rv;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (IntPtr)(ulong)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return (IntPtr)System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return default(IntPtr);
                })
            },
            { typeof(UIntPtr), new TypedValueTypeConverter<UIntPtr>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(UIntPtr);
                    }
                    if (obj is UIntPtr)
                    {
                        return (UIntPtr)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return (UIntPtr)rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (UIntPtr)(ulong)(IntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return (UIntPtr)System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return default(UIntPtr);
                })
            },
            { typeof(float), new TypedValueTypeConverter<float>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is float)
                    {
                        return (float)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        float rv;
                        float.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (float)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (float)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToSingle(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(double), new TypedValueTypeConverter<double>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is double)
                    {
                        return (double)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        double rv;
                        double.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (double)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (double)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDouble(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(decimal), new TypedValueTypeConverter<decimal>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is decimal)
                    {
                        return (decimal)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        decimal rv;
                        decimal.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (decimal)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (decimal)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDecimal(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(TimeSpan), new TypedValueTypeConverter<TimeSpan>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(TimeSpan);
                    }
                    if (obj is TimeSpan)
                    {
                        return (TimeSpan)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        TimeSpan rv;
                        TimeSpan.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return new TimeSpan((long)(IntPtr)obj);
                    }
                    else if (obj is UIntPtr)
                    {
                        return new TimeSpan((long)(UIntPtr)obj);
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return new TimeSpan(System.Convert.ToInt64(obj));
                        }
                        catch { }
                    }
                    return default(TimeSpan);
                })
            },
            { typeof(DateTime), new TypedValueTypeConverter<DateTime>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(DateTime);
                    }
                    if (obj is DateTime)
                    {
                        return (DateTime)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        DateTime rv;
                        DateTime.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return new DateTime((long)(IntPtr)obj);
                    }
                    else if (obj is UIntPtr)
                    {
                        return new DateTime((long)(UIntPtr)obj);
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDateTime(obj);
                        }
                        catch { }
                    }
                    return default(DateTime);
                })
            },
        };
        public static T Convert<T>(this object obj)
        {
            var type = typeof(T);
            var uutype = Nullable.GetUnderlyingType(type);
            TypedConverter converter;
            if (_TypedConverters.TryGetValue(uutype ?? type, out converter))
            {
                ITypedConverter<T> tconverter = converter as ITypedConverter<T>;
                if (tconverter != null)
                {
                    return tconverter.Convert(obj);
                }
            }
            if (obj == null)
                return default(T);
            if (obj is T)
                return (T)obj;
            if (type.IsEnum())
            {
                if (obj is string)
                {
                    return EnumUtils.ConvertStrToEnum<T>(obj as string);
                }
                else if (NumericTypes.Contains(obj.GetType()))
                {
#if CONVERT_ENUM_SAFELY
                    return (T)Enum.ToObject(type, (object)System.Convert.ToUInt64(obj));
#else
                    return EnumUtils.ConvertToEnumForcibly<T>(System.Convert.ToUInt64(obj));
#endif
                }
                else if (obj is Enum)
                {
#if CONVERT_ENUM_SAFELY
                    return (T)System.Convert.ChangeType(System.Convert.ToUInt64(obj), type);
#else
                    return EnumUtils.ConvertToEnumForcibly<T>(System.Convert.ToUInt64(obj));
#endif
                }
                else
                {
                    return default(T);
                }
            }
            else if (uutype != null && uutype.IsEnum())
            {
                if (obj is string)
                {
                    return (T)Enum.Parse(uutype, obj as string);
                }
                else if (NumericTypes.Contains(obj.GetType()))
                {
                    var num = System.Convert.ToUInt64(obj);
                    return (T)Enum.ToObject(uutype, num);
                }
                else if (obj is Enum)
                {
                    var num = System.Convert.ToUInt64(obj);
                    return (T)Enum.ToObject(uutype, num);
                }
                else
                {
                    return default(T);
                }
            }
            return default(T);
        }

        public abstract class TypedValueConverter
        {
            protected Type _FromType;
            protected Type _ToType;
        }
        public class TypedValueConverter<F, T> : TypedValueConverter
        {
            public TypedValueConverter(Func<F, T> convFunc)
            {
                _FromType = typeof(F);
                _ToType = typeof(T);
                ConvertFunc = convFunc;
            }
            public Func<F, T> ConvertFunc;
        }
        public class TypedNullableConverterWrapper<F, T> where F : struct where T : struct
        {
            public Func<F, T> ConvertFunc;
            public Func<T, F> ConvertBackFunc;

            public TypedNullableConverterWrapper()
            {
                TypedValueConverter rawconverter;
                _TypedValueConverters.TryGetValue(new Pack<Type, Type>(typeof(F), typeof(T)), out rawconverter);
                TypedValueConverter<F, T> converter = rawconverter as TypedValueConverter<F, T>;
                if (converter != null)
                {
                    ConvertFunc = converter.ConvertFunc;
                }

                _TypedValueConverters.TryGetValue(new Pack<Type, Type>(typeof(T), typeof(F)), out rawconverter);
                TypedValueConverter<T, F> converterback = rawconverter as TypedValueConverter<T, F>;
                if (converterback != null)
                {
                    ConvertBackFunc = converterback.ConvertFunc;
                }

                _TypedValueConverters[new Pack<Type, Type>(typeof(F?), typeof(T))] = new TypedValueConverter<F?, T>(ConvertNF2T);
                _TypedValueConverters[new Pack<Type, Type>(typeof(F), typeof(T?))] = new TypedValueConverter<F, T?>(ConvertF2NT);
                _TypedValueConverters[new Pack<Type, Type>(typeof(F?), typeof(T?))] = new TypedValueConverter<F?, T?>(ConvertNF2NT);
                _TypedValueConverters[new Pack<Type, Type>(typeof(T?), typeof(F))] = new TypedValueConverter<T?, F>(ConvertNT2F);
                _TypedValueConverters[new Pack<Type, Type>(typeof(T), typeof(F?))] = new TypedValueConverter<T, F?>(ConvertT2NF);
                _TypedValueConverters[new Pack<Type, Type>(typeof(T?), typeof(F?))] = new TypedValueConverter<T?, F?>(ConvertNT2NF);
            }
            public bool TryConvert(F val, out T converted)
            {
                if (ConvertFunc != null)
                {
                    converted = ConvertFunc(val);
                    return true;
                }
                else
                {
                    return Convert<F, T>(val, out converted);
                }
            }
            public bool TryConvertBack(T val, out F converted)
            {
                if (ConvertBackFunc != null)
                {
                    converted = ConvertBackFunc(val);
                    return true;
                }
                else
                {
                    return Convert<T, F>(val, out converted);
                }
            }
            public T Convert(F val)
            {
                T converted;
                TryConvert(val, out converted);
                return converted;
            }
            public F ConvertBack(T val)
            {
                F converted;
                TryConvertBack(val, out converted);
                return converted;
            }

            public T ConvertNF2T(F? val)
            {
                if (val == null)
                {
                    return default(T);
                }
                return Convert(val.Value);
            }
            public T? ConvertF2NT(F val)
            {
                T converted;
                var success = TryConvert(val, out converted);
                if (success)
                {
                    return converted;
                }
                else
                {
                    return null;
                }
            }
            public T? ConvertNF2NT(F? val)
            {
                if (val == null)
                {
                    return null;
                }
                return ConvertF2NT(val.Value);
            }
            public F ConvertNT2F(T? val)
            {
                if (val == null)
                {
                    return default(F);
                }
                return ConvertBack(val.Value);
            }
            public F? ConvertT2NF(T val)
            {
                F converted;
                var success = TryConvertBack(val, out converted);
                if (success)
                {
                    return converted;
                }
                else
                {
                    return null;
                }
            }
            public F? ConvertNT2NF(T? val)
            {
                if (val == null)
                {
                    return null;
                }
                return ConvertT2NF(val.Value);
            }
        }
        static ConvertUtils()
        {
            new TypedNullableConverterWrapper<bool, bool>();
            new TypedNullableConverterWrapper<bool, byte>();
            new TypedNullableConverterWrapper<bool, sbyte>();
            new TypedNullableConverterWrapper<bool, short>();
            new TypedNullableConverterWrapper<bool, ushort>();
            new TypedNullableConverterWrapper<bool, int>();
            new TypedNullableConverterWrapper<bool, uint>();
            new TypedNullableConverterWrapper<bool, long>();
            new TypedNullableConverterWrapper<bool, ulong>();
            new TypedNullableConverterWrapper<bool, IntPtr>();
            new TypedNullableConverterWrapper<bool, UIntPtr>();
            new TypedNullableConverterWrapper<bool, float>();
            new TypedNullableConverterWrapper<bool, double>();

            new TypedNullableConverterWrapper<byte, byte>();
            new TypedNullableConverterWrapper<byte, sbyte>();
            new TypedNullableConverterWrapper<byte, short>();
            new TypedNullableConverterWrapper<byte, ushort>();
            new TypedNullableConverterWrapper<byte, int>();
            new TypedNullableConverterWrapper<byte, uint>();
            new TypedNullableConverterWrapper<byte, long>();
            new TypedNullableConverterWrapper<byte, ulong>();
            new TypedNullableConverterWrapper<byte, IntPtr>();
            new TypedNullableConverterWrapper<byte, UIntPtr>();
            new TypedNullableConverterWrapper<byte, float>();
            new TypedNullableConverterWrapper<byte, double>();

            new TypedNullableConverterWrapper<sbyte, sbyte>();
            new TypedNullableConverterWrapper<sbyte, short>();
            new TypedNullableConverterWrapper<sbyte, ushort>();
            new TypedNullableConverterWrapper<sbyte, int>();
            new TypedNullableConverterWrapper<sbyte, uint>();
            new TypedNullableConverterWrapper<sbyte, long>();
            new TypedNullableConverterWrapper<sbyte, ulong>();
            new TypedNullableConverterWrapper<sbyte, IntPtr>();
            new TypedNullableConverterWrapper<sbyte, UIntPtr>();
            new TypedNullableConverterWrapper<sbyte, float>();
            new TypedNullableConverterWrapper<sbyte, double>();

            new TypedNullableConverterWrapper<short, short>();
            new TypedNullableConverterWrapper<short, ushort>();
            new TypedNullableConverterWrapper<short, int>();
            new TypedNullableConverterWrapper<short, uint>();
            new TypedNullableConverterWrapper<short, long>();
            new TypedNullableConverterWrapper<short, ulong>();
            new TypedNullableConverterWrapper<short, IntPtr>();
            new TypedNullableConverterWrapper<short, UIntPtr>();
            new TypedNullableConverterWrapper<short, float>();
            new TypedNullableConverterWrapper<short, double>();

            new TypedNullableConverterWrapper<ushort, ushort>();
            new TypedNullableConverterWrapper<ushort, int>();
            new TypedNullableConverterWrapper<ushort, uint>();
            new TypedNullableConverterWrapper<ushort, long>();
            new TypedNullableConverterWrapper<ushort, ulong>();
            new TypedNullableConverterWrapper<ushort, IntPtr>();
            new TypedNullableConverterWrapper<ushort, UIntPtr>();
            new TypedNullableConverterWrapper<ushort, float>();
            new TypedNullableConverterWrapper<ushort, double>();

            new TypedNullableConverterWrapper<int, int>();
            new TypedNullableConverterWrapper<int, uint>();
            new TypedNullableConverterWrapper<int, long>();
            new TypedNullableConverterWrapper<int, ulong>();
            new TypedNullableConverterWrapper<int, IntPtr>();
            new TypedNullableConverterWrapper<int, UIntPtr>();
            new TypedNullableConverterWrapper<int, float>();
            new TypedNullableConverterWrapper<int, double>();

            new TypedNullableConverterWrapper<uint, uint>();
            new TypedNullableConverterWrapper<uint, long>();
            new TypedNullableConverterWrapper<uint, ulong>();
            new TypedNullableConverterWrapper<uint, IntPtr>();
            new TypedNullableConverterWrapper<uint, UIntPtr>();
            new TypedNullableConverterWrapper<uint, float>();
            new TypedNullableConverterWrapper<uint, double>();

            new TypedNullableConverterWrapper<long, long>();
            new TypedNullableConverterWrapper<long, ulong>();
            new TypedNullableConverterWrapper<long, IntPtr>();
            new TypedNullableConverterWrapper<long, UIntPtr>();
            new TypedNullableConverterWrapper<long, float>();
            new TypedNullableConverterWrapper<long, double>();

            new TypedNullableConverterWrapper<ulong, ulong>();
            new TypedNullableConverterWrapper<ulong, IntPtr>();
            new TypedNullableConverterWrapper<ulong, UIntPtr>();
            new TypedNullableConverterWrapper<ulong, float>();
            new TypedNullableConverterWrapper<ulong, double>();

            new TypedNullableConverterWrapper<IntPtr, IntPtr>();
            new TypedNullableConverterWrapper<IntPtr, UIntPtr>();
            new TypedNullableConverterWrapper<IntPtr, float>();
            new TypedNullableConverterWrapper<IntPtr, double>();

            new TypedNullableConverterWrapper<UIntPtr, UIntPtr>();
            new TypedNullableConverterWrapper<UIntPtr, float>();
            new TypedNullableConverterWrapper<UIntPtr, double>();

            new TypedNullableConverterWrapper<float, float>();
            new TypedNullableConverterWrapper<float, double>();

            new TypedNullableConverterWrapper<double, double>();
        }
        public static readonly Dictionary<Pack<Type, Type>, TypedValueConverter> _TypedValueConverters = new Dictionary<Pack<Type, Type>, TypedValueConverter>()
        {
            { new Pack<Type, Type>(typeof(bool), typeof(bool)), new TypedValueConverter<bool, bool>(v => v) },
            { new Pack<Type, Type>(typeof(bool), typeof(byte)), new TypedValueConverter<bool, byte>(v => v ? (byte)1 : (byte)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(sbyte)), new TypedValueConverter<bool, sbyte>(v => v ? (sbyte)1 : (sbyte)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(short)), new TypedValueConverter<bool, short>(v => v ? (short)1 : (short)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(ushort)), new TypedValueConverter<bool, ushort>(v => v ? (ushort)1 : (ushort)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(int)), new TypedValueConverter<bool, int>(v => v ? 1 : 0) },
            { new Pack<Type, Type>(typeof(bool), typeof(uint)), new TypedValueConverter<bool, uint>(v => v ? 1U : 0U) },
            { new Pack<Type, Type>(typeof(bool), typeof(long)), new TypedValueConverter<bool, long>(v => v ? 1L : 0L) },
            { new Pack<Type, Type>(typeof(bool), typeof(ulong)), new TypedValueConverter<bool, ulong>(v => v ? 1UL : 0UL) },
            { new Pack<Type, Type>(typeof(bool), typeof(IntPtr)), new TypedValueConverter<bool, IntPtr>(v => v ? (IntPtr)1 : (IntPtr)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(UIntPtr)), new TypedValueConverter<bool, UIntPtr>(v => v ? (UIntPtr)1 : (UIntPtr)0) },
            { new Pack<Type, Type>(typeof(bool), typeof(float)), new TypedValueConverter<bool, float>(v => v ? 1f : 0f) },
            { new Pack<Type, Type>(typeof(bool), typeof(double)), new TypedValueConverter<bool, double>(v => v ? 1.0 : 0.0) },

            { new Pack<Type, Type>(typeof(byte), typeof(bool)), new TypedValueConverter<byte, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(byte), typeof(byte)), new TypedValueConverter<byte, byte>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(sbyte)), new TypedValueConverter<byte, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(byte), typeof(short)), new TypedValueConverter<byte, short>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(ushort)), new TypedValueConverter<byte, ushort>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(int)), new TypedValueConverter<byte, int>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(uint)), new TypedValueConverter<byte, uint>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(long)), new TypedValueConverter<byte, long>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(ulong)), new TypedValueConverter<byte, ulong>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(IntPtr)), new TypedValueConverter<byte, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(byte), typeof(UIntPtr)), new TypedValueConverter<byte, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(byte), typeof(float)), new TypedValueConverter<byte, float>(v => v) },
            { new Pack<Type, Type>(typeof(byte), typeof(double)), new TypedValueConverter<byte, double>(v => v) },

            { new Pack<Type, Type>(typeof(sbyte), typeof(bool)), new TypedValueConverter<sbyte, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(byte)), new TypedValueConverter<sbyte, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(sbyte)), new TypedValueConverter<sbyte, sbyte>(v => v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(short)), new TypedValueConverter<sbyte, short>(v => v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(ushort)), new TypedValueConverter<sbyte, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(int)), new TypedValueConverter<sbyte, int>(v => v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(uint)), new TypedValueConverter<sbyte, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(long)), new TypedValueConverter<sbyte, long>(v => v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(ulong)), new TypedValueConverter<sbyte, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(IntPtr)), new TypedValueConverter<sbyte, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(UIntPtr)), new TypedValueConverter<sbyte, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(float)), new TypedValueConverter<sbyte, float>(v => v) },
            { new Pack<Type, Type>(typeof(sbyte), typeof(double)), new TypedValueConverter<sbyte, double>(v => v) },

            { new Pack<Type, Type>(typeof(short), typeof(bool)), new TypedValueConverter<short, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(short), typeof(byte)), new TypedValueConverter<short, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(short), typeof(sbyte)), new TypedValueConverter<short, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(short), typeof(short)), new TypedValueConverter<short, short>(v => v) },
            { new Pack<Type, Type>(typeof(short), typeof(ushort)), new TypedValueConverter<short, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(short), typeof(int)), new TypedValueConverter<short, int>(v => v) },
            { new Pack<Type, Type>(typeof(short), typeof(uint)), new TypedValueConverter<short, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(short), typeof(long)), new TypedValueConverter<short, long>(v => v) },
            { new Pack<Type, Type>(typeof(short), typeof(ulong)), new TypedValueConverter<short, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(short), typeof(IntPtr)), new TypedValueConverter<short, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(short), typeof(UIntPtr)), new TypedValueConverter<short, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(short), typeof(float)), new TypedValueConverter<short, float>(v => v) },
            { new Pack<Type, Type>(typeof(short), typeof(double)), new TypedValueConverter<short, double>(v => v) },

            { new Pack<Type, Type>(typeof(ushort), typeof(bool)), new TypedValueConverter<ushort, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(ushort), typeof(byte)), new TypedValueConverter<ushort, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(sbyte)), new TypedValueConverter<ushort, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(short)), new TypedValueConverter<ushort, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(ushort)), new TypedValueConverter<ushort, ushort>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(int)), new TypedValueConverter<ushort, int>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(uint)), new TypedValueConverter<ushort, uint>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(long)), new TypedValueConverter<ushort, long>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(ulong)), new TypedValueConverter<ushort, ulong>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(IntPtr)), new TypedValueConverter<ushort, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(UIntPtr)), new TypedValueConverter<ushort, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(float)), new TypedValueConverter<ushort, float>(v => v) },
            { new Pack<Type, Type>(typeof(ushort), typeof(double)), new TypedValueConverter<ushort, double>(v => v) },

            { new Pack<Type, Type>(typeof(int), typeof(bool)), new TypedValueConverter<int, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(int), typeof(byte)), new TypedValueConverter<int, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(int), typeof(sbyte)), new TypedValueConverter<int, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(int), typeof(short)), new TypedValueConverter<int, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(int), typeof(ushort)), new TypedValueConverter<int, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(int), typeof(int)), new TypedValueConverter<int, int>(v => v) },
            { new Pack<Type, Type>(typeof(int), typeof(uint)), new TypedValueConverter<int, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(int), typeof(long)), new TypedValueConverter<int, long>(v => v) },
            { new Pack<Type, Type>(typeof(int), typeof(ulong)), new TypedValueConverter<int, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(int), typeof(IntPtr)), new TypedValueConverter<int, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(int), typeof(UIntPtr)), new TypedValueConverter<int, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(int), typeof(float)), new TypedValueConverter<int, float>(v => v) },
            { new Pack<Type, Type>(typeof(int), typeof(double)), new TypedValueConverter<int, double>(v => v) },

            { new Pack<Type, Type>(typeof(uint), typeof(bool)), new TypedValueConverter<uint, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(uint), typeof(byte)), new TypedValueConverter<uint, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(sbyte)), new TypedValueConverter<uint, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(short)), new TypedValueConverter<uint, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(ushort)), new TypedValueConverter<uint, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(int)), new TypedValueConverter<uint, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(uint)), new TypedValueConverter<uint, uint>(v => v) },
            { new Pack<Type, Type>(typeof(uint), typeof(long)), new TypedValueConverter<uint, long>(v => v) },
            { new Pack<Type, Type>(typeof(uint), typeof(ulong)), new TypedValueConverter<uint, ulong>(v => v) },
            { new Pack<Type, Type>(typeof(uint), typeof(IntPtr)), new TypedValueConverter<uint, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(UIntPtr)), new TypedValueConverter<uint, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(uint), typeof(float)), new TypedValueConverter<uint, float>(v => v) },
            { new Pack<Type, Type>(typeof(uint), typeof(double)), new TypedValueConverter<uint, double>(v => v) },

            { new Pack<Type, Type>(typeof(long), typeof(bool)), new TypedValueConverter<long, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(long), typeof(byte)), new TypedValueConverter<long, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(long), typeof(sbyte)), new TypedValueConverter<long, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(long), typeof(short)), new TypedValueConverter<long, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(long), typeof(ushort)), new TypedValueConverter<long, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(long), typeof(int)), new TypedValueConverter<long, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(long), typeof(uint)), new TypedValueConverter<long, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(long), typeof(long)), new TypedValueConverter<long, long>(v => v) },
            { new Pack<Type, Type>(typeof(long), typeof(ulong)), new TypedValueConverter<long, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(long), typeof(IntPtr)), new TypedValueConverter<long, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(long), typeof(UIntPtr)), new TypedValueConverter<long, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(long), typeof(float)), new TypedValueConverter<long, float>(v => v) },
            { new Pack<Type, Type>(typeof(long), typeof(double)), new TypedValueConverter<long, double>(v => v) },

            { new Pack<Type, Type>(typeof(ulong), typeof(bool)), new TypedValueConverter<ulong, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(ulong), typeof(byte)), new TypedValueConverter<ulong, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(sbyte)), new TypedValueConverter<ulong, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(short)), new TypedValueConverter<ulong, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(ushort)), new TypedValueConverter<ulong, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(int)), new TypedValueConverter<ulong, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(uint)), new TypedValueConverter<ulong, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(long)), new TypedValueConverter<ulong, long>(v => (long)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(ulong)), new TypedValueConverter<ulong, ulong>(v => v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(IntPtr)), new TypedValueConverter<ulong, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(UIntPtr)), new TypedValueConverter<ulong, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(float)), new TypedValueConverter<ulong, float>(v => v) },
            { new Pack<Type, Type>(typeof(ulong), typeof(double)), new TypedValueConverter<ulong, double>(v => v) },

            { new Pack<Type, Type>(typeof(IntPtr), typeof(bool)), new TypedValueConverter<IntPtr, bool>(v => v != IntPtr.Zero) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(byte)), new TypedValueConverter<IntPtr, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(sbyte)), new TypedValueConverter<IntPtr, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(short)), new TypedValueConverter<IntPtr, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(ushort)), new TypedValueConverter<IntPtr, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(int)), new TypedValueConverter<IntPtr, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(uint)), new TypedValueConverter<IntPtr, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(long)), new TypedValueConverter<IntPtr, long>(v => (long)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(ulong)), new TypedValueConverter<IntPtr, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(IntPtr)), new TypedValueConverter<IntPtr, IntPtr>(v => v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(UIntPtr)), new TypedValueConverter<IntPtr, UIntPtr>(v => (UIntPtr)(ulong)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(float)), new TypedValueConverter<IntPtr, float>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(IntPtr), typeof(double)), new TypedValueConverter<IntPtr, double>(v => (ulong)v) },

            { new Pack<Type, Type>(typeof(UIntPtr), typeof(bool)), new TypedValueConverter<UIntPtr, bool>(v => v != UIntPtr.Zero) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(byte)), new TypedValueConverter<UIntPtr, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(sbyte)), new TypedValueConverter<UIntPtr, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(short)), new TypedValueConverter<UIntPtr, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(ushort)), new TypedValueConverter<UIntPtr, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(int)), new TypedValueConverter<UIntPtr, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(uint)), new TypedValueConverter<UIntPtr, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(long)), new TypedValueConverter<UIntPtr, long>(v => (long)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(ulong)), new TypedValueConverter<UIntPtr, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(IntPtr)), new TypedValueConverter<UIntPtr, IntPtr>(v => (IntPtr)(ulong)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(UIntPtr)), new TypedValueConverter<UIntPtr, UIntPtr>(v => v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(float)), new TypedValueConverter<UIntPtr, float>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(UIntPtr), typeof(double)), new TypedValueConverter<UIntPtr, double>(v => (ulong)v) },

            { new Pack<Type, Type>(typeof(float), typeof(bool)), new TypedValueConverter<float, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(float), typeof(byte)), new TypedValueConverter<float, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(float), typeof(sbyte)), new TypedValueConverter<float, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(float), typeof(short)), new TypedValueConverter<float, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(float), typeof(ushort)), new TypedValueConverter<float, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(float), typeof(int)), new TypedValueConverter<float, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(float), typeof(uint)), new TypedValueConverter<float, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(float), typeof(long)), new TypedValueConverter<float, long>(v => (long)v) },
            { new Pack<Type, Type>(typeof(float), typeof(ulong)), new TypedValueConverter<float, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(float), typeof(IntPtr)), new TypedValueConverter<float, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(float), typeof(UIntPtr)), new TypedValueConverter<float, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(float), typeof(float)), new TypedValueConverter<float, float>(v => v) },
            { new Pack<Type, Type>(typeof(float), typeof(double)), new TypedValueConverter<float, double>(v => v) },

            { new Pack<Type, Type>(typeof(double), typeof(bool)), new TypedValueConverter<double, bool>(v => v != 0) },
            { new Pack<Type, Type>(typeof(double), typeof(byte)), new TypedValueConverter<double, byte>(v => (byte)v) },
            { new Pack<Type, Type>(typeof(double), typeof(sbyte)), new TypedValueConverter<double, sbyte>(v => (sbyte)v) },
            { new Pack<Type, Type>(typeof(double), typeof(short)), new TypedValueConverter<double, short>(v => (short)v) },
            { new Pack<Type, Type>(typeof(double), typeof(ushort)), new TypedValueConverter<double, ushort>(v => (ushort)v) },
            { new Pack<Type, Type>(typeof(double), typeof(int)), new TypedValueConverter<double, int>(v => (int)v) },
            { new Pack<Type, Type>(typeof(double), typeof(uint)), new TypedValueConverter<double, uint>(v => (uint)v) },
            { new Pack<Type, Type>(typeof(double), typeof(long)), new TypedValueConverter<double, long>(v => (long)v) },
            { new Pack<Type, Type>(typeof(double), typeof(ulong)), new TypedValueConverter<double, ulong>(v => (ulong)v) },
            { new Pack<Type, Type>(typeof(double), typeof(IntPtr)), new TypedValueConverter<double, IntPtr>(v => (IntPtr)v) },
            { new Pack<Type, Type>(typeof(double), typeof(UIntPtr)), new TypedValueConverter<double, UIntPtr>(v => (UIntPtr)v) },
            { new Pack<Type, Type>(typeof(double), typeof(float)), new TypedValueConverter<double, float>(v => (float)v) },
            { new Pack<Type, Type>(typeof(double), typeof(double)), new TypedValueConverter<double, double>(v => v) },
        };
        internal static class FakeConverter<T>
        {
            public readonly static Func<T, T> Converter = v => v;
        }
        public static T FakeConvert<F, T>(F from)
        {
            Func<F, F> nonconverter = FakeConverter<F>.Converter;
            Func<F, T> forciblyconverter = (Func<F, T>)(Delegate)nonconverter;
            return forciblyconverter(from);
        }
        public static bool Convert<F, T>(this F from, out T to)
        {
            var ftype = typeof(F);
            var ttype = typeof(T);
            if (ftype == ttype)
            {
                to = FakeConvert<F, T>(from);
                return true;
            }
            if (ttype.IsEnum())
            {
                if (ftype == typeof(string) || ftype == typeof(byte[]))
                {
                    string str;
                    if (ftype == typeof(byte[]))
                    {
                        str = System.Text.Encoding.UTF8.GetString((byte[])(object)from);
                    }
                    else
                    {
                        str = (string)(object)from;
                    }
                    return EnumUtils.TryConvertStrToEnum<T>(str, out to);
                }
                else
                {
                    ulong val;
                    if (Convert<F, ulong>(from, out val))
                    {
#if CONVERT_ENUM_SAFELY
                        to = (T)Enum.ToObject(typeof(T), val);
#else
                        to = EnumUtils.ConvertToEnumForcibly<T>(val);
#endif
                        return true;
                    }
                    else
                    {
                        to = default(T);
                        return false;
                    }
                }
            }
            else if (ftype.IsEnum())
            {
                if (ttype == typeof(string) || ttype == typeof(byte[]))
                {
                    string str = EnumUtils.ConvertEnumToStr(from);
                    if (ttype == typeof(string))
                    {
                        to = (T)(object)str;
                        return true;
                    }
                    else
                    {
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                        to = (T)(object)bytes;
                        return true;
                    }
                }
                else
                {
#if CONVERT_ENUM_SAFELY
                    ulong val = System.Convert.ToUInt64(from);
#else
                    ulong val = EnumUtils.ConvertFromEnumForcibly(from);
#endif
                    return Convert<ulong, T>(val, out to);
                }
            }
            else if (ftype.IsValueType && ttype.IsValueType)
            {
                TypedValueConverter converter;
                if (_TypedValueConverters.TryGetValue(new Pack<Type, Type>(ftype, ttype), out converter))
                {
                    var valueconverter = converter as TypedValueConverter<F, T>;
                    to = valueconverter.ConvertFunc(from);
                    return true;
                }
            }
            // the non-generic logic
            {
                var uuttype = Nullable.GetUnderlyingType(ttype);
                TypedConverter converter;
                if (_TypedConverters.TryGetValue(uuttype ?? ttype, out converter))
                {
                    ITypedConverter<T> tconverter = converter as ITypedConverter<T>;
                    if (tconverter != null)
                    {
                        to = tconverter.Convert(from);
                        return true;
                    }
                }
                if (from == null)
                {
                    to = default(T);
                    return false;
                }
                if (from is T)
                {
                    to = (T)(object)from;
                    return true;
                }
            }
            // Nullable?
            {
                var uuftype = Nullable.GetUnderlyingType(ftype);
                var uuttype = Nullable.GetUnderlyingType(ttype);
                if (uuftype != null || uuttype != null)
                {
                    to = Convert<T>(from);
                    return true;
                }
            }
            to = default(T);
            return false;
        }
        public static T Convert<F, T>(F from)
        {
            T rv;
            Convert<F, T>(from, out rv);
            return rv;
        }
    }

    public class CommonContainer<T>
    {
        public T Value;
        // TODO: implict and explicit operators.
    }
    public class CommonContainer
    {
        public object Value;
        // TODO: Generic get / set.
    }

    public struct InitializedInitializer
    {
        public InitializedInitializer(Action init)
        {
            init();
        }
    }

    public class SafeDict<TK, TV> : Dictionary<TK, TV>, IDictionary<TK, TV>, IDictionary
    {
        public new TV this[TK key]
        {
            get { TV v; TryGetValue(key, out v); return v; }
            set { base[key] = value; }
        }
        TV IDictionary<TK, TV>.this[TK key]
        {
            get { return this[key]; }
            set { this[key] = value; }
        }
        object IDictionary.this[object key]
        {
            get { return this[(TK)key]; }
            set { this[(TK)key] = (TV)key; }
        }
    }

    public class BiDict<TK, TV> // TODO: Add/Remove Methods
    {
        public readonly Dictionary<TK, TV> ForwardMap = new Dictionary<TK, TV>();
        public readonly Dictionary<TV, TK> BackwardMap = new Dictionary<TV, TK>();
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = true)]
    public class InheritablePreserveAttribute
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        : UnityEngine.Scripting.PreserveAttribute
#else
        : Attribute
#endif
    { }
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public class UnityPreserveAttribute
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        : UnityEngine.Scripting.PreserveAttribute
#else
        : Attribute
#endif
    { }
}
