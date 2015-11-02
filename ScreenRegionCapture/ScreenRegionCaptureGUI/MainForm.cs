using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenRegionCaptureGUI
{
    public partial class MainForm : Form
    {
        #region Variables

        private int ScreenHeight;
        private int ScreenWidth;
        private int blockSize = 64;

        #endregion

        public MainForm()
        {
            InitializeComponent();

            ScreenHeight = Screen.FromControl(this).Bounds.Height;
            ScreenWidth = Screen.FromControl(this).Bounds.Width;

            while (true)
            {
                Thread.Sleep(1000);
                CheckScreenUpdate();
            }

            //var onTick = new TimerCallback(DrawInitBounds);
            //var drawBoundsTimer = new System.Threading.Timer(onTick, null, 0, 1000);

        }

        private Bitmap currentScreen;

        private void CheckScreenUpdate()
        {
            //Vars
            string currentVal;
            string NextVal;

            #region ScreenShot

            // Get Screen
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

            #endregion

            if (currentScreen == null)
            {
                currentScreen = bmpScreenshot;
                return;
            }
            else
                for (var yBlock = 0; yBlock < ScreenHeight/blockSize; yBlock++)
                    for (var xBlock = 0; xBlock < ScreenWidth/blockSize; xBlock++)
                        for (var y = yBlock * blockSize; y < yBlock * blockSize + blockSize; y++)
                            for (var x = xBlock * blockSize; x < xBlock * blockSize + blockSize; x++)
                                if (currentScreen.GetPixel(x, y) != bmpScreenshot.GetPixel(x, y))
                                {
                                    Console.WriteLine("REGION CHANGED: {0}:{1}", xBlock, yBlock);
                                    break;
                                }
        }
    }
}
