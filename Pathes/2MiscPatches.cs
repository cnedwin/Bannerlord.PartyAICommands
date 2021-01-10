using HarmonyLib;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch]
  public class conversation_clan_member_manage_troops_on_conditionPatch
  {
    private static MethodBase TargetMethod() => (MethodBase) AccessTools.Method(AccessTools.TypeByName("LordConversationsCampaignBehavior"), "conversation_clan_member_manage_troops_on_condition", new Type[0]);

    private static bool Prefix(ref bool __result)
    {
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
