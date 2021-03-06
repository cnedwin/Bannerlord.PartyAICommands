﻿using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (MobileParty), "CalculateInitiativeScoresForEnemy")]
  public class CalculateInitiativeScoresForEnemyPatch
  {
    private static void Postfix(
      MobileParty __instance,
      float ____attackInitiative,
      float ____avoidInitiative,
      MobileParty enemyParty,
      ref float avoidScore,
      ref float attackScore)
    {
      if (__instance == null)
        return;
      Hero leaderHero = __instance.LeaderHero;
      int num;
      if (leaderHero == null)
      {
        num = 0;
      }
      else
      {
        PartyOrder order = leaderHero.getOrder();
        if (order == null)
        {
          num = 0;
        }
        else
        {
          int behavior = (int) order.Behavior;
          num = 1;
        }
      }
      if (num == 0 || __instance.IsJoiningArmy || (!enemyParty.IsLordParty || enemyParty.LeaderHero == null) || !enemyParty.LeaderHero.IsNoble)
        return;
      attackScore *= ____attackInitiative;
      avoidScore *= ____avoidInitiative;
      if ((double) attackScore < 1.0)
        return;
      avoidScore = 0.0f;
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
