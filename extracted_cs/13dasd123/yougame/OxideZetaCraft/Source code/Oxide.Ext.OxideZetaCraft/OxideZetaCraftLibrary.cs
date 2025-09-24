using System;
using Oxide.Core.Libraries;

namespace ZetaCraft.Oxide
{
	// Token: 0x02000010 RID: 16
	public class OxideZetaCraftLibrary : Library
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00005238 File Offset: 0x00003438
		public override bool IsGlobal
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00005248 File Offset: 0x00003448
		internal OxideZetaCraftLibrary(OxideZetaCraftExtension oxideZetaCraftExtension_1)
		{
			this.oxideZetaCraftExtension_0 = oxideZetaCraftExtension_1;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00005264 File Offset: 0x00003464
		[LibraryFunction("IsInstalled")]
		public bool IsInstalled()
		{
			return Module.module_0 != null;
		}

		// Token: 0x04000036 RID: 54
		private OxideZetaCraftExtension oxideZetaCraftExtension_0;
	}
}
