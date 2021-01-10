using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PlayerEncounter), "Finish")]
  [HarmonyPatch(new Type[] {typeof (bool)})]
  public class PlayerEncounterPatch
  {
    private static bool Prefix(bool forcePlayerOutFromSettlement)
    {
      if (!forcePlayerOutFromSettlement)
      {
        MobileParty encounteredMobileParty = PlayerEncounter.EncounteredMobileParty;
        PartyOrder partyOrder;
        if (encounteredMobileParty == null)
        {
          partyOrder = (PartyOrder) null;
        }
        else
        {
          Hero leaderHero = encounteredMobileParty.LeaderHero;
          partyOrder = leaderHero != null ? leaderHero.getOrder() : (PartyOrder) null;
        }
        if (partyOrder != null && PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().Behavior == AiBehavior.EscortParty && PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TargetParty == Hero.MainHero.PartyBelongedTo)
        {
          PlayerEncounter.EncounteredMobileParty.SetMoveEscortParty(PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TargetParty);
          PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TempTargetParty = (MobileParty) null;
        }
      }
      return true;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
