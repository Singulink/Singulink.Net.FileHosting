using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Singulink.IO;
using Singulink.Net.FileHosting.Utilities;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides image upload and hosting capabilities.
    /// </summary>
    public class ImageHost
    {
        #region Static Fields

        private static readonly ImageCodecInfo _jpegEncoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
        private static readonly ImageAttributes _imageAttributes = GetImageAttributes();

        private static ImageAttributes GetImageAttributes()
        {
            var a = new ImageAttributes();
            a.SetWrapMode(WrapMode.TileFlipXY); // fixes 50% transparent border
            return a;
        }

        #endregion

        private readonly IAbsoluteDirectoryPath _baseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageHost"/> class.
        /// </summary>
        /// <param name="baseDir">The base path where images will be stored.</param>
        public ImageHost(IAbsoluteDirectoryPath baseDir)
        {
            _baseDir = baseDir;
        }

        /// <summary>
        /// Gets the path to an image.
        /// </summary>
        /// <param name="guid">The ID of the image.</param>
        /// <param name="sizeId">The size identifier string, or <see langword="null"/> to get the main image.</param>
        /// <returns>The path to the image with the specified parameters, which may or may not actually exist.</returns>
        public IAbsoluteFilePath GetImagePath(Guid guid, string? sizeId = null)
        {
            string guidString = guid.ToString("N");
            string fileName = sizeId == null ? $"{guidString}.jpg" : $"{guidString}-{sizeId}.jpg";

            return GetImageDir(guidString).CombineFile(fileName);
        }

        /// <summary>
        /// Adds an image to the repository.
        /// </summary>
        /// <param name="source">The stream source containing the image.</param>
        /// <param name="options">The image processing options.</param>
        /// <returns>The ID of the image.</returns>
        public Guid Add(Stream source, ImageOptions options)
        {
            var guid = SecureGuid.Create();
            Add(guid, null, source, options);
            return guid;
        }

        /// <summary>
        /// Adds an image size to the repository.
        /// </summary>
        /// <param name="guid">The ID of the image.</param>
        /// <param name="sizeId">A string that is appended to the file name to identify the size.</param>
        /// <param name="options">The image options for the new size.</param>
        public void AddSize(Guid guid, string sizeId, ImageOptions options)
        {
            if (options.Size == null)
                throw new ArgumentException("A size must be specified in the options.", nameof(sizeId));

            if (sizeId.Length == 0)
                throw new ArgumentException("A size identifier is required.", nameof(sizeId));

            using var stream = GetImagePath(guid, null).OpenStream(access: FileAccess.Read, share: FileShare.Read | FileShare.Delete);
            Add(guid, sizeId, stream, options);
        }

        /// <summary>
        /// Deletes an image and all of its sizes from the repository.
        /// </summary>
        /// <param name="guid">The ID of the image.</param>
        public void Delete(Guid guid)
        {
            string guidString = guid.ToString("N");
            var dir = GetImageDir(guidString);
            string searchPattern = guidString + "*.jpg";

            foreach (var file in dir.GetChildFiles(searchPattern)) {
                try {
                    file.Delete();
                }
                catch { }
            }
        }

        private void Add(Guid guid, string? sizeId, Stream source, ImageOptions options)
        {
            Image image;

            if (options.ValidateSource != null) {
                if (!source.CanSeek)
                    throw new ArgumentException("Stream must be seekable.", nameof(source));

                long startPosition = source.Position;

                try {
                    image = Image.FromStream(source, false, false);
                }
                catch {
                    throw new ArgumentException("Invalid image format.");
                }

                string message = options.ValidateSource?.Invoke(image);
                image.Dispose();

                if (message != null)
                    throw new ArgumentException(message);

                source.Position = startPosition;
            }

            try {
                image = Image.FromStream(source);
            }
            catch {
                throw new ArgumentException("Invalid image format.");
            }

            try {
                if (options.Size is Size resize) {
                    var resizedImage = options.ResizeMode switch {
                        ImageResizeMode.Downsize => Downsize(image, resize.Width, resize.Height),
                        ImageResizeMode.DownsizeAndCover => DownsizeAndCover(image, resize.Width, resize.Height),
                        _ => throw new ArgumentOutOfRangeException(nameof(options), "Unsupported image size mode."),
                    };

                    if (resizedImage != null) {
                        image.Dispose();
                        image = resizedImage;
                    }
                }

                var path = GetImagePath(guid, sizeId);

                if (sizeId == null)
                    path.ParentDirectory.Create();

                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, options.Quality);

                image.Save(path.PathExport, _jpegEncoder, encoderParams);
            }
            finally {
                image.Dispose();
            }
        }

        private IAbsoluteDirectoryPath GetImageDir(string guidString) => _baseDir
                .CombineDirectory(guidString.AsSpan()[0..3], PathOptions.None)
                .CombineDirectory(guidString.AsSpan()[3..6], PathOptions.None);

        private static Image? Downsize(Image image, int width, int height)
        {
            if (image.Width < width && image.Height < height)
                return null;

            double shrinkByX = (double)image.Width / width;
            double shrinkByY = (double)image.Height / height;

            if (shrinkByX > shrinkByY) {
                height = (int)(image.Height / shrinkByX);
            }
            else {
                width = (int)(image.Width / shrinkByY);
            }

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            try {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using var graphics = Graphics.FromImage(destImage);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.Clear(Color.White);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, _imageAttributes);
            }
            catch {
                destImage.Dispose();
                throw;
            }

            return destImage;
        }

        private static Image? DownsizeAndCover(Image image, int width, int height)
        {
            double srcAspectRatio = (double)image.Width / image.Height;
            double destAspectRatio = (double)width / height;

            int srcX, srcY, srcWidth, srcHeight;

            if (srcAspectRatio >= destAspectRatio) {
                srcHeight = image.Height;
                srcY = 0;
                srcWidth = (int)(srcHeight * destAspectRatio);
                srcX = (image.Width - srcWidth) / 2;
            }
            else {
                srcWidth = image.Width;
                srcX = 0;
                srcHeight = (int)(srcWidth / destAspectRatio);
                srcY = (image.Height - srcHeight) / 2;
            }

            if (srcWidth == width && srcHeight == height)
                return null;

            if (srcWidth < width) {
                width = srcWidth;
                height = srcHeight;
            }

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            try {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using var graphics = Graphics.FromImage(destImage);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.Clear(Color.White);
                graphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel, _imageAttributes);
            }
            catch {
                destImage.Dispose();
                throw;
            }

            return destImage;
        }
    }
}
