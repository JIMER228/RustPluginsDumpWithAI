using System;
using System.Collections.Generic;
using Network;
using Oxide.Core;
using Newtonsoft.Json.Linq;

//	Author info:
//   * Lomarine
//   * https://vk.com/id41504602

namespace Oxide.Plugins
{
    [Info("VPN,Proxy,ISP blocker", "Lomarine", "1.0.0")]
    class VpnBlocker : RustPlugin
    {
       private Dictionary<string,MainClass> Connections = new Dictionary<string, MainClass>();
       private string Ip;

        class MainClass
        {
            public bool VPN;
            public int Connects;
        }

        void Init()
        {
            LoadData();
            permission.RegisterPermission("vpnblocker.bypass", this);
        }
        
        void OnClientAuth(Connection connection)
        {
            if (permission.UserHasPermission(connection.userid.ToString(), "vpnblocker.bypass"))
            {
                return;
            }
            
            Ip = connection.ipaddress.Split(':')[0];

            if (Connections.ContainsKey(Ip))
            {
                if(Connections[Ip].VPN)
                {
                    ConnectionAuth.Reject(connection, "BANNED - VPN");
                }
                else
                {
                    if (Connections[Ip].Connects >= 10)
                    {
                        VPNCheck(Ip, connection);
                        Connections[Ip].Connects = 0;
                    }
                    else
                    {
                        Connections[Ip].Connects += 1;
                    }
                }
                
            }
            else
            {
                VPNCheck(Ip, connection);
            }
        }

        private void VPNCheck(string Ip, Connection connection)
        {
            if(!Connections.ContainsKey(Ip))
                Connections.Add(Ip, new  MainClass());
            
            webrequest.EnqueueGet($"http://v2.api.iphub.info/ip/" + Ip,(code, response) => IpHub(code, response, connection), this, new Dictionary<string, string> { { "X-Key", "NzQwNDpXRXFYOWw0WVkyMW1QZTVPVUF0SXVMUm1uWHhjSkhxSQ==" } });
        }

        private void IpHub(int code, string response, Connection connect)
        {
            Ip = connect.ipaddress.Split(':')[0];
            if (response == null)
            {
                ConnectionAuth.Reject(connect, "Нет ответа от сервера [1]!");
                return;
            }
            JObject res = JObject.Parse(response);
            if (Convert.ToInt32(res["block"]) > 0)
            {
                ConnectionAuth.Reject(connect, "BANNED - VPN");
                Connections[Ip].VPN = true;
            }
            else
            {
                webrequest.EnqueueGet($"http://check.getipintel.net/check.php?ip=" + Ip + "&contact=Oceangeorge1998@gmail.com&flags=m",(codes, responses) => GetIpIntel(codes, responses, connect), this);
            }
        }

        private void GetIpIntel(int codes, string responses, Connection connect)
        {
            Ip = connect.ipaddress.Split(':')[0];
            
            if (responses == null)
            {
                ConnectionAuth.Reject(connect, "Нет ответа от сервера [2]!");
                return;
            }

            if (Convert.ToDouble(responses) >= 0.95)
            {
                ConnectionAuth.Reject(connect, "BANNED - VPN");
                Connections[Ip].VPN = true;
            }
            else
            {
                Connections[Ip].VPN = false;
            }
        }
        
        #region Data
		
        private void LoadData() => Connections = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, MainClass>>("VPN");
		
        void OnServerSave() => SaveData();
		
        void Unload() => SaveData();
		
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("VPN", Connections);
		
        #endregion
    }
}