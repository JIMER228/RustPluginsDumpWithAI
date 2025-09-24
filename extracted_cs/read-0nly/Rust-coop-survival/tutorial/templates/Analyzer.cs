using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using UnityEngine; 

namespace Oxide.Plugins
{
	[Info("Analyzer", "your-name-here", "0.0.1")]
	[Description("Server debugging tool for modders")]
	public class Analyzer : CovalencePlugin
	{
		private void OnServerInitialized()
        {
			//Set yourself up
		}
		private void OnItemUse(Item i, int n)
        {
			if(i.ToString().Contains("cactus")){
				//Do thing triggered by item use
			}
		}
		[Command("analyze")] 
		private void analyze(IPlayer player, string command, string[] args){	
			BasePlayer bp = (BasePlayer)player.Object;		
			
		}
		private BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
	}
}
