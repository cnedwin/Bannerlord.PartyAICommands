using System;
using System.Windows.Forms;
using HarmonyLib;
using PartyAIOverhaulCommands.src.Behaviours;
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
			catch (Exception e)
			{
				MessageBox.Show("Couldn't apply Harmony due to: " + e.FlattenException());
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			try
			{
				base.OnGameStart(game, gameStarterObject);
				if (game.GameType is Campaign)
				{
					AddBehaviors(gameStarterObject as CampaignGameStarter);
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.FlattenException());
			}
		}

		public override void OnGameInitializationFinished(Game game)
		{
		}

		private void AddBehaviors(CampaignGameStarter gameStarterObject)
		{
			gameStarterObject?.AddBehavior(PartyAICommandsBehavior.Instance);
		}
	}
}
