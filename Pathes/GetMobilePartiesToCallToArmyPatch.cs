using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(DefaultArmyManagementCalculationModel), "GetMobilePartiesToCallToArmy")]
	public class GetMobilePartiesToCallToArmyPatch
	{
		private static void Postfix(ref List<MobileParty> __result, MobileParty leaderParty)
		{
			if (leaderParty == MobileParty.MainParty || leaderParty.MapFaction != MobileParty.MainParty.MapFaction)
			{
				return;
			}
			for (int i = 0; i < __result.Count; i++)
			{
				MobileParty mobileParty = __result[i];
				if ((mobileParty != null && mobileParty.LeaderHero?.getOrder()?.AllowJoiningArmies == false && __result[i]?.LeaderHero != leaderParty?.LeaderHero) || __result[i] == MobileParty.MainParty)
				{
					__result.RemoveAt(i);
					i--;
				}
			}
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
