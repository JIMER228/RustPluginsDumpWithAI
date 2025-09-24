using System.ComponentModel;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Config", "Kill me", "0.0.1")]
    public  class FixNick : RustPlugin
    {
        [DefaultValue(30)]
        [JsonProperty("characterLimit")]
        public int CharsLimit { get; set; } = 30;

        private bool? CanClientLogin(Network.Connection connection)
        {
            if (connection.username.Length > CharsLimit)
            {
                ConnectionAuth.Reject(connection, "хуесос");
                connection.rejected = true;
            }
            return null;
        }
    }
}