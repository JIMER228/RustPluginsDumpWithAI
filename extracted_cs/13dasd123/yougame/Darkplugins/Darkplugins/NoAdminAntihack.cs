// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins {
	[Info("NoAdminAntihack", "rever", "0.1.1", ResourceId = 7709904)]
	[Description("NoAdminAntihack")]
	class NoAdminAntihack : RustPlugin {
		object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount) {
			if (player != null && player.IsAdmin) return false;

			return null;
		}
	}
}
