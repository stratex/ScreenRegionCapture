using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using LZ4;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenRegionCaptureGUI.Classes
{
    class DecompressScreenCapture
    {
        private Rectangle screenBounds;
        private Bitmap prev;
        private Bitmap cur;
        private byte[] decompressionBuffer;
        private byte[] backbuf;

        private int backbufSize;

        private int n = 0;

        public DecompressScreenCapture()
        {
            screenBounds = Screen.PrimaryScreen.Bounds;

            prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(prev))
                g.Clear(Color.Black);

            //decompressionBuffer = new byte[screenBounds.Width * screenBounds.Height * 4];

            //backbufSize = LZ4Codec.MaximumOutputLength(decompressionBuffer.Length);
            //backbuf = new byte[backbufSize];
        }

        private unsafe void ApplyXor(BitmapData previous, BitmapData current)
        {
            byte* prev0 = (byte*)previous.Scan0.ToPointer();
            byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = previous.Height;
            int width = previous.Width;
            int halfwidth = width / 2;

            fixed (byte* target = decompressionBuffer)
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

        private void Decompress()
        {
            decompressionBuffer = LZ4Codec.Unwrap(backbuf);
        }

        public Image Iterate(byte[] next)
        {
            backbuf = next;
            
            Stopwatch sw = Stopwatch.StartNew();

            Decompress();
            TimeSpan timeToDecompress = sw.Elapsed;

            cur = (Bitmap)ImageExtensions.ImageFromRawBgraArray(decompressionBuffer, screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            var lockedCur = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var lockedPrev = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                ApplyXor(lockedPrev, lockedCur);

                TimeSpan timeToXor = sw.Elapsed;

                                //if (n++ % 50 == 0)
                //Console.Write("Iteration: {0:0.00}s, {1:0.00}s {2} Kb => {3:0.0} FPS     \r", timeToDecompress.TotalSeconds, timeToXor.TotalSeconds, length / 1024, 1.0 / sw.Elapsed.TotalSeconds);

                var tmp = cur;
                cur = prev;
                prev = tmp;
            }
            finally
            {
                cur.UnlockBits(lockedCur);
                prev.UnlockBits(lockedPrev);
            }
            //cur = (Bitmap)ImageExtensions.ImageFromRawBgraArray(decompressionBuffer, screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            //prev.Save("ret.bmp");
            //prev.Save("askdhjaksjdh.bmp");

            return ImageExtensions.ImageFromRawBgraArray(decompressionBuffer, screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
        }

    }
    public static class ImageExtensions
    {
        public static Image ImageFromRawBgraArray(this byte[] arr, int width, int height, PixelFormat pixelFormat)
        {
            var output = new Bitmap(width, height, pixelFormat);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect, ImageLockMode.ReadWrite, output.PixelFormat);

            // Row-by-row copy
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
