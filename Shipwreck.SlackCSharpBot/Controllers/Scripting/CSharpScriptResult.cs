using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections;
using System.IO;
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

    internal sealed class CSharpScriptResult
    {
        internal string Code { get; set; }

        public bool IsFormatted { get; set; } = true;

        public bool IsStatefull { get; set; } = true;

        public bool ShowHelp { get; set; }

        public bool ShowNamespaces { get; set; }

        public IReadOnlyCollection<string> Namespaces { get; set; }


        public IReadOnlyCollection<ScriptVariable> Variables { get; set; }

        public bool OutputLog { get; set; }



        public bool CodeEvaluated { get; set; }

        public object ReturnValue { get; set; }

        public Exception Exception { get; set; }
    }

}