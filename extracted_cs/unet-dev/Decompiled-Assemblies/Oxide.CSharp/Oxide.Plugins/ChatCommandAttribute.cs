// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Runtime.CompilerServices;

namespace Oxide.Plugins
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ChatCommandAttribute : Attribute
	{
		public string Command
		{
			get;
			private set;
		}

		public ChatCommandAttribute(string command)
		{
			this.Command = command;
		}
	}
}