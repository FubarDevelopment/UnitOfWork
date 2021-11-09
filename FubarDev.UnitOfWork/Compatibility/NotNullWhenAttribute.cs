// <copyright file="NotNullWhenAttribute.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NotNullWhenAttribute : Attribute
    {
        // ReSharper disable once UnusedParameter.Local
        public NotNullWhenAttribute(bool returnValue)
        {
        }
    }
}
#endif
