using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PartiesBuyFoodCampaignBehavior), "TryBuyingFood")]
	internal class PartiesBuyFoodCampaignBehaviorPatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand as float? == 10f)
				{
					codes[i].operand = Config.Value.MinimumDaysFoodToLastWhileBuyingFood;
				}
			}
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
