using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Specifies the image size mode.
    /// </summary>
    public enum ImageResizeMode
    {
        /// <summary>
        /// Indicates that the image will maintain its aspect ratio and be scaled down if is larger than the given size.
        /// </summary>
        Downsize,

        /// <summary>
        /// Indicates that the image will be cropped to the aspect ratio of the given size and scaled down if it is larger than the size.
        /// </summary>
        DownsizeAndCover,
    }
}
