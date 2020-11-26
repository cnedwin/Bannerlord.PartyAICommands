using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(AiMilitaryBehavior), "FindBestTargetAndItsValueForFaction")]
	public class FindBestTargetAndItsValueForFactionPatch
	{
		private static void Postfix(Army.ArmyTypes missionType, PartyThinkParams p)
		{
			if (missionType != 0)
			{
				return;
			}
			MobileParty party = p.MobilePartyOf;
			if (party?.Army?.LeaderParty != party || party?.LeaderHero?.Clan?.Kingdom == null)
			{
				return;
			}
			Dictionary<AIBehaviorTuple, float> targets = new Dictionary<AIBehaviorTuple, float>(10);
			float closest_distance = 99999f;
			Settlement home = party.LeaderHero.HomeSettlement;
			if (home == null)
			{
				home = party.LastVisitedSettlement;
			}
			if (home == null)
			{
				return;
			}
			Settlement closest_target = null;
			foreach (KeyValuePair<AIBehaviorTuple, float> pair2 in p.AIBehaviorScores)
			{
				if (pair2.Value > 0f && pair2.Key.AiBehavior == AiBehavior.BesiegeSettlement && pair2.Key.Party != null && pair2.Key.Party is Settlement)
				{
					targets.Add(pair2.Key, pair2.Value);
					float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(home, pair2.Key.Party as Settlement);
					if (distance2 < closest_distance)
					{
						closest_distance = distance2;
						closest_target = (pair2.Key.Party as Settlement);
					}
				}
			}
			foreach (KeyValuePair<AIBehaviorTuple, float> pair in targets)
			{
				Settlement target = pair.Key.Party as Settlement;
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(home, target);
				float new_score = pair.Value * 1.2f * Math.Max(0f, 1f - (distance - closest_distance) / ((target.Culture == party.LeaderHero.Culture && closest_target.Culture != party.LeaderHero.Culture) ? Campaign.AverageDistanceBetweenTwoTowns : (Campaign.AverageDistanceBetweenTwoTowns / 3f)));
				p.AIBehaviorScores[pair.Key] = new_score;
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
			return Config.Value.EnableBorderOnlySieges;
		}
	}
}
