// <copyright file="StatusResultEvaluator.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.UnitOfWork.StatusManagement
{
    internal static class StatusResultEvaluator
    {
        public static StatusItemResult ApplyResult(
            StatusItemResult chainResult,
            IStatusItemInfo itemInfo,
            StatusItemResult itemResult)
        {
            if (itemInfo.OwnsTransaction)
            {
                return StatusItemResult.Undefined;
            }

            return GetEffectiveResult(
                chainResult,
                itemResult);
        }

        public static StatusItemResult GetEffectiveResult(
            StatusItemResult chainResult,
            StatusItemResult itemResult)
        {
            return chainResult switch
            {
                StatusItemResult.Commit => itemResult switch
                {
                    StatusItemResult.Commit => itemResult,
                    StatusItemResult.Rollback => itemResult,
                    StatusItemResult.Undefined => chainResult,
                    _ => throw new InvalidOperationException($"Unsupported status {chainResult} => {itemResult}"),
                },
                StatusItemResult.Rollback => chainResult,
                StatusItemResult.Undefined => itemResult,
                _ => throw new InvalidOperationException($"Unsupported status {chainResult}"),
            };
        }
    }
}
