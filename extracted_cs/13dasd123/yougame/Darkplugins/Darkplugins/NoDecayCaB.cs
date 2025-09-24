using Rust;

namespace Oxide.Plugins
{
	/*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("NoDecayCaB", "https://discord.gg/dNGbxafuJn", "1.0.2")]
	class NoDecayCaB : RustPlugin
	{
		private void OnEntityTakeDamage(BaseCombatEntity combatentity, HitInfo hitinfo)
		{
			if ((combatentity is MiniCopter || combatentity is MotorRowboat || combatentity is RidableHorse) && hitinfo != null)
			{
				if (hitinfo.damageTypes.Get(DamageType.Decay) > 0)
				{
					BaseEntity entity = combatentity as BaseEntity;
					if (entity != null)
					{
						BuildingPrivlidge buildingprivlidge = entity.GetBuildingPrivilege();
						if (buildingprivlidge != null)
						{
							hitinfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
						}
					}
				}
			}
		}
	}
}