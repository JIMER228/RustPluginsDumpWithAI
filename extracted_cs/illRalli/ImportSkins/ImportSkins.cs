using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ImportSkins", "illRalli", "1.0.2")]
    [Description("A plugin that automates sending the /addskin command for each skin ID placed in the data file with a delay between each message")]

    public class ImportSkins : RustPlugin
    {
        private class SkinItem
        {
            public string ItemShortname { get; set; } 
            public string Permission { get; set; }
            public List<long> Skins { get; set; }
        }

        private class RootObject
        {
            public List<SkinItem> Skins { get; set; }
        }

        private RootObject skinData;

        // Delay between each chat message in seconds
        private const float delayBetweenMessages = 1.0f;

        private const string AdminPermission = "importskins.admin";

        void Init()
        {
            LoadData();
            permission.RegisterPermission(AdminPermission, this);
            Puts($"ImportSkins loaded with {skinData?.Skins.Count} items.");
        }

        void LoadData()
        {
            try
            {
                skinData = Interface.Oxide.DataFileSystem.ReadObject<RootObject>("ImportSkins/import");
            }
            catch
            {
                skinData = null;
            }

            if (skinData?.Skins == null || !skinData.Skins.Any())
            {
                Puts("No skins found in the data file, generating default skin data.");

                skinData = new RootObject
                {
                    Skins = new List<SkinItem>
                    {
                        new SkinItem
                        {
                            ItemShortname = "hoodie",
                            Permission = "",
                            Skins = new List<long>  
                            {
                                123456, 7891011, 112131415
                            }
                        }
                    }
                };

                Interface.Oxide.DataFileSystem.WriteObject("ImportSkins/import", skinData);
                Puts("Default skin data file created.");
            }
        }

        [ChatCommand("importskins")]
        void ImportSkinsCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            if (skinData?.Skins == null || !skinData.Skins.Any())
            {
                player.ChatMessage("No skins available to import.");
                return;
            }

            int index = 0;
            int totalSkins = skinData.Skins.Sum(item => item.Skins.Count);

            foreach (var item in skinData.Skins)
            {
                foreach (var skinID in item.Skins)
                {
                    var currentSkinID = skinID;

                    timer.Once(delayBetweenMessages * index, () =>
                    {
                        string message = $"/addskin {currentSkinID}";
                        rust.RunClientCommand(player, $"chat.say \"{message}\"");

                        if (index == totalSkins - 1)
                        {
                            player.ChatMessage("All skins have been successfully imported!");
                        }
                    });

                    index++;
                }
            }
        }
    }
}
