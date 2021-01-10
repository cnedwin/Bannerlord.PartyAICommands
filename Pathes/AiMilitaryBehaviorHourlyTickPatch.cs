using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (AiMilitaryBehavior), "AiHourlyTick")]
  [HarmonyPatch(new Type[] {typeof (MobileParty), typeof (PartyThinkParams)})]
  public class AiMilitaryBehaviorHourlyTickPatch
  {
    private static void Postfix(MobileParty mobileParty, PartyThinkParams p)
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
      foreach (KeyValuePair<AIBehaviorTuple, float> keyValuePair in p.AIBehaviorScores.ToList<KeyValuePair<AIBehaviorTuple, float>>())
      {
        float num = keyValuePair.Value;
        IMapPoint party = keyValuePair.Key.Party;
        if (keyValuePair.Key.AiBehavior == AiBehavior.GoToSettlement)
        {
          if (!order.LeaveTroopsToGarrisonOtherClans)
            num *= AiMilitaryBehaviorHourlyTickPatch.getDoNotReplenishGarrisonCorrectionMult(mobileParty, (Settlement) party);
          p.AIBehaviorScores[keyValuePair.Key] = num * order.PartyMaintenanceScoreMultiplier;
        }
        else if (keyValuePair.Key.AiBehavior == AiBehavior.DefendSettlement || keyValuePair.Key.AiBehavior == AiBehavior.PatrolAroundPoint)
          p.AIBehaviorScores[keyValuePair.Key] = ((Settlement) keyValuePair.Key.Party).OwnerClan != mobileParty.LeaderHero.Clan ? num * order.FriendlyVillagesScoreMultiplier : num * order.OwnClanVillagesScoreMultiplier;
        else if (keyValuePair.Key.AiBehavior == AiBehavior.BesiegeSettlement || keyValuePair.Key.AiBehavior == AiBehavior.AssaultSettlement)
          p.AIBehaviorScores[keyValuePair.Key] = num * order.HostileSettlementsScoreMultiplier;
        else if (keyValuePair.Key.AiBehavior == AiBehavior.RaidSettlement)
        {
          if (!order.AllowRaidingVillages)
            p.AIBehaviorScores[keyValuePair.Key] = 0.0f;
        }
        else if (keyValuePair.Key.AiBehavior == AiBehavior.EngageParty)
          InformationManager.DisplayMessage(new InformationMessage("EngageParty: " + keyValuePair.Key.Party.Name?.ToString() + " " + keyValuePair.Value.ToString()));
      }
      if (mobileParty.IsDisbanding)
      {
        mobileParty.LeaderHero.cancelOrder();
      }
      else
      {
        if (order.Behavior == AiBehavior.None)
          return;
        if (mobileParty.Army != null && mobileParty.Army.LeaderParty == Hero.MainHero.PartyBelongedTo)
          mobileParty.LeaderHero.cancelOrder();
        else if (order.Behavior == AiBehavior.PatrolAroundPoint)
        {
          if (order.TargetSettlement == null)
          {
            int num = (int) MessageBox.Show("Patrol target settlement not set, please report this bug to the developer of Party Ai Overhaul.");
          }
          AIBehaviorTuple key = new AIBehaviorTuple((IMapPoint) order.TargetSettlement, order.Behavior);
          if (p.AIBehaviorScores.ContainsKey(key))
            p.AIBehaviorScores[key] = order.getScore(p.AIBehaviorScores[key]);
          else
            p.AIBehaviorScores.Add(key, order.getScore());
        }
        else
        {
          if (order.Behavior != AiBehavior.EscortParty)
            return;
          AIBehaviorTuple key = new AIBehaviorTuple((IMapPoint) order.TargetParty, order.Behavior);
          if (p.AIBehaviorScores.ContainsKey(key))
            p.AIBehaviorScores[key] = order.getScore(p.AIBehaviorScores[key]);
          else
            p.AIBehaviorScores.Add(key, order.getScore());
          if ((double) order.ScoreMinimum <= 1.0 || order.TargetParty != Hero.MainHero.PartyBelongedTo || mobileParty.GetNumDaysForFoodToLast() >= 3)
            return;
          InformationManager.DisplayMessage(new InformationMessage(mobileParty.Name?.ToString() + " is short on food.", Colors.Red));
        }
      }
    }

    public static float getDoNotReplenishGarrisonCorrectionMult(
      MobileParty mobileParty,
      Settlement settlement)
    {
      if (settlement.IsVillage || settlement.OwnerClan.Kingdom != mobileParty.LeaderHero.Clan.Kingdom)
        return 1f;
      float strengthPerWalledCenter = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mobileParty.MapFaction as Kingdom);
      if (mobileParty.Army != null)
        strengthPerWalledCenter *= 0.75f;
      if (settlement.IsFortification && settlement.OwnerClan != Clan.PlayerClan)
      {
        float num1 = settlement.Town.GarrisonParty != null ? settlement.Town.GarrisonParty.Party.TotalStrength : 0.0f;
        float num2 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
        float num3 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement);
        float num4 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
        float num5 = strengthPerWalledCenter * num2 * num3 * num4;
        if ((double) num1 < (double) num5)
          return (float) (1.0 / (1.0 + Math.Pow(1.0 - (double) num1 / (double) num5, 3.0) * 99.0));
      }
      return 1f;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }

    private static bool Prepare() => true;
  }
}
