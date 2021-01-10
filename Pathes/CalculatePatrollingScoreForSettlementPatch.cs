
using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (DefaultTargetScoreCalculatingModel), "CalculatePatrollingScoreForSettlement")]
  public class CalculatePatrollingScoreForSettlementPatch
  {
    private static void Postfix(
      DefaultTargetScoreCalculatingModel __instance,
      Settlement settlement,
      MobileParty mobileParty,
      ref float __result)
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
      if (partyOrder == null)
        return;
      PartyOrder order = mobileParty.LeaderHero.getOrder();
      if (settlement.OwnerClan == mobileParty.LeaderHero.Clan)
        __result *= order.OwnClanVillagesScoreMultiplier;
      else
        __result *= order.FriendlyVillagesScoreMultiplier;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }

    private static bool Prepare() => false;
  }
}
