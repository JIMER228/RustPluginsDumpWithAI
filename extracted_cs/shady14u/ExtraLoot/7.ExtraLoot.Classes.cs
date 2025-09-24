using Newtonsoft.Json;

namespace Oxide.Plugins
{
    //Define:FileOrder=80
    public partial class ExtraLoot
    {
        public class BaseItem
        {
            [JsonProperty(PropertyName = "1. ShortName")]
            public string ShortName;

            [JsonProperty(PropertyName = "2. Chance")]
            public float Chance;

            [JsonProperty(PropertyName = "3. Minimal amount")]
            public int AmountMin;

            [JsonProperty(PropertyName = "4. Maximal Amount")]
            public int AmountMax;

            [JsonProperty(PropertyName = "5. Skin Id")]
            public ulong SkinId;

            [JsonProperty(PropertyName = "6. Display Name")]
            public string DisplayName;

            [JsonProperty(PropertyName = "7. Blueprint")]
            public bool IsBlueprint;
        }
    }
}
