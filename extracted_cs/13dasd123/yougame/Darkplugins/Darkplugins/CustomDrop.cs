// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CustomDrop", "https://discord.gg/dNGbxafuJn", "1.0.5")]
    [Description("Именные вещи у убитых игроков! Куплено на whiteplugins.ru")]
    public class CustomDrop : RustPlugin
    {
        #region Variables

        private string Permission = "CustomDrop.Use";
        private string Name = "<color=#DC143C>%P.NAME% %I.NAME%</color>";

        #endregion

        #region Hooks
        
        protected override void LoadDefaultConfig()
        {
            GetConfig("Основное", "Название привилегии, игроки с которой будут иметь именные вещи", ref Permission);
            GetConfig("Основное", "Формат названия именной вещи, %P.NAME% имя игрока, %I.NAME% название вещи, %C.TIME% время убийства", ref Name);
            
            SaveConfig();
        }


        private void OnServerInitialized()
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    if (this.Author != "Hougan" || this.Description != "Именные вещи у убитых игроков! Куплено на whiteplugins.ru") return;
            LoadDefaultConfig();
            
            permission.RegisterPermission(Permission, this);
        }

        #endregion

        #region Hooks

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    if (this.Author != "Hougan" || this.Description != "Именные вещи у убитых игроков! Куплено на whiteplugins.ru") return;
            if (!permission.UserHasPermission(player.UserIDString, Permission) || player.GetComponent<NPCPlayer>() != null)
                return;

            for (int i = 0; i < player.inventory.AllItems().Length; i++)
            {
                Item x = player.inventory.AllItems()[i];
                if (x.MaxStackable() != 1)
                    continue;

                string resultString = Name.Replace($"%P.NAME%", player.displayName)
                                          .Replace($"%I.NAME%", x.info.displayName.english)
                                          .Replace($"%C.TIME%", DateTime.Now.ToShortTimeString());
                
                x.name = resultString;
            }
        }

        #endregion

        #region Utils

        private void GetConfig<T>(string menu, string key, ref T varObject)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    if (this.Author != "Hougan" || this.Description != "Именные вещи у убитых игроков! Куплено на whiteplugins.ru") return;
            if (Config[menu, key] != null)
            {
                varObject = Config.ConvertValue<T>(Config[menu, key]);
            }
            else
            {
                Config[menu, key] = varObject;
            }
        }

        #endregion
    