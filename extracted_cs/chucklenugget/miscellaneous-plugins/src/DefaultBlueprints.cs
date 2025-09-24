namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;

  [Info("DefaultBlueprints", "chucklenugget", "0.1.0")]
  public partial class DefaultBlueprints : RustPlugin
  {
    List<ItemDefinition> ItemsToUnlock = new List<ItemDefinition>();

    void Loaded()
    {
      try
      {
        foreach (string prefab in Config.Get<List<string>>("blueprints"))
        {
          ItemDefinition itemDef = ItemManager.FindItemDefinition(prefab);

          if (itemDef == null)
            PrintWarning($"Unknown item added as default blueprint: {prefab}");
          else
            Puts($"Added {itemDef.name} to default blueprints");

          ItemsToUnlock.Add(itemDef);
        }
      }
      catch (Exception ex)
      {
        PrintError($"Error while loading item definitions for default blueprints: {ex.ToString()}");
      }

      foreach (BasePlayer player in BasePlayer.activePlayerList)
        UnlockBlueprints(player);
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.Clear();
      Config["blueprints"] = new List<string>();
      Config.Save();
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null)
        return;

      UnlockBlueprints(player);
    }

    void UnlockBlueprints(BasePlayer player)
    {
      foreach (ItemDefinition itemDef in ItemsToUnlock)
      {
        if (!player.blueprints.HasUnlocked(itemDef))
          player.blueprints.Unlock(itemDef);
      }
    }
  }
}