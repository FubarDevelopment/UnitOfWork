// <copyright file="UnitOfWorkIdGenerator.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Generator for IDs for unit of works.
    /// </summary>
    internal static class UnitOfWorkIdGenerator
    {
        /// <summary>
        /// Creates a new for a unit of work.
        /// </summary>
        /// <returns>The new identifier.</returns>
        public static string CreateId() => Guid.NewGuid().ToString();
    }
}
