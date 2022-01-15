using BepInEx;
using On.RoR2.Artifacts;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

using CustomHooks = ItemRoulette.Hooks;
using Hook = On.RoR2;

namespace ItemRoulette
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("Risk of Rain 2.exe")]
    [BepInPlugin("com.zeusesneckmeat.itemroulette", "ItemRoulette", "2.1.0.0")]
    public class ItemRoulette : BaseUnityPlugin
    {
        private ConfigSettings _configSettings;

        private CustomHooks.Run _run;

        public void Awake()
        {
            _configSettings = new ConfigSettings(Config, Logger);

            var customDropTable = new CustomDropTable(Logger, _configSettings);
            _run = new CustomHooks.Run(Logger, _configSettings, customDropTable);

            Hook.ArenaMissionController.OnStartServer += customDropTable.OnArenaStartServer;
            Hook.Console.Awake += (orig, self) => orig(self);
            Hook.ItemCatalog.Init += _run.OnItemCatalogInit;
            Hook.PickupCatalog.Init += _run.OnPickupCatalogInit;
            Hook.Run.BeginStage += _run.OnBeginStage;
            Hook.Run.BuildDropTable += _run.OnBuildDropTable;
            Hook.Run.OnDestroy += _run.OnRunDestroy;

            MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += _run.OnGenerateAvailableItemsSet;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                _configSettings.RefreshConfigSettings();

            if (!Run.instance)
                return;

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LogItemsInDropTable(ItemTier.Tier1, Run.instance.availableTier1DropList);
                LogItemsInDropTable(ItemTier.Tier2, Run.instance.availableTier2DropList);
                LogItemsInDropTable(ItemTier.Tier3, Run.instance.availableTier3DropList);
                LogItemsInDropTable(ItemTier.Boss, Run.instance.availableBossDropList);
                LogItemsInDropTable(ItemTier.Lunar, Run.instance.availableLunarDropList);
            }
        }

        private void LogItemsInDropTable(ItemTier itemTier, List<PickupIndex> items)
        {
            Logger.LogInfo($"================================{itemTier}============================");
            Logger.LogInfo($"{itemTier} count: {items.Count}");
            foreach (var item in items)
            {
                var pickup = PickupCatalog.GetPickupDef(item);
                var displayName = Language.GetString(pickup.nameToken);
                Logger.LogInfo($"{itemTier}: {displayName}");
            }
            Logger.LogInfo($"================================{itemTier}============================");
        }
    }
}