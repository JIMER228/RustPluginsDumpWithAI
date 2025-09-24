using System;
using Facepunch;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealSleigh", "Kira", "1.0.0")]
    public class ZealSleigh : RustPlugin
    {
        private static ZealSleigh _;

        private void OnServerInitialized()
        {
            _ = this;
        }

        private void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            var sled = entity.GetComponentInParent<BaseEntity>() as Sled;
            if (!(sled == null)) return;
            var entMounted = entity.GetComponentInParent<Sled>(); 
            if (entMounted == null) return;
            var component = entMounted.gameObject.AddComponent<SledComponent>();
            component.player = player;
            PrintWarning("Add component");
        }

        public class SledComponent : MonoBehaviour
        {
            public Sled Sled;
            public Rigidbody Rigidbody;
            public BasePlayer player;
 
            private void Awake()
            {
                Sled = GetComponent<Sled>();
                Rigidbody = Sled.GetComponent<Rigidbody>();
            }

            private void FixedUpdate()
            {
                 
                var inputState = player.serverInput;
                float num1 = (float) ((inputState.IsDown(BUTTON.LEFT) ? -1.0 : 0.0) + (inputState.IsDown(BUTTON.RIGHT) ? 1.0 : 0.0));
                float num2 = num1 * Sled.TurnForce;
                if (inputState.IsDown(BUTTON.FORWARD))
                {
                    Rigidbody.AddForce(Sled.transform.forward * Sled.NudgeForce, ForceMode.Acceleration);
                    Sled.SendNetworkUpdate_Position();
                }
                
                if (inputState.IsDown(BUTTON.BACKWARD))
                {
                    Rigidbody.AddForce(Sled.transform.up * Sled.NudgeForce, ForceMode.Acceleration);
                    Sled.SendNetworkUpdate_Position();
                }
                
                if (inputState.IsDown(BUTTON.SPRINT))
                {
                    Rigidbody.AddForce(Sled.transform.up * Sled.NudgeForce, ForceMode.Acceleration);
                    Sled.SendNetworkUpdate_Position();
                }

                Sled.transform.Rotate(Vector3.up * num2 * Time.deltaTime * 5f, Space.Self);
                Sled.SendNetworkUpdate_Position();
            }
        }
    }
}