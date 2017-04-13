using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    internal sealed class CSharpSandbox : IDisposable
    {
        private readonly object _SandboxLock = new object();
        private AppDomain _SandboxDomain;

        private AppDomain SandboxDomain
        {
            get
            {
                lock (_SandboxLock)
                {
                    if (_SandboxDomain == null)
                    {
                        var cad = AppDomain.CurrentDomain;
                        var asms = cad.GetAssemblies();
                        var si = AppDomain.CurrentDomain.SetupInformation;

                        var ads = new AppDomainSetup()
                        {
                            ApplicationBase = si.ApplicationBase,

                            ConfigurationFile = si.ConfigurationFile,

                            TargetFrameworkName = si.TargetFrameworkName,

                            DisallowApplicationBaseProbing = si.DisallowApplicationBaseProbing,
                            PrivateBinPath = si.PrivateBinPath,
                            PrivateBinPathProbe = si.PrivateBinPathProbe,
                        };

                        var perms = new PermissionSet(PermissionState.Unrestricted);
                        //perms.AddPermission(new SecurityPermission(PermissionState.Unrestricted));
                        //perms.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));

                        //var dirs = new[] {
                        //    @"%systemroot%\assembly",
                        //    @"%windir%\Microsoft.NET",
                        //    Path.GetDirectoryName( GetType().Assembly.Location)
                        //};

                        //foreach (var d in dirs)
                        //{
                        //    perms.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, Environment.ExpandEnvironmentVariables(d)));
                        //}

                        _SandboxDomain = AppDomain.CreateDomain
                            (
                                GetType().FullName + "@" + GetHashCode(),
                                new Evidence() { },
                                ads,
                                perms,
                                asms.Where(a => !a.IsDynamic).Select(a => a.Evidence.GetHostEvidence<StrongName>()).OfType<StrongName>().ToArray()
                            );
                    }
                }
                return _SandboxDomain;
            }
        }

        public Task<CSharpSandboxResult> ExecuteAsync(CSharpSandboxParameter parameter)
            => Task.Run(() =>
            {
                var ph = Activator.CreateInstanceFrom(
                                    SandboxDomain,
                                    typeof(CSharpRemoteSandbox).Assembly.ManifestModule.FullyQualifiedName,
                                    typeof(CSharpSandboxParameter).FullName);

                var p = (CSharpSandboxParameter)ph.Unwrap();

                p.Code = parameter.Code;
                p.ResetState = parameter.ResetState;
                p.UsesSeparateContext = parameter.UsesSeparateContext;
                p.ReturnsRawValue = parameter.ReturnsRawValue;
                p.ReturnsNamespaces = parameter.ReturnsNamespaces;
                p.ReturnsSourceCode = parameter.ReturnsSourceCode;
                p.ReturnsVariables = parameter.ReturnsVariables;
                if (parameter.HasAssembly)
                {
                    foreach (var a in parameter.Assemblies)
                    {
                        p.Assemblies.Add(a);
                    }
                }
                if (parameter.HasNamespace)
                {
                    foreach (var a in parameter.Namespaces)
                    {
                        p.Namespaces.Add(a);
                    }
                }

                var r = p.Execute();

                return r;
            });


        #region IDisposable メソッド

        /// <summary>
        /// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region デストラクタ

        /// <summary>
        /// オブジェクトがガベジ コレクションにより収集される前に、そのオブジェクトがリソースを解放し、その他のクリーンアップ操作を実行できるようにします。
        /// </summary>
        ~CSharpSandbox()
        {
            Dispose(false);
        }

        #endregion

        #region 仮想メソッド

        /// <summary>
        /// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
        /// </summary>
        /// <param name="disposing">メソッドが<see cref="CSharpSandbox.Dispose()" />から呼び出された場合は<c>true</c>。その他の場合は<c>false</c>。</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO:マネージ リソースの解放処理をこの位置に記述します。
            }
            if (_SandboxDomain != null)
            {
                try
                {
                    AppDomain.Unload(_SandboxDomain);
                }
                catch { }
                _SandboxDomain = null;
            }
        }

        #endregion

    }
}