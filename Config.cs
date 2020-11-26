using System;

namespace PartyAIOverhaulCommands
{
	[Serializable]
	public class Config
	{
		public static Config Value => ConfigLoader.Instance.Config;

		public float RelationRaidingPositiveMultMin
		{
			get;
			set;
		} = 0.3f;


		public float RelationRaidingNegativeMultMax
		{
			get;
			set;
		} = 1.5f;


		public float RelationSiegingPositiveMultMin
		{
			get;
			set;
		} = 0.5f;


		public float RelationSiegingNegativeMultMax
		{
			get;
			set;
		} = 1.5f;


		public float SameCultureRaidingMult
		{
			get;
			set;
		} = 0.5f;


		public float SameCultureSiegingMult
		{
			get;
			set;
		} = 1.5f;


		public int OrderEscortEngageHoldKey
		{
			get;
			set;
		} = 56;


		public bool EnableDebugCancelAllOrders
		{
			get;
			set;
		}

		public float MinimumDaysFoodToLastWhileBuyingFood
		{
			get;
			set;
		} = 15f;


		public int ClanPartyGoldLimitToTakeFromTreasury
		{
			get;
			set;
		} = 200;


		public bool EnableBorderOnlySieges
		{
			get;
			set;
		} = true;

	}
}
