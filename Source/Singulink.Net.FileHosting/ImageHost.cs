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

        #endregion

        /// <summary>
        /// Gets the base directory that this image host uses to store files.
        /// </summary>
        public IAbsoluteDirectoryPath BaseDirectory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageHost"/> class.
        /// </summary>
        /// <param name="baseDirectory">The base directory path where images are stored.</param>
        public ImageHost(IAbsoluteDirectoryPath baseDirectory)
        {
            BaseDirectory = baseDirectory;
        }

        /// <summary>
        /// Gets the absolute path to an image.
        /// </summary>
        /// <param name="guid">The ID of the image.</param>
        /// <param name="sizeId">The size identifier string, or <see langword="null"/> to get the main image.</param>
        /// <returns>The path to the image with the specified parameters, which may or may not actually exist.</returns>
        public IAbsoluteFilePath GetAbsoluteImagePath(Guid guid, string? sizeId = null) => BaseDirectory.Combine(GetRelativeImagePath(guid, sizeId));

        /// <summary>
        /// Gets the path to an image relative to the base directory of this image host.
        /// </summary>
        /// <param name="guid">The ID of the image.</param>
        /// <param name="sizeId">The size identifier string, or <see langword="null"/> to get the main image.</param>
        /// <returns>The relative path to the image with the specified parameters, which may or may not actually exist.</returns>
        public IRelativeFilePath GetRelativeImagePath(Guid guid, string? sizeId = null)
        {
            string guidString = guid.ToString("N");
            string fileName = GetImageFileNamePart(guidString) + (string.IsNullOrWhiteSpace(sizeId) ? ".jpg" : $"-{sizeId.Trim()}.jpg");
            return GetImageRelativeDir(guidString).CombineFile(fileName);
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
            sizeId = sizeId.Trim();

            if (sizeId.Length == 0)
                throw new ArgumentException("A size identifier is required.", nameof(sizeId));

            if (options.ImageEditor == null)
                throw new ArgumentException("No image editor specified in options.", nameof(options));

            using var stream = GetAbsoluteImagePath(guid, null).OpenStream(access: FileAccess.Read, share: FileShare.Read | FileShare.Delete);
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
            string searchPattern = GetImageFileNamePart(guidString) + "*.jpg";

            foreach (var file in dir.GetChildFiles(searchPattern)) {
                try {
                    file.Delete();
                }
                catch { }
            }

            try {
                dir.Delete();
                dir.ParentDirectory!.Delete();
            }
            catch { }
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
                if (options.ImageEditor != null) {
                    var editedImage = options.ImageEditor.ApplyEdits(image);

                    if (editedImage != null) {
                        image.Dispose();
                        image = editedImage;
                    }
                }

                var path = GetAbsoluteImagePath(guid, sizeId);

                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, options.Quality);

                CreateDirectory:

                if (string.IsNullOrWhiteSpace(sizeId))
                    path.ParentDirectory.Create();

                try {
                    image.Save(path.PathExport, _jpegEncoder, encoderParams);
                }
                catch (DirectoryNotFoundException) when (string.IsNullOrWhiteSpace(sizeId)) {
                    goto CreateDirectory; // avoid race condition for directory removal during delete prior to image saving.
                }
            }
            finally {
                image.Dispose();
            }
        }

        private IAbsoluteDirectoryPath GetImageDir(string guidString) => BaseDirectory.Combine(GetImageRelativeDir(guidString));

        private IRelativeDirectoryPath GetImageRelativeDir(string guidString) => DirectoryPath
            .ParseRelative(guidString.AsSpan()[0..3], PathFormat.Universal, PathOptions.None)
            .CombineDirectory(guidString.AsSpan()[3..6], PathOptions.None);

        /// <summary>
        /// Gets the image file name (with no extension).
        /// </summary>
        private string GetImageFileNamePart(string guidString) => guidString[6..];
    }
}
