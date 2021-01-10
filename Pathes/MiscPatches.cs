
using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (SkillLevelingManager), "OnTradeProfitMade")]
  [HarmonyPatch(new Type[] {typeof (PartyBase), typeof (int)})]
  public class OnTradeProfitMadePatch
  {
    public static bool enableProfitXP = true;

    private static bool Prefix() => OnTradeProfitMadePatch.enableProfitXP;

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
