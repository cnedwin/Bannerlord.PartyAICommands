using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(TroopRoster), "AddToCounts")]
	[HarmonyPatch(new Type[]
	{
		typeof(CharacterObject),
		typeof(int),
		typeof(bool),
		typeof(int),
		typeof(int),
		typeof(bool),
		typeof(int)
	})]
	public class AddToCountsPatch
	{
		private static bool Prefix(TroopRoster __instance, CharacterObject character, int count, int index, ref int __result)
		{
			PartyBase partyBase = Traverse.Create(__instance).Property<PartyBase>("OwnerParty").Value;
			if (partyBase?.MobileParty?.LeaderHero != null && partyBase.MobileParty.LeaderHero != Hero.MainHero && partyBase?.MobileParty?.LeaderHero?.Clan == Hero.MainHero.Clan && partyBase.MapEvent != null && partyBase.MapEvent.HasWinner && partyBase.MemberRoster != null && partyBase.PrisonRoster != null && character != null)
			{
				Hero hero = partyBase.MobileParty.LeaderHero;
				if (__instance == partyBase.MemberRoster)
				{
					if (partyBase.PartySizeLimit <= partyBase.NumberOfAllMembers)
					{
						__result = -1;
						return false;
					}
					if (hero.getTemplate() != null)
					{
						int template_count = hero.getTemplate().GetTroopCount(character);
						if (template_count == 0)
						{
							__result = -1;
						}
						else if (template_count == 1 || template_count - hero.PartyBelongedTo.MemberRoster.GetTroopCount(character) > 0)
						{
							return true;
						}
						__result = -1;
						return false;
					}
				}
				else if (__instance == partyBase.PrisonRoster)
				{
					if (partyBase.PrisonerSizeLimit <= partyBase.NumberOfPrisoners)
					{
						goto IL_0180;
					}
					if (hero != null)
					{
						PartyOrder order = hero.getOrder();
						int num;
						if (order == null)
						{
							num = 0;
						}
						else
						{
							_ = order.StopTakingPrisoners;
							num = 1;
						}
						if (num != 0 && hero.getOrder().StopTakingPrisoners && !character.IsHero)
						{
							goto IL_0180;
						}
					}
				}
			}
			return true;
			IL_0180:
			__result = -1;
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
