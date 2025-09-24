using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries;
using System.Reflection;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("CloudflareUpdater", "PsychoTea", "1.0.0")]

    class CloudflareUpdater : RustPlugin
    {
        Timer _timer;

        const float timeout = 200f;
        Dictionary<string, string> header;

        Library lib;
        MethodInfo putRequest;

        class IDRequest
        {
            [JsonProperty("result")]
            public ResultObj[] Result { get; set; }

            public class ResultObj
            {
                [JsonProperty("id")]
                public string ID { get; set; }
            }
        }

        #region Oxide Hooks

        void Init()
        {
            header = new Dictionary<string, string>()
            {
                { "X-Auth-Email", GetConfig<string>("Login Email") },
                { "X-Auth-Key", GetConfig<string>("API Key") }
            };

            _timer = timer.Repeat(GetConfig<int>("Seconds Between Updates"), 0, () => UpdateIPs());
            UpdateIPs();
        }

        void OnServerInitialized()
        {
            lib = Interface.Oxide.GetLibrary<Library>("PutRequest");
            putRequest = lib.GetFunction("PutRequest");
        }

        void Unload()
        {
            _timer.Destroy();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Generating new config...");

            Config["Seconds Between Updates"] = 300;
            Config["API Key"] = "<Insert API key here>";
            Config["Login Email"] = "me@example.org";
            Config["Zone Name"] = "example.org";
            Config["Domain Names - Use Proxy"] = new Dictionary<string, bool> {
                { "play.example.org", false },
                { "website.example.org", true }
            };

            Puts("New config file generated.");
        }

        #endregion

        void UpdateIPs()
        {
            foreach (var domainName in GetConfig<Dictionary<string, object>>("Domain Names - Use Proxy").Keys)
                UpdateIP(domainName);
        }

        void UpdateIP(string domainName)
        {
            Puts($"Updating {domainName}...");
            DoUpdate(domainName);
        }

        void DoUpdate(string domainName, string zoneId = null, string recordId = null, string newIp = null)
        {
            if (zoneId == null)
            {
                GetZoneID(domainName);
                return;
            }

            if (recordId == null)
            {
                GetRecordID(domainName, zoneId);
                return;
            }

            if (newIp == null)
            {
                GetIP(domainName, zoneId, recordId);
                return;
            }

            if (UpdateIPTo(domainName, zoneId, recordId, newIp))
                Puts($"Updated {domainName} successfully.");
        }

        #region Web Requests

        void GetZoneID(string domainName)
        {
            webrequest.EnqueueGet($"https://api.cloudflare.com/client/v4/zones?name=sparkes.tech", (zoneCode, zoneResponse) =>
            {
                if (zoneCode == 200 && zoneResponse != null)
                {
                    var zoneIdResponse = JsonConvert.DeserializeObject<IDRequest>(zoneResponse);
                    string zoneIdentifier = zoneIdResponse.Result[0].ID;
                    DoUpdate(domainName, zoneIdentifier);
                }
                else
                {
                    Puts($"Failed to get zone ID for domain {domainName}. Code: {zoneCode}\nRetrying in 3s...");
                    timer.Once(3f, () => UpdateIP(domainName));
                }

            }, this, header, timeout);
        }

        void GetRecordID(string domainName, string zoneID)
        {
            webrequest.EnqueueGet($"https://api.cloudflare.com/client/v4/zones/{zoneID}/dns_records?name={domainName}&type=A", (code, response) =>
            {
                if (code == 200 && response != null)
                {
                    var recordIdResponse = JsonConvert.DeserializeObject<IDRequest>(response);
                    string recordIdentifier = recordIdResponse.Result[0].ID;
                    DoUpdate(domainName, zoneID, recordIdentifier);
                }
                else
                {
                    Puts($"Failed to get record ID for domain {domainName}. Code: {code}\nRetrying in 3s...");
                    timer.Once(3f, () => DoUpdate(domainName, zoneID));
                    return;
                }

            }, this, header, timeout);
        }

        void GetIP(string domainName, string zoneID, string recordID)
        {
            webrequest.EnqueueGet($"http://canihazip.com/s", (code, response) =>
            {
                if (code == 200 && response != null)
                {
                    DoUpdate(domainName, zoneID, recordID, response);
                }
                else
                {
                    Puts($"Failed to get IP. Code: {code}\nRetrying in 3s...");
                    timer.Once(3f, () => DoUpdate(domainName, zoneID, recordID));
                    return;
                }

            }, this, null, timeout);
        }

        bool UpdateIPTo(string domainName, string zoneID, string recordID, string newIP)
        {
            string url = $"https://api.cloudflare.com/client/v4/zones/{zoneID}/dns_records/{recordID}";

            Dictionary<string, object> data = new Dictionary<string, object>()
            {
                { "type", "A" },
                { "name", domainName },
                { "content", newIP },
                { "proxied", UseProxy(domainName) }
            };
            
            string result = putRequest.Invoke(lib, new object[] { url, header, data }).ToString();
            return (result != null && result.Contains("\"success\":true"));
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string key) => (T)Config[key];

        bool UseProxy(string domainName) => (bool)GetConfig<Dictionary<string, object>>("Domain Names - Use Proxy")[domainName];

        #endregion
    }
}
