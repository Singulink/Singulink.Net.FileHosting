using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Singulink.Net.FileHosting
{
    /// <summary>
    /// Specifies how delete operations handle failure.
    /// </summary>
    public enum DeleteFailureMode
    {
        /// <summary>
        /// Specifies that the image host will suppress exceptions and write a cleanup record file with the exception details into a <c>.cleanup</c> direcory
        /// located inside the host's base directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The file name of the cleanup record is the image ID with a <c>.delete</c> extension. If this flag is not set then cleaning methods will throw an
        /// <see cref="InvalidOperationException"/>. You should periodically call <see cref="ImageHost.Clean(CancellationToken)"/> to read these records and
        /// re-attempt to delete files that failed to delete properly (i.e. due to the file being locked).</para>
        /// </remarks>
        WriteCleanupRecord,

        /// <summary>
        /// Specifies that the image host will throw an <see cref="AggregateException"/> containing any exceptions that occurred during the deletion attempt.
        /// </summary>
        Throw,
    }
}