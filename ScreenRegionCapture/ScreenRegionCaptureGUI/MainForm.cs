using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScreenRegionCaptureGUI.Classes;

namespace ScreenRegionCaptureGUI
{
    public partial class MainForm : Form
    {
        CompressScreenCapture compressScreenCapture = new CompressScreenCapture();
        DecompressScreenCapture decompressScreenCapture = new DecompressScreenCapture();
        private object lockObject = new object();

        public MainForm()
        {
            InitializeComponent();

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
                //compressScreenCapture.Iterate();
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

    }
}
