using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace BitmapToIcon
{
    class Program
    {
        public static bool IsPOT(uint n)
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
        public static Bitmap Resize(Bitmap origin, int width, int height)
        {
            var dest = new Bitmap(width, height);
            var g = Graphics.FromImage(dest);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(origin, 0, 0, width, height);
            g.Dispose();
            return dest;
        }
        public static List<Bitmap> GenerateMipmaps(string file)
        {
            List<Bitmap> bitmaps = new List<Bitmap>();
            try
            {
                Bitmap origin = (Bitmap)Image.FromFile(file);
                var size = Math.Max(origin.Width, origin.Height);
                size = (int)GetNearestPOT((uint)size);
                size = Math.Min(size, 256);
                var bitmap = Resize(origin, size, size);
                origin.Dispose();
                var memstream = new MemoryStream();
                bitmap.Save(memstream, System.Drawing.Imaging.ImageFormat.Png);
                bitmap.Dispose();
                memstream.Seek(0, SeekOrigin.Begin);
                bitmap = (Bitmap)Image.FromStream(memstream);
                bitmaps.Add(bitmap);

                while (bitmap.Width >= 32)
                {
                    bitmap = Resize(bitmap, bitmap.Width / 2, bitmap.Height / 2);
                    memstream.Seek(0, SeekOrigin.Begin);
                    memstream.SetLength(0);
                    bitmap.Save(memstream, System.Drawing.Imaging.ImageFormat.Png);
                    bitmap.Dispose();
                    memstream.Seek(0, SeekOrigin.Begin);
                    bitmap = (Bitmap)Image.FromStream(memstream);
                    bitmaps.Add(bitmap);
                }
                memstream.Dispose();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                for (int i = 0; i < bitmaps.Count; ++i)
                {
                    var bitmap = bitmaps[i];
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
            return bitmaps;
        }
        public static List<Bitmap> LoadIconImages(IEnumerable<string> files)
        {
            List<Bitmap> bitmaps = new List<Bitmap>();
            try
            {
                using (var memstream = new MemoryStream())
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            Bitmap origin = (Bitmap)Image.FromFile(file);
                            var size = Math.Max(origin.Width, origin.Height);
                            size = (int)GetNearestPOT((uint)size);
                            size = Math.Min(size, 256);
                            var bitmap = Resize(origin, size, size);
                            origin.Dispose();
                            memstream.Seek(0, SeekOrigin.Begin);
                            memstream.SetLength(0);
                            bitmap.Save(memstream, System.Drawing.Imaging.ImageFormat.Png);
                            bitmap.Dispose();
                            memstream.Seek(0, SeekOrigin.Begin);
                            bitmap = (Bitmap)Image.FromStream(memstream);
                            bitmaps.Add(bitmap);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                for (int i = 0; i < bitmaps.Count; ++i)
                {
                    var bitmap = bitmaps[i];
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
            return bitmaps;
        }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var src = args[0];
                var dir = Path.GetDirectoryName(src);
                var fileraw = Path.GetFileNameWithoutExtension(src);
                var tar = Path.Combine(dir, fileraw + ".ico");

                List<Bitmap> bitmaps;
                if (args.Length == 1)
                {
                    bitmaps = GenerateMipmaps(src);
                }
                else
                {
                    bitmaps = LoadIconImages(args);
                }
                try
                {
                    using (var stream = File.OpenWrite(tar))
                    {
                        IconFactory.SavePngsAsIcon(bitmaps, stream);
                    }
                }
                finally
                {
                    for (int i = 0; i < bitmaps.Count; ++i)
                    {
                        var bitmap = bitmaps[i];
                        if (bitmap != null)
                        {
                            bitmap.Dispose();
                        }
                    }
                }
            }
        }
    }
}
