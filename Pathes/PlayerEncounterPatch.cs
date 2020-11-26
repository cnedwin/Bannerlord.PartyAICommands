using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(PlayerEncounter), "Finish")]
	[HarmonyPatch(new Type[]
	{
		typeof(bool)
	})]
	public class PlayerEncounterPatch
	{
		private static bool Prefix(bool forcePlayerOutFromSettlement)
		{
			if (!forcePlayerOutFromSettlement && PlayerEncounter.EncounteredMobileParty?.LeaderHero?.getOrder() != null && PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().Behavior == AiBehavior.EscortParty && PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TargetParty == Hero.MainHero.PartyBelongedTo)
			{
				PlayerEncounter.EncounteredMobileParty.SetMoveEscortParty(PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TargetParty);
				PlayerEncounter.EncounteredMobileParty.LeaderHero.getOrder().TempTargetParty = null;
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

		private static bool Prepare()
		{
			return true;
		}
	}
}
