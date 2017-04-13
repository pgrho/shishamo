using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    internal sealed class CSharpDirective
    {
        private readonly Action<Match, CSharpSandboxParameter> _Action;

        public CSharpDirective(string name, string corePattern, Action<Match, CSharpSandboxParameter> action, string help)
        {
            Name = name;
            Pattern = new Regex(@"^\s*#" + corePattern + @"(\s+|$|\s*;)", RegexOptions.IgnoreCase);
            _Action = action;
            Help = help;
        }

        public CSharpDirective(string name, string corePattern, string valuePattern, Action<Match, CSharpSandboxParameter> action, string help)
        {
            Name = name;
            Pattern = new Regex(@"^\s*#" + corePattern + @"\s+(?<v>" + valuePattern + @")\s*;", RegexOptions.IgnoreCase);
            _Action = action;
            Help = help;
        }

        public Regex Pattern { get; }

        public string Name { get; }

        public string Help { get; }

        public bool Apply(ref string code, CSharpSandboxParameter parameter)
        {
            var m = Pattern.Match(code);

            if (m.Success)
            {
                code = code.Substring(m.Length);
                _Action(m, parameter);
                return true;
            }

            return false;
        }
    }
}