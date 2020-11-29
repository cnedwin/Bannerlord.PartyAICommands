using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(MobileParty), "SetMoveEngageParty")]
	[HarmonyPatch(new Type[]
	{
		typeof(MobileParty)
	})]
	public class SetMoveEngagePartyPatch
	{
		public static void Postfix(MobileParty __instance, MobileParty party)
		{
			if (party?.LeaderHero?.getOrder() != null && party.LeaderHero.getOrder().Behavior == AiBehavior.EscortParty && party.LeaderHero.getOrder().TargetParty == __instance)
			{
				party.SetMoveModeHold();
			}
			else
			{
				if (!Input.IsKeyDown((InputKey)Config.Value.OrderEscortEngageHoldKey))
				{
					return;
				}
				foreach (KeyValuePair<Hero, PartyOrder> pair in PartyAICommandsBehavior.Instance.order_map)
				{
					Hero leader = pair.Key;
					PartyOrder order = pair.Value;
					if (leader == null || order == null || leader.PartyBelongedTo == null || order.Behavior != AiBehavior.EscortParty || order.TargetParty != __instance)
					{
						continue;
					}
					MobileParty ordered_party = leader.PartyBelongedTo;
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(__instance, ordered_party);
					if (distance < ordered_party.SeeingRange)
					{
						if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, __instance.MapFaction))
						{
							ordered_party.SetMoveEscortParty(__instance);
							continue;
						}
						ordered_party.SetMoveEngageParty(party);
						order.TempTargetParty = party;
					}
					else if (distance < __instance.SeeingRange)
					{
						InformationManager.DisplayMessage(new InformationMessage(ordered_party.Name?.ToString() + " can't see our signals!", Colors.Red));
					}
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
