using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngineEx
{
    public struct Crc
    {
        private uint _Init;
        private uint _Poly;
        private int _PolyLen;
        private bool _Reverse;
        private uint _ValueMask;

        private uint crc;
        public Crc(uint poly, uint init)
        {
            _ValueMask = 0;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = GetPolyLength(poly);
            crc = _Init;
        }
        public Crc(uint poly, uint init, int crclen)
        {
            _ValueMask = 0;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public Crc(uint poly, uint init, bool reverse)
        {
            _ValueMask = 0;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = GetPolyLength(poly);
            crc = _Init;
        }
        public Crc(uint poly, uint init, int crclen, bool reverse)
        {
            _ValueMask = 0;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public Crc(uint poly, uint init, uint valuemask)
        {
            _ValueMask = valuemask;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = GetPolyLength(poly);
            crc = _Init;
        }
        public Crc(uint poly, uint init, uint valuemask, int crclen)
        {
            _ValueMask = valuemask;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public Crc(uint poly, uint init, uint valuemask, bool reverse)
        {
            _ValueMask = valuemask;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = GetPolyLength(poly);
            crc = _Init;
        }
        public Crc(uint poly, uint init, uint valuemask, int crclen, bool reverse)
        {
            _ValueMask = valuemask;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }

        public void Reset()
        {
            crc = _Init;
        }
        public void Update(byte b)
        {
            if (_Reverse)
            {
                b = Reverse(b);
            }
            crc ^= ((uint)b) << (_PolyLen - 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & (1 << (_PolyLen - 1))) != 0)
                {
                    crc &= (~(1U << (_PolyLen - 1)));
                    crc <<= 1;
                    crc ^= _Poly;
                }
                else
                {
                    crc <<= 1;
                }
            }
        }
        public uint Value
        {
            get
            {
                if (_Reverse)
                {
                    return Reverse(crc) ^ _ValueMask;
                }
                else
                {
                    return crc ^ _ValueMask;
                }
            }
        }
        public uint Poly { get { return _Poly; } }
        public uint InitValue { get { return _Init; } }
        public int CrcLength { get { return _PolyLen; } }

        public static bool IsPOT(uint n)
        {
            return n != 0 && ((n & (n - 1)) == 0);
        }
        public static bool IsPOT(ulong n)
        {
            return n != 0 && ((n & (n - 1)) == 0);
        }
        public static uint GetNearestPOT(uint num)
        {
            num -= 1;
            for (int i = 1; i < 32; i <<= 1)
                num |= (num >> i);
            num += 1;

            return num;
        }
        public static ulong GetNearestPOT(ulong num)
        {
            num -= 1;
            for (int i = 1; i < 64; i <<= 1)
                num |= (num >> i);
            num += 1;

            return num;
        }

        private static int[] _TrueBitTable =
        {
            0,1,1,2,1,2,2,3,1,2,2,3,2,3,3,4,
            1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,5,
            1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,5,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,5,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            3,4,4,5,4,5,5,6,4,5,5,6,5,6,6,7,
            1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,5,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            3,4,4,5,4,5,5,6,4,5,5,6,5,6,6,7,
            2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,6,
            3,4,4,5,4,5,5,6,4,5,5,6,5,6,6,7,
            3,4,4,5,4,5,5,6,4,5,5,6,5,6,6,7,
            4,5,5,6,5,6,6,7,5,6,6,7,6,7,7,8
        };
        public static int TrueBitCount(uint num)
        {
            int cnt = 0;
            cnt += _TrueBitTable[num & 0xFF];
            cnt += _TrueBitTable[(num >> 8) & 0xFF];
            cnt += _TrueBitTable[(num >> 16) & 0xFF];
            cnt += _TrueBitTable[(num >> 24) & 0xFF];
            return cnt;
        }
        public static int TrueBitCount(ulong num)
        {
            int cnt = 0;
            cnt += _TrueBitTable[num & 0xFF];
            cnt += _TrueBitTable[(num >> 8) & 0xFF];
            cnt += _TrueBitTable[(num >> 16) & 0xFF];
            cnt += _TrueBitTable[(num >> 24) & 0xFF];
            cnt += _TrueBitTable[(num >> 32) & 0xFF];
            cnt += _TrueBitTable[(num >> 40) & 0xFF];
            cnt += _TrueBitTable[(num >> 48) & 0xFF];
            cnt += _TrueBitTable[(num >> 56) & 0xFF];
            return cnt;
        }

        public static int GetPolyLength(uint num)
        {
            return TrueBitCount(GetNearestPOT(num) - 1);
        }
        public static int GetPolyLength(ulong num)
        {
            return TrueBitCount(GetNearestPOT(num) - 1);
        }

        public static byte Reverse(byte x)
        {
            return (byte)(((x * 0x0802LU & 0x22110LU) | (x * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
        }
        public static ushort Reverse(ushort x)
        {
            return (ushort)((((ushort)Reverse((byte)(x))) << 8) | ((ushort)Reverse((byte)(x >> 8))));
        }
        public static uint Reverse(uint x)
        {
            x = ((x >> 1) & 0x55555555) | ((x << 1) & 0xaaaaaaaa);
            x = ((x >> 2) & 0x33333333) | ((x << 2) & 0xcccccccc);
            x = ((x >> 4) & 0x0f0f0f0f) | ((x << 4) & 0xf0f0f0f0);
            x = ((x >> 8) & 0x00ff00ff) | ((x << 8) & 0xff00ff00);
            x = ((x >> 16) & 0x0000ffff) | ((x << 16) & 0xffff0000);
            return x;
        }
        public static ulong Reverse(ulong x)
        {
            return (((ulong)Reverse((uint)(x))) << 32) | ((ulong)Reverse((uint)(x >> 32)));
        }

        private static readonly Crc _Crc24 = new Crc(0x864cfb, 0xb704ce);
        public static Crc Crc24 { get { return _Crc24; } }
        private static readonly Crc _Crc32 = new Crc(0x04C11DB7, 0xFFFFFFFFU, 0xFFFFFFFFU, 32, true);
        public static Crc Crc32 { get { return _Crc32; } }
    }
    public struct CrcLong
    {
        private ulong _Init;
        private ulong _Poly;
        private int _PolyLen;
        private bool _Reverse;
        private ulong _ValueMask;

        private ulong crc;
        public CrcLong(ulong poly, ulong init)
        {
            _ValueMask = 0;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = Crc.GetPolyLength(poly);
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, int crclen)
        {
            _ValueMask = 0;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, bool reverse)
        {
            _ValueMask = 0;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = Crc.GetPolyLength(poly);
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, int crclen, bool reverse)
        {
            _ValueMask = 0;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, ulong valuemask)
        {
            _ValueMask = valuemask;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = Crc.GetPolyLength(poly);
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, ulong valuemask, int crclen)
        {
            _ValueMask = valuemask;
            _Reverse = false;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, ulong valuemask, bool reverse)
        {
            _ValueMask = valuemask;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = Crc.GetPolyLength(poly);
            crc = _Init;
        }
        public CrcLong(ulong poly, ulong init, ulong valuemask, int crclen, bool reverse)
        {
            _ValueMask = valuemask;
            _Reverse = reverse;
            _Poly = poly;
            _Init = init;
            _PolyLen = crclen;
            crc = _Init;
        }

        public void Reset()
        {
            crc = _Init;
        }
        public void Update(byte b)
        {
            if (_Reverse)
            {
                b = Crc.Reverse(b);
            }
            crc ^= ((ulong)b) << (_PolyLen - 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & (1UL << (_PolyLen - 1))) != 0)
                {
                    crc &= (~(1UL << (_PolyLen - 1)));
                    crc <<= 1;
                    crc ^= _Poly;
                }
                else
                {
                    crc <<= 1;
                }
            }
        }
        public ulong Value
        {
            get
            {
                if (_Reverse)
                {
                    return Crc.Reverse(crc) ^ _ValueMask;
                }
                else
                {
                    return crc ^ _ValueMask;
                }
            }
        }
        public ulong Poly { get { return _Poly; } }
        public ulong InitValue { get { return _Init; } }
        public int CrcLength { get { return _PolyLen; } }
    }

    public static class ExtendedStringHash
    {
        public static uint GetCrc24(this string str)
        {
            var crc24 = Crc.Crc24;
            foreach (var ch in str)
            {
                crc24.Update((byte)ch);
            }
            return crc24.Value;
        }
        public static uint GetCrc32(this string str)
        {
            if (str == null)
            {
                return 0;
            }
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            return (uint)UnityEngine.Animator.StringToHash(str);
#else
            var crc32 = Crc.Crc32;
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < bytes.Length; ++i)
            {
                crc32.Update(bytes[i]);
            }
            return crc32.Value;
#endif
        }

        public static long GetHashCodeEx(this string str, ushort headOffset, ushort tailOffset, int criticalPos, byte exflag)
        {
            long hash = 0;
            int len = str.Length;

            hash = GetCrc24(str);

            if (len > 127)
            {
                hash |= (1U << 31);
            }
            hash |= ((uint)(len & 0x7F) << 24);

            int headpos = -1, midpos = -1, tailpos = -1;
            if (len - headOffset > 0)
            {
                var c0 = str[headpos = headOffset];
                byte b = (byte)c0;
                b &= 0x3F;
                hash |= (((long)b) << 34);
            }
            var reallen = len - headOffset - tailOffset;
            if (reallen > 2)
            {
                {
                    var ce = str[tailpos = len - 1 - tailOffset];
                    byte b = (byte)ce;
                    b &= 0x3F;
                    hash |= (((long)b) << 46);
                }
                {
                    var cm = str[midpos = headOffset + reallen / 2];
                    byte b = (byte)cm;
                    b &= 0x3F;
                    hash |= (((long)b) << 40);
                }
            }
            else if (reallen == 2)
            {
                var c1 = str[midpos = headOffset + 1];
                byte b = (byte)c1;
                b &= 0x3F;
                hash |= (((long)b) << 40);
            }

            if (criticalPos >= 0 && criticalPos < len && criticalPos != headpos && criticalPos != midpos && criticalPos != tailpos)
            {
                var cc = str[criticalPos];
                byte b = (byte)cc;
                b &= 0x3F;
                hash ^= (((long)b) << 37);
            }

            { // TODO: clamp exflag to 6-bits
                hash ^= (((long)exflag) << 43);
            }

            return hash;
        }
        public static long GetHashCodeEx(this string str)
        {
            return GetHashCodeEx(str, 0, 0, -1, 0);
        }
        public static int GetHashCodeExShort(long hash)
        {
            int hash_short = (int)((hash & 0xFFFFFFFFL) ^ (long)Crc.Reverse(((ulong)hash) & (0xFFFFFFFFUL << 32)));
            return hash_short;
        }
    }
}

#if UNITY_EDITOR
#if UNITY_INCLUDE_TESTS
#region TESTS
namespace Mods.Test
{
    using UnityEngineEx;

    public static class TestExtendedHash
    {
        [UnityEditor.MenuItem("Test/Hash/Check Hash Conflict in MemberList", priority = 500010)]
        public static void CheckHashConflictInMemberList()
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            Dictionary<int, string> map_short = new Dictionary<int, string>();
            var memberlist = PlatDependant.ReadAllLines("EditorOutput/LuaPrecompile/MemberList.txt");
            foreach (var str in memberlist)
            {
                if (str.StartsWith("member "))
                {
                    var parts = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 4)
                    {
                        var real = parts[3] + " " + parts[4];
                        var hash = real.GetHashCodeEx(0, 1, parts[3].Length + 1, 0);
                        int hash_short = ExtendedStringHash.GetHashCodeExShort(hash);
                        if (map.ContainsKey(hash) && map[hash] != real)
                        {
                            UnityEngine.Debug.LogError($"Duplicated Hash: {hash:X}, {real}, {map[hash]}");
                        }
                        else
                        {
                            map[hash] = real;
                        }
                        if (map_short.ContainsKey(hash_short) && map_short[hash_short] != real)
                        {
                            UnityEngine.Debug.LogError($"Duplicated Short Hash: {hash:X}, {real}, {map_short[hash_short]}");
                        }
                        else
                        {
                            map_short[hash_short] = real;
                        }
                    }
                }
            }
        }

        [UnityEditor.MenuItem("Test/Hash/Test Crc32", priority = 500020)]
        public static void TestCrc32()
        {
            var str = "";
            var crc32 = Crc.Crc32;
            foreach (var ch in str)
            {
                crc32.Update((byte)ch);
            }
            UnityEngine.Debug.Log(str + " Crc32: " + crc32.Value.ToString("x"));
            UnityEngine.Debug.Log(str + " Crc32(Unity): " + UnityEngine.Animator.StringToHash(str).ToString("x"));

            str = "a";
            crc32.Reset();
            foreach (var ch in str)
            {
                crc32.Update((byte)ch);
            }
            UnityEngine.Debug.Log(str + " Crc32: " + crc32.Value.ToString("x"));
            UnityEngine.Debug.Log(str + " Crc32(Unity): " + UnityEngine.Animator.StringToHash(str).ToString("x"));

            str = "let's compute crc of this";
            crc32.Reset();
            foreach (var ch in str)
            {
                crc32.Update((byte)ch);
            }
            UnityEngine.Debug.Log(str + " Crc32: " + crc32.Value.ToString("x"));
            UnityEngine.Debug.Log(str + " Crc32(Unity): " + UnityEngine.Animator.StringToHash(str).ToString("x"));
        }
    }
}
#endregion
#endif
#endif
