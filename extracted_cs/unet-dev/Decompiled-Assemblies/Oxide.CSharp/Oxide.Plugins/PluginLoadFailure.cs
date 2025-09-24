// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;

namespace Oxide.Plugins
{
	public class PluginLoadFailure : Exception
	{
		public PluginLoadFailure(string reason)
		{
		}
	}
}