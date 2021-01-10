using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PartyUpgrader), "UpgradeReadyTroops")]
  public class UpgradeReadyTroopsPatch
  {
    public static bool templateProhibitsUpgrade(PartyBase party, CharacterObject upgrade)
    {
      TroopRoster troopRoster;
      if (party == null)
      {
        troopRoster = (TroopRoster) null;
      }
      else
      {
        Hero leaderHero = party.LeaderHero;
        troopRoster = leaderHero != null ? leaderHero.getTemplate() : (TroopRoster) null;
      }
      if (!(troopRoster != (TroopRoster) null))
        return false;
      Hero leaderHero1 = party.LeaderHero;
      int troopCount = leaderHero1.getTemplate().GetTroopCount(upgrade);
      switch (troopCount)
      {
        case 0:
          return true;
        case 1:
          return false;
        default:
          if (troopCount - leaderHero1.PartyBelongedTo.MemberRoster.GetTroopCount(upgrade) <= 0)
            return true;
          goto case 1;
      }
    }

    private static IEnumerable<CodeInstruction> Transpiler(
      IEnumerable<CodeInstruction> instructions,
      ILGenerator il)
    {
      MethodInfo methodInfo = AccessTools.Method(typeof (UpgradeReadyTroopsPatch), "templateProhibitsUpgrade");
      MethodInfo getMethod1 = AccessTools.Property(typeof (CharacterObject), "UpgradeTargets").GetGetMethod();
      MethodInfo getMethod2 = AccessTools.Property(typeof (BasicCultureObject), "IsBandit").GetGetMethod();
      List<CodeInstruction> source = new List<CodeInstruction>(instructions);
      object operand = (object) null;
      for (int index = 0; index < source.Count; ++index)
      {
        if (operand == null && source[index].opcode == OpCodes.Stloc_S && (source[index - 1].opcode == OpCodes.Ldelem_Ref && source[index - 2].opcode == OpCodes.Ldloc_S) && (source[index - 3].opcode == OpCodes.Callvirt && source[index - 3].operand as MethodInfo == getMethod1))
          operand = source[index].operand;
        if (operand != null && source[index].opcode == OpCodes.Stloc_S && (source[index - 1].opcode == OpCodes.Callvirt && source[index - 1].operand as MethodInfo == getMethod2))
        {
          List<System.Reflection.Emit.Label> labels = source[index + 1].labels;
          source[index + 1].labels = (List<System.Reflection.Emit.Label>) null;
          source.Insert(index + 1, new CodeInstruction(OpCodes.Ldarg_1)
          {
            labels = labels
          });
          source.Insert(index + 2, new CodeInstruction(OpCodes.Ldloc_S, operand));
          source.Insert(index + 3, new CodeInstruction(OpCodes.Call, (object) methodInfo));
          System.Reflection.Emit.Label label = il.DefineLabel();
          source.Insert(index + 4, new CodeInstruction(OpCodes.Brfalse_S, (object) label));
          source.Insert(index + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
          source.Insert(index + 6, new CodeInstruction(OpCodes.Stloc_S, source[index].operand));
          source.Insert(index + 7, new CodeInstruction(OpCodes.Nop)
          {
            labels = new List<System.Reflection.Emit.Label>() { label }
          });
          return source.AsEnumerable<CodeInstruction>();
        }
      }
      int num = (int) MessageBox.Show("Party AI Overhaul and Commands: Failed to make AI troop upgrading adhere to recruitment templates. This is not a critical bug, you may continue playing without this feature.");
      return source.AsEnumerable<CodeInstruction>();
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
