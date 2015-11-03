using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenRegionCaptureGUI.Classes
{
    internal class ScreenRegionCapture
    {
        #region Imports

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int
            wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        #endregion

        #region GlobalVariables

        private Size screenSize { get; set; }
        private int blockSize { get; set; }
        private Bitmap currentBitmap { get; set; }
        private Bitmap currentZone { get; set; }
        private System.Threading.Timer screenBufferTimer { get; set; }

        #endregion

        #region Delegates and Events

        public delegate void OnRegionUpdatedCallback(int xBlock, int yBlock, Image updatedImage);

        public event OnRegionUpdatedCallback OnRegionUpdated;

        #endregion

        #region Constructors

        public ScreenRegionCapture()
        {
            screenSize = Screen.PrimaryScreen.Bounds.Size;
            currentBitmap = GetScreenShot();
            blockSize = 64;          
        }

        public ScreenRegionCapture(int block)
            : this()
        {
            blockSize = block;
        }

        #endregion

        #region Methods

        public bool Start(int interval)
        {
            try
            {
                var onTimerTick = new TimerCallback(Tick);
                screenBufferTimer = new System.Threading.Timer(onTimerTick, null, interval, interval);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                screenBufferTimer.Dispose();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Bitmap GetScreenShot()
        {
            IntPtr hDesk = GetDesktopWindow();
            IntPtr hSrce = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrce);
            IntPtr hBmp = CreateCompatibleBitmap(hSrce, screenSize.Width, screenSize.Height);
            IntPtr hOldBmp = SelectObject(hDest, hBmp);
            bool b = BitBlt(hDest, 0, 0, screenSize.Width, screenSize.Height, hSrce, 0, 0,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            using (var bmp = Bitmap.FromHbitmap(hBmp))
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
                return bmp;
            }
        }

        private Bitmap GetScreenShot(int xPoint, int yPoint)
        {
            IntPtr hDesk = GetDesktopWindow();
            IntPtr hSrce = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrce);
            IntPtr hBmp = CreateCompatibleBitmap(hSrce, blockSize, blockSize);
            IntPtr hOldBmp = SelectObject(hDest, hBmp);
            bool b = BitBlt(hDest, 0, 0, blockSize, blockSize, hSrce, xPoint, yPoint,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            using (var bmp = Bitmap.FromHbitmap(hBmp))
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
                return bmp;
            }
        }

        private void CompareBitmap(Bitmap nextFrame)
        {
            for (var yBlock = 0; yBlock < screenSize.Height/blockSize; yBlock++)
                for (var xBlock = 0; xBlock < screenSize.Width/blockSize; xBlock++)
                    for (var y = yBlock*blockSize; y < yBlock*blockSize + blockSize; y++)
                    {
                        for (var x = xBlock*blockSize; x < xBlock*blockSize + blockSize; x++)
                            if (currentBitmap.GetPixel(x, y) == nextFrame.GetPixel(x, y))
                                if (OnRegionUpdated != null)
                                {
                                    OnRegionUpdated(xBlock, yBlock, GetScreenShot(xBlock*blockSize, yBlock*blockSize));
                                    currentBitmap = nextFrame;
                                    break;
                                }
                        break;
                    }
        }

        #endregion

        #region Callbacks

        private void Tick(object state)
        {
            CompareBitmap(GetScreenShot());
        }

        #endregion
    }
}
