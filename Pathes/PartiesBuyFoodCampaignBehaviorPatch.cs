
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PartiesBuyFoodCampaignBehavior), "TryBuyingFood")]
  internal class PartiesBuyFoodCampaignBehaviorPatch
  {
    private static IEnumerable<CodeInstruction> Transpiler(
      IEnumerable<CodeInstruction> instructions,
      ILGenerator il)
    {
      List<CodeInstruction> source = new List<CodeInstruction>(instructions);
      for (int index = 0; index < source.Count; ++index)
      {
        if (source[index].opcode == OpCodes.Ldc_R4)
        {
          float? operand = source[index].operand as float?;
          float num = 10f;
          if ((double) operand.GetValueOrDefault() == (double) num & operand.HasValue)
            source[index].operand = (object) Config.Value.MinimumDaysFoodToLastWhileBuyingFood;
        }
      }
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
