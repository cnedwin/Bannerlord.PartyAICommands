
using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (TroopRoster), "AddToCounts")]
  [HarmonyPatch(new Type[] {typeof (CharacterObject), typeof (int), typeof (bool), typeof (int), typeof (int), typeof (bool), typeof (int)})]
  public class AddToCountsPatch
  {
    private static bool Prefix(
      TroopRoster __instance,
      CharacterObject character,
      int count,
      int index,
      ref int __result)
    {
      PartyBase partyBase = Traverse.Create((object) __instance).Property<PartyBase>("OwnerParty").Value;
      if (partyBase?.MobileParty?.LeaderHero != null && partyBase.MobileParty.LeaderHero != Hero.MainHero && (partyBase?.MobileParty?.LeaderHero?.Clan == Hero.MainHero.Clan && partyBase.MapEvent != null && (partyBase.MapEvent.HasWinner && partyBase.MemberRoster != (TroopRoster) null) && (partyBase.PrisonRoster != (TroopRoster) null && character != null)))
      {
        Hero leaderHero = partyBase.MobileParty.LeaderHero;
        if (__instance == partyBase.MemberRoster)
        {
          if (partyBase.PartySizeLimit <= partyBase.NumberOfAllMembers)
          {
            __result = -1;
            return false;
          }
          if (leaderHero.getTemplate() != (TroopRoster) null)
          {
            int troopCount = leaderHero.getTemplate().GetTroopCount(character);
            switch (troopCount)
            {
              case 0:
                __result = -1;
                break;
              case 1:
                return true;
              default:
                if (troopCount - leaderHero.PartyBelongedTo.MemberRoster.GetTroopCount(character) > 0)
                  goto case 1;
                else
                  break;
            }
            __result = -1;
            return false;
          }
        }
        else if (__instance == partyBase.PrisonRoster)
        {
          if (partyBase.PrisonerSizeLimit > partyBase.NumberOfPrisoners)
          {
            if (leaderHero != null)
            {
              PartyOrder order = leaderHero.getOrder();
              int num1;
              if (order == null)
              {
                num1 = 0;
              }
              else
              {
                int num2 = order.StopTakingPrisoners ? 1 : 0;
                num1 = 1;
              }
              if (num1 == 0 || !leaderHero.getOrder().StopTakingPrisoners || character.IsHero)
                goto label_18;
            }
            else
              goto label_18;
          }
          __result = -1;
          return false;
        }
      }
label_18:
      return true;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
