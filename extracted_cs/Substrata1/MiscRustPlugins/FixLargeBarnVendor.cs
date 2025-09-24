using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fix Large Barn Vendor", "Substrata", "1.0.0")]
    [Description("Fixes disappearing large barn vendor")]

    class FixLargeBarnVendor : RustPlugin
    {
        void OnServerInitialized()
        {
            CheckLargeBarnVendor();

            timer.Once(60f, () =>
            {
                CheckLargeBarnVendor();
            });
        }

        void CheckLargeBarnVendor()
        {
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if (monument.name == "assets/bundled/prefabs/autospawn/monument/small/stables_b.prefab")
                {
                    // Vending Machine
                    Vector3 vmPosition = monument.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(16.72f, 3.25f, -7.28f));
                    List<InvisibleVendingMachine> list = new List<InvisibleVendingMachine>();
                    Vis.Entities(vmPosition, 0.1f, list);
                    InvisibleVendingMachine vm = list.Count >= 1 ? list[0] : null;

                    // Shop Keeper
                    Vector3 shopKeeperPosition = monument.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(16.66f, 3.37f, -7.25f));
                    List<NPCShopKeeper> list2 = new List<NPCShopKeeper>();
                    Vis.Entities(shopKeeperPosition, 0.1f, list2);
                    NPCShopKeeper shopKeeper = list2.Count >= 1 ? list2[0] : null;

                    if (vm != null && shopKeeper != null) return;

                    Quaternion rotation = monument.transform.rotation * new Quaternion(0.00000f, -0.99978f, 0.00000f, 0.02116f);

                    if (vm == null)
                    {
                        vm = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab", vmPosition, rotation) as InvisibleVendingMachine;
                        if (vm != null)
                        {
                            NPCVendingOrder vendingOrders = null;

                            foreach (NPCVendingOrder order in vm.vmoManifest.orderList)
                            {
                                if (order.name == "stables")
                                {
                                    vendingOrders = order;
                                    break;
                                }
                            }

                            if (vendingOrders != null)
                            {
                                vm.shopName = "Stables Shopkeeper";
                                vm.vendingOrders = vendingOrders;
                                vm.SetFlag(BaseEntity.Flags.Reserved6, true);

                                if (shopKeeper != null)
                                {
                                    shopKeeper.machine = vm;
                                    shopKeeper.invisibleVendingMachineRef.Set(shopKeeper.machine);
                                    shopKeeper.machine.SetAttachedNPC(shopKeeper);
                                }

                                vm.Spawn();
                            }
                        }
                    }

                    if (shopKeeper == null)
                    {
                        shopKeeper = GameManager.server.CreateEntity("assets/prefabs/npc/bandit/shopkeepers/stables_shopkeeper.prefab", shopKeeperPosition, rotation) as NPCShopKeeper;
                        if (shopKeeper != null && vm != null)
                        {
                            shopKeeper.machine = vm;
                            shopKeeper.Spawn();
                        }
                    }
                }
            }
        }
    }
}