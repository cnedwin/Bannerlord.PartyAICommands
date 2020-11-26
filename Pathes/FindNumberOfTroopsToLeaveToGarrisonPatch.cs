using System;
using System.Reflection;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch]
	public class FindNumberOfTroopsToLeaveToGarrisonPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(AccessTools.TypeByName("DefaultSettlementGarrisonModel"), "FindNumberOfTroopsToLeaveToGarrison", new Type[2]
			{
				typeof(MobileParty),
				typeof(Settlement)
			});
		}

		private static void Postfix(MobileParty mobileParty, Settlement settlement, ref int __result)
		{
			if ((mobileParty?.LeaderHero?.getOrder() != null && settlement.OwnerClan != mobileParty.LeaderHero.Clan && !mobileParty.LeaderHero.getOrder().LeaveTroopsToGarrisonOtherClans) || (mobileParty.Army != null && mobileParty.LeaderHero.Clan == Hero.MainHero.Clan && mobileParty.Army.LeaderParty == Hero.MainHero.PartyBelongedTo))
			{
				__result = 0;
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
