using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Plugins;

namespace Oxide.Ext.Rocket.WebSockets.Events
{
    /// <summary>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InitEvent
    {
        /// <summary>
        /// </summary>
        [JsonProperty("v")]
        public int Version { get; private set; }

        /// <summary>
        /// </summary>
        //[JsonProperty("user")]
        //public User User { get; set; }
    }
}