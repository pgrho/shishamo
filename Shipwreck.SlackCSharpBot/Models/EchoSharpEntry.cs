using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Models
{
    internal sealed class EchoSharpEntry
    {
        private string _Pattern;

        private Regex _Regex;

        [Key]
        [StringLength(64)]
        public string Name { get; set; }

        [Required]
        [StringLength(1024)]
        public string Pattern
        {
            get
            {
                return _Pattern;
            }
            set
            {
                if (value != _Pattern)
                {
                    _Pattern = value;
                    _Regex = null;
                }
            }
        }

        [Required]
        [StringLength(2048)]
        public string Command { get; set; }

        public Regex Regex
            => _Regex ?? (_Regex = new Regex(_Pattern ?? "^$", RegexOptions.IgnoreCase));
    }
}