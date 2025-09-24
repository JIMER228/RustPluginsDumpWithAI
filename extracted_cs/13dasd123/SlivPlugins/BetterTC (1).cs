// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6

using System;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BetterTC", "ninco90", "1.2.3")]
    internal class BetterTC : RustPlugin
    {
        #region Fields
        [PluginReference] private Plugin ImageLibrary, NoEscape, TiersMode, Notify, UINotify, TCLevels;

        private const string apiUrl = "https://app.rustspain.com/facepunch/bettertc.json";
        private const string fxnoresources = "assets/bundled/prefabs/fx/ore_break.prefab";
        private const string fxfinish = "assets/prefabs/deployable/research table/effects/research-success.prefab";
        private const string fxspray = "assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab";
        private const string fxreskin = "assets/prefabs/tools/spraycan/reskineffect.prefab";
        private const string fxerror = "assets/prefabs/weapons/toolgun/effects/repairerror.prefab";

        private const string permadmin = "bettertc.admin";
        private const string permupgrade = "bettertc.upgrade";
        private const string permupgradenocost = "bettertc.upgrade.nocost";
        private const string permrepair = "bettertc.repair";
        private const string permrepairnocost = "bettertc.repair.nocost";
        private const string permreskin = "bettertc.reskin";
        private const string permreskinnocost = "bettertc.reskin.nocost";
        private const string permlist = "bettertc.authlist";
        private const string permdelauth = "bettertc.deleteauth";
        private const string permupskin = "bettertc.upskin";

        private const string upgrade_0 = "upgrade.base";
        private const string upgrade_1 = "upgrade.windows";
        private const string upgrade_2 = "upgrade.item";
        private const string upgrade_3 = "upgrade.subitem";
        private const string buttons_0 = "buttons.cup";
        private const string color_0 = "color.base";
        private const string color_1 = "color.windows";
        private const string color_2 = "color.item";
        private const string color_3 = "color.subitem";
        private const string authlist_0 = "authlist.base";
        private const string authlist_1 = "authlist.windows";
        private const string authlist_2 = "authlist.item";
        private const string authlist_3 = "authlist.subitem";

        private Dictionary<BuildingPrivlidge, TCConfig> BuildingCupboard = new Dictionary<BuildingPrivlidge, TCConfig>();
        private int maxGradeTier;
        #endregion
        
        #region Hooks
        private void OnServerInitialized(){
            if (!permission.PermissionExists(permadmin, this)) permission.RegisterPermission(permadmin, this);
            if (!permission.PermissionExists(permupgrade, this)) permission.RegisterPermission(permupgrade, this);
            if (!permission.PermissionExists(permupgradenocost, this)) permission.RegisterPermission(permupgradenocost, this);
            if (!permission.PermissionExists(permrepair, this)) permission.RegisterPermission(permrepair, this);
            if (!permission.PermissionExists(permrepairnocost, this)) permission.RegisterPermission(permrepairnocost, this);
            if (!permission.PermissionExists(permreskin, this)) permission.RegisterPermission(permreskin, this);
            if (!permission.PermissionExists(permreskinnocost, this)) permission.RegisterPermission(permreskinnocost, this);
            if (!permission.PermissionExists(permlist, this)) permission.RegisterPermission(permlist, this);
            if (!permission.PermissionExists(permdelauth, this)) permission.RegisterPermission(permdelauth, this);
            if (!permission.PermissionExists(permupskin, this)) permission.RegisterPermission(permupskin, this);

            foreach (var check in config.FrequencyUpgrade){
                if (!permission.PermissionExists(check.Key, this)) permission.RegisterPermission(check.Key, this);
            }

            Dictionary<string, string> imageListCraft = new Dictionary<string, string>();
            foreach (var recipe in config.itemsList){
                if (!permission.PermissionExists(recipe.permission, this)) permission.RegisterPermission(recipe.permission, this);
                if (recipe.img != "" && !imageListCraft.ContainsKey(recipe.img)) imageListCraft.Add(recipe.img, recipe.img);
            }
            for (int i = 0; i <= 16; i++){
                imageListCraft.Add("color_" + i, "https://img.rustspain.com/bettertc/colours/" + i + ".png");
            }
            imageListCraft.Add("lock5", "https://img.rustspain.com/bettertc/lock5.png");
            imageListCraft.Add("spray", "https://img.rustspain.com/bettertc/spray.png");
            imageListCraft.Add("upgrade2", "https://img.rustspain.com/bettertc/upgrade2.png");
            imageListCraft.Add("hammerbt", "https://img.rustspain.com/bettertc/icon/hammer.png");
            imageListCraft.Add("cupboard.toolbt", "https://img.rustspain.com/bettertc/icon/cupboard.tool.png");
            imageListCraft.Add("metal.fragmentsbt", "https://img.rustspain.com/bettertc/icon/metal.fragments.png");
            imageListCraft.Add("stonesbt", "https://img.rustspain.com/bettertc/icon/stones.png");
            imageListCraft.Add("woodbt", "https://img.rustspain.com/bettertc/icon/wood.png");
            imageListCraft.Add("metal.refinedbt", "https://img.rustspain.com/bettertc/icon/metal.refined.png");

            ImageLibrary?.Call("ImportImageList", Title, imageListCraft, 0UL, true, null);
        }

        private void Unload(){
            foreach (var player in BasePlayer.activePlayerList){
            	CuiHelper.DestroyUi(player, buttons_0);
                CuiHelper.DestroyUi(player, upgrade_0);
                CuiHelper.DestroyUi(player, color_0);
                CuiHelper.DestroyUi(player, authlist_0);
            }

            foreach (var cup in BuildingCupboard){
                if (cup.Value.workupgrade != null) ServerMgr.Instance.StopCoroutine(cup.Value.workupgrade);
                if (cup.Value.workrepair != null) ServerMgr.Instance.StopCoroutine(cup.Value.workrepair);
                if (cup.Value.workreskin != null) ServerMgr.Instance.StopCoroutine(cup.Value.workreskin);
            }
        }

        private void OnLootEntity(BasePlayer player, BuildingPrivlidge cup){
            if (player == null || cup == null) return;
            if (!BuildingCupboard.ContainsKey(cup)){
                BuildingCupboard.Add(cup, new TCConfig(){
                    grade = BuildingGrade.Enum.Wood,
                    skinid = 0,
                    color = false,
                    colour = 0,
                    work = false,
                    repair = false,
                    reskin = false,
                    effect = true,
                    downgrade = false
                });
            }
            ShowButtonTC(player, cup);
        }
        
        private  void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity){
            if (player == null) return;
            CuiHelper.DestroyUi(player, buttons_0);
            CuiHelper.DestroyUi(player, upgrade_0);
        }
        #endregion

        #region Function
        private IEnumerator RepairProgress(BasePlayer player, BuildingPrivlidge cup){
            var building = cup.GetBuilding();
            yield return CoroutineEx.waitForSeconds(0.15f);
            var cd = Frequency(player.UserIDString, config.FrequencyRepair);
            var cost = ResourcesRepair(player.UserIDString);
            bool show = true;
            for (int index = 0; index < building.buildingBlocks.Count; index++){
                var entity = building.buildingBlocks[index];
                if (!BuildingCupboard[cup].repair){ show = false; break; }
                if (!RepairBlock(player, entity, cup, cost)) continue;
                yield return CoroutineEx.waitForSeconds(cd);
            }
            BuildingCupboard[cup].repair = false;
            if(show) CreateGameTip(cup, Languaje("RepairFinish", player.UserIDString), player, fxfinish, 10);
            yield return 0;
        }

        private IEnumerator UpdateProgress(BasePlayer player, BuildingPrivlidge cup){
            var set = cup.GetBuilding().buildingBlocks;
            yield return CoroutineEx.waitForSeconds(0.15f);
            var cd = Frequency(player.UserIDString, config.FrequencyUpgrade);
            bool show = true;
            for (var index = 0; index < set.Count; index++){
                var block = set[index];
                if (cup == null) yield break;
                if (!BuildingCupboard[cup].work){ show = false; break; }
                var grade = BuildingCupboard[cup].grade;
                if (Interface.CallHook("OnStructureUpgrade", block, player, grade) != null){
                    BuildingCupboard[cup].work = false;
                    ShowButtonTC(player, cup);
                    CreateGameTip(cup, Languaje("UpgradeBlock", player.UserIDString), player, fxnoresources, 10, "danger");
                    show = false;
                    break;
                }

                if (grade == block.grade) continue;
                if ((!config.downgrade || !BuildingCupboard[cup].downgrade || (config.onlyowner && player.userID != block.OwnerID)) && grade <= block.grade) continue;
                if ((config.onlyownerup && player.userID != block.OwnerID) && grade >= block.grade) continue;
                if (config.teamupdate && player.userID != block.OwnerID){
                    if(!CheckTeam(block.OwnerID, player.userID)) continue;
                }

                UpgradeBlock(cup, block, grade, player);
                yield return CoroutineEx.waitForSeconds(cd);
            }
            BuildingCupboard[cup].work = false;
            if(show) CreateGameTip(cup, Languaje("UpgradeFinish", player.UserIDString), player, fxfinish, 10);
            yield return 0;
        }

        private IEnumerator ReskinProgress(BasePlayer player, BuildingPrivlidge cup){
            var set = cup.GetBuilding().buildingBlocks;
            yield return CoroutineEx.waitForSeconds(0.15f);
            var cd = Frequency(player.UserIDString, config.FrequencyReskin);
            bool show = true;
            for (var index = 0; index < set.Count; index++){
                var block = set[index];
                if (cup == null) yield break;
                if(!BuildingCupboard[cup].reskin){ show = false; break; }
                var grade = BuildingCupboard[cup].grade;
                if (grade != block.grade) continue;
                if (Convert.ToUInt32(BuildingCupboard[cup].skinid) == block.skinID && BuildingCupboard[cup].colour == block.customColour) continue;
                ReskinBlock(cup, block, grade, player);
                yield return CoroutineEx.waitForSeconds(cd);
            }
            BuildingCupboard[cup].reskin = false;
            if(show) CreateGameTip(cup, Languaje("ReskinFinish", player.UserIDString), player, fxfinish, 10);
            yield return 0;
        }

        private bool RepairBlock(BasePlayer player, BaseCombatEntity entity, BuildingPrivlidge cup, float cost){
            if (entity == null || !entity.IsValid() || entity.IsDestroyed || entity.transform == null) return false;
            if (!entity.repair.enabled || entity.health == entity.MaxHealth()) return false;
            if (Interface.CallHook("OnStructureRepair", entity, player) != null) return false;

            var missingHealth = entity.MaxHealth() - entity.health;
            var healthPercentage = missingHealth / entity.MaxHealth();
            if (missingHealth <= 0f || healthPercentage <= 0f){
                entity.OnRepairFailed(null, string.Empty);
                return false;
            }

            var itemAmounts = entity.RepairCost(healthPercentage);
            if (itemAmounts.Sum(x => x.amount) <= 0f){
                entity.health += missingHealth;
                entity.SendNetworkUpdate();
                entity.OnRepairFinished();
                return true;
            }

            if (!HasPermission(player.UserIDString, permrepairnocost)){
                foreach (var amount in itemAmounts){
                    amount.amount *= cost;
                }

                if (itemAmounts.Any(ia => cup.inventory.GetAmount(ia.itemid, false) < (int)ia.amount)){
                    entity.OnRepairFailed(null, string.Empty);
                    CreateGameTip(cup, Languaje("NoResourcesRepair", player.UserIDString), player, fxnoresources, 10, "danger");
                    BuildingCupboard[cup].repair = false;
                    return false;
                }

                foreach (var amount in itemAmounts){
                    cup.inventory.Take(null, amount.itemid, (int)amount.amount);
                }
            }
            
            entity.health += missingHealth;
            entity.SendNetworkUpdate();
            if (entity.health < entity.MaxHealth()){
                entity.OnRepair();
            } else {
                entity.OnRepairFinished();
            }
            return true;
        }

        private void UpgradeBlock(BuildingPrivlidge cup, BuildingBlock block, BuildingGrade.Enum grade, BasePlayer player){
            if (!HasPermission(player.UserIDString, permupgradenocost) && !CanUpgrade(player, cup, block, grade)){
                BuildingCupboard[cup].work = false;
                CreateGameTip(cup, Languaje("NoResourcesUpgrade", player.UserIDString), player, fxnoresources, 10, "danger");
                return;
            }
            
            if (CheckBlock(block)) return;
            
            if (!HasPermission(player.UserIDString, permupgradenocost)){
                var list = block.blockDefinition.GetGrade(grade, 0).CostToBuild();
                for (var index = 0; index < list.Count; index++){
                    var check = list[index];
                    TakeResources(cup.inventory.itemList, check.itemDef.shortname, (int)check.amount);
                }
            }
            
            ulong skin = Convert.ToUInt32(BuildingCupboard[cup].skinid);
            block.skinID = skin;

            if (config.playfx && BuildingCupboard[cup].effect){
                var effect = "assets/bundled/prefabs/fx/build/promote_toptier.prefab";
                if(grade == BuildingGrade.Enum.Wood) {
                    effect = "assets/bundled/prefabs/fx/build/frame_place.prefab";
                    block.ClientRPC<int, ulong>(null, "DoUpgradeEffect", (int)BuildingGrade.Enum.Wood, skin);
                } else if(grade == BuildingGrade.Enum.Stone) {
                    effect = "assets/bundled/prefabs/fx/build/promote_stone.prefab";
                    block.ClientRPC<int, ulong>(null, "DoUpgradeEffect", (int)BuildingGrade.Enum.Stone, skin);
                } else if(grade == BuildingGrade.Enum.Metal) {
                    effect = "assets/bundled/prefabs/fx/build/promote_metal.prefab";
                    block.ClientRPC<int, ulong>(null, "DoUpgradeEffect", (int)BuildingGrade.Enum.Metal, skin);
                } else {
                    block.ClientRPC<int, ulong>(null, "DoUpgradeEffect", (int)BuildingGrade.Enum.TopTier, skin);
                }
                Effect.server.Run(effect, block.transform.position);
            }

            block.SetGrade(grade);
            block.UpdateSkin();
            block.SetHealthToMax();
            if(BuildingCupboard[cup].color) block.SetCustomColour(BuildingCupboard[cup].colour);
            block.SendNetworkUpdateImmediate();
        }

        private void ReskinBlock(BuildingPrivlidge cup, BuildingBlock block, BuildingGrade.Enum grade, BasePlayer player){
            if (!HasPermission(player.UserIDString, permreskinnocost) && !CanUpgrade(player, cup, block, grade)){
                BuildingCupboard[cup].reskin = false;
                CreateGameTip(cup, Languaje("NoResourcesReskin", player.UserIDString), player, fxnoresources, 10, "danger");
                return;
            }
            
            if (CheckBlock(block)) return;
            
            if (!HasPermission(player.UserIDString, permreskinnocost)){
                var list = block.blockDefinition.GetGrade(grade, 0).CostToBuild();
                for (var index = 0; index < list.Count; index++){
                    var check = list[index];
                    TakeResources(cup.inventory.itemList, check.itemDef.shortname, (int)check.amount);
                }
            }
            
            ulong skin = Convert.ToUInt32(BuildingCupboard[cup].skinid);
            block.skinID = skin;
            block.UpdateSkin();
            if(BuildingCupboard[cup].color) block.SetCustomColour(BuildingCupboard[cup].colour);
            block.SendNetworkUpdateImmediate();

            if (config.playfx && BuildingCupboard[cup].effect){
                Effect.server.Run(fxspray, block.transform.position);
                Effect.server.Run(fxreskin, block.transform.position);
            } 
        }
        
        private bool CanUpgrade(BasePlayer player, BuildingPrivlidge cup, BuildingBlock block, BuildingGrade.Enum grade){
            var list = block.blockDefinition.GetGrade(grade, 0).CostToBuild();
            for (var index = 0; index < list.Count; index++){
                ItemAmount itemAmount = list[index];
                if (cup.inventory.GetAmount(itemAmount.itemid, false) < (double) itemAmount.amount) return false;
            }
            return true;
        }

        private bool Unlock(int maxGradeTier, string requiredGrade){
            if (maxGradeTier == 1 && requiredGrade == "wood") return true;
            if (maxGradeTier == 2 && (requiredGrade == "wood" || requiredGrade == "stone")) return true;
            if (maxGradeTier == 3 && (requiredGrade == "wood" || requiredGrade == "stone" || requiredGrade == "metal")) return true;
            if (maxGradeTier == 4) return true;
            return false;
        }
        
        private static void TakeResources(IEnumerable<Item> itemList, string name, int takeitems){
            if (takeitems == 0) return;
            var list = Facepunch.Pool.GetList<Item>();
            var num1 = 0;
            foreach (var obj in itemList){
                if (obj.info.shortname != name) continue;
                var num2 = takeitems - num1;
                if (num2 <= 0) continue;
                if (obj.amount > num2){
                    obj.MarkDirty();
                    obj.amount -= num2;
                    break;
                }
                if (obj.amount <= num2){
                    num1 += obj.amount;
                    list.Add(obj);
                }
                if (num1 == takeitems) break;
            }

            foreach (var obj in list)
                obj.Remove();
            Facepunch.Pool.FreeList(ref list);
        }

        private bool CheckTeam(ulong owner, ulong player) => RelationshipManager.ServerInstance.FindPlayersTeam(owner)?.members?.Contains(player) ?? false;

        private bool CheckBlock(BuildingBlock block) =>  block.blockDefinition.checkVolumeOnUpgrade && DeployVolume.Check(block.transform.position, block.transform.rotation, PrefabAttribute.server.FindAll<DeployVolume>(block.prefabID), ~(1 << block.gameObject.layer));

		private List<ProtoBuf.PlayerNameID> GetAuthPlayers(BuildingPrivlidge cup){
            return cup.authorizedPlayers.Where(player => (HasPermission(player.userid.ToString(), permadmin) && config.adminshow == true) || !HasPermission(player.userid.ToString(), permadmin)).ToList();
        }
        
        private List<ItemInfo> GetBuildingItems(BasePlayer player){
            return config.itemsList.Where(kit => kit.enabled == true).ToList();
        }

        private float Frequency(string steamid, Dictionary<string, float> frequency){
            float c = 100.0f;
            foreach (var item in frequency){
                if (HasPermission(steamid, item.Key)) c = Math.Min(c, item.Value);
            }
            return c;
        }

        private float ResourcesRepair(string steamid){
            float r = 100.0f;
            foreach (var item in config.CostListRepair){
                if (HasPermission(steamid, item.Key)) r = Math.Min(r, item.Value);
            }
            return r;
        }

        private void GetNewItems(BasePlayer player){
            try {
                using (WebClient client = new WebClient()){
                    string json = client.DownloadString(apiUrl);
                    List<ItemInfo> newItemList = JsonConvert.DeserializeObject<List<ItemInfo>>(json);
                    if (newItemList != null){
                        Dictionary<string, string> imageListCraft = new Dictionary<string, string>();
                        bool configUpdated = false;
                        foreach (var newItem in newItemList){
                            if (newItem.ID > GetMaxItemId()){
                                var newItemInfo = new ItemInfo {
                                    ID = newItem.ID,
                                    enabled = newItem.enabled,
                                    name = newItem.name,
                                    grade = newItem.grade,
                                    img = newItem.img,
                                    skinid = newItem.skinid,
                                    color = newItem.color,
                                    permission = newItem.permission
                                };
                                if (newItem.img != "" && !imageListCraft.ContainsKey(newItem.img)) imageListCraft.Add(newItem.img, newItem.img);
                                config.itemsList.Add(newItemInfo);
                                configUpdated = true;
                            }
                        }
                        if (configUpdated){
                            ImageLibrary?.Call("ImportImageList", Title, imageListCraft, 0UL, true, null);
                            SaveConfig();
                            var soundEffect = new Effect(fxfinish, player.transform.position, Vector3.zero);
                            if (soundEffect == null) return;
                            EffectNetwork.Send(soundEffect, player.net.connection);
                            Puts("Updated configuration with new items.");
                        } else {
                            Puts("There are no new items.");
                        }
                    }
                }
            }
            catch (Exception ex){
                Puts("Error getting new items: " + ex.Message);
            }
        }

        int GetMaxItemId(){
            int maxId = 0;
            foreach (var item in config.itemsList){
                if (item.ID > maxId) maxId = item.ID;
            }
            return maxId;
        }
        
        private bool HasPermission(string userID, string perm){
            return string.IsNullOrEmpty(perm) || permission.UserHasPermission(userID, perm);
        }
        
        private string GetImageLibrary(string name, ulong skinid = 0){ 
            return ImageLibrary?.Call<string>("GetImage", name, skinid); 
        }
        
        private void CreateGameTip(BuildingPrivlidge cup, string text, BasePlayer player, string sound, float length = 10f, string red = ""){
            if (player == null) return;
            int type = config.notifyType["info"];
            if (red == "danger") type = config.notifyType["error"];
            if (cup != null){
                Effect.server.Run(sound, cup.transform.position);
                foreach (ProtoBuf.PlayerNameID pnid in cup.authorizedPlayers) {
                    BasePlayer foundPlayer = BasePlayer.Find(pnid.userid.ToString());
                    if(foundPlayer != null){
                        if(config.alertgametip){
                            if (red == "danger"){
                                foundPlayer.SendConsoleCommand($"gametip.showtoast {1} \"{text}\"  ");
                            } else {
                                foundPlayer.SendConsoleCommand("gametip.hidegametip");
                                foundPlayer.SendConsoleCommand("gametip.showgametip", text);
                                timer.Once(length, () => foundPlayer.SendConsoleCommand("gametip.hidegametip"));
                            }
                        }
                        if(config.alertnotify && (Notify != null || UINotify != null)) Interface.Oxide.CallHook("SendNotify", foundPlayer, type, text);
                        if(config.alertchat) PrintToChat(foundPlayer, text);
                    }
                    
                }
            } else {
                Effect.server.Run(sound, player.transform.position);
                    if(config.alertgametip){
                    if (red == "danger"){
                        player.SendConsoleCommand($"gametip.showtoast {1} \"{text}\"  ");
                    } else {
                        player.SendConsoleCommand("gametip.hidegametip");
                        player.SendConsoleCommand("gametip.showgametip", text);
                        timer.Once(length, () => player.SendConsoleCommand("gametip.hidegametip"));
                    }
                }
                if(config.alertnotify && (Notify != null || UINotify != null)) Interface.Oxide.CallHook("SendNotify", player, type, text);
                if(config.alertchat) PrintToChat(player, text);
            }
        }
        #endregion
        
        #region Command
        [ConsoleCommand("SENDCMD")]
        private void commands(ConsoleSystem.Arg arg){
            var player = arg.Player();
            if (!player.IsBuildingAuthed()) return;
            var cup = player.GetBuildingPrivilege();
            if (!BuildingCupboard.ContainsKey(cup)){
            	CreateGameTip(cup, Languaje("ErrorTC", player.UserIDString), player, fxerror, 10, "danger");
                return;
            }

            switch (arg.Args[0]){
                case "MENU":
                {
                    ShowMenu(player, cup);
                    break;
                }
                case "PAGE":
                {
                    var page = int.Parse(arg.Args[1]);
            		ShowMenu(player, cup, page);
                    break;
                }
                case "UPGRADE":
                { 
                    var grade = arg.Args[2];
                	if (!HasPermission(player.UserIDString, permupgrade) || !Unlock(maxGradeTier, grade)){
                        Effect.server.Run(fxerror, player.transform.position);
                        return;
                    } 
                    if (NoEscape != null){
                        if (NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString)){
                            CreateGameTip(cup, Languaje("RaidBlocked", player.UserIDString), player, fxerror, 10, "danger");
                            CuiHelper.DestroyUi(player, upgrade_0);
                            CuiHelper.DestroyUi(player, color_0);
                            return;
                        }
                    }
                	var id = int.Parse(arg.Args[1]);
                    var skinid = int.Parse(arg.Args[3]);
                    var page = int.Parse(arg.Args[4]);
                    var bg = BuildingGrade.Enum.Wood;
                    if(grade == "stone") bg = BuildingGrade.Enum.Stone;
                    if(grade == "metal") bg = BuildingGrade.Enum.Metal;
                    if(grade == "armored") bg = BuildingGrade.Enum.TopTier;
                    BuildingCupboard[cup].id = id;
                    BuildingCupboard[cup].color = (arg.Args[5] != "0");
                    BuildingCupboard[cup].grade = bg;
                    BuildingCupboard[cup].skinid = skinid;
                    BuildingCupboard[cup].work = !BuildingCupboard[cup].work;
                    if (BuildingCupboard[cup].work){
                        BuildingCupboard[cup].workupgrade = ServerMgr.Instance.StartCoroutine(UpdateProgress(player, cup));
                    } else {
                        if (BuildingCupboard[cup].workupgrade != null){
                            ServerMgr.Instance.StopCoroutine(BuildingCupboard[cup].workupgrade);
                        }
                    }
                    CuiHelper.DestroyUi(player, upgrade_0);
                    CuiHelper.DestroyUi(player, color_0);
                    ShowButtonTC(player, cup);
                    break;
                }
                case "REPAIR":
                {
                	if (!HasPermission(player.UserIDString, permrepair)) return;
                    if (NoEscape != null){
                        if (NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString)){
                            CreateGameTip(cup, Languaje("RaidBlocked", player.UserIDString), player, fxerror, 10, "danger");
                            CuiHelper.DestroyUi(player, upgrade_0);
                            CuiHelper.DestroyUi(player, color_0);
                            return;
                        }
                    }
                    BuildingCupboard[cup].repair = !BuildingCupboard[cup].repair;
                    if (BuildingCupboard[cup].repair){
                        BuildingCupboard[cup].workrepair = ServerMgr.Instance.StartCoroutine(RepairProgress(player, cup));
                    } else {
                        if (BuildingCupboard[cup].workrepair != null){
                            ServerMgr.Instance.StopCoroutine(BuildingCupboard[cup].workrepair);
                        }
                    }
                    ShowButtonTC(player, cup);
                    break;
                }
                case "STOP":
                {
                	CuiHelper.DestroyUi(player, color_0);
                	var page = int.Parse(arg.Args[4]);
                    BuildingCupboard[cup].work = !BuildingCupboard[cup].work;
                    if (BuildingCupboard[cup].work){
                        BuildingCupboard[cup].workupgrade = ServerMgr.Instance.StartCoroutine(UpdateProgress(player, cup));
                    } else {
                        if (BuildingCupboard[cup].workupgrade != null){
                            ServerMgr.Instance.StopCoroutine(BuildingCupboard[cup].workupgrade);
                        }
                    }
                    ShowMenu(player, cup, page);
                    break;
                }
                case "EFFECT":
                {
                    var page = int.Parse(arg.Args[1]);
                    BuildingCupboard[cup].effect = !BuildingCupboard[cup].effect;
                    ShowMenu(player, cup, page);
                    break;
                }
                case "DOWNGRADE":
                {
                    var page = int.Parse(arg.Args[1]);
                    BuildingCupboard[cup].downgrade = !BuildingCupboard[cup].downgrade;
                    ShowMenu(player, cup, page);
                    break;
                }
                case "COLOR":
                {
                	CuiHelper.DestroyUi(player, upgrade_0);
                    string id = arg.Args[1];
                    string grade = arg.Args[2];
                    string skinid = arg.Args[3];
                    string color = arg.Args[4];
                    var page = int.Parse(arg.Args[5]);
                    ShowMenuColor(player, cup, id, grade, skinid, color, page);
                    break;
                }
                case "COLORSELECT":
                {
                	CuiHelper.DestroyUi(player, upgrade_0);
                    string id = arg.Args[1];
                    string grade = arg.Args[2];
                    string skinid = arg.Args[3];
                    string color = arg.Args[4];
                    var page = int.Parse(arg.Args[5]);
                    BuildingCupboard[cup].colour = uint.Parse(color);
                    ShowMenuColor(player, cup, id, grade, skinid, color, page);
                    break;
                }
                case "RESKIN":
                {
                    if (!HasPermission(player.UserIDString, permreskin)) return;
                    if (NoEscape != null){
                        if (NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString)){
                            CreateGameTip(cup, Languaje("RaidBlocked", player.UserIDString), player, fxerror, 10, "danger");
                            CuiHelper.DestroyUi(player, upgrade_0);
                            CuiHelper.DestroyUi(player, color_0);
                            return;
                        }
                    }
                	CuiHelper.DestroyUi(player, upgrade_0);
                    var id = int.Parse(arg.Args[1]);
                    string grade = arg.Args[2];
                    var skinid = int.Parse(arg.Args[3]);
                    var page = int.Parse(arg.Args[4]);
                    var bg = BuildingGrade.Enum.Wood;
                    if(grade == "stone") bg = BuildingGrade.Enum.Stone;
                    if(grade == "metal") bg = BuildingGrade.Enum.Metal;
                    if(grade == "armored") bg = BuildingGrade.Enum.TopTier;
                    BuildingCupboard[cup].id = id;
                    BuildingCupboard[cup].grade = bg;
                    BuildingCupboard[cup].color = (arg.Args[5] != "0");
                    BuildingCupboard[cup].skinid = skinid;
                    BuildingCupboard[cup].reskin = !BuildingCupboard[cup].reskin;
                    if (BuildingCupboard[cup].reskin){
                        BuildingCupboard[cup].workreskin = ServerMgr.Instance.StartCoroutine(ReskinProgress(player, cup));
                    } else {
                        if (BuildingCupboard[cup].workreskin != null){
                            ServerMgr.Instance.StopCoroutine(BuildingCupboard[cup].workreskin);
                        }
                    }
                    ShowMenu(player, cup, page);
                    break;
                }
                case "REFRESH":
                {
                	if (!HasPermission(player.UserIDString, permadmin)) return;
                    GetNewItems(player);
                    ShowMenu(player, cup);
                    break;
                }
                case "AUTH":
                {
                	if (!HasPermission(player.UserIDString, permlist)) return;
                    var page = int.Parse(arg.Args[1]);
                    ShowMenuAuthlist(player, cup, page);
                    break;
                }
                case "REMOVEAUTH":
                {
                	if (!HasPermission(player.UserIDString, permlist)) return;
                    var page = int.Parse(arg.Args[1]);
                    //var tc2 = int.Parse(arg.Args[2]);
                    var userid = Convert.ToUInt64(arg.Args[3]);
                    if (cup == null) return;
                    if (!cup.IsAuthed(player)) return;
                    if (Interface.CallHook("OnCupboardDeauthorize", cup, player) != null)  return;
                    cup.authorizedPlayers.RemoveWhere(x => x.userid == userid);
                    cup.SendNetworkUpdate();
                    if(player.userID == userid){
                    	CuiHelper.DestroyUi(player, authlist_0);
                    	return;
                    }
                    ShowMenuAuthlist(player, cup, page);
                    break;
                }
                case "CLOSE":
                {
                    CuiHelper.DestroyUi(player, upgrade_0);
                    CuiHelper.DestroyUi(player, authlist_0);
                    ShowButtonTC(player, cup);
                    break;
                }
                case "CLOSE2":
                {
                	var page = int.Parse(arg.Args[1]);
                    CuiHelper.DestroyUi(player, color_0);
                    ShowMenu(player, cup, page);
                    break;
                }
                case "ERROR":
                {
                    CreateGameTip(null, Languaje("UpgradeLock", player.UserIDString), player, fxerror, 10, "danger");
                    break;
                }
            }
        }
        #endregion
        
        #region CUI
        private void ShowMenu(BasePlayer player, BuildingPrivlidge cup, int page = 0){
            CuiHelper.DestroyUi(player, upgrade_0);

            if (TiersMode != null) {
                object maxGradeTierObject = TiersMode.Call("GetMaxGradeBuild");
                if (maxGradeTierObject != null && maxGradeTierObject is int) { 
                    maxGradeTier = (int)maxGradeTierObject;
                } else { maxGradeTier = 4; }
            } else {
                maxGradeTier = 4;
            }

            var BuildingItems = GetBuildingItems(player).Skip(12 * page).Take(12).ToList();
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = upgrade_0,
                Parent = "OverlayNonScaled",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0 0 0 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-1000 -800",
                        OffsetMax = "1000 800"
                    },
                    new CuiNeedsCursorComponent()
                }
            });

			container.Add(new CuiElement {
                Name = "title",
                Parent = upgrade_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.10 0.15 0.10 0.9",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-450 230",
                        OffsetMax = "450 260"
                    }
                }
            });
            
            UI.Label(ref container, "title", Languaje("title1", player.UserIDString), 16, "0.022 0.05", "0.8 0.95", "1.00 1.00 1.00 0.9", TextAnchor.MiddleLeft, true);
            UI.Button(ref container, "title", "0.90 0.20 0.20 0.50", Languaje("CLOSE", player.UserIDString), 13, "0.89 0", "0.999 0.982", "SENDCMD CLOSE");
            
            container.Add(new CuiElement {
                Name = upgrade_1,
                Parent = upgrade_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.2 0.23 0.2 0.40",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-450 -190",
                        OffsetMax = "450 230"
                    }
                }
            });
            
            var list_sizeX = 135;
            var list_sizeY = 135;
            var list_startX = -430;
            var list_startY = 190;
            var list_x = list_startX;
            var list_y = list_startY;
            var po = 0;
            var e = 0;

            bool unlock2 = (!HasPermission(player.UserIDString, permreskin));
         
            foreach (var list_entry in BuildingItems){
                var perm = list_entry.permission;
                if (po != 0 && po % 6 == 0){
                    list_x = list_startX;
                    list_y -= list_sizeY + 35;
                }
                po++;
                
                string list_name = list_entry.name;
                string list_img = list_entry.img;
                int ID = list_entry.ID;
                
                bool unlock = (!HasPermission(player.UserIDString, perm) || !Unlock(maxGradeTier, list_entry.grade));
                bool up = (BuildingCupboard[cup].work && BuildingCupboard[cup].id == ID);
                
                container.Add(new CuiElement {
                    Name = upgrade_2,
                    Parent = upgrade_1,
                    Components = {
                        new CuiImageComponent {
                            Color = "0.2 0.30 0.2 0.60",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY-25}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });
                
                container.Add(new CuiElement {
                    Name = "bggreen",
                    Parent = upgrade_1,
                    Components = {
                        new CuiImageComponent {
                            Color = "0.2 0.30 0.2 0.80",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });

                container.Add(new CuiElement {
                    Name = upgrade_3, Parent = upgrade_1, Components = {
                        new CuiRawImageComponent{
                            Png = GetImageLibrary(list_img)
                        },
                        new CuiRectTransformComponent{
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });

                if (unlock) UI.Image(ref container, upgrade_3, GetImageLibrary("lock5"), "0.1 0.1", "0.9 0.9");
                if (BuildingCupboard[cup].work && BuildingCupboard[cup].id == ID) UI.Image(ref container, upgrade_3, GetImageLibrary("upgrade2"), "0.1 0.1", "0.9 0.9");
				UI.Label(ref container, upgrade_2, list_name, 12, "0.05 0", "0.55 0.15", "0.70 0.70 0.70 1.00", TextAnchor.MiddleLeft, true);
                
                if(list_entry.color && !unlock){
                	UI.Panel(ref container, upgrade_3, "0.80 1.00 0.50 0.30", "0.82 0.82", "0.95 0.95");
                	UI.Image(ref container, upgrade_3, GetImageLibrary("color_" + BuildingCupboard[cup].colour), "0.83 0.83", "0.94 0.94");
                	UI.Button(ref container, upgrade_3, "0 0 0 0", "", 10, "0.83 0.83", "0.94 0.94", $"SENDCMD COLOR {ID} {list_entry.grade} {list_entry.skinid} {BuildingCupboard[cup].colour} {page}");
                }

                if(config.reskin){
                    bool ups = (BuildingCupboard[cup].reskin && BuildingCupboard[cup].id == ID);
                	UI.Panel(ref container, upgrade_3, ups ? "0.90 0.20 0.20 0.50" : "0.80 1.00 0.50 0.30", (list_entry.color && !unlock) ? "0.82 0.66" : "0.82 0.82", (list_entry.color && !unlock) ? "0.95 0.79" : "0.95 0.95");
                	UI.Image(ref container, upgrade_3, GetImageLibrary("spray"), (list_entry.color && !unlock) ? "0.83 0.66" : "0.83 0.82", (list_entry.color && !unlock) ? "0.94 0.79" : "0.94 0.95");
                	UI.Button(ref container, upgrade_3, "0 0 0 0", "", 10, (list_entry.color && !unlock) ? "0.83 0.66" : "0.83 0.82", (list_entry.color && !unlock) ? "0.94 0.79" : "0.94 0.95", unlock2 ? $"SENDCMD ERROR" : $"SENDCMD RESKIN {ID} {list_entry.grade} {list_entry.skinid} {page} {list_entry.color}");
                }
                
                if (HasPermission(player.UserIDString, permupgrade)) UI.Button(ref container, upgrade_2, unlock ? "0.20 0.20 0.20 0.80" : up ? "0.90 0.20 0.20 0.50" : "0.80 1.00 0.50 0.10", Languaje(unlock ? "LOCK" : up ? "STOP" : "UPGRADE", player.UserIDString), 10, "0.6 0", "0.993 0.15", up ? $"SENDCMD STOP {ID} {list_entry.grade} {list_entry.skinid} {page} {list_entry.color}" : unlock ? $"SENDCMD ERROR" : list_entry.color ? $"SENDCMD COLOR {ID} {list_entry.grade} {list_entry.skinid} {BuildingCupboard[cup].colour} {page}" : $"SENDCMD UPGRADE {ID} {list_entry.grade} {list_entry.skinid} {page} {list_entry.color}");
                if (!HasPermission(player.UserIDString, permupgrade) && HasPermission(player.UserIDString, permreskin)) UI.Button(ref container, upgrade_2, "0.80 1.00 0.50 0.10", Languaje("Reskin", player.UserIDString), 10, "0.6 0", "0.993 0.15", $"SENDCMD RESKIN {ID} {list_entry.grade} {list_entry.skinid} {page} {list_entry.color}");
                
                list_x += list_sizeX + 10;
                e++;
            }
            
            if (config.itemsList.Count > 12 || page != 0){
                UI.Button(ref container, upgrade_1, page > 0 ? "0.30 0.30 0.80 0.90" : "0.5 0.5 0.5 0.1", Languaje("Back", player.UserIDString), 14, "0.3 0.05", "0.49 0.12", page > 0 ? $"SENDCMD PAGE {page - 1}": "");
                UI.Button(ref container, upgrade_1, GetBuildingItems(player).Skip(12 * (page + 1)).Count() > 0 ? "0.30 0.30 0.80 0.90" : "0.5 0.5 0.5 0.1"  , Languaje("Next", player.UserIDString), 14, "0.51 0.05", "0.7 0.12", GetBuildingItems(player).Skip(12 * (page + 1)).Count() > 0 ? $"SENDCMD PAGE {page + 1}": $"");
            }
            
            if (HasPermission(player.UserIDString, permadmin)) UI.Button(ref container, upgrade_1, "0.35 0.35 0.60 0.90", Languaje("CheckUpdate", player.UserIDString), 14, "0.634 0.05", "0.806 0.12", $"SENDCMD REFRESH {page}");
            
            if (TCLevels != null) UI.Button(ref container, upgrade_1, "0.35 0.60 0.35 0.90", "TC Levels Upgrades", 14, "0.82 0.05", "0.976 0.12", $"tclevels.show {cup.net.ID.Value}");
            
            if(config.playfx){
                UI.Panel(ref container, upgrade_1, "1.0 1.0 1.0 0.05", "0.02 0.06", "0.043 0.11");
                UI.Button(ref container, upgrade_1, BuildingCupboard[cup].effect ? "0.2 0.5 0.2 0.9" : "0.5 0.2 0.2 0.9", "", 10, "0.023 0.065", "0.040 0.102", $"SENDCMD EFFECT {page}");
                UI.Label(ref container, upgrade_1, Languaje(BuildingCupboard[cup].effect ? "EffectON" : "EffectOFF", player.UserIDString), 10, "0.05 0.06", "0.3 0.11", "0.70 0.70 0.70 1.00", TextAnchor.MiddleLeft, true);
            }

            if(config.downgrade){
                UI.Panel(ref container, upgrade_1, "1.0 1.0 1.0 0.05", "0.12 0.06", "0.143 0.11");
                UI.Button(ref container, upgrade_1, BuildingCupboard[cup].downgrade ? "0.2 0.5 0.2 0.9" : "0.5 0.2 0.2 0.9", "", 10, "0.123 0.065", "0.140 0.102", $"SENDCMD DOWNGRADE {page}");
                UI.Label(ref container, upgrade_1, Languaje(BuildingCupboard[cup].downgrade ? "DowngradeON" : "DowngradeOFF", player.UserIDString), 10, "0.15 0.06", "0.4 0.11", "0.70 0.70 0.70 1.00", TextAnchor.MiddleLeft, true);
            }

            CuiHelper.AddUi(player, container);
        }

		private void ShowMenuColor(BasePlayer player, BuildingPrivlidge cup, string id, string grade, string skinid, string color, int page = 0){
            CuiHelper.DestroyUi(player, color_0);
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = color_0,
                Parent = "OverlayNonScaled",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0 0 0 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-1000 -800",
                        OffsetMax = "1000 800"
                    },
                    new CuiNeedsCursorComponent()
                }
            });

			container.Add(new CuiElement {
                Name = "title2",
                Parent = color_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.10 0.15 0.10 0.9",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 230",
                        OffsetMax = "200 260"
                    }
                }
            });
            
            UI.Label(ref container, "title2", Languaje("title2", player.UserIDString), 16, "0.03 0.05", "0.8 0.95", "1.00 1.00 1.00 0.9", TextAnchor.MiddleLeft, true);
            UI.Button(ref container, "title2", "0.90 0.20 0.20 0.50", Languaje("CLOSE", player.UserIDString), 13, "0.775 0", "0.999 0.982", $"SENDCMD CLOSE2 {page}");
            
            container.Add(new CuiElement {
                Name = color_1,
                Parent = color_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.2 0.23 0.2 0.40",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 -190",
                        OffsetMax = "200 230"
                    }
                }
            });
            
            var e = 0;
            var list_sizeX = 80;
            var list_sizeY = 80;
            var list_startX = -175;
            var list_startY = 185;
            var list_x = list_startX;
            var list_y = list_startY;
         
            for (var i = 0; i < 17;i++){
                if(i < 13){
                    if (i != 0 && i % 4 == 0){
                        list_x = list_startX;
                        list_y -= list_sizeY + 10;
                    }
                }
                if(i > 11){
                    list_sizeX = 62;
                    list_sizeY = 62;
                }
                
                container.Add(new CuiElement {
                    Name = color_2,
                    Parent = color_1,
                    Components = {
                        new CuiImageComponent {
                            Color = BuildingCupboard[cup].colour == e ? "1 1 1 0.70" : "0.2 0.30 0.2 0.60",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });

                container.Add(new CuiElement {
                    Name = color_3, Parent = color_1, Components = {
                        new CuiRawImageComponent{
                            Png = GetImageLibrary("color_" + e)
                        },
                        new CuiRectTransformComponent{
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x + 3.0f} {list_y-list_sizeY + 3.0f}",
                            OffsetMax = $"{list_x + list_sizeX - 3.0f} {list_y - 3.0f}"
                        }
                    }
                });

                UI.Button(ref container, color_3, "0 0 0 0", "", 10, "0 0", "1 1", $"SENDCMD COLORSELECT {id} {grade} {skinid} {e} {page}");
                list_x += list_sizeX + 10;
                e = i+1;
            }
            
            bool up = (BuildingCupboard[cup].work && BuildingCupboard[cup].id == int.Parse(id));
            UI.Button(ref container, color_1, up ? "0.90 0.20 0.20 0.50" : "0.80 1.00 0.50 0.10", Languaje(up ? "STOP" : "UPGRADE", player.UserIDString), 12, "0.35 0.04", "0.65 0.11", up ? $"SENDCMD STOP {id} {grade} {skinid} {page} {color}" : $"SENDCMD UPGRADE {id} {grade} {skinid} {page} {color}");
            CuiHelper.AddUi(player, container);
        }

		private void ShowMenuAuthlist(BasePlayer player, BuildingPrivlidge cup, int page = 0){
            CuiHelper.DestroyUi(player, authlist_0);
            var PlayersTC = GetAuthPlayers(cup).Skip(6 * page).Take(6).ToList();
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = authlist_0,
                Parent = "OverlayNonScaled",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0 0 0 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-1000 -800",
                        OffsetMax = "1000 800"
                    },
                    new CuiNeedsCursorComponent()
                }
            });

			container.Add(new CuiElement {
                Name = "title3",
                Parent = authlist_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.10 0.15 0.10 0.9",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 230",
                        OffsetMax = "200 260"
                    }
                }
            });
            
            UI.Label(ref container, "title3", Languaje("title3", player.UserIDString), 16, "0.03 0.05", "0.8 0.95", "1.00 1.00 1.00 0.9", TextAnchor.MiddleLeft, true);
            UI.Button(ref container, "title3", "0.90 0.20 0.20 0.50", Languaje("CLOSE", player.UserIDString), 13, "0.775 0", "0.999 0.982", $"SENDCMD CLOSE");
            
            container.Add(new CuiElement {
                Name = authlist_1,
                Parent = authlist_0,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0.2 0.23 0.2 0.40",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 -190",
                        OffsetMax = "200 230"
                    }
                }
            });
            
            var e = 0;
            var list_sizeX = 370;
            var list_sizeY = 50;
            var list_startX = -185;
            var list_startY = 190;
            var list_x = list_startX;
            var list_y = list_startY;
            bool showsteamid = config.steamidshow || HasPermission(player.UserIDString, permadmin);
         
            foreach (var entry in PlayersTC){
                container.Add(new CuiElement {
                    Name = authlist_2,
                    Parent = authlist_1,
                    Components = {
                        new CuiImageComponent {
                            Color = "0.2 0.30 0.2 0.50",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });

                container.Add(new CuiElement {
                    Name = authlist_3,
                    Parent = authlist_1,
                    Components = {
                        new CuiImageComponent {
                            Color = "0.1 0.2 0.1 0.60",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent{
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x + 3.0f} {list_y-list_sizeY + 3.0f}",
                            OffsetMax = $"{list_x + list_sizeX - 3.0f} {list_y - 3.0f}"
                        }
                    }
                });
                
				UI.Label(ref container, authlist_3, entry.username, 14, (showsteamid) ? "0.03 0.40" : "0.03 0.05", "0.8 0.95", "1.00 1.00 1.00 0.9", TextAnchor.MiddleLeft, true);
                if (showsteamid) UI.Label(ref container, authlist_3, entry.userid.ToString(), 10, "0.03 0.05", "0.8 0.50", "1.00 1.00 1.00 0.7", TextAnchor.MiddleLeft);
                if (HasPermission(player.UserIDString, permdelauth)) UI.Button(ref container, authlist_3, "0.7 0.2 0.2 0.8", "REMOVE", 10, "0.8 0", "1 1", $"SENDCMD REMOVEAUTH {page} {cup} {entry.userid}");
                list_y -= list_sizeY + 8;
            }
            
            if (GetAuthPlayers(cup).Count > 6 || page != 0){
                UI.Button(ref container, authlist_1, page > 0 ? "0.30 0.30 0.80 0.90" : "0.5 0.5 0.5 0.1", Languaje("Back", player.UserIDString), 12, "0.2 0.05", "0.49 0.12", page > 0 ? $"SENDCMD AUTH {page - 1}": "");
                UI.Button(ref container, authlist_1, GetAuthPlayers(cup).Skip(6 * (page + 1)).Count() > 0 ? "0.30 0.30 0.80 0.90" : "0.5 0.5 0.5 0.1"  , Languaje("Next", player.UserIDString), 12, "0.51 0.05", "0.8 0.12", GetAuthPlayers(cup).Skip(6 * (page + 1)).Count() > 0 ? $"SENDCMD AUTH {page + 1}": $"");
            }
            
            CuiHelper.AddUi(player, container);
        }
        
        private void ShowButtonTC(BasePlayer player, BuildingPrivlidge cup){
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = buttons_0,
                Parent = "OverlayNonScaled",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.3f,
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = config.AnchorMin,
                        AnchorMax = config.AnchorMax
                    }
                }
            });
            
            var start = 0.675f;
            var width = 0.32f;
            var space = 0.015f;

            if (HasPermission(player.UserIDString, permlist)){
                var text3 = Languaje("ListAuth", player.UserIDString) + "    ";
                UI.Button(ref container, buttons_0, config.btntccolor, text3, 12, start + " 0.0", (start + width) + " 1", $"SENDCMD AUTH 0", TextAnchor.MiddleCenter, false);
                UI.Image(ref container, buttons_0, GetImageLibrary("cupboard.toolbt"), (start + width - 0.075f) + " 0.1", (start + width - 0.01f) + " 0.9");
                start -= width + space;
            }

            if (HasPermission(player.UserIDString, permrepair)){
                var text = (BuildingCupboard[cup].repair ? Languaje("Repairing", player.UserIDString) : Languaje("Repair", player.UserIDString)) + "     ";
                UI.Button(ref container, buttons_0, BuildingCupboard[cup].repair ? config.btntccolora : config.btntccolor, text, 12, start + " 0.0", (start + width) + " 1", $"SENDCMD REPAIR", TextAnchor.MiddleCenter, false);
                UI.Image(ref container, buttons_0, GetImageLibrary("hammerbt"), (start + width - 0.075f) + " 0.1", (start + width - 0.01f) + " 0.9");
                start -= width + space;
            }

            if (HasPermission(player.UserIDString, permupgrade)){
                var grade = BuildingCupboard[cup].grade;
                var image = grade == BuildingGrade.Enum.Metal ? "metal.fragmentsbt" : grade == BuildingGrade.Enum.Stone ? "stonesbt" : grade == BuildingGrade.Enum.Wood ? "woodbt" : "metal.refinedbt";
                var text2 = (BuildingCupboard[cup].work ? Languaje("Upgrading", player.UserIDString) : BuildingCupboard[cup].reskin ? Languaje("Skining", player.UserIDString) : Languaje("Upgrade", player.UserIDString)) + "     ";
                UI.Button(ref container, buttons_0, (BuildingCupboard[cup].work || BuildingCupboard[cup].reskin) ? config.btntccolora : config.btntccolor, text2, 12, start + " 0.0", (start + width) + " 1", $"SENDCMD MENU", TextAnchor.MiddleCenter, false);
                UI.Image(ref container, buttons_0, GetImageLibrary(BuildingCupboard[cup].reskin ? "spray" : image), (start + width - 0.075f) + " 0.1", (start + width - 0.01f) + " 0.9");
            } else if (HasPermission(player.UserIDString, permreskin)){
                var text3 = Languaje(BuildingCupboard[cup].reskin ? "Skining" : "Reskin", player.UserIDString) + "     ";
                UI.Button(ref container, buttons_0, BuildingCupboard[cup].reskin ? config.btntccolora : config.btntccolor, text3, 12, start + " 0.0", (start + width) + " 1", $"SENDCMD MENU", TextAnchor.MiddleCenter, false);
                UI.Image(ref container, buttons_0, GetImageLibrary("spray"), (start + width - 0.075f) + " 0.1", (start + width - 0.01f) + " 0.9");
            }

            CuiHelper.DestroyUi(player, buttons_0);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region CUI Helper
        public class UI {
            static public void Panel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false, string material = "assets/content/ui/namefontmaterial.mat"){
                container.Add(new CuiPanel{
                    Image = { Color = color, Material = material},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax},
                    CursorEnabled = cursor
                },
                panel);
            }

            static public void Label(ref CuiElementContainer container, string panel, string text, int size, string aMin, string aMax, string color = "1 1 1 0.6", TextAnchor align = TextAnchor.MiddleCenter, bool font = false){
                container.Add(new CuiLabel{
                    Text = { FontSize = size, Font = font? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf", Color = color, Align = align, Text = text},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax}
                },
                panel);
            }

            static public void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, bool font = true){
                container.Add(new CuiButton{
                    Button = { Color = color, Material = "assets/content/ui/namefontmaterial.mat", Command = command, FadeIn = 0f},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax},
                    Text = { Text = text, Font = font? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf", FontSize = size, Align = align}
                },
                panel);
            }

            static public void Image(ref CuiElementContainer container, string panel, string png, string aMin, string aMax){
                container.Add(new CuiElement{
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components = {
                        new CuiRawImageComponent {Png = png},
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax}
                    }
                });
            }
        }
        #endregion
      
        #region Config
        private static ConfigData config;

        private class ConfigData {
            [JsonProperty("GUI Buttons TC - Color Default")]
            public string btntccolor = "0.3 0.40 0.3 0.60";

            [JsonProperty("GUI Buttons TC - Color Active")]
            public string btntccolora = "0.90 0.20 0.20 0.50";

            [JsonProperty("GUI Buttons TC - AnchorMin")]
            public string AnchorMin = "0.71 0.862";

            [JsonProperty("GUI Buttons TC - AnchorMax")]
            public string AnchorMax = "0.947 0.892";

            [JsonProperty("Alert Gametip")]
            public bool alertgametip = true;

            [JsonProperty("Alert Chat")]
            public bool alertchat = true;

            [JsonProperty("Alert Notify Plugin")]
            public bool alertnotify = false;

            [JsonProperty("Notify: select what notification type to be used")]
            public Dictionary<string, int> notifyType = new Dictionary<string, int>(){
                ["error"] = 0,
                ["info"] = 0,
            };

            [JsonProperty("Color Prefix Chat")]
            public string colorprefix = "#f74d31";

            [JsonProperty("Show Admin Auth List")]
            public bool adminshow = false;

            [JsonProperty("Show SteamID Auth List")]
            public bool steamidshow = true;

            [JsonProperty("Upgrade Effect")]
            public bool playfx = true;

            [JsonProperty("Downgrade Enable")]
            public bool downgrade = true;

            [JsonProperty("Downgrade only Owner Entity Build")]
            public bool onlyowner = true;

            [JsonProperty("Upgrade only Owner Entity Build")]
            public bool onlyownerup = true;

            [JsonProperty("Upgrade / Downgrade only Owner and Team")]
            public bool teamupdate = false;

            [JsonProperty("Reskin Enable")]
            public bool reskin = true;

            [JsonProperty("Cooldown Frequency Upgrade (larger number is slower)")]
            public Dictionary<string, float> FrequencyUpgrade = new Dictionary<string, float>(){
                ["bettertc.use"] = 2.0f,
                ["bettertc.vip"] = 1.0f,
            };

            [JsonProperty("Cooldown Frequency Reskin (larger number is slower)")]
            public Dictionary<string, float> FrequencyReskin = new Dictionary<string, float>(){
                ["bettertc.use"] = 2.0f,
                ["bettertc.vip"] = 1.0f,
            };
            
            [JsonProperty("Cooldown Frequency Repair (larger number is slower)")]
            public Dictionary<string, float> FrequencyRepair = new Dictionary<string, float>(){
                ["bettertc.use"] = 2.0f,
                ["bettertc.vip"] = 1.0f,
            };

            [JsonProperty("Cost Modifier for repairs")]
            public Dictionary<string, float> CostListRepair = new Dictionary<string, float>(){
                ["bettertc.use"] = 1.5f,
                ["bettertc.vip"] = 1.0f,
            };

            [JsonProperty("Items")]
            public List<ItemInfo> itemsList = new List<ItemInfo>();
        }
        
        private class ItemInfo {
            [JsonProperty(PropertyName = "ID")]
            public int ID;

            [JsonProperty(PropertyName = "Enabled")]
            public bool enabled;

            [JsonProperty(PropertyName = "Short Name")]
            public string name;
            
            [JsonProperty(PropertyName = "Grade")]
            public string grade;

            [JsonProperty(PropertyName = "Img Icon")]
            public string img;

            [JsonProperty(PropertyName = "SkinID")]
            public int skinid;
            
            [JsonProperty(PropertyName = "Color")]
            public bool color;
            
            [JsonProperty(PropertyName = "Permission Use")]
            public string permission;
        }

        private class TCConfig {
            public int id;
            public BuildingGrade.Enum grade;
            public int skinid;
            public bool color;
            public uint colour;
            public Coroutine workupgrade;
            public Coroutine workrepair;
            public Coroutine workreskin;
            public bool work;
            public bool repair;
            public bool reskin;
            public bool effect;
            public bool downgrade;
        }

        protected override void LoadConfig() {
            base.LoadConfig();
            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null) LoadDefaultConfig();
            } catch {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }
            SaveConfig();
        }

        protected override void SaveConfig(){
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig(){
            config = new ConfigData();
            config.itemsList.Add(new ItemInfo {
                ID = 1,
                enabled = true,
                name = "Wood",
                grade = "wood",
                img = "https://img.rustspain.com/bettertc/wood.png",
                skinid = 0,
                color = false,
                permission = "bettertc.updefault"
            });
            SaveConfig();
        }
        #endregion

		#region Language
        protected override void LoadDefaultMessages(){
            lang.RegisterMessages(new Dictionary<string, string> {
                ["title1"] = "BUILDING AUTO UPGRADE",
                ["title2"] = "COLOUR SELECTION",
                ["title3"] = "AUTHORIZED PLAYERS",
                ["CLOSE"] = "CLOSE",
                ["STOP"] = "STOP",
                ["UPGRADE"] = "UPGRADE",
                ["ListAuth"] = "LIST AUTH",
                ["Repair"] = "REPAIR",
                ["Repairing"] = "REPAIRING",
                ["Upgrade"] = "UPGRADE",
                ["Upgrading"] = "UPGRADING",
                ["Skining"] = "SKINNING",
                ["Reskin"] = "RESKIN",
                ["CheckUpdate"] = "Check Update",
                ["RaidBlocked"] = "You cannot do this while you have Raid Block.",
                ["ErrorTC"] = "Oops something went wrong, open the panel again.",
                ["UpgradeFinish"] = "The improvement process is complete.",
                ["RepairFinish"] = "The repair process is complete.",
                ["ReskinFinish"]  = "The reskin process is complete.",
                ["NoResourcesRepair"] = "Repair stopped due to lack of resources.",
                ["NoResourcesUpgrade"] = "Improvements stopped due to lack of resources.",
                ["NoResourcesReskin"] = "Reskin stopped due to lack of resources.",
                ["UpgradeBlock"] = "Upgrading to this level is currently locked.",
                ["UpgradeLock"] = "You do not have permissions to improve the selected option.",
                ["LOCK"] = "LOCK",
                ["EffectON"] = "EFFECT ON",
                ["EffectOFF"] = "EFFECT OFF",
                ["DowngradeON"] = "DOWNGRADE ON",
                ["DowngradeOFF"] = "DOWNGRADE OFF"
            }, this);
        }

        private string Languaje(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void PrintToChat(BasePlayer player, string message) => Player.Message(player, "<color=" + config.colorprefix + ">BetterTC:</color> " + message);
        #endregion
    }
}