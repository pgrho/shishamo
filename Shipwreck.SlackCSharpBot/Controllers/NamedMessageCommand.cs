using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    public abstract class NamedMessageCommand : RegexMessageCommand
    {
        protected NamedMessageCommand(string name, string help)
            : base($"^!{Regex.Escape(name)}(\\s+|$)")
        {
            Name = name;
            Help = help;
        }

        public string Name { get; }

        public string Help { get; set; }
    }
}