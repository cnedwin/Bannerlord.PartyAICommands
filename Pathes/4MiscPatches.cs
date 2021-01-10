using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PlayerTrackCompanionBehavior), "AddHeroToScatteredCompanions")]
  public class PlayerTrackCompanionBehaviorPatch
  {
    private static bool Prefix(Hero hero) => hero.PartyBelongedTo == null || hero.getOrder() == null;

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
