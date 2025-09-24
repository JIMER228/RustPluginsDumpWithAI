using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("SkinChanger", "Speedy2M", "0.2.0", ResourceId = 000)]

    /// +-------------------------------------------------------------------+
    /// |                          PRIVATE PLUGIN                           |
    /// |               Rewritten and Developed by Speedy2M                 |
    /// |                 Credits:  uplusion23 &  k1lly0u                   |
    /// |                  Not intended for public release                  |
    /// +-------------------------------------------------------------------+
    class SkinChanger : RustPlugin
    {
        string iconProfile = "76561198220239093";
        string preFix = "<color=white>[<color=#ce422c>Skin Changer</color>]</color>";


        private Dictionary<ulong, bool> activePlayers = new Dictionary<ulong, bool>();
        private Dictionary<int, Dictionary<int, int>> itemSkins = new Dictionary<int, Dictionary<int, int>>();


        #region oxide hooks
        ////////////////////////////////////////
        /// Hook Related
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("skinchanger.use", this);
        }

        void OnServerInitialized()
        {
            FillItemList();
        }

        private void FillItemList()
        {
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                if (HasSkins(item))
                {
                    itemSkins.Add(item.itemid, new Dictionary<int, int>());
                    FillSkinList(item);
                }
            }
        }

        private void FillSkinList(ItemDefinition item)
        {
            if (itemSkins.ContainsKey(item.itemid))
            {
                int i = 0;
                var skins = ItemSkinDirectory.ForItem(item).ToList();
                itemSkins[item.itemid].Add(i, 0);
                i++;
                foreach (var entry in skins) { itemSkins[item.itemid].Add(i, entry.id); i++; }
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (hasPerms(player))
                if (activePlayers.ContainsKey(player.userID))
                    if (activePlayers[player.userID])
                        if (input.WasJustPressed(BUTTON.USE))
                        {
                            var entity = player.GetActiveItem();
                            ProcessItem(player, entity);
                        }
        }
        #endregion

        #region methods
        ////////////////////////////////////////
        /// Methods Related
        ////////////////////////////////////////

        private void ProcessItem(BasePlayer player, Item item)
        {
            if (item != null)
                if (itemSkins.ContainsKey(item.info.itemid))
            {

                var definition = ItemManager.FindItemDefinition(item.info.shortname);
                    if (HasSkins(definition))
                    {
                        int nextSkinID = 0;
                        int currentSkin = (int)item.skin;
                        int currentSkinKey = itemSkins[item.info.itemid].FirstOrDefault(x => x.Value == currentSkin).Key;
                        if (itemSkins[item.info.itemid].ContainsKey(currentSkinKey + 1))
                            nextSkinID = itemSkins[item.info.itemid][currentSkinKey + 1];

                        if (item.info.category.ToString() == "Weapon")
                        {
                            int ammoCount = 0;
                            string ammoType = "";
                            List<string> mods = new List<string>();

                            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                            if (weapon != null)
                            {
                                if (weapon.primaryMagazine != null)
                                {
                                    ammoType = weapon.primaryMagazine.ammoType.shortname;
                                    ammoCount = weapon.primaryMagazine.contents;
                                    if (item.contents != null)
                                        foreach (var mod in item.contents.itemList)
                                        {
                                            if (mod.info.itemid != 0)
                                                mods.Add(mod.info.shortname);
                                        }
                                }
                                Item newItem = BuildWeapon(definition, mods, ammoType, ammoCount, nextSkinID, item.condition);
                                item.RemoveFromContainer();
                                player.SendNetworkUpdateImmediate();
                                    if (!newItem.MoveToContainer(player.inventory.containerBelt))
                                    {
                                        if (!newItem.MoveToContainer(player.inventory.containerMain))
                                        {
                                            newItem.Drop(player.GetDropPosition(), player.GetDropVelocity(), player.transform.rotation);
                                        }
                                    }
                            }
                        }
                        else item.skin = (ulong)nextSkinID;
                        rust.SendChatMessage(player, preFix,"<color=#ce422c>SkinChanger: </color> Changed skin for item: <color=#ce422c>" + item.info.displayName.english + "</color>", iconProfile);
                        player.SendNetworkUpdateImmediate(false);
                }
            }
        }
        private bool HasSkins(ItemDefinition item)
        {
            if (item != null)
            {
                var skins = ItemSkinDirectory.ForItem(item).ToList();
                if (skins.Count > 0)
                    return true;
            }
            return false;
        }
        #endregion

        #region itembuilder
        ////////////////////////////////////////
        /// Itembuilder Related
        ////////////////////////////////////////
        private Item BuildItem(string shortname, object skin)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                if (skin == null) skin = 0;
                Item item = ItemManager.CreateByItemID(definition.itemid, 1, (ulong)skin);
                return item;
            }
            return null;
        }
        private Item BuildWeapon(ItemDefinition item, List<string> mods, string ammotype, int ammoAmount, int skin, float condition)
        {
            var ammo = ItemManager.FindItemDefinition(ammotype);
            if (item != null)
            {
                Item Gun = ItemManager.CreateByItemID(item.itemid, 1, (ulong)skin);
                Gun.condition = condition;

                var weapon = Gun.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (weapon.primaryMagazine != null)
                    {
                        if (ammo != null) (Gun.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ammo;
                        (Gun.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = ammoAmount;

                        if (Gun.contents != null)
                            if (mods != null)
                                foreach (string mod in mods) Gun.contents.AddItem(BuildItem(mod, 1).info, 1);
                    }
                }

                return Gun;
            }
            return null;
        }
        #endregion

        #region chat console commands
        ////////////////////////////////////////
        /// Command Related
        ////////////////////////////////////////
        [ChatCommand("sc")]
        private void cmdSC(BasePlayer player, string command, string[] args)
        {
            if (!hasPerms(player)) return;
            if (args != null)
                if (args.Length == 1)
                    if (args[0].ToLower() == "help")
                    {
                        rust.SendChatMessage(player, preFix, "<color=#ce422c>Skin Changer Help Menu</color>", iconProfile);
                        rust.SendChatMessage(player, preFix, "Activate the function using <color=#ce422c>/sc</color>", iconProfile);
                        rust.SendChatMessage(player, preFix, "Place the item you want to change in your hotbar", iconProfile);
                        rust.SendChatMessage(player, preFix, "Put the item in your hands and press the USE button (def. <color=#ce422c>E</color>)", iconProfile);
                        return;
                    }
            if (!activePlayers.ContainsKey(player.userID))
                activePlayers.Add(player.userID, false);
            if (activePlayers[player.userID] == false)
            {
                activePlayers[player.userID] = true;
                rust.SendChatMessage(player, preFix, "You have <color=#ce422c>activated </color>Skin Changer!", iconProfile);
                rust.SendChatMessage(player, preFix, "For help type <color=#ce422c>/sc help </color>", iconProfile);
                addUI(player);
                return;

            }
            else
            {
                activePlayers[player.userID] = false;
                rust.SendChatMessage(player, preFix, "You have <color=#ce422c>de-activated</color> Skin Changer!", iconProfile);
                DestroyGUI(player, "HelpGUI");
                return;
            }

        }
        private bool hasPerms(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skinchanger.use")) return false;
            return true;
        }
        #endregion

        ////////////////////////////////////////
        /// GUI Related
        ////////////////////////////////////////

        private void addUI(BasePlayer player)
        {
          CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json, null, null, null, null));
        }

        void DestroyGUI(BasePlayer player, string GUIName) { Game.Rust.Cui.CuiHelper.DestroyUi(player, GUIName); }

        const string json = @"
        [{
          ""name"":   ""HelpGUI"",
          ""parent"": ""Overlay"",
          ""components"":
          [
              {
                  ""type"":""UnityEngine.UI.Text"",
                  ""text"":""Press <color=#ce422c>E</color> to switch skins on the selected item in your hotbar."",
                  ""fontSize"":18,
                  ""align"": ""MiddleCenter""
              },
              {
                  ""type"":""RectTransform"",
                  ""anchormin"": ""0 0.8"",
                  ""anchormax"": ""1 0.9""
              }
          ]
      }]";
        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
             SendReply(player, "<size=18>SkinChanger</size> by <color=#ce422b>Speedy2M</color>\n<color=\"#ffd479\">/sc help</color> - To display the SkinChanger HelpText.");
        }
    }
}
