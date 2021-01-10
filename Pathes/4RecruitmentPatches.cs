using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PartyVM), "get_IsMainTroopsLimitWarningEnabled")]
  public class IsMainTroopsLimitWarningEnabledPatch
  {
    public static bool ignore = true;

    private static bool Prefix(ref bool __result)
    {
      if (IsMainTroopsLimitWarningEnabledPatch.ignore)
        return true;
      __result = false;
      return false;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
