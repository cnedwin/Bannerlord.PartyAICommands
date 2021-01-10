
using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (DefaultTargetScoreCalculatingModel), "GetTargetScoreForFaction")]
  [HarmonyPatch(new Type[] {typeof (Settlement), typeof (Army.ArmyTypes), typeof (MobileParty), typeof (float)})]
  public class GetTargetScoreForFactionPatch
  {
    private static void Postfix(
      DefaultTargetScoreCalculatingModel __instance,
      Settlement targetSettlement,
      Army.ArmyTypes missionType,
      MobileParty mobileParty,
      float ourStrength,
      ref float __result)
    {
      if (mobileParty.LeaderHero == null)
        return;
      switch (missionType)
      {
        case Army.ArmyTypes.Besieger:
          if (targetSettlement.OwnerClan != null)
          {
            float num = Math.Max(Math.Min((float) targetSettlement.OwnerClan.Leader.GetRelation(mobileParty.LeaderHero) / 20f, 1f), -1f);
            __result *= (float) (1.0 - (double) num * ((double) num > 0.0 ? 1.0 - (double) Config.Value.RelationSiegingPositiveMultMin : (double) Config.Value.RelationSiegingNegativeMultMax - 1.0));
          }
          if (targetSettlement.Culture != mobileParty.MapFaction.Culture)
            break;
          __result *= Config.Value.SameCultureSiegingMult;
          break;
        case Army.ArmyTypes.Raider:
          if (targetSettlement.OwnerClan != null)
          {
            float num = Math.Max(Math.Min((float) targetSettlement.OwnerClan.Leader.GetRelation(mobileParty.LeaderHero) / 20f, 1f), -1f);
            __result *= (float) (1.0 - (double) num * ((double) num > 0.0 ? 1.0 - (double) Config.Value.RelationRaidingPositiveMultMin : (double) Config.Value.RelationRaidingNegativeMultMax - 1.0));
          }
          if (targetSettlement.Culture != mobileParty.LeaderHero.Culture && targetSettlement.Culture != mobileParty.MapFaction.Culture)
            break;
          __result *= Config.Value.SameCultureRaidingMult;
          break;
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
