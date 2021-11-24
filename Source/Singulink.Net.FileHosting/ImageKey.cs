using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Singulink.Enums;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Represents an image ID and format pair, used to identify an image.
    /// </summary>
    public readonly struct ImageKey : IEquatable<ImageKey>
    {
        private static readonly EnumParser<ImageFormat> _formatParser = new EnumParser<ImageFormat>(caseSensitive: false);

        /// <summary>
        /// Gets the ID of the image.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the format of the image.
        /// </summary>
        public ImageFormat Format { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageKey"/> struct.
        /// </summary>
        public ImageKey(Guid id, ImageFormat format)
        {
            Id = id;
            Format = format;
        }

        /// <summary>
        /// Parses the input string into an image key.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <param name="imageKey">The resulting image key.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        public bool TryParse(ReadOnlySpan<char> s, out ImageKey imageKey)
        {
            imageKey = default;

            int dotIndex = s.IndexOf('.');

            if (dotIndex < 0 || !Guid.TryParse(s[..dotIndex], out var id) || !_formatParser.TryParse(s[(dotIndex + 1)..].ToString(), out var format))
                return false;

            imageKey = new ImageKey(id, format);
            return true;
        }

        /// <summary>
        /// Parses the input string into an image key.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        public ImageKey Parse(ReadOnlySpan<char> s)
        {
            if (!TryParse(s, out var imageKey))
                throw new FormatException("The input string was not in the correct format.");

            return imageKey;
        }

        /// <inheritdoc/>
        public bool Equals(ImageKey other) => Id == other.Id && Format == other.Format;

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is ImageKey imageKey && Equals(imageKey);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id, Format);

        /// <summary>
        /// Gets a string representing the image key.
        /// </summary>
        /// <remarks>
        /// <para>The string returned by this method can be parsed back into an image key using the <see cref="Parse(ReadOnlySpan{char})"/> or <see
        /// cref="TryParse(ReadOnlySpan{char}, out ImageKey)"/> methods.</para>
        /// </remarks>
        public override string ToString() => $"{Id:N}.{Format}";
    }
}