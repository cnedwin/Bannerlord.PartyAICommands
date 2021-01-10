using HarmonyLib;
using Helpers;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (HeroHelper), "HeroCanRecruitFromHero")]
  public class HeroCanRecruitFromHeroPatch
  {
    private static bool Prefix(ref Hero buyerHero, Hero sellerHero, int index, ref bool __result)
    {
      Hero leader = buyerHero;
      if ((leader != null ? leader.getTemplate() : (TroopRoster) null) != (TroopRoster) null && buyerHero?.PartyBelongedTo?.Party != null)
      {
        TroopRoster template = buyerHero.getTemplate();
        CharacterObject volunteerType = sellerHero.VolunteerTypes[index];
        CharacterObject troop = volunteerType;
        int troopCount = template.GetTroopCount(troop);
        if (buyerHero.PartyBelongedTo.Party.PartySizeLimit <= buyerHero.PartyBelongedTo.Party.NumberOfAllMembers)
        {
          __result = false;
          return false;
        }
        switch (troopCount)
        {
          case 0:
            __result = false;
            break;
          case 1:
            if (buyerHero.Clan == Clan.PlayerClan)
              buyerHero = CharacterObject.PlayerCharacter.HeroObject;
            __result = index < HeroHelper.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero);
            break;
          default:
            if (troopCount - buyerHero.PartyBelongedTo.MemberRoster.GetTroopCount(volunteerType) <= 0)
            {
              __result = false;
              break;
            }
            goto case 1;
        }
        return false;
      }
      if (buyerHero.Clan == Clan.PlayerClan)
        buyerHero = CharacterObject.PlayerCharacter.HeroObject;
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
