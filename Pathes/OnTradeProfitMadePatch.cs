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
		typeof(PartyBase),
		typeof(int)
	})]
	public class OnTradeProfitMadePatch
	{
		public static bool enableProfitXP = true;

		private static bool Prefix()
		{
			return enableProfitXP;
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
