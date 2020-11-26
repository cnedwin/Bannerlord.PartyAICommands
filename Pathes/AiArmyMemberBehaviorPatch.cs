using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(AiArmyMemberBehavior), "AiHourlyTick")]
	[HarmonyPatch(new Type[]
	{
		typeof(MobileParty),
		typeof(PartyThinkParams)
	})]
	public class AiArmyMemberBehaviorPatch
	{
		private static bool Prefix(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty?.LeaderHero?.getOrder() == null || mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty || mobileParty.IsDeserterParty)
			{
				return true;
			}
			if (!mobileParty.LeaderHero.getOrder().AllowJoiningArmies && mobileParty.Army.LeaderParty != Hero.MainHero.PartyBelongedTo)
			{
				mobileParty.Army = null;
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

		private static bool Prepare()
		{
			return true;
		}
	}
}
