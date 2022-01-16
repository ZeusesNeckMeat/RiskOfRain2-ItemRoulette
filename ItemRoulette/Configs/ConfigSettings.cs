using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using System.Collections.Generic;

namespace ItemRoulette.Configs
{
    public class ConfigSettings
    {
        private readonly ConfigFile _config;
        private readonly List<ConfigBase> _configs = new List<ConfigBase>();

        public ConfigSettings(ConfigFile config, ManualLogSource logger)
        {
            _config = config;
            GeneralSettings = new General(config);
            ItemTierCountSettings = new ItemTierCount(config);
            ItemTagPercentsSettings = new ItemTagPercents(config);
            GuaranteedItemsSettings = new GuaranteedItems(config, logger);

            _configs.Add(GeneralSettings);
            _configs.Add(ItemTierCountSettings);
            _configs.Add(ItemTagPercentsSettings);
            _configs.Add(GuaranteedItemsSettings);
        }

        internal General GeneralSettings { get; private set; }
        internal ItemTierCount ItemTierCountSettings { get; private set; }
        internal ItemTagPercents ItemTagPercentsSettings { get; private set; }
        internal GuaranteedItems GuaranteedItemsSettings { get; private set; }

        public void InitializeConfigFile(bool shouldReloadConfig = false)
        {
            if (shouldReloadConfig)
                _config.Reload();

            foreach (var config in _configs)
                config.Initialize();
        }

        public void RefreshConfigSettings()
        {
            InitializeConfigFile(true);
            Chat.AddMessage($"ItemRoulette reloaded. Mod is enabled: {GeneralSettings.IsModEnabled}.");
        }
    }
}