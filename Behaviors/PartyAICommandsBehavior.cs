using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
        private ApplicationVersion savegame_module_version;
        public Dictionary<Hero, PartyOrder> order_map;
        public Dictionary<Hero, TroopRoster> template_map;
        public static readonly PartyAICommandsBehavior Instance = new PartyAICommandsBehavior();

        public override void RegisterEvents()
        {
            this.order_map = new Dictionary<Hero, PartyOrder>();
            this.template_map = new Dictionary<Hero, TroopRoster>();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, new Action(this.OnDailyTick));
            CampaignEvents.ConversationEnded.AddNonSerializedListener((object)this, new Action<CharacterObject>(this.OnConversationEnded));
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener((object)this, new Action<MobileParty, Settlement, Hero>(this.OnAfterSettlementEntered));
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener((object)this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party != MobileParty.MainParty)
                return;
            foreach (KeyValuePair<Hero, PartyOrder> order in PartyAICommandsBehavior.Instance.order_map)
            {
                Hero key = order.Key;
                PartyOrder partyOrder = order.Value;
                if (key != null && partyOrder != null && (key.PartyBelongedTo != null && partyOrder.Behavior == AiBehavior.EscortParty) && (partyOrder.TargetParty == MobileParty.MainParty && partyOrder.TempTargetParty == null))
                    key.PartyBelongedTo.SetMoveEscortParty(partyOrder.TargetParty);
            }
        }

        private void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty)
                return;
            foreach (KeyValuePair<Hero, PartyOrder> order in PartyAICommandsBehavior.Instance.order_map)
            {
                Hero key = order.Key;
                PartyOrder partyOrder = order.Value;
                if (key != null && partyOrder != null && (key.PartyBelongedTo != null && partyOrder.Behavior == AiBehavior.EscortParty) && (partyOrder.TargetParty == MobileParty.MainParty && partyOrder.TempTargetParty == null))
                {
                    key.PartyBelongedTo.SetMoveGoToSettlement(settlement);
                    Traverse.Create((object)key.PartyBelongedTo).Method("OnAiTickInternal").GetValue();
                }
            }
        }

        private void OnConversationEnded(CharacterObject character)
        {
            PartyOrder partyOrder;
            if (character == null)
            {
                partyOrder = (PartyOrder)null;
            }
            else
            {
                Hero heroObject = character.HeroObject;
                partyOrder = heroObject != null ? heroObject.getOrder() : (PartyOrder)null;
            }
            if (partyOrder == null)
                return;
            Hero heroObject1 = character.HeroObject;
            PartyOrder order = heroObject1.getOrder();
            if (!heroObject1.IsPartyLeader)
                return;
            if (order.Behavior == AiBehavior.EscortParty)
                heroObject1.PartyBelongedTo.SetMoveEscortParty(order.TargetParty);
            heroObject1.PartyBelongedTo.Ai.RethinkAtNextHourlyTick = true;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party?.LeaderHero == null || !settlement.IsTown && !settlement.IsVillage || !party.IsLordParty && !party.IsCaravan)
                return;
            Dictionary<ItemCategory, int> dictionary1 = new Dictionary<ItemCategory, int>(5);
            MobileParty mobileParty = party;
            TroopRoster troopRoster1;
            if (mobileParty == null)
            {
                troopRoster1 = (TroopRoster)null;
            }
            else
            {
                Hero leaderHero = mobileParty.LeaderHero;
                troopRoster1 = leaderHero != null ? leaderHero.getTemplate() : (TroopRoster)null;
            }
            TroopRoster troopRoster2 = troopRoster1;
            Town town = (Town)null;
            if (settlement.IsTown)
                town = settlement.GetComponent<Town>();
            foreach (TroopRosterElement troopRosterElement in party.MemberRoster)
            {
                int val1 = troopRosterElement.NumberReadyToUpgrade;
                if (val1 > 0)
                {
                    foreach (CharacterObject upgradeTarget in troopRosterElement.Character.UpgradeTargets)
                    {
                        ItemCategory itemFromCategory = upgradeTarget.UpgradeRequiresItemFromCategory;
                        if ((town == null || town.MarketData.GetItemCountOfCategory(itemFromCategory) != 0) && itemFromCategory != null)
                        {
                            if (troopRoster2 != (TroopRoster)null)
                            {
                                int troopCount = troopRoster2.GetTroopCount(upgradeTarget);
                                if (troopCount != 0)
                                {
                                    if (troopCount > 1)
                                    {
                                        val1 = Math.Min(val1, troopCount - party.MemberRoster.GetTroopCount(upgradeTarget));
                                        if (val1 <= 0)
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            if (dictionary1.ContainsKey(itemFromCategory))
                                dictionary1[itemFromCategory] += val1;
                            else
                                dictionary1[itemFromCategory] = val1;
                        }
                    }
                }
            }
            if (dictionary1.Count <= 0)
                return;
            if (town != null)
            {
                foreach (ItemCategory key in dictionary1.Keys)
                {
                    if (dictionary1[key] > town.MarketData.GetItemCountOfCategory(key))
                        dictionary1[key] = town.MarketData.GetItemCountOfCategory(key);
                }
            }
            Hero leaderHero1 = party.LeaderHero;
            Dictionary<ItemCategory, List<ItemRosterElement>> dictionary2 = new Dictionary<ItemCategory, List<ItemRosterElement>>(dictionary1.Count);
            SettlementComponent market = settlement.GetComponent(typeof(SettlementComponent));
            foreach (ItemRosterElement itemRosterElement in settlement.ItemRoster)
            {
                ItemCategory itemCategory = itemRosterElement.EquipmentElement.Item.ItemCategory;
                if (dictionary1.ContainsKey(itemCategory) && !dictionary2.ContainsKey(itemCategory))
                    dictionary2[itemCategory] = new List<ItemRosterElement>();
                dictionary2[itemCategory].Add(itemRosterElement);
            }
            foreach (KeyValuePair<ItemCategory, List<ItemRosterElement>> keyValuePair in dictionary2)
            {
                foreach (ItemRosterElement subject in keyValuePair.Value.OrderBy<ItemRosterElement, int>((Func<ItemRosterElement, int>)(o => market.GetItemPrice(o.EquipmentElement, party))).ToList<ItemRosterElement>())
                {
                    int num = Math.Min(subject.Amount, dictionary1[keyValuePair.Key]);
                    while (num > 0 && leaderHero1.Gold > market.GetItemPrice(subject.EquipmentElement, party))
                    {
                        SellItemsAction.Apply(settlement.Party, party.Party, subject, 1, settlement);
                        --num;
                        dictionary1[keyValuePair.Key]--;
                    }
                    if (num <= 0)
                    {
                        if (dictionary1[keyValuePair.Key] == 0)
                            break;
                    }
                    else
                        break;
                }
            }
        }

        public static void RegisterOrder(Hero leader, PartyOrder order)
        {
            if (PartyAICommandsBehavior.Instance.order_map == null)
                PartyAICommandsBehavior.Instance.order_map = new Dictionary<Hero, PartyOrder>();
            PartyAICommandsBehavior.Instance.order_map[leader] = order;
            leader.PartyBelongedTo.SetInititave(order.AttackInitiative, order.AvoidInitiative, CampaignTime.YearsFromNow(100f).RemainingHoursFromNow);
        }

        public static void RegisterTemplate(Hero leader, TroopRoster template)
        {
            if (PartyAICommandsBehavior.Instance.template_map == null)
                PartyAICommandsBehavior.Instance.template_map = new Dictionary<Hero, TroopRoster>();
            PartyAICommandsBehavior.Instance.template_map[leader] = new TroopRoster((PartyBase)null);
            PartyAICommandsBehavior.Instance.template_map[leader].Add(template);
        }

        private void OnDailyTick()
        {
            if (Config.Value.ClanPartyGoldLimitToTakeFromTreasury <= 0)
                return;
            foreach (MobileParty warParty in Clan.PlayerClan.WarParties)
            {
                if (!warParty.IsGarrison && !warParty.IsMilitia && (!warParty.IsVillager && !warParty.IsCaravan) && !warParty.IsMainParty && (warParty?.LeaderHero != null && warParty.LeaderHero.Gold < Config.Value.ClanPartyGoldLimitToTakeFromTreasury))
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, warParty.LeaderHero, Config.Value.ClanPartyGoldLimitToTakeFromTreasury);
                    InformationManager.DisplayMessage(new InformationMessage(warParty.LeaderHero.Name?.ToString() + " is short on gold and gets " + Config.Value.ClanPartyGoldLimitToTakeFromTreasury.ToString() + " from the treasury.", Colors.Yellow));
                }
            }
        }

        private void OnGameLoaded(CampaignGameStarter gameStarterObject)
        {
            try
            {
                gameStarterObject.LoadConversations(typeof(ConversationsCallbacks), BasePath.Name + "Modules/PartyAIOverhaulCommands/ModuleData/party_ai_commands.xml");
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
            }
        }

        private void OnNewGameCreated(CampaignGameStarter gameStarterObject)
        {
            try
            {
                gameStarterObject.LoadConversations(typeof(ConversationsCallbacks), BasePath.Name + "Modules/PartyAIOverhaulCommands/ModuleData/party_ai_commands.xml");
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsLoading)
            {
                this.order_map = new Dictionary<Hero, PartyOrder>();
                this.template_map = new Dictionary<Hero, TroopRoster>();
                ApplicationVersion? nullable = new ApplicationVersion?(Traverse.Create(typeof(TaleWorlds.Core.MetaDataExtensions)).Method("GetModuleVersion", new System.Type[2]
                {
          typeof (MetaData),
          typeof (string)
                }, (object[])null).GetValue<ApplicationVersion>((object)CheckModulesPatch.meta_data, (object)"Party AI Overhaul and Commands"));
                CheckModulesPatch.meta_data = (MetaData)null;
                this.savegame_module_version = nullable.GetValueOrDefault();
                if (this.savegame_module_version.Major == 1)
                {
                    Dictionary<MobileParty, PartyOrder> data = new Dictionary<MobileParty, PartyOrder>();
                    dataStore.SyncData<Dictionary<MobileParty, PartyOrder>>("order_list", ref data);
                    dataStore.SyncData<Dictionary<Hero, PartyOrder>>("order_map", ref this.order_map);
                    if (data.Count > 0 && this.order_map.Count == 0)
                    {
                        foreach (KeyValuePair<MobileParty, PartyOrder> keyValuePair in data)
                        {
                            if (keyValuePair.Key?.LeaderHero != null && keyValuePair.Value != null)
                                this.order_map[keyValuePair.Key.LeaderHero] = keyValuePair.Value;
                        }
                    }
                }
                else if (this.savegame_module_version.Major > 1)
                {
                    dataStore.SyncData<Dictionary<Hero, PartyOrder>>("order_map", ref this.order_map);
                    dataStore.SyncData<Dictionary<Hero, TroopRoster>>("template_map", ref this.template_map);
                }
                else
                {
                    dataStore.SyncData<Dictionary<Hero, PartyOrder>>("order_map", ref this.order_map);
                    dataStore.SyncData<Dictionary<Hero, TroopRoster>>("template_map", ref this.template_map);
                    int num = (int)MessageBox.Show(string.Format("{0} TEST Party AI Overhaul and Commands: It seems you are loading a savegame where you temporarily disabled this mod. This is not recommended and may lead to unexpected issues. It is recommended to revert to a savegame where you hadn't yet disabled this mod. Continue at your own risk.", (object)this.savegame_module_version));
                }
                if (!CheckModulesPatch.missing_modules)
                    return;
                foreach (KeyValuePair<Hero, TroopRoster> template in this.template_map)
                    template.Value.RemoveIf((Predicate<TroopRosterElement>)(troop => !troop.Character.IsInitialized));
            }
            else
            {
                if (this.savegame_module_version.Major == 1)
                {
                    Dictionary<MobileParty, PartyOrder> data = new Dictionary<MobileParty, PartyOrder>();
                    dataStore.SyncData<Dictionary<MobileParty, PartyOrder>>("order_list", ref data);
                }
                dataStore.SyncData<Dictionary<Hero, PartyOrder>>("order_map", ref this.order_map);
                dataStore.SyncData<Dictionary<Hero, TroopRoster>>("template_map", ref this.template_map);
            }
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            if (this.savegame_module_version.Major < 3 && this.savegame_module_version.Minor < 5 && this.savegame_module_version.Revision < 3)
                this.FindDuplicateParties();
            this.RegisterIndyArmyTimer();
            PartyOrder.PartyOrderBuilder.Instance.RegisterDialogue(campaignGameStarter);
            if (GetBestInitiativeBehaviorPatch.parties_around_position_patched && GetBestInitiativeBehaviorPatch.parties_distance_patched)
                return;
            int num = (int)MessageBox.Show("One or more \"Party AI Overhaul and Commands\" transpiler patches failed.\nThe mod likely needs an update. If there is no update available yet, you can still continue playing the game.\nThe only thing that you're missing out on is the extended range parties react to each other");
        }

        private void FindDuplicateParties()
        {
            try
            {
                HashSet<Hero> heroSet1 = new HashSet<Hero>(5);
                HashSet<Hero> heroSet2 = new HashSet<Hero>(5);
                for (int index = 0; index < MobileParty.MainParty.MemberRoster.Count; ++index)
                {
                    TroopRosterElement elementCopyAtIndex = MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(index);
                    if (elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character != Hero.MainHero.CharacterObject)
                    {
                        if (elementCopyAtIndex.Number > 1)
                        {
                            int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Found duplicate companion: " + elementCopyAtIndex.Character.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
                            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(index, -(elementCopyAtIndex.Number - 1));
                        }
                        heroSet1.Add(elementCopyAtIndex.Character.HeroObject);
                    }
                }
                for (int index = 0; index < Campaign.Current.MobileParties.Count; ++index)
                {
                    MobileParty mobileParty = Campaign.Current.MobileParties[index];
                    if (mobileParty?.Party?.Owner != null && mobileParty.IsLordParty)
                    {
                        if (mobileParty.LeaderHero == null)
                        {
                            int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Found duplicate party: " + mobileParty.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
                            mobileParty.RemoveParty();
                        }
                        else if (heroSet1.Contains(mobileParty.LeaderHero))
                        {
                            int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Found duplicate companion and party: " + mobileParty.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate companion in your party will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
                            MobileParty.MainParty.MemberRoster.RemoveTroop(mobileParty.Leader);
                            heroSet2.Add(mobileParty.LeaderHero);
                        }
                        else if (heroSet2.Contains(mobileParty.LeaderHero) && mobileParty.LeaderHero.Clan == Clan.PlayerClan)
                        {
                            int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Found duplicate party: " + mobileParty.Name?.ToString() + "\nThis was most likely caused by a bug, the duplicate will now be deleted. You can continue playing.\nIf you have good reason to believe this was not a duplicate, please report so on nexusmods.");
                            mobileParty.RemoveParty();
                        }
                        else
                            heroSet2.Add(mobileParty.LeaderHero);
                    }
                }
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
            }
        }

        private void RegisterIndyArmyTimer()
        {
            if (Hero.MainHero.PartyBelongedTo == null)
                return;
            Army army = Hero.MainHero.PartyBelongedTo.Army;
            if (army?.LeaderParty == null || army.LeaderParty != Hero.MainHero.PartyBelongedTo || army.Kingdom != null)
                return;
            Traverse traverse = Traverse.Create((object)army).Method("OnAfterLoad");
            if (traverse.MethodExists())
            {
                traverse.GetValue();
            }
            else
            {
                int num = (int)MessageBox.Show("Party AI Overhaul and Commands: Cannot find method Army.OnAfterLoad, needs update.");
            }
        }

        public class MySaveDefiner : SaveableTypeDefiner
        {
            public MySaveDefiner()
              : base(56335716)
            {
            }

            protected override void DefineClassTypes() => this.AddClassDefinition(typeof(PartyOrder), 56335717);

            protected override void DefineContainerDefinitions()
            {
                this.ConstructContainerDefinition(typeof(Dictionary<MobileParty, PartyOrder>));
                this.ConstructContainerDefinition(typeof(Dictionary<Hero, PartyOrder>));
                this.ConstructContainerDefinition(typeof(Dictionary<Hero, TroopRoster>));
            }
        }
    }
}