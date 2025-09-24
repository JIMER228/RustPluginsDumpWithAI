// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Runtime.CompilerServices;

namespace Oxide.Plugins
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DescriptionAttribute : Attribute
	{
		public string Description
		{
			get;
		}

		public DescriptionAttribute(string description)
		{
			this.Description = description;
		}
	}
}