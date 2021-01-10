
using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (MobileParty), "SetMoveEngageParty")]
  [HarmonyPatch(new Type[] {typeof (MobileParty)})]
  public class SetMoveEngagePartyPatch
  {
    public static void Postfix(MobileParty __instance, MobileParty party)
    {
      PartyOrder partyOrder1;
      if (party == null)
      {
        partyOrder1 = (PartyOrder) null;
      }
      else
      {
        Hero leaderHero = party.LeaderHero;
        partyOrder1 = leaderHero != null ? leaderHero.getOrder() : (PartyOrder) null;
      }
      if (partyOrder1 != null && party.LeaderHero.getOrder().Behavior == AiBehavior.EscortParty && party.LeaderHero.getOrder().TargetParty == __instance)
      {
        party.SetMoveModeHold();
      }
      else
      {
        if (!Input.IsKeyDown((InputKey) Config.Value.OrderEscortEngageHoldKey))
          return;
        foreach (KeyValuePair<Hero, PartyOrder> order in PartyAICommandsBehavior.Instance.order_map)
        {
          Hero key = order.Key;
          PartyOrder partyOrder2 = order.Value;
          if (key != null && partyOrder2 != null && (key.PartyBelongedTo != null && partyOrder2.Behavior == AiBehavior.EscortParty) && partyOrder2.TargetParty == __instance)
          {
            MobileParty partyBelongedTo = key.PartyBelongedTo;
            float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(__instance, partyBelongedTo);
            if ((double) distance < (double) partyBelongedTo.SeeingRange)
            {
              if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, __instance.MapFaction))
              {
                partyBelongedTo.SetMoveEscortParty(__instance);
              }
              else
              {
                partyBelongedTo.SetMoveEngageParty(party);
                partyOrder2.TempTargetParty = party;
              }
            }
            else if ((double) distance < (double) __instance.SeeingRange)
              InformationManager.DisplayMessage(new InformationMessage(partyBelongedTo.Name?.ToString() + " can't see our signals!", Colors.Red));
          }
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
