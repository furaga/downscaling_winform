﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

namespace FLib
{

    //using (BitmapIterator iter = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
    //{
    //    byte* data = (byte*)iter.PixelData;
    //    for (int y = 0; y < input.Height; y++)
    //    {
    //        for (int x = 0; x < input.Width; x++)
    //        {
    //            int idx = 4 * x + y * iter.Stride;
    //            byte b = data[idx + 0];
    //            byte g = data[idx + 1];
    //            byte r = data[idx + 2];
    //            byte a = data[idx + 3];
    //        }
    //    }
    //}


    unsafe public class BitmapIterator : IDisposable
    {
        BitmapData lck;
        Bitmap bmp;
        int stride = 0;
        IntPtr pixelData;
        byte* data;
        int pixelSize = 0;

        public Bitmap Bmp { get { return bmp; } }
        public int Stride { get { return stride; } }
        public int PixelSize { get { return pixelSize; } }
        public IntPtr PixelData { get { return pixelData; } }
        public byte* Data { get { return data; } }

        public BitmapIterator(Bitmap bmp, ImageLockMode lockMode, PixelFormat pixelFormat)
        {
            this.bmp = bmp;
            lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), lockMode, pixelFormat);
            pixelData = lck.Scan0;
            data = (byte*)pixelData;
            pixelSize =
                (pixelFormat == PixelFormat.Format32bppArgb ? 4 :
                 pixelFormat == PixelFormat.Format32bppPArgb ? 4 :
                 pixelFormat == PixelFormat.Format32bppRgb ? 3 :
                 pixelFormat == PixelFormat.Format24bppRgb ? 3 :
                 pixelFormat == PixelFormat.Format8bppIndexed ? 1 : 0);
            stride = lck.Width * pixelSize;
            stride = stride % 4 == 0 ? stride : (stride / 4 + 1) * 4;
        }

        public void Dispose()
        {
            if (bmp != null && lck != null)
            {
                bmp.UnlockBits(lck);
            }
        }
    }
}
