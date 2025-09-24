using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Firearm Modifier", "Khan", "1.1.3")]
    [Description("Allows you to change Magazine Size + Weapon Condition levels")]
    public class FirearmModifier : CovalencePlugin
    {
        private const string Use = "firearmmodifier.use";
        private const string Vip = "firearmmodifier.vip";
        private const string Admin = "firearmmodifier.admin";

        private PluginConfig _config;

        private List<string> Exclude = new List<string>
        {
            "bow_hunting.entity",
            "compound_bow.entity",
            "crossbow.entity"
        };

        private class PluginConfig
        {
            [JsonProperty("Weapon Options")]
            public Dictionary<string, WeaponOption> WeaponOptions = new Dictionary<string, WeaponOption>();

            public string ToJson() => JsonConvert.SerializeObject(this);
            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class WeaponOption
        {
            public int MagazineSize;
            public float ItemCondition;
            public int MagazineSizeVip;
            public float ItemConditionVip;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"InvalidSelection", "You've selected an invalid weapon to modify please type a valid weapon shortname" },
                {"Syntax", "Invalid Params please do /modify shortname magazinesize amount"},
                {"NoPerm", "Unkown Command: modify"},
                {"Success", "You've successfully set {0} to {1}"},
                {"SuccessVip", "You've successfully set {0} to {1}"},
                {"DoublePerms", "Error Double Perms, Defaults applied, Contact admin to fix"},
                {"DoublePermsReload", "You have both firearmmodifier.use & firearmmodifier.vip please contact Admin, Rust defaults applied" }
            }, this);
        }
        private string GetMessage(string key, string userId = null, params object[] args) => string.Format(lang.GetMessage(key, this, userId), args);

        private void Init()
        {
            permission.RegisterPermission(Use, this);
            permission.RegisterPermission(Admin, this);
            permission.RegisterPermission(Vip, this);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }

                if (!_config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning($"Configuration file {Name}.json was Updated");
                    SaveConfig();
                }

            }
            catch
            {
                PrintError("Configuration file is corrupt! Loading Default Config");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        protected override void LoadDefaultConfig() => _config = new PluginConfig();

        private void Unload()
        {
            _config = null;
        }

        private void OnServerInitialized()
        {
            foreach (var itemDefinition in ItemManager.itemList)
            {
                if (itemDefinition == null) continue;

                ItemModEntity itemModEntity = itemDefinition.GetComponent<ItemModEntity>();
                if (itemModEntity == null) continue;

                BaseProjectile baseProjectile = itemModEntity.entityPrefab?.Get()?.GetComponent<BaseProjectile>();
                if (baseProjectile ==null) continue;

                if (Exclude.Contains(baseProjectile.ShortPrefabName)) continue;

                if (_config.WeaponOptions.ContainsKey(baseProjectile.ShortPrefabName)) continue;

                _config.WeaponOptions.Add(baseProjectile.ShortPrefabName, new WeaponOption
                {
                    MagazineSize = baseProjectile.primaryMagazine.definition.builtInSize,
                    ItemCondition = itemDefinition.condition.max,
                    MagazineSizeVip = baseProjectile.primaryMagazine.definition.builtInSize,
                    ItemConditionVip = itemDefinition.condition.max
                });
            }
            SaveConfig();
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            BaseProjectile projectile = item?.GetHeldEntity() as BaseProjectile;
            if (projectile == null || !_config.WeaponOptions.ContainsKey(projectile.ShortPrefabName)) return;

            if (permission.UserHasPermission(task.owner.UserIDString, Use) && permission.UserHasPermission(task.owner.UserIDString, Vip))
            {
                BasePlayer.FindByID(task.owner.userID).ChatMessage(GetMessage("DoublePerms"));
                return;
            }
            
            WeaponOption weaponOptions;

            if (permission.UserHasPermission(task.owner.UserIDString, Use) && !permission.UserHasPermission(task.owner.UserIDString, Vip))
            {
                weaponOptions = _config.WeaponOptions[projectile.ShortPrefabName];
                item._maxCondition = weaponOptions.ItemCondition;
                item.condition = weaponOptions.ItemCondition;
                projectile.SendNetworkUpdateImmediate();
            }
            else if (permission.UserHasPermission(task.owner.UserIDString, Vip) && !permission.UserHasPermission(task.owner.UserIDString, Use))
            {
                weaponOptions = _config.WeaponOptions[projectile.ShortPrefabName];
                item._maxCondition = weaponOptions.ItemConditionVip;
                item.condition = weaponOptions.ItemConditionVip;
                projectile.SendNetworkUpdateImmediate();
            }

        }

        private void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (projectile == null || !_config.WeaponOptions.ContainsKey(projectile.ShortPrefabName)) return;

            if (permission.UserHasPermission(player.UserIDString, Use) && permission.UserHasPermission(player.UserIDString, Vip))
            {
                player.ChatMessage(GetMessage("DoublePermsReload"));
                return;
            }

            WeaponOption weaponOptions;

            if (permission.UserHasPermission(player.UserIDString, Use) && !permission.UserHasPermission(player.UserIDString, Vip))
            {
                weaponOptions = _config.WeaponOptions[projectile.ShortPrefabName];
                projectile.primaryMagazine.definition.builtInSize = weaponOptions.MagazineSize;
                projectile.primaryMagazine.capacity = weaponOptions.MagazineSize;
                projectile.SendNetworkUpdate();
            }
            else if (permission.UserHasPermission(player.UserIDString, Vip) && !permission.UserHasPermission(player.UserIDString, Use))
            {
                weaponOptions = _config.WeaponOptions[projectile.ShortPrefabName];
                projectile.primaryMagazine.definition.builtInSize = weaponOptions.MagazineSizeVip;
                projectile.primaryMagazine.capacity = weaponOptions.MagazineSizeVip;
                projectile.SendNetworkUpdate();
            }
        }

        [Command("modify")]
        private void Cmdmodify(IPlayer player, string command, string[] args)
        {

            if (!player.HasPermission("firearmmodifier.admin"))
            {
                player.Message(GetMessage("NoPerm", player.Id));
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(GetMessage("Syntax", player.Id));
                return;
            }
            WeaponOption weaponoption;
            if (!_config.WeaponOptions.TryGetValue(args[0].ToLower(), out weaponoption))
            {
                player.Reply(GetMessage("InvalidSelection", player.Id));
                return;
            }
            switch (args[1].ToLower())
            {
                case "magazinesize":
                    weaponoption.MagazineSize = int.Parse(args[2]);
                    player.Reply(GetMessage("Success", player.Id, "magazinesize", weaponoption.MagazineSize));
                    SaveConfig();
                    break;
                case "magazinesizevip":
                    weaponoption.MagazineSizeVip = int.Parse(args[2]);
                    player.Reply(GetMessage("SuccessVip", player.Id, "magazinesizevip", weaponoption.MagazineSizeVip));
                    SaveConfig();
                    break;
                case "itemcondition":
                    weaponoption.ItemCondition = int.Parse(args[2]);
                    player.Reply(GetMessage("Success", player.Id, "itemcondition", weaponoption.ItemCondition));
                    SaveConfig();
                    break;
                case "itemconditionvip":
                    weaponoption.ItemConditionVip = int.Parse(args[2]);
                    player.Reply(GetMessage("SuccessVip", player.Id, "itemconditionvip", weaponoption.ItemConditionVip));
                    SaveConfig();
                    break;
                default:
                    player.Message(GetMessage("Syntax", player.Id));
                    break;
            }

        }
    }
}