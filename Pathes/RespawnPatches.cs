
using HarmonyLib;
using Helpers;
using System;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (ScatterCompanionAction), "ApplyInternal")]
  public class ScatterCompanionActionPatch
  {
    private static bool Prefix(Hero companion, EndCaptivityDetail detail) => !companion.IsActive;

    private static void Postfix(Hero companion, EndCaptivityDetail detail)
    {
      if (companion.Clan != Hero.MainHero.Clan || companion.PartyBelongedTo != null && companion.PartyBelongedTo.Party.Owner == companion || ((companion != null ? companion.getOrder() : (PartyOrder) null) == null || !companion.IsAlive || companion.Clan.CommanderLimit <= companion.Clan.WarParties.Count<MobileParty>((Func<MobileParty, bool>) (p => !p.IsGarrison && !p.IsMilitia && !p.IsVillager && !p.IsCaravan))))
        return;
      companion.ChangeState(Hero.CharacterStates.Active);
      MobilePartyHelper.CreateNewClanMobileParty(companion, companion.Clan, out bool _);
      if (companion.PartyBelongedTo == null)
        return;
      PartyOrder order = companion.getOrder();
      if (order.Behavior == AiBehavior.EscortParty && (double) order.ScoreMinimum > 1.0)
        companion.PartyBelongedTo.SetInititave(0.0f, 1f, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
      else
        companion.PartyBelongedTo.SetInititave(order.AttackInitiative, order.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
