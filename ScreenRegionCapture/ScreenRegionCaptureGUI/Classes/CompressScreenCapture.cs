using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LZ4;

namespace ScreenRegionCaptureGUI.Classes
{
    class CompressScreenCapture
    {
        private Rectangle screenBounds;
        private Rectangle imageRes;
        private Bitmap prev;
        private Bitmap cur;
        private Bitmap ss;
        private byte[] compressionBuffer;

        private byte[] backbuf;

        private int n = 0;

        public CompressScreenCapture(Rectangle Size)
        {
            screenBounds = Screen.PrimaryScreen.Bounds;
            imageRes = Size;

            prev = new Bitmap(imageRes.Width, imageRes.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(imageRes.Width, imageRes.Height, PixelFormat.Format32bppArgb);
            ss = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(prev))
                g.Clear(Color.Black);

            compressionBuffer = new byte[imageRes.Width * imageRes.Height * 4];
        }

        private void Capture()
        {
            using (var gfxScreenshot = Graphics.FromImage(ss))
                gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
            using (var g = Graphics.FromImage(cur))
                g.DrawImage(ss, 0, 0, imageRes.Width, imageRes.Height);
        }

        private unsafe void ApplyXor(BitmapData previous, BitmapData current)
        {
            byte* prev0 = (byte*)previous.Scan0.ToPointer();
            byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = previous.Height;
            int width = previous.Width;
            int halfwidth = width / 2;

            fixed (byte* target = compressionBuffer)
            {
                ulong* dst = (ulong*)target;
                for (int y = 0; y < height; y++)
                {
                    ulong* prevRow = (ulong*)(prev0 + previous.Stride * y);
                    ulong* curRow = (ulong*)(cur0 + current.Stride * y);
                    for (int x = 0; x < halfwidth; x++)
                        *dst++ = curRow[x] ^ prevRow[x];
                }
            }
        }

        private void Difference(Bitmap bmp0, Bitmap bmp1, bool restore)
        {
            int Bpp = 4;  // assuming an effective pixelformat of 32bpp
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

            //Marshal.Copy(data0, 0, bmpData0.Scan0, len);

            bmp0.UnlockBits(bmpData0);
            bmp1.UnlockBits(bmpData1);

            compressionBuffer = data0;
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

            Difference(prev, cur, false);

            TimeSpan timeToXor = sw.Elapsed;

            int length = Compress();

            TimeSpan timeToCompress = sw.Elapsed;

            if ((n++) % 10 == 0)
                Console.Write("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s {3} Kb => {4:0.0} FPS     \r", timeToCapture.TotalSeconds, timeToXor.TotalSeconds, timeToCompress.TotalSeconds, length / 1024, 1.0 / sw.Elapsed.TotalSeconds);

            var tmp = cur;
            cur = prev;
            prev = tmp;

            prev.Save("sadas.bmp");
            return backbuf;
        }

    }
}