using System;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    [Serializable]
    internal sealed class CSharpSandboxVariable : MarshalByRefObject
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Value { get; set; }
    }
}