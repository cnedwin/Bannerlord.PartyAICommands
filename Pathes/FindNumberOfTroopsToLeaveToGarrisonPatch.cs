
using HarmonyLib;
using System;
using System.Reflection;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch]
  public class FindNumberOfTroopsToLeaveToGarrisonPatch
  {
    private static MethodBase TargetMethod() => (MethodBase) AccessTools.Method(AccessTools.TypeByName("DefaultSettlementGarrisonModel"), "FindNumberOfTroopsToLeaveToGarrison", new Type[2]
    {
      typeof (MobileParty),
      typeof (Settlement)
    });

    private static void Postfix(MobileParty mobileParty, Settlement settlement, ref int __result)
    {
      PartyOrder partyOrder;
      if (mobileParty == null)
      {
        partyOrder = (PartyOrder) null;
      }
      else
      {
        Hero leaderHero = mobileParty.LeaderHero;
        partyOrder = leaderHero != null ? leaderHero.getOrder() : (PartyOrder) null;
      }
      if ((partyOrder == null || settlement.OwnerClan == mobileParty.LeaderHero.Clan || mobileParty.LeaderHero.getOrder().LeaveTroopsToGarrisonOtherClans) && (mobileParty.Army == null || mobileParty.LeaderHero.Clan != Hero.MainHero.Clan || mobileParty.Army.LeaderParty != Hero.MainHero.PartyBelongedTo))
        return;
      __result = 0;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }

    private static bool Prepare() => true;
  }
}
