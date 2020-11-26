using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(RemoveCompanionAction), "ApplyInternal")]
	public class RemoveCompanionActionPatch
	{
		private static void Postfix(Clan clan, Hero companion)
		{
			if (clan == Clan.PlayerClan)
			{
				companion.cancelOrder();
			}
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
