using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace PartyAIOverhaulCommands
{
    internal class ConfigLoader
	{
		private static ConfigLoader _instance;

		public Config Config
		{
			get;
			private set;
		}

		public static ConfigLoader Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new ConfigLoader();
				}
				return _instance;
			}
		}

		private ConfigLoader()
		{
			string path = Path.Combine(BasePath.Name, "Modules", "PartyAIOverhaulCommands", "ModuleData", "config.xml");
			Config = getConfig(path);
		}

		private Config getConfig(string filePath)
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Config));
				using StreamReader reader = new StreamReader(filePath);
				return (Config)serializer.Deserialize(reader);
			}
			catch (Exception e)
			{
				MessageBox.Show("Failed to load Party AI Overhaul and Commands config, using default values due to: " + e.FlattenException());
				return new Config();
			}
		}
	}
}
