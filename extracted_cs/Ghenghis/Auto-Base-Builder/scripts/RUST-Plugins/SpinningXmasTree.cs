// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
// Requires: ParentedEntityRenderFix

using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Spinning Xmas Tree", "Bazz3l", "1.1.0")]
    [Description("Add decorations to your tree and watch it spin with awesomeness.")]
    public class SpinningXmasTree : RustPlugin
    {
        #region Oxide Hooks

        private void OnServerInitialized()
        {
            PrintWarning("please wait for all trees to initialize.");

            timer.In(25f, () =>
            {
                SpinnerManagement.CreateAllSpinners();
                
                PrintWarning("all trees are now initialized.");
            });
        }

        private void Unload()
        {
            SpinnerManagement.RemoveAllSpinners();
        }

        private void OnEntityKill(ChristmasTree christmasTree)
        {
            SpinnerManagement.RemoveSpinner(christmasTree);
        }

        private void OnEntityBuilt(Planner plan, GameObject gameObject)
        {
            ChristmasTree christmasTree = gameObject.GetComponent<ChristmasTree>();
            if (christmasTree != null)
                NextFrame(() => SpinnerManagement.CreateSpinner(christmasTree));
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            ChristmasTree christmasTree = container?.entityOwner as ChristmasTree;
            if (christmasTree != null)
                christmasTree.GetParentEntity()
                    ?.GetComponent<TreeSpinnerController>()
                    ?.SetActive(container);
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            ChristmasTree christmasTree = container?.entityOwner as ChristmasTree;
            if (christmasTree != null)
                christmasTree.GetParentEntity()
                    ?.GetComponent<TreeSpinnerController>()
                    ?.SetActive(container);
        }

        #endregion
        
        #region Spawn Management

        private class SpinnerManagement
        {
            public static void CreateAllSpinners()
            {
                foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
                {
                    ChristmasTree christmasTree = entity as ChristmasTree;
                    if (christmasTree != null)
                        CreateSpinner(christmasTree);
                }
            }
            
            public static void RemoveAllSpinners()
            {
                foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
                {
                    ChristmasTree christmasTree = entity as ChristmasTree;
                    if (christmasTree != null)
                        RestoreEntity(christmasTree);
                }
            }
            
            public static void CreateSpinner(ChristmasTree christmasTree)
            {
                if (christmasTree == null || christmasTree.IsDestroyed)
                    return;

                if (christmasTree.HasParent())
                    return;

                DroppedItem droppedItem = CreateDroppedItem(christmasTree.transform.position);
                if (droppedItem == null) 
                    return;
                
                christmasTree.SetParent(droppedItem, true, true);
                
                droppedItem.GetOrAddComponent<TreeSpinnerController>()?.SetActive(christmasTree.inventory);
            }
            
            public static void RemoveSpinner(ChristmasTree christmasTree)
            {
                BaseEntity parent = christmasTree.GetParentEntity();
                if (parent != null && !parent.IsDestroyed)
                    parent.Kill();
            }

            public static void RestoreEntity(ChristmasTree christmasTree)
            {
                BaseEntity parent = christmasTree.GetParentEntity();
                
                christmasTree.SetParent(null, true, true);
                
                if (parent != null && !parent.IsDestroyed)
                    parent.Kill();
            }

            public static DroppedItem CreateDroppedItem(Vector3 position)
            {
                DroppedItem droppedItem = ItemManager.CreateByName("rock", 1)?.Drop(position, Vector3.zero)?.GetComponent<DroppedItem>();
                if (droppedItem == null)
                    return null;
                
                Rigidbody rigidbody = droppedItem.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                }

                droppedItem.syncPosition = true;
                droppedItem.enableSaving = false;
                droppedItem.allowPickup = false;
                droppedItem.OwnerID = 1717;
                droppedItem.CancelInvoke(droppedItem.IdleDestroy);

                return droppedItem;
            }
        }

        private class TreeSpinnerController : MonoBehaviour
        {
            public float spinSpeed = 10f;
            public bool shouldSpin;

            private void FixedUpdate()
            {
                if (!shouldSpin) 
                    return;
                
                transform.Rotate(0, Time.fixedDeltaTime * spinSpeed, 0);
                transform.hasChanged = true;
            }

            public void SetActive(ItemContainer container)
            {
                shouldSpin = container?.itemList != null && container.itemList.Count > 0;
            }
        }

        #endregion
    }
}
 