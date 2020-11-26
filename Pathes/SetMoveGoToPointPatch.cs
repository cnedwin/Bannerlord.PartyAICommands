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
	[HarmonyPatch(typeof(MobileParty), "SetMoveGoToPoint")]
	[HarmonyPatch(new Type[]
	{
		typeof(Vec2)
	})]
	public class SetMoveGoToPointPatch
	{
		private static void Postfix(MobileParty __instance, Vec2 point)
		{
			if (!Input.IsKeyDown((InputKey)Config.Value.OrderEscortEngageHoldKey) || !__instance.IsMainParty)
			{
				return;
			}
			foreach (KeyValuePair<Hero, PartyOrder> pair in PartyAICommandsBehavior.Instance.order_map)
			{
				Hero leader = pair.Key;
				PartyOrder order = pair.Value;
				if (leader != null && order != null && leader.PartyBelongedTo != null && order.Behavior == AiBehavior.EscortParty && order.TargetParty == __instance)
				{
					MobileParty ordered_party = leader.PartyBelongedTo;
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(__instance, ordered_party);
					if (distance < ordered_party.SeeingRange)
					{
						ordered_party.SetMoveEscortParty(__instance);
						order.TempTargetParty = null;
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

		private static bool Prepare()
		{
			return true;
		}
	}
}
