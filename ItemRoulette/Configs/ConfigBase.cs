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
        public virtual string SectionKey => "{0}";
        public virtual string SectionDescription => "{0}";

        public abstract void Initialize();

        public ConfigEntry<T> Bind<T>(string keyExtraThing, T defaultValue, string descriptionExtraThing)
        {
            return Bind(SectionName, string.Format(SectionKey, keyExtraThing), defaultValue, string.Format(SectionDescription, descriptionExtraThing));
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return _config.Bind(section, key, defaultValue, description);
        }

        public void Reload()
        {
            _config.Reload();
        }
    }
}