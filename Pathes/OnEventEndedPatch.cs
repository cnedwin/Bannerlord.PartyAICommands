using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(MobileParty), "OnEventEnded")]
	[HarmonyPatch(new Type[]
	{
		typeof(MapEvent)
	})]
	public class OnEventEndedPatch
	{
		private static void Postfix(MobileParty __instance, MapEvent mapEvent)
		{
			if (__instance?.LeaderHero?.getOrder() == null || __instance.LeaderHero.getOrder().Behavior != AiBehavior.EscortParty)
			{
				return;
			}
			foreach (KeyValuePair<Hero, PartyOrder> pair in PartyAICommandsBehavior.Instance.order_map)
			{
				Hero leader = pair.Key;
				PartyOrder order = pair.Value;
				if (leader != null && order != null && leader.PartyBelongedTo != null && order.Behavior == AiBehavior.EscortParty && order.TargetParty == MobileParty.MainParty)
				{
					leader.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
					order.TempTargetParty = null;
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

		private static bool Prepare()
		{
			return true;
		}
	}
}
