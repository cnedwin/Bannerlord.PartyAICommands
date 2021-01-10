// Decompiled with JetBrains decompiler
// Type: PartyAIOverhaulCommands.EndCaptivityActionPatch
// Assembly: PartyAIOverhaulCommands, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1884D07-B28E-496E-845C-CC173262A25F
// Assembly location: F:\Downloads\Party AI Overhaul and Commands Continue-2594-2-4-96-1610237294\Modules\PartyAIOverhaulCommandsCont\bin\Win64_Shipping_Client\PartyAIOverhaulCommands.dll

using HarmonyLib;
using Helpers;
using System;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
  [HarmonyPatch(typeof (EndCaptivityAction), "ApplyInternal")]
  public class EndCaptivityActionPatch
  {
    private static bool Prefix(Hero prisoner, EndCaptivityDetail detail)
    {
      if (prisoner.Clan != Hero.MainHero.Clan || prisoner.PartyBelongedToAsPrisoner == null || (prisoner.IsActive || !prisoner.IsAlive) || prisoner.PartyBelongedTo != null && prisoner.PartyBelongedTo.Party.Owner == prisoner || detail != EndCaptivityDetail.ReleasedAfterBattle && detail != EndCaptivityDetail.ReleasedAfterPeace && detail != EndCaptivityDetail.RemovedParty || (prisoner.Clan.CommanderLimit <= prisoner.Clan.WarParties.Count<MobileParty>((Func<MobileParty, bool>) (p => !p.IsGarrison && !p.IsMilitia && !p.IsVillager && !p.IsCaravan)) || (double) prisoner.PartyBelongedToAsPrisoner.Position2D.DistanceSquared(MobileParty.MainParty.VisualPosition2DWithoutError) >= 25.0))
        return true;
      StatisticsDataLogHelper.AddLog(StatisticsDataLogHelper.LogAction.EndCaptivityAction);
      PartyBase belongedToAsPrisoner = prisoner.PartyBelongedToAsPrisoner;
      IFaction faction = belongedToAsPrisoner != null ? belongedToAsPrisoner.MapFaction : (IFaction) CampaignData.NeutralFaction;
      Traverse.Create((object) CampaignEventDispatcher.Instance).Method("OnHeroPrisonerReleased", new Type[4]
      {
        typeof (Hero),
        typeof (PartyBase),
        typeof (IFaction),
        typeof (EndCaptivityDetail)
      }, (object[]) null).GetValue((object) prisoner, (object) belongedToAsPrisoner, (object) faction, (object) detail);
      EndCaptivityActionPatch.SpawnPartyAtPosition(prisoner, MobileParty.MainParty.VisualPosition2DWithoutError);
      if (prisoner.PartyBelongedTo != null && (prisoner != null ? prisoner.getOrder() : (PartyOrder) null) != null)
      {
        PartyOrder order = prisoner.getOrder();
        if (order.Behavior == AiBehavior.EscortParty && (double) order.ScoreMinimum > 1.0)
        {
          if ((double) Campaign.Current.Models.MapDistanceModel.GetDistance(prisoner.PartyBelongedTo, MobileParty.MainParty) > 15.0)
            prisoner.PartyBelongedTo.SetInititave(0.0f, 1f, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
          else
            prisoner.PartyBelongedTo.SetInititave(order.AttackInitiative, order.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
          prisoner.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
        }
      }
      return false;
    }

    private static void SpawnPartyAtPosition(Hero hero, Vec2 position)
    {
      if (hero.IsActive || !hero.IsAlive)
        return;
      hero.ChangeState(Hero.CharacterStates.Active);
      GiveGoldAction.ApplyBetweenCharacters((Hero) null, hero, 3000, true);
      MobilePartyHelper.SpawnLordParty(hero, position, 0.5f);
    }

    private static void Finalizer(Exception __exception)
    {
      if (__exception == null)
        return;
      int num = (int) MessageBox.Show(__exception.FlattenException());
    }
  }
}
