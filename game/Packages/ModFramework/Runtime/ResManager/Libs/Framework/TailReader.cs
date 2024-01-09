using System;
using System.IO;
using System.Text;

namespace UnityEngineEx
{
    public class TailReaderUTF8 : IDisposable
    {
        protected Stream _Stream;
        protected long _Length;
        protected long _Pos = 0;
        protected bool _LeaveOpen;
        protected bool _Disposed;

        public Stream UnderlayStream => _Stream;
        public long Length => _Length;

        public TailReaderUTF8(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            else if (!stream.CanSeek)
            {
                throw new NotSupportedException($"{nameof(stream)}.{nameof(stream.CanSeek)} must be true.");
            }
            _Stream = stream;
            _Length = stream.Length;
            _LeaveOpen = true;
        }
        public TailReaderUTF8(string path)
            : this(PlatDependant.OpenRead(path))
        {
            _LeaveOpen = false;
        }

        public void Dispose()
        {
            if (!_Disposed)
            {
                _Disposed = true;
                if (!_LeaveOpen)
                {
                    _Stream.Dispose();
                }
            }
        }

        public (int ch, int bytecnt) Peek()
        {
            if (_Pos >= _Length)
            {
                return (-1, 0);
            }
            int ch = 0;
            int offset;
            for (offset = 0; ; ++offset)
            {
                if (_Pos + offset >= _Length)
                {
                    break;
                }
                _Stream.Seek(_Length - 1 - _Pos - offset, SeekOrigin.Begin);
                var b = _Stream.ReadByte();
                if (b < 0x80) // 0xxxxxxx
                {
                    ch = b;
                    break;
                }
                else if (b >= 0xc0) // 11xxxxxx (110xxxxx / 1110xxxx / 11110xxx)
                {
                    var bytecnt = offset + 1;
                    ch += (((b << bytecnt) & 0xFF) >> bytecnt) << (offset * 6);
                    break;
                }
                else // 10xxxxxx
                {
                    ch += (b & 0x3f) << (offset * 6);
                }
            }

            return (ch, offset + 1);
        }

        public static (int ch, int chh) UnicodeEncode(int uch)
        {
            if (uch <= 0xFFFF)
            {
                return (uch, -1);
            }
            else
            {
                uch -= 0x10000;
                var low = uch & 0x3FF;
                var high = ((uch & (0x3FF << 10)) >> 10) & 0x3FF;

                return (0xDC00 | low, 0xD800 | high);
            }
        }

        public long FakeReadLines(int linecnt)
        {
            var oldPos = _Pos;
            int readlinecnt = 0;
            while (true)
            {
                if (_Pos >= _Length)
                {
                    break;
                }
                if (readlinecnt >= linecnt)
                {
                    break;
                }
                var peeked = Peek();
                _Pos += peeked.bytecnt;
                if (peeked.ch == '\n')
                {
                    ++readlinecnt;
                    if (_Pos < _Length)
                    {
                        var peeked2 = Peek();
                        if (peeked2.ch == '\r')
                        {
                            _Pos += peeked2.bytecnt;
                        }
                    }
                }
            }
            return _Pos - oldPos;
        }

        public long FakeReadUChars(int cnt)
        {
            var oldPos = _Pos;
            int readcnt = 0;
            while (true)
            {
                if (_Pos >= _Length)
                {
                    break;
                }
                if (readcnt >= cnt)
                {
                    break;
                }
                var peeked = Peek();
                _Pos += peeked.bytecnt;
                ++readcnt;
            }
            return _Pos - oldPos;
        }

        public long FakeReadChars(int maxcnt)
        {
            var oldPos = _Pos;
            int readcnt = 0;
            while (true)
            {
                if (_Pos >= _Length)
                {
                    break;
                }
                if (readcnt >= maxcnt)
                {
                    break;
                }
                var peeked = Peek();
                int ccnt = 1;
                var utf16chars = UnicodeEncode(peeked.ch);
                if (utf16chars.chh > 0)
                {
                    ccnt = 2;
                }
                if (readcnt + ccnt > maxcnt)
                {
                    break;
                }
                else
                {
                    _Pos += peeked.bytecnt;
                    readcnt += ccnt;
                }
            }
            return _Pos - oldPos;
        }

        public long FakeReadBytes(int maxcnt)
        {
            var oldPos = _Pos;
            _Pos += maxcnt;
            if (_Pos > _Length)
            {
                _Pos = _Length;
            }
            _Stream.Seek(_Length - _Pos, SeekOrigin.Begin);
            while (true)
            {
                var b = _Stream.ReadByte();
                if (b < 0x80) // 0xxxxxxx
                {
                    break;
                }
                else if (b >= 0xc0) // 11xxxxxx (110xxxxx / 1110xxxx / 11110xxx)
                {
                    break;
                }
                else if (_Pos <= oldPos)
                {
                    break;
                }
                --_Pos;
            }
            return _Pos - oldPos;
        }

        public void CopyToStream(Stream stream, long cnt)
        {
            if (cnt < 0)
            {
                cnt = _Pos;
            }
            else if (cnt > _Pos)
            {
                cnt = _Pos;
            }
            var sstream = new SpanStream(_Stream, _Length - _Pos, cnt);
            sstream.CopyTo(stream);
        }
        public void CopyToStream(Stream stream)
        {
            CopyToStream(stream, -1);
        }

        public void AppendToFile(string path, long cnt)
        {
            using (var stream = PlatDependant.OpenAppend(path))
            {
                CopyToStream(stream, cnt);
            }
        }
        public void AppendToFile(string path)
        {
            AppendToFile(path, -1);
        }
        public void CopyToFile(string path, long cnt)
        {
            PlatDependant.DeleteFile(path);
            AppendToFile(path, cnt);
        }
        public void CopyToFile(string path)
        {
            CopyToFile(path, -1);
        }
    }
}