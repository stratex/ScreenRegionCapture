using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenRegionCaptureGUI.Classes
{
    class ScreenRegionCapture
    {
        #region GlobalVariables
        private int Height { get; set; }
        private int Width { get; set; }
        private int blockSize { get; set; }
        private Bitmap currentBitmap { get; set; }
        System.Threading.Timer screenBufferTimer { get; set; }
        #endregion

        #region Delegates and Events
        public delegate void OnRegionUpdatedCallback(int xBlock, int yBlock);
        public event OnRegionUpdatedCallback OnRegionUpdated;
        #endregion

        #region Constructors
        public ScreenRegionCapture()
        {
            Height = Screen.PrimaryScreen.Bounds.Height;
            Width = Screen.PrimaryScreen.Bounds.Width;
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
                TimerCallback OnTimerTick = new TimerCallback(Tick);
                screenBufferTimer = new System.Threading.Timer(OnTimerTick, null, interval, interval);
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
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);

            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                Screen.PrimaryScreen.Bounds.Y,
                0,
                0,
                Screen.PrimaryScreen.Bounds.Size,
                CopyPixelOperation.SourceCopy);

            return bmpScreenshot;
        }

        private void CompareBitmap(Bitmap nextFrame)
        {
            for (var yBlock = 0; yBlock < Height / blockSize; yBlock++)
                for (var xBlock = 0; xBlock < Width / blockSize; xBlock++)
                    for (var y = yBlock * blockSize; y < yBlock * blockSize + blockSize; y++)
                        for (var x = xBlock * blockSize; x < xBlock * blockSize + blockSize; x++)
                            if (currentBitmap.GetPixel(x, y) != nextFrame.GetPixel(x, y))
                            {
                                if (OnRegionUpdated != null)
                                    OnRegionUpdated(xBlock, yBlock);
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
