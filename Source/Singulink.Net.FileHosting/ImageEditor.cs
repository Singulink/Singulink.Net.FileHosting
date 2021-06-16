using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides image editing functionality.
    /// </summary>
    public abstract class ImageEditor
    {
        /// <summary>
        /// Applies image edits to the provided image.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <returns>The resized image or <see langword="null"/> if no changes were needed.</returns>
        public abstract Image? ApplyEdits(Image image);

        /// <summary>
        /// Gets an <see cref="ImageAttributes"/> object to use for drawing the image.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation of this method returns image attributes with the wrap mode to <see cref="WrapMode.TileFlipXY"/> to prevent a 50%
        /// transparent border.</para>
        /// </remarks>
        protected virtual ImageAttributes GetImageAttributes()
        {
            var a = new ImageAttributes();
            a.SetWrapMode(WrapMode.TileFlipXY);
            return a;
        }

        /// <summary>
        /// Gets a <see cref="Graphics"/> object to use for drawing the image.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation of this method returns a high quality graphics object with the following settings: <c>CompositingMode =
        /// SourceCopy</c>, <c>CompositingQuality = HighQuality</c>, <c>InterpolationMode = HighQualityBicubic</c>, <c>SmoothingMode = HighQuality</c>,
        /// <c>PixelOffsetMode = HighQuality</c>.</para>
        /// </remarks>
        protected virtual Graphics GetGraphics(Image image)
        {
            var graphics = Graphics.FromImage(image);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            return graphics;
        }
    }
}
