namespace Oxide.Plugins
{
	[Info("Lethal Headshots", "turner#7777", "1.0.1")]
	[Description("Add some realism eh?")]
	public class LethalHeadshots : RustPlugin
	{
		void Init()
		{
			permission.RegisterPermission("lethalheadshots.use", this);
		}

		object OnPlayerAttack(BasePlayer attacker, HitInfo info)
		{
			if (info.isHeadshot && permission.UserHasPermission(attacker.UserIDString, "lethalheadshots.use"))
				info.damageTypes.ScaleAll(1000000f);
			
			return null;
		}
	}
}
 