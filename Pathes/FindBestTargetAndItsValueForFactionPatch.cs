
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (AiMilitaryBehavior), "FindBestTargetAndItsValueForFaction")]
  public class FindBestTargetAndItsValueForFactionPatch
  {
    private static void Postfix(Army.ArmyTypes missionType, PartyThinkParams p)
    {
      if (missionType != Army.ArmyTypes.Besieger)
        return;
      MobileParty mobilePartyOf = p.MobilePartyOf;
      if (mobilePartyOf?.Army?.LeaderParty != mobilePartyOf || mobilePartyOf?.LeaderHero?.Clan?.Kingdom == null)
        return;
      Dictionary<AIBehaviorTuple, float> dictionary = new Dictionary<AIBehaviorTuple, float>(10);
      float num1 = 99999f;
      Settlement fromSettlement = mobilePartyOf.LeaderHero.HomeSettlement ?? mobilePartyOf.LastVisitedSettlement;
      if (fromSettlement == null)
        return;
      Settlement settlement = (Settlement) null;
      foreach (KeyValuePair<AIBehaviorTuple, float> aiBehaviorScore in p.AIBehaviorScores)
      {
        if ((double) aiBehaviorScore.Value > 0.0 && aiBehaviorScore.Key.AiBehavior == AiBehavior.BesiegeSettlement && (aiBehaviorScore.Key.Party != null && aiBehaviorScore.Key.Party is Settlement))
        {
          dictionary.Add(aiBehaviorScore.Key, aiBehaviorScore.Value);
          float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, aiBehaviorScore.Key.Party as Settlement);
          if ((double) distance < (double) num1)
          {
            num1 = distance;
            settlement = aiBehaviorScore.Key.Party as Settlement;
          }
        }
      }
      foreach (KeyValuePair<AIBehaviorTuple, float> keyValuePair in dictionary)
      {
        Settlement party = keyValuePair.Key.Party as Settlement;
        float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, party);
        float num2 = keyValuePair.Value * 1.2f * Math.Max(0.0f, (float) (1.0 - ((double) distance - (double) num1) / (party.Culture != mobilePartyOf.LeaderHero.Culture || settlement.Culture == mobilePartyOf.LeaderHero.Culture ? (double) Campaign.AverageDistanceBetweenTwoTowns / 3.0 : (double) Campaign.AverageDistanceBetweenTwoTowns)));
        p.AIBehaviorScores[keyValuePair.Key] = num2;
      }
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }

    private static bool Prepare() => Config.Value.EnableBorderOnlySieges;
  }
}
