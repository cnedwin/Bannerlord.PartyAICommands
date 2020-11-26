using System;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace PartyAIOverhaulCommands
{
	[HarmonyPatch(typeof(DefaultTargetScoreCalculatingModel), "GetTargetScoreForFaction")]
	[HarmonyPatch(new Type[]
	{
		typeof(Settlement),
		typeof(Army.ArmyTypes),
		typeof(MobileParty),
		typeof(float)
	})]
	public class GetTargetScoreForFactionPatch
	{
		private static void Postfix(DefaultTargetScoreCalculatingModel __instance, Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength, ref float __result)
		{
			if (mobileParty.LeaderHero == null)
			{
				return;
			}
			switch (missionType)
			{
			case Army.ArmyTypes.Raider:
				if (targetSettlement.OwnerClan != null)
				{
					float relation_modifier2 = Math.Max(Math.Min((float)targetSettlement.OwnerClan.Leader.GetRelation(mobileParty.LeaderHero) / 20f, 1f), -1f);
					__result *= 1f - relation_modifier2 * ((relation_modifier2 > 0f) ? (1f - Config.Value.RelationRaidingPositiveMultMin) : (Config.Value.RelationRaidingNegativeMultMax - 1f));
				}
				if (targetSettlement.Culture == mobileParty.LeaderHero.Culture || targetSettlement.Culture == mobileParty.MapFaction.Culture)
				{
					__result *= Config.Value.SameCultureRaidingMult;
				}
				break;
			case Army.ArmyTypes.Besieger:
				if (targetSettlement.OwnerClan != null)
				{
					float relation_modifier = Math.Max(Math.Min((float)targetSettlement.OwnerClan.Leader.GetRelation(mobileParty.LeaderHero) / 20f, 1f), -1f);
					__result *= 1f - relation_modifier * ((relation_modifier > 0f) ? (1f - Config.Value.RelationSiegingPositiveMultMin) : (Config.Value.RelationSiegingNegativeMultMax - 1f));
				}
				if (targetSettlement.Culture == mobileParty.MapFaction.Culture)
				{
					__result *= Config.Value.SameCultureSiegingMult;
				}
				break;
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
