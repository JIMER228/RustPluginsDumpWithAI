using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Ext.CustomNpc.Gameplay.Components;
using Oxide.Ext.CustomNpc.Gameplay.Configurations;
using Oxide.Ext.CustomNpc.Gameplay.Managers;
using Oxide.Ext.CustomNpc.Gameplay.NpcCreator;
using Oxide.Ext.CustomNpc.PluginExntesions;
using Oxide.Game.Rust.Libraries;
using Oxide.Plugins;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Ext.CustomNpc
{
    [Info("CustomNpcPlugin", "Flutes", "0.1.0")]
    [Description("test")]
    public class CustomNpcPlugin : RustPlugin
    {
        private void Init()
        {
            new PluginsExtensionsManager();
            CustomNpc_Manager.Setup();

            Unsubscribe("OnInventoryNetworkUpdate");

        }

        private void Unload()
        {
            CustomNpc_Manager.DestroyAllNpcs();
            if (NpcCreator_Manager.IsStarted)
            {
                NpcCreator_Manager.Stop();

			}
        }

		protected override object InvokeMethod(HookMethod method, object[] args)
		{
			return method.Method.Invoke(this, args);
		}


		#region Oxide Hooks
		BaseCorpse OnCorpsePopulate(BasePlayer player, BaseCorpse corpse)
		{
            if (player == null || corpse == null)
                return null;

            ScientistNPC npc = player as ScientistNPC;
            if (npc == null)
                return null;

            NPCPlayerCorpse npcCorpse = corpse as NPCPlayerCorpse;
            if (npcCorpse == null)
                return null;

            CustomNpc_Manager.OnPopulateCorpse(npc, npcCorpse.containers[0]);

            if (NpcCreator_Manager.IsStarted)
            {
                NpcCreator_Manager.OnEntityKill(npc);
            }

            return null;
        }

        object OnInventoryNetworkUpdate(PlayerInventory inventory, ItemContainer container, ProtoBuf.UpdateItemContainer updateItemContainer, PlayerInventory.Type type, bool broadcast)
        {
            if (inventory.baseEntity == null)
                return null;

            if (NpcCreator_Manager.IsStarted)
            {
                if (NpcCreator_Manager.Controllers.ContainsKey(inventory.baseEntity.userID) == false)
                    return null;

                var controller = NpcCreator_Manager.Controllers[inventory.baseEntity.userID];
                controller.CopyWearToSelectedNpc();
            }

            return null;
        }
        #endregion


        [ChatCommand("customnpc_creator_start")]
        private void CustomNpcCreatorStartChatCommand(BasePlayer player, string command, string[] args)
        {
            Interface.Oxide.LogInfo("customnpc_creator_start");

            if (player.IsAdmin == false)
                return;

            if (args.Length == 0)
                return;

            string pluginName = args[0];
            var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);

            if (plugin == null)
            {
                player.ChatMessage($"Plugin {pluginName} not found");
                return;
            }

            if (NpcCreator_Manager.IsStarted == false)
                Subscribe("OnInventoryNetworkUpdate");

            NpcCreator_Manager.StartCreator(player, plugin);
            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_stop")]
        private void CustomNpcCreatorStopChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            NpcCreator_Manager.StopCreator(player);

            if (NpcCreator_Manager.IsStarted == false)
                Unsubscribe("OnInventoryNetworkUpdate");
        }

        [ChatCommand("customnpc_creator_stop_all")]
        private void CustomNpcCreatorStopAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            NpcCreator_Manager.Stop();

            Unsubscribe("OnInventoryNetworkUpdate");

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_edit")]
        private void CustomNpcCreatorEditChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.InstanceNpcCommand(args);

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_select")]
        private void CustomNpcCreatorSelectChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SelectNpcCommand();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_unselect")]
        private void CustomNpcCreatorUnselectChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.UnselectNpcCommand();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_save_all")]
        private void CustomNpcCreatorSaveAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SaveAllNpcConfig();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_save_selected")]
        private void CustomNpcCreatorSaveSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SaveSelectNpcConfig();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_reload_all")]
        private void CustomNpcCreatoReloadAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.ReloadAllNpcConfig();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_reload_selected")]
        private void CustomNpcCreatoReloadSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.ReloadSelectedNpcConfig();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_test_selected")]
        private void CustomNpcCreatorTestSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.TestSelect();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_test_all")]
        private void CustomNpcCreatorTestAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;


            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.TestAll();

            player.ChatMessage($"Success");
        }

        [ChatCommand("customnpc_creator_test_stop")]
        private void CustomNpcCreatorTestEndChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
                return;

            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
                return;

            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.StopTest();

            player.ChatMessage($"Success");
        }
    }
}
