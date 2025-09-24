using System;
using System.Text;
using Facepunch;
using Facepunch.Models.Database;
using Oxide.Game.Rust.Cui;
using UnityEngine.UIElements;

// Initialize an empty CuiElementContainer as that will act as parent for sub modules
// Stick Container to "Hud" Parent.
// Make API available to load modules
// Check for validate modules (Elements components usage should be correct)

namespace Oxide.Plugins
{
    [Info("Hud Panel Core", "Blitzmachine", "1.0.0")]
    public class HudPanelCore : RustPlugin
    {
        #region Definition
        private const string PARENT_ELEMENT_IDENTIFIER = "hudpanelcore.core";
        private const string PARENT_PANEL_IDENTIFIER = PARENT_ELEMENT_IDENTIFIER + ".panel";
        private CuiElementContainer _elementContainer = new();
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            InitializeParentHud();

            // TEST
            CuiElement cuiElement = new()
            {
                Name = PARENT_PANEL_IDENTIFIER + ".myModule",
                Parent = PARENT_PANEL_IDENTIFIER,
                Components =
                {
                    {
                        new CuiImageComponent() { Color = "55 55 55 1" }
                    },
                    {
                        new CuiRectTransformComponent() { AnchorMin = "0.4 0.8", AnchorMax = "0.6 0.2"}
                    }
                }
            };


        }
        private void Loaded()
        {
            ListHashSet<BasePlayer> players = BasePlayer.activePlayerList;
            for (int i = 0; i < players.Count; i++)
            {
                BasePlayer player = players[i];
                DisplayCoreHud(ref player);
            }
        }

        private void Unload()
        {
            ListHashSet<BasePlayer> players = BasePlayer.activePlayerList;
            for (int i = 0; i < players.Count; i++)
            {
                BasePlayer player = players[i];
                HideCoreHud(ref player);
            }
        }
        #endregion

        #region Oxide (Rust related) Hooks
        private void OnPlayerConnected(BasePlayer player) => DisplayCoreHud(ref player);
        private void OnPlayerDisconnected(BasePlayer player) => HideCoreHud(ref player);
        #endregion

        #region Internal Cui Helpers
        private void InitializeParentHud()
        {
            CuiElement cuiElement = new()
            {
                Name = PARENT_ELEMENT_IDENTIFIER,
                Parent = "Hud"
            };

            CuiPanel cuiPanel = new()
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.0 0.0", AnchorMax = "1.0 1.0" }
            };

            try
            {
                _elementContainer.Add(cuiElement);
                _elementContainer.Add(cuiPanel, PARENT_ELEMENT_IDENTIFIER, PARENT_PANEL_IDENTIFIER);
            }
            catch (Exception ex)
            {
                StringBuilder builder = new("Failed to initialize parent HUD");
                builder.AppendLine(ex.Message);
            }
        }

        private void AddModuleToCorePanel(ref CuiElement cuiElement)
        {
            try
            {
                _elementContainer.Add(cuiElement);
            }
            catch (Exception ex)
            {
                PrintError("Failed to add module to core panel!");
                PrintError(ex.Message);
            }
        }

        private void DisplayCoreHud(ref BasePlayer player)
        {
            if (IsPlayerActive(ref player))
            {
                CuiHelper.AddUi(player, _elementContainer);
            }
        }

        private void HideCoreHud(ref BasePlayer player)
        {
            if (IsPlayerActive(ref player))
            {
                CuiHelper.DestroyUi(player, PARENT_ELEMENT_IDENTIFIER);
            }
        }
        #endregion

        #region Internal Helpers
        private bool IsPlayerActive(ref BasePlayer player)
        {
            if (player is null)
                return false;

            if (!player.userID.IsSteamId())
                return false;

            if (!player.IsConnected)
                return false;
            return true;
        }
        #endregion

    }
}

