using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(AiMilitaryBehavior), "AiHourlyTick")]
	[HarmonyPatch(new Type[]
	{
		typeof(MobileParty),
		typeof(PartyThinkParams)
	})]
	public class AiMilitaryBehaviorHourlyTickPatch
	{
		private static void Postfix(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty?.LeaderHero?.getOrder() == null)
			{
				return;
			}
			PartyOrder order = mobileParty.LeaderHero.getOrder();
			foreach (KeyValuePair<AIBehaviorTuple, float> keyValuePair in p.AIBehaviorScores.ToList())
			{
				float value = keyValuePair.Value;
				IMapPoint target = keyValuePair.Key.Party;
				if (keyValuePair.Key.AiBehavior == AiBehavior.GoToSettlement)
				{
					if (!order.LeaveTroopsToGarrisonOtherClans)
					{
						value *= getDoNotReplenishGarrisonCorrectionMult(mobileParty, (Settlement)target);
					}
					p.AIBehaviorScores[keyValuePair.Key] = value * order.PartyMaintenanceScoreMultiplier;
				}
				else if (keyValuePair.Key.AiBehavior == AiBehavior.DefendSettlement || keyValuePair.Key.AiBehavior == AiBehavior.PatrolAroundPoint)
				{
					if (((Settlement)keyValuePair.Key.Party).OwnerClan == mobileParty.LeaderHero.Clan)
					{
						p.AIBehaviorScores[keyValuePair.Key] = value * order.OwnClanVillagesScoreMultiplier;
					}
					else
					{
						p.AIBehaviorScores[keyValuePair.Key] = value * order.FriendlyVillagesScoreMultiplier;
					}
				}
				else if (keyValuePair.Key.AiBehavior == AiBehavior.BesiegeSettlement || keyValuePair.Key.AiBehavior == AiBehavior.AssaultSettlement)
				{
					p.AIBehaviorScores[keyValuePair.Key] = value * order.HostileSettlementsScoreMultiplier;
				}
				else if (keyValuePair.Key.AiBehavior == AiBehavior.RaidSettlement)
				{
					if (!order.AllowRaidingVillages)
					{
						p.AIBehaviorScores[keyValuePair.Key] = 0f;
					}
				}
				else if (keyValuePair.Key.AiBehavior == AiBehavior.EngageParty)
				{
					InformationManager.DisplayMessage(new InformationMessage("EngageParty: " + keyValuePair.Key.Party.Name?.ToString() + " " + keyValuePair.Value));
				}
			}
			if (mobileParty.IsDisbanding)
			{
				mobileParty.LeaderHero.cancelOrder();
			}
			else
			{
				if (order.Behavior == AiBehavior.None)
				{
					return;
				}
				if (mobileParty.Army != null && mobileParty.Army.LeaderParty == Hero.MainHero.PartyBelongedTo)
				{
					mobileParty.LeaderHero.cancelOrder();
				}
				else if (order.Behavior == AiBehavior.PatrolAroundPoint)
				{
					if (order.TargetSettlement == null)
					{
						MessageBox.Show("Patrol target settlement not set, please report this bug to the developer of Party Ai Overhaul.");
					}
					AIBehaviorTuple aibehaviorTuple2 = new AIBehaviorTuple(order.TargetSettlement, order.Behavior);
					if (p.AIBehaviorScores.ContainsKey(aibehaviorTuple2))
					{
						p.AIBehaviorScores[aibehaviorTuple2] = order.getScore(p.AIBehaviorScores[aibehaviorTuple2]);
					}
					else
					{
						p.AIBehaviorScores.Add(aibehaviorTuple2, order.getScore());
					}
				}
				else if (order.Behavior == AiBehavior.EscortParty)
				{
					AIBehaviorTuple aibehaviorTuple = new AIBehaviorTuple(order.TargetParty, order.Behavior);
					if (p.AIBehaviorScores.ContainsKey(aibehaviorTuple))
					{
						p.AIBehaviorScores[aibehaviorTuple] = order.getScore(p.AIBehaviorScores[aibehaviorTuple]);
					}
					else
					{
						p.AIBehaviorScores.Add(aibehaviorTuple, order.getScore());
					}
					if (order.ScoreMinimum > 1f && order.TargetParty == Hero.MainHero.PartyBelongedTo && mobileParty.GetNumDaysForFoodToLast() < 3)
					{
						InformationManager.DisplayMessage(new InformationMessage(mobileParty.Name?.ToString() + " is short on food.", Colors.Red));
					}
				}
			}
		}

		public static float getDoNotReplenishGarrisonCorrectionMult(MobileParty mobileParty, Settlement settlement)
		{
			if (settlement.IsVillage || settlement.OwnerClan.Kingdom != mobileParty.LeaderHero.Clan.Kingdom)
			{
				return 1f;
			}
			float num21 = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mobileParty.MapFaction as Kingdom);
			if (mobileParty.Army != null)
			{
				num21 *= 0.75f;
			}
			if (settlement.IsFortification && settlement.OwnerClan != Clan.PlayerClan)
			{
				float garrionstrength = (settlement.Town.GarrisonParty != null) ? settlement.Town.GarrisonParty.Party.TotalStrength : 0f;
				float num22 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
				float num23 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement);
				float num24 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
				float garrisontarget = num21 * num22 * num23 * num24;
				if (garrionstrength < garrisontarget)
				{
					return 1f / (1f + (float)Math.Pow(1f - garrionstrength / garrisontarget, 3.0) * 99f);
				}
			}
			return 1f;
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
