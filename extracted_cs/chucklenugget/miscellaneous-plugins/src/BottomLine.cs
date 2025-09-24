namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using Oxide.Game.Rust.Cui;
  using UnityEngine;
  using Newtonsoft.Json;

  [Info("BottomLine", "chucklenugget", "0.1.0")]
  public partial class BottomLine : RustPlugin
  {
    const string UI_HUD = "Hud";
    const string UI_PANEL = "BottomLine.Panel";
    const string UI_TEXT = "BottomLine.Text";
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    BottomLineUiManager UiManager;
    BottomLineOptions Options;

    void Loaded()
    {
      Options = new BottomLineOptions();

      try
      {
        Options = Config.ReadObject<BottomLineOptions>();
      }
      catch (Exception ex)
      {
        PrintError($"Error while loading configuration: {ex.ToString()}");
      }

      UiManager = new GameObject().AddComponent<BottomLineUiManager>();
      UiManager.Init(this);
    }

    void Unload()
    {
      UnityEngine.Object.DestroyImmediate(UiManager);
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");

      var options = new BottomLineOptions();
      options.Messages.Add("Please configure BottomLine!");
      options.DurationSeconds = 10f;

      Config.WriteObject(new BottomLineOptions(), true);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;

      // If the player hasn't fully connected yet, try again in 2 seconds.
      if (player.IsReceivingSnapshot)
      {
        timer.In(2, () => OnPlayerInit(player));
        return;
      }

      UiManager.Add(player);
    }

    void OnPlayerDisconnected(BasePlayer player)
    {
      if (player != null)
        UiManager.Remove(player);
    }

    class BottomLineOptions
    {
      [JsonProperty("messages")]
      public List<string> Messages = new List<string>();

      [JsonProperty("durationSeconds")]
      public float DurationSeconds;
    }

    class BottomLineUiManager : MonoBehaviour
    {
      BottomLine Core;
      int CurrentMessage;

      public void Init(BottomLine core)
      {
        Core = core;
        CurrentMessage = 0;

        foreach (BasePlayer player in BasePlayer.activePlayerList)
          Add(player);

        InvokeRepeating("UpdateHudForAllPlayers", Core.Options.DurationSeconds, Core.Options.DurationSeconds);
      }

      void OnDestroy()
      {
        if (IsInvoking("UpdateHudForAllPlayers"))
          CancelInvoke("UpdateHudForAllPlayers");

        foreach (BasePlayer player in BasePlayer.activePlayerList)
          Remove(player);
      }

      public void Add(BasePlayer player)
      {
        Remove(player);

        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0", Sprite = UI_TRANSPARENT_TEXTURE },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.022" }
        }, UI_HUD, UI_PANEL);

        container.Add(new CuiLabel {
          Text = {
            Text = Core.Options.Messages[CurrentMessage],
            Color = "0.85 0.85 0.85 1",
            FontSize = 9,
            Align = TextAnchor.MiddleCenter
          },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
        }, UI_PANEL, UI_TEXT);

        CuiHelper.AddUi(player, container);
      }

      public void Remove(BasePlayer player)
      {
        CuiHelper.DestroyUi(player, UI_PANEL);
      }

      void UpdateHudForAllPlayers()
      {
        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
          Remove(player);
          Add(player);
        }

        CurrentMessage = (CurrentMessage + 1) % Core.Options.Messages.Count;
      }
    }
  }
}