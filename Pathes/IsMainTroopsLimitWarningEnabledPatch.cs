using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PartyVM), "get_IsMainTroopsLimitWarningEnabled")]
	public class IsMainTroopsLimitWarningEnabledPatch
	{
		public static bool ignore = true;

		private static bool Prefix(ref bool __result)
		{
			if (!ignore)
			{
				__result = false;
				return false;
			}
			return true;
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
