using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (MBSaveLoad), "CheckModules")]
  public class CheckModulesPatch
  {
    public static bool missing_modules;
    public static MetaData meta_data;

    private static void Postfix(MetaData fileMetaData, List<ModuleCheckResult> __result)
    {
      CheckModulesPatch.meta_data = fileMetaData;
      if (__result.Count > 0)
        CheckModulesPatch.missing_modules = true;
      else
        CheckModulesPatch.missing_modules = false;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
