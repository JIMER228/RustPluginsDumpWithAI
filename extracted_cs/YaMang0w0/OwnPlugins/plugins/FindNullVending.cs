using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/*
 * To find problematic Vending machines
 * It can be a Vending machine refill.
 * 
 * */
namespace Oxide.Plugins
{
    [Info("Find Null Vending", "YaMang -w-", "1.0.0")]
    [Description("Null VendingMachine Problem Solve Plugins")]
    public class FindNullVending : RustPlugin
    {
        #region Fields
        private Coroutine _vendingCoroutine = null;
        private bool destroyVending = false;
        private float startTimer = 120f;
        private float delayTimer = 1f;
        #endregion

        #region Hook
        int i = 0;
        void OnServerInitialized(bool initial)
        {
            timer.Once(startTimer, () =>
            {
                _vendingCoroutine = ServerMgr.Instance.StartCoroutine(FindNPCVending());
            });
        }

        void Unload()
        {
            if (_vendingCoroutine != null) ServerMgr.Instance.StopCoroutine(_vendingCoroutine);
        }


        private IEnumerator FindNPCVending()
        {
            var npcvending = BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().ToArray();
            Puts($"Find VendingMachine Count: {npcvending.Length}");
            foreach (var b in npcvending)
            {
                i++;
                Puts($"{i} ---start---");
                try
                {
                    b.Refill();
                    Puts("Ok");
                }
                catch (Exception ex) 
                {
                    string msg = $"{i} {b.transform.position} -";

                    if (!destroyVending)
                    {
                        msg += "Vending Machine has issue check !!";
                    }
                    else
                    {
                        msg += "Vending Machine has been Admin Killed";
                        b.AdminKill();
                    }

                    PrintWarning(msg);
                }

                Puts($"{i} ---end---");
                yield return CoroutineEx.waitForSeconds(delayTimer);
            }
        }

        #endregion
    }
}
