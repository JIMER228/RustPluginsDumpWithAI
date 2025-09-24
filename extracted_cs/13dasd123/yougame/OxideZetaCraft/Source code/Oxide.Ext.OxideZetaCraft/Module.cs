using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Oxide.Core;

namespace ZetaCraft.Oxide
{
	// Token: 0x02000019 RID: 25
	public class Module
	{
		// Token: 0x06000143 RID: 323 RVA: 0x00007904 File Offset: 0x00005B04
		public static Module Install(byte[] rawData, Action<string, Exception> logHandler, JObject config)
		{
			OxideZetaCraftExtension instance = OxideZetaCraftExtension.Instance;
			Module module = new Module();
			module.assembly_0 = Assembly.Load(rawData);
			Version version = module.assembly_0.GetName().Version;
			module.assembly_0.GetName().Version = new Version(version.Major, version.Minor, version.Revision, Module.int_0++);
			module.type_0 = module.assembly_0.GetType("ZetaCraft.Interface");
			EventInfo @event = module.type_0.GetEvent("OnLog");
			Delegate @delegate = Delegate.CreateDelegate(@event.EventHandlerType, logHandler.Target, logHandler.Method);
			module.action_0 = logHandler;
			module.object_0 = Activator.CreateInstance(module.type_0, new object[]
			{
				"oxide",
				Path.Combine(Interface.GetMod().DataDirectory, "ZetaCraft")
			});
			@event.GetAddMethod().Invoke(module.object_0, new object[]
			{
				@delegate
			});
			if (Module.module_0 != null)
			{
				Module.module_0.method_0();
			}
			Module.module_0 = module;
			module.CallEx("OnInstall", new object[]
			{
				config
			});
			return module;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00007A40 File Offset: 0x00005C40
		public bool Call(string method, params object[] args)
		{
			MethodInfo method2 = this.type_0.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod);
			bool result;
			if (method2 == null)
			{
				result = false;
			}
			else
			{
				try
				{
					method2.Invoke(this.object_0, args);
					result = true;
				}
				catch
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00007A98 File Offset: 0x00005C98
		public void CallEx(string method, params object[] args)
		{
			MethodInfo method2 = this.type_0.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod);
			if (method2 == null)
			{
				throw new MissingMethodException(method);
			}
			try
			{
				method2.Invoke(this.object_0, args);
			}
			catch (Exception ex)
			{
				throw ex.InnerException;
			}
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00007AF0 File Offset: 0x00005CF0
		public bool Call(string method, out object returnValue, params object[] args)
		{
			returnValue = null;
			MethodInfo method2 = this.type_0.GetMethod(method);
			bool result;
			if (!(method2 == null))
			{
				returnValue = method2.Invoke(this.object_0, args);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00007B2C File Offset: 0x00005D2C
		internal bool method_0()
		{
			return this.Call("OnUninstall", null);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00007B64 File Offset: 0x00005D64
		static Assembly smethod_0(byte[] byte_0)
		{
			return Assembly.Load(byte_0);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00004768 File Offset: 0x00002968
		static AssemblyName smethod_1(Assembly assembly_1)
		{
			return assembly_1.GetName();
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000477C File Offset: 0x0000297C
		static Version smethod_2(AssemblyName assemblyName_0)
		{
			return assemblyName_0.Version;
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00004790 File Offset: 0x00002990
		static int smethod_3(Version version_0)
		{
			return version_0.Major;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x000047A4 File Offset: 0x000029A4
		static int smethod_4(Version version_0)
		{
			return version_0.Minor;
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00007B78 File Offset: 0x00005D78
		static int smethod_5(Version version_0)
		{
			return version_0.Revision;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00007B8C File Offset: 0x00005D8C
		static Version smethod_6(int int_1, int int_2, int int_3, int int_4)
		{
			return new Version(int_1, int_2, int_3, int_4);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00007BA4 File Offset: 0x00005DA4
		static void smethod_7(AssemblyName assemblyName_0, Version version_0)
		{
			assemblyName_0.Version = version_0;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00007BB8 File Offset: 0x00005DB8
		static Type smethod_8(Assembly assembly_1, string string_0)
		{
			return assembly_1.GetType(string_0);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00007BCC File Offset: 0x00005DCC
		static EventInfo smethod_9(Type type_1, string string_0)
		{
			return type_1.GetEvent(string_0);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00007BE0 File Offset: 0x00005DE0
		static Type smethod_10(EventInfo eventInfo_0)
		{
			return eventInfo_0.EventHandlerType;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00007BF4 File Offset: 0x00005DF4
		static object smethod_11(Delegate delegate_0)
		{
			return delegate_0.Target;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00007C08 File Offset: 0x00005E08
		static MethodInfo smethod_12(Delegate delegate_0)
		{
			return delegate_0.Method;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00007C1C File Offset: 0x00005E1C
		static Delegate smethod_13(Type type_1, object object_1, MethodInfo methodInfo_0)
		{
			return Delegate.CreateDelegate(type_1, object_1, methodInfo_0);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00004CD0 File Offset: 0x00002ED0
		static OxideMod smethod_14()
		{
			return Interface.GetMod();
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00004D20 File Offset: 0x00002F20
		static string smethod_15(OxideMod oxideMod_0)
		{
			return oxideMod_0.DataDirectory;
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00004FC0 File Offset: 0x000031C0
		static string smethod_16(string string_0, string string_1)
		{
			return Path.Combine(string_0, string_1);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00007C34 File Offset: 0x00005E34
		static object smethod_17(Type type_1, object[] object_1)
		{
			return Activator.CreateInstance(type_1, object_1);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00007C48 File Offset: 0x00005E48
		static MethodInfo smethod_18(EventInfo eventInfo_0)
		{
			return eventInfo_0.GetAddMethod();
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000287C File Offset: 0x00000A7C
		static object smethod_19(MethodBase methodBase_0, object object_1, object[] object_2)
		{
			return methodBase_0.Invoke(object_1, object_2);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00007C5C File Offset: 0x00005E5C
		static MethodInfo smethod_20(Type type_1, string string_0, BindingFlags bindingFlags_0)
		{
			return type_1.GetMethod(string_0, bindingFlags_0);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00007C74 File Offset: 0x00005E74
		static bool smethod_21(MethodInfo methodInfo_0, MethodInfo methodInfo_1)
		{
			return methodInfo_0 == methodInfo_1;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00007C88 File Offset: 0x00005E88
		static MissingMethodException smethod_22(string string_0)
		{
			return new MissingMethodException(string_0);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00007C9C File Offset: 0x00005E9C
		static Exception smethod_23(Exception exception_0)
		{
			return exception_0.InnerException;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00007CB0 File Offset: 0x00005EB0
		static MethodInfo smethod_24(Type type_1, string string_0)
		{
			return type_1.GetMethod(string_0);
		}

		// Token: 0x0400004C RID: 76
		internal static Module module_0 = null;

		// Token: 0x0400004D RID: 77
		private static int int_0 = 0;

		// Token: 0x0400004E RID: 78
		private Assembly assembly_0;

		// Token: 0x0400004F RID: 79
		private Type type_0;

		// Token: 0x04000050 RID: 80
		private object object_0;

		// Token: 0x04000051 RID: 81
		private Action<string, Exception> action_0;
	}
}
