using BepInEx.Logging;
using ItemRoulette.Configs;
using On.RoR2.Artifacts;

using Hook = On.RoR2;
using RoR2Run = RoR2.Run;

namespace ItemRoulette.Hooks
{
    internal class Run
    {
        private readonly ManualLogSource _logger;
        private readonly ConfigSettings _configSettings;
        private readonly CustomDropTable _customDropTable;

        private bool _isBuildDropTableDone = false;
        private bool _isMonsterGenerateAvailableItemsSetDone = false;
        private bool _isItemCatalogInitDone = false;
        private bool _isPickupCatalogInitDone = false;

        private int _currentLoopCount = 0;

        public Run(ManualLogSource logger, ConfigSettings configSettings, CustomDropTable customDropTable)
        {
            _logger = logger;
            _configSettings = configSettings;
            _customDropTable = customDropTable;
        }

        public void OnBuildDropTable(Hook.Run.orig_BuildDropTable orig, RoR2Run self)
        {
            //clear default loaded lists first
            _logger.LogInfo("Building full drop table before changing items");
            orig(self);

            _isBuildDropTableDone = true;

            if (_isBuildDropTableDone && _isMonsterGenerateAvailableItemsSetDone)
                _customDropTable.BuildDropTable();
        }

        public void OnBeginStage(Hook.Run.orig_BeginStage orig, RoR2Run self)
        {
            orig(self);

            if (RoR2Run.instance.stageClearCount == 0)
                return;

            var newLoopStarted = RoR2Run.instance.loopClearCount > _currentLoopCount;
            if (newLoopStarted)
                _currentLoopCount = RoR2Run.instance.loopClearCount;

            var shouldRefreshItemPool = _configSettings.GeneralSettings.ItemRefreshOptions == ItemRefreshOptions.EachStage
                                        || (_configSettings.GeneralSettings.ItemRefreshOptions == ItemRefreshOptions.EachLoop && newLoopStarted);

            if (!shouldRefreshItemPool)
                return;

            _customDropTable.ResetInstanceDropLists();
            _customDropTable.BuildDropTable();
        }

        public void OnRunDestroy(Hook.Run.orig_OnDestroy orig, RoR2Run self)
        {
            _logger.LogInfo("Run Over. Cleaning up.");
            _isItemCatalogInitDone = false;
            _isPickupCatalogInitDone = false;
            _isMonsterGenerateAvailableItemsSetDone = false;
            _isBuildDropTableDone = false;
            _currentLoopCount = 0;

            _configSettings.RefreshConfigSettings();

            orig(self);
        }

        public void OnGenerateAvailableItemsSet(MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig)
        {
            orig();
            _isMonsterGenerateAvailableItemsSetDone = true;

            if (_isBuildDropTableDone && _isMonsterGenerateAvailableItemsSetDone)
                _customDropTable.BuildDropTable();
        }

        public void OnItemCatalogInit(Hook.ItemCatalog.orig_Init orig)
        {
            orig();
            _isItemCatalogInitDone = true;

            if (_isItemCatalogInitDone && _isPickupCatalogInitDone)
                GenerateItemLists();
        }

        public void OnPickupCatalogInit(Hook.PickupCatalog.orig_Init orig)
        {
            orig();
            _isPickupCatalogInitDone = true;

            if (_isItemCatalogInitDone && _isPickupCatalogInitDone)
                GenerateItemLists();
        }

        private void GenerateItemLists()
        {
            ItemInfos.GenerateItemLists();
            _configSettings.InitializeConfigFile();
        }
    }
}