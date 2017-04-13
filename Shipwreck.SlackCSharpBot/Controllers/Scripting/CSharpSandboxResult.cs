using System;
using System.Collections.Generic;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    [Serializable]
    internal sealed class CSharpSandboxResult : MarshalByRefObject
    {
        private List<string> _Namespaces;
        private List<CSharpSandboxVariable> _Variables;

        public string ReturnValue { get; set; }

        public string Exception { get; set; }

        public string StandardOutput { get; set; }

        public string ErrorOutput { get; set; }

        public bool HasNamespace => _Namespaces?.Count > 0;

        public IList<string> Namespaces => _Namespaces ?? (_Namespaces = new List<string>());

        public bool HasVariable
            => _Variables?.Count > 0;

        public IList<CSharpSandboxVariable> Variables
            => _Variables ?? (_Variables = new List<CSharpSandboxVariable>());

        public string SourceCode { get; set; }
    }
}