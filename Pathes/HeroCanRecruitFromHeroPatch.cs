using System;
using System.Windows.Forms;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(HeroHelper), "HeroCanRecruitFromHero")]
	public class HeroCanRecruitFromHeroPatch
	{
		private static bool Prefix(ref Hero buyerHero, Hero sellerHero, int index, ref bool __result)
		{
			if (buyerHero?.getTemplate() != null && buyerHero?.PartyBelongedTo?.Party != null)
			{
				TroopRoster template = buyerHero.getTemplate();
				CharacterObject troop = sellerHero.VolunteerTypes[index];
				int template_count = template.GetTroopCount(troop);
				if (buyerHero.PartyBelongedTo.Party.PartySizeLimit <= buyerHero.PartyBelongedTo.Party.NumberOfAllMembers)
				{
					__result = false;
					return false;
				}
				if (template_count == 0)
				{
					__result = false;
				}
				else if (template_count == 1 || template_count - buyerHero.PartyBelongedTo.MemberRoster.GetTroopCount(troop) > 0)
				{
					if (buyerHero.Clan == Clan.PlayerClan)
					{
						buyerHero = CharacterObject.PlayerCharacter.HeroObject;
					}
					__result = (index < HeroHelper.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero));
				}
				else
				{
					__result = false;
				}
				return false;
			}
			if (buyerHero.Clan == Clan.PlayerClan)
			{
				buyerHero = CharacterObject.PlayerCharacter.HeroObject;
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
