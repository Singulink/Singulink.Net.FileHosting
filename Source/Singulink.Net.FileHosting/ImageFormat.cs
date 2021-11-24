using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable SA1649 // File name should match first type name

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Specifies the format an image was saved in.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// Specifies that an image was saved in JPEG format.
        /// </summary>
        Jpeg,
    }

    internal static class ImageFormatExtensions
    {
        public static string GetExtension(this ImageFormat format) => format switch {
            ImageFormat.Jpeg => ".jpg",
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Invalid image format"),
        };
    }
}