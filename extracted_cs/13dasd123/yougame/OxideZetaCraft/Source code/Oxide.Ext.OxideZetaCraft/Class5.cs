// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Reflection;
using Oxide.Core;

// Token: 0x0200000D RID: 13
internal static class Class5
{
	// Token: 0x06000053 RID: 83 RVA: 0x00004754 File Offset: 0x00002954
	static Assembly smethod_0()
	{
		return Assembly.GetExecutingAssembly();
	}

	// Token: 0x06000054 RID: 84 RVA: 0x00004768 File Offset: 0x00002968
	static AssemblyName smethod_1(Assembly assembly_0)
	{
		return assembly_0.GetName();
	}

	// Token: 0x06000055 RID: 85 RVA: 0x0000477C File Offset: 0x0000297C
	static Version smethod_2(AssemblyName assemblyName_0)
	{
		return assemblyName_0.Version;
	}

	// Token: 0x06000056 RID: 86 RVA: 0x00004790 File Offset: 0x00002990
	static int smethod_3(Version version_0)
	{
		return version_0.Major;
	}

	// Token: 0x06000057 RID: 87 RVA: 0x000047A4 File Offset: 0x000029A4
	static int smethod_4(Version version_0)
	{
		return version_0.Minor;
	}

	// Token: 0x06000058 RID: 88 RVA: 0x000047B8 File Offset: 0x000029B8
	static int smethod_5(Version version_0)
	{
		return version_0.Build;
	}

	// Token: 0x0400002E RID: 46
	public static VersionNumber versionNumber_0 = new VersionNumber((int)((ushort)Assembly.GetExecutingAssembly().GetName().Version.Major), (int)((ushort)Assembly.GetExecutingAssembly().GetName().Version.Minor), (int)((ushort)Assembly.GetExecutingAssembly().GetName().Version.Build));

	// Token: 0x0400002F RID: 47
	public const string string_0 = "ZetaCraft for Oxide";

	// Token: 0x04000030 RID: 48
	public const string string_1 = "ZetaCraft";

	// Token: 0x04000031 RID: 49
	public const string string_2 = "fermens";
}
