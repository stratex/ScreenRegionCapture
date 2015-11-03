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
        public MainForm()
        {
            InitializeComponent();
            ScreenRegionCapture capture = new ScreenRegionCapture();
            capture.OnRegionUpdated += ScreenChangedCallback;
            capture.Start(2500);
        }

        private void ScreenChangedCallback(int x, int y, Image updatedImage)
        {
            Console.WriteLine("Region Changed: {0}:{1}", x, y);
        }
    }
}
