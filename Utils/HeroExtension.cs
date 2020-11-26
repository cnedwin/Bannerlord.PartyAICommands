using System;
using System.Windows.Forms;
using PartyAIOverhaulCommands.src.Behaviours;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
	public static class HeroExtension
	{
		public static void cancelOrder(this Hero leader)
		{
			try
			{
				if (leader?.getOrder() != null)
				{
					MobileParty party = leader.PartyBelongedTo;
					PartyAICommandsBehavior.Instance.order_map.Remove(leader);
					if (party != null)
					{
						party.SetInititave(1f, 1f, 0f);
						party.Ai.SetDoNotMakeNewDecisions(enabled: false);
					}
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
			}
		}

		public static PartyOrder getOrder(this Hero leader)
		{
			try
			{
				if (PartyAICommandsBehavior.Instance.order_map == null)
				{
					return null;
				}
				PartyAICommandsBehavior.Instance.order_map.TryGetValue(leader, out PartyOrder order);
				return order;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
				return null;
			}
		}

		public static TroopRoster getTemplate(this Hero leader)
		{
			try
			{
				if (PartyAICommandsBehavior.Instance.template_map == null)
				{
					return null;
				}
				PartyAICommandsBehavior.Instance.template_map.TryGetValue(leader, out TroopRoster template);
				return template;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
				return null;
			}
		}
	}
}
