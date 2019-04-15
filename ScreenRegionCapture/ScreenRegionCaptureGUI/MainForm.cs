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
        public MainForm()
        {
            InitializeComponent();

            Thread t = new Thread(new ThreadStart(Iter));
            t.IsBackground = true;
            t.Start();

        }

        private void Iter()
        {
            while (true)
            {
                UpdateImage(decompressScreenCapture.Iterate(compressScreenCapture.Iterate()));
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
