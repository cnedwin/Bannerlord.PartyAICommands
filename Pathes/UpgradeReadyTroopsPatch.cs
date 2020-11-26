using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PartyUpgrader), "UpgradeReadyTroops")]
	public class UpgradeReadyTroopsPatch
	{
		public static bool templateProhibitsUpgrade(PartyBase party, CharacterObject upgrade)
		{
			if (party?.LeaderHero?.getTemplate() != null)
			{
				Hero hero = party.LeaderHero;
				int template_count = hero.getTemplate().GetTroopCount(upgrade);
				switch (template_count)
				{
				case 0:
					return true;
				default:
					if (template_count - hero.PartyBelongedTo.MemberRoster.GetTroopCount(upgrade) <= 0)
					{
						return true;
					}
					goto case 1;
				case 1:
					return false;
				}
			}
			return false;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			MethodInfo templateProhibitsUpgrade = AccessTools.Method(typeof(UpgradeReadyTroopsPatch), "templateProhibitsUpgrade");
			MethodInfo get_UpgradeTargets = AccessTools.Property(typeof(CharacterObject), "UpgradeTargets").GetGetMethod();
			MethodInfo get_IsBandit = AccessTools.Property(typeof(BasicCultureObject), "IsBandit").GetGetMethod();
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			object character_object = null;
			for (int i = 0; i < codes.Count; i++)
			{
				if (character_object == null && codes[i].opcode == OpCodes.Stloc_S && codes[i - 1].opcode == OpCodes.Ldelem_Ref && codes[i - 2].opcode == OpCodes.Ldloc_S && codes[i - 3].opcode == OpCodes.Callvirt && codes[i - 3].operand as MethodInfo == get_UpgradeTargets)
				{
					character_object = codes[i].operand;
				}
				if (character_object != null && codes[i].opcode == OpCodes.Stloc_S && codes[i - 1].opcode == OpCodes.Callvirt && codes[i - 1].operand as MethodInfo == get_IsBandit)
				{
					List<System.Reflection.Emit.Label> isBandit_labels = codes[i + 1].labels;
					codes[i + 1].labels = null;
					codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_1)
					{
						labels = isBandit_labels
					});
					codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldloc_S, character_object));
					codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, templateProhibitsUpgrade));
					System.Reflection.Emit.Label jumpToEnd = il.DefineLabel();
					codes.Insert(i + 4, new CodeInstruction(OpCodes.Brfalse_S, jumpToEnd));
					codes.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
					codes.Insert(i + 6, new CodeInstruction(OpCodes.Stloc_S, codes[i].operand));
					codes.Insert(i + 7, new CodeInstruction(OpCodes.Nop)
					{
						labels = new List<System.Reflection.Emit.Label>
						{
							jumpToEnd
						}
					});
					return codes.AsEnumerable();
				}
			}
			MessageBox.Show("Party AI Overhaul and Commands: Failed to make AI troop upgrading adhere to recruitment templates. This is not a critical bug, you may continue playing without this feature.");
			return codes.AsEnumerable();
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
