using BepInEx;
using ItemRoulette.Configs;
using On.RoR2.Artifacts;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

using CustomHooks = ItemRoulette.Hooks;
using Hook = On.RoR2;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ItemRoulette
{
    [BepInProcess("Risk of Rain 2.exe")]
    [BepInPlugin("com.zeusesneckmeat.itemroulette", "ItemRoulette", "3.0.0")]
    public class ItemRoulette : BaseUnityPlugin
    {
        private ConfigSettings _configSettings;

        public void Awake()
        {
            _configSettings = new ConfigSettings(Config, Logger);

            var hookStateTracker = new CustomHooks.HookStateTracker();
            var customDropTable = new CustomDropTable(Logger, _configSettings, hookStateTracker);
            var run = new CustomHooks.Run(Logger, _configSettings, customDropTable, hookStateTracker);
            var catalogs = new CustomHooks.Catalogs(_configSettings, hookStateTracker);

            Hook.ArenaMonsterItemDropTable.GenerateWeightedSelection += customDropTable.GenerateWeightedSelectionArena;
            Hook.Console.Awake += (orig, self) => orig(self);
            Hook.Run.BeginStage += run.OnBeginStage;
            Hook.BasicPickupDropTable.GenerateWeightedSelection += run.GenerateWeightedSelection;
            Hook.Run.OnDestroy += run.OnRunDestroy;

            MonsterTeamGainsItemsArtifactManager.OnRunStartGlobal += run.OnGenerateAvailableItemsSet;
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
                LogItemsInDropTable(ItemTier.Lunar, Run.instance.availableLunarItemDropList);
                LogItemsInDropTable(ItemTier.VoidTier1, Run.instance.availableVoidTier1DropList);
                LogItemsInDropTable(ItemTier.VoidTier2, Run.instance.availableVoidTier2DropList);
                LogItemsInDropTable(ItemTier.VoidTier3, Run.instance.availableVoidTier3DropList);
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