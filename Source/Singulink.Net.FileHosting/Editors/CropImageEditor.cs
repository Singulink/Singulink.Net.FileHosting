using System;
using System.Drawing;
using Singulink.Net.FileHosting.Utilities;

namespace Singulink.Net.FileHosting.Editors
{
    /// <summary>
    /// Image editor that crops the image to the aspect ratio of the specified size and scales it down if it is larger than the size.
    /// </summary>
    public class CropImageEditor : ImageEditor
    {
        /// <summary>
        /// Gets the maximum size of the downsized image.
        /// </summary>
        public Size MaxSize { get; }

        /// <summary>
        /// Gets the background color to apply prior to drawing the image.
        /// </summary>
        public Color BackgroundColor { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CropImageEditor"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public CropImageEditor(Size maxSize, Color backgroundColor)
        {
            MaxSize = maxSize;
            BackgroundColor = backgroundColor;
        }

        /// <inheritdoc/>
        public override Image? ApplyEdits(Image image)
        {
            int destWidth = MaxSize.Width;
            int destHeight = MaxSize.Height;

            double srcAspectRatio = (double)image.Width / image.Height;
            double destAspectRatio = (double)destWidth / destHeight;

            int srcX, srcY, srcWidth, srcHeight;

            if (srcAspectRatio >= destAspectRatio) {
                srcHeight = image.Height;
                srcY = 0;
                srcWidth = (int)Math.Round(srcHeight * destAspectRatio);
                srcX = (image.Width - srcWidth) / 2;
            }
            else {
                srcWidth = image.Width;
                srcX = 0;
                srcHeight = (int)Math.Round(srcWidth / destAspectRatio);
                srcY = (image.Height - srcHeight) / 2;
            }

            if (srcWidth < destWidth) {
                destWidth = srcWidth;
                destHeight = srcHeight;
            }

            if (image.Width == destWidth && image.Height == destHeight && !image.MightHaveTransparency())
                return null;

            var destImage = new Bitmap(destWidth, destHeight);

            try {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using var graphics = GetGraphics(destImage);
                graphics.Clear(BackgroundColor);

                var destRect = new Rectangle(0, 0, destWidth, destHeight);
                graphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel, GetImageAttributes());
            }
            catch {
                destImage.Dispose();
                throw;
            }

            return destImage;
        }
    }
}
