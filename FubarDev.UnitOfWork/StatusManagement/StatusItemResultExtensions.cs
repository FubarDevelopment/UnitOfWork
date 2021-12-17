// <copyright file="StatusItemResultExtensions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Extension methods for <see cref="StatusItemResult"/>.
    /// </summary>
    internal static class StatusItemResultExtensions
    {
        /// <summary>
        /// Gets the log level to be used for a given status.
        /// </summary>
        /// <param name="result">The status to get the log level for.</param>
        /// <returns>The log level.</returns>
        public static LogLevel GetLogLevel(this StatusItemResult result)
        {
            return result switch
            {
                StatusItemResult.Rollback => LogLevel.Warning,
                _ => LogLevel.Information,
            };
        }
    }
}
