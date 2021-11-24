using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Singulink.IO;
using Singulink.Net.FileHosting.Utilities;

using SystemImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides image upload and hosting capabilities. Methods are thread-safe unless otherwise noted in the summary of the method.
    /// </summary>
    public class ImageHost
    {
        internal const string CleanupNotSupportedMessage =
            "Host instance does not support cleanup. Initialize the host with cleanup support in the constructor to use cleanup records.";

        private static readonly ImageCodecInfo? _jpegEncoder =
            Array.Find(ImageCodecInfo.GetImageEncoders(), e => e.FormatID == SystemImageFormat.Jpeg.Guid);

        /// <summary>
        /// Gets the base directory that this image host uses to store files.
        /// </summary>
        public IAbsoluteDirectoryPath BaseDirectory { get; }

        /// <summary>
        /// Gets the directory path where cleanup records are placed, which is named <c>'.cleanup'</c> and located inside the base directory. Returns null if
        /// options set the delete failure mode to <see cref="DeleteFailureMode.Throw"/>.
        /// </summary>
        public IAbsoluteDirectoryPath? CleanupDirectory { get; }

        /// <summary>
        /// Gets a value that specifies the behavior of delete operations when they fail. Default value (unless otherwise specified when constructing the image
        /// host) is <see cref="DeleteFailureMode.WriteCleanupRecord"/>.
        /// </summary>
        public DeleteFailureMode DeleteFailureMode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageHost"/> class.
        /// </summary>
        /// <param name="baseDirectory">The base directory path where images are stored. Multiple image host instances (either in the same process or in
        /// different processes) can safely share the same directory but it should not be used by anything other than image hosts.</param>
        /// <param name="options">Options for configuring the image host.</param>
        public ImageHost(IAbsoluteDirectoryPath baseDirectory, ImageHostOptions options)
        {
            if (_jpegEncoder == null)
                throw new InvalidOperationException("JPEG encoder was not found.");

            BaseDirectory = baseDirectory;
            DeleteFailureMode = options.DeleteFailureMode;

            BaseDirectory.Create();

            if (options.DeleteFailureMode == DeleteFailureMode.WriteCleanupRecord) {
                CleanupDirectory = baseDirectory.CombineDirectory(".cleanup", PathOptions.None);
                CleanupDirectory.Create();
            }
        }

        /// <summary>
        /// Adds an image to the repository in its original format without applying any editors. The image must be in one of the supported formats (see <see
        /// cref="ImageFormat"/>).
        /// </summary>
        /// <param name="stream">The stream source containing the image.</param>
        /// <param name="validateSource">
        /// Optional function used to validate the source image. Only headers are loaded in the image passed into the function.
        /// </param>
        /// <returns>The image key to access the image.</returns>
        public ImageKey Add(Stream stream, Action<Image>? validateSource)
        {
            if (!stream.CanSeek) {
                var oldStream = stream;
                stream = new MemoryStream();
                oldStream.CopyTo(stream);
            }

            long startPosition = stream.Position;
            ImageFormat format;

            using (var image = Image.FromStream(stream, false, false)) {
                validateSource?.Invoke(image);

                if (image.RawFormat.Guid == SystemImageFormat.Jpeg.Guid)
                    format = ImageFormat.Jpeg;
                else
                    throw new ArgumentException("The stream does not have a valid image format.", nameof(stream));
            }

            stream.Position = startPosition;

            var id = SecureGuid.Create();
            var imageKey = new ImageKey(id, format);
            var filePath = GetAbsoluteImagePath(imageKey);

            using var fs = filePath.OpenStream(FileMode.CreateNew);
            stream.CopyTo(fs);

            return imageKey;
        }

        /// <summary>
        /// Adds an image to the repository with the specified image processing options.
        /// </summary>
        /// <param name="stream">The stream source containing the image.</param>
        /// <param name="options">The image processing options.</param>
        /// <returns>The image key to access the image.</returns>
        public ImageKey Add(Stream stream, ImageOptions options)
        {
            var id = SecureGuid.Create();
            var format = Add(id, null, stream, options);
            return new ImageKey(id, format);
        }

        /// <summary>
        /// Adds an image size to the repository. An <see cref="IOException"/> will be thrown if the size already exists or if multiple threads/processes
        /// attempt to add the same size to the same image simultaneously.
        /// </summary>
        /// <param name="imageKey">The image key of the image to use as a source for the new size.</param>
        /// <param name="sizeId">A string that is appended to the file name to identify the size.</param>
        /// <param name="options">The image options for the new size.</param>
        /// <returns>
        /// The image key for the new image size, which has the same ID as the source image and a format determined by <paramref name="options"/>.
        /// </returns>
        public ImageKey AddSize(ImageKey imageKey, string sizeId, ImageOptions options)
        {
            sizeId = sizeId.Trim();

            if (sizeId.Length == 0)
                throw new ArgumentException("A size identifier is required.", nameof(sizeId));

            if (options.ImageEditor == null)
                throw new ArgumentException("No image editor specified in options.", nameof(options));

            using var stream = GetAbsoluteImagePath(imageKey, null).OpenStream(access: FileAccess.Read, share: FileShare.Read | FileShare.Delete);
            var format = Add(imageKey.Id, sizeId, stream, options);

            return new ImageKey(imageKey.Id, format);
        }

        /// <summary>
        /// Deletes an image and all of its sizes/formats from the repository.
        /// </summary>
        /// <param name="imageId">The ID of the image (from the image key).</param>
        /// <exception cref="AggregateException">
        /// Some files could not be deleted. Only thown if the <see cref="DeleteFailureMode.Throw"/> option was set in the image host options.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Clean(CancellationToken)"/> method can be used to clean up any files that fail to delete.</para>
        /// </remarks>
        public void Delete(Guid imageId)
        {
            try {
                DeleteWithAggregateThrow(imageId);
            }
            catch (AggregateException ex) when (DeleteFailureMode != DeleteFailureMode.Throw) {
                if (DeleteFailureMode != DeleteFailureMode.WriteCleanupRecord)
                    return;

                var cleanupRecord = CleanupDirectory!.CombineFile(imageId.ToString("N") + ".delete");
                using var sw = new StreamWriter(cleanupRecord.PathExport, false);

                WriteCleanupRecord(sw, "IMAGE FILE DELETE FAILED", ex);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are files that need to be cleaned up. This method is thread-safe.
        /// </summary>
        public bool NeedsCleaning()
        {
            return CleanupDirectory != null ?
                CleanupDirectory.GetChildFiles("*.delete").Any() :
                throw new InvalidOperationException("Cleanup is not supported.");
        }

        /// <summary>
        /// Cleans files that failed to be deleted during previous delete or clean operations. If another thread/process is currently running a clean
        /// operation on the folder assigned to this image host, an <see cref="IOException"/> is thrown.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">
        /// This image host does not support cleanup.
        /// </exception>
        /// <exception cref="IOException">
        /// Another thread/process is already running a cleaning operation on the base directory.
        /// </exception>
        public void Clean(CancellationToken cancellationToken = default)
        {
            if (CleanupDirectory == null)
                throw new InvalidOperationException(CleanupNotSupportedMessage);

            var cleanLockFile = CleanupDirectory.CombineFile(".lock", PathOptions.None);
            FileStream cleanLock;

            try {
                cleanLock = cleanLockFile.OpenStream(FileMode.OpenOrCreate, options: FileOptions.DeleteOnClose);
            }
            catch (IOException ex) {
                throw new IOException($"Could not obtain a lock on '{cleanLockFile.PathDisplay}'. Another thread or process may be cleaning right now.", ex);
            }

            using (cleanLock)
            {
                foreach (var cleanupRecord in CleanupDirectory.GetChildFiles("*.delete")) {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    var imageId = Guid.Parse(cleanupRecord.NameWithoutExtension);

                    try {
                        DeleteWithAggregateThrow(imageId);
                    }
                    catch (AggregateException ex) {
                        using var fs = cleanupRecord.OpenStream(FileMode.Truncate);
                        using var sw = new StreamWriter(fs);

                        WriteCleanupRecord(sw, "IMAGE FILE CLEANUP FAILED", ex);
                        continue;
                    }

                    try {
                        cleanupRecord.Delete();
                    }
                    catch (FileNotFoundException) { }
                    catch (Exception ex) {
                        try {
                            const string header = "CLEANUP RECORD REMOVAL FAILED";

                            using var fs = cleanupRecord.OpenStream(FileMode.Truncate);
                            using var sw = new StreamWriter(fs);

                            WriteCleanupRecord(sw, header, ex);
                        }
                        catch (FileNotFoundException) { }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the absolute path to an image. This method does not validate that the file exists.
        /// </summary>
        /// <param name="imageKey">The image key.</param>
        /// <param name="sizeId">The size identifier string, or <see langword="null"/> to get the main image.</param>
        public IAbsoluteFilePath GetAbsoluteImagePath(ImageKey imageKey, string? sizeId = null)
        {
            return GetAbsoluteFilePath(imageKey.Id, imageKey.Format.GetExtension(), sizeId);
        }

        /// <summary>
        /// Gets the path to an image relative to the base directory of this image host. This method does not validate that the file exists.
        /// </summary>
        /// <param name="imageKey">The image key.</param>
        /// <param name="sizeId">The size identifier string, or <see langword="null"/> to get the main image.</param>
        public IRelativeFilePath GetRelativeImagePath(ImageKey imageKey, string? sizeId = null)
        {
            return GetRelativeFilePath(imageKey.Id, imageKey.Format.GetExtension(), sizeId);
        }

        private ImageFormat Add(Guid imageId, string? sizeId, Stream stream, ImageOptions options)
        {
            if (options.ValidateSource != null) {
                if (!stream.CanSeek) {
                    var oldStream = stream;
                    stream = new MemoryStream();
                    oldStream.CopyTo(stream);
                }

                long startPosition = stream.Position;

                using var validateImage = Image.FromStream(stream, false, false);
                options.ValidateSource.Invoke(validateImage);
                stream.Position = startPosition;
            }

            var image = Image.FromStream(stream);

            try {
                RotateImageByExifOrientationData(image);
                ImageFormat format;

                if (options.ImageEditor != null) {
                    var editedImage = options.ImageEditor.ApplyEdits(image);
                    format = GetImageFormat(image, editedImage, options);

                    if (editedImage != null) {
                        image.Dispose();
                        image = editedImage;
                    }
                }
                else {
                    format = GetImageFormat(image, null, options);
                }

                var path = GetAbsoluteImagePath(new ImageKey(imageId, format), sizeId);

                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, options.JpegQuality);

                CreateDirectory:

                if (string.IsNullOrWhiteSpace(sizeId))
                    path.ParentDirectory.Create();

                try {
                    image.Save(path.PathExport, _jpegEncoder, encoderParams);
                }
                catch (DirectoryNotFoundException) when (string.IsNullOrWhiteSpace(sizeId)) {
                    goto CreateDirectory; // avoid race condition for directory removal during delete prior to image saving.
                }

                return format;
            }
            finally {
                image.Dispose();
            }
        }

        private static void RotateImageByExifOrientationData(Image image)
        {
            const int orientationId = 0x0112;

            if (image.PropertyIdList.Contains(orientationId)) {
                var propertyItem = image.GetPropertyItem(orientationId);

                if (propertyItem?.Value?.Length > 0) {
                    var flipType = GetRotateFlipTypeByExifOrientationData(propertyItem.Value[0]);

                    if (flipType != RotateFlipType.RotateNoneFlipNone) {
                        image.RotateFlip(flipType);
                        image.RemovePropertyItem(orientationId);
                    }
                }
            }
        }

        private static RotateFlipType GetRotateFlipTypeByExifOrientationData(int orientation) => orientation switch {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone,
        };

        private static ImageFormat GetImageFormat(Image originalImage, Image? editedImage, ImageOptions options)
        {
            if (options.ImageSaveFormat != ImageSaveFormat.AlwaysJpeg)
                 throw new ArgumentException("Unsupported image save format in options.", nameof(options));

            return ImageFormat.Jpeg;
        }

        private IAbsoluteFilePath GetAbsoluteFilePath(Guid id, string extension, string? sizeId = null)
        {
            return BaseDirectory.Combine(GetRelativeFilePath(id, extension, sizeId));
        }

        private IRelativeFilePath GetRelativeFilePath(Guid id, string extension, string? sizeId = null)
        {
            sizeId = GetValidatedSizeId(sizeId);

            string guidString = id.ToString("N");
            string fileNamePart = GetImageFileNameWithoutExtension(guidString);

            string fullFileName;

            if (sizeId == null)
                fullFileName = fileNamePart + extension;
            else
                fullFileName = $"{fileNamePart}-{sizeId.Trim()}{extension}";

            return GetRelativeDirectory(guidString).CombineFile(fullFileName, PathOptions.NoUnfriendlyNames | PathOptions.NoNavigation);
        }

        private IAbsoluteDirectoryPath GetAbsoluteDirectoryPath(string guidString) => BaseDirectory.Combine(GetRelativeDirectory(guidString));

        private IRelativeDirectoryPath GetRelativeDirectory(string guidString) => DirectoryPath
            .ParseRelative(guidString.AsSpan()[0..3], PathFormat.Universal, PathOptions.None)
            .CombineDirectory(guidString.AsSpan()[3..6], PathOptions.None);

        private string GetImageFileNameWithoutExtension(string guidString) => guidString[6..];

        private string? GetValidatedSizeId(string? sizeId)
        {
            if (sizeId == null || string.IsNullOrWhiteSpace(sizeId))
                return null;

            sizeId = sizeId.Trim();

            if (sizeId.Contains('.') || sizeId.Contains('/') || sizeId.Contains('\\'))
                throw new ArgumentException("Size ID cannot contain the any of the following characters: . \\ /");

            return sizeId;
        }

        private void DeleteWithAggregateThrow(Guid imageId)
        {
            string guidString = imageId.ToString("N");
            var dir = GetAbsoluteDirectoryPath(guidString);
            string searchPattern = GetImageFileNameWithoutExtension(guidString) + "*";

            List<Exception> exceptions = null;

            try {
                foreach (var file in dir.GetChildFiles(searchPattern)) {
                    try {
                        file.Delete();
                    }
                    catch (FileNotFoundException) { }
                    catch (Exception ex) {
                        (exceptions ??= new()).Add(ex);
                    }
                }
            }
            catch (DirectoryNotFoundException) { }

            if (exceptions != null)
                throw new AggregateException("Some files could not be deleted.", exceptions);

            try {
                dir.Delete();
                dir.ParentDirectory!.Delete();
            }
            catch { }
        }

        private static void WriteCleanupRecord(StreamWriter sw, string header, Exception ex)
        {
            sw.WriteLine($"=============== {header} ===============");
            sw.WriteLine();
            sw.WriteLine("Exception: " + ex);
            sw.WriteLine();
            sw.WriteLine();
        }
    }
}