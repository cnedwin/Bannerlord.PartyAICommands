using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PartiesBuyHorseCampaignBehavior), "OnSettlementEntered")]
	public class PartiesBuyHorseCampaignBehaviorOnSettlementEnteredPatch
	{
		private static bool Prefix(PartiesBuyHorseCampaignBehavior __instance, MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown)
			{
				_ = Campaign.CurrentTime;
				int budget = Math.Min(100000, mobileParty.Leader.HeroObject.Gold);
				int numberOfMounts = mobileParty.Party.NumberOfMounts;
				if (numberOfMounts > mobileParty.Party.NumberOfRegularMembers)
				{
					return false;
				}
				Town component = settlement.GetComponent<Town>();
				if (component.MarketData.GetItemCountOfCategory(DefaultItemCategories.Horse) == 0)
				{
					return false;
				}
				float averageHorseValue = DefaultItemCategories.Horse.AverageValue;
				float ourHorsesValueToBudgetRatio = averageHorseValue * (float)numberOfMounts / (float)budget;
				if (ourHorsesValueToBudgetRatio < 0.08f)
				{
					float randomFloat = MBRandom.RandomFloat;
					float randomFloat2 = MBRandom.RandomFloat;
					float randomFloat3 = MBRandom.RandomFloat;
					float num3 = (0.08f - ourHorsesValueToBudgetRatio) * (float)budget * randomFloat * randomFloat2 * randomFloat3;
					if (num3 > (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * averageHorseValue)
					{
						num3 = (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * averageHorseValue;
					}
					Traverse.Create(__instance).Method("BuyHorses", new Type[3]
					{
						typeof(MobileParty),
						typeof(Town),
						typeof(float)
					}).GetValue(mobileParty, component, num3);
				}
			}
			if (mobileParty != null && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown)
			{
				float totalValueOfOurHorses = 0f;
				for (int i = mobileParty.ItemRoster.Count - 1; i >= 0; i--)
				{
					ItemRosterElement subject = mobileParty.ItemRoster[i];
					if (subject.EquipmentElement.Item.IsMountable)
					{
						totalValueOfOurHorses += (float)(subject.Amount * subject.EquipmentElement.Item.Value);
					}
					else if (!subject.EquipmentElement.Item.IsFood)
					{
						SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, subject.Amount, settlement);
					}
				}
				float budget2 = Math.Min(100000f, mobileParty.LeaderHero.Gold);
				float amount_horses_to_keep = mobileParty.Party.PartySizeLimit - mobileParty.Party.NumberOfMenWithHorse;
				if (totalValueOfOurHorses > budget2 * 0.1f)
				{
					for (int j = (int)((float)mobileParty.Party.NumberOfMounts - amount_horses_to_keep); j < 10; j++)
					{
						ItemRosterElement mostExpensiveHorse = default(ItemRosterElement);
						int mostExpensiveHorseValue = 0;
						foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
						{
							if (itemRosterElement.EquipmentElement.Item.IsMountable && itemRosterElement.EquipmentElement.Item.Value > mostExpensiveHorseValue)
							{
								mostExpensiveHorseValue = itemRosterElement.EquipmentElement.Item.Value;
								mostExpensiveHorse = itemRosterElement;
							}
						}
						if (mostExpensiveHorseValue <= 0)
						{
							break;
						}
						SellItemsAction.Apply(mobileParty.Party, settlement.Party, mostExpensiveHorse, 1, settlement);
						totalValueOfOurHorses -= (float)mostExpensiveHorseValue;
						if (totalValueOfOurHorses < budget2 * 0.1f)
						{
							break;
						}
					}
				}
				return false;
			}
			return true;
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
