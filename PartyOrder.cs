using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
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
		public class PartyOrderBuilder
		{
			public static readonly PartyOrderBuilder Instance = new PartyOrderBuilder();

			private int line_index;

			private readonly int lines_per_page = 8;

			private int lines_current_page = 1;

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
					cgs = campaignGameStarter;
					campaignGameStarter.AddPlayerLine("give_party_order", "hero_main_options", "give_party_order_reply", "{=mZEVvOi4}I have a new assignment for you.", conversation_is_clan_party_on_condition, delegate
					{
						lines_current_page = 1;
						order = new PartyOrder(Hero.OneToOneConversationHero);
					}, 96);
					campaignGameStarter.AddDialogLine("give_party_order_reply", "give_party_order_reply", "give_party_order_choose", "{=GVM3O4PW}What is it?", null, null);
					campaignGameStarter.AddPlayerLine("give_party_order_patrol", "give_party_order_choose", "give_party_order_reply2", "{=sFk6o6pM}I'm sending you on a patrol.", null, null, 99);
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind", "give_party_order_choose", "lord_pretalk", "{=IN6LdIzh}Never mind.", null, null, 99);
					campaignGameStarter.AddDialogLine("give_party_order_reply2", "give_party_order_reply2", "give_party_order_patrol_choose", "{=3LDRNP3W}Where should I patrol?", null, delegate
					{
						line_index = 0;
					});
					foreach (Settlement settlement in (IEnumerable<Settlement>)Campaign.Current.Settlements)
					{
						int priority = (settlement.OwnerClan == Hero.MainHero.Clan) ? 101 : 100;
						campaignGameStarter.AddPlayerLine("give_party_order_patrol_" + settlement.Name.ToString(), "give_party_order_patrol_choose", "give_party_order_patrol_ask_additional", "{=xa1kmmj5}" + settlement.Name.ToString(), () => get_is_line_on_page(settlement.MapFaction == Hero.MainHero.MapFaction || Campaign.Current.Models.MapDistanceModel.GetDistance(Hero.MainHero.PartyBelongedTo, settlement) < Campaign.AverageDistanceBetweenTwoTowns), delegate
						{
							order.TargetSettlement = settlement;
						}, priority);
					}
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_more", "give_party_order_patrol_choose", "give_party_order_reply2", "{=XRGi3bSd}More...", () => lines_per_page < line_index, delegate
					{
						lines_current_page = ((lines_per_page * lines_current_page >= line_index) ? 1 : (lines_current_page + 1));
					});
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind1", "give_party_order_patrol_choose", "lord_pretalk", "{=gX0L27IB}Never mind.", null, null, 99);
					campaignGameStarter.AddDialogLine("give_party_order_patrol_ask_additional", "give_party_order_patrol_ask_additional", "give_party_order_patrol_additional", "{ADDITIONAL_ORDERS_PATROL_1}{ADDITIONAL_ORDERS_PATROL_2}{ADDITIONAL_ORDERS_PATROL_3}", () => give_party_order_patrol_additional_condition(), null);
					campaignGameStarter.AddPlayerLine("give_party_order_order.FriendlyVillagesScoreMultiplier", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=yX4VtrOP}Focus on protecting our clan's villages.", () => order.FriendlyVillagesScoreMultiplier > 0f, delegate
					{
						order.FriendlyVillagesScoreMultiplier = 0f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_order.FriendlyVillagesScoreMultiplier_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=KkjBE78p}On second thought, also come to the aid of villages of allied clans.", () => order.FriendlyVillagesScoreMultiplier == 0f, delegate
					{
						order.FriendlyVillagesScoreMultiplier = 1f * order.OwnClanVillagesScoreMultiplier;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_distant_villages", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=pmqWOoV0}Also help the more distant villages if they raise the alarm.", () => order.OwnClanVillagesScoreMultiplier == 1f, delegate
					{
						order.OwnClanVillagesScoreMultiplier = 1.3f;
						order.FriendlyVillagesScoreMultiplier *= 1.3f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_distant_villages_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=uHzw3ULH}I changed my mind, do not travel too far to help other villages.", () => order.OwnClanVillagesScoreMultiplier > 1f, delegate
					{
						order.OwnClanVillagesScoreMultiplier = 1f;
						if (order.FriendlyVillagesScoreMultiplier > 0f)
						{
							order.FriendlyVillagesScoreMultiplier = 1f;
						}
					});
					campaignGameStarter.AddPlayerLine("give_party_order_order.PartyMaintenanceScoreMultiplierenance", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=F2vg9SGK}Visit often the surrounding settlements for your party's needs.", () => order.PartyMaintenanceScoreMultiplier == 1f, delegate
					{
						order.PartyMaintenanceScoreMultiplier = 1.7f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_order.PartyMaintenanceScoreMultiplierenance_revert", "give_party_order_patrol_additional", "give_party_order_patrol_ask_additional", "{=16NEiony}Actually, don't visit the surrounding settlements more than usual.", () => order.PartyMaintenanceScoreMultiplier > 1f, delegate
					{
						order.PartyMaintenanceScoreMultiplier = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_confirm", "give_party_order_patrol_additional", "affirm_party_order", "{=AZYIGzy8}That's it. You have your orders.", null, delegate
					{
						give_party_order_patrol_confirm_consequence();
					});
					campaignGameStarter.AddPlayerLine("give_party_order_patrol_nevermind2", "give_party_order_patrol_additional", "lord_pretalk", "{=O1M2H0C2}Never mind.", null, null, 99);
					campaignGameStarter.AddPlayerLine("give_party_order_escort", "give_party_order_choose", "give_party_order_escort_ask_additional", "{=S7IXOvya}Your party is to follow mine.", null, delegate
					{
						order._attackInitiative = 1f;
						order._avoidInitiative = 1f;
					});
					campaignGameStarter.AddDialogLine("give_party_order_escort_ask_additional", "give_party_order_escort_ask_additional", "give_party_order_escort_additional", "{ADDITIONAL_ORDERS_ESCORT}", () => give_party_order_escort_additional_condition(), null);
					campaignGameStarter.AddPlayerLine("give_party_order_escort_attack_initiative", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=9VkT0QtD}Do not chase enemies until I give the signal.", () => order._attackInitiative == 1f, delegate
					{
						order._attackInitiative = 0f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_attack_initiative_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=KbwF9ipY}On second thought, engage our enemies at will.", () => order._attackInitiative == 0f, delegate
					{
						order._attackInitiative = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_avoid_initiative", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=ShLWeZiY}Do not flee from dangerous enemies.", () => order._avoidInitiative == 1f, delegate
					{
						order._avoidInitiative = 0f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_avoid_initiative_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=jdgK44KM}Actually, avoid dangerous enemies as you would otherwise", () => order._avoidInitiative == 0f, delegate
					{
						order._avoidInitiative = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_stop_imprisoning", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=99JeSjud}Stop taking prisoners, except for the leading nobility.", () => !order.StopTakingPrisoners, delegate
					{
						order.StopTakingPrisoners = true;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_stop_imprisoning_revert", "give_party_order_escort_additional", "give_party_order_escort_ask_additional", "{=xPRt2fdZ}I've reconsidered, feel free to take prisoners at will.", () => order.StopTakingPrisoners, delegate
					{
						order.StopTakingPrisoners = false;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_confirm", "give_party_order_escort_additional", "affirm_party_order", "{=IQKXYHAq}That's it. You have your orders.", null, delegate
					{
						give_party_order_escort_confirm_consequence();
					});
					campaignGameStarter.AddPlayerLine("give_party_order_escort_nevermind", "give_party_order_escort_additional", "lord_pretalk", "{=6LrwJL3W}Never mind.", null, null, 99);
					campaignGameStarter.AddPlayerLine("give_party_order_roam", "give_party_order_choose", "give_party_order_roam_ask_additional", "{=5ur2y5hP}I want you to roam the lands.", null, delegate
					{
						order._attackInitiative = 1f;
						order._avoidInitiative = 1f;
						order.LeaveTroopsToGarrisonOtherClans = true;
						order.AllowJoiningArmies = true;
						order.AllowRaidingVillages = true;
						order.HostileSettlementsScoreMultiplier = 1f;
						findplayerifbored = false;
						order.FriendlyVillagesScoreMultiplier = 1f;
					}, 101);
					campaignGameStarter.AddDialogLine("give_party_order_roam_ask_additional", "give_party_order_roam_ask_additional", "give_party_order_roam_additional", "{ADDITIONAL_ORDERS_ROAM_1}{ADDITIONAL_ORDERS_ROAM_2}{ADDITIONAL_ORDERS_ROAM_3}{ADDITIONAL_ORDERS_ROAM_4}{ADDITIONAL_ORDERS_ROAM_5}{ADDITIONAL_ORDERS_ROAM_6}{ADDITIONAL_ORDERS_ROAM_7}{ADDITIONAL_ORDERS_ROAM_8}", () => give_party_order_roam_additional_condition(), null);
					campaignGameStarter.AddPlayerLine("give_party_order_roam_only_clan", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=hVxWVKX7}Don't bother protecting settlements of allied clans.", () => order.FriendlyVillagesScoreMultiplier > 0f, delegate
					{
						order.FriendlyVillagesScoreMultiplier = 0f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_only_clan_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=welScFSY}I've reconsidered, protect settlements of allied clans as well", () => order.FriendlyVillagesScoreMultiplier == 0f, delegate
					{
						order.FriendlyVillagesScoreMultiplier = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_leave_garrison", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=7E06qXBJ}Never leave our troops in garrisons of allied clans.", () => order.LeaveTroopsToGarrisonOtherClans, delegate
					{
						order.LeaveTroopsToGarrisonOtherClans = false;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_leave_garrison_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=wZOn0NEF}Actually, feel free to donate troops to the garrisons of allied clans.", () => !order.LeaveTroopsToGarrisonOtherClans, delegate
					{
						order.LeaveTroopsToGarrisonOtherClans = true;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_join_army", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=hn1giswY}Do not merge with armies unless I personally lead.", () => order.AllowJoiningArmies, delegate
					{
						order.AllowJoiningArmies = false;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_join_army_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=ksQCaRwa}I will allow you to merge with armies after all.", () => !order.AllowJoiningArmies, delegate
					{
						order.AllowJoiningArmies = true;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_no_raiding", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=1EgmZn7s}Refrain from raiding any villages.", () => order.AllowRaidingVillages, delegate
					{
						order.AllowRaidingVillages = false;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_no_raiding_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=cTY5EsLq}On second thought, I allow you to raid the villages of our enemies.", () => !order.AllowRaidingVillages, delegate
					{
						order.AllowRaidingVillages = true;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_no_sieging", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=MkcYMAy8}Do not besiege any towns or castles.", () => order.HostileSettlementsScoreMultiplier > 0f, delegate
					{
						order.HostileSettlementsScoreMultiplier = 0f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_no_sieging_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=1cnLX765}After reconsideration, you may grasp opportunities to conquer new lands.", () => order.HostileSettlementsScoreMultiplier == 0f, delegate
					{
						order.HostileSettlementsScoreMultiplier = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_avoid_initiative", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=Ym9U0Qzi}Avoid enemies unless they are much weaker than you.", () => order._avoidInitiative == 1f, delegate
					{
						order._avoidInitiative = 1.1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_avoid_initiative_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=UFKzmROA}Actually, do not avoid any enemies you may be able to defeat", () => order._avoidInitiative > 1f, delegate
					{
						order._avoidInitiative = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_attack_initiative", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=RfBlwhmR}Only chase the most tempting enemy parties.", () => order._attackInitiative == 1f, delegate
					{
						order._attackInitiative = 0.9f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_attack_initiative_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=ESFnnRKG}I've reconsidered, attack our enemies as usual.", () => order._attackInitiative < 1f, delegate
					{
						order._attackInitiative = 1f;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_find_player", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=wlCqbKzm}Come find me whenever you can't think of anything else worth doing.", () => !findplayerifbored, delegate
					{
						findplayerifbored = true;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_find_player_revert", "give_party_order_roam_additional", "give_party_order_roam_ask_additional", "{=HT5HEB5P}On secound thought, don't bother finding me on your own initiative.", () => findplayerifbored, delegate
					{
						findplayerifbored = false;
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_confirm", "give_party_order_roam_additional", "affirm_party_order", "{=XyzVaqot}That's it. You know what to do.", null, delegate
					{
						give_party_order_roam_confirm_consequence();
					});
					campaignGameStarter.AddPlayerLine("give_party_order_roam_nevermind", "give_party_order_roam_additional", "lord_pretalk", "{=GHXpDIEK}Never mind.", null, null, 99);
					campaignGameStarter.AddDialogLine("affirm_party_order", "affirm_party_order", "close_window", "{=CX6JHsbB}We shall carry out your instructions!", null, conversation_lord_leave_on_consequence);
					campaignGameStarter.AddPlayerLine("equipment_party_clan", "hero_main_options", "equipment_party_clan_reply", "{=9n1Uij0W}Let me see your goods and equipment.", conversation_is_clan_party_on_condition, conversation_equipment_party_clan_on_consequence, 99);
					campaignGameStarter.AddDialogLine("equipment_party_clan_reply", "equipment_party_clan_reply", "lord_pretalk", "{=kJC7LLJm}All right.", conversation_equipment_clan_reply_on_condition, null);
					campaignGameStarter.AddDialogLine("equipment_party_clan_reply", "equipment_party_clan_reply", "lord_pretalk", "{=52AcmMM0}All right, I will change my gear after our conversation.", conversation_equipment_clan_reply_change_on_condition, delegate
					{
						battle_equipment_backup = null;
						civilian_equipment_backup = null;
					});
					campaignGameStarter.AddPlayerLine("troops_and_prisoners_party_clan", "hero_main_options", "troops_and_prisoners_party_clan_reply", "{=dob2z0My}Let's exchange troops and prisoners.", conversation_is_clan_party_on_condition, null, 97);
					campaignGameStarter.AddDialogLine("troops_and_prisoners_party_clan_reply", "troops_and_prisoners_party_clan_reply", "lord_pretalk", "{=ps3U3ots}All right.", conversation_troops_and_prisoners_party_clan_on_condition, null);
					campaignGameStarter.AddPlayerLine("equipment_caravan_clan", "caravan_talk", "equipment_caravan_clan_reply", "{=TAyfMDef}Let me see your goods and equipment.", conversation_is_clan_party_or_caravan_on_condition, conversation_equipment_party_clan_on_consequence, 101);
					campaignGameStarter.AddDialogLine("equipment_caravan_clan_reply", "equipment_caravan_clan_reply", "caravan_pretalk", "{=1baIw5Rl}All right.", conversation_equipment_clan_reply_on_condition, null);
					campaignGameStarter.AddDialogLine("equipment_caravan_clan_reply_change", "equipment_caravan_clan_reply", "caravan_pretalk", "{=dQwm7RgG}All right, I will change my equipment after our conversation.", conversation_equipment_clan_reply_change_on_condition, delegate
					{
						battle_equipment_backup = null;
						civilian_equipment_backup = null;
					});
					campaignGameStarter.AddPlayerLine("troops_and_prisoners_caravan_clan", "caravan_talk", "troops_and_prisoners_caravan_clan_reply", "{=iGMVomiK}Let's exchange troops and prisoners.", conversation_is_clan_party_or_caravan_on_condition, null, 102);
					campaignGameStarter.AddDialogLine("troops_and_prisoners_caravan_clan_reply", "troops_and_prisoners_caravan_clan_reply", "caravan_pretalk", "{=4RvBh0oa}All right.", conversation_troops_and_prisoners_party_clan_on_condition, null);
					campaignGameStarter.AddPlayerLine("give_party_order_disband_join", "give_party_order_choose", "give_party_order_disband_join_ask_additional", "{=KQwXgzec}I want you and your entire party to merge into mine.", null, null, 103);
					campaignGameStarter.AddDialogLine("give_party_order_disband_join_ask_additional", "give_party_order_disband_join_ask_additional", "give_party_order_disband_join_additional", "{=2zCnqIKP}Are you sure?", null, null);
					campaignGameStarter.AddPlayerLine("give_party_order_disband_join_confirm", "give_party_order_disband_join_additional", "close_window", "{=e4bQb6Sj}Yes, I'm sure.", null, delegate
					{
						PlayerEncounter.LeaveEncounter = true;
						MergeDisbandParty(Hero.OneToOneConversationHero.PartyBelongedTo, MobileParty.MainParty.Party);
					});
					campaignGameStarter.AddPlayerLine("give_party_order_disband_join_nevermind", "give_party_order_disband_join_additional", "lord_pretalk", "{=hzBp0Sdu}Never mind.", null, null, 99);
					campaignGameStarter.AddPlayerLine("cancel_party_order", "hero_main_options", "cancel_party_order_reply", "{=L6eNTxsS}All your standing orders are hereby rescinded.", () => Hero.OneToOneConversationHero.getOrder() != null, null, 97);
					campaignGameStarter.AddDialogLine("cancel_party_order_reply_affirm", "cancel_party_order_reply", "lord_pretalk", "{=6O44WwxR}All right.", null, delegate
					{
						Hero.OneToOneConversationHero.cancelOrder();
					});
					campaignGameStarter.AddPlayerLine("give_party_order_army_join", "give_party_order_choose", "give_party_order_army_join_ask_additional", "{=2zoK2gG3}I want your party in my army.", null, null, 102);
					campaignGameStarter.AddDialogLine("give_party_order_army_join_ask_additional", "give_party_order_army_join_ask_additional", "give_party_order_army_join_additional", "{=zAZKsHor}Are you sure?", null, null);
					campaignGameStarter.AddPlayerLine("give_party_order_army_join_confirm", "give_party_order_army_join_additional", "close_window", "{=gj7auuZH}Yes, I'm sure.", null, delegate
					{
						join_army();
					});
					campaignGameStarter.AddPlayerLine("give_party_order_army_join_nevermind", "give_party_order_army_join_additional", "lord_pretalk", "{=pURcjber}Never mind.", null, null, 99);
					campaignGameStarter.AddPlayerLine("cancel_all_party_order", "hero_main_options", "cancel_all_party_order_reply", "{=OXdtDXFm}Spread the word, everyone's orders are hereby rescinded.", () => Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Config.Value.EnableDebugCancelAllOrders, null, 110);
					campaignGameStarter.AddDialogLine("cancel_all_party_order_reply_affirm", "cancel_all_party_order_reply", "lord_pretalk", "{=6O44WwxR}All right.", null, delegate
					{
						PartyAICommandsBehavior.Instance.order_map = new Dictionary<Hero, PartyOrder>();
					});
					campaignGameStarter.AddPlayerLine("recruit_template_party_order", "hero_main_options", "recruit_template_party_order_reply", "{=JRfAvHER}Let's review your party's composition plan.", () => conversation_is_clan_party_or_caravan_on_condition(), null, 110);
					campaignGameStarter.AddDialogLine("recruit_template_party_order_reply1", "recruit_template_party_order_reply", "recruit_template_party_order_menu1", "{=GCLIA72h}All right.", null, null);
					campaignGameStarter.AddPlayerLine("recruit_template_party_order_remove", "recruit_template_party_order_menu1", "lord_pretalk", "{=s7s9Ildy}Forget what I said earlier, recruit anyone you like.", () => Hero.OneToOneConversationHero.getTemplate() != null, delegate
					{
						PartyAICommandsBehavior.Instance.template_map.Remove(Hero.OneToOneConversationHero);
					}, 109);
					campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order_menu1", "recruit_template_party_order_reply1", "{=fkdrQGr6}Let's decide what troops you should recruit.", () => conversation_is_clan_party_or_caravan_on_condition(), null, 110);
					campaignGameStarter.AddDialogLine("recruit_template_party_order_reply1", "recruit_template_party_order_reply1", "recruit_template_party_order_reply2", "{=6IpN0iuL}All right, what troop trees will we look at?", null, null);
					campaignGameStarter.AddDialogLine("recruit_template_party_order_reply2", "recruit_template_party_order_reply2", "recruit_template_party_order2", "{=6i4eGhT1}Are there any troops I should exclude or limit to a certain amount?", null, delegate
					{
						template_select_troop_trees();
					});
					campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order2", "recruit_template_party_order_reply2", "{=bwKblPvf}Wait, let's go back.", null, null);
					campaignGameStarter.AddPlayerLine("recruit_template_party_order", "recruit_template_party_order2", "recruit_template_party_order_reply_affirm2", "{=7xD1KsX5}Let's have a closer look.", () => template_party.MemberRoster.Count > 0 || Hero.OneToOneConversationHero.PartyBelongedTo.Party.NumberOfAllMembers > 1, delegate
					{
						template_set_troop_limits();
					}, 110);
					campaignGameStarter.AddPlayerLine("recruit_template_party_order_empty", "recruit_template_party_order2", "lord_pretalk", "{=8qD1KsX5}Just don't recruit anyone whatsoever.", () => template_party.MemberRoster.Count == 0 && Hero.OneToOneConversationHero.PartyBelongedTo.Party.NumberOfAllMembers == 1, delegate
					{
						template_apply_and_clean_up();
					}, 110);
					campaignGameStarter.AddDialogLine("recruit_template_party_order_reply_affirm2", "recruit_template_party_order_reply_affirm2", "lord_pretalk", "{=uacVKFj6}All right, I won't recruit any other troops than those and will adhere to any limits you have set.", null, delegate
					{
						template_apply_and_clean_up();
					});
					if (PartyAICommandsBehavior.Instance?.template_map == null)
					{
						PartyAICommandsBehavior.Instance.template_map = new Dictionary<Hero, TroopRoster>();
					}
					foreach (KeyValuePair<Hero, TroopRoster> pair in PartyAICommandsBehavior.Instance.template_map)
					{
						Hero hero = pair.Key;
						if (hero != null && !(pair.Value == null))
						{
							campaignGameStarter.AddPlayerLine("template_" + hero.Name.ToString(), "recruit_template_party_order_menu1", "recruit_template_party_order_reply_affirm3", "{=xa1kmmj5}Use the same plan as " + hero.Name.ToString() + ".", () => PartyAICommandsBehavior.Instance.template_map.ContainsKey(hero) && hero != Hero.OneToOneConversationHero, delegate
							{
								template_party = new MobileParty();
								template_party.MemberRoster.Add(pair.Value);
							}, 141);
						}
					}
					campaignGameStarter.AddDialogLine("recruit_template_party_order_reply_affirm3", "recruit_template_party_order_reply_affirm3", "lord_pretalk", "{=uac43Fj6}All right, I'll make a copy of that composition plan immediately.", null, delegate
					{
						template_apply_and_clean_up();
					});
					conversation_manager = Traverse.Create(campaignGameStarter).Field("_conversationManager").GetValue<ConversationManager>();
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
			}

			private ConversationSentence getConversationSentenceByID(string id)
			{
				foreach (ConversationSentence sentence in Traverse.Create(conversation_manager).Field("_sentences").GetValue<List<ConversationSentence>>())
				{
					if (sentence.Id == id)
					{
						return sentence;
					}
				}
				return null;
			}

			private void template_apply_and_clean_up()
			{
				Hero hero = Hero.OneToOneConversationHero;
				PartyAICommandsBehavior.RegisterTemplate(hero, template_party.MemberRoster);
				if (template_party != null)
				{
					removeParty(ref template_party);
				}
				if (all_recruits_party != null)
				{
					removeParty(ref all_recruits_party);
				}
				if (template_limits_party != null)
				{
					removeParty(ref template_limits_party);
				}
				IsMainTroopsLimitWarningEnabledPatch.ignore = false;
				if (getConversationSentenceByID("template_" + hero.Name.ToString()) == null)
				{
					cgs.AddPlayerLine("template_" + hero.Name.ToString(), "recruit_template_party_order_menu1", "recruit_template_party_order_reply_affirm3", "{=xa1kmmj5}Use the same plan as " + hero.Name.ToString() + ".", () => PartyAICommandsBehavior.Instance.template_map.ContainsKey(hero) && hero != Hero.OneToOneConversationHero, delegate
					{
						template_party = new MobileParty();
						template_party.MemberRoster.Add(PartyAICommandsBehavior.Instance.template_map[hero]);
					}, 141);
				}
			}

			private void removeParty(ref MobileParty party)
			{
				if (party != null)
				{
					party.MemberRoster.Reset();
					party.PrisonRoster.Reset();
					party.IsActive = false;
					party.IsVisible = false;
					Traverse.Create(Campaign.Current).Method("FinalizeParty", new Type[1]
					{
						typeof(PartyBase)
					}).GetValue(party.Party);
					GC.SuppressFinalize(party.Party);
					party = null;
				}
			}

			private void template_select_troop_trees()
			{
				try
				{
					IsMainTroopsLimitWarningEnabledPatch.ignore = false;
					if (template_party == null)
					{
						template_party = new MobileParty();
						template_party.Name = new TextObject("{=6a8ajCJO}Selected Troop Trees");
					}
					if (all_recruits_party == null)
					{
						all_recruits_party = new MobileParty();
						all_recruits_party.Name = new TextObject("{=CpuzeJFb}Available Troop Trees");
						IEnumerable<CharacterObject> enumerable = CharacterObject.FindAll((CharacterObject i) => i.IsSoldier || i.IsRegular);
						HashSet<CharacterObject> recruits = new HashSet<CharacterObject>(enumerable);
						foreach (CharacterObject soldier2 in enumerable)
						{
							if (soldier2.UpgradeTargets != null)
							{
								CharacterObject[] upgradeTargets = soldier2.UpgradeTargets;
								foreach (CharacterObject upgrade in upgradeTargets)
								{
									if ((soldier2.IsSoldier && upgrade.IsSoldier && soldier2.IsRegular && upgrade.IsRegular) || (!soldier2.IsSoldier && !upgrade.IsSoldier && soldier2.IsRegular && upgrade.IsRegular))
									{
										recruits.Remove(upgrade);
									}
								}
							}
							else
							{
								recruits.Remove(soldier2);
							}
						}
						foreach (CharacterObject soldier in recruits)
						{
							all_recruits_party.MemberRoster.AddToCounts(soldier, 1);
						}
						all_recruits_party.IsVillager = true;
					}
					Traverse traverse = Traverse.Create(PartyScreenManager.Instance);
					PartyScreenLogic logic = new PartyScreenLogic();
					traverse.Field("_partyScreenLogic").SetValue(logic);
					traverse.Field("_currentMode").SetValue(PartyScreenMode.TroopsManage);
					logic.Initialize(template_party.Party, all_recruits_party, isDismissMode: false, new TextObject("{=3AQlcqvU}Template"), 9999, SelectTroopTreesDoneHandler, new TextObject("{=UoLVHbJh}Party Template Manager"));
					logic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
					logic.SetTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate);
					logic.SetCancelActivateHandler(CancelHandler);
					logic.SetDoneConditionHandler(PartyPresentationDoneButtonConditionDelegate);
					PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
					partyState.InitializeLogic(logic);
					Game.Current.GameStateManager.PushState(partyState);
					InformationManager.DisplayMessage(new InformationMessage("Transfer the recruits of the troop tree(s) you want to manage in the next step.", Colors.Green));
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
			}

			private static void AddUpgrades(CharacterObject troop)
			{
				if (troop.UpgradeTargets == null)
				{
					return;
				}
				CharacterObject[] upgradeTargets = troop.UpgradeTargets;
				foreach (CharacterObject upgrade in upgradeTargets)
				{
					if (upgrade != null && !template_party.MemberRoster.Contains(upgrade))
					{
						template_party.MemberRoster.AddToCounts(upgrade, 1);
						AddUpgrades(upgrade);
					}
				}
			}

			private void template_set_troop_limits()
			{
				try
				{
					foreach (TroopRosterElement item in template_party.MemberRoster)
					{
						AddUpgrades(item.Character);
					}
					if (Hero.OneToOneConversationHero.getTemplate() != null)
					{
						foreach (TroopRosterElement element3 in Hero.OneToOneConversationHero.getTemplate())
						{
							if (template_party.MemberRoster.GetTroopCount(element3.Character) == 0)
							{
								template_party.MemberRoster.AddToCounts(element3.Character, element3.Number);
							}
						}
					}
					foreach (TroopRosterElement element2 in Hero.OneToOneConversationHero.PartyBelongedTo.MemberRoster)
					{
						if (template_party.MemberRoster.GetTroopCount(element2.Character) == 0 && !element2.Character.IsHero)
						{
							template_party.MemberRoster.AddToCounts(element2.Character, 1);
						}
					}
					template_party.Name = new TextObject("{=9lssoqlP}Allowed Troops");
					if (template_limits_party == null)
					{
						template_limits_party = new MobileParty();
						template_limits_party.Name = new TextObject("{=5R2m2nND}Add these to set Limits");
						foreach (TroopRosterElement element in template_party.MemberRoster)
						{
							template_limits_party.AddElementToMemberRoster(element.Character, 1000);
						}
					}
					Traverse traverse = Traverse.Create(PartyScreenManager.Instance);
					PartyScreenLogic logic = new PartyScreenLogic();
					traverse.Field("_partyScreenLogic").SetValue(logic);
					traverse.Field("_currentMode").SetValue(PartyScreenMode.TroopsManage);
					logic.Initialize(template_party.Party, template_limits_party, isDismissMode: false, new TextObject("{=3AQlcqvU}Template"), 9999, SelectTroopTreesDoneHandler, new TextObject("{=UoLVHbJh}Party Template Manager"));
					logic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
					logic.SetTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate);
					logic.SetDoneConditionHandler(PartyPresentationDoneButtonConditionDelegate);
					logic.SetCancelActivateHandler(CancelHandler);
					PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
					partyState.InitializeLogic(logic);
					Game.Current.GameStateManager.PushState(partyState);
					InformationManager.DisplayMessage(new InformationMessage("Troops with 1 member only will be recruited without limitation.\nAdd more from the right to set limits.", Colors.Green));
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
			}

			public static Tuple<bool, string> PartyPresentationDoneButtonConditionDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
			{
				return new Tuple<bool, string>(item1: true, "What?");
			}

			private static bool CancelHandler()
			{
				return false;
			}

			private static bool SelectTroopTreesDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, List<MobileParty> leftParties = null, List<MobileParty> rigthParties = null)
			{
				return true;
			}

			private bool conversation_is_clan_member_not_in_party_on_condition()
			{
				if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan)
				{
					return Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo;
				}
				return false;
			}

			private bool conversation_is_clan_party_on_condition()
			{
				if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan && Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo)
				{
					return !Hero.OneToOneConversationHero.PartyBelongedTo.IsCaravan;
				}
				return false;
			}

			private bool conversation_is_clan_party_or_caravan_on_condition()
			{
				if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan)
				{
					return Hero.MainHero.PartyBelongedTo != Hero.OneToOneConversationHero.PartyBelongedTo;
				}
				return false;
			}

			private bool conversation_equipment_clan_reply_on_condition()
			{
				if (!Hero.OneToOneConversationHero.CharacterObject.Equipment.IsCivilian)
				{
					return battle_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.BattleEquipment);
				}
				return civilian_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.CivilianEquipment);
			}

			private bool conversation_equipment_clan_reply_change_on_condition()
			{
				if (!Hero.OneToOneConversationHero.CharacterObject.Equipment.IsCivilian)
				{
					return !battle_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.BattleEquipment);
				}
				return !civilian_equipment_backup.IsEquipmentEqualTo(Hero.OneToOneConversationHero.CivilianEquipment);
			}

			private bool conversation_trade_party_clan_on_condition()
			{
				try
				{
					OnTradeProfitMadePatch.enableProfitXP = false;
					InventoryManager.OpenScreenAsInventoryOf(Hero.MainHero.PartyBelongedTo.Party, Hero.OneToOneConversationHero.PartyBelongedTo.Party);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
				return true;
			}

			private void conversation_equipment_party_clan_on_consequence()
			{
				try
				{
					PartyBase rightparty = Hero.MainHero.PartyBelongedTo.Party;
					PartyBase leftparty = Hero.OneToOneConversationHero.PartyBelongedTo.Party;
					civilian_equipment_backup = Hero.OneToOneConversationHero.CivilianEquipment.Clone();
					battle_equipment_backup = Hero.OneToOneConversationHero.BattleEquipment.Clone();
					IMarketData marketdata = Traverse.Create<InventoryManager>().Method("GetCurrentMarketData").GetValue<IMarketData>();
					InventoryLogic inventoryLogic;
					if (leftparty != null)
					{
						inventoryLogic = new InventoryLogic(Campaign.Current, leftparty);
						inventoryLogic.Initialize(leftparty.ItemRoster, rightparty.ItemRoster, rightparty.MemberRoster, isTrading: false, isSpecialActionsPermitted: true, Hero.OneToOneConversationHero.CharacterObject, InventoryManager.InventoryCategoryType.None, marketdata, useBasePrices: true, leftparty.Name);
					}
					else
					{
						inventoryLogic = new InventoryLogic(Campaign.Current, null);
						inventoryLogic.Initialize(null, rightparty.ItemRoster, rightparty.MemberRoster, isTrading: false, isSpecialActionsPermitted: true, Hero.OneToOneConversationHero.CharacterObject, InventoryManager.InventoryCategoryType.None, marketdata, useBasePrices: false, Hero.OneToOneConversationHero.Name);
					}
					inventoryLogic.AfterReset += ResetHeroEquipment;
					InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
					inventoryState.InitializeLogic(inventoryLogic);
					Game.Current.GameStateManager.PushState(inventoryState);
					Traverse.Create(Campaign.Current.InventoryManager).Field<InventoryLogic>("_inventoryLogic").Value = inventoryLogic;
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
			}

			private void ResetHeroEquipment(InventoryLogic inventoryLogic)
			{
				if (battle_equipment_backup != null)
				{
					Hero.OneToOneConversationHero.BattleEquipment.FillFrom(battle_equipment_backup);
				}
				if (civilian_equipment_backup != null)
				{
					Hero.OneToOneConversationHero.CivilianEquipment.FillFrom(civilian_equipment_backup);
				}
				civilian_equipment_backup = null;
				battle_equipment_backup = null;
			}

			private bool conversation_troops_and_prisoners_party_clan_on_condition()
			{
				try
				{
					MBTextManager.SetTextVariable("PARTY_LIST_TAG", Hero.OneToOneConversationHero.PartyBelongedTo.Party.Name);
					PartyScreenManager.OpenScreenAsLoot(Hero.OneToOneConversationHero.PartyBelongedTo.Party);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
				return true;
			}

			private bool get_is_line_on_page(bool othercondition)
			{
				if (othercondition)
				{
					if (line_index < lines_current_page * lines_per_page && line_index >= (lines_current_page - 1) * lines_per_page)
					{
						line_index++;
						return true;
					}
					line_index++;
				}
				return false;
			}

			private bool give_party_order_patrol_additional_condition()
			{
				for (int i = 1; i <= 3; i++)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_" + i, "");
				}
				if (order.FriendlyVillagesScoreMultiplier == 1f && order.PartyMaintenanceScoreMultiplier == 1f && order.OwnClanVillagesScoreMultiplier == 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_1", "{=DpjCas89}Any additional instructions to follow during the patrol?");
				}
				else if (order.FriendlyVillagesScoreMultiplier < 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_1", "{=gZ3JPgdJ}The villages of our allied clans will have to do without us.\n");
				}
				if (order.OwnClanVillagesScoreMultiplier > 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_2", "{=xVmFj0Vt}We'll come to the aid of more distant villages as well.\n");
				}
				if (order.PartyMaintenanceScoreMultiplier > 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_PATROL_3", "{=FZsSqjwF}We shall frequently visit nearby settlements for troops and to trade loot, supplies and prisoners.");
				}
				return true;
			}

			private bool give_party_order_escort_additional_condition()
			{
				string _0 = "";
				string _1 = "{=2EyKGT16}Any additional instructions to follow while escorting you?";
				string _2 = "{=hg0GrxBV}We won't flee from enemies stronger than us.";
				string _3 = "{=BEdedw1C}We won't chase any enemies unless you give the signal.";
				string _4 = "{=DL3kEH8n}We shall stay at your side until commanded otherwise.";
				string _5 = "\nWe'll only take enemy leaders as prisoner.";
				if (order.AttackInitiative == 1f && order.AvoidInitiative == 1f)
				{
					_0 = _1;
				}
				if (order.AvoidInitiative == 0f)
				{
					_0 = _2;
				}
				if (order.AttackInitiative == 0f)
				{
					_0 = _3;
				}
				if (order.AttackInitiative == 0f && order.AvoidInitiative == 0f)
				{
					_0 = _4;
				}
				if (order.StopTakingPrisoners)
				{
					_0 += _5;
				}
				MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ESCORT", _0);
				return true;
			}

			private bool give_party_order_roam_additional_condition()
			{
				for (int i = 1; i <= 8; i++)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_" + i, "");
				}
				if (order.FriendlyVillagesScoreMultiplier == 1f && !findplayerifbored && order.LeaveTroopsToGarrisonOtherClans && order.AllowJoiningArmies && order.AllowRaidingVillages && order.HostileSettlementsScoreMultiplier > 0f && order.AttackInitiative == 1f && order.AvoidInitiative == 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_1", "{=EBO8npRn}Any additional instructions to follow while roaming the lands?");
				}
				else if (order.FriendlyVillagesScoreMultiplier < 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_1", "{=n8KqPSs3}I won't help defending settlements of allied clans.\n");
				}
				if (!order.LeaveTroopsToGarrisonOtherClans)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_2", "{=hg8h2ZRQ}I'll never give my troops to garrisons of allied clans.\n");
				}
				if (!order.AllowJoiningArmies)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_3", "{=GKLbkTYg}I won't join armies unless you call for me.\n");
				}
				if (!order.AllowRaidingVillages)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_4", "{=Et1Fdug7}I won't raid any villages.\n");
				}
				if (order.HostileSettlementsScoreMultiplier == 0f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_5", "{=xCAkZSk8}I'll ignore opportunities to conquer territory.\n");
				}
				if (order.AvoidInitiative > 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_6", "{=s3xzYOlT}We'll run from anyone who may cause us significant losses.\n");
				}
				if (order.AttackInitiative < 1f)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_7", "{=2wfMJTSO}We'll chase only the most tempting enemy parties.\n");
				}
				if (findplayerifbored)
				{
					MBTextManager.SetTextVariable("ADDITIONAL_ORDERS_ROAM_8", "{=66JVQdWZ}We'll come follow you if I can't think of much else to do.");
				}
				return true;
			}

			private void give_party_order_roam_confirm_consequence()
			{
				order.OwnerHero.cancelOrder();
				order.OwnerHero.PartyBelongedTo.Army = null;
				if (findplayerifbored)
				{
					order.Behavior = AiBehavior.EscortParty;
					order.TargetParty = Hero.MainHero.PartyBelongedTo;
					order.ScoreMinimum = 0.25f;
					order.ScoreMultiplier = 1f;
					order.OwnerHero.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
				}
				else
				{
					order.Behavior = AiBehavior.None;
					order.ScoreMinimum = 0f;
					order.ScoreMultiplier = 0f;
					order.OwnerHero.PartyBelongedTo.SetMoveModeHold();
				}
				PartyAICommandsBehavior.RegisterOrder(order.OwnerHero, order);
			}

			private void give_party_order_patrol_confirm_consequence()
			{
				order.OwnerHero.cancelOrder();
				order.OwnerHero.PartyBelongedTo.Army = null;
				order.Behavior = AiBehavior.PatrolAroundPoint;
				order.ScoreMinimum = 0.5f;
				order.ScoreMultiplier = 1.3f;
				order.HostileSettlementsScoreMultiplier = 0.1f;
				order.LeaveTroopsToGarrisonOtherClans = false;
				order.AllowJoiningArmies = false;
				order.AllowRaidingVillages = false;
				PartyAICommandsBehavior.RegisterOrder(order.OwnerHero, order);
				order.OwnerHero.PartyBelongedTo.SetMovePatrolAroundSettlement(order.TargetSettlement);
			}

			private void give_party_order_escort_confirm_consequence()
			{
				order.OwnerHero.cancelOrder();
				order.OwnerHero.PartyBelongedTo.Army = null;
				order.Behavior = AiBehavior.EscortParty;
				order.TargetParty = Hero.MainHero.PartyBelongedTo;
				order.ScoreMinimum = 15f;
				order.ScoreMultiplier = 1f;
				order.FriendlyVillagesScoreMultiplier = 0.5f;
				order.OwnClanVillagesScoreMultiplier = 0.5f;
				order.PartyMaintenanceScoreMultiplier = 0.5f;
				order.HostileSettlementsScoreMultiplier = 0.1f;
				order.LeaveTroopsToGarrisonOtherClans = false;
				order.AllowJoiningArmies = false;
				order.AllowRaidingVillages = false;
				PartyAICommandsBehavior.RegisterOrder(order.OwnerHero, order);
				order.OwnerHero.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
				if (Hero.OneToOneConversationHero.PartyBelongedTo.GetNumDaysForFoodToLast() < 3)
				{
					InformationManager.DisplayMessage(new InformationMessage(Hero.OneToOneConversationHero.PartyBelongedTo.Name?.ToString() + " is short on food.", Colors.Red));
				}
			}

			private void conversation_lord_leave_on_consequence()
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
			}

			private void MergeDisbandParty(MobileParty disbandParty, PartyBase mergeToParty)
			{
				disbandParty.LeaderHero.cancelOrder();
				mergeToParty.ItemRoster.Add(disbandParty.ItemRoster.AsEnumerable());
				FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster();
				foreach (TroopRosterElement troopRosterElement in disbandParty.PrisonRoster)
				{
					if (troopRosterElement.Character.IsHero)
					{
						GivePrisonerAction.Apply(troopRosterElement.Character, disbandParty.Party, mergeToParty);
					}
					else
					{
						flattenedTroopRoster.Add(troopRosterElement.Character, troopRosterElement.Number, troopRosterElement.WoundedNumber);
					}
				}
				foreach (TroopRosterElement troopRosterElement2 in disbandParty.MemberRoster.ToList())
				{
					disbandParty.MemberRoster.RemoveTroop(troopRosterElement2.Character);
					if (troopRosterElement2.Character.IsHero)
					{
						AddHeroToPartyAction.Apply(troopRosterElement2.Character.HeroObject, mergeToParty.MobileParty);
					}
					else
					{
						mergeToParty.MemberRoster.AddToCounts(troopRosterElement2.Character, troopRosterElement2.Number, insertAtFront: false, troopRosterElement2.WoundedNumber, troopRosterElement2.Xp);
					}
				}
				mergeToParty.AddPrisoners(flattenedTroopRoster);
				disbandParty.RemoveParty();
			}

			private void join_army()
			{
				PlayerEncounter.LeaveEncounter = true;
				if (MobileParty.MainParty.Army == null)
				{
					if (Clan.PlayerClan.IsUnderMercenaryService || Clan.PlayerClan.Kingdom == null)
					{
						CreateArmy(Hero.MainHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
					}
					else
					{
						Clan.PlayerClan.Kingdom.CreateArmy(Hero.MainHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
					}
				}
				Hero.OneToOneConversationHero.cancelOrder();
				Hero.OneToOneConversationHero.PartyBelongedTo.Army = MobileParty.MainParty.Army;
				SetPartyAiAction.GetActionForEscortingParty(Hero.OneToOneConversationHero.PartyBelongedTo, MobileParty.MainParty);
				Hero.OneToOneConversationHero.PartyBelongedTo.IsJoiningArmy = true;
			}

			private void CreateArmy(Hero armyLeader, IMapPoint target, Army.ArmyTypes selectedArmyType)
			{
				if (!armyLeader.IsActive)
				{
					return;
				}
				if (armyLeader?.PartyBelongedTo.Leader != null)
				{
					Army army = new Army(null, armyLeader.PartyBelongedTo, selectedArmyType, target)
					{
						AIBehavior = Army.AIBehaviorFlags.Gathering
					};
					army.Gather();
					Traverse method = Traverse.Create(CampaignEventDispatcher.Instance).Method("OnArmyCreated", new Type[1]
					{
						typeof(Army)
					});
					if (method.MethodExists())
					{
						method.GetValue(army);
					}
					else
					{
						MessageBox.Show("Party AI Overhaul and Commands: Cannot find dispatch method OnArmyCreated, needs update.");
					}
				}
				if (armyLeader == Hero.MainHero)
				{
					(Game.Current.GameStateManager.GameStates.Single((GameState S) => S is MapState) as MapState)?.OnArmyCreated(MobileParty.MainParty);
				}
			}
		}

		[SaveableField(13)]
		private MobileParty _tempTargetParty;

		[SaveableField(14)]
		private MobileParty _ownerParty;

		[SaveableField(15)]
		private Hero _ownerHero;

		[SaveableField(16)]
		private float _avoidInitiative = -1f;

		[SaveableField(17)]
		private float _attackInitiative = -1f;

		[SaveableProperty(1)]
		public MobileParty TargetParty
		{
			get;
			private set;
		}

		[SaveableProperty(2)]
		public Settlement TargetSettlement
		{
			get;
			private set;
		}

		[SaveableProperty(3)]
		public AiBehavior Behavior
		{
			get;
			private set;
		}

		[SaveableProperty(4)]
		public float ScoreMultiplier
		{
			get;
			private set;
		} = 1f;


		[SaveableProperty(5)]
		public float ScoreMinimum
		{
			get;
			private set;
		}

		[SaveableProperty(6)]
		public float HostileSettlementsScoreMultiplier
		{
			get;
			private set;
		} = 1f;


		[SaveableProperty(7)]
		public float FriendlyVillagesScoreMultiplier
		{
			get;
			private set;
		} = 1f;


		[SaveableProperty(8)]
		public float PartyMaintenanceScoreMultiplier
		{
			get;
			private set;
		} = 1f;


		[SaveableProperty(9)]
		public float OwnClanVillagesScoreMultiplier
		{
			get;
			private set;
		} = 1f;


		[SaveableProperty(10)]
		public bool LeaveTroopsToGarrisonOtherClans
		{
			get;
			private set;
		} = true;


		[SaveableProperty(11)]
		public bool AllowRaidingVillages
		{
			get;
			private set;
		} = true;


		[SaveableProperty(12)]
		public bool AllowJoiningArmies
		{
			get;
			private set;
		} = true;


		public MobileParty TempTargetParty
		{
			get
			{
				return _tempTargetParty;
			}
			set
			{
				try
				{
					if (value == null)
					{
						_tempTargetParty = null;
						if (OwnerParty != null)
						{
							OwnerParty.Ai.SetDoNotMakeNewDecisions(enabled: false);
						}
					}
					else
					{
						_tempTargetParty = value;
						if (OwnerParty != null)
						{
							OwnerParty.Ai.SetDoNotMakeNewDecisions(enabled: true);
						}
					}
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.FlattenException());
				}
			}
		}

		public MobileParty OwnerParty => OwnerHero.PartyBelongedTo;

		public Hero OwnerHero
		{
			get
			{
				if (_ownerHero == null)
				{
					_ownerHero = PartyAICommandsBehavior.Instance.order_map.FirstOrDefault((KeyValuePair<Hero, PartyOrder> x) => x.Value == this).Key;
				}
				return _ownerHero;
			}
		}

		public float AvoidInitiative
		{
			get
			{
				if (_avoidInitiative < 0f && OwnerParty != null)
				{
					_avoidInitiative = Traverse.Create(OwnerParty).Field("_avoidInitiative").GetValue<float>();
				}
				return _avoidInitiative;
			}
			set
			{
				_avoidInitiative = value;
				if (OwnerParty != null)
				{
					OwnerParty.SetInititave(AttackInitiative, AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
				}
			}
		}

		public float AttackInitiative
		{
			get
			{
				if (_attackInitiative < 0f && OwnerParty != null)
				{
					_attackInitiative = Traverse.Create(OwnerParty).Field("_attackInitiative").GetValue<float>();
				}
				return _attackInitiative;
			}
			set
			{
				_attackInitiative = value;
				if (OwnerParty != null)
				{
					OwnerParty.SetInititave(AttackInitiative, AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
				}
			}
		}

		[SaveableProperty(18)]
		public bool StopRecruitingTroops
		{
			get;
			private set;
		}

		[SaveableProperty(19)]
		public bool StopTakingPrisoners
		{
			get;
			private set;
		}

		public PartyOrder(Hero owner)
		{
			_ownerHero = owner;
			_attackInitiative = 1f;
			_avoidInitiative = 1f;
		}

		public float getScore(float base_score = 0f)
		{
			return Math.Max(base_score * ScoreMultiplier, ScoreMinimum);
		}
	}
}
