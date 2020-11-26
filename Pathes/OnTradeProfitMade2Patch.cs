using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(SkillLevelingManager), "OnTradeProfitMade")]
	[HarmonyPatch(new Type[]
	{
		typeof(Hero),
		typeof(int)
	})]
	public class OnTradeProfitMade2Patch
	{
		private static bool Prefix()
		{
			return OnTradeProfitMadePatch.enableProfitXP;
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
