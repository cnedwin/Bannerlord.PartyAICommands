using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (RecruitmentCampaignBehavior), "GetRecruitVolunteerFromMap")]
  public class GetRecruitVolunteerFromMapPatch
  {
    private static bool Prefix(MobileParty side1Party) => side1Party?.LeaderHero?.Clan != Hero.MainHero.Clan;

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
