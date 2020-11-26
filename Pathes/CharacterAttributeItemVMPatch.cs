using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(CharacterAttributeItemVM), "RefreshWithCurrentValues")]
	public class CharacterAttributeItemVMPatch
	{
		private static bool Prefix(CharacterAttributeItemVM __instance, ref bool ____isInSamePartyAsPlayer)
		{
			____isInSamePartyAsPlayer = true;
			return true;
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
