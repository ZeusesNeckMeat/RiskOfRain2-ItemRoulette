using ItemRoulette.Configs;

using Hook = On.RoR2;

namespace ItemRoulette.Hooks
{
    internal class Catalogs
    {
        private readonly ConfigSettings _configSettings;
        private readonly HookStateTracker _hookStateTracker;

        public Catalogs(ConfigSettings configSettings, HookStateTracker hookStateTracker)
        {
            _configSettings = configSettings;
            _hookStateTracker = hookStateTracker;
        }

        public void OnItemCatalogInit(Hook.ItemCatalog.orig_Init orig)
        {
            orig();
            _hookStateTracker.IsItemCatalogInitDone = true;

            if (_hookStateTracker.IsItemCatalogInitDone && _hookStateTracker.IsPickupCatalogInitDone)
                GenerateItemLists();
        }

        public void OnPickupCatalogInit(Hook.PickupCatalog.orig_Init orig)
        {
            orig();
            _hookStateTracker.IsPickupCatalogInitDone = true;

            if (_hookStateTracker.IsItemCatalogInitDone && _hookStateTracker.IsPickupCatalogInitDone)
                GenerateItemLists();
        }

        private void GenerateItemLists()
        {
            ItemInfos.GenerateItemLists();
            _configSettings.InitializeConfigFile();
        }
    }
}