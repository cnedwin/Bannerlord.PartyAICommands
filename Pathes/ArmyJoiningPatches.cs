using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (AiArmyMemberBehavior), "AiHourlyTick")]
  [HarmonyPatch(new Type[] {typeof (MobileParty), typeof (PartyThinkParams)})]
  public class AiArmyMemberBehaviorPatch
  {
    private static bool Prefix(MobileParty mobileParty, PartyThinkParams p)
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
      if (partyOrder == null || mobileParty.Army == null || (mobileParty.Army.LeaderParty == mobileParty || mobileParty.IsDeserterParty) || (mobileParty.LeaderHero.getOrder().AllowJoiningArmies || mobileParty.Army.LeaderParty == Hero.MainHero.PartyBelongedTo))
        return true;
      mobileParty.Army = (Army) null;
      return false;
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
