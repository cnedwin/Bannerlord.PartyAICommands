using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (RemoveCompanionAction), "ApplyInternal")]
  public class RemoveCompanionActionPatch
  {
    private static void Postfix(Clan clan, Hero companion)
    {
      if (clan != Clan.PlayerClan)
        return;
      companion.cancelOrder();
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
