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
  [HarmonyPatch(typeof (MobileParty), "SetMoveGoToPoint")]
  [HarmonyPatch(new Type[] {typeof (Vec2)})]
  public class SetMoveGoToPointPatch
  {
    private static void Postfix(MobileParty __instance, Vec2 point)
    {
      if (!Input.IsKeyDown((InputKey) Config.Value.OrderEscortEngageHoldKey) || !__instance.IsMainParty)
        return;
      foreach (KeyValuePair<Hero, PartyOrder> order in PartyAICommandsBehavior.Instance.order_map)
      {
        Hero key = order.Key;
        PartyOrder partyOrder = order.Value;
        if (key != null && partyOrder != null && (key.PartyBelongedTo != null && partyOrder.Behavior == AiBehavior.EscortParty) && partyOrder.TargetParty == __instance)
        {
          MobileParty partyBelongedTo = key.PartyBelongedTo;
          float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(__instance, partyBelongedTo);
          if ((double) distance < (double) partyBelongedTo.SeeingRange)
          {
            partyBelongedTo.SetMoveEscortParty(__instance);
            partyOrder.TempTargetParty = (MobileParty) null;
          }
          else if ((double) distance < (double) __instance.SeeingRange)
            InformationManager.DisplayMessage(new InformationMessage(partyBelongedTo.Name?.ToString() + " can't see our signals!", Colors.Red));
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
