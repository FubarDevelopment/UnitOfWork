// <copyright file="TestModel.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FubarDev.UnitOfWork.Tests.Support.DbModels
{
    [Table("TestModel")]
    public class TestModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        public virtual int Id { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [MaxLength(100)]
        public virtual string? Text { get; set; }
    }
}
