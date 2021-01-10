using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace PartyAIOverhaulCommands
{
    public class PartyOrder
    {
        [SaveableField(16)]
        private float _avoidInitiative = -1f;
        [SaveableField(17)]
        private float _attackInitiative = -1f;
        [SaveableField(13)]
        private MobileParty _tempTargetParty;
        [SaveableField(14)]
        private MobileParty _ownerParty;
        [SaveableField(15)]
        private Hero _ownerHero;

        public PartyOrder(Hero owner)
        {
            this._ownerHero = owner;
            this._attackInitiative = 1f;
            this._avoidInitiative = 1f;
        }

        public float getScore(float base_score = 0.0f) => Math.Max(base_score * this.ScoreMultiplier, this.ScoreMinimum);

        [SaveableProperty(1)]
        public MobileParty TargetParty { get; private set; }

        [SaveableProperty(2)]
        public Settlement TargetSettlement { get; private set; }

        [SaveableProperty(3)]
        public AiBehavior Behavior { get; private set; }

        [SaveableProperty(4)]
        public float ScoreMultiplier { get; private set; } = 1f;

        [SaveableProperty(5)]
        public float ScoreMinimum { get; private set; }

        [SaveableProperty(6)]
        public float HostileSettlementsScoreMultiplier { get; private set; } = 1f;

        [SaveableProperty(7)]
        public float FriendlyVillagesScoreMultiplier { get; private set; } = 1f;

        [SaveableProperty(8)]
        public float PartyMaintenanceScoreMultiplier { get; private set; } = 1f;

        [SaveableProperty(9)]
        public float OwnClanVillagesScoreMultiplier { get; private set; } = 1f;

        [SaveableProperty(10)]
        public bool LeaveTroopsToGarrisonOtherClans { get; private set; } = true;

        [SaveableProperty(11)]
        public bool AllowRaidingVillages { get; private set; } = true;

        [SaveableProperty(12)]
        public bool AllowJoiningArmies { get; private set; } = true;

        public MobileParty TempTargetParty
        {
            get => this._tempTargetParty;
            set
            {
                try
                {
                    if (value == null)
                    {
                        this._tempTargetParty = (MobileParty)null;
                        if (this.OwnerParty == null)
                            return;
                        this.OwnerParty.Ai.SetDoNotMakeNewDecisions(false);
                    }
                    else
                    {
                        this._tempTargetParty = value;
                        if (this.OwnerParty == null)
                            return;
                        this.OwnerParty.Ai.SetDoNotMakeNewDecisions(true);
                    }
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
            }
        }

        public MobileParty OwnerParty => this.OwnerHero.PartyBelongedTo;

        public Hero OwnerHero
        {
            get
            {
                if (this._ownerHero == null)
                    this._ownerHero = PartyAICommandsBehavior.Instance.order_map.FirstOrDefault<KeyValuePair<Hero, PartyOrder>>((Func<KeyValuePair<Hero, PartyOrder>, bool>)(x => x.Value == this)).Key;
                return this._ownerHero;
            }
        }

        public float AvoidInitiative
        {
            get
            {
                if ((double)this._avoidInitiative < 0.0 && this.OwnerParty != null)
                    this._avoidInitiative = Traverse.Create((object)this.OwnerParty).Field("_avoidInitiative").GetValue<float>();
                return this._avoidInitiative;
            }
            set
            {
                this._avoidInitiative = value;
                if (this.OwnerParty == null)
                    return;
                this.OwnerParty.SetInititave(this.AttackInitiative, this.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
            }
        }

        public float AttackInitiative
        {
            get
            {
                if ((double)this._attackInitiative < 0.0 && this.OwnerParty != null)
                    this._attackInitiative = Traverse.Create((object)this.OwnerParty).Field("_attackInitiative").GetValue<float>();
                return this._attackInitiative;
            }
            set
            {
                this._attackInitiative = value;
                if (this.OwnerParty == null)
                    return;
                this.OwnerParty.SetInititave(this.AttackInitiative, this.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
            }
        }

        [SaveableProperty(18)]
        public bool StopRecruitingTroops { get; private set; }

        [SaveableProperty(19)]
        public bool StopTakingPrisoners { get; private set; }

        public class PartyOrderBuilder
        {
            public static readonly PartyOrder.PartyOrderBuilder Instance = new PartyOrder.PartyOrderBuilder();
            private readonly int lines_per_page = 8;
            private int lines_current_page = 1;
            private int line_index;
            private bool findplayerifbored;
            private PartyOrder order;
            private ConversationManager conversation_manager;
            private CampaignGameStarter cgs;
            private static MobileParty template_party;
            private static MobileParty all_recruits_party;
            private static MobileParty template_limits_party;
            private Equipment civilian_equipment_backup;
            private Equipment battle_equipment_backup;

            public void RegisterDialogue(CampaignGameStarter campaignGameStarter)
            {
                try
                {
                    this.cgs = campaignGameStarter;
                    campaignGameStarter.AddPlayerLine("give_party_order", "hero_main_options", "give_party_order_reply", "{=mZEVvOi4}I have a new assignment for you.", new ConversationSentence.OnConditionDelegate(this.conversation_is_clan_party_on_condition), (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.lines_current_page = 1;
                        this.order = new PartyOrder(Hero.OneToOneConversationHero);
                    }), 96);
                    campaignGameStarter.AddDialogLine("give_party_order_reply", "give_party_order_reply", "give_party_order_choose", "{=GVM3O4PW}What is it?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol", "give_party_order_choose", "give_party_order_reply2", "{=sFk6o6pM}I'm sending you on a patrol.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind", "give_party_order_choose", "lord_pretalk", "{=IN6LdIzh}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddDialogLine("give_party_order_reply2", "give_party_order_reply2", "give_party_order_patrol_choose", "{=3LDRNP3W}Where should I patrol?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.line_index = 0));
                    foreach (Settlement settlement1 in (IEnumerable<Settlement>)Campaign.Current.Settlements)
                    {
                        Settlement settlement = settlement1;
                        int priority = settlement.OwnerClan == Hero.MainHero.Clan ? 101 : 100;
                        campaignGameStarter.AddPlayerLine("give_party_order_patrol_" + settlement.Name.ToString(), "give_party_order_patrol_choose", "give_party_order_patrol_ask_additional", "{=xa1kmmj5}" + settlement.Name.ToString(), (ConversationSentence.OnConditionDelegate)(() => this.get_is_line_on_page(settlement.MapFaction == Hero.MainHero.MapFaction || (double)Campaign.Current.Models.MapDistanceModel.GetDistance(Hero.MainHero.PartyBelongedTo, settlement) < (double)Campaign.AverageDistanceBetweenTwoTowns)), (ConversationSentence.OnConsequenceDelegate)(() => this.order.TargetSettlement = settlement), priority);
                    }
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_more", "give_party_order_patrol_choose", "give_party_order_reply2", "{=XRGi3bSd}More...", (ConversationSentence.OnConditionDelegate)(() => this.lines_per_page < this.line_index), (ConversationSentence.OnConsequenceDelegate)(() => this.lines_current_page = this.lines_per_page * this.lines_current_page < this.line_index ? this.lines_current_page + 1 : 1));
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind1", "give_party_order_patrol_choose", "lord_pretalk", "{=gX0L27IB}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddDialogLine("give_party_order_patrol_ask_additional", "give_party_order_patrol_ask_additional", "give_party_order_patrol_additional", "{ADDITIONAL_ORDERS_PATROL_1}{ADDITIONAL_ORDERS_PATROL_2}{ADDITIONAL_ORDERS_PATROL_3}", (ConversationSentence.OnConditionDelegate)(() => this.give_party_order_patrol_additional_condition()), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_order.FriendlyVillagesScoreMultiplier", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=yX4VtrOP}Focus on protecting our clan's villages.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.FriendlyVillagesScoreMultiplier > 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.FriendlyVillagesScoreMultiplier = 0.0f));
                    campaignGameStarter.AddPlayerLine("give_party_order_order.FriendlyVillagesScoreMultiplier_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=KkjBE78p}On second thought, also come to the aid of villages of allied clans.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.FriendlyVillagesScoreMultiplier == 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.FriendlyVillagesScoreMultiplier = 1f * this.order.OwnClanVillagesScoreMultiplier));
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_distant_villages", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=pmqWOoV0}Also help the more distant villages if they raise the alarm.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.OwnClanVillagesScoreMultiplier == 1.0), (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.order.OwnClanVillagesScoreMultiplier = 1.3f;
                        this.order.FriendlyVillagesScoreMultiplier *= 1.3f;
                    }));
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_distant_villages_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=uHzw3ULH}I changed my mind, do not travel too far to help other villages.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.OwnClanVillagesScoreMultiplier > 1.0), (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.order.OwnClanVillagesScoreMultiplier = 1f;
                        if ((double)this.order.FriendlyVillagesScoreMultiplier <= 0.0)
                            return;
                        this.order.FriendlyVillagesScoreMultiplier = 1f;
                    }));
                    campaignGameStarter.AddPlayerLine("give_party_order_order.PartyMaintenanceScoreMultiplierenance", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=F2vg9SGK}Visit often the surrounding settlements for your party's needs.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.PartyMaintenanceScoreMultiplier == 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.PartyMaintenanceScoreMultiplier = 1.7f));
                    campaignGameStarter.AddPlayerLine("give_party_order_order.PartyMaintenanceScoreMultiplierenance_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=16NEiony}Actually, don't visit the surrounding settlements more than usual.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.PartyMaintenanceScoreMultiplier > 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.PartyMaintenanceScoreMultiplier = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_confirm", "give_party_order_patrol_additional", "affirm_party_order", "{=AZYIGzy8}That's it. You have your orders.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.give_party_order_patrol_confirm_consequence()));
                    campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind2", "give_party_order_patrol_additional", "lord_pretalk", "{=O1M2H0C2}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddPlayerLine("give_party_order_escort", "give_party_order_choose", "give_party_order_escort_ask_additional", "{=S7IXOvya}Your party is to follow mine.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.order._attackInitiative = 1f;
                        this.order._avoidInitiative = 1f;
                    }));
                    campaignGameStarter.AddDialogLine("give_party_order_escort_ask_additional", "give_party_order_escort_ask_additional", "give_party_order_escort_additional", "{ADDITIONAL_ORDERS_ESCORT}", (ConversationSentence.OnConditionDelegate)(() => this.give_party_order_escort_additional_condition()), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_attack_initiative", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=9VkT0QtD}Do not chase enemies until I give the signal.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._attackInitiative == 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._attackInitiative = 0.0f));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_attack_initiative_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=KbwF9ipY}On second thought, engage our enemies at will.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._attackInitiative == 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._attackInitiative = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_avoid_initiative", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=ShLWeZiY}Do not flee from dangerous enemies.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._avoidInitiative == 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._avoidInitiative = 0.0f));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_avoid_initiative_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=jdgK44KM}Actually, avoid dangerous enemies as you would otherwise", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._avoidInitiative == 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._avoidInitiative = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_stop_imprisoning", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=99JeSjud}Stop taking prisoners, except for the leading nobility.", (ConversationSentence.OnConditionDelegate)(() => !this.order.StopTakingPrisoners), (ConversationSentence.OnConsequenceDelegate)(() => this.order.StopTakingPrisoners = true));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_stop_imprisoning_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=xPRt2fdZ}I've reconsidered, feel free to take prisoners at will.", (ConversationSentence.OnConditionDelegate)(() => this.order.StopTakingPrisoners), (ConversationSentence.OnConsequenceDelegate)(() => this.order.StopTakingPrisoners = false));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_confirm", "give_party_order_escort_additional", "affirm_party_order", "{=IQKXYHAq}That's it. You have your orders.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.give_party_order_escort_confirm_consequence()));
                    campaignGameStarter.AddPlayerLine("give_party_order_escort_nevermind", "give_party_order_escort_additional", "lord_pretalk", "{=6LrwJL3W}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddPlayerLine("give_party_order_roam", "give_party_order_choose", "give_party_order_roam_ask_additional", "{=5ur2y5hP}I want you to roam the lands.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.order._attackInitiative = 1f;
                        this.order._avoidInitiative = 1f;
                        this.order.LeaveTroopsToGarrisonOtherClans = true;
                        this.order.AllowJoiningArmies = true;
                        this.order.AllowRaidingVillages = true;
                        this.order.HostileSettlementsScoreMultiplier = 1f;
                        this.findplayerifbored = false;
                        this.order.FriendlyVillagesScoreMultiplier = 1f;
                    }), 101);
                    campaignGameStarter.AddDialogLine("give_party_order_roam_ask_additional", "give_party_order_roam_ask_additional", "give_party_order_roam_additional", "{ADDITIONAL_ORDERS_ROAM_1}{ADDITIONAL_ORDERS_ROAM_2}{ADDITIONAL_ORDERS_ROAM_3}{ADDITIONAL_ORDERS_ROAM_4}{ADDITIONAL_ORDERS_ROAM_5}{ADDITIONAL_ORDERS_ROAM_6}{ADDITIONAL_ORDERS_ROAM_7}{ADDITIONAL_ORDERS_ROAM_8}", (ConversationSentence.OnConditionDelegate)(() => this.give_party_order_roam_additional_condition()), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_only_clan", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=hVxWVKX7}Don't bother protecting settlements of allied clans.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.FriendlyVillagesScoreMultiplier > 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.FriendlyVillagesScoreMultiplier = 0.0f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_only_clan_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=welScFSY}I've reconsidered, protect settlements of allied clans as well", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.FriendlyVillagesScoreMultiplier == 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.FriendlyVillagesScoreMultiplier = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_leave_garrison", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=7E06qXBJ}Never leave our troops in garrisons of allied clans.", (ConversationSentence.OnConditionDelegate)(() => this.order.LeaveTroopsToGarrisonOtherClans), (ConversationSentence.OnConsequenceDelegate)(() => this.order.LeaveTroopsToGarrisonOtherClans = false));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_leave_garrison_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=wZOn0NEF}Actually, feel free to donate troops to the garrisons of allied clans.", (ConversationSentence.OnConditionDelegate)(() => !this.order.LeaveTroopsToGarrisonOtherClans), (ConversationSentence.OnConsequenceDelegate)(() => this.order.LeaveTroopsToGarrisonOtherClans = true));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_join_army", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=hn1giswY}Do not merge with armies unless I personally lead.", (ConversationSentence.OnConditionDelegate)(() => this.order.AllowJoiningArmies), (ConversationSentence.OnConsequenceDelegate)(() => this.order.AllowJoiningArmies = false));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_join_army_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=ksQCaRwa}I will allow you to merge with armies after all.", (ConversationSentence.OnConditionDelegate)(() => !this.order.AllowJoiningArmies), (ConversationSentence.OnConsequenceDelegate)(() => this.order.AllowJoiningArmies = true));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_no_raiding", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=1EgmZn7s}Refrain from raiding any villages.", (ConversationSentence.OnConditionDelegate)(() => this.order.AllowRaidingVillages), (ConversationSentence.OnConsequenceDelegate)(() => this.order.AllowRaidingVillages = false));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_no_raiding_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=cTY5EsLq}On second thought, I allow you to raid the villages of our enemies.", (ConversationSentence.OnConditionDelegate)(() => !this.order.AllowRaidingVillages), (ConversationSentence.OnConsequenceDelegate)(() => this.order.AllowRaidingVillages = true));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_no_sieging", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=MkcYMAy8}Do not besiege any towns or castles.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.HostileSettlementsScoreMultiplier > 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.HostileSettlementsScoreMultiplier = 0.0f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_no_sieging_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=1cnLX765}After reconsideration, you may grasp opportunities to conquer new lands.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order.HostileSettlementsScoreMultiplier == 0.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order.HostileSettlementsScoreMultiplier = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_avoid_initiative", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=Ym9U0Qzi}Avoid enemies unless they are much weaker than you.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._avoidInitiative == 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._avoidInitiative = 1.1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_avoid_initiative_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=UFKzmROA}Actually, do not avoid any enemies you may be able to defeat", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._avoidInitiative > 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._avoidInitiative = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_attack_initiative", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=RfBlwhmR}Only chase the most tempting enemy parties.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._attackInitiative == 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._attackInitiative = 0.9f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_attack_initiative_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=ESFnnRKG}I've reconsidered, attack our enemies as usual.", (ConversationSentence.OnConditionDelegate)(() => (double)this.order._attackInitiative < 1.0), (ConversationSentence.OnConsequenceDelegate)(() => this.order._attackInitiative = 1f));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_find_player", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=wlCqbKzm}Come find me whenever you can't think of anything else worth doing.", (ConversationSentence.OnConditionDelegate)(() => !this.findplayerifbored), (ConversationSentence.OnConsequenceDelegate)(() => this.findplayerifbored = true));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_find_player_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=HT5HEB5P}On secound thought, don't bother finding me on your own initiative.", (ConversationSentence.OnConditionDelegate)(() => this.findplayerifbored), (ConversationSentence.OnConsequenceDelegate)(() => this.findplayerifbored = false));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_confirm", "give_party_order_roam_additional", "affirm_party_order", "{=XyzVaqot}That's it. You know what to do.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.give_party_order_roam_confirm_consequence()));
                    campaignGameStarter.AddPlayerLine("give_party_order_roam_nevermind", "give_party_order_roam_additional", "lord_pretalk", "{=GHXpDIEK}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddDialogLine("affirm_party_order", "affirm_party_order", "close_window", "{=CX6JHsbB}We shall carry out your instructions!", (ConversationSentence.OnConditionDelegate)null, new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_leave_on_consequence));
                    campaignGameStarter.AddPlayerLine("equipment_party_clan", "hero_main_options", "equipment_party_clan_reply", "{=9n1Uij0W}Let me see your goods and equipment.", new ConversationSentence.OnConditionDelegate(this.conversation_is_clan_party_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_equipment_party_clan_on_consequence), 99);
                    campaignGameStarter.AddDialogLine("equipment_party_clan_reply", "equipment_party_clan_reply", "lord_pretalk", "{=kJC7LLJm}All right.", new ConversationSentence.OnConditionDelegate(this.conversation_equipment_clan_reply_on_condition), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddDialogLine("equipment_party_clan_reply", "equipment_party_clan_reply", "lord_pretalk", "{=52AcmMM0}All right, I will change my gear after our conversation.", new ConversationSentence.OnConditionDelegate(this.conversation_equipment_clan_reply_change_on_condition), (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.battle_equipment_backup = (Equipment)null;
                        this.civilian_equipment_backup = (Equipment)null;
                    }));
                    campaignGameStarter.AddPlayerLine("troops_and_prisoners_party_clan", "hero_main_options", "troops_and_prisoners_party_clan_reply", "{=dob2z0My}Let's exchange troops and prisoners.", new ConversationSentence.OnConditionDelegate(this.conversation_is_clan_party_on_condition), (ConversationSentence.OnConsequenceDelegate)null, 97);
                    campaignGameStarter.AddDialogLine("troops_and_prisoners_party_clan_reply", "troops_and_prisoners_party_clan_reply", "lord_pretalk", "{=ps3U3ots}All right.", new ConversationSentence.OnConditionDelegate(this.conversation_troops_and_prisoners_party_clan_on_condition), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("equipment_caravan_clan", "caravan_talk", "equipment_caravan_clan_reply", "{=TAyfMDef}Let me see your goods and equipment.", new ConversationSentence.OnConditionDelegate(this.conversation_is_clan_party_or_caravan_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_equipment_party_clan_on_consequence), 101);
                    campaignGameStarter.AddDialogLine("equipment_caravan_clan_reply", "equipment_caravan_clan_reply", "caravan_pretalk", "{=1baIw5Rl}All right.", new ConversationSentence.OnConditionDelegate(this.conversation_equipment_clan_reply_on_condition), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddDialogLine("equipment_caravan_clan_reply_change", "equipment_caravan_clan_reply", "caravan_pretalk", "{=dQwm7RgG}All right, I will change my equipment after our conversation.", new ConversationSentence.OnConditionDelegate(this.conversation_equipment_clan_reply_change_on_condition), (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        this.battle_equipment_backup = (Equipment)null;
                        this.civilian_equipment_backup = (Equipment)null;
                    }));
                    campaignGameStarter.AddPlayerLine("troops_and_prisoners_caravan_clan", "caravan_talk", "troops_and_prisoners_caravan_clan_reply", "{=iGMVomiK}Let's exchange troops and prisoners.", new ConversationSentence.OnConditionDelegate(this.conversation_is_clan_party_or_caravan_on_condition), (ConversationSentence.OnConsequenceDelegate)null, 102);
                    campaignGameStarter.AddDialogLine("troops_and_prisoners_caravan_clan_reply", "troops_and_prisoners_caravan_clan_reply", "caravan_pretalk", "{=4RvBh0oa}All right.", new ConversationSentence.OnConditionDelegate(this.conversation_troops_and_prisoners_party_clan_on_condition), (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_disband_join", "give_party_order_choose", "give_party_order_disband_join_ask_additional", "{=KQwXgzec}I want you and your entire party to merge into mine.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 103);
                    campaignGameStarter.AddDialogLine("give_party_order_disband_join_ask_additional", "give_party_order_disband_join_ask_additional", "give_party_order_disband_join_additional", "{=2zCnqIKP}Are you sure?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_disband_join_confirm", "give_party_order_disband_join_additional", "close_window", "{=e4bQb6Sj}Yes, I'm sure.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() =>
                    {
                        PlayerEncounter.LeaveEncounter = true;
                        this.MergeDisbandParty(Hero.OneToOneConversationHero.PartyBelongedTo, MobileParty.MainParty.Party);
                    }));
                    campaignGameStarter.AddPlayerLine("give_party_order_disband_join_nevermind", "give_party_order_disband_join_additional", "lord_pretalk", "{=hzBp0Sdu}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddPlayerLine("cancel_party_order", "hero_main_options", "cancel_party_order_reply", "{=L6eNTxsS}All your standing orders are hereby rescinded.", (ConversationSentence.OnConditionDelegate)(() => Hero.OneToOneConversationHero.getOrder() != null), (ConversationSentence.OnConsequenceDelegate)null, 97);
                    campaignGameStarter.AddDialogLine("cancel_party_order_reply_affirm", "cancel_party_order_reply", "lord_pretalk", "{=6O44WwxR}All right.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => Hero.OneToOneConversationHero.cancelOrder()));
                    campaignGameStarter.AddPlayerLine("give_party_order_army_join", "give_party_order_choose", "give_party_order_army_join_ask_additional", "{=2zoK2gG3}I want your party in my army.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 102);
                    campaignGameStarter.AddDialogLine("give_party_order_army_join_ask_additional", "give_party_order_army_join_ask_additional", "give_party_order_army_join_additional", "{=zAZKsHor}Are you sure?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("give_party_order_army_join_confirm", "give_party_order_army_join_additional", "close_window", "{=gj7auuZH}Yes, I'm sure.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.join_army()));
                    campaignGameStarter.AddPlayerLine("give_party_order_army_join_nevermind", "give_party_order_army_join_additional", "lord_pretalk", "{=pURcjber}Never mind.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 99);
                    campaignGameStarter.AddPlayerLine("cancel_all_party_order", "hero_main_options", "cancel_all_party_order_reply", "{=OXdtDXFm}Spread the word, everyone's orders are hereby rescinded.", (ConversationSentence.OnConditionDelegate)(() => Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Config.Value.EnableDebugCancelAllOrders), (ConversationSentence.OnConsequenceDelegate)null, 110);
                    campaignGameStarter.AddDialogLine("cancel_all_party_order_reply_affirm", "cancel_all_party_order_reply", "lord_pretalk", "{=6O44WwxR}All right.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => PartyAICommandsBehavior.Instance.order_map = new Dictionary<Hero, PartyOrder>()));
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order", "hero_main_options", "recruit_template_party_order_reply", "{=JRfAvHER}Let's review your party's composition plan.", (ConversationSentence.OnConditionDelegate)(() => this.conversation_is_clan_party_or_caravan_on_condition()), (ConversationSentence.OnConsequenceDelegate)null, 110);
                    campaignGameStarter.AddDialogLine("recruit_template_party_order_reply1", "recruit_template_party_order_reply", "recruit_template_party_order_menu1", "{=GCLIA72h}All right.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order_remove", "recruit_template_party_order_menu1", "lord_pretalk", "{=s7s9Ildy}Forget what I said earlier, recruit anyone you like.", (ConversationSentence.OnConditionDelegate)(() => Hero.OneToOneConversationHero.getTemplate() != (TroopRoster)null), (ConversationSentence.OnConsequenceDelegate)(() => PartyAICommandsBehavior.Instance.template_map.Remove(Hero.OneToOneConversationHero)), 109);
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order_menu1", "recruit_template_party_order_reply1", "{=fkdrQGr6}Let's decide what troops you should recruit.", (ConversationSentence.OnConditionDelegate)(() => this.conversation_is_clan_party_or_caravan_on_condition()), (ConversationSentence.OnConsequenceDelegate)null, 110);
                    campaignGameStarter.AddDialogLine("recruit_template_party_order_reply1", "recruit_template_party_order_reply1", "recruit_template_party_order_reply2", "{=6IpN0iuL}All right, what troop trees will we look at?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddDialogLine("recruit_template_party_order_reply2", "recruit_template_party_order_reply2", "recruit_template_party_order2", "{=6i4eGhT1}Are there any troops I should exclude or limit to a certain amount?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.template_select_troop_trees()));
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order2", "recruit_template_party_order_reply2", "{=bwKblPvf}Wait, let's go back.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null);
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order2", "recruit_template_party_order_reply_affirm2", "{=7xD1KsX5}Let's have a closer look.", (ConversationSentence.OnConditionDelegate)(() => PartyOrder.PartyOrderBuilder.template_party.MemberRoster.Count > 0 || Hero.OneToOneConversationHero.PartyBelongedTo.Party.NumberOfAllMembers > 1), (ConversationSentence.OnConsequenceDelegate)(() => this.template_set_troop_limits()), 110);
                    campaignGameStarter.AddPlayerLine("recruit_template_party_order_empty", "recruit_template_party_order2", "lord_pretalk", "{=8qD1KsX5}Just don't recruit anyone whatsoever.", (ConversationSentence.OnConditionDelegate)(() => PartyOrder.PartyOrderBuilder.template_party.MemberRoster.Count == 0 && Hero.OneToOneConversationHero.PartyBelongedTo.Party.NumberOfAllMembers == 1), (ConversationSentence.OnConsequenceDelegate)(() => this.template_apply_and_clean_up()), 110);
                    campaignGameStarter.AddDialogLine("recruit_template_party_order_reply_affirm2", "recruit_template_party_order_reply_affirm2", "lord_pretalk", "{=uacVKFj6}All right, I won't recruit any other troops than those and will adhere to any limits you have set.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.template_apply_and_clean_up()));
                    if (PartyAICommandsBehavior.Instance?.template_map == null)
                        PartyAICommandsBehavior.Instance.template_map = new Dictionary<Hero, TroopRoster>();
                    foreach (KeyValuePair<Hero, TroopRoster> template in PartyAICommandsBehavior.Instance.template_map)
                    {
                        KeyValuePair<Hero, TroopRoster> pair = template;
                        Hero hero = pair.Key;
                        if (hero != null && !(pair.Value == (TroopRoster)null))
                            campaignGameStarter.AddPlayerLine("template_" + hero.Name.ToString(), "recruit_template_party_order_menu1", "recruit_template_party_order_reply_affirm3", "{=xa1kmmj5}Use the same plan as " + hero.Name.ToString() + ".", (ConversationSentence.OnConditionDelegate)(() => PartyAICommandsBehavior.Instance.template_map.ContainsKey(hero) && hero != Hero.OneToOneConversationHero), (ConversationSentence.OnConsequenceDelegate)(() =>
                            {
                                PartyOrder.PartyOrderBuilder.template_party = new MobileParty();
                                PartyOrder.PartyOrderBuilder.template_party.StringId = " ";
                                PartyOrder.PartyOrderBuilder.template_party.MemberRoster.Add(pair.Value);
                            }), 141);
                    }
                    campaignGameStarter.AddDialogLine("recruit_template_party_order_reply_affirm3", "recruit_template_party_order_reply_affirm3", "lord_pretalk", "{=uac43Fj6}All right, I'll make a copy of that composition plan immediately.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)(() => this.template_apply_and_clean_up()));
                    this.conversation_manager = Traverse.Create((object)campaignGameStarter).Field("_conversationManager").GetValue<ConversationManager>();
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
            }

            private ConversationSentence getConversationSentenceByID(string id)
            {
                foreach (ConversationSentence conversationSentence in Traverse.Create((object)this.conversation_manager).Field("_sentences").GetValue<List<ConversationSentence>>())
                {
                    if (conversationSentence.Id == id)
                        return conversationSentence;
                }
                return (ConversationSentence)null;
            }

            private void template_apply_and_clean_up()
            {
                Hero hero = Hero.OneToOneConversationHero;
                PartyAICommandsBehavior.RegisterTemplate(hero, PartyOrder.PartyOrderBuilder.template_party.MemberRoster);
                if (PartyOrder.PartyOrderBuilder.template_party != null)
                    this.removeParty(ref PartyOrder.PartyOrderBuilder.template_party);
                if (PartyOrder.PartyOrderBuilder.all_recruits_party != null)
                    this.removeParty(ref PartyOrder.PartyOrderBuilder.all_recruits_party);
                if (PartyOrder.PartyOrderBuilder.template_limits_party != null)
                    this.removeParty(ref PartyOrder.PartyOrderBuilder.template_limits_party);
                IsMainTroopsLimitWarningEnabledPatch.ignore = false;
                if (this.getConversationSentenceByID("template_" + hero.Name.ToString()) != null)
                    return;
                this.cgs.AddPlayerLine("template_" + hero.Name.ToString(), "recruit_template_party_order_menu1", "recruit_template_party_order_reply_affirm3", "{=xa1kmmj5}Use the same plan as " + hero.Name.ToString() + ".", (ConversationSentence.OnConditionDelegate)(() => PartyAICommandsBehavior.Instance.template_map.ContainsKey(hero) && hero != Hero.OneToOneConversationHero), (ConversationSentence.OnConsequenceDelegate)(() =>
                {
                    PartyOrder.PartyOrderBuilder.template_party = new MobileParty();
                    PartyOrder.PartyOrderBuilder.template_party.StringId = "";
                    PartyOrder.PartyOrderBuilder.template_party.MemberRoster.Add(PartyAICommandsBehavior.Instance.template_map[hero]);
                }), 141);
            }

            private void removeParty(ref MobileParty party)
            {
                if (party == null)
                    return;
                party.MemberRoster.Reset();
                party.PrisonRoster.Reset();
                party.IsActive = false;
                party.IsVisible = false;
                Traverse.Create((object)Campaign.Current).Method("FinalizeParty", new Type[1]
                {
          typeof (PartyBase)
                }, (object[])null).GetValue((object)party.Party);
                GC.SuppressFinalize((object)party.Party);
                party = (MobileParty)null;
            }

            private void template_select_troop_trees()
            {
                try
                {
                    IsMainTroopsLimitWarningEnabledPatch.ignore = false;
                    if (PartyOrder.PartyOrderBuilder.template_party == null)
                    {
                        PartyOrder.PartyOrderBuilder.template_party = new MobileParty();
                        PartyOrder.PartyOrderBuilder.template_party.StringId = " ";
                        PartyOrder.PartyOrderBuilder.template_party.Name = new TextObject("{=6a8ajCJO}Selected Troop Trees");
                    }
                    if (PartyOrder.PartyOrderBuilder.all_recruits_party == null)
                    {
                        PartyOrder.PartyOrderBuilder.all_recruits_party = new MobileParty();
                        PartyOrder.PartyOrderBuilder.all_recruits_party.StringId = " ";
                        PartyOrder.PartyOrderBuilder.all_recruits_party.Name = new TextObject("{=CpuzeJFb}Available Troop Trees");
                        IEnumerable<CharacterObject> all = CharacterObject.FindAll((Predicate<CharacterObject>)(i => i.IsSoldier || i.IsRegular));
                        HashSet<CharacterObject> characterObjectSet = new HashSet<CharacterObject>(all);
                        foreach (CharacterObject characterObject in all)
                        {
                            if (characterObject.UpgradeTargets != null)
                            {
                                foreach (CharacterObject upgradeTarget in characterObject.UpgradeTargets)
                                {
                                    if (characterObject.IsSoldier && upgradeTarget.IsSoldier && (characterObject.IsRegular && upgradeTarget.IsRegular) || !characterObject.IsSoldier && !upgradeTarget.IsSoldier && (characterObject.IsRegular && upgradeTarget.IsRegular))
                                        characterObjectSet.Remove(upgradeTarget);
                                }
                            }
                            else
                                characterObjectSet.Remove(characterObject);
                        }
                        foreach (CharacterObject character in characterObjectSet)
                            PartyOrder.PartyOrderBuilder.all_recruits_party.MemberRoster.AddToCounts(character, 1);
                    }
                    Traverse traverse = Traverse.Create((object)PartyScreenManager.Instance);
                    PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
                    traverse.Field("_partyScreenLogic").SetValue((object)partyScreenLogic);
                    traverse.Field("_currentMode").SetValue((object)PartyScreenMode.TroopsManage);
                    partyScreenLogic.Initialize(PartyOrder.PartyOrderBuilder.template_party.Party, PartyOrder.PartyOrderBuilder.all_recruits_party, false, new TextObject("{=3AQlcqvU}Template"), 9999, new PartyPresentationDoneButtonDelegate(PartyOrder.PartyOrderBuilder.SelectTroopTreesDoneHandler), new TextObject("{=UoLVHbJh}Party Template Manager"));
                    partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
                    partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
                    partyScreenLogic.SetCancelActivateHandler(new PartyPresentationCancelButtonActivateDelegate(PartyOrder.PartyOrderBuilder.CancelHandler));
                    partyScreenLogic.SetDoneConditionHandler(new PartyPresentationDoneButtonConditionDelegate(PartyOrder.PartyOrderBuilder.PartyPresentationDoneButtonConditionDelegate));
                    PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
                    state.InitializeLogic(partyScreenLogic);
                    Game.Current.GameStateManager.PushState((GameState)state);
                    InformationManager.DisplayMessage(new InformationMessage("Transfer the recruits of the troop tree(s) you want to manage in the next step.", Colors.Green));
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
            }

            private static void AddUpgrades(CharacterObject troop)
            {
                if (troop.UpgradeTargets == null)
                    return;
                foreach (CharacterObject upgradeTarget in troop.UpgradeTargets)
                {
                    if (upgradeTarget != null && !PartyOrder.PartyOrderBuilder.template_party.MemberRoster.Contains(upgradeTarget))
                    {
                        PartyOrder.PartyOrderBuilder.template_party.MemberRoster.AddToCounts(upgradeTarget, 1);
                        PartyOrder.PartyOrderBuilder.AddUpgrades(upgradeTarget);
                    }
                }
            }

            private void template_set_troop_limits()
            {
                try
                {
                    foreach (TroopRosterElement troopRosterElement in PartyOrder.PartyOrderBuilder.template_party.MemberRoster)
                        PartyOrder.PartyOrderBuilder.AddUpgrades(troopRosterElement.Character);
                    if (Hero.OneToOneConversationHero.getTemplate() != (TroopRoster)null)
                    {
                        foreach (TroopRosterElement troopRosterElement in Hero.OneToOneConversationHero.getTemplate())
                        {
                            if (PartyOrder.PartyOrderBuilder.template_party.MemberRoster.GetTroopCount(troopRosterElement.Character) == 0)
                                PartyOrder.PartyOrderBuilder.template_party.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number);
                        }
                    }
                    foreach (TroopRosterElement troopRosterElement in Hero.OneToOneConversationHero.PartyBelongedTo.MemberRoster)
                    {
                        if (PartyOrder.PartyOrderBuilder.template_party.MemberRoster.GetTroopCount(troopRosterElement.Character) == 0 && !troopRosterElement.Character.IsHero)
                            PartyOrder.PartyOrderBuilder.template_party.MemberRoster.AddToCounts(troopRosterElement.Character, 1);
                    }
                    PartyOrder.PartyOrderBuilder.template_party.Name = new TextObject("{=9lssoqlP}Allowed Troops");
                    if (PartyOrder.PartyOrderBuilder.template_limits_party == null)
                    {
                        PartyOrder.PartyOrderBuilder.template_limits_party = new MobileParty();
                        PartyOrder.PartyOrderBuilder.template_limits_party.Name = new TextObject("{=5R2m2nND}Add these to set Limits");
                        PartyOrder.PartyOrderBuilder.template_limits_party.StringId = " ";
                        foreach (TroopRosterElement troopRosterElement in PartyOrder.PartyOrderBuilder.template_party.MemberRoster)
                            PartyOrder.PartyOrderBuilder.template_limits_party.AddElementToMemberRoster(troopRosterElement.Character, 1000);
                    }
                    Traverse traverse = Traverse.Create((object)PartyScreenManager.Instance);
                    PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
                    traverse.Field("_partyScreenLogic").SetValue((object)partyScreenLogic);
                    traverse.Field("_currentMode").SetValue((object)PartyScreenMode.TroopsManage);
                    partyScreenLogic.Initialize(PartyOrder.PartyOrderBuilder.template_party.Party, PartyOrder.PartyOrderBuilder.template_limits_party, false, new TextObject("{=3AQlcqvU}Template"), 9999, new PartyPresentationDoneButtonDelegate(PartyOrder.PartyOrderBuilder.SelectTroopTreesDoneHandler), new TextObject("{=UoLVHbJh}Party Template Manager"));
                    partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
                    partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
                    partyScreenLogic.SetDoneConditionHandler(new PartyPresentationDoneButtonConditionDelegate(PartyOrder.PartyOrderBuilder.PartyPresentationDoneButtonConditionDelegate));
                    partyScreenLogic.SetCancelActivateHandler(new PartyPresentationCancelButtonActivateDelegate(PartyOrder.PartyOrderBuilder.CancelHandler));
                    PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
                    state.InitializeLogic(partyScreenLogic);
                    Game.Current.GameStateManager.PushState((GameState)state);
                    InformationManager.DisplayMessage(new InformationMessage("Troops with 1 member only will be recruited without limitation.\nAdd more from the right to set limits.", Colors.Green));
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
            }

            public static Tuple<bool, string> PartyPresentationDoneButtonConditionDelegate(
              TroopRoster leftMemberRoster,
              TroopRoster leftPrisonRoster,
              TroopRoster rightMemberRoster,
              TroopRoster rightPrisonRoster,
              int leftLimitNum,
              int rightLimitNum)
            {
                return new Tuple<bool, string>(true, "What?");
            }

            private static bool CancelHandler() => false;

            private static bool SelectTroopTreesDoneHandler(
              TroopRoster leftMemberRoster,
              TroopRoster leftPrisonRoster,
              TroopRoster rightMemberRoster,
              TroopRoster rightPrisonRoster,
              FlattenedTroopRoster takenPrisonerRoster,
              FlattenedTroopRoster releasedPrisonerRoster,
              bool isForced,
              List<MobileParty> leftParties = null,
              List<MobileParty> rigthParties = null)
            {
                return true;
            }

            private bool conversation_is_clan_member_not_in_party_on_condition() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo;

            private bool conversation_is_clan_party_on_condition() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.PartyBelongedTo != null && (Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo) && !Hero.OneToOneConversationHero.PartyBelongedTo.IsCaravan;

            private bool conversation_is_clan_party_or_caravan_on_condition() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo;

            private bool conversation_equipment_clan_reply_on_condition() => Hero.OneToOneConversationHero.CharacterObject.Equipment.IsCivilian ? this.civilian_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.CivilianEquipment) : this.battle_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.BattleEquipment);

            private bool conversation_equipment_clan_reply_change_on_condition() => Hero.OneToOneConversationHero.CharacterObject.Equipment.IsCivilian ? !this.civilian_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.CivilianEquipment) : !this.battle_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.BattleEquipment);

            private bool conversation_trade_party_clan_on_condition()
            {
                try
                {
                    OnTradeProfitMadePatch.enableProfitXP = false;
                    InventoryManager.OpenScreenAsInventoryOf(Hero.MainHero.PartyBelongedTo.Party, Hero.OneToOneConversationHero.PartyBelongedTo.Party);
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
                return true;
            }

            private void conversation_equipment_party_clan_on_consequence()
            {
                try
                {
                    PartyBase party1 = Hero.MainHero.PartyBelongedTo.Party;
                    PartyBase party2 = Hero.OneToOneConversationHero.PartyBelongedTo.Party;
                    this.civilian_equipment_backup = Hero.OneToOneConversationHero.CivilianEquipment.Clone();
                    this.battle_equipment_backup = Hero.OneToOneConversationHero.BattleEquipment.Clone();
                    IMarketData marketData = Traverse.Create<InventoryManager>().Method("GetCurrentMarketData").GetValue<IMarketData>();
                    InventoryLogic inventoryLogic;
                    if (party2 != null)
                    {
                        inventoryLogic = new InventoryLogic(Campaign.Current, party2);
                        inventoryLogic.Initialize(party2.ItemRoster, party1.ItemRoster, party1.MemberRoster, false, true, Hero.OneToOneConversationHero.CharacterObject, InventoryManager.InventoryCategoryType.None, marketData, true, party2.Name);
                    }
                    else
                    {
                        inventoryLogic = new InventoryLogic(Campaign.Current, (PartyBase)null);
                        inventoryLogic.Initialize((ItemRoster)null, party1.ItemRoster, party1.MemberRoster, false, true, Hero.OneToOneConversationHero.CharacterObject, InventoryManager.InventoryCategoryType.None, marketData, false, Hero.OneToOneConversationHero.Name);
                    }
                    inventoryLogic.AfterReset += new InventoryLogic.AfterResetDelegate(this.ResetHeroEquipment);
                    InventoryState state = Game.Current.GameStateManager.CreateState<InventoryState>();
                    state.InitializeLogic(inventoryLogic);
                    Game.Current.GameStateManager.PushState((GameState)state);
                    Traverse.Create((object)Campaign.Current.InventoryManager).Field<InventoryLogic>("_inventoryLogic").Value = inventoryLogic;
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
            }

            private void ResetHeroEquipment(InventoryLogic inventoryLogic)
            {
                if (this.battle_equipment_backup != null)
                    Hero.OneToOneConversationHero.BattleEquipment.FillFrom(this.battle_equipment_backup);
                if (this.civilian_equipment_backup != null)
                    Hero.OneToOneConversationHero.CivilianEquipment.FillFrom(this.civilian_equipment_backup);
                this.civilian_equipment_backup = (Equipment)null;
                this.battle_equipment_backup = (Equipment)null;
            }

            private bool conversation_troops_and_prisoners_party_clan_on_condition()
            {
                try
                {
                    MBTextManager.SetTextVariable("PARTY_LIST_TAG", Hero.OneToOneConversationHero.PartyBelongedTo.Party.Name, false);
                    PartyScreenManager.OpenScreenAsLoot(Hero.OneToOneConversationHero.PartyBelongedTo.Party);
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.FlattenException());
                }
                return true;
            }

            private bool get_is_line_on_page(bool othercondition)
            {
                if (othercondition)
                {
                    if (this.line_index < this.lines_current_page * this.lines_per_page && this.line_index >= (this.lines_current_page - 1) * this.lines_per_page)
                    {
                        ++this.line_index;
                        return true;
                    }
                    ++this.line_index;
                }
                return false;
            }

            private bool give_party_order_patrol_additional_condition()
            {
                for (int index = 1; index <= 3; ++index)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_" + index.ToString(), "", false);
                if ((double)this.order.FriendlyVillagesScoreMultiplier == 1.0 && (double)this.order.PartyMaintenanceScoreMultiplier == 1.0 && (double)this.order.OwnClanVillagesScoreMultiplier == 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_1", "{=DpjCas89}Any additional instructions to follow during the patrol?", false);
                else if ((double)this.order.FriendlyVillagesScoreMultiplier < 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_1", "{=gZ3JPgdJ}The villages of our allied clans will have to do without us.\n", false);
                if ((double)this.order.OwnClanVillagesScoreMultiplier > 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_2", "{=xVmFj0Vt}We'll come to the aid of more distant villages as well.\n", false);
                if ((double)this.order.PartyMaintenanceScoreMultiplier > 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_3", "{=FZsSqjwF}We shall frequently visit nearby settlements for troops and to trade loot, supplies and prisoners.", false);
                return true;
            }

            private bool give_party_order_escort_additional_condition()
            {
                string text = "";
                string str1 = "{=2EyKGT16}Any additional instructions to follow while escorting you?";
                string str2 = "{=hg0GrxBV}We won't flee from enemies stronger than us.";
                string str3 = "{=BEdedw1C}We won't chase any enemies unless you give the signal.";
                string str4 = "{=DL3kEH8n}We shall stay at your side until commanded otherwise.";
                string str5 = "\nWe'll only take enemy leaders as prisoner.";
                if ((double)this.order.AttackInitiative == 1.0 && (double)this.order.AvoidInitiative == 1.0)
                    text = str1;
                if ((double)this.order.AvoidInitiative == 0.0)
                    text = str2;
                if ((double)this.order.AttackInitiative == 0.0)
                    text = str3;
                if ((double)this.order.AttackInitiative == 0.0 && (double)this.order.AvoidInitiative == 0.0)
                    text = str4;
                if (this.order.StopTakingPrisoners)
                    text += str5;
                MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ESCORT", text, false);
                return true;
            }

            private bool give_party_order_roam_additional_condition()
            {
                for (int index = 1; index <= 8; ++index)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_" + index.ToString(), "", false);
                if ((double)this.order.FriendlyVillagesScoreMultiplier == 1.0 && !this.findplayerifbored && (this.order.LeaveTroopsToGarrisonOtherClans && this.order.AllowJoiningArmies) && (this.order.AllowRaidingVillages && (double)this.order.HostileSettlementsScoreMultiplier > 0.0 && ((double)this.order.AttackInitiative == 1.0 && (double)this.order.AvoidInitiative == 1.0)))
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_1", "{=EBO8npRn}Any additional instructions to follow while roaming the lands?", false);
                else if ((double)this.order.FriendlyVillagesScoreMultiplier < 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_1", "{=n8KqPSs3}I won't help defending settlements of allied clans.\n", false);
                if (!this.order.LeaveTroopsToGarrisonOtherClans)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_2", "{=hg8h2ZRQ}I'll never give my troops to garrisons of allied clans.\n", false);
                if (!this.order.AllowJoiningArmies)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_3", "{=GKLbkTYg}I won't join armies unless you call for me.\n", false);
                if (!this.order.AllowRaidingVillages)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_4", "{=Et1Fdug7}I won't raid any villages.\n", false);
                if ((double)this.order.HostileSettlementsScoreMultiplier == 0.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_5", "{=xCAkZSk8}I'll ignore opportunities to conquer territory.\n", false);
                if ((double)this.order.AvoidInitiative > 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_6", "{=s3xzYOlT}We'll run from anyone who may cause us significant losses.\n", false);
                if ((double)this.order.AttackInitiative < 1.0)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_7", "{=2wfMJTSO}We'll chase only the most tempting enemy parties.\n", false);
                if (this.findplayerifbored)
                    MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_8", "{=66JVQdWZ}We'll come follow you if I can't think of much else to do.", false);
                return true;
            }

            private void give_party_order_roam_confirm_consequence()
            {
                this.order.OwnerHero.cancelOrder();
                this.order.OwnerHero.PartyBelongedTo.Army = (Army)null;
                if (this.findplayerifbored)
                {
                    this.order.Behavior = AiBehavior.EscortParty;
                    this.order.TargetParty = Hero.MainHero.PartyBelongedTo;
                    this.order.ScoreMinimum = 0.25f;
                    this.order.ScoreMultiplier = 1f;
                    this.order.OwnerHero.PartyBelongedTo.SetMoveEscortParty(this.order.TargetParty);
                }
                else
                {
                    this.order.Behavior = AiBehavior.None;
                    this.order.ScoreMinimum = 0.0f;
                    this.order.ScoreMultiplier = 0.0f;
                    this.order.OwnerHero.PartyBelongedTo.SetMoveModeHold();
                }
                PartyAICommandsBehavior.RegisterOrder(this.order.OwnerHero, this.order);
            }

            private void give_party_order_patrol_confirm_consequence()
            {
                this.order.OwnerHero.cancelOrder();
                this.order.OwnerHero.PartyBelongedTo.Army = (Army)null;
                this.order.Behavior = AiBehavior.PatrolAroundPoint;
                this.order.ScoreMinimum = 0.5f;
                this.order.ScoreMultiplier = 1.3f;
                this.order.HostileSettlementsScoreMultiplier = 0.1f;
                this.order.LeaveTroopsToGarrisonOtherClans = false;
                this.order.AllowJoiningArmies = false;
                this.order.AllowRaidingVillages = false;
                PartyAICommandsBehavior.RegisterOrder(this.order.OwnerHero, this.order);
                this.order.OwnerHero.PartyBelongedTo.SetMovePatrolAroundSettlement(this.order.TargetSettlement);
            }

            private void give_party_order_escort_confirm_consequence()
            {
                this.order.OwnerHero.cancelOrder();
                this.order.OwnerHero.PartyBelongedTo.Army = (Army)null;
                this.order.Behavior = AiBehavior.EscortParty;
                this.order.TargetParty = Hero.MainHero.PartyBelongedTo;
                this.order.ScoreMinimum = 15f;
                this.order.ScoreMultiplier = 1f;
                this.order.FriendlyVillagesScoreMultiplier = 0.5f;
                this.order.OwnClanVillagesScoreMultiplier = 0.5f;
                this.order.PartyMaintenanceScoreMultiplier = 0.5f;
                this.order.HostileSettlementsScoreMultiplier = 0.1f;
                this.order.LeaveTroopsToGarrisonOtherClans = false;
                this.order.AllowJoiningArmies = false;
                this.order.AllowRaidingVillages = false;
                PartyAICommandsBehavior.RegisterOrder(this.order.OwnerHero, this.order);
                this.order.OwnerHero.PartyBelongedTo.SetMoveEscortParty(this.order.TargetParty);
                if (Hero.OneToOneConversationHero.PartyBelongedTo.GetNumDaysForFoodToLast() >= 3)
                    return;
                InformationManager.DisplayMessage(new InformationMessage(Hero.OneToOneConversationHero.PartyBelongedTo.Name?.ToString() + " is short on food.", Colors.Red));
            }

            private void conversation_lord_leave_on_consequence()
            {
                if (PlayerEncounter.Current == null)
                    return;
                PlayerEncounter.LeaveEncounter = true;
            }

            private void MergeDisbandParty(MobileParty disbandParty, PartyBase mergeToParty)
            {
                disbandParty.LeaderHero.cancelOrder();
                mergeToParty.ItemRoster.Add(disbandParty.ItemRoster.AsEnumerable<ItemRosterElement>());
                FlattenedTroopRoster roster = new FlattenedTroopRoster();
                foreach (TroopRosterElement troopRosterElement in disbandParty.PrisonRoster)
                {
                    if (troopRosterElement.Character.IsHero)
                        GivePrisonerAction.Apply(troopRosterElement.Character, disbandParty.Party, mergeToParty);
                    else
                        roster.Add(troopRosterElement.Character, troopRosterElement.Number, troopRosterElement.WoundedNumber);
                }
                foreach (TroopRosterElement troopRosterElement in disbandParty.MemberRoster.ToList<TroopRosterElement>())
                {
                    disbandParty.MemberRoster.RemoveTroop(troopRosterElement.Character);
                    if (troopRosterElement.Character.IsHero)
                        AddHeroToPartyAction.Apply(troopRosterElement.Character.HeroObject, mergeToParty.MobileParty);
                    else
                        mergeToParty.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, woundedCount: troopRosterElement.WoundedNumber, xpChange: troopRosterElement.Xp);
                }
                mergeToParty.AddPrisoners(roster);
                disbandParty.RemoveParty();
            }

            private void join_army()
            {
                PlayerEncounter.LeaveEncounter = true;
                if (MobileParty.MainParty.Army == null)
                {
                    if (Clan.PlayerClan.IsUnderMercenaryService || Clan.PlayerClan.Kingdom == null)
                        this.CreateArmy(Hero.MainHero, (IMapPoint)Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
                    else
                        Clan.PlayerClan.Kingdom.CreateArmy(Hero.MainHero, (IMapPoint)Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
                }
                Hero.OneToOneConversationHero.cancelOrder();
                Hero.OneToOneConversationHero.PartyBelongedTo.Army = MobileParty.MainParty.Army;
                SetPartyAiAction.GetActionForEscortingParty(Hero.OneToOneConversationHero.PartyBelongedTo, MobileParty.MainParty);
                Hero.OneToOneConversationHero.PartyBelongedTo.IsJoiningArmy = true;
            }

            private void CreateArmy(Hero armyLeader, IMapPoint target, Army.ArmyTypes selectedArmyType)
            {
                if (!armyLeader.IsActive)
                    return;
                if (armyLeader?.PartyBelongedTo.Leader != null)
                {
                    Army army = new Army((Kingdom)null, armyLeader.PartyBelongedTo, selectedArmyType, target)
                    {
                        AIBehavior = Army.AIBehaviorFlags.Gathering
                    };
                    army.Gather();
                    Traverse traverse = Traverse.Create((object)CampaignEventDispatcher.Instance).Method("OnArmyCreated", new Type[1]
                    {
            typeof (Army)
                    }, (object[])null);
                    if (traverse.MethodExists())
                    {
                        traverse.GetValue((object)army);
                    }
                    else
                    {
                        int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Cannot find dispatch method OnArmyCreated, needs update.");
                    }
                }
                if (armyLeader != Hero.MainHero || !(Game.Current.GameStateManager.GameStates.Single<GameState>((Func<GameState, bool>)(S => S is MapState)) is MapState mapState))
                    return;
                mapState.OnArmyCreated(MobileParty.MainParty);
            }
        }
    }
}