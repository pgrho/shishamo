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
    public class SachikoOtsukai
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "CHAR")]
        public string Asin { get; set; }

        public byte Quantity { get; set; }

        public int Price { get; set; }

        public string Title { get; set; }

        public DateTime? LastUpdatedAt { get; set; }
    }
}