using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.Core;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(MBSaveLoad), "CheckModules")]
	public class CheckModulesPatch
	{
		public static bool missing_modules;

		private static void Postfix(List<ModuleCheckResult> __result)
		{
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
