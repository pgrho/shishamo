using Shipwreck.SlackCSharpBot.Models.Migrations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Models
{
    public sealed class TaskRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR")]
        [StringLength(32)]
        public string UserName { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Column(TypeName = "DATE")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "DATE")]
        public DateTime? DoneAt { get; set; }

        public bool IsDone { get; set; }

        public bool IsDeleted { get; set; }
    }
}