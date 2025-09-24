using UnityEngine;

namespace Oxide.Plugins {
	[Info("CargoShipFix", "rever", "1.0.0", ResourceId = 7702011)]
	[Description("Fix a problem when CargoShip drags a player outside of him")]
	class CargoShipFix : RustPlugin {
		void UnmointFromCargoShip(BasePlayer player, CargoShip cargoShip) {
			var currPosition = player.transform.position;
			cargoShip.RemoveChild(player);
			cargoShip.UpdateNetworkGroup();
			cargoShip.SendNetworkUpdateImmediate();
			player.SetParent(null);
			player.transform.position = currPosition;
		}

		object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled) {
			if (player == null || !player.IsConnected) return null;

			var cargoShip = player.GetComponentInParent<CargoShip>();
			if (cargoShip == null) return null;

			var cargoDistance = Vector3.Distance(cargoShip.transform.position, player.transform.position);
			if (cargoDistance < 120f) return null;

			UnmointFromCargoShip(player, cargoShip);

			return null;
		}
	}
}
