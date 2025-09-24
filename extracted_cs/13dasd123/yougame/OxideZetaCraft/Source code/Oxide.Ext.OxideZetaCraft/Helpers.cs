// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace ZetaCraft.Oxide
{
	// Token: 0x0200000E RID: 14
	public static class Helpers
	{
		// Token: 0x06000059 RID: 89 RVA: 0x000047CC File Offset: 0x000029CC
		public static Dictionary<string, string> GetDirectories()
		{
			return new Dictionary<string, string>
			{
				{
					"plugin",
					Interface.GetMod().PluginDirectory
				},
				{
					"extension",
					Interface.GetMod().ExtensionDirectory
				},
				{
					"config",
					Interface.GetMod().ConfigDirectory
				},
				{
					"data",
					Interface.GetMod().DataDirectory
				},
				{
					"log",
					Interface.GetMod().LogDirectory
				},
				{
					"root",
					Interface.GetMod().RootDirectory
				}
			};
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004860 File Offset: 0x00002A60
		public static JArray GetPlugins()
		{
			OxideMod mod = Interface.GetMod();
			PluginManager rootPluginManager = mod.RootPluginManager;
			JArray jarray = new JArray();
			List<string> list = new List<string>();
			foreach (Plugin plugin in rootPluginManager.GetPlugins())
			{
				JObject jobject = new JObject();
				jobject["id"] = plugin.ResourceId;
				jobject["name"] = plugin.Name;
				jobject["title"] = plugin.Title;
				jobject["version"] = plugin.Version.ToString();
				jobject["author"] = plugin.Author;
				try
				{
					string path = (string)plugin.Object.GetType().GetField("Filename", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField).GetValue(plugin.Object);
					string fileName = Path.GetFileName(path);
					list.Add(fileName);
					jobject["filename"] = fileName;
					try
					{
						jobject["source"] = File.ReadAllText(path, Encoding.UTF8);
					}
					catch
					{
					}
					goto IL_188;
				}
				catch
				{
					goto IL_188;
				}
				goto IL_143;
				IL_17B:
				jarray.Add(jobject);
				continue;
				IL_143:
				JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
				jsonSerializerSettings.Converters.Add(new KeyValuesConverter());
				jobject["config"] = JsonConvert.SerializeObject(plugin.Config, 1, jsonSerializerSettings);
				goto IL_17B;
				IL_188:
				if (plugin.HasConfig)
				{
					goto IL_143;
				}
				goto IL_17B;
			}
			foreach (string text in Directory.GetFiles(mod.PluginDirectory))
			{
				if (Helpers.regex_0.IsMatch(text))
				{
					string fileName2 = Path.GetFileName(text);
					if (!list.Contains(fileName2))
					{
						JObject jobject2 = new JObject();
						jobject2["filename"] = fileName2;
						string path2 = Path.Combine(mod.PluginDirectory, fileName2);
						if (File.Exists(path2))
						{
							try
							{
								jobject2["source"] = File.ReadAllText(path2, Encoding.UTF8);
								goto IL_278;
							}
							catch
							{
								goto IL_278;
							}
							goto IL_242;
						}
						goto IL_278;
						IL_265:
						jarray.Add(jobject2);
						goto IL_26D;
						IL_278:
						string path3 = Path.Combine(mod.ConfigDirectory, Path.GetFileNameWithoutExtension(fileName2) + ".json");
						if (!File.Exists(path3))
						{
							goto IL_265;
						}
						IL_242:
						try
						{
							jobject2["config"] = File.ReadAllText(path3, Encoding.UTF8);
						}
						catch
						{
						}
						goto IL_265;
					}
				}
				IL_26D:;
			}
			return jarray;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004B90 File Offset: 0x00002D90
		public static bool InstallPlugin(string filename, byte[] source)
		{
			filename = Path.Combine(Interface.GetMod().PluginDirectory, Path.GetFileName(filename));
			bool result;
			try
			{
				File.WriteAllBytes(filename, source);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00004BD8 File Offset: 0x00002DD8
		public static bool InstallConfig(string filename, string source)
		{
			filename = Path.Combine(Interface.GetMod().ConfigDirectory, Path.GetFileName(filename));
			bool result;
			try
			{
				File.WriteAllText(filename, source, Encoding.UTF8);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00004C24 File Offset: 0x00002E24
		public static bool UninstallPlugin(string filename, bool keepConfig = false)
		{
			filename = Path.Combine(Interface.GetMod().PluginDirectory, Path.GetFileName(filename));
			bool result;
			try
			{
				if (!File.Exists(filename))
				{
					result = false;
				}
				else
				{
					File.Delete(filename);
					if (!keepConfig)
					{
						try
						{
							File.Delete(Path.Combine(Interface.GetMod().ConfigDirectory, Path.GetFileNameWithoutExtension(filename) + ".json"));
						}
						catch
						{
						}
					}
					result = true;
				}
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00004CD0 File Offset: 0x00002ED0
		static OxideMod smethod_0()
		{
			return Interface.GetMod();
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00004CE4 File Offset: 0x00002EE4
		static string smethod_1(OxideMod oxideMod_0)
		{
			return oxideMod_0.PluginDirectory;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00004CF8 File Offset: 0x00002EF8
		static string smethod_2(OxideMod oxideMod_0)
		{
			return oxideMod_0.ExtensionDirectory;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00004D0C File Offset: 0x00002F0C
		static string smethod_3(OxideMod oxideMod_0)
		{
			return oxideMod_0.ConfigDirectory;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004D20 File Offset: 0x00002F20
		static string smethod_4(OxideMod oxideMod_0)
		{
			return oxideMod_0.DataDirectory;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004D34 File Offset: 0x00002F34
		static string smethod_5(OxideMod oxideMod_0)
		{
			return oxideMod_0.LogDirectory;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004D48 File Offset: 0x00002F48
		static string smethod_6(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootDirectory;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004D5C File Offset: 0x00002F5C
		static PluginManager smethod_7(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootPluginManager;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004D70 File Offset: 0x00002F70
		static JArray smethod_8()
		{
			return new JArray();
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00004D84 File Offset: 0x00002F84
		static IEnumerable<Plugin> smethod_9(PluginManager pluginManager_0)
		{
			return pluginManager_0.GetPlugins();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004D98 File Offset: 0x00002F98
		static JObject smethod_10()
		{
			return new JObject();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004DAC File Offset: 0x00002FAC
		static int smethod_11(Plugin plugin_0)
		{
			return plugin_0.ResourceId;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004DC0 File Offset: 0x00002FC0
		static JToken smethod_12(int int_0)
		{
			return int_0;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004DD4 File Offset: 0x00002FD4
		static void smethod_13(JObject jobject_0, string string_0, JToken jtoken_0)
		{
			jobject_0[string_0] = jtoken_0;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00004DEC File Offset: 0x00002FEC
		static string smethod_14(Plugin plugin_0)
		{
			return plugin_0.Name;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004E00 File Offset: 0x00003000
		static JToken smethod_15(string string_0)
		{
			return string_0;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004E14 File Offset: 0x00003014
		static string smethod_16(Plugin plugin_0)
		{
			return plugin_0.Title;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00004E28 File Offset: 0x00003028
		static VersionNumber smethod_17(Plugin plugin_0)
		{
			return plugin_0.Version;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00004E3C File Offset: 0x0000303C
		static string smethod_18(Plugin plugin_0)
		{
			return plugin_0.Author;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004E50 File Offset: 0x00003050
		static object smethod_19(Plugin plugin_0)
		{
			return plugin_0.Object;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00004E64 File Offset: 0x00003064
		static Type smethod_20(object object_0)
		{
			return object_0.GetType();
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00004E78 File Offset: 0x00003078
		static FieldInfo smethod_21(Type type_0, string string_0, BindingFlags bindingFlags_0)
		{
			return type_0.GetField(string_0, bindingFlags_0);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00004E90 File Offset: 0x00003090
		static object smethod_22(FieldInfo fieldInfo_0, object object_0)
		{
			return fieldInfo_0.GetValue(object_0);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00004EA4 File Offset: 0x000030A4
		static string smethod_23(string string_0)
		{
			return Path.GetFileName(string_0);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00004EB8 File Offset: 0x000030B8
		static Encoding smethod_24()
		{
			return Encoding.UTF8;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00004ECC File Offset: 0x000030CC
		static string smethod_25(string string_0, Encoding encoding_0)
		{
			return File.ReadAllText(string_0, encoding_0);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00004EE0 File Offset: 0x000030E0
		static bool smethod_26(Plugin plugin_0)
		{
			return plugin_0.HasConfig;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00004EF4 File Offset: 0x000030F4
		static JsonSerializerSettings smethod_27()
		{
			return new JsonSerializerSettings();
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00004F08 File Offset: 0x00003108
		static IList<JsonConverter> smethod_28(JsonSerializerSettings jsonSerializerSettings_0)
		{
			return jsonSerializerSettings_0.Converters;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004F1C File Offset: 0x0000311C
		static KeyValuesConverter smethod_29()
		{
			return new KeyValuesConverter();
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00004F30 File Offset: 0x00003130
		static DynamicConfigFile smethod_30(Plugin plugin_0)
		{
			return plugin_0.Config;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004F44 File Offset: 0x00003144
		static string smethod_31(object object_0, Formatting formatting_0, JsonSerializerSettings jsonSerializerSettings_0)
		{
			return JsonConvert.SerializeObject(object_0, formatting_0, jsonSerializerSettings_0);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00004F5C File Offset: 0x0000315C
		static void smethod_32(JArray jarray_0, JToken jtoken_0)
		{
			jarray_0.Add(jtoken_0);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00004F70 File Offset: 0x00003170
		static bool smethod_33(IEnumerator ienumerator_0)
		{
			return ienumerator_0.MoveNext();
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00004F84 File Offset: 0x00003184
		static void smethod_34(IDisposable idisposable_0)
		{
			idisposable_0.Dispose();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00004F98 File Offset: 0x00003198
		static string[] smethod_35(string string_0)
		{
			return Directory.GetFiles(string_0);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00004FAC File Offset: 0x000031AC
		static bool smethod_36(Regex regex_1, string string_0)
		{
			return regex_1.IsMatch(string_0);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00004FC0 File Offset: 0x000031C0
		static string smethod_37(string string_0, string string_1)
		{
			return Path.Combine(string_0, string_1);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00004FD4 File Offset: 0x000031D4
		static bool smethod_38(string string_0)
		{
			return File.Exists(string_0);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004FE8 File Offset: 0x000031E8
		static string smethod_39(string string_0)
		{
			return Path.GetFileNameWithoutExtension(string_0);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00002868 File Offset: 0x00000A68
		static string smethod_40(string string_0, string string_1)
		{
			return string_0 + string_1;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004FFC File Offset: 0x000031FC
		static void smethod_41(string string_0, byte[] byte_0)
		{
			File.WriteAllBytes(string_0, byte_0);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00005010 File Offset: 0x00003210
		static void smethod_42(string string_0, string string_1, Encoding encoding_0)
		{
			File.WriteAllText(string_0, string_1, encoding_0);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00005028 File Offset: 0x00003228
		static void smethod_43(string string_0)
		{
			File.Delete(string_0);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000503C File Offset: 0x0000323C
		static Regex smethod_44(string string_0)
		{
			return new Regex(string_0);
		}

		// Token: 0x04000032 RID: 50
		private static Regex regex_0 = new Regex("\\.(cs|lua|js|py)$");
	}
}
