using Microsoft.Bot.Connector.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Configuration;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class CustomBotAuthentication : BotAuthentication
    {
        private static string[] _Ids;
        private static string[] _Secrets;

        public CustomBotAuthentication(string appId = null, string appSecret = null)
            : base(appId, appSecret)
        {
        }

        protected override Task<bool> OnAuthorizeUser(BasicAuthIdentity identity, HttpActionContext actionContext)
            => Task.Run(() =>
            {
                if (_Ids == null)
                {
                    var sd = ConfigurationManager.AppSettings;
                    var ids = sd["AppId"].Split(';') ?? new string[0];
                    var secrets = sd["AppSecret"]?.Split(';') ?? new string[0];
                    if (ids.Length == secrets.Length)
                    {
                        _Secrets = secrets.Select(_ => _.Trim()).ToArray();
                        _Ids = ids.Select(_ => _.Trim()).ToArray();
                    }
                    else if (secrets.Length == 1)
                    {
                        _Secrets = Enumerable.Repeat(secrets[0].Trim(), ids.Length).ToArray();
                        _Ids = ids.Select(_ => _.Trim()).ToArray();
                    }
                    else
                    {
                        var min = Math.Min(ids.Length, secrets.Length);
                        _Secrets = secrets.Take(min).Select(_ => _.Trim()).ToArray();
                        _Ids = ids.Take(min).Select(_ => _.Trim()).ToArray();
                    }
                }

                for (var i = 0; i < _Ids.Length; i++)
                {
                    if (identity?.Id == _Ids[i] && identity?.Password == _Secrets[i])
                    {
                        return true;
                    }
                }
                return false;

            });
    }
}