using System;
using System.IO;
using System.Threading.Tasks;

namespace UnityEngineEx
{
    public class SpanStream : Stream
    {
        protected Stream Underlay;
        protected long HeadPos;
        protected long SpanLength;
        protected bool LeaveOpen;

        public SpanStream(Stream underlay, long start, long length, bool leaveOpen)
        {
            Underlay = underlay;
            HeadPos = start;
            SpanLength = length;
            LeaveOpen = leaveOpen;
            
            if (underlay != null && underlay.CanSeek)
            {
                underlay.Seek(start, SeekOrigin.Begin);
            }
        }
        public SpanStream(Stream underlay, long start, long length) : this(underlay, start, length, false) { }

        public override bool CanRead
        {
            get
            {
                if (Underlay != null)
                {
                    return Underlay.CanRead;
                }
                return false;
            }
        }
        public override bool CanSeek
        {
            get
            {
                if (Underlay != null)
                {
                    return Underlay.CanSeek;
                }
                return false;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return false; // TODO: Enable write?
            }
        }
        public override long Length
        {
            get
            {
                return SpanLength;
            }
        }
        public override long Position
        {
            get
            {
                if (Underlay != null)
                {
                    return Underlay.Position - HeadPos;
                }
                return 0;
            }
            set
            {
                if (Underlay != null)
                {
                    Underlay.Position = value + HeadPos;
                }
            }
        }

        public override void Flush()
        {
            // TODO: Enable write?
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Underlay != null)
            {
                return Underlay.Read(buffer, offset, count);
            }
            return 0;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (Underlay != null)
            {
                if (origin == SeekOrigin.Begin)
                {
                    return Underlay.Seek(offset + HeadPos, SeekOrigin.Begin);
                }
                else if (origin == SeekOrigin.Current)
                {
                    return Underlay.Seek(offset, SeekOrigin.Current);
                }
                else if (origin == SeekOrigin.End)
                {
                    return Underlay.Seek(HeadPos + SpanLength + offset, SeekOrigin.Begin);
                }
            }
            return 0;
        }
        public override void SetLength(long value)
        {
            // TODO: Enable write?
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            // TODO: Enable write?
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (LeaveOpen)
            {
                Underlay = null;
            }
            else
            {
                if (Underlay != null)
                {
                    Underlay.Dispose();
                    Underlay = null;
                }
            }
        }
#if UNITY_2020_2_OR_NEWER || NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1 || NETSTANDARD2_1_OR_GREATER
        public override async ValueTask DisposeAsync()
        {
            if (LeaveOpen)
            {
                Underlay = null;
            }
            await base.DisposeAsync();
            if (!LeaveOpen)
            {
                if (Underlay != null)
                {
                    var underlay = Underlay;
                    Underlay = null;
                    await underlay.DisposeAsync();
                }
            }
        }
#endif
    }
}