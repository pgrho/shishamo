using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    internal sealed class CSharpRemoteSandbox : MarshalByRefObject
    {
        private sealed class InTextReader : TextReader
        {
            public override int Read()
            {
                throw new InvalidOperationException("標準入力を使用することはできません。");
            }

            public override int Read(char[] buffer, int index, int count)
            {
                throw new InvalidOperationException("標準入力を使用することはできません。");
            }

            public override int ReadBlock(char[] buffer, int index, int count)
            {
                throw new InvalidOperationException("標準入力を使用することはできません。");
            }

            public override string ReadLine()
            {
                throw new InvalidOperationException("標準入力を使用することはできません。");
            }

            public override string ReadToEnd()
            {
                throw new InvalidOperationException("標準入力を使用することはできません。");
            }

            public override Task<int> ReadAsync(char[] buffer, int index, int count)
                => Task.FromException<int>(new InvalidOperationException("標準入力を使用することはできません。"));

            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
                => Task.FromException<int>(new InvalidOperationException("標準入力を使用することはできません。"));

            public override Task<string> ReadLineAsync()
                => Task.FromException<string>(new InvalidOperationException("標準入力を使用することはできません。"));

            public override Task<string> ReadToEndAsync()
                => Task.FromException<string>(new InvalidOperationException("標準入力を使用することはできません。"));
        }

        private HashSet<Assembly> _Assemblies;
        private HashSet<string> _Namespaces;

        private ScriptState<object> _State;

        public CSharpSandboxResult Execute(CSharpSandboxParameter parameter)
        {
            Task<CSharpSandboxResult> t;
            try
            {
                t = ExecuteAsyncCore(parameter);
                t.ConfigureAwait(false);
                t.Wait();

                return t.Result;
            }
            catch (Exception ex)
            {
                return new CSharpSandboxResult()
                {
                    Exception = ex.ToString()
                };
            }
        }

        private Semaphore _QueueSemaphore = new Semaphore(1, 1);

        private async Task<CSharpSandboxResult> ExecuteAsyncCore(CSharpSandboxParameter parameter)
        {
            if (!parameter.UsesSeparateContext)
            {
                _QueueSemaphore.WaitOne();
            }
            try
            {
                var outWriter = new StringWriter();
                var errWriter = new StringWriter();
                Console.SetIn(new InTextReader());
                Console.SetOut(outWriter);
                Console.SetError(errWriter);

                IEnumerable<Assembly> asms;
                IReadOnlyCollection<string> ns;
                if (parameter.UsesSeparateContext)
                {
                    asms = _DefaultAssemblies;
                    ns = _DefaultNamespaces;
                }
                else
                {
                    if (_State == null || parameter.ResetState)
                    {
                        ResetState();
                    }

                    if (parameter.HasAssembly)
                    {
                        _Assemblies.UnionWith(parameter.Assemblies.Select(an => Assembly.LoadFile(an)));
                    }
                    if (parameter.HasNamespace)
                    {
                        _Namespaces.UnionWith(parameter.Namespaces);
                    }
                    asms = _Assemblies;
                    ns = _Namespaces;
                }

                //   var ns = _Namespaces.ToArray();
                var r = new CSharpSandboxResult();
                try
                {
                    ScriptState<object> s;
                    if (string.IsNullOrEmpty(parameter.Code))
                    {
                        s = parameter.UsesSeparateContext ? null : _State;
                    }
                    else
                    {
                        var op = ScriptOptions.Default.AddReferences(asms).AddImports(ns);
                        if (parameter.UsesSeparateContext)
                        {
                            s = await CSharpScript.RunAsync(parameter.Code, op).ConfigureAwait(false);
                        }
                        else if (_State == null)
                        {
                            s = _State = await CSharpScript.RunAsync(parameter.Code, op).ConfigureAwait(false);
                        }
                        else
                        {
                            s = _State = await _State.ContinueWithAsync(parameter.Code, op).ConfigureAwait(false);
                        }

                        r.ReturnValue = parameter.ReturnsRawValue ? (s.ReturnValue ?? "null").ToString() : s.ReturnValue.ToMarkup(ns, true);
                    }
                    if (parameter.ReturnsNamespaces || parameter.ReturnsVariables)
                    {
                        foreach (var n in ns)
                        {
                            r.Namespaces.Add(n);
                        }
                    }
                    if (parameter.ReturnsVariables)
                    {
                        if (s != null)
                        {
                            foreach (var v in s.Variables)
                            {
                                r.Variables.Add(new CSharpSandboxVariable()
                                {
                                    Name = v.Name,
                                    TypeName = v.Type.AssemblyQualifiedName,
                                    Value = v.Value.ToMarkup(ns, false)
                                });
                            }
                        }
                    }
                    if (parameter.ReturnsSourceCode)
                    {
                        r.SourceCode = s?.Script.GetSourceCode();
                    }
                }
                catch (Exception ex)
                {
                    r.Exception = ex.ToString();
                }
                r.StandardOutput = outWriter.ToString();
                r.ErrorOutput = errWriter.ToString();

                return r;
            }
            finally
            {
                if (!parameter.UsesSeparateContext)
                {
                    _QueueSemaphore.Release();
                }
            }
        }

        private static readonly Assembly[] _DefaultAssemblies = {
                        typeof(object).Assembly,
                        typeof(Uri).Assembly,
                        typeof(Enumerable).Assembly,
                        typeof(XmlDocument).Assembly,
                        typeof(XDocument).Assembly,
                        typeof(DataContractAttribute).Assembly};
        private static readonly string[] _DefaultNamespaces = {
                        typeof(object).Namespace,
                        typeof(List<>).Namespace,
                        typeof(Enumerable).Namespace,
                        typeof(DataContractAttribute).Namespace,
                        typeof(Task).Namespace,
                        typeof(StringBuilder).Namespace,
                        typeof(Regex).Namespace,
                        typeof(XmlDocument).Namespace,
                        typeof(XDocument).Namespace};

        private void ResetState()
        {
            _Assemblies = new HashSet<Assembly>(_DefaultAssemblies);
            _Namespaces = new HashSet<string>(_DefaultNamespaces);
            _State = null;
        }
    }
}