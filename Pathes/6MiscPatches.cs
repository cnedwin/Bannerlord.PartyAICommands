using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PartyAIOverhaulCommands
{
  public class Formation_set_ArrangementOrder_Patch
  {
    private static void Postfix(ArrangementOrder __instance, Formation formation)
    {
      if (__instance.OrderEnum != ArrangementOrder.ArrangementOrderEnum.ShieldWall)
        return;
      int num1 = (int) MessageBox.Show("Shieldbearers: " + formation.GetCountOfUnitsWithCondition((Func<Agent, bool>) (agent => agent.Equipment.HasShield())).ToString());
      IFormationArrangement formationArrangement = Traverse.Create((object) formation).Property<IFormationArrangement>("arrangement").Value;
      int num2 = Traverse.Create((object) formationArrangement).Property<int>("FileCount").Value;
      int rankCount = formationArrangement.RankCount;
      MBList2D<IFormationUnit> mbList2D = Traverse.Create((object) formationArrangement).Field("_units2D").GetValue<MBList2D<IFormationUnit>>();
      for (int index1 = 0; index1 < num2; ++index1)
        ((Agent) mbList2D[index1, 0]).Equipment.HasShield();
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
