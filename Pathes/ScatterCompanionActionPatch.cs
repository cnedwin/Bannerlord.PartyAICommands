using System;
using System.Linq;
using System.Windows.Forms;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(ScatterCompanionAction), "ApplyInternal")]
	public class ScatterCompanionActionPatch
	{
		private static bool Prefix(Hero companion, EndCaptivityDetail detail)
		{
			if (companion.IsActive)
			{
				return false;
			}
			return true;
		}

		private static void Postfix(Hero companion, EndCaptivityDetail detail)
		{
			if (companion.Clan != Hero.MainHero.Clan || (companion.PartyBelongedTo != null && companion.PartyBelongedTo.Party.Owner == companion) || companion?.getOrder() == null || !companion.IsAlive || companion.Clan.CommanderLimit <= companion.Clan.WarParties.Count((MobileParty p) => !p.IsGarrison && !p.IsMilitia && !p.IsVillager && !p.IsCaravan))
			{
				return;
			}
			companion.ChangeState(Hero.CharacterStates.Active);
			MobilePartyHelper.CreateNewClanMobileParty(companion, companion.Clan, out bool whatever);
			if (companion.PartyBelongedTo != null)
			{
				PartyOrder order = companion.getOrder();
				if (order.Behavior == AiBehavior.EscortParty && order.ScoreMinimum > 1f)
				{
					companion.PartyBelongedTo.SetInititave(0f, 1f, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
				}
				else
				{
					companion.PartyBelongedTo.SetInititave(order.AttackInitiative, order.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
				}
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
