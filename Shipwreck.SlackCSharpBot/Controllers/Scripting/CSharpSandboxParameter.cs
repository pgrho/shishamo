using System;
using System.Collections.Generic;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    [Serializable]
    internal sealed class CSharpSandboxParameter : MarshalByRefObject
    {
        private List<string> _Assemblies;
        private List<string> _Namespaces;

        public string Code { get; set; }

        public bool ResetState { get; set; }

        public bool UsesSeparateContext { get; set; }

        public bool ReturnsRawValue { get; set; }

        public bool ReturnsNamespaces { get; set; }

        public bool ReturnsSourceCode { get; set; }

        public bool ReturnsVariables { get; set; }

        public bool HasAssembly => _Assemblies?.Count > 0;

        public IList<string> Assemblies => _Assemblies ?? (_Assemblies = new List<string>());

        public bool HasNamespace => _Namespaces?.Count > 0;

        public IList<string> Namespaces => _Namespaces ?? (_Namespaces = new List<string>());

        public CSharpSandboxResult Execute()
            => CSharpRemoteSandbox.Default.Execute(this);
    }
}