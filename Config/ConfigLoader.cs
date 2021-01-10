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

        public Config Config { get; private set; }

        public static ConfigLoader Instance
        {
            get
            {
                if (ConfigLoader._instance == null)
                    ConfigLoader._instance = new ConfigLoader();
                return ConfigLoader._instance;
            }
        }

        private ConfigLoader() => this.Config = this.getConfig(Path.Combine(BasePath.Name, "Modules", "PartyAIOverhaulCommands", "ModuleData", "config.xml"));

        private Config getConfig(string filePath)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(filePath))
                    return (Config)new XmlSerializer(typeof(Config)).Deserialize((TextReader)streamReader);
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show("Failed to load Party AI Overhaul and Commands config, using default values due to: " + ex.FlattenException());
                return new Config();
            }
        }
    }
}
