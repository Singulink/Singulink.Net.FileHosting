using System;
using System.Drawing;
using Singulink.Net.FileHosting.Utilities;

namespace Singulink.Net.FileHosting.Editors
{
    /// <summary>
    /// Image editor that pads the image to the aspect ratio of the specified size and scales it down if it is larger than the size.
    /// </summary>
    public class PadImageEditor : ImageEditor
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
        /// Initializes a new instance of the <see cref="PadImageEditor"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public PadImageEditor(Size maxSize, Color backgroundColor)
        {
            MaxSize = maxSize;
            BackgroundColor = backgroundColor;
        }

        /// <inheritdoc/>
        public override Image? ApplyEdits(Image image)
        {
            int resultWidth = MaxSize.Width;
            int resultHeight = MaxSize.Height;

            double srcAspectRatio = (double)image.Width / image.Height;
            double destAspectRatio = (double)resultWidth / resultHeight;

            int destX, destY, destWidth, destHeight;

            if (srcAspectRatio >= destAspectRatio) {
                if (resultWidth > image.Width) {
                    resultWidth = image.Width;
                    resultHeight = (int)Math.Round(resultWidth / destAspectRatio);
                }

                destX = 0;
                destWidth = resultWidth;

                destHeight = (int)Math.Round(resultWidth / srcAspectRatio);
                destY = (resultHeight - destHeight) / 2;
            }
            else {
                if (resultHeight > image.Height) {
                    resultHeight = image.Height;
                    resultWidth = (int)Math.Round(resultHeight * destAspectRatio);
                }

                destY = 0;
                destHeight = resultHeight;

                destWidth = (int)Math.Round(resultHeight * srcAspectRatio);
                destX = (resultWidth - destWidth) / 2;
            }

            if (image.Width == resultWidth && image.Height == resultHeight && !image.MightHaveTransparency())
                return null;

            var destImage = new Bitmap(resultWidth, resultHeight);

            try {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using var graphics = GetGraphics(destImage);
                graphics.Clear(BackgroundColor);

                var destRect = new Rectangle(destX, destY, destWidth, destHeight);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, GetImageAttributes());
            }
            catch {
                destImage.Dispose();
                throw;
            }

            return destImage;
        }
    }
}
