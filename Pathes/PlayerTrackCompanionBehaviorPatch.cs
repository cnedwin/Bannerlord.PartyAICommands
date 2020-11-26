using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PlayerTrackCompanionBehavior), "AddHeroToScatteredCompanions")]
	public class PlayerTrackCompanionBehaviorPatch
	{
		private static bool Prefix(Hero hero)
		{
			if (hero.PartyBelongedTo != null && hero.getOrder() != null)
			{
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
