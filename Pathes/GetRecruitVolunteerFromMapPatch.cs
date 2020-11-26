using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "GetRecruitVolunteerFromMap")]
	public class GetRecruitVolunteerFromMapPatch
	{
		private static bool Prefix(MobileParty side1Party)
		{
			if (side1Party?.LeaderHero?.Clan == Hero.MainHero.Clan)
			{
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
