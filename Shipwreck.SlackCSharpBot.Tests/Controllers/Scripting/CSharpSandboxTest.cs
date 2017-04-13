using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    [TestClass]
    public class CSharpSandboxTest
    {
        [TestMethod]
        public void ExecuteTest()
        {
            using (var sb = new CSharpSandbox())
            {

                var t = sb.ExecuteAsync(new CSharpSandboxParameter()
                {
                    ReturnsRawValue = true,
                    Code = "1 + 2"
                });
                t.ConfigureAwait(false);
                t.Wait();

                Assert.AreEqual(null, t.Result.Exception);
                Assert.AreEqual("3", t.Result.ReturnValue);
            }
        }
    }
}
