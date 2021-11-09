// <copyright file="CountingRepository.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork.Tests.Support
{
    internal record CountingRepository(
        CountingRepositoryManager Manager,
        Guid Id)
        : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Manager.CountDisposedRepository(this);
            return default;
        }
    }
}
