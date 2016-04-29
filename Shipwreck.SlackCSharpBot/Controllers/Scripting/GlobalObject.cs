using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    public sealed class GlobalObject
    {
        private PseudoConsole _Console;

        public PseudoConsole Console
            => _Console ?? (_Console = new PseudoConsole());

        public string StandardOutput => _Console?.Result;

        public void Clear()
        {
            _Console?.Clear();
        }
    }
}