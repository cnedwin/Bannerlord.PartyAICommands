using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (MobileParty), "OnEventEnded")]
  [HarmonyPatch(new Type[] {typeof (MapEvent)})]
  public class OnEventEndedPatch
  {
    private static void Postfix(MobileParty __instance, MapEvent mapEvent)
    {
      PartyOrder partyOrder1;
      if (__instance == null)
      {
        partyOrder1 = (PartyOrder) null;
      }
      else
      {
        Hero leaderHero = __instance.LeaderHero;
        partyOrder1 = leaderHero != null ? leaderHero.getOrder() : (PartyOrder) null;
      }
      if (partyOrder1 == null || __instance.LeaderHero.getOrder().Behavior != AiBehavior.EscortParty)
        return;
      foreach (KeyValuePair<Hero, PartyOrder> order in PartyAICommandsBehavior.Instance.order_map)
      {
        Hero key = order.Key;
        PartyOrder partyOrder2 = order.Value;
        if (key != null && partyOrder2 != null && (key.PartyBelongedTo != null && partyOrder2.Behavior == AiBehavior.EscortParty) && partyOrder2.TargetParty == MobileParty.MainParty)
        {
          key.PartyBelongedTo.SetMoveEscortParty(partyOrder2.TargetParty);
          partyOrder2.TempTargetParty = (MobileParty) null;
        }
      }
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
