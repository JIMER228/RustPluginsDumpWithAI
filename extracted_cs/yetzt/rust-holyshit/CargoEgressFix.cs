using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins {
	[Info("Cargo Egress Fix", "yetzt", "1.0.0")]
	[Description("Prevent CargoShip from egressing until safe")]

	public class CargoEgressFix : RustPlugin {

		Timer checker;

		// check if position is safe
		bool safePos(Vector3 pos) {
			if (pos.x < 1500f && pos.x > -1100f && pos.z < 1100f && pos.z > -600f) return false;
			return true;
		}

		void OnEntitySpawned(CargoShip ship) {
			if (ship == null) return;

			// disable egress
			timer.In(5f, ()=>{
				ship.CancelInvoke(new Action(ship.StartEgress));
			});

			// start egress when safe
			timer.In((CargoShip.event_duration_minutes*60f), ()=>{
				checker?.Destroy();
				checker = timer.Every(1f, () => {
					if (safePos(ship.transform.position)) {
						ship.Invoke(new Action(ship.StartEgress), 1f);
						checker?.Destroy();
					} 
				});
			});

		}
		
	}
}