using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Shipwreck.SlackCSharpBot.Scripting
{
    internal sealed class CSharpScriptCommand : RegexMessageCommand
    {
        private readonly CSharpScriptState _State;

        public CSharpScriptCommand()
            : base(@"^\s*(run|eval(uate)?|csharp|cs)\s+")
        {
            _State = new CSharpScriptState();
        }

        protected override Task<Message> ExecuteAsyncCore(Message message, string text)
            => new CSharpExecutionContext(_State, message, text).ExecuteAsync();
    }
}