// <copyright file="StatusItemContainer.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.UnitOfWork.StatusManagement
{
    internal class StatusItemContainer<TStatusItem>
        where TStatusItem : class, IStatusItemInfo
    {
        private StatusItemResult? _result;

        public StatusItemContainer(TStatusItem statusItem)
        {
            StatusItem = statusItem;
        }

        /// <summary>
        /// Gets the internal ID of this status container.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        public TStatusItem StatusItem { get; }

        public StatusItemResult Result
        {
            get => _result ?? StatusItemResult.Undefined;
            set => _result = value;
        }

        public StatusItemResult? EffectiveResult { get; set; }

        public bool IsCompleted => _result != null;
    }
}
