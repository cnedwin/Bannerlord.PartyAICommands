// Decompiled with JetBrains decompiler
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (DefaultArmyManagementCalculationModel), "GetMobilePartiesToCallToArmy")]
  public class GetMobilePartiesToCallToArmyPatch
  {
    private static void Postfix(ref List<MobileParty> __result, MobileParty leaderParty)
    {
      if (leaderParty == MobileParty.MainParty || leaderParty.MapFaction != MobileParty.MainParty.MapFaction)
        return;
      for (int index = 0; index < __result.Count; ++index)
      {
        MobileParty mobileParty = __result[index];
        int num;
        if (mobileParty == null)
        {
          num = 0;
        }
        else
        {
          Hero leaderHero = mobileParty.LeaderHero;
          bool? nullable = leaderHero != null ? leaderHero.getOrder()?.AllowJoiningArmies : new bool?();
          bool flag = false;
          num = nullable.GetValueOrDefault() == flag & nullable.HasValue ? 1 : 0;
        }
        if (num != 0 && __result[index]?.LeaderHero != leaderParty?.LeaderHero || __result[index] == MobileParty.MainParty)
        {
          __result.RemoveAt(index);
          --index;
        }
      }
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
