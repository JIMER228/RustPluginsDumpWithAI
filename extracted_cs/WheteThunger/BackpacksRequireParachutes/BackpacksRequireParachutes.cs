using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Backpacks Require Parachutes", "WhiteThunder", "0.1.0")]
    [Description("Requires players to have a parachute equipped in order to access their backpack.")]
    internal class BackpacksRequireParachutes : CovalencePlugin
    {
        #region Fields

        private const string PermissionBypass = "backpacksrequireparachutes.bypass";
        private const int ParachuteItemId = 602628465;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionBypass, this);
        }

        private object CanOpenBackpack(BasePlayer looter, ulong ownerId)
        {
            // Don't block admins from accessing other players' backpacks.
            if (looter.userID != ownerId)
                return null;

            // Allow users with the bypass permission to not require a parachute.
            if (permission.UserHasPermission(looter.UserIDString, PermissionBypass))
                return null;

            if (IsWearingItem(looter, ParachuteItemId))
                return null;

            return GetMessage(looter.UserIDString, LangEntry.RequiresParachute);
        }

        #endregion

        #region Helpers

        private static bool IsWearingItem(BasePlayer player, int itemId)
        {
            foreach (var item in player.inventory.containerWear.itemList)
            {
                if (item.info.itemid == itemId)
                    return true;
            }

            return false;
        }

        #endregion

        #region Localization

        private class LangEntry
        {
            public static readonly List<LangEntry> AllLangEntries = new List<LangEntry>();

            public static readonly LangEntry RequiresParachute = new LangEntry("RequiresParachute", "You must equip a parachute equipped to access your backpack.");

            public string Name;
            public string English;

            public LangEntry(string name, string english)
            {
                Name = name;
                English = english;

                AllLangEntries.Add(this);
            }
        }

        private string GetMessage(string playerId, LangEntry langEntry) =>
            lang.GetMessage(langEntry.Name, this, playerId);

        private void ChatMessage(BasePlayer player, LangEntry langEntry) =>
            player.ChatMessage(GetMessage(player.UserIDString, langEntry));

        protected override void LoadDefaultMessages()
        {
            var englishLangKeys = new Dictionary<string, string>();

            foreach (var langEntry in LangEntry.AllLangEntries)
            {
                englishLangKeys[langEntry.Name] = langEntry.English;
            }

            lang.RegisterMessages(englishLangKeys, this, "en");
        }

        #endregion
    }
}
