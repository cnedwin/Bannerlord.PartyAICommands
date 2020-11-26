using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(CharacterVM), "CanAddFocusToSkillWithFocusAmount")]
	public class CanAddFocusToSkillWithFocusAmountPatch
	{
		private static bool Prefix(CharacterVM __instance, ref bool __result, int currentFocusAmount)
		{
			__result = (currentFocusAmount < 5 && __instance.UnspentCharacterPoints > 0);
			return false;
		}

		private static void Finalizer(Exception __exception)
		{
			if (__exception != null)
			{
				MessageBox.Show(__exception.FlattenException());
			}
		}

		private static bool Prepare()
		{
			return true;
		}
	}
}
