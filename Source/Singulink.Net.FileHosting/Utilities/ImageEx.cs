using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Singulink.Net.FileHosting.Utilities
{
    internal static class ImageEx
    {
        public static bool MightHaveTransparency(this Image image)
        {
            return (image.PixelFormat & (PixelFormat.Indexed | PixelFormat.Alpha | PixelFormat.PAlpha)) != PixelFormat.Undefined;
        }
    }
}