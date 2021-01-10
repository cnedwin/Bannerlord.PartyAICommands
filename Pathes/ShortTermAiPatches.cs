
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (MobileParty), "GetBestInitiativeBehavior")]
  public class GetBestInitiativeBehaviorPatch
  {
    public static bool parties_around_position_patched;
    public static bool parties_distance_patched;

    private static IEnumerable<CodeInstruction> Transpiler(
      IEnumerable<CodeInstruction> instructions)
    {
      MethodInfo getMethod = AccessTools.Property(typeof (MobileParty), "SeeingRange").GetGetMethod();
      MethodInfo methodInfo1 = AccessTools.Method(typeof (Vec2), "Distance");
      MethodInfo methodInfo2 = AccessTools.Method(typeof (GetBestInitiativeBehaviorPatch), "inAiRange");
      List<CodeInstruction> source = new List<CodeInstruction>(instructions);
      for (int index = 0; index < source.Count && index < 220; ++index)
      {
        if (!GetBestInitiativeBehaviorPatch.parties_around_position_patched && source[index].opcode == OpCodes.Ldc_R4 && source[index + 1].opcode == OpCodes.Callvirt)
        {
          MethodInfo operand = source[index + 1].operand as MethodInfo;
          if (((object) operand != null ? operand.Name : (string) null) == "GetPartiesAroundPosition")
          {
            source[index] = new CodeInstruction(OpCodes.Ldarg_0);
            source.Insert(index + 1, new CodeInstruction(OpCodes.Call, (object) getMethod));
            GetBestInitiativeBehaviorPatch.parties_around_position_patched = true;
          }
        }
        if (!GetBestInitiativeBehaviorPatch.parties_distance_patched && source[index].opcode == OpCodes.Bge && source[index - 1].opcode == OpCodes.Ldc_R4)
        {
          float? operand = source[index - 1].operand as float?;
          float num = 6f;
          if ((double) operand.GetValueOrDefault() == (double) num & operand.HasValue && source[index - 4].opcode == OpCodes.Call && source[index - 4].operand as MethodInfo == methodInfo1)
          {
            source[index].opcode = OpCodes.Brfalse;
            source[index - 1] = new CodeInstruction(OpCodes.Call, (object) methodInfo2);
            source.Insert(index - 2, new CodeInstruction(OpCodes.Ldarg_0));
            GetBestInitiativeBehaviorPatch.parties_distance_patched = true;
            break;
          }
        }
      }
      return source.AsEnumerable<CodeInstruction>();
    }

    private static void Postfix(
      MobileParty __instance,
      float ____attackInitiative,
      float ____avoidInitiative,
      AiBehavior bestInitiativeBehavior,
      MobileParty bestInitiativeTargetParty,
      ref float bestInitiativeBehaviorScore,
      Vec2 avarageEnemyVec)
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
      if (num == 0 || bestInitiativeTargetParty == null || (__instance.IsJoiningArmy || bestInitiativeBehavior != AiBehavior.EngageParty) || !bestInitiativeTargetParty.IsBandit || __instance.LeaderHero.getOrder().Behavior != AiBehavior.PatrolAroundPoint && (__instance.LeaderHero.getOrder().Behavior != AiBehavior.EscortParty || (double) ____attackInitiative <= 0.0))
        return;
      Vec2 v = (bestInitiativeTargetParty.BesiegedSettlement != null ? bestInitiativeTargetParty.GetVisualPosition().AsVec2 : bestInitiativeTargetParty.Position2D) - __instance.Position2D;
      if ((double) bestInitiativeBehaviorScore >= 1.10000002384186 || (double) __instance.GetCachedPureSpeed() <= (double) bestInitiativeTargetParty.GetCachedPureSpeed() * 1.05 && (double) bestInitiativeTargetParty.Bearing.DotProduct(v) > 0.0)
        return;
      bestInitiativeBehaviorScore = 1.1f;
    }

    public static bool inAiRange(MobileParty party, float distance)
    {
      PartyOrder partyOrder;
      if (party == null)
      {
        partyOrder = (PartyOrder) null;
      }
      else
      {
        Hero leaderHero = party.LeaderHero;
        partyOrder = leaderHero != null ? leaderHero.getOrder() : (PartyOrder) null;
      }
      return partyOrder != null && (double) distance < (double) party.SeeingRange || (double) distance < 6.0;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
