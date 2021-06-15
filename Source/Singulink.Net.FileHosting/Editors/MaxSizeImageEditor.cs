using System;
using System.Drawing;
using Singulink.Net.FileHosting.Utilities;

namespace Singulink.Net.FileHosting.Editors
{
    /// <summary>
    /// Image editor that maintains the aspect ratio of the image and scales it down if is larger than the given size.
    /// </summary>
    public class MaxSizeImageEditor : ImageEditor
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
        /// Initializes a new instance of the <see cref="MaxSizeImageEditor"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size of the image.</param>
        /// <param name="backgroundColor">Background color to apply prior to drawing the image.</param>
        public MaxSizeImageEditor(Size maxSize, Color backgroundColor)
        {
            MaxSize = maxSize;
            BackgroundColor = backgroundColor;
        }

        /// <inheritdoc/>
        public override Image? ApplyEdits(Image image)
        {
            int destWidth;
            int destHeight;

            if (image.Width < MaxSize.Width && image.Height < MaxSize.Height) {
                if (!image.MightHaveTransparency())
                    return null;

                destWidth = image.Width;
                destHeight = image.Height;
            }
            else {
                double sizeByX = (double)image.Width / MaxSize.Width;
                double sizeByY = (double)image.Height / MaxSize.Height;

                if (sizeByX > sizeByY) {
                    destWidth = MaxSize.Width;
                    destHeight = (int)Math.Round(image.Height / sizeByX);
                }
                else {
                    destWidth = (int)Math.Round(image.Width / sizeByY);
                    destHeight = MaxSize.Height;
                }
            }

            var destImage = new Bitmap(destWidth, destHeight);

            try {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using var graphics = GetGraphics(destImage);
                graphics.Clear(BackgroundColor);

                var destRect = new Rectangle(0, 0, destWidth, destHeight);
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
