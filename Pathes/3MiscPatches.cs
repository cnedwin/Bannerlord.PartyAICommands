using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (PartiesBuyHorseCampaignBehavior), "OnSettlementEntered")]
  public class PartiesBuyHorseCampaignBehaviorOnSettlementEnteredPatch
  {
    private static bool Prefix(
      PartiesBuyHorseCampaignBehavior __instance,
      MobileParty mobileParty,
      Settlement settlement,
      Hero hero)
    {
      if (mobileParty != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) && (mobileParty != MobileParty.MainParty && mobileParty.IsLordParty) && (mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown))
      {
        double currentTime = (double) Campaign.CurrentTime;
        int num1 = Math.Min(100000, mobileParty.Leader.HeroObject.Gold);
        int numberOfMounts = mobileParty.Party.NumberOfMounts;
        if (numberOfMounts > mobileParty.Party.NumberOfRegularMembers)
          return false;
        Town component = settlement.GetComponent<Town>();
        if (component.MarketData.GetItemCountOfCategory(DefaultItemCategories.Horse) == 0)
          return false;
        float averageValue = DefaultItemCategories.Horse.AverageValue;
        float num2 = averageValue * (float) numberOfMounts / (float) num1;
        if ((double) num2 < 0.0799999982118607)
        {
          float randomFloat1 = MBRandom.RandomFloat;
          float randomFloat2 = MBRandom.RandomFloat;
          float randomFloat3 = MBRandom.RandomFloat;
          float num3 = (0.08f - num2) * (float) num1 * randomFloat1 * randomFloat2 * randomFloat3;
          if ((double) num3 > (double) (mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * (double) averageValue)
            num3 = (float) (mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * averageValue;
          Traverse.Create((object) __instance).Method("BuyHorses", new Type[3]
          {
            typeof (MobileParty),
            typeof (Town),
            typeof (float)
          }, (object[]) null).GetValue((object) mobileParty, (object) component, (object) num3);
        }
      }
      if (mobileParty == null || mobileParty == MobileParty.MainParty || (!mobileParty.IsLordParty || mobileParty.LeaderHero == null) || (mobileParty.IsDisbanding || !settlement.IsTown))
        return true;
      float num4 = 0.0f;
      EquipmentElement equipmentElement;
      for (int index = mobileParty.ItemRoster.Count - 1; index >= 0; --index)
      {
        ItemRosterElement subject = mobileParty.ItemRoster[index];
        equipmentElement = subject.EquipmentElement;
        if (equipmentElement.Item.IsMountable)
        {
          double num1 = (double) num4;
          int amount = subject.Amount;
          equipmentElement = subject.EquipmentElement;
          int num2 = equipmentElement.Item.Value;
          double num3 = (double) (amount * num2);
          num4 = (float) (num1 + num3);
        }
        else
        {
          equipmentElement = subject.EquipmentElement;
          if (!equipmentElement.Item.IsFood)
            SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, subject.Amount, settlement);
        }
      }
      float num5 = Math.Min(100000f, (float) mobileParty.LeaderHero.Gold);
      float num6 = (float) (mobileParty.Party.PartySizeLimit - mobileParty.Party.NumberOfMenWithHorse);
      if ((double) num4 > (double) num5 * 0.100000001490116)
      {
        for (int index = (int) ((double) mobileParty.Party.NumberOfMounts - (double) num6); index < 10; ++index)
        {
          ItemRosterElement subject = new ItemRosterElement();
          int num1 = 0;
          foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
          {
            equipmentElement = itemRosterElement.EquipmentElement;
            if (equipmentElement.Item.IsMountable)
            {
              equipmentElement = itemRosterElement.EquipmentElement;
              if (equipmentElement.Item.Value > num1)
              {
                equipmentElement = itemRosterElement.EquipmentElement;
                num1 = equipmentElement.Item.Value;
                subject = itemRosterElement;
              }
            }
          }
          if (num1 > 0)
          {
            SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, 1, settlement);
            num4 -= (float) num1;
            if ((double) num4 < (double) num5 * 0.100000001490116)
              break;
          }
          else
            break;
        }
      }
      return false;
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
