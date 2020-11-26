using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(MobileParty), "GetBestInitiativeBehavior")]
	public class GetBestInitiativeBehaviorPatch
	{
		public static bool parties_around_position_patched;

		public static bool parties_distance_patched;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo getSeeingRange = AccessTools.Property(typeof(MobileParty), "SeeingRange").GetGetMethod();
			MethodInfo Distance = AccessTools.Method(typeof(Vec2), "Distance");
			MethodInfo inAiRange = AccessTools.Method(typeof(GetBestInitiativeBehaviorPatch), "inAiRange");
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count && i < 220; i++)
			{
				if (!parties_around_position_patched && codes[i].opcode == OpCodes.Ldc_R4 && codes[i + 1].opcode == OpCodes.Callvirt && (codes[i + 1].operand as MethodInfo)?.Name == "GetPartiesAroundPosition")
				{
					codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
					codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, getSeeingRange));
					parties_around_position_patched = true;
				}
				if (!parties_distance_patched && codes[i].opcode == OpCodes.Bge && codes[i - 1].opcode == OpCodes.Ldc_R4 && codes[i - 1].operand as float? == 6f && codes[i - 4].opcode == OpCodes.Call && codes[i - 4].operand as MethodInfo == Distance)
				{
					codes[i].opcode = OpCodes.Brfalse;
					codes[i - 1] = new CodeInstruction(OpCodes.Call, inAiRange);
					codes.Insert(i - 2, new CodeInstruction(OpCodes.Ldarg_0));
					parties_distance_patched = true;
					break;
				}
			}
			return codes.AsEnumerable();
		}

		private static void Postfix(MobileParty __instance, float ____attackInitiative, float ____avoidInitiative, AiBehavior bestInitiativeBehavior, MobileParty bestInitiativeTargetParty, ref float bestInitiativeBehaviorScore, Vec2 avarageEnemyVec)
		{
			if (__instance == null)
			{
				return;
			}
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
					_ = order.Behavior;
					num = 1;
				}
			}
			if (num != 0 && bestInitiativeTargetParty != null && !__instance.IsJoiningArmy && bestInitiativeBehavior == AiBehavior.EngageParty && bestInitiativeTargetParty.IsBandit && (__instance.LeaderHero.getOrder().Behavior == AiBehavior.PatrolAroundPoint || (__instance.LeaderHero.getOrder().Behavior == AiBehavior.EscortParty && ____attackInitiative > 0f)))
			{
				Vec2 relative_target_position = ((bestInitiativeTargetParty.BesiegedSettlement != null) ? bestInitiativeTargetParty.GetVisualPosition().AsVec2 : bestInitiativeTargetParty.Position2D) - __instance.Position2D;
				if (bestInitiativeBehaviorScore < 1.1f && ((double)__instance.GetCachedPureSpeed() > (double)bestInitiativeTargetParty.GetCachedPureSpeed() * 1.05 || bestInitiativeTargetParty.Bearing.DotProduct(relative_target_position) <= 0f))
				{
					bestInitiativeBehaviorScore = 1.1f;
				}
			}
		}

		public static bool inAiRange(MobileParty party, float distance)
		{
			if (party?.LeaderHero?.getOrder() != null && distance < party.SeeingRange)
			{
				return true;
			}
			if (distance < 6f)
			{
				return true;
			}
			return false;
		}

		private static void Finalizer(Exception __exception)
		{
			if (__exception != null)
			{
				MessageBox.Show(__exception.FlattenException());
			}
		}
	}
}
