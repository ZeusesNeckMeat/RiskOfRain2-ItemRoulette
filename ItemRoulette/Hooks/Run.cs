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

        public void OnBeginStage(Hook.Run.orig_BeginStage orig, RoR2Run self)
        {
            orig(self);

            if (self.stageClearCount == 0)
                return;

            var newLoopStarted = self.loopClearCount > _hookStateTracker.CurrentLoopCount;
            if (newLoopStarted)
                _hookStateTracker.CurrentLoopCount = self.loopClearCount;

            var shouldRefreshItemPool = _configSettings.GeneralSettings.ItemRefreshOptions == ItemRefreshOptions.EachStage
                                        || (_configSettings.GeneralSettings.ItemRefreshOptions == ItemRefreshOptions.EachLoop && newLoopStarted);

            if (!shouldRefreshItemPool)
                return;

            _customDropTable.ResetInstanceDropLists(self);
            _customDropTable.BuildDropTable(self);
        }

        internal void GenerateWeightedSelection(Hook.BasicPickupDropTable.orig_GenerateWeightedSelection orig, RoR2.BasicPickupDropTable self, RoR2Run run)
        {
            if (!_hookStateTracker.IsBuildDropTableDone)
            {
                _customDropTable.BuildDropTable(run);
                _logger.LogInfo("Building full drop table before changing items");
            }

            orig(self, run);
            _hookStateTracker.IsBuildDropTableDone = true;
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

        public void OnGenerateAvailableItemsSet(MonsterTeamGainsItemsArtifactManager.orig_OnRunStartGlobal orig, RoR2Run self)
        {
            _customDropTable.AllowMonsterDrops(orig, self);
        }
    }
}