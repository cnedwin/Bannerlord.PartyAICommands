using PartyAIOverhaulCommands.src.Behaviours;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;

namespace PartyAIOverhaulCommands
{
    public static class HeroExtension
    {
        public static void cancelOrder(this Hero leader)
        {
            try
            {
                if ((leader != null ? leader.getOrder() : (PartyOrder)null) == null)
                    return;
                MobileParty partyBelongedTo = leader.PartyBelongedTo;
                PartyAICommandsBehavior.Instance.order_map.Remove(leader);
                if (partyBelongedTo == null)
                    return;
                partyBelongedTo.SetInititave(1f, 1f, 0.0f);
                partyBelongedTo.Ai.SetDoNotMakeNewDecisions(false);
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
            }
        }

        public static PartyOrder getOrder(this Hero leader)
        {
            try
            {
                if (PartyAICommandsBehavior.Instance.order_map == null)
                    return (PartyOrder)null;
                PartyOrder partyOrder;
                PartyAICommandsBehavior.Instance.order_map.TryGetValue(leader, out partyOrder);
                return partyOrder;
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
                return (PartyOrder)null;
            }
        }

        public static TroopRoster getTemplate(this Hero leader)
        {
            try
            {
                if (PartyAICommandsBehavior.Instance.template_map == null)
                    return (TroopRoster)null;
                TroopRoster troopRoster;
                PartyAICommandsBehavior.Instance.template_map.TryGetValue(leader, out troopRoster);
                return troopRoster;
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
                return (TroopRoster)null;
            }
        }
    }
}