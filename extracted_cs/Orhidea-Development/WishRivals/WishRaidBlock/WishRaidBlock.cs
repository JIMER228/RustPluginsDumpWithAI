using Oxide.Core;
using System;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("WishRaidBlock", "Latvish", "0.0.1")]
    [Description("WishRaidBlock")]
    public partial class WishRaidBlock : RustPlugin
    {
        const string adminPriv = "Raidblock.admin";
        private RaidBlockService _raidBlockService;
        void Init()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Init WishRaidBlock");

            LoadConfig();
            _raidBlockService = new RaidBlockService(Config);
            LoadRaidBlock();
            SubscribeToEvents();

            stopwatch.Stop();
            GuiService guiService = new GuiService();
            timer.Every(30, () =>
            {
                _raidBlockService.UpdateIsOnUsingConfigTime();
                if (_raidBlockService.IsOn())
                {
                    Interface.Oxide.LogDebug("Raidlock active, enabling UI");

                    guiService.ActivateGui();
                }
                else
                {
                    Interface.Oxide.LogDebug("Raidlock disabled, destroying UI");
                    guiService.DestroyGui();
                }
            });
            Interface.Oxide.LogDebug($"END Init WishRaidBlock {stopwatch.ElapsedMilliseconds}ms");

        }
        void LoadRaidBlock()
        {
            try
            {
                RegisterMessages();

                if ((bool)Config["RaidBlockOn"])
                {
                    PrintWarning("Raid block starts at " + ShowTime(_raidBlockService.GetStartTime()));
                    PrintWarning("Raid block ends at  " + ShowTime(_raidBlockService.GetEndTime()));
                }
                else
                {
                    PrintWarning("Raid block is off.");
                }
                permission.RegisterPermission(adminPriv, this);

                PrintWarning(adminPriv + " privilidge is registered.");

            }
            catch (Exception ex)
            {
                PrintError($"Error Loaded: {ex.StackTrace}");
            }

        }
        protected override void LoadDefaultConfig() // Only called if the config file does not already exist
        {
            Interface.Oxide.LogWarning("Creating a new configuration file.");

            Config.Clear();

            Config["RaidBlockStart"] = "23:30";           // 8:30 AM
            Config["RaidBlockEnd"] = "12:00";
            Config["RaidBlockOn"] = true;
            Config["RaidBlockInformPlayer"] = true;
            SaveConfig();
        }
        private void RegisterMessages()
        {
            MessageRegister messageRegister = new MessageRegister(this);
            messageRegister.Register();
        }

        private void SubscribeToEvents()
        {
            Subscribe("OnEntityTakeDamage");
        }

        public string ShowTime(TimeSpan TimeIn)
        {
            return TimeIn.ToString(@"hh\:mm");
        }
    }
}
