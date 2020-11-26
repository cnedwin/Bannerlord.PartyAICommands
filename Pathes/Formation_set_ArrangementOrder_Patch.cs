using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PartyAIOverhaulCommands
{
	public class Formation_set_ArrangementOrder_Patch
	{
		private static void Postfix(ArrangementOrder __instance, Formation formation)
		{
			if (__instance.OrderEnum == ArrangementOrder.ArrangementOrderEnum.ShieldWall)
			{
				MessageBox.Show("Shieldbearers: " + formation.GetCountOfUnitsWithCondition((Agent agent) => agent.Equipment.HasShield()));
				IFormationArrangement value = Traverse.Create(formation).Property<IFormationArrangement>("arrangement").Value;
				int FileCount = Traverse.Create(value).Property<int>("FileCount").Value;
				_ = value.RankCount;
				MBList2D<IFormationUnit> units2D = Traverse.Create(value).Field("_units2D").GetValue<MBList2D<IFormationUnit>>();
				for (int i = 0; i < FileCount; i++)
				{
					((Agent)units2D[i, 0]).Equipment.HasShield();
				}
			}
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
