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

        public DecompressScreenCapture(Rectangle Size)
        {
            screenBounds = Size;

            prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(prev))
                g.Clear(Color.Black);
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
                        *dst++ = prevRow[x] ^ curRow[x];
                }
            }
        }

        private Bitmap Difference(Bitmap bmp0, Bitmap bmp1, bool restore)
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

        private void Decompress()
        {
            decompressionBuffer = LZ4Codec.Unwrap(backbuf);

        }

        public Image Iterate(byte[] next)
        {
            backbuf = next;

            Decompress();

            cur = (Bitmap)ImageExtensions.ImageFromRawBgraArray(decompressionBuffer, screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            cur = Difference(prev, cur, true);

            var tmp = cur;
            cur = prev;
            prev = tmp;

            Bitmap ret = prev;

            return ret;
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
