using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides options for the image host.
    /// </summary>
    public sealed class ImageOptions
    {
        /// <summary>
        /// Gets or sets a function used to validate the source image. Only headers are loaded in the image passed into the function.
        /// </summary>
        /// <value>A function that returns <see langword="null"/> if the image validates successfully, otherwise returns an error message.</value>
        public Func<Image, string?>? ValidateSource { get; set; }

        /// <summary>
        /// Gets or sets an image editor that can modify the image.
        /// </summary>
        public ImageEditor? ImageEditor { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies the level of compression for an image when it is saved in JPEG format. The range of useful values for the quality
        /// category is from 0 to 100. The lower the number specified, the higher the compression and therefore the lower the quality of the image. Zero would
        /// give you the lowest quality image and 100 the highest. Default is 75.
        /// </summary>
        public int Quality { get; set; } = 75;
    }
}
