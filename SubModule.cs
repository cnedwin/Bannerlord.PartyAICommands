using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PartyAIOverhaulCommands
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                new Harmony("mod.octavius.bannerlord").PatchAll();
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show("Couldn't apply Harmony due to: " + ex.FlattenException());
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                base.OnGameStart(game, gameStarterObject);
                if (!(game.GameType is Campaign))
                    return;
                this.AddBehaviors(gameStarterObject as CampaignGameStarter);
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.FlattenException());
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
        }

        private void AddBehaviors(CampaignGameStarter gameStarterObject) => gameStarterObject?.AddBehavior((CampaignBehaviorBase)PartyAICommandsBehavior.Instance);
    }
}