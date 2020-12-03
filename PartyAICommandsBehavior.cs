using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HarmonyLib;
using PartyAIOverhaulCommands;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.Conversations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace PartyAIOverhaulCommands.src.Behaviours
{
	internal class PartyAICommandsBehavior : CampaignBehaviorBase
	{
		public class MySaveDefiner : SaveableTypeDefiner
		{
			public MySaveDefiner()
				: base(56335716)
			{
			}

			protected override void DefineClassTypes()
			{
				AddClassDefinition(typeof(PartyOrder), 56335717);
			}

			protected override void DefineContainerDefinitions()
			{
				ConstructContainerDefinition(typeof(Dictionary<MobileParty, PartyOrder>));
				ConstructContainerDefinition(typeof(Dictionary<Hero, PartyOrder>));
				ConstructContainerDefinition(typeof(Dictionary<Hero, TroopRoster>));
			}
		}

		private ApplicationVersion savegame_module_version;

		public Dictionary<Hero, PartyOrder> order_map;

		public Dictionary<Hero, TroopRoster> template_map;

		public static readonly PartyAICommandsBehavior Instance = new PartyAICommandsBehavior();

		public override void RegisterEvents()
		{
			order_map = new Dictionary<Hero, PartyOrder>();
			template_map = new Dictionary<Hero, TroopRoster>();
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
			CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
			CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party != MobileParty.MainParty)
			{
				return;
			}
			foreach (KeyValuePair<Hero, PartyOrder> pair in Instance.order_map)
			{
				Hero leader = pair.Key;
				PartyOrder order = pair.Value;
				if (leader != null && order != null && leader.PartyBelongedTo != null && order.Behavior == AiBehavior.EscortParty && order.TargetParty == MobileParty.MainParty && order.TempTargetParty == null)
				{
					leader.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
				}
			}
		}

		private void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party != MobileParty.MainParty)
			{
				return;
			}
			foreach (KeyValuePair<Hero, PartyOrder> pair in Instance.order_map)
			{
				Hero leader = pair.Key;
				PartyOrder order = pair.Value;
				if (leader != null && order != null && leader.PartyBelongedTo != null && order.Behavior == AiBehavior.EscortParty && order.TargetParty == MobileParty.MainParty && order.TempTargetParty == null)
				{
					leader.PartyBelongedTo.SetMoveGoToSettlement(settlement);
					Traverse.Create(leader.PartyBelongedTo).Method("OnAiTickInternal").GetValue();
				}
			}
		}

		private void OnConversationEnded(CharacterObject character)
		{
			if (character?.HeroObject?.getOrder() == null)
			{
				return;
			}
			Hero hero = character.HeroObject;
			PartyOrder order = hero.getOrder();
			if (hero.IsPartyLeader)
			{
				if (order.Behavior == AiBehavior.EscortParty)
				{
					hero.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
				}
				hero.PartyBelongedTo.Ai.RethinkAtNextHourlyTick = true;
			}
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party?.LeaderHero == null || (!settlement.IsTown && !settlement.IsVillage) || (!party.IsLordParty && !party.IsCaravan))
			{
				return;
			}
			Dictionary<ItemCategory, int> needed = new Dictionary<ItemCategory, int>(5);
			TroopRoster template = party?.LeaderHero?.getTemplate();
			Town town = null;
			if (settlement.IsTown)
			{
				town = settlement.GetComponent<Town>();
			}
			foreach (TroopRosterElement element3 in party.MemberRoster)
			{
				int upgrades = element3.NumberReadyToUpgrade;
				if (upgrades <= 0)
				{
					continue;
				}
				CharacterObject[] upgradeTargets = element3.Character.UpgradeTargets;
				foreach (CharacterObject upgrade in upgradeTargets)
				{
					ItemCategory item_category = upgrade.UpgradeRequiresItemFromCategory;
					if ((town != null && town.MarketData.GetItemCountOfCategory(item_category) == 0) || item_category == null)
					{
						continue;
					}
					if (template != null)
					{
						int template_count = template.GetTroopCount(upgrade);
						if (template_count == 0)
						{
							continue;
						}
						if (template_count > 1)
						{
							upgrades = Math.Min(upgrades, template_count - party.MemberRoster.GetTroopCount(upgrade));
							if (upgrades <= 0)
							{
								continue;
							}
						}
					}
					if (needed.ContainsKey(item_category))
					{
						needed[item_category] += upgrades;
					}
					else
					{
						needed[item_category] = upgrades;
					}
				}
			}
			if (needed.Count <= 0)
			{
				return;
			}
			if (town != null)
			{
				foreach (ItemCategory category2 in needed.Keys)
				{
					if (needed[category2] > town.MarketData.GetItemCountOfCategory(category2))
					{
						needed[category2] = town.MarketData.GetItemCountOfCategory(category2);
					}
				}
			}
			Hero leader = party.LeaderHero;
			Dictionary<ItemCategory, List<ItemRosterElement>> candidate_items = new Dictionary<ItemCategory, List<ItemRosterElement>>(needed.Count);
			SettlementComponent market = settlement.GetComponent(typeof(SettlementComponent));
			foreach (ItemRosterElement element2 in settlement.ItemRoster)
			{
				ItemCategory category = element2.EquipmentElement.Item.ItemCategory;
				if (needed.ContainsKey(category) && !candidate_items.ContainsKey(category))
				{
					candidate_items[category] = new List<ItemRosterElement>();
				}
				candidate_items[category].Add(element2);
			}
			foreach (KeyValuePair<ItemCategory, List<ItemRosterElement>> pair in candidate_items)
			{
				foreach (ItemRosterElement element in pair.Value.OrderBy((ItemRosterElement o) => market.GetItemPrice(o.EquipmentElement, party)).ToList())
				{
					int tobuy = Math.Min(element.Amount, needed[pair.Key]);
					while (tobuy > 0 && leader.Gold > market.GetItemPrice(element.EquipmentElement, party))
					{
						SellItemsAction.Apply(settlement.Party, party.Party, element, 1, settlement);
						tobuy--;
						needed[pair.Key]--;
					}
					if (tobuy > 0 || needed[pair.Key] == 0)
					{
						break;
					}
				}
			}
		}

		public static void RegisterOrder(Hero leader, PartyOrder order)
		{
			if (Instance.order_map == null)
			{
				Instance.order_map = new Dictionary<Hero, PartyOrder>();
			}
			Instance.order_map[leader] = order;
			leader.PartyBelongedTo.SetInititave(order.AttackInitiative, order.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
		}

		public static void RegisterTemplate(Hero leader, TroopRoster template)
		{
			if (Instance.template_map == null)
			{
				Instance.template_map = new Dictionary<Hero, TroopRoster>();
			}
			Instance.template_map[leader] = new TroopRoster(null);
			Instance.template_map[leader].Add(template);
		}

		private void OnDailyTick()
		{
			if (Config.Value.ClanPartyGoldLimitToTakeFromTreasury <= 0)
			{
				return;
			}
			foreach (MobileParty p in Clan.PlayerClan.WarParties)
			{
				if (!p.IsGarrison && !p.IsMilitia && !p.IsVillager && !p.IsCaravan && !p.IsMainParty && p?.LeaderHero != null && p.LeaderHero.Gold < Config.Value.ClanPartyGoldLimitToTakeFromTreasury)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, p.LeaderHero, Config.Value.ClanPartyGoldLimitToTakeFromTreasury);
					InformationManager.DisplayMessage(new InformationMessage(p.LeaderHero.Name?.ToString() + " is short on gold and gets " + Config.Value.ClanPartyGoldLimitToTakeFromTreasury + " from the treasury.", Colors.Yellow));
				}
			}
		}

		private void OnGameLoaded(CampaignGameStarter gameStarterObject)
		{
			try
			{
				gameStarterObject.LoadConversations(typeof(ConversationsCallbacks), BasePath.Name + "Modules/PartyAIOverhaulCommands/ModuleData/party_ai_commands.xml");
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
			}
		}

		private void OnNewGameCreated(CampaignGameStarter gameStarterObject)
		{
			try
			{
				gameStarterObject.LoadConversations(typeof(ConversationsCallbacks), BasePath.Name + "Modules/PartyAIOverhaulCommands/ModuleData/party_ai_commands.xml");
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
			}
		}

		public override void SyncData(IDataStore dataStore)
		{
			if (dataStore.IsLoading)
			{
				order_map = new Dictionary<Hero, PartyOrder>();
				template_map = new Dictionary<Hero, TroopRoster>();
				ApplicationVersion? GetModuleVersion = Traverse.Create(typeof(TaleWorlds.Core.MetaDataExtensions)).Method("GetModuleVersion", new Type[2]
				{
				typeof(MetaData),
				typeof(string)
				}).GetValue<ApplicationVersion>(new object[2]
				{
				CheckModulesPatch.meta_data,
				"Party AI Overhaul and Commands"
				});
				CheckModulesPatch.meta_data = null;
				savegame_module_version = GetModuleVersion.GetValueOrDefault();
				if (savegame_module_version.Major == 1)
				{
					Dictionary<MobileParty, PartyOrder> old_order_map2 = new Dictionary<MobileParty, PartyOrder>();
					dataStore.SyncData("order_list", ref old_order_map2);
					dataStore.SyncData("order_map", ref order_map);
					if (old_order_map2.Count > 0 && order_map.Count == 0)
					{
						foreach (KeyValuePair<MobileParty, PartyOrder> pair in old_order_map2)
						{
							if (pair.Key?.LeaderHero != null && pair.Value != null)
							{
								order_map[pair.Key.LeaderHero] = pair.Value;
							}
						}
						old_order_map2 = null;
					}
				}
				else if (savegame_module_version.Major > 1)
				{
					dataStore.SyncData("order_map", ref order_map);
					dataStore.SyncData("template_map", ref template_map);
				}
				else
				{
					dataStore.SyncData("order_map", ref order_map);
					dataStore.SyncData("template_map", ref template_map);
					MessageBox.Show("Party AI Overhaul and Commands: It seems you are loading a savegame where you temporarily disabled this mod. This is not recommended and may lead to unexpected issues. It is recommended to revert to a savegame where you hadn't yet disabled this mod. Continue at your own risk.");
				}
				if (!CheckModulesPatch.missing_modules)
				{
					return;
				}
				foreach (KeyValuePair<Hero, TroopRoster> item in template_map)
				{
					item.Value.RemoveIf((TroopRosterElement troop) => !troop.Character.IsInitialized);
				}
			}
			else
			{
				if (savegame_module_version.Major == 1)
				{
					Dictionary<MobileParty, PartyOrder> old_order_map = new Dictionary<MobileParty, PartyOrder>();
					dataStore.SyncData("order_list", ref old_order_map);
				}
				dataStore.SyncData("order_map", ref order_map);
				dataStore.SyncData("template_map", ref template_map);
			}
		}

		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			if (savegame_module_version.Major < 3 && savegame_module_version.Minor < 5 && savegame_module_version.Revision < 3)
			{
				FindDuplicateParties();
			}
			RegisterIndyArmyTimer();
			PartyOrder.PartyOrderBuilder.Instance.RegisterDialogue(campaignGameStarter);
			if (!GetBestInitiativeBehaviorPatch.parties_around_position_patched || !GetBestInitiativeBehaviorPatch.parties_distance_patched)
			{
				MessageBox.Show("One or more \"Party AI Overhaul and Commands\" transpiler patches failed.\nThe mod likely needs an update. If there is no update available yet, you can still continue playing the game.\nThe only thing that you're missing out on is the extended range parties react to each other");
			}
		}

		private void FindDuplicateParties()
		{
			try
			{
				HashSet<Hero> companions = new HashSet<Hero>(5);
				HashSet<Hero> party_leaders = new HashSet<Hero>(5);
				for (int j = 0; j < MobileParty.MainParty.MemberRoster.Count; j++)
				{
					TroopRosterElement element = MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(j);
					if (element.Character.IsHero && element.Character != Hero.MainHero.CharacterObject)
					{
						if (element.Number > 1)
						{
							MessageBox.Show("Party AI Overhaul and Commands: Found duplicate companion: " + element.Character.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
							MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(j, -(element.Number - 1));
						}
						companions.Add(element.Character.HeroObject);
					}
				}
				for (int i = 0; i < Campaign.Current.MobileParties.Count; i++)
				{
					MobileParty party = Campaign.Current.MobileParties[i];
					if (party?.Party?.Owner != null && party.IsLordParty)
					{
						if (party.LeaderHero == null)
						{
							MessageBox.Show("Party AI Overhaul and Commands: Found duplicate party: " + party.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
							party.RemoveParty();
						}
						else if (companions.Contains(party.LeaderHero))
						{
							MessageBox.Show("Party AI Overhaul and Commands: Found duplicate companion and party: " + party.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate companion in your party will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
							MobileParty.MainParty.MemberRoster.RemoveTroop(party.Leader);
							party_leaders.Add(party.LeaderHero);
						}
						else if (party_leaders.Contains(party.LeaderHero) && party.LeaderHero.Clan == Clan.PlayerClan)
						{
							MessageBox.Show("Party AI Overhaul and Commands: Found duplicate party: " + party.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
							party.RemoveParty();
						}
						else
						{
							party_leaders.Add(party.LeaderHero);
						}
					}
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
			}
		}

		private void RegisterIndyArmyTimer()
		{
			if (Hero.MainHero.PartyBelongedTo == null)
			{
				return;
			}
			Army army = Hero.MainHero.PartyBelongedTo.Army;
			if (army?.LeaderParty != null && army.LeaderParty == Hero.MainHero.PartyBelongedTo && army.Kingdom == null)
			{
				Traverse method = Traverse.Create(army).Method("OnAfterLoad");
				if (method.MethodExists())
				{
					method.GetValue();
				}
				else
				{
					MessageBox.Show("Party AI Overhaul and Commands: Cannot find method Army.OnAfterLoad, needs update.");
				}
			}
		}
	}

}

