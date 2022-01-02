using BepInEx;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hook = On.RoR2;

namespace ItemRoulette
{
    [R2APISubmoduleDependency("ItemAPI")]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("Risk of Rain 2.exe")]
    [BepInPlugin("com.zeusesneckmeat.itemroulette", "ItemRoulette", "2.0.0.0")]
    public class ItemRoulette : BaseUnityPlugin
    {
        private readonly IDictionary<ItemTier, List<PickupIndex>> _allUnlockedItemsByDefault = new Dictionary<ItemTier, List<PickupIndex>>();
        private List<IItemsInTier> _itemsInTiers = new List<IItemsInTier>();
        private ConfigSettings _configSettings;
        private int _currentLoopCount = 0;

        public void Awake()
        {
            Hook.Run.BuildDropTable += OnBuildDropTable;
            Hook.Run.BeginStage += OnBeginStage;
            Hook.Console.Awake += (orig, self) => orig(self);

            _configSettings = new ConfigSettings(Config);
            _configSettings.InitializeConfigFile();

            Logger.LogInfo($"ItemRoulette loaded successfully. Mod Enabled: {_configSettings.IsModEnabled}");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _configSettings.InitializeConfigFile(true);
                Chat.AddMessage($"ItemRoulette reloaded. Mod is enabled: {_configSettings.IsModEnabled}.");
            }

            if (!Run.instance)
                return;

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LogItemsInDropTable(ItemTier.Tier1, Run.instance.availableTier1DropList, _configSettings.Tier1ItemCount);
                LogItemsInDropTable(ItemTier.Tier2, Run.instance.availableTier2DropList, _configSettings.Tier2ItemCount);
                LogItemsInDropTable(ItemTier.Tier3, Run.instance.availableTier3DropList, _configSettings.Tier3ItemCount);
                LogItemsInDropTable(ItemTier.Boss, Run.instance.availableBossDropList, _configSettings.BossItemCount);
                LogItemsInDropTable(ItemTier.Lunar, Run.instance.availableLunarDropList, _configSettings.LunarItemCount);
            }
        }

        private void OnBeginStage(Hook.Run.orig_BeginStage orig, Run self)
        {
            orig(self);

            if (Run.instance.stageClearCount == 0)
                return;

            var newLoopStarted = Run.instance.loopClearCount > _currentLoopCount;
            if (newLoopStarted)
                _currentLoopCount = Run.instance.loopClearCount;


            var shouldRefreshItemPool = _configSettings.ItemRefreshOptions == ItemRefreshOptions.EachStage
                                        || (_configSettings.ItemRefreshOptions == ItemRefreshOptions.EachLoop && newLoopStarted);

            if (!shouldRefreshItemPool)
                return;

            ResetInstanceDropLists();

            BuildDropTable();
        }

        private void ResetInstanceDropLists()
        {
            Run.instance.availableTier1DropList.Clear();
            Run.instance.availableTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier1]);

            Run.instance.availableTier2DropList.Clear();
            Run.instance.availableTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier2]);

            Run.instance.availableTier3DropList.Clear();
            Run.instance.availableTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier3]);

            Run.instance.availableBossDropList.Clear();
            Run.instance.availableBossDropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Boss]);

            Run.instance.availableLunarDropList.Clear();
            Run.instance.availableLunarDropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Lunar]);
        }

        private void OnBuildDropTable(Hook.Run.orig_BuildDropTable orig, Run self)
        {
            //clear default loaded lists first
            Logger.LogInfo("Building full drop table before changing items");
            orig(self);

            if (!_configSettings.IsModEnabled)
            {
                Logger.LogInfo("Mod disabled. Not altering drop tables.");
                return;
            }

            _allUnlockedItemsByDefault[ItemTier.Tier1] = Run.instance.availableTier1DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Tier2] = Run.instance.availableTier2DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Tier3] = Run.instance.availableTier3DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Boss] = Run.instance.availableBossDropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Lunar] = Run.instance.availableLunarDropList.ToList();

            BuildDropTable();
        }

        private void BuildDropTable() 
        {
            var tier1Through3Items = Run.instance.availableTier1DropList.Concat(Run.instance.availableTier2DropList)
                                                                        .Concat(Run.instance.availableTier3DropList)
                                                                        .ToList();

            Logger.LogInfo("Loading default items for Boss and Lunar tiers");
            LoadDefaultItemsInTiers();

            Logger.LogInfo("Loading initial information for AllItemsByType");
            var allItemsByTag = AllItemsByTag.Build(Logger)
                                             .UsingItemsForTags(tier1Through3Items)
                                             .HavingTotalItemCount(_configSettings.TotalItemCountForTiers123)
                                             .WithTotalAllowedDamageItemCount(_configSettings.PercentageOfDamageItems)
                                             .WithTotalAllowedHealingItemCount(_configSettings.PercentageOfHealingItems)
                                             .WithTotalAllowedUtilityItemCount(_configSettings.PercentageOfUtilityItems)
                                             .WithItemsInTiers(_itemsInTiers)
                                             .Create();

            Logger.LogInfo("Getting items for run in Tiers through AllItemsByType");
            _itemsInTiers = allItemsByTag.GetItemPoolForTiers();

            Logger.LogInfo("Overwriting instance items with list of allowed items for Boss and Lunar tiers");
            foreach (var itemPoolForTier in _itemsInTiers)
                itemPoolForTier.SetItemsInInstance();
        }

        private void LoadDefaultItemsInTiers()
        {
            var itemsInTier1 = ItemsInTiers.Build(Logger)
                                           .AsTier(ItemTier.Tier1)
                                           .HavingDefaultItems(Run.instance.availableTier1DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier1ItemCount)
                                           .Create();

            var itemsInTier2 = ItemsInTiers.Build(Logger)
                                           .AsTier(ItemTier.Tier2)
                                           .HavingDefaultItems(Run.instance.availableTier2DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier2ItemCount)
                                           .Create();

            var itemsInTier3 = ItemsInTiers.Build(Logger)
                                           .AsTier(ItemTier.Tier3)
                                           .HavingDefaultItems(Run.instance.availableTier3DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier3ItemCount)
                                           .Create();

            var itemsInBoss = ItemsInTiers.Build(Logger)
                                          .AsTier(ItemTier.Boss)
                                          .HavingDefaultItems(Run.instance.availableBossDropList)
                                          .WithMaxItemsAllowed(_configSettings.BossItemCount)
                                          .Create();

            var itemsInLunar = ItemsInTiers.Build(Logger)
                                           .AsTier(ItemTier.Lunar)
                                           .HavingDefaultItems(Run.instance.availableLunarDropList)
                                           .WithMaxItemsAllowed(_configSettings.LunarItemCount)
                                           .Create();

            _itemsInTiers.ForEach(item => item = null);
            _itemsInTiers.Clear();
            _itemsInTiers.Add(itemsInTier1);
            _itemsInTiers.Add(itemsInTier2);
            _itemsInTiers.Add(itemsInTier3);
            _itemsInTiers.Add(itemsInBoss);
            _itemsInTiers.Add(itemsInLunar);
        }

        private void LogItemsInDropTable(ItemTier itemTier, List<PickupIndex> items, int configValue)
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