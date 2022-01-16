using BepInEx.Configuration;

namespace ItemRoulette.Configs
{
    internal abstract class ConfigBase
    {
        private readonly ConfigFile _config;

        public ConfigBase(ConfigFile config)
        {
            _config = config;
        }

        public abstract string SectionName { get; }
        public virtual string SectionDescription => "{0}";

        public abstract void Initialize();

        public ConfigEntry<T> Bind<T>(string key, T defaultValue, string descriptionExtraThing)
        {
            return Bind(SectionName, key, defaultValue, string.Format(SectionDescription, descriptionExtraThing));
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return _config.Bind(section, key, defaultValue, description);
        }

        public ConfigEntry<T> Bind<T>(string key, T defaultValue, (string descriptionExtraThing, AcceptableValueBase acceptableValues) configDescription)
        {
            return _config.Bind(SectionName, key, defaultValue, GetConfigDescription(string.Format(SectionDescription, configDescription.descriptionExtraThing), configDescription.acceptableValues));
        }

        public void Reload()
        {
            _config.Reload();
        }

        private ConfigDescription GetConfigDescription(string description, AcceptableValueBase acceptableValues = null)
        {
            return new ConfigDescription(description, acceptableValues);
        }
    }
}