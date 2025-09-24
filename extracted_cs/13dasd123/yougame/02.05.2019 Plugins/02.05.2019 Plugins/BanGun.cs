// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BanGun", "SkiTles", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("One kill - one ban")]
    class BanGun : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Variables
        private Dictionary<BasePlayer, string> ActiveAdmins = new Dictionary<BasePlayer, string>();
        ulong skinid = 0;
        int magazine = 0;
        #endregion

        #region Config
        private string BunGunPerm = "bangun.use";
        private string BanGunShortname = "rifle.lr300";
        private string BanGunSkin = "1295381722";
        private string BanGunMagazine = "150";
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        private void LoadConfigValues()
        {
            GetConfig("Привилегия для использования", ref BunGunPerm);
            GetConfig("Shortname оружия", ref BanGunShortname);
            GetConfig("SkinID оружия", ref BanGunSkin);
            GetConfig("Размер магазина оружия", ref BanGunMagazine);
            SaveConfig();
        }
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        #endregion

        #region OxideHooks
        void OnServerInitialized()
        {
            if (!permission.PermissionExists(BunGunPerm)) permission.RegisterPermission(BunGunPerm, this);
        }
        void Loaded()
        {
            LoadMessages();
            LoadConfigValues();
            ulong.TryParse(BanGunSkin, out skinid);
            if (skinid == 0)
            {
                PrintWarning("Не удалось получить skinid из файла конфигурации.");
                rust.RunServerCommand("o.unload BanGun");
            }
            if (!Int32.TryParse(BanGunMagazine, out magazine) || magazine == 0)
            {
                magazine = 30;
                PrintWarning("Не удалось получить размер магазина, установлено дефолтное значение 30 патронов.");
            }
        }
        void Unload()
        {
            if (ActiveAdmins.Count > 0)
            {
                foreach (var admin in ActiveAdmins)
                {
                    ClearInv(admin.Key);
                }
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (ActiveAdmins.ContainsKey(player))
            {
                ClearInv(player);
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (entity.name.Contains("corpse")) return;
            if (hitInfo == null) return;
      //  Слив плагинов server-rust by Apolo YouGame
            var attacker = hitInfo.Initiator?.ToPlayer();
      //  Слив плагинов server-rust by Apolo YouGame
            if (attacker == null) return;
            if (!permission.UserHasPermission(attacker.UserIDString, BunGunPerm)) return;
            if (entity is BasePlayer)
            {
                var victim = entity.ToPlayer();
                if (victim == null) return;
                if (IsNPC(victim)) return;
                if (attacker == victim) return;
                var weaponName = hitInfo?.Weapon?.GetItem()?.info?.shortname ?? hitInfo?.WeaponPrefab?.GetItem()?.info?.shortname ?? string.Empty;
      //  Слив плагинов server-rust by Apolo YouGame
                if (weaponName == string.Empty || weaponName != BanGunShortname) return;
                var weaponskin = hitInfo?.Weapon?.GetItem()?.skin ?? 0;
      //  Слив плагинов server-rust by Apolo YouGame
                if (weaponskin == 0 || weaponskin != skinid) return;
                BanVictim(victim, attacker);
            }
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner == null) return;
            if (ActiveAdmins.ContainsKey(container.playerOwner))
            {
                if (item.info.shortname == BanGunShortname && item.skin == skinid)
                {
                    var player = container.playerOwner;
                    item.RemoveFromWorld();
                    item.Remove();
                    ClearInv(player);
                }
            }
        }
        object OnPlayerDie(BasePlayer player, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (ActiveAdmins.ContainsKey(player))
            {
                ClearInv(player);
            }
            return null;
        }
        bool CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if (ActiveAdmins.ContainsKey(target)) return false;
            return true;
        }
        #endregion

        #region Main
        private void BanVictim(BasePlayer player, BasePlayer admin)
        {
            player.IPlayer.Ban(ActiveAdmins[admin]);
            PrintToChat(player, string.Format(GetMsg("UserBanned", admin), player.displayName, ActiveAdmins[admin]));
            if (player.IsConnected)
            {
                player.Kick(ActiveAdmins[admin]);
            }
        }
        private void GiveBanGun(BasePlayer player, string reason)
        {
            player.inventory.containerBelt.Clear();
            player.inventory.containerMain.Clear();
            player.inventory.containerWear.Clear();
            //Создание предметов и выдача их админу
            //Одежда
            Item w1 = ItemManager.CreateByItemID(-194953424, 1, 822215130); //маска
            w1.MoveToContainer(player.inventory.containerWear);
            Item w2 = ItemManager.CreateByItemID(1110385766, 1, 821926807); //нагрудник
            w2.MoveToContainer(player.inventory.containerWear);
            Item w3 = ItemManager.CreateByItemID(1850456855, 1, 0); //килт
            w3.MoveToContainer(player.inventory.containerWear);
            Item w4 = ItemManager.CreateByItemID(1751045826, 1, 854821748); //худи
            w4.MoveToContainer(player.inventory.containerWear);
            Item w5 = ItemManager.CreateByItemID(237239288, 1, 815753148); //штаны, Pants
            w5.MoveToContainer(player.inventory.containerWear);
            Item w6 = ItemManager.CreateByItemID(-1549739227, 1, 869007492); //ботинки
            w6.MoveToContainer(player.inventory.containerWear);
            //Хотбар, оружие и тп.
            Item b1 = ItemManager.CreateByName(BanGunShortname, 1, skinid); //bangun
            //Заполнение магазина и добавление модов
            var weapon = b1.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                (b1.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity = magazine;
                (b1.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = magazine;
            }
            b1.contents.AddItem(ItemManager.CreateByItemID(1478091698, 1).info, 1); // дульный тормоз
            b1.contents.AddItem(ItemManager.CreateByItemID(442289265, 1).info, 1); // голографический прицел
            b1.contents.AddItem(ItemManager.CreateByItemID(952603248, 1).info, 1); // фонарик
            b1.MoveToContainer(player.inventory.containerBelt);
            //Инвентарь, патроны
            var ammo = (b1.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.itemid;
            Item m1 = ItemManager.CreateByItemID(ammo, 500, 0); //патроны
            m1.MoveToContainer(player.inventory.containerMain);
            PrintToChat(player, string.Format(GetMsg("BanGunGive", player)));
            ActiveAdmins.Add(player, reason);
        }
        private bool IsNPC(BasePlayer player)
        {
            //BotSpawn
            if (player is NPCPlayer)
                return true;
            //HumanNPC
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        private void ClearInv(BasePlayer player)
        {
            player.inventory.containerBelt.Clear();
            player.inventory.containerMain.Clear();
            player.inventory.containerWear.Clear();
            if (player.IsConnected) PrintToChat(player, string.Format(GetMsg("InvClear", player)));
            ActiveAdmins.Remove(player);
        }
        #endregion

        #region Commands
        [ChatCommand("bangun")]
        private void BanGunCmd(BasePlayer player, string cmd, string[] args)
        {
            //Использование команды: /bangun reason - включение режима, reason - причина всех следущих банов с использованием BunGun
            //                       /bangun - отключение режима, если включен.
            if (!permission.UserHasPermission(player.UserIDString, BunGunPerm))
            {
                PrintToChat(player, string.Format(GetMsg("NoPerm", player)));
            }
            if (ActiveAdmins.ContainsKey(player))
            {
                ClearInv(player);
            }
            else
            {
                if (args.Length > 0)
                {
                    string BanReason = string.Join(" ", args.Skip(0).ToArray());
                    GiveBanGun(player, BanReason);
                }
                else
                {
                    PrintToChat(player, string.Format(GetMsg("NoReason", player)));
                }
            }
        }
        #endregion

        #region Lang
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPerm", "<size=17><color=#049906>У вас нет прав для использования этой команды!</color></size>"},
                {"InvClear", "<size=17><color=#049906>Ваш инвентарь очищен.</color></size>"},
                {"BanGunGive", "<size=17><color=#049906>Набор для бана нарушителей успешно выдан!</color></size>"},
                {"NoReason", "<size=17><color=#049906>Нужно указать причину бана! команда /bangun reason</color></size>"},
                {"UserBanned", "<size=17>Игрок <color=#049906>{0}</color> забанен! Причина: {1}</size>"}
            }, this);
        }
        string GetMsg(string key, BasePlayer player = null) => GetMsg(key, player.UserIDString);
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}
