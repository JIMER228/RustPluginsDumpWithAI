using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace ZetaCraft.Oxide
{
	// Token: 0x02000018 RID: 24
	public class OxideZetaCraftPluginLoader : PluginLoader
	{
		// Token: 0x06000138 RID: 312 RVA: 0x000077D8 File Offset: 0x000059D8
		public OxideZetaCraftPluginLoader(OxideZetaCraftExtension oxideZetaCraftExtension_1)
		{
			this.oxideZetaCraftExtension_0 = oxideZetaCraftExtension_1;
			this.logger_0 = Interface.GetMod().RootLogger;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00007804 File Offset: 0x00005A04
		public override IEnumerable<string> ScanDirectory(string directory)
		{
			return new string[]
			{
				"ZetaCraft"
			};
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00007824 File Offset: 0x00005A24
		public override Plugin Load(string directory, string name)
		{
			Plugin result;
			if (!(name == "ZetaCraft"))
			{
				result = null;
			}
			else
			{
				OxideZetaCraft oxideZetaCraft = new OxideZetaCraft(this.oxideZetaCraftExtension_0);
				try
				{
					FieldInfo field = base.GetType().GetField("LoadedPlugins", BindingFlags.Instance | BindingFlags.Public);
					if (field != null)
					{
						((Dictionary<string, Plugin>)field.GetValue(this)).Add(name, oxideZetaCraft);
					}
				}
				catch
				{
				}
				result = oxideZetaCraft;
			}
			return result;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000789C File Offset: 0x00005A9C
		public override void Unloading(Plugin plugin)
		{
			OxideZetaCraft oxideZetaCraft = plugin as OxideZetaCraft;
			if (oxideZetaCraft != null && oxideZetaCraft.bool_0)
			{
				oxideZetaCraft.bool_0 = false;
			}
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00004CD0 File Offset: 0x00002ED0
		static OxideMod smethod_0()
		{
			return Interface.GetMod();
		}

		// Token: 0x0600013D RID: 317 RVA: 0x000051CC File Offset: 0x000033CC
		static CompoundLogger smethod_1(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootLogger;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x000078C8 File Offset: 0x00005AC8
		static bool smethod_2(string string_0, string string_1)
		{
			return string_0 == string_1;
		}

		// Token: 0x0600013F RID: 319 RVA: 0x000078DC File Offset: 0x00005ADC
		static Type smethod_3(object object_0)
		{
			return object_0.GetType();
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00004E78 File Offset: 0x00003078
		static FieldInfo smethod_4(Type type_0, string string_0, BindingFlags bindingFlags_0)
		{
			return type_0.GetField(string_0, bindingFlags_0);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x000078F0 File Offset: 0x00005AF0
		static bool smethod_5(FieldInfo fieldInfo_0, FieldInfo fieldInfo_1)
		{
			return fieldInfo_0 != fieldInfo_1;
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00004E90 File Offset: 0x00003090
		static object smethod_6(FieldInfo fieldInfo_0, object object_0)
		{
			return fieldInfo_0.GetValue(object_0);
		}

		// Token: 0x0400004A RID: 74
		private OxideZetaCraftExtension oxideZetaCraftExtension_0;

		// Token: 0x0400004B RID: 75
		private Logger logger_0;
	}
}
