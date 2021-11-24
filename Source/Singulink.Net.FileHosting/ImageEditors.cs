using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Singulink.Net.FileHosting.Editors;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides a set of standard image editors.
    /// </summary>
    public static class ImageEditors
    {
        /// <summary>
        /// Gets an image editor that pads the image to the aspect ratio of the specified size and scales it down if it is larger than the size.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public static PadImageEditor Pad(Size maxSize, Color backgroundColor) => new PadImageEditor(maxSize, backgroundColor);

        /// <summary>
        /// Gets an image editor that crops the image to the aspect ratio of the specified size and scales it down if it is larger than the size.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public static CropImageEditor Crop(Size maxSize, Color backgroundColor) => new CropImageEditor(maxSize, backgroundColor);

        /// <summary>
        /// Gets an image editor that maintains the aspect ratio of the image and scales it down if is larger than the given size.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public static MaxSizeImageEditor MaxSize(Size maxSize, Color backgroundColor) => new MaxSizeImageEditor(maxSize, backgroundColor);
    }
}