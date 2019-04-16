using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ScreenRegionCaptureGUI.Classes;

namespace ScreenRegionCaptureGUI
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private Rectangle rect = new Rectangle(0, 0, 2560, 1440);
        private CompressScreenCapture compressScreenCapture;
        private DecompressScreenCapture decompressScreenCapture;
        private object lockObject = new object();

        public MainForm()
        {
            InitializeComponent();

            compressScreenCapture = new CompressScreenCapture(rect);
            decompressScreenCapture = new DecompressScreenCapture(rect);

            Thread t = new Thread(new ThreadStart(Iter));
            t.IsBackground = true;
            t.Start();

        }

        private void Iter()
        {
            Image imgClone;
            while (true)
            {
                byte[] data = compressScreenCapture.Iterate();
                lock (lockObject)
                {
                    Image bmp = decompressScreenCapture.Iterate(data);
                    imgClone = (Image)bmp.Clone();
                }
                UpdateImage(imgClone);
            }
        }

        private void UpdateImage(Image bmp)
        {
            if (!InvokeRequired)
            {
                pictureBox1.Image = bmp;
            }
            else
            {
                Invoke(new Action<Bitmap>(UpdateImage), bmp);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            var curPos = Cursor.Position;
            Cursor.Position = new Point(e.X * Screen.PrimaryScreen.Bounds.Width / pictureBox1.Width, e.Y * Screen.PrimaryScreen.Bounds.Height / pictureBox1.Height);
            DoMouseClick();
            Cursor.Position = curPos;
        }

        private void DoMouseClick()
        {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }
    }
}
