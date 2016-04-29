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

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    internal sealed class CSharpScriptState
    {
        private readonly List<Assembly> _Assemblies = new List<Assembly>();
        private readonly List<string> _Namespaces = new List<string>();

        private ScriptState<object> _State;

        private bool _OptionsChanged;

        internal GlobalObject Globals { get; private set; }

        public CSharpScriptState()
        {
            ResetState();
        }

        public async Task<ScriptState<object>> RunAsync(string code)
        {
            var state = _State;
            if (state == null)
            {
                // 初回
                state = await CSharpScript.RunAsync(code, GetScriptOptions(), globals: Globals);
            }
            else
            {
                if (_OptionsChanged)
                {
                    // NS追加
                    state = await state.ContinueWithAsync(code, GetScriptOptions());
                }
                else
                {
                    // 二回目以降
                    state = await state.ContinueWithAsync(code);
                }
            }
            return _State = state;
        }

        public ScriptOptions GetScriptOptions()
            => ScriptOptions.Default.AddReferences(_Assemblies).AddImports(_Namespaces);

        public void ResetState()
        {
            lock (_Assemblies)
            {
                _State = null;
                Globals = new GlobalObject();


                _Assemblies.Clear();
                _Assemblies.AddRange(new[]
                {
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(XmlDocument).Assembly,
                    typeof(XDocument).Assembly,
                    typeof(DataContractAttribute).Assembly
                });

                _Namespaces.Clear();
                _Namespaces.AddRange(new[]
                {
                    typeof(object).Namespace,
                    typeof(List<>).Namespace,
                    typeof(Enumerable).Namespace,
                    typeof(DataContractAttribute).Namespace,
                    typeof(Task).Namespace,
                    typeof(StringBuilder).Namespace,
                    typeof(Regex).Namespace,
                    typeof(XmlDocument).Namespace,
                    typeof(XDocument).Namespace
                });

                _OptionsChanged = false;
            }
        }

        public string GetCode()
        {
            var sb = new StringBuilder();

            Script st;
            lock (_Assemblies)
            {
                st = _State?.Script;
            }
            if (st != null)
            {
                var stack = new Stack<Script>();
                while (st != null)
                {
                    stack.Push(st);
                    st = st.Previous;
                }

                foreach (var s in stack)
                {
                    if (!string.IsNullOrWhiteSpace(s.Code))
                    {
                        sb.Append(s.Code);
                        if (sb[sb.Length - 1] == ';')
                        {
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.AppendLine(";");
                        }
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }

        public string[] GetNamespaces()
        {
            lock (_Assemblies)
            {
                return _Namespaces.ToArray();
            }
        }

        public IEnumerable<ScriptVariable> GetVariables()
        {
            lock (_Assemblies)
            {
                return _State?.Variables.ToArray();
            }
        }

        public void AddNamespace(string ns)
        {
            if (ns == null)
            {
                return;
            }

            ns = Regex.Replace(ns, "\\s", "");

            lock (_Assemblies)
            {
                if (!_Namespaces.Contains(ns))
                {
                    _Namespaces.Add(ns);
                    _OptionsChanged = true;
                }
            }
        }
        public void AddNamespaces(IEnumerable<string> newNs)
        {
            if (newNs == null)
            {
                return;
            }
            lock (_Assemblies)
            {
                foreach (var ns in newNs)
                {
                    var v = Regex.Replace(ns, "\\s", "");
                    if (!_Namespaces.Contains(v))
                    {
                        _Namespaces.Add(v);
                        _OptionsChanged = true;
                    }
                }
            }
        }
    }
}