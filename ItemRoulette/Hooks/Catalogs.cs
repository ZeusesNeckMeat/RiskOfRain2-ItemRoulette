using ItemRoulette.Configs;
using RoR2;

namespace ItemRoulette.Hooks
{
    internal class Catalogs
    {
        private static ConfigSettings _configSettings;
        private static HookStateTracker _hookStateTracker;

        public Catalogs(ConfigSettings configSettings, HookStateTracker hookStateTracker)
        {
            _configSettings = configSettings;
            _hookStateTracker = hookStateTracker;
        }

        [SystemInitializer(typeof(ItemCatalog), typeof(PickupCatalog))]
        public static void GenerateItemLists()
        {
            ItemInfos.GenerateItemLists();
            _configSettings.InitializeConfigFile();
        }
    }
}