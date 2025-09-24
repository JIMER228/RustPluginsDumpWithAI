// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;
using System.Linq;
using System.Collections.Generic;
using RustProto;
using Oxide.Core.Libraries;

// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

namespace Oxide.Plugins
{
    [Info("RNDFix", "lutSEfer", "1.0.0")]
    class RNDFix : RustLegacyPlugin
    {
				public double distFix { get; set; } = 0;

		void OnItemDeployed(DeployableObject component, IDeployableItem item)
		{			
				distFix = Math.Floor(Vector3.Distance(component.transform.position, item.character.playerClient.lastKnownPosition));
				if (component.gameObject.name == "Barricade_Fence_Deployable(Clone)"||component.gameObject.name == "Furnace(Clone)")
					if (distFix<=1)
				{
				SendReply(item.character.playerClient.netUser, "[color#FF4500]      ![color#CDB38B]");
				item.character.GetComponent<Inventory>().AddItemAmount(item.datablock, 1);
				timer.Once(0.01f, () => NetCull.Destroy(component.gameObject));
				}
				if (component.gameObject.name.Contains("Shelter"))
				{
				Vector3 lastPosition = component.transform.position;
				foreach (Collider collider in Physics.OverlapSphere(lastPosition, 3f))
				{
					if (collider.gameObject.name.Contains("Door"))					
					{
						SendReply(item.character.playerClient.netUser, "[color#FF4500]  ![color#CDB38B]");
						item.character.GetComponent<Inventory>().AddItemAmount(item.datablock, 1);
						timer.Once(0.01f, () => NetCull.Destroy(component.gameObject));
					}
				}
				}
		}
	}
}