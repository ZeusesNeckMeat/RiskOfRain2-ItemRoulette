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
        private readonly HookStateTracker _hookStateTracker;

        public Run(ManualLogSource logger, ConfigSettings configSettings, CustomDropTable customDropTable, HookStateTracker hookStateTracker)
        {
            _logger = logger;
            _configSettings = configSettings;
            _customDropTable = customDropTable;
            _hookStateTracker = hookStateTracker;
        }

        public void OnBuildDropTable(Hook.Run.orig_BuildDropTable orig, RoR2Run self)
        {
            //clear default loaded lists first
            _logger.LogInfo("Building full drop table before changing items");
            orig(self);

            _hookStateTracker.IsBuildDropTableDone = true;

            if (_hookStateTracker.IsBuildDropTableDone && _hookStateTracker.IsMonsterGenerateAvailableItemsSetDone)
                _customDropTable.BuildDropTable();
        }

        public void OnBeginStage(Hook.Run.orig_BeginStage orig, RoR2Run self)
        {
            orig(self);

            if (RoR2Run.instance.stageClearCount == 0)
                return;

            var newLoopStarted = RoR2Run.instance.loopClearCount > _hookStateTracker.CurrentLoopCount;
            if (newLoopStarted)
                _hookStateTracker.CurrentLoopCount = RoR2Run.instance.loopClearCount;

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
            _hookStateTracker.IsItemCatalogInitDone = false;
            _hookStateTracker.IsPickupCatalogInitDone = false;
            _hookStateTracker.IsMonsterGenerateAvailableItemsSetDone = false;
            _hookStateTracker.IsBuildDropTableDone = false;
            _hookStateTracker.CurrentLoopCount = 0;

            _configSettings.RefreshConfigSettings();

            orig(self);
        }

        public void OnGenerateAvailableItemsSet(MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig)
        {
            orig();
            _hookStateTracker.IsMonsterGenerateAvailableItemsSetDone = true;

            if (_hookStateTracker.IsBuildDropTableDone && _hookStateTracker.IsMonsterGenerateAvailableItemsSetDone)
                _customDropTable.BuildDropTable();
        }
    }
}