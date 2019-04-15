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
        private Bitmap prev;
        private Bitmap cur;
        private byte[] compressionBuffer;

        private byte[] backbuf;

        private int n = 0;

        public CompressScreenCapture()
        {
            screenBounds = Screen.PrimaryScreen.Bounds;

            prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(prev))
                g.Clear(Color.Black);

            compressionBuffer = new byte[screenBounds.Width * screenBounds.Height * 4];
        }

        private void Capture()
        {
            using (var gfxScreenshot = Graphics.FromImage(cur))
                gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
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
                for(int y = 0; y < height; y++)
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

            //var lockedCur = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //var lockedPrev = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);


                Difference(prev, cur, false);

                TimeSpan timeToXor = sw.Elapsed;

                int length = Compress();

                TimeSpan timeToCompress = sw.Elapsed;

                if ((n++) % 50 == 0)
                    Console.Write("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s {3} Kb => {4:0.0} FPS     \r", timeToCapture.TotalSeconds, timeToXor.TotalSeconds, timeToCompress.TotalSeconds, length / 1024, 1.0 / sw.Elapsed.TotalSeconds);

                var tmp = cur;
                cur = prev;
                prev = tmp;

            prev.Save("sadas.bmp");
            return backbuf;
        }

    }
}
