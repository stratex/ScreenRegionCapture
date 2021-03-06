﻿using System;
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

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);

        private enum ProcessDPIAwareness
        {
            ProcessDPIUnaware = 0,
            ProcessSystemDPIAware = 1,
            ProcessPerMonitorDPIAware = 2
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private Rectangle rect = new Rectangle(0, 0, 1856, 1392);
        private ScreenRegionCapture.CompressScreen compressScreen;
        private ScreenRegionCapture.DecompressScreen decompressScreen;
        private object lockObject = new object();

        public MainForm()
        {
            SetDpiAwareness();
            InitializeComponent();

            compressScreen = new ScreenRegionCapture.CompressScreen(rect);
            decompressScreen = new ScreenRegionCapture.DecompressScreen(rect);

            Thread t = new Thread(new ThreadStart(Iter));
            t.IsBackground = true;
            t.Start();

        }

        private void Iter()
        {
            Image imgClone;
            while (true)
            {
                byte[] data = compressScreen.Iterate();
                lock (lockObject)
                {
                    Image bmp = decompressScreen.Iterate(data);
                    imgClone = (Image)bmp.Clone();
                }
                UpdateImage(imgClone);
            }
        }

        private void UpdateImage(Image bmp)
        {
            if (!InvokeRequired)
                pictureBox1.Image = bmp;
            else
                Invoke(new Action<Bitmap>(UpdateImage), bmp);
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

        private static void SetDpiAwareness()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                    SetProcessDpiAwareness(ProcessDPIAwareness.ProcessPerMonitorDPIAware);
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
