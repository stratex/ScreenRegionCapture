using LZ4;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenRegionCaptureGUI.Classes
{
    internal class ScreenRegionCapture
    {
        private static Bitmap DifferenceToBitmap(Bitmap bmp0, Bitmap bmp1, bool restore)
        {
            int Bpp = 4;
            var bmpData0 = bmp0.LockBits(
                            new Rectangle(0, 0, bmp0.Width, bmp0.Height),
                            ImageLockMode.ReadWrite, bmp0.PixelFormat);
            var bmpData1 = bmp1.LockBits(
                            new Rectangle(0, 0, bmp1.Width, bmp1.Height),
                            ImageLockMode.ReadOnly, bmp1.PixelFormat);

            int len = bmp0.Height * bmpData0.Stride;
            byte[] data0 = new byte[len];
            byte[] data1 = new byte[len];
            Marshal.Copy(bmpData0.Scan0, data0, 0, len);
            Marshal.Copy(bmpData1.Scan0, data1, 0, len);

            for (int i = 0; i < len; i += Bpp)
            {
                if (restore)
                {
                    bool toberestored = (data1[i] != 2 && data1[i + 1] != 3 &&
                                         data1[i + 2] != 7 && data1[i + 2] != 42);
                    if (toberestored)
                    {
                        data0[i] = data1[i];    // Blue
                        data0[i + 1] = data1[i + 1];  // Green 
                        data0[i + 2] = data1[i + 2];  // Red
                        data0[i + 3] = data1[i + 3];  // Alpha
                    }
                }
                else
                {
                    bool changed = ((data0[i] != data1[i]) ||
                                    (data0[i + 1] != data1[i + 1]) || (data0[i + 2] != data1[i + 2]));
                    data0[i] = changed ? data1[i] : (byte)2;   // special markers
                    data0[i + 1] = changed ? data1[i + 1] : (byte)3;   // special markers
                    data0[i + 2] = changed ? data1[i + 2] : (byte)7;   // special markers
                    data0[i + 3] = changed ? (byte)255 : (byte)42;  // special markers
                }
            }

            Marshal.Copy(data0, 0, bmpData0.Scan0, len);
            bmp0.UnlockBits(bmpData0);
            bmp1.UnlockBits(bmpData1);

            return bmp0;
        }

        private static byte[] DifferenceToBytes(Bitmap bmp0, Bitmap bmp1, bool restore)
        {
            int Bpp = 4;
            var bmpData0 = bmp0.LockBits(
                            new Rectangle(0, 0, bmp0.Width, bmp0.Height),
                            ImageLockMode.ReadWrite, bmp0.PixelFormat);
            var bmpData1 = bmp1.LockBits(
                            new Rectangle(0, 0, bmp1.Width, bmp1.Height),
                            ImageLockMode.ReadOnly, bmp1.PixelFormat);

            int len = bmp0.Height * bmpData0.Stride;
            byte[] data0 = new byte[len];
            byte[] data1 = new byte[len];
            Marshal.Copy(bmpData0.Scan0, data0, 0, len);
            Marshal.Copy(bmpData1.Scan0, data1, 0, len);

            for (int i = 0; i < len; i += Bpp)
            {
                bool changed = ((data0[i] != data1[i]) ||
                                (data0[i + 1] != data1[i + 1]) || (data0[i + 2] != data1[i + 2]));
                data0[i] = changed ? data1[i] : (byte)2;   // special markers
                data0[i + 1] = changed ? data1[i + 1] : (byte)3;   // special markers
                data0[i + 2] = changed ? data1[i + 2] : (byte)7;   // special markers
                data0[i + 3] = changed ? (byte)255 : (byte)42;  // special markers
            }

            bmp0.UnlockBits(bmpData0);
            bmp1.UnlockBits(bmpData1);

            return data0;
        }
        private static unsafe byte[] ApplyXorToBytes(Bitmap prev, Bitmap cur)
        {
            var prevData = prev.LockBits(
                new Rectangle(0, 0, prev.Width, prev.Height),
                ImageLockMode.ReadOnly, prev.PixelFormat);
            var curData = cur.LockBits(
                new Rectangle(0, 0, cur.Width, cur.Height),
                ImageLockMode.ReadOnly, cur.PixelFormat);

            byte* prev0 = (byte*)prevData.Scan0.ToPointer();
            byte* cur0 = (byte*)curData.Scan0.ToPointer();
            byte[] buffer = new byte[prevData.Height * prevData.Stride];

            int height = prev.Height;
            int width = prev.Width;
            int halfwidth = width / 2;

            fixed (byte* target = buffer)
            {
                ulong* dst = (ulong*)target;
                for (int y = 0; y < height; y++)
                {
                    ulong* prevRow = (ulong*)(prev0 + prevData.Stride * y);
                    ulong* curRow = (ulong*)(cur0 + curData.Stride * y);
                    for (int x = 0; x < halfwidth; x++)
                        *dst++ = prevRow[x] ^ curRow[x];
                }
            }

            //Marshal.Copy(buffer, 0, prevData.Scan0, curData.Height * curData.Stride);
            prev.UnlockBits(prevData);
            cur.UnlockBits(curData);

            return buffer;
        }

        private static unsafe Bitmap ApplyXorToBitmap(Bitmap prev, Bitmap cur)
        {
            var prevData = prev.LockBits(
                new Rectangle(0, 0, prev.Width, prev.Height),
                ImageLockMode.ReadOnly, prev.PixelFormat);
            var curData = cur.LockBits(
                new Rectangle(0, 0, cur.Width, cur.Height),
                ImageLockMode.ReadOnly, cur.PixelFormat);

            byte* prev0 = (byte*)prevData.Scan0.ToPointer();
            byte* cur0 = (byte*)curData.Scan0.ToPointer();
            byte[] buffer = new byte[prevData.Height * prevData.Stride];

            int height = prev.Height;
            int width = prev.Width;
            int halfwidth = width / 2;

            fixed (byte* target = buffer)
            {
                ulong* dst = (ulong*)target;
                for (int y = 0; y < height; y++)
                {
                    ulong* prevRow = (ulong*)(prev0 + prevData.Stride * y);
                    ulong* curRow = (ulong*)(cur0 + curData.Stride * y);
                    for (int x = 0; x < halfwidth; x++)
                        *dst++ = prevRow[x] ^ curRow[x];
                }
            }

            Marshal.Copy(buffer, 0, prevData.Scan0, curData.Height * curData.Stride);
            prev.UnlockBits(prevData);
            cur.UnlockBits(curData);

            return prev;
        }

        public class CompressScreen
        {
            private Rectangle screenBounds;
            private Rectangle imageRes;
            private Bitmap prev;
            private Bitmap cur;
            //private Bitmap ss;
            private byte[] compressionBuffer;

            private byte[] backbuf;

            private int n = 0;

            public CompressScreen(Rectangle Size)
            {
                screenBounds = Screen.PrimaryScreen.Bounds;
                imageRes = Size;

                //prev = new Bitmap(imageRes.Width, imageRes.Height, PixelFormat.Format32bppArgb);
                //cur = new Bitmap(imageRes.Width, imageRes.Height, PixelFormat.Format32bppArgb);
                prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                //ss = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(prev))
                    g.Clear(Color.Black);

                compressionBuffer = new byte[imageRes.Width * imageRes.Height * 4];
            }

            private void Capture()
            {
                using (var gfxScreenshot = Graphics.FromImage(cur))
                    gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
                //using (var g = Graphics.FromImage(cur))
                    //g.DrawImage(ss, 0, 0, imageRes.Width, imageRes.Height);
            }

            private int Compress()
            {
                backbuf = LZ4Codec.Wrap(compressionBuffer);
                return backbuf.Length;
            }

            public byte[] Iterate()
            {
                Stopwatch sw = Stopwatch.StartNew();
                Capture();
                TimeSpan timeToCapture = sw.Elapsed;
                //compressionBuffer = DifferenceToBytes(prev, cur, false);
                compressionBuffer = ApplyXorToBytes(prev, cur);
                TimeSpan timeToXor = sw.Elapsed;
                int length = Compress();
                TimeSpan timeToCompress = sw.Elapsed;

                if ((n++) % 10 == 0)
                    Console.Write("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s {3} Kb => {4:0.0} FPS     \r", timeToCapture.TotalSeconds, timeToXor.TotalSeconds, timeToCompress.TotalSeconds, length / 1024, 1.0 / sw.Elapsed.TotalSeconds);

                var tmp = cur;
                cur = prev;
                prev = tmp;

                return backbuf;
            }
        }

        public class DecompressScreen
        {
            private Rectangle screenBounds;
            private Bitmap prev;
            private Bitmap cur;
            private byte[] decompressionBuffer;

            public DecompressScreen(Rectangle Size)
            {
                //screenBounds = Size;
                screenBounds = Screen.PrimaryScreen.Bounds;

                prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(prev))
                    g.Clear(Color.Black);
            }

            private int Decompress(byte[] data)
            {
                decompressionBuffer = LZ4Codec.Unwrap(data);
                return decompressionBuffer.Length;
            }
            
            public Image Iterate(byte[] next)
            {
                Decompress(next);
                cur = (Bitmap)ImageExtensions.ImageFromRawBgraArray(decompressionBuffer, screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                //cur = DifferenceToBitmap(prev, cur, true);
                cur = ApplyXorToBitmap(prev, cur);

                var tmp = cur;
                cur = prev;
                prev = tmp;

                return prev;
            }
        }
    }

    public static class ImageExtensions
    {
        public static Image ImageFromRawBgraArray(this byte[] arr, int width, int height, PixelFormat pixelFormat)
        {
            var output = new Bitmap(width, height, pixelFormat);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect, ImageLockMode.ReadWrite, output.PixelFormat);

            var arrRowLength = width * Image.GetPixelFormatSize(output.PixelFormat) / 8;
            var ptr = bmpData.Scan0;
            for (var i = 0; i < height; i++)
            {
                Marshal.Copy(arr, i * arrRowLength, ptr, arrRowLength);
                ptr += bmpData.Stride;
            }

            output.UnlockBits(bmpData);
            return output;
        }
    }
}
