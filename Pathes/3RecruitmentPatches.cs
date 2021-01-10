using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (RecruitmentCampaignBehavior), "ApplyRecruitMercenary")]
  public class ApplyRecruitMercenaryPatch
  {
    private static bool Prefix(
      MobileParty side1Party,
      Settlement side2Party,
      CharacterObject subject,
      ref int number)
    {
      TroopRoster troopRoster;
      if (side1Party == null)
      {
        troopRoster = (TroopRoster) null;
      }
      else
      {
        Hero leaderHero = side1Party.LeaderHero;
        troopRoster = leaderHero != null ? leaderHero.getTemplate() : (TroopRoster) null;
      }
      if (!(troopRoster != (TroopRoster) null))
        return true;
      int troopCount = side1Party.LeaderHero.getTemplate().GetTroopCount(subject);
      if (side1Party.Party.PartySizeLimit <= side1Party.Party.NumberOfAllMembers || troopCount == 0)
        return false;
      if (troopCount == 1)
        return true;
      int val2 = troopCount - side1Party.MemberRoster.GetTroopCount(subject);
      if (val2 <= 0)
        return false;
      number = Math.Min(number, val2);
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
