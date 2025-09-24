// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;

namespace Oxide.Plugins
{
	[AttributeUsage(AttributeTargets.Field)]
	public class OnlinePlayersAttribute : Attribute
	{
		public OnlinePlayersAttribute()
		{
		}
	}
}