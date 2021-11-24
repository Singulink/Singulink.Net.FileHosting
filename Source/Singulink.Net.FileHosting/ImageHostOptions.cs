using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Provides options for image host instances.
    /// </summary>
    public class ImageHostOptions
    {
        /// <summary>
        /// Gets or sets a value that specifies the behavior of delete operations when they fail. Default value is <see
        /// cref="DeleteFailureMode.WriteCleanupRecord"/>.
        /// </summary>
        public DeleteFailureMode DeleteFailureMode { get; set; } = DeleteFailureMode.WriteCleanupRecord;
    }
}