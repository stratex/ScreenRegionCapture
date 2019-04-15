using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

        private int backbufSize;
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

            //backbufSize = LZ4Codec.MaximumOutputLength(compressionBuffer.Length);
            //backbuf = new CompressedCaptureScreen(backbufSize);
        }

        private void Capture()
        {
            using (var gfxScreenshot = Graphics.FromImage(cur))
                gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
            //cur.Save("test.bmp");
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

            var lockedCur = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var lockedPrev = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                ApplyXor(lockedPrev, lockedCur);

                TimeSpan timeToXor = sw.Elapsed;

                int length = Compress();

                TimeSpan timeToCompress = sw.Elapsed;

                if ((n++) % 50 == 0)
                    Console.Write("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s {3} Kb => {4:0.0} FPS     \r", timeToCapture.TotalSeconds, timeToXor.TotalSeconds, timeToCompress.TotalSeconds, length / 1024, 1.0 / sw.Elapsed.TotalSeconds);

                var tmp = cur;
                cur = prev;
                prev = tmp;
            }
            finally
            {
                cur.UnlockBits(lockedCur);
                prev.UnlockBits(lockedPrev);
            }
            prev.Save("sadas.bmp");
            return backbuf;
        }

    }
}
