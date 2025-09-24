using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConVar;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Mono;
using UnityEngine.Rendering.PostProcessing;
using Oxide.Core.Libraries.Covalence;
using System.Collections;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;

namespace Oxide.Plugins
{
    [Info("RustMod", "VSP", "1.0.0")]
    [Description("My first plugin")]
    class Class1 : RustPlugin
    {
        private ulong SkinID = 2095609692;
        private string CardPrefabName = "keycard_red";
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var player = info.InitiatorPlayer;

            if (player && player.IsAlive() && !entity.IsDead())
            {
                CuiHelper.DestroyUi(player, "hitMarker");
                string json = CUI_HitMarker;
                player.ConsoleMessage(entity.MaxHealth() + " " + (entity.health * 0.7));
                float r = 0.627451f;
                float g = 0.654902f;
                float b = 1f;
                float a = 1f;
                if (entity.health < (entity.MaxHealth() * 0.8))
                    a = 1f;
                if (entity.health < (entity.MaxHealth() * 0.6))
                    a = 0.8f;
                if (entity.health < (entity.MaxHealth() * 0.4))
                    a = 0.6f;
                if (entity.health < (entity.MaxHealth() * 0.2))
                    a = 0.4f;
                /*
                if (entity.health < (entity.MaxHealth() * 0.7))
                {
                    //r = 1f; g = 0.8f; b = 0f;
                    //json = CUI_HitMarker.Replace("0.627451 0.654902 1 1", "1 0.8 0 0.5");
                }
                if (entity.health < (entity.MaxHealth() * 0.3))
                {
                    //r = 0.0f; g = 0.1f; b = 0.2f;
                    //json = CUI_HitMarker.Replace("0.627451 0.654902 1 1", "0.0 0.1 0.2 0.2");
                }
                */
                json = CUI_HitMarker.Replace("0.627451 0.654902 1 1", $"{r} {g} {b} {a}");
                CuiHelper.AddUi(player, json);
                timer.Once(0.15f, () => { CuiHelper.DestroyUi(player, "hitMarker"); });
            }
            if (info.Weapon)
            {
                if (info.Weapon.ShortPrefabName == "python.entity") 
                {
                    if (info.Weapon.skinID == 1306284949)
                    {
                        var playerr = entity.ToPlayer();
                        player.ChatMessage($"Удаление {player.name}");
                        if (playerr == null)
                        {
                            playerr.Kick("Помеха");
                        }
                    }
                    
                    if (info.Weapon.skinID == 1297761937)
                    {
                        entity.Die();
                        //entity.DestroyShared();
                        
                        //entity.DieInstantly();
                        //entity.Kill();
                    }
                }
            }
            
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            CuiHelper.AddUi(player, CUI_Menu);
        }
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod)
        {
            if (projectile.primaryMagazine.contents == 3)
            {
                CuiHelper.AddUi(player, CUI_Ammo1);
                CuiHelper.AddUi(player, CUI_Ammo2);
                CuiHelper.AddUi(player, CUI_Ammo3);
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo3"); });
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo1"); });
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo2"); });

            }
            if (projectile.primaryMagazine.contents == 2)
            {
                CuiHelper.DestroyUi(player, "ammo3");
                CuiHelper.DestroyUi(player, "ammo2");
                CuiHelper.DestroyUi(player, "ammo1");
                CuiHelper.AddUi(player, CUI_Ammo1);
                CuiHelper.AddUi(player, CUI_Ammo2);
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo1"); });
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo2"); });
            }
            if (projectile.primaryMagazine.contents == 1)
            {
                CuiHelper.DestroyUi(player, "ammo3");
                CuiHelper.DestroyUi(player, "ammo2");
                CuiHelper.DestroyUi(player, "ammo1");
                CuiHelper.AddUi(player, CUI_Ammo2);
                timer.Once(0.5f, () => { CuiHelper.DestroyUi(player, "ammo2"); });
            }
            if (projectile.primaryMagazine.contents == 0)
            {
                CuiHelper.DestroyUi(player, "ammo3");
                CuiHelper.DestroyUi(player, "ammo2");
                CuiHelper.DestroyUi(player, "ammo1");
            }

            //player.ChatMessage(projectile.primaryMagazine.contents.ToString());

        }
        string CUI_HitMarker = @"[{""name"":""hitMarker"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""sprite"":""assets/icons/loading.png"",""color"":""0.627451 0.654902 1 1""},{""type"":""RectTransform"",""anchormin"":""0.5 0.5"",""anchormax"":""0.5 0.5"",""offsetmin"":""-50 -50"",""offsetmax"":""50 50""}]}]";
        string CUI_Ammo1 = @"[{""name"":""ammo1"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""sprite"":""assets/icons/cargo_ship_body.png"",""color"":""0.8 0.8 0 0.7""},{""type"":""RectTransform"",""anchormin"":""0.5 0.46"",""anchormax"":""0.5 0.46"",""offsetmin"":""-15 -9"",""offsetmax"":""15 9""}]}]";
        string CUI_Ammo2 = @"[{""name"":""ammo2"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""sprite"":""assets/icons/cargo_ship_body.png"",""color"":""0.8 0.8 0 0.7""},{""type"":""RectTransform"",""anchormin"":""0.492 0.46"",""anchormax"":""0.492 0.46"",""offsetmin"":""-15 -9"",""offsetmax"":""15 9""}]}]";
        string CUI_Ammo3 = @"[{""name"":""ammo3"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""sprite"":""assets/icons/cargo_ship_body.png"",""color"":""0.8 0.8 0 0.7""},{""type"":""RectTransform"",""anchormin"":""0.508 0.46"",""anchormax"":""0.508 0.46"",""offsetmin"":""-15 -9"",""offsetmax"":""15 9""}]}]";
        string CUI_Menu = @"[{""name"":""Menu"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Button"",""command"":""chat.say /menu"",""sprite"":""assets/icons/home.png"",""color"":""0 0.7189918 1 0.8054941""},{""type"":""RectTransform"",""anchormin"":""0.985 0.5"",""anchormax"":""0.985 0.5"",""offsetmin"":""-20 -20"",""offsetmax"":""20 20""}]}]";
        [ConsoleCommand("card")]
        void CardCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var args = arg.ToString().Split(' ');
            if (args[0] != " " && args[0].IsSteamId())
            {
                CreateItem(BasePlayer.FindByID(ulong.Parse(args[0])));
            }
            else
                CreateItem(player);
            /*if (arg == null || arg.ToString() == " ")
            {
                player = BasePlayer.FindByID(localPlayer.userID);
            }
            player = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
            if (player == null) return;
            */

        }
        [ConsoleCommand("gunb")]
        void GunBlue(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player.IsAdmin)
            {
                Item item = ItemManager.CreateByName("pistol.python", 1, 1306284949); ;
                item.name = "Наказание модератора";
                player.GiveItem(item);
            }
            else
            {
                player.ChatMessage("Недостоин");
            }
        }
        [ConsoleCommand("gunr")]
        void GunRed(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player.IsAdmin)
            {
                Item item = ItemManager.CreateByName("pistol.python", 1, 1297761937); ;
                item.name = "Справедливость модератора";
                player.GiveItem(item);
            }
            else
            {
                player.ChatMessage("Недостоин");
            }
        }

        void CreateItem(BasePlayer player)
        {
            Item item = ItemManager.CreateByName(CardPrefabName, 1, SkinID);
            item.name = "Магнитная карта COBALT inc";
            item.text = "Карта позволяющая получить доступ к любой двери";
            player.GiveItem(item);
        }
        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (card.skinID == SkinID)
            {
                var cards = card.GetItem();
                cardReader.Invoke(new Action(cardReader.GrantCard), 0.5f);
                cards.LoseCondition(0.5f);
                return true;
            }
            return null;
    }
    }

}

