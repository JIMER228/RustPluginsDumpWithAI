namespace Oxide.Plugins
{
  [Info("CanPutAnythingInRefrigerators", "chucklenugget", "0.1.0")]
  public partial class CanPutAnythingInRefrigerators : RustPlugin
  {
    const string PREFAB = "fridge.deployed";

    void OnLootEntity(BasePlayer player, BaseEntity entity)
    {
      if (player == null || entity == null)
        return;

      var storage = entity as StorageContainer;

      if (storage == null && storage.inventory != null)
        return;

      if (storage.ShortPrefabName == PREFAB && storage.inventory.canAcceptItem != AcceptAnyItem)
        storage.inventory.canAcceptItem = AcceptAnyItem;
    }

    bool AcceptAnyItem(Item item, int targetPos)
    {
      return true;
    }
  }
}