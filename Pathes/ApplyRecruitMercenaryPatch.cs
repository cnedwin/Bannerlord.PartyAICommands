using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyRecruitMercenary")]
	public class ApplyRecruitMercenaryPatch
	{
		private static bool Prefix(MobileParty side1Party, Settlement side2Party, CharacterObject subject, ref int number)
		{
			if (side1Party?.LeaderHero?.getTemplate() != null)
			{
				int template_count = side1Party.LeaderHero.getTemplate().GetTroopCount(subject);
				if (side1Party.Party.PartySizeLimit > side1Party.Party.NumberOfAllMembers)
				{
					switch (template_count)
					{
					case 0:
						break;
					case 1:
						return true;
					default:
					{
						int needed = template_count - side1Party.MemberRoster.GetTroopCount(subject);
						if (needed > 0)
						{
							number = Math.Min(number, needed);
							return true;
						}
						return false;
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
