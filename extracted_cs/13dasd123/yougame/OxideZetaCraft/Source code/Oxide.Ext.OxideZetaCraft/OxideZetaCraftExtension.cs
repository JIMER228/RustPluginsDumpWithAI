// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace ZetaCraft.Oxide
{
	// Token: 0x0200000F RID: 15
	public class OxideZetaCraftExtension : Extension
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00005050 File Offset: 0x00003250
		public override string Name
		{
			get
			{
				return "ZetaCraft";
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00005064 File Offset: 0x00003264
		public override VersionNumber Version
		{
			get
			{
				return Class5.versionNumber_0;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00005078 File Offset: 0x00003278
		public override string Author
		{
			get
			{
				return "fermenspwnz";
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600008F RID: 143 RVA: 0x0000508C File Offset: 0x0000328C
		// (set) Token: 0x06000090 RID: 144 RVA: 0x000050A0 File Offset: 0x000032A0
		public static OxideZetaCraftExtension Instance { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000091 RID: 145 RVA: 0x000050B4 File Offset: 0x000032B4
		// (set) Token: 0x06000092 RID: 146 RVA: 0x000050C8 File Offset: 0x000032C8
		public OxideZetaCraftLibrary Library { get; private set; }

		// Token: 0x06000093 RID: 147 RVA: 0x000050DC File Offset: 0x000032DC
		public OxideZetaCraftExtension(ExtensionManager extensionManager_0) : base(extensionManager_0)
		{
			this.compoundLogger_0 = Interface.GetMod().RootLogger;
			OxideZetaCraftExtension.Instance = this;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00005108 File Offset: 0x00003308
		public void Log(string message)
		{
			this.compoundLogger_0.Write(2, message, Array.Empty<object>());
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00005128 File Offset: 0x00003328
		public override void Load()
		{
			base.Manager.RegisterPluginLoader(new OxideZetaCraftPluginLoader(this));
			base.Manager.RegisterLibrary("ZetaCraft", this.Library = new OxideZetaCraftLibrary(this));
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00005168 File Offset: 0x00003368
		internal bool method_0(string string_0, params object[] object_0)
		{
			Module module_ = Module.module_0;
			return module_ != null && module_.Call(string_0, object_0);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00005190 File Offset: 0x00003390
		internal bool method_1(string string_0, out object object_0, params object[] object_1)
		{
			object_0 = null;
			Module module_ = Module.module_0;
			return module_ != null && module_.Call(string_0, out object_0, object_1);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x000051BC File Offset: 0x000033BC
		public override void LoadPluginWatchers(string s)
		{
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000051BC File Offset: 0x000033BC
		public override void OnModLoad()
		{
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00004CD0 File Offset: 0x00002ED0
		static OxideMod smethod_0()
		{
			return Interface.GetMod();
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000051CC File Offset: 0x000033CC
		static CompoundLogger smethod_1(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootLogger;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x000051E0 File Offset: 0x000033E0
		static void smethod_2(Logger logger_0, LogType logType_0, string string_0, object[] object_0)
		{
			logger_0.Write(logType_0, string_0, object_0);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000051F8 File Offset: 0x000033F8
		static ExtensionManager smethod_3(Extension extension_0)
		{
			return extension_0.Manager;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x0000520C File Offset: 0x0000340C
		static void smethod_4(ExtensionManager extensionManager_0, PluginLoader pluginLoader_0)
		{
			extensionManager_0.RegisterPluginLoader(pluginLoader_0);
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00005220 File Offset: 0x00003420
		static void smethod_5(ExtensionManager extensionManager_0, string string_0, Library library_0)
		{
			extensionManager_0.RegisterLibrary(string_0, library_0);
		}

		// Token: 0x04000033 RID: 51
		private CompoundLogger compoundLogger_0;

		// Token: 0x04000034 RID: 52
		[CompilerGenerated]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static OxideZetaCraftExtension oxideZetaCraftExtension_0;

		// Token: 0x04000035 RID: 53
		[CompilerGenerated]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private OxideZetaCraftLibrary oxideZetaCraftLibrary_0;
	}
}
