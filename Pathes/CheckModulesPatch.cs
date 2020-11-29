using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(MBSaveLoad), "CheckModules")]
	public class CheckModulesPatch
	{
		public static bool missing_modules;

		public static MetaData meta_data;

		private static void Postfix(MetaData fileMetaData, List<ModuleCheckResult> __result)
		{
			meta_data = fileMetaData;
			if (__result.Count > 0)
			{
				missing_modules = true;
			}
			else
			{
				missing_modules = false;
			}
		}

		private static void Finalizer(Exception __exception)
		{
			if (__exception != null)
			{
				MessageBox.Show(__exception.FlattenException());
			}
		}
	}
}
