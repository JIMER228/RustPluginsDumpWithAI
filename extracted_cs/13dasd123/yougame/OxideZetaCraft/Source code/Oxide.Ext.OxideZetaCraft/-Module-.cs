// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Token: 0x02000001 RID: 1
internal class <Module>
{
	// Token: 0x06000001 RID: 1 RVA: 0x00002710 File Offset: 0x00000910
	static <Module>()
	{
		<Module>.smethod_30();
		<Module>.smethod_24();
		<Module>.smethod_0();
	}

	// Token: 0x06000002 RID: 2 RVA: 0x0000272C File Offset: 0x0000092C
	private static void smethod_0()
	{
		string str = "COR";
		Type typeFromHandle = typeof(Environment);
		MethodInfo method = typeFromHandle.GetMethod("GetEnvironmentVariable", new Type[]
		{
			typeof(string)
		});
		if (method != null && "1".Equals(method.Invoke(null, new object[]
		{
			str + "_ENABLE_PROFILING"
		})))
		{
			Environment.FailFast(null);
		}
		new Thread(new ParameterizedThreadStart(<Module>.smethod_1))
		{
			IsBackground = true
		}.Start(null);
	}

	// Token: 0x06000003 RID: 3 RVA: 0x000027C4 File Offset: 0x000009C4
	private static void smethod_1(object object_0)
	{
		Thread thread = object_0 as Thread;
		if (thread == null)
		{
			thread = new Thread(new ParameterizedThreadStart(<Module>.smethod_1));
			thread.IsBackground = true;
			thread.Start(Thread.CurrentThread);
			Thread.Sleep(500);
		}
		for (;;)
		{
			if (Debugger.IsAttached || Debugger.IsLogging())
			{
				Environment.FailFast(null);
			}
			if (!thread.IsAlive)
			{
				Environment.FailFast(null);
			}
			Thread.Sleep(1000);
		}
	}

	// Token: 0x06000004 RID: 4 RVA: 0x0000283C File Offset: 0x00000A3C
	static Type smethod_2(RuntimeTypeHandle runtimeTypeHandle_0)
	{
		return Type.GetTypeFromHandle(runtimeTypeHandle_0);
	}

	// Token: 0x06000005 RID: 5 RVA: 0x0000283C File Offset: 0x00000A3C
	static Type smethod_3(RuntimeTypeHandle runtimeTypeHandle_0)
	{
		return Type.GetTypeFromHandle(runtimeTypeHandle_0);
	}

	// Token: 0x06000006 RID: 6 RVA: 0x00002850 File Offset: 0x00000A50
	static MethodInfo smethod_4(Type type_0, string string_0, Type[] type_1)
	{
		return type_0.GetMethod(string_0, type_1);
	}

	// Token: 0x06000007 RID: 7 RVA: 0x00002868 File Offset: 0x00000A68
	static string smethod_5(string string_0, string string_1)
	{
		return string_0 + string_1;
	}

	// Token: 0x06000008 RID: 8 RVA: 0x0000287C File Offset: 0x00000A7C
	static object smethod_6(MethodBase methodBase_0, object object_0, object[] object_1)
	{
		return methodBase_0.Invoke(object_0, object_1);
	}

	// Token: 0x06000009 RID: 9 RVA: 0x00002894 File Offset: 0x00000A94
	static bool smethod_7(object object_0, object object_1)
	{
		return object_0.Equals(object_1);
	}

	// Token: 0x0600000A RID: 10 RVA: 0x000028A8 File Offset: 0x00000AA8
	static void smethod_8(string string_0)
	{
		Environment.FailFast(string_0);
	}

	// Token: 0x0600000B RID: 11 RVA: 0x000028BC File Offset: 0x00000ABC
	static Thread smethod_9(ParameterizedThreadStart parameterizedThreadStart_0)
	{
		return new Thread(parameterizedThreadStart_0);
	}

	// Token: 0x0600000C RID: 12 RVA: 0x000028D0 File Offset: 0x00000AD0
	static void smethod_10(Thread thread_0, bool bool_0)
	{
		thread_0.IsBackground = bool_0;
	}

	// Token: 0x0600000D RID: 13 RVA: 0x000028E4 File Offset: 0x00000AE4
	static void smethod_11(Thread thread_0, object object_0)
	{
		thread_0.Start(object_0);
	}

	// Token: 0x0600000E RID: 14 RVA: 0x000028BC File Offset: 0x00000ABC
	static Thread smethod_12(ParameterizedThreadStart parameterizedThreadStart_0)
	{
		return new Thread(parameterizedThreadStart_0);
	}

	// Token: 0x0600000F RID: 15 RVA: 0x000028D0 File Offset: 0x00000AD0
	static void smethod_13(Thread thread_0, bool bool_0)
	{
		thread_0.IsBackground = bool_0;
	}

	// Token: 0x06000010 RID: 16 RVA: 0x000028F8 File Offset: 0x00000AF8
	static Thread smethod_14()
	{
		return Thread.CurrentThread;
	}

	// Token: 0x06000011 RID: 17 RVA: 0x000028E4 File Offset: 0x00000AE4
	static void smethod_15(Thread thread_0, object object_0)
	{
		thread_0.Start(object_0);
	}

	// Token: 0x06000012 RID: 18 RVA: 0x0000290C File Offset: 0x00000B0C
	static void smethod_16(int int_0)
	{
		Thread.Sleep(int_0);
	}

	// Token: 0x06000013 RID: 19 RVA: 0x00002920 File Offset: 0x00000B20
	static bool smethod_17()
	{
		return Debugger.IsAttached;
	}

	// Token: 0x06000014 RID: 20 RVA: 0x00002934 File Offset: 0x00000B34
	static bool smethod_18()
	{
		return Debugger.IsLogging();
	}

	// Token: 0x06000015 RID: 21 RVA: 0x000028A8 File Offset: 0x00000AA8
	static void smethod_19(string string_0)
	{
		Environment.FailFast(string_0);
	}

	// Token: 0x06000016 RID: 22 RVA: 0x00002948 File Offset: 0x00000B48
	static bool smethod_20(Thread thread_0)
	{
		return thread_0.IsAlive;
	}

	// Token: 0x06000017 RID: 23 RVA: 0x000028A8 File Offset: 0x00000AA8
	static void smethod_21(string string_0)
	{
		Environment.FailFast(string_0);
	}

	// Token: 0x06000018 RID: 24 RVA: 0x0000290C File Offset: 0x00000B0C
	static void smethod_22(int int_0)
	{
		Thread.Sleep(int_0);
	}

	// Token: 0x06000019 RID: 25 RVA: 0x0000295C File Offset: 0x00000B5C
	internal static byte[] smethod_23(byte[] byte_1)
	{
		MemoryStream memoryStream = new MemoryStream(byte_1);
		<Module>.Class1 @class = new <Module>.Class1();
		byte[] buffer = new byte[5];
		memoryStream.Read(buffer, 0, 5);
		@class.method_5(buffer);
		long num = 0L;
		for (int i = 0; i < 8; i++)
		{
			int num2 = memoryStream.ReadByte();
			num |= (long)((long)((ulong)((byte)num2)) << 8 * i);
		}
		byte[] array = new byte[(int)num];
		MemoryStream stream_ = new MemoryStream(array, true);
		long long_ = memoryStream.Length - 13L;
		@class.method_4(memoryStream, stream_, long_, num);
		return array;
	}

	// Token: 0x0600001A RID: 26 RVA: 0x000029F4 File Offset: 0x00000BF4
	internal static void smethod_24()
	{
		uint num = 320u;
		uint[] array = new uint[]
		{
			2838120936u,
			186839824u,
			824732059u,
			2334817052u,
			2208328088u,
			897641155u,
			3795374383u,
			2529994507u,
			807149717u,
			3835063561u,
			1832747996u,
			3061830428u,
			43146230u,
			2254161076u,
			3404432640u,
			782218045u,
			2509219086u,
			2202148364u,
			3713913242u,
			2681848768u,
			99003358u,
			1182490881u,
			1324455498u,
			898541494u,
			3745750094u,
			2714267766u,
			2249154784u,
			3770922509u,
			1703867509u,
			1866213880u,
			649238040u,
			3621060710u,
			3160359801u,
			277757637u,
			3965029247u,
			2871969569u,
			2117494409u,
			1206336026u,
			3424895931u,
			2902884656u,
			1057945139u,
			3887231614u,
			3266253748u,
			771991621u,
			2196733045u,
			4198432214u,
			317540351u,
			3068681342u,
			1313247293u,
			3443702593u,
			4282665491u,
			3345035621u,
			2158982035u,
			3288967954u,
			3708026332u,
			1814796301u,
			242265034u,
			468753879u,
			2888220510u,
			3447161794u,
			4131396201u,
			122712143u,
			874376472u,
			1849751381u,
			2049606937u,
			2823994081u,
			756224093u,
			73085982u,
			1541323419u,
			2671920125u,
			2321759042u,
			1885317436u,
			2727497922u,
			2226546382u,
			3001875324u,
			4264424148u,
			1017674735u,
			4097858465u,
			3612520550u,
			3112018298u,
			640377875u,
			781704587u,
			3664581884u,
			1280431464u,
			1948507631u,
			1552630643u,
			1964323423u,
			4079693652u,
			208297721u,
			840140990u,
			1826202246u,
			3059470316u,
			4179581905u,
			3880855445u,
			2959542696u,
			4069104415u,
			1358365065u,
			3948467184u,
			300280640u,
			2396055041u,
			3451728164u,
			625418489u,
			2329060412u,
			3763054940u,
			269764080u,
			693685002u,
			3887098604u,
			1396700569u,
			3977175654u,
			2307495342u,
			58023235u,
			1480223406u,
			2525977270u,
			2183648323u,
			1580652790u,
			777687898u,
			1027444847u,
			3507110949u,
			3477443736u,
			4232519428u,
			288422984u,
			2775734980u,
			1611066006u,
			95131552u,
			186139858u,
			2190857428u,
			1652824363u,
			1340003200u,
			3698244732u,
			1084867575u,
			549893793u,
			12896931u,
			3834352016u,
			1278722881u,
			1865609952u,
			1151870333u,
			2084558856u,
			992908733u,
			3717863471u,
			123666176u,
			3039637153u,
			3592718598u,
			1775851350u,
			3966205355u,
			2792323961u,
			4229571069u,
			3745194514u,
			947070531u,
			353765025u,
			1583210524u,
			401689233u,
			1138267095u,
			606782020u,
			1621170058u,
			508076360u,
			2908203425u,
			1721065319u,
			3447774035u,
			2255793795u,
			2410411081u,
			1023266857u,
			2598438481u,
			3230961521u,
			831266964u,
			2321542404u,
			3563030621u,
			1358264375u,
			2356528374u,
			3755085322u,
			1311877317u,
			3409934665u,
			3383410799u,
			429085340u,
			2331222782u,
			3849922366u,
			814337426u,
			1303182454u,
			1036987699u,
			331836809u,
			3823735820u,
			4140735162u,
			2421529121u,
			1008786115u,
			4019705996u,
			609421720u,
			2906202367u,
			662347566u,
			2163432853u,
			3354895087u,
			2442262677u,
			3211462236u,
			3929140648u,
			4261568100u,
			3217005145u,
			3202118825u,
			3811767037u,
			861015683u,
			3759286836u,
			2456368949u,
			1671232316u,
			3236983392u,
			365504559u,
			2247277804u,
			3732611619u,
			716021322u,
			2813326520u,
			2140628606u,
			1282605126u,
			2968967395u,
			3375097254u,
			1473246424u,
			2350936146u,
			3224950965u,
			1737767831u,
			2013061078u,
			758343077u,
			2472641022u,
			766113503u,
			1809023961u,
			1660216151u,
			3035505777u,
			2900704643u,
			3527769690u,
			1687958279u,
			3585934283u,
			470609848u,
			4187125082u,
			2927809722u,
			3304679141u,
			475954224u,
			1859831972u,
			3593306544u,
			4157323317u,
			2601699383u,
			3876637810u,
			601203438u,
			866512117u,
			2122907655u,
			2680040038u,
			450807679u,
			114121362u,
			3317932231u,
			4214045119u,
			1450976037u,
			3652089728u,
			1556396696u,
			4104881256u,
			3071958247u,
			2592155505u,
			4000725194u,
			2443419490u,
			3740078380u,
			3099269457u,
			2894168581u,
			1170395430u,
			332717966u,
			2910874403u,
			1276185856u,
			1145207459u,
			3685952220u,
			581628873u,
			2232165904u,
			3649631050u,
			551041302u,
			2067246940u,
			252957094u,
			3230881376u,
			2853368030u,
			1524727995u,
			3636832426u,
			2804198928u,
			3309663131u,
			4187693870u,
			1626109972u,
			726897798u,
			2708775990u,
			1503502758u,
			442755830u,
			2934813269u,
			3035530049u,
			1968546467u,
			516139688u,
			3943443720u,
			4025069491u,
			3986624976u,
			2932247397u,
			3131513920u,
			1130840666u,
			1423084585u,
			1244163089u,
			3742677315u,
			489540110u,
			3385488776u,
			453344336u,
			3761643130u,
			3786840961u,
			3010678406u,
			324992916u,
			1575945176u,
			1549801503u,
			3252229257u,
			2600267108u,
			946107464u,
			3989153074u,
			753212361u,
			101992196u,
			2634276735u,
			317675981u,
			3142756624u,
			2297968598u,
			1909720u,
			1055739746u,
			458318404u,
			2287807016u,
			1108978828u,
			3567640219u,
			2757358707u,
			2600267108u,
			946107464u,
			3989153074u
		};
		uint[] array2 = new uint[16];
		uint num2 = 1227807581u;
		for (int i = 0; i < 16; i++)
		{
			num2 ^= num2 >> 12;
			num2 ^= num2 << 25;
			num2 ^= num2 >> 27;
			array2[i] = num2;
		}
		int num3 = 0;
		int num4 = 0;
		uint[] array3 = new uint[16];
		byte[] array4 = new byte[num * 4u];
		while ((long)num3 < (long)((ulong)num))
		{
			for (int j = 0; j < 16; j++)
			{
				array3[j] = array[num3 + j];
			}
			array3[0] = (array3[0] ^ array2[0]);
			array3[1] = (array3[1] ^ array2[1]);
			array3[2] = (array3[2] ^ array2[2]);
			array3[3] = (array3[3] ^ array2[3]);
			array3[4] = (array3[4] ^ array2[4]);
			array3[5] = (array3[5] ^ array2[5]);
			array3[6] = (array3[6] ^ array2[6]);
			array3[7] = (array3[7] ^ array2[7]);
			array3[8] = (array3[8] ^ array2[8]);
			array3[9] = (array3[9] ^ array2[9]);
			array3[10] = (array3[10] ^ array2[10]);
			array3[11] = (array3[11] ^ array2[11]);
			array3[12] = (array3[12] ^ array2[12]);
			array3[13] = (array3[13] ^ array2[13]);
			array3[14] = (array3[14] ^ array2[14]);
			array3[15] = (array3[15] ^ array2[15]);
			for (int k = 0; k < 16; k++)
			{
				uint num5 = array3[k];
				array4[num4++] = (byte)num5;
				array4[num4++] = (byte)(num5 >> 8);
				array4[num4++] = (byte)(num5 >> 16);
				array4[num4++] = (byte)(num5 >> 24);
				array2[k] ^= num5;
			}
			num3 += 16;
		}
		<Module>.byte_0 = <Module>.smethod_23(array4);
	}

	// Token: 0x0600001B RID: 27 RVA: 0x00002BF0 File Offset: 0x00000DF0
	internal static T smethod_25<T>(uint uint_0)
	{
		uint_0 = (uint_0 * 3837059973u ^ 699393715u);
		uint num = uint_0 >> 30;
		T result = default(T);
		uint_0 &= 1073741823u;
		uint_0 <<= 2;
		if ((ulong)num == 2UL)
		{
			int count = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			result = (T)((object)string.Intern(Encoding.UTF8.GetString(<Module>.byte_0, (int)uint_0, count)));
		}
		else if ((ulong)num == 3UL)
		{
			T[] array = new T[1];
			Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array, 0, sizeof(T));
			result = array[0];
		}
		else if ((ulong)num == 1UL)
		{
			int num2 = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			int length = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			Array array2 = Array.CreateInstance(typeof(T).GetElementType(), length);
			Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array2, 0, num2 - 4);
			result = (T)((object)array2);
		}
		return result;
	}

	// Token: 0x0600001C RID: 28 RVA: 0x00002D90 File Offset: 0x00000F90
	internal static T smethod_26<T>(uint uint_0)
	{
		uint_0 = (uint_0 * 1065796155u ^ 3826426580u);
		uint num = uint_0 >> 30;
		T result = default(T);
		uint_0 &= 1073741823u;
		uint_0 <<= 2;
		if ((ulong)num != 2UL)
		{
			if ((ulong)num != 0UL)
			{
				if ((ulong)num == 3UL)
				{
					int num2 = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
					int length = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
					Array array = Array.CreateInstance(typeof(T).GetElementType(), length);
					Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array, 0, num2 - 4);
					result = (T)((object)array);
				}
			}
			else
			{
				T[] array2 = new T[1];
				Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array2, 0, sizeof(T));
				result = array2[0];
			}
		}
		else
		{
			int count = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			result = (T)((object)string.Intern(Encoding.UTF8.GetString(<Module>.byte_0, (int)uint_0, count)));
		}
		return result;
	}

	// Token: 0x0600001D RID: 29 RVA: 0x00002F34 File Offset: 0x00001134
	internal static T smethod_27<T>(uint uint_0)
	{
		uint_0 = (uint_0 * 1465878855u ^ 911088994u);
		uint num = uint_0 >> 30;
		T result = default(T);
		uint_0 &= 1073741823u;
		uint_0 <<= 2;
		if ((ulong)num == 2UL)
		{
			int count = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			result = (T)((object)string.Intern(Encoding.UTF8.GetString(<Module>.byte_0, (int)uint_0, count)));
		}
		else if ((ulong)num != 0UL)
		{
			if ((ulong)num == 3UL)
			{
				int num2 = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				int length = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				Array array = Array.CreateInstance(typeof(T).GetElementType(), length);
				Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array, 0, num2 - 4);
				result = (T)((object)array);
			}
		}
		else
		{
			T[] array2 = new T[1];
			Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array2, 0, sizeof(T));
			result = array2[0];
		}
		return result;
	}

	// Token: 0x0600001E RID: 30 RVA: 0x000030D4 File Offset: 0x000012D4
	internal static T smethod_28<T>(uint uint_0)
	{
		uint_0 = (uint_0 * 3338720491u ^ 2132110723u);
		uint num = uint_0 >> 30;
		T result = default(T);
		uint_0 &= 1073741823u;
		uint_0 <<= 2;
		if ((ulong)num == 2UL)
		{
			int count = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			result = (T)((object)string.Intern(Encoding.UTF8.GetString(<Module>.byte_0, (int)uint_0, count)));
		}
		else if ((ulong)num != 3UL)
		{
			if ((ulong)num == 0UL)
			{
				int num2 = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				int length = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				Array array = Array.CreateInstance(typeof(T).GetElementType(), length);
				Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array, 0, num2 - 4);
				result = (T)((object)array);
			}
		}
		else
		{
			T[] array2 = new T[1];
			Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array2, 0, sizeof(T));
			result = array2[0];
		}
		return result;
	}

	// Token: 0x0600001F RID: 31 RVA: 0x00003274 File Offset: 0x00001474
	internal static T smethod_29<T>(uint uint_0)
	{
		uint_0 = (uint_0 * 4193001365u ^ 2560522825u);
		uint num = uint_0 >> 30;
		T result = default(T);
		uint_0 &= 1073741823u;
		uint_0 <<= 2;
		if ((ulong)num != 0UL)
		{
			if ((ulong)num == 3UL)
			{
				T[] array = new T[1];
				Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array, 0, sizeof(T));
				result = array[0];
			}
			else if ((ulong)num == 1UL)
			{
				int num2 = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				int length = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
				Array array2 = Array.CreateInstance(typeof(T).GetElementType(), length);
				Buffer.BlockCopy(<Module>.byte_0, (int)uint_0, array2, 0, num2 - 4);
				result = (T)((object)array2);
			}
		}
		else
		{
			int count = (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 8 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 16 | (int)<Module>.byte_0[(int)((UIntPtr)(uint_0++))] << 24;
			result = (T)((object)string.Intern(Encoding.UTF8.GetString(<Module>.byte_0, (int)uint_0, count)));
		}
		return result;
	}

	// Token: 0x06000020 RID: 32 RVA: 0x00003414 File Offset: 0x00001614
	internal static void smethod_30()
	{
		uint num = 112u;
		uint[] array = new uint[]
		{
			3619675595u,
			2277204088u,
			2713279879u,
			2912571466u,
			2092825899u,
			1119567009u,
			759621981u,
			2834890614u,
			1816857569u,
			2562688101u,
			1131748991u,
			2403529873u,
			1245639117u,
			1841600929u,
			3545573848u,
			2744794730u,
			2423919290u,
			2773526611u,
			1543021506u,
			3742697977u,
			322827311u,
			2279952539u,
			2241522697u,
			4109561425u,
			3575284862u,
			3109020121u,
			1189158242u,
			2061238951u,
			2547263389u,
			345361442u,
			3725511982u,
			294745449u,
			2112040784u,
			2631125139u,
			2740408098u,
			591051613u,
			1161239751u,
			775737729u,
			832132288u,
			2340501534u,
			3365939822u,
			3410090436u,
			613533841u,
			1914183676u,
			1367583245u,
			543054753u,
			3924400927u,
			4268788479u,
			876124441u,
			2022268828u,
			3931321393u,
			1243753428u,
			3107588525u,
			3907540442u,
			1971002643u,
			1046894852u,
			4100959695u,
			1704216422u,
			3576127583u,
			1299237013u,
			2273925576u,
			2434472898u,
			3584709066u,
			2987737814u,
			2922518160u,
			1465954948u,
			4267421811u,
			569113148u,
			3514171530u,
			4190074843u,
			626365260u,
			3255989886u,
			604154854u,
			1565660024u,
			969217719u,
			1974561507u,
			3069877292u,
			956766420u,
			549148826u,
			4049193485u,
			1465084256u,
			2499390031u,
			3494194933u,
			1290686000u,
			3003463992u,
			479544405u,
			4020286978u,
			1900004648u,
			3155213896u,
			1639756319u,
			3597289331u,
			2463672179u,
			3981026290u,
			3472873392u,
			562951676u,
			2737272762u,
			1890335423u,
			2313874007u,
			2876815913u,
			3203639122u,
			3341046130u,
			4271618165u,
			4020270666u,
			1900004648u,
			3155213896u,
			1639756319u,
			3597289331u,
			2463672179u,
			3981026290u,
			3472873392u,
			562951676u,
			2737272762u
		};
		uint[] array2 = new uint[16];
		uint num2 = 1874632825u;
		for (int i = 0; i < 16; i++)
		{
			num2 ^= num2 >> 13;
			num2 ^= num2 << 25;
			num2 ^= num2 >> 27;
			array2[i] = num2;
		}
		int num3 = 0;
		int num4 = 0;
		uint[] array3 = new uint[16];
		byte[] array4 = new byte[num * 4u];
		while ((long)num3 < (long)((ulong)num))
		{
			for (int j = 0; j < 16; j++)
			{
				array3[j] = array[num3 + j];
			}
			array3[0] = (array3[0] ^ array2[0]);
			array3[1] = (array3[1] ^ array2[1]);
			array3[2] = (array3[2] ^ array2[2]);
			array3[3] = (array3[3] ^ array2[3]);
			array3[4] = (array3[4] ^ array2[4]);
			array3[5] = (array3[5] ^ array2[5]);
			array3[6] = (array3[6] ^ array2[6]);
			array3[7] = (array3[7] ^ array2[7]);
			array3[8] = (array3[8] ^ array2[8]);
			array3[9] = (array3[9] ^ array2[9]);
			array3[10] = (array3[10] ^ array2[10]);
			array3[11] = (array3[11] ^ array2[11]);
			array3[12] = (array3[12] ^ array2[12]);
			array3[13] = (array3[13] ^ array2[13]);
			array3[14] = (array3[14] ^ array2[14]);
			array3[15] = (array3[15] ^ array2[15]);
			for (int k = 0; k < 16; k++)
			{
				uint num5 = array3[k];
				array4[num4++] = (byte)num5;
				array4[num4++] = (byte)(num5 >> 8);
				array4[num4++] = (byte)(num5 >> 16);
				array4[num4++] = (byte)(num5 >> 24);
				array2[k] ^= num5;
			}
			num3 += 16;
		}
		<Module>.assembly_0 = Assembly.Load(<Module>.smethod_23(array4));
		AppDomain.CurrentDomain.AssemblyResolve += <Module>.smethod_31;
	}

	// Token: 0x06000021 RID: 33 RVA: 0x0000362C File Offset: 0x0000182C
	internal static Assembly smethod_31(object object_0, ResolveEventArgs resolveEventArgs_0)
	{
		if (<Module>.assembly_0.FullName == resolveEventArgs_0.Name)
		{
			return <Module>.assembly_0;
		}
		return null;
	}

	// Token: 0x04000001 RID: 1
	internal static byte[] byte_0;

	// Token: 0x04000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
	internal static <Module>.Struct4 struct4_0;

	// Token: 0x04000003 RID: 3
	internal static Assembly assembly_0;

	// Token: 0x04000004 RID: 4 RVA: 0x00002550 File Offset: 0x00000750
	internal static <Module>.Struct5 struct5_0;

	// Token: 0x02000002 RID: 2
	internal struct Struct0
	{
		// Token: 0x06000022 RID: 34 RVA: 0x00003658 File Offset: 0x00001858
		internal void method_0()
		{
			this.uint_0 = 1024u;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00003670 File Offset: 0x00001870
		internal uint method_1(<Module>.Class0 class0_0)
		{
			uint num = (class0_0.uint_1 >> 11) * this.uint_0;
			if (class0_0.uint_0 < num)
			{
				class0_0.uint_1 = num;
				this.uint_0 += 2048u - this.uint_0 >> 5;
				if (class0_0.uint_1 < 16777216u)
				{
					class0_0.uint_0 = (class0_0.uint_0 << 8 | (uint)((byte)class0_0.stream_0.ReadByte()));
					class0_0.uint_1 <<= 8;
				}
				return 0u;
			}
			class0_0.uint_1 -= num;
			class0_0.uint_0 -= num;
			this.uint_0 -= this.uint_0 >> 5;
			if (class0_0.uint_1 < 16777216u)
			{
				class0_0.uint_0 = (class0_0.uint_0 << 8 | (uint)((byte)class0_0.stream_0.ReadByte()));
				class0_0.uint_1 <<= 8;
			}
			return 1u;
		}

		// Token: 0x04000005 RID: 5
		internal uint uint_0;
	}

	// Token: 0x02000003 RID: 3
	internal struct Struct1
	{
		// Token: 0x06000024 RID: 36 RVA: 0x0000375C File Offset: 0x0000195C
		internal Struct1(int int_1)
		{
			this.int_0 = int_1;
			this.struct0_0 = new <Module>.Struct0[1 << int_1];
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00003784 File Offset: 0x00001984
		internal void method_0()
		{
			uint num = 1u;
			while ((ulong)num < (ulong)(1L << (this.int_0 & 31)))
			{
				this.struct0_0[(int)((UIntPtr)num)].method_0();
				num += 1u;
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000037BC File Offset: 0x000019BC
		internal uint method_1(<Module>.Class0 class0_0)
		{
			uint num = 1u;
			for (int i = this.int_0; i > 0; i--)
			{
				num = (num << 1) + this.struct0_0[(int)((UIntPtr)num)].method_1(class0_0);
			}
			return num - (1u << this.int_0);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003804 File Offset: 0x00001A04
		internal uint method_2(<Module>.Class0 class0_0)
		{
			uint num = 1u;
			uint num2 = 0u;
			for (int i = 0; i < this.int_0; i++)
			{
				uint num3 = this.struct0_0[(int)((UIntPtr)num)].method_1(class0_0);
				num <<= 1;
				num += num3;
				num2 |= num3 << i;
			}
			return num2;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000384C File Offset: 0x00001A4C
		internal static uint smethod_0(<Module>.Struct0[] struct0_1, uint uint_0, <Module>.Class0 class0_0, int int_1)
		{
			uint num = 1u;
			uint num2 = 0u;
			for (int i = 0; i < int_1; i++)
			{
				uint num3 = struct0_1[(int)((UIntPtr)(uint_0 + num))].method_1(class0_0);
				num <<= 1;
				num += num3;
				num2 |= num3 << i;
			}
			return num2;
		}

		// Token: 0x04000006 RID: 6
		internal readonly <Module>.Struct0[] struct0_0;

		// Token: 0x04000007 RID: 7
		internal readonly int int_0;
	}

	// Token: 0x02000004 RID: 4
	internal class Class0
	{
		// Token: 0x06000029 RID: 41 RVA: 0x0000388C File Offset: 0x00001A8C
		internal void method_0(Stream stream_1)
		{
			this.stream_0 = stream_1;
			this.uint_0 = 0u;
			this.uint_1 = uint.MaxValue;
			for (int i = 0; i < 5; i++)
			{
				this.uint_0 = (this.uint_0 << 8 | (uint)((byte)this.stream_0.ReadByte()));
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000038D8 File Offset: 0x00001AD8
		internal void method_1()
		{
			this.stream_0 = null;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000038EC File Offset: 0x00001AEC
		internal void method_2()
		{
			while (this.uint_1 < 16777216u)
			{
				this.uint_0 = (this.uint_0 << 8 | (uint)((byte)this.stream_0.ReadByte()));
				this.uint_1 <<= 8;
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003934 File Offset: 0x00001B34
		internal uint method_3(int int_0)
		{
			uint num = this.uint_1;
			uint num2 = this.uint_0;
			uint num3 = 0u;
			for (int i = int_0; i > 0; i--)
			{
				num >>= 1;
				uint num4 = num2 - num >> 31;
				num2 -= (num & num4 - 1u);
				num3 = (num3 << 1 | 1u - num4);
				if (num < 16777216u)
				{
					num2 = (num2 << 8 | (uint)((byte)this.stream_0.ReadByte()));
					num <<= 8;
				}
			}
			this.uint_1 = num;
			this.uint_0 = num2;
			return num3;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000039A8 File Offset: 0x00001BA8
		internal Class0()
		{
		}

		// Token: 0x04000008 RID: 8
		internal uint uint_0;

		// Token: 0x04000009 RID: 9
		internal uint uint_1;

		// Token: 0x0400000A RID: 10
		internal Stream stream_0;
	}

	// Token: 0x02000005 RID: 5
	internal class Class1
	{
		// Token: 0x0600002E RID: 46 RVA: 0x000039BC File Offset: 0x00001BBC
		internal Class1()
		{
			this.uint_0 = uint.MaxValue;
			int num = 0;
			while ((long)num < 4L)
			{
				this.struct1_0[num] = new <Module>.Struct1(6);
				num++;
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003ABC File Offset: 0x00001CBC
		internal void method_0(uint uint_3)
		{
			if (this.uint_0 != uint_3)
			{
				this.uint_0 = uint_3;
				this.uint_1 = Math.Max(this.uint_0, 1u);
				uint uint_4 = Math.Max(this.uint_1, 4096u);
				this.class4_0.method_0(uint_4);
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00003B08 File Offset: 0x00001D08
		internal void method_1(int int_0, int int_1)
		{
			this.class3_0.method_0(int_0, int_1);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003B24 File Offset: 0x00001D24
		internal void method_2(int int_0)
		{
			uint num = 1u << int_0;
			this.class2_0.method_0(num);
			this.class2_1.method_0(num);
			this.uint_2 = num - 1u;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003B5C File Offset: 0x00001D5C
		internal void method_3(Stream stream_0, Stream stream_1)
		{
			this.class0_0.method_0(stream_0);
			this.class4_0.method_1(stream_1, this.bool_0);
			for (uint num = 0u; num < 12u; num += 1u)
			{
				for (uint num2 = 0u; num2 <= this.uint_2; num2 += 1u)
				{
					uint num3 = (num << 4) + num2;
					this.struct0_0[(int)((UIntPtr)num3)].method_0();
					this.struct0_1[(int)((UIntPtr)num3)].method_0();
				}
				this.struct0_2[(int)((UIntPtr)num)].method_0();
				this.struct0_3[(int)((UIntPtr)num)].method_0();
				this.struct0_4[(int)((UIntPtr)num)].method_0();
				this.struct0_5[(int)((UIntPtr)num)].method_0();
			}
			this.class3_0.method_1();
			for (uint num = 0u; num < 4u; num += 1u)
			{
				this.struct1_0[(int)((UIntPtr)num)].method_0();
			}
			for (uint num = 0u; num < 114u; num += 1u)
			{
				this.struct0_6[(int)((UIntPtr)num)].method_0();
			}
			this.class2_0.method_1();
			this.class2_1.method_1();
			this.struct1_1.method_0();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003C88 File Offset: 0x00001E88
		internal void method_4(Stream stream_0, Stream stream_1, long long_0, long long_1)
		{
			this.method_3(stream_0, stream_1);
			<Module>.Struct3 @struct = default(<Module>.Struct3);
			@struct.method_0();
			uint num = 0u;
			uint num2 = 0u;
			uint num3 = 0u;
			uint num4 = 0u;
			ulong num5 = 0UL;
			if (0L < long_1)
			{
				this.struct0_0[(int)((UIntPtr)(@struct.uint_0 << 4))].method_1(this.class0_0);
				@struct.method_1();
				byte byte_ = this.class3_0.method_3(this.class0_0, 0u, 0);
				this.class4_0.method_5(byte_);
				num5 += 1UL;
			}
			while (num5 < (ulong)long_1)
			{
				uint num6 = (uint)num5 & this.uint_2;
				if (this.struct0_0[(int)((UIntPtr)((@struct.uint_0 << 4) + num6))].method_1(this.class0_0) != 0u)
				{
					uint num7;
					if (this.struct0_2[(int)((UIntPtr)@struct.uint_0)].method_1(this.class0_0) != 1u)
					{
						num4 = num3;
						num3 = num2;
						num2 = num;
						num7 = 2u + this.class2_0.method_2(this.class0_0, num6);
						@struct.method_2();
						uint num8 = this.struct1_0[(int)((UIntPtr)<Module>.Class1.smethod_0(num7))].method_1(this.class0_0);
						if (num8 < 4u)
						{
							num = num8;
						}
						else
						{
							int num9 = (int)((num8 >> 1) - 1u);
							num = (2u | (num8 & 1u)) << num9;
							if (num8 >= 14u)
							{
								num += this.class0_0.method_3(num9 - 4) << 4;
								num += this.struct1_1.method_2(this.class0_0);
							}
							else
							{
								num += <Module>.Struct1.smethod_0(this.struct0_6, num - num8 - 1u, this.class0_0, num9);
							}
						}
					}
					else
					{
						if (this.struct0_3[(int)((UIntPtr)@struct.uint_0)].method_1(this.class0_0) != 0u)
						{
							uint num10;
							if (this.struct0_4[(int)((UIntPtr)@struct.uint_0)].method_1(this.class0_0) == 0u)
							{
								num10 = num2;
							}
							else
							{
								if (this.struct0_5[(int)((UIntPtr)@struct.uint_0)].method_1(this.class0_0) != 0u)
								{
									num10 = num4;
									num4 = num3;
								}
								else
								{
									num10 = num3;
								}
								num3 = num2;
							}
							num2 = num;
							num = num10;
						}
						else if (this.struct0_1[(int)((UIntPtr)((@struct.uint_0 << 4) + num6))].method_1(this.class0_0) == 0u)
						{
							@struct.method_4();
							this.class4_0.method_5(this.class4_0.method_6(num));
							num5 += 1UL;
							continue;
						}
						num7 = this.class2_1.method_2(this.class0_0, num6) + 2u;
						@struct.method_3();
					}
					if (((ulong)num >= num5 || num >= this.uint_1) && num == 4294967295u)
					{
						break;
					}
					this.class4_0.method_4(num, num7);
					num5 += (ulong)num7;
				}
				else
				{
					byte byte_2 = this.class4_0.method_6(0u);
					byte byte_3;
					if (@struct.method_5())
					{
						byte_3 = this.class3_0.method_3(this.class0_0, (uint)num5, byte_2);
					}
					else
					{
						byte_3 = this.class3_0.method_4(this.class0_0, (uint)num5, byte_2, this.class4_0.method_6(num));
					}
					this.class4_0.method_5(byte_3);
					@struct.method_1();
					num5 += 1UL;
				}
			}
			this.class4_0.method_3();
			this.class4_0.method_2();
			this.class0_0.method_1();
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00004004 File Offset: 0x00002204
		internal void method_5(byte[] byte_0)
		{
			int int_ = (int)(byte_0[0] % 9);
			int num = (int)(byte_0[0] / 9);
			int int_2 = num % 5;
			int int_3 = num / 5;
			uint num2 = 0u;
			for (int i = 0; i < 4; i++)
			{
				num2 += (uint)((uint)byte_0[1 + i] << i * 8);
			}
			this.method_0(num2);
			this.method_1(int_2, int_);
			this.method_2(int_3);
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00004064 File Offset: 0x00002264
		internal static uint smethod_0(uint uint_3)
		{
			uint_3 -= 2u;
			if (uint_3 < 4u)
			{
				return uint_3;
			}
			return 3u;
		}

		// Token: 0x0400000B RID: 11
		internal readonly <Module>.Struct0[] struct0_0 = new <Module>.Struct0[192];

		// Token: 0x0400000C RID: 12
		internal readonly <Module>.Struct0[] struct0_1 = new <Module>.Struct0[192];

		// Token: 0x0400000D RID: 13
		internal readonly <Module>.Struct0[] struct0_2 = new <Module>.Struct0[12];

		// Token: 0x0400000E RID: 14
		internal readonly <Module>.Struct0[] struct0_3 = new <Module>.Struct0[12];

		// Token: 0x0400000F RID: 15
		internal readonly <Module>.Struct0[] struct0_4 = new <Module>.Struct0[12];

		// Token: 0x04000010 RID: 16
		internal readonly <Module>.Struct0[] struct0_5 = new <Module>.Struct0[12];

		// Token: 0x04000011 RID: 17
		internal readonly <Module>.Class1.Class2 class2_0 = new <Module>.Class1.Class2();

		// Token: 0x04000012 RID: 18
		internal readonly <Module>.Class1.Class3 class3_0 = new <Module>.Class1.Class3();

		// Token: 0x04000013 RID: 19
		internal readonly <Module>.Class4 class4_0 = new <Module>.Class4();

		// Token: 0x04000014 RID: 20
		internal readonly <Module>.Struct0[] struct0_6 = new <Module>.Struct0[114];

		// Token: 0x04000015 RID: 21
		internal readonly <Module>.Struct1[] struct1_0 = new <Module>.Struct1[4];

		// Token: 0x04000016 RID: 22
		internal readonly <Module>.Class0 class0_0 = new <Module>.Class0();

		// Token: 0x04000017 RID: 23
		internal readonly <Module>.Class1.Class2 class2_1 = new <Module>.Class1.Class2();

		// Token: 0x04000018 RID: 24
		internal bool bool_0;

		// Token: 0x04000019 RID: 25
		internal uint uint_0;

		// Token: 0x0400001A RID: 26
		internal uint uint_1;

		// Token: 0x0400001B RID: 27
		internal <Module>.Struct1 struct1_1 = new <Module>.Struct1(4);

		// Token: 0x0400001C RID: 28
		internal uint uint_2;

		// Token: 0x02000006 RID: 6
		internal class Class2
		{
			// Token: 0x06000036 RID: 54 RVA: 0x00004080 File Offset: 0x00002280
			internal void method_0(uint uint_1)
			{
				for (uint num = this.uint_0; num < uint_1; num += 1u)
				{
					this.struct1_0[(int)((UIntPtr)num)] = new <Module>.Struct1(3);
					this.struct1_1[(int)((UIntPtr)num)] = new <Module>.Struct1(3);
				}
				this.uint_0 = uint_1;
			}

			// Token: 0x06000037 RID: 55 RVA: 0x000040D8 File Offset: 0x000022D8
			internal void method_1()
			{
				this.struct0_0.method_0();
				for (uint num = 0u; num < this.uint_0; num += 1u)
				{
					this.struct1_0[(int)((UIntPtr)num)].method_0();
					this.struct1_1[(int)((UIntPtr)num)].method_0();
				}
				this.struct0_1.method_0();
				this.struct1_2.method_0();
			}

			// Token: 0x06000038 RID: 56 RVA: 0x0000413C File Offset: 0x0000233C
			internal uint method_2(<Module>.Class0 class0_0, uint uint_1)
			{
				if (this.struct0_0.method_1(class0_0) == 0u)
				{
					return this.struct1_0[(int)((UIntPtr)uint_1)].method_1(class0_0);
				}
				uint num = 8u;
				if (this.struct0_1.method_1(class0_0) != 0u)
				{
					num += 8u;
					num += this.struct1_2.method_1(class0_0);
				}
				else
				{
					num += this.struct1_1[(int)((UIntPtr)uint_1)].method_1(class0_0);
				}
				return num;
			}

			// Token: 0x06000039 RID: 57 RVA: 0x000041A8 File Offset: 0x000023A8
			internal Class2()
			{
			}

			// Token: 0x0400001D RID: 29
			internal readonly <Module>.Struct1[] struct1_0 = new <Module>.Struct1[16];

			// Token: 0x0400001E RID: 30
			internal readonly <Module>.Struct1[] struct1_1 = new <Module>.Struct1[16];

			// Token: 0x0400001F RID: 31
			internal <Module>.Struct0 struct0_0 = default(<Module>.Struct0);

			// Token: 0x04000020 RID: 32
			internal <Module>.Struct0 struct0_1 = default(<Module>.Struct0);

			// Token: 0x04000021 RID: 33
			internal <Module>.Struct1 struct1_2 = new <Module>.Struct1(8);

			// Token: 0x04000022 RID: 34
			internal uint uint_0;
		}

		// Token: 0x02000007 RID: 7
		internal class Class3
		{
			// Token: 0x0600003A RID: 58 RVA: 0x000041FC File Offset: 0x000023FC
			internal void method_0(int int_2, int int_3)
			{
				if (this.struct2_0 != null)
				{
					if (this.int_1 == int_3)
					{
						if (this.int_0 == int_2)
						{
							return;
						}
					}
				}
				this.int_0 = int_2;
				this.uint_0 = (1u << int_2) - 1u;
				this.int_1 = int_3;
				uint num = 1u << this.int_1 + this.int_0;
				this.struct2_0 = new <Module>.Class1.Class3.Struct2[num];
				for (uint num2 = 0u; num2 < num; num2 += 1u)
				{
					this.struct2_0[(int)((UIntPtr)num2)].method_0();
				}
			}

			// Token: 0x0600003B RID: 59 RVA: 0x00004280 File Offset: 0x00002480
			internal void method_1()
			{
				uint num = 1u << this.int_1 + this.int_0;
				for (uint num2 = 0u; num2 < num; num2 += 1u)
				{
					this.struct2_0[(int)((UIntPtr)num2)].method_1();
				}
			}

			// Token: 0x0600003C RID: 60 RVA: 0x000042C0 File Offset: 0x000024C0
			internal uint method_2(uint uint_1, byte byte_0)
			{
				return ((uint_1 & this.uint_0) << this.int_1) + (uint)(byte_0 >> 8 - this.int_1);
			}

			// Token: 0x0600003D RID: 61 RVA: 0x000042F0 File Offset: 0x000024F0
			internal byte method_3(<Module>.Class0 class0_0, uint uint_1, byte byte_0)
			{
				return this.struct2_0[(int)((UIntPtr)this.method_2(uint_1, byte_0))].method_2(class0_0);
			}

			// Token: 0x0600003E RID: 62 RVA: 0x00004318 File Offset: 0x00002518
			internal byte method_4(<Module>.Class0 class0_0, uint uint_1, byte byte_0, byte byte_1)
			{
				return this.struct2_0[(int)((UIntPtr)this.method_2(uint_1, byte_0))].method_3(class0_0, byte_1);
			}

			// Token: 0x0600003F RID: 63 RVA: 0x000039A8 File Offset: 0x00001BA8
			internal Class3()
			{
			}

			// Token: 0x04000023 RID: 35
			internal <Module>.Class1.Class3.Struct2[] struct2_0;

			// Token: 0x04000024 RID: 36
			internal int int_0;

			// Token: 0x04000025 RID: 37
			internal int int_1;

			// Token: 0x04000026 RID: 38
			internal uint uint_0;

			// Token: 0x02000008 RID: 8
			internal struct Struct2
			{
				// Token: 0x06000040 RID: 64 RVA: 0x00004344 File Offset: 0x00002544
				internal void method_0()
				{
					this.struct0_0 = new <Module>.Struct0[768];
				}

				// Token: 0x06000041 RID: 65 RVA: 0x00004364 File Offset: 0x00002564
				internal void method_1()
				{
					for (int i = 0; i < 768; i++)
					{
						this.struct0_0[i].method_0();
					}
				}

				// Token: 0x06000042 RID: 66 RVA: 0x00004394 File Offset: 0x00002594
				internal byte method_2(<Module>.Class0 class0_0)
				{
					uint num = 1u;
					do
					{
						num = (num << 1 | this.struct0_0[(int)((UIntPtr)num)].method_1(class0_0));
					}
					while (num < 256u);
					return (byte)num;
				}

				// Token: 0x06000043 RID: 67 RVA: 0x000043C8 File Offset: 0x000025C8
				internal byte method_3(<Module>.Class0 class0_0, byte byte_0)
				{
					uint num = 1u;
					for (;;)
					{
						uint num2 = (uint)(byte_0 >> 7 & 1);
						byte_0 = (byte)(byte_0 << 1);
						uint num3 = this.struct0_0[(int)((UIntPtr)((1u + num2 << 8) + num))].method_1(class0_0);
						num = (num << 1 | num3);
						if (num2 != num3)
						{
							break;
						}
						if (num >= 256u)
						{
							goto IL_5E;
						}
					}
					while (num < 256u)
					{
						num = (num << 1 | this.struct0_0[(int)((UIntPtr)num)].method_1(class0_0));
					}
					IL_5E:
					return (byte)num;
				}

				// Token: 0x04000027 RID: 39
				internal <Module>.Struct0[] struct0_0;
			}
		}
	}

	// Token: 0x02000009 RID: 9
	internal class Class4
	{
		// Token: 0x06000044 RID: 68 RVA: 0x00004438 File Offset: 0x00002638
		internal void method_0(uint uint_3)
		{
			if (this.uint_2 != uint_3)
			{
				this.byte_0 = new byte[uint_3];
			}
			this.uint_2 = uint_3;
			this.uint_0 = 0u;
			this.uint_1 = 0u;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00004470 File Offset: 0x00002670
		internal void method_1(Stream stream_1, bool bool_0)
		{
			this.method_2();
			this.stream_0 = stream_1;
			if (!bool_0)
			{
				this.uint_1 = 0u;
				this.uint_0 = 0u;
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000449C File Offset: 0x0000269C
		internal void method_2()
		{
			this.method_3();
			this.stream_0 = null;
			Buffer.BlockCopy(new byte[this.byte_0.Length], 0, this.byte_0, 0, this.byte_0.Length);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000044D8 File Offset: 0x000026D8
		internal void method_3()
		{
			uint num = this.uint_0 - this.uint_1;
			if (num == 0u)
			{
				return;
			}
			this.stream_0.Write(this.byte_0, (int)this.uint_1, (int)num);
			if (this.uint_0 >= this.uint_2)
			{
				this.uint_0 = 0u;
			}
			this.uint_1 = this.uint_0;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00004530 File Offset: 0x00002730
		internal void method_4(uint uint_3, uint uint_4)
		{
			uint num = this.uint_0 - uint_3 - 1u;
			if (num >= this.uint_2)
			{
				num += this.uint_2;
			}
			while (uint_4 > 0u)
			{
				if (num >= this.uint_2)
				{
					num = 0u;
				}
				this.byte_0[(int)((UIntPtr)(this.uint_0++))] = this.byte_0[(int)((UIntPtr)(num++))];
				if (this.uint_0 >= this.uint_2)
				{
					this.method_3();
				}
				uint_4 -= 1u;
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000045AC File Offset: 0x000027AC
		internal void method_5(byte byte_1)
		{
			this.byte_0[(int)((UIntPtr)(this.uint_0++))] = byte_1;
			if (this.uint_0 >= this.uint_2)
			{
				this.method_3();
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000045E8 File Offset: 0x000027E8
		internal byte method_6(uint uint_3)
		{
			uint num = this.uint_0 - uint_3 - 1u;
			if (num >= this.uint_2)
			{
				num += this.uint_2;
			}
			return this.byte_0[(int)((UIntPtr)num)];
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000039A8 File Offset: 0x00001BA8
		internal Class4()
		{
		}

		// Token: 0x04000028 RID: 40
		internal byte[] byte_0;

		// Token: 0x04000029 RID: 41
		internal uint uint_0;

		// Token: 0x0400002A RID: 42
		internal Stream stream_0;

		// Token: 0x0400002B RID: 43
		internal uint uint_1;

		// Token: 0x0400002C RID: 44
		internal uint uint_2;
	}

	// Token: 0x0200000A RID: 10
	internal struct Struct3
	{
		// Token: 0x0600004C RID: 76 RVA: 0x0000461C File Offset: 0x0000281C
		internal void method_0()
		{
			this.uint_0 = 0u;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004630 File Offset: 0x00002830
		internal void method_1()
		{
			if (this.uint_0 < 4u)
			{
				this.uint_0 = 0u;
				return;
			}
			if (this.uint_0 < 10u)
			{
				this.uint_0 -= 3u;
				return;
			}
			this.uint_0 -= 6u;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004678 File Offset: 0x00002878
		internal void method_2()
		{
			this.uint_0 = ((this.uint_0 < 7u) ? 7u : 10u);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x0000469C File Offset: 0x0000289C
		internal void method_3()
		{
			this.uint_0 = ((this.uint_0 < 7u) ? 8u : 11u);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000046C0 File Offset: 0x000028C0
		internal void method_4()
		{
			this.uint_0 = ((this.uint_0 < 7u) ? 9u : 11u);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x000046E4 File Offset: 0x000028E4
		internal bool method_5()
		{
			return this.uint_0 < 7u;
		}

		// Token: 0x0400002D RID: 45
		internal uint uint_0;
	}

	// Token: 0x0200000B RID: 11
	[StructLayout(LayoutKind.Explicit, Size = 1280)]
	internal struct Struct4
	{
	}

	// Token: 0x0200000C RID: 12
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 448)]
	internal struct Struct5
	{
	}
}
