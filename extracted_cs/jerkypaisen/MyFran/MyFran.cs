
using Oxide.Core;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;


namespace Oxide.Plugins
{
    [Info("MyFran", "jerkypaisen", "1.0.0")]
    [Description("This is MyFran.")]
    class MyFran : RustPlugin
    {
        public void SpawnFrankenstein(BasePlayer owner)
        {
            Vector3 playerPos = owner.transform.position;
            Vector3 position = Vector3.zero;
            position = new Vector3(playerPos.x + 1, 0, playerPos.z + 1);
            position.y = TerrainMeta.HeightMap.GetHeight(position);

            BaseEntity baseEntity = GameManager.server.CreateEntity("assets/rust.ai/agents/npcplayer/pet/frankensteinpet.prefab", position, Quaternion.identity, startActive: false);
   
            baseEntity.enableSaving = false;
            PoolableEx.AwakeFromInstantiate(baseEntity.gameObject);
            baseEntity.Spawn();
            
            BasePet frankenstein = baseEntity as FrankensteinPet;
            frankenstein.inventory.GiveItem(ItemManager.CreateByItemID(-297099594, 1, 0uL), frankenstein.inventory.containerWear);
            frankenstein.inventory.GiveItem(ItemManager.CreateByItemID(1614528785, 1, 0uL), frankenstein.inventory.containerWear);
            frankenstein.inventory.GiveItem(ItemManager.CreateByItemID(-2024549027, 1, 0uL), frankenstein.inventory.containerWear);
            frankenstein.inventory.GiveItem(ItemManager.CreateByItemID(-1469578201, 1, 0uL), frankenstein.inventory.containerBelt);
            frankenstein.EquipWeapon();

            baseEntity.SendNetworkUpdateImmediate();
            owner.SendNetworkUpdateImmediate();

            //FrankensteinBrain fb = baseEntity.GetComponent<FrankensteinBrain>();
            //fb.Start();
            //frankenstein.Brain.Events = fb.Events;

            frankenstein.Brain.Events = new AIEvents();
            frankenstein.Brain.Events.Memory = new AIMemory();

            frankenstein.ApplyPetStatModifiers();
            frankenstein.Brain.SetOwningPlayer(owner);
            frankenstein.CreateMapMarker();
            owner.SendClientPetLink();
            owner.ClientRPC(null, "CL_WakeFrankenstein");
        }

        [ConsoleCommand("myfran")]
        private void Cmd(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                BasePlayer basePlayer = null;
                ulong id = 0;
                if (arg.Connection != null && arg.Args == null)
                {
                    basePlayer = ArgEx.Player(arg);
                }
                else if (arg.Args != null && ulong.TryParse(arg.Args[0], out id))
                {
                    basePlayer = BasePlayer.FindByID(id);
                }
                if (basePlayer != null && basePlayer.PetEntity == null) SpawnFrankenstein(basePlayer);
            }
        }
    }
}
