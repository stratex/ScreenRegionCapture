using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenRegionCaptureGUI.Classes
{
    class CompressedCaptureScreen
    {
        public CompressedCaptureScreen(int size)
        {
            Data = new byte[size];
            Size = 4;
        }

        public int Size;
        public byte[] Data;
    }
}
