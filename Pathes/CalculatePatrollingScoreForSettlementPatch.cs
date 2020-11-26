using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(DefaultTargetScoreCalculatingModel), "CalculatePatrollingScoreForSettlement")]
	public class CalculatePatrollingScoreForSettlementPatch
	{
		private static void Postfix(DefaultTargetScoreCalculatingModel __instance, Settlement settlement, MobileParty mobileParty, ref float __result)
		{
			if (mobileParty?.LeaderHero?.getOrder() != null)
			{
				PartyOrder order = mobileParty.LeaderHero.getOrder();
				if (settlement.OwnerClan == mobileParty.LeaderHero.Clan)
				{
					__result *= order.OwnClanVillagesScoreMultiplier;
				}
				else
				{
					__result *= order.FriendlyVillagesScoreMultiplier;
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
			return false;
		}
	}
}
