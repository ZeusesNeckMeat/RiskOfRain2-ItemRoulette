using BepInEx.Logging;
using ItemRoulette.Configs;
using On.RoR2.Artifacts;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

using Hook = On.RoR2;
using IRHook = ItemRoulette.Hooks;

namespace ItemRoulette
{
    internal class CustomDropTable
    {
        private readonly ManualLogSource _logger;
        private readonly ConfigSettings _configSettings;
        private readonly IRHook.HookStateTracker _hookStateTracker;
        private List<IItemsInTier> _itemsInTiers = new List<IItemsInTier>();

        private readonly IDictionary<ItemTier, List<PickupIndex>> _allUnlockedItemsByDefault = GetDictionary();
        private readonly IDictionary<ItemTier, List<PickupIndex>> _itemsInRun = GetDictionary();

        private static IDictionary<ItemTier, List<PickupIndex>> GetDictionary() => new Dictionary<ItemTier, List<PickupIndex>>
        {
            [ItemTier.Tier1] = new List<PickupIndex>(),
            [ItemTier.Tier2] = new List<PickupIndex>(),
            [ItemTier.Tier3] = new List<PickupIndex>(),
            [ItemTier.Boss] = new List<PickupIndex>(),
            [ItemTier.Lunar] = new List<PickupIndex>(),
            [ItemTier.VoidTier1] = new List<PickupIndex>(),
            [ItemTier.VoidTier2] = new List<PickupIndex>(),
            [ItemTier.VoidTier3] = new List<PickupIndex>(),
            [ItemTier.VoidBoss] = new List<PickupIndex>()
        };

        public CustomDropTable(ManualLogSource logger, ConfigSettings configSettings, IRHook.HookStateTracker hookStateTracker)
        {
            _logger = logger;
            _configSettings = configSettings;
            _hookStateTracker = hookStateTracker;
        }

        private void SetItemsInRun(Run run, bool isSyncVoidItems)
        {
            run.availableTier1DropList.Clear();
            run.availableTier2DropList.Clear();
            run.availableTier3DropList.Clear();
            run.availableBossDropList.Clear();
            run.availableLunarItemDropList.Clear();
            run.availableTier1DropList.AddRange(_itemsInRun[ItemTier.Tier1]);
            run.availableTier2DropList.AddRange(_itemsInRun[ItemTier.Tier2]);
            run.availableTier3DropList.AddRange(_itemsInRun[ItemTier.Tier3]);
            run.availableBossDropList.AddRange(_itemsInRun[ItemTier.Boss]);
            run.availableLunarItemDropList.AddRange(_itemsInRun[ItemTier.Lunar]);

            if (!isSyncVoidItems)
                return;

            run.availableVoidTier1DropList.Clear();
            run.availableVoidTier2DropList.Clear();
            run.availableVoidTier3DropList.Clear();
            run.availableVoidBossDropList.Clear();
            run.availableVoidTier1DropList.AddRange(_itemsInRun[ItemTier.VoidTier1]);
            run.availableVoidTier2DropList.AddRange(_itemsInRun[ItemTier.VoidTier2]);
            run.availableVoidTier3DropList.AddRange(_itemsInRun[ItemTier.VoidTier3]);
            run.availableVoidBossDropList.AddRange(_itemsInRun[ItemTier.VoidBoss]);
        }

        private void SetAllUnlockedItemsByDefault(Run run)
        {
            var allItemDefs = run.availableItems.ToList().Select(ItemInfos.GetItemDef);
            foreach (var tier in _allUnlockedItemsByDefault.Keys.ToList())
                _allUnlockedItemsByDefault[tier] = allItemDefs.Where(x => x.tier == tier).Select(x => ItemInfos.GetPickupDef(x.itemIndex).pickupIndex).ToList();
        }

        public void BuildDropTable(Run run)
        {
            if (!_configSettings.GeneralSettings.IsModEnabled)
            {
                _logger.LogInfo("Mod disabled. Not altering drop tables.");
                return;
            }

            if (_hookStateTracker.IsBuildDropTableDone)
            {
                SetItemsInRun(run, _configSettings.GeneralSettings.ShouldSyncVoidItems);
                return;
            }

            SetAllUnlockedItemsByDefault(run);
            var tier1Through3Items = _allUnlockedItemsByDefault[ItemTier.Tier1].Concat(_allUnlockedItemsByDefault[ItemTier.Tier2])
                                                                               .Concat(_allUnlockedItemsByDefault[ItemTier.Tier3])
                                                                               .ToList();

            _logger.LogInfo($"NumberOfItemsInTiers1Through3: {tier1Through3Items.Count}");
            if (!tier1Through3Items.Any())
            {
                _logger.LogInfo("No items have been generated yet");
                return;
            }

            _logger.LogInfo("Loading default items for Boss and Lunar tiers");

            _itemsInTiers.ForEach(item => item = null);
            _itemsInTiers.Clear();
            var itemsInTiersDefault = GetDefaultLoadedItemsInTiers();

            _logger.LogInfo("Loading initial information for AllItemsByType");
            var allItemsByTag = AllItemsByTag.Build(_logger)
                                             .UsingItemsForTags(tier1Through3Items)
                                             .HavingTotalItemCount(_configSettings.ItemTierCountSettings.TotalItemCountForTiers123)
                                             .WithTotalAllowedDamageItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfDamageItems)
                                             .WithTotalAllowedHealingItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfHealingItems)
                                             .WithTotalAllowedUtilityItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfUtilityItems)
                                             .WithItemsInTiers(itemsInTiersDefault)
                                             .Create();

            _logger.LogInfo("Getting items for run in Tiers through AllItemsByType");
            _itemsInTiers = allItemsByTag.GetItemPoolForTiers();

            _logger.LogInfo("Overwriting instance items with list of allowed items");

            foreach (var (itemTier, shouldUseOnlyGuaranteedItems, guaranteedItems) in _configSettings.GuaranteedItemsSettings.GetGuaranteedItemSettings())
            {
                var itemPoolForTier = _itemsInTiers.First(itemsInTier => itemsInTier.Tier == itemTier);

                _logger.LogInfo($"{itemTier}: Should only use guaranteed: {shouldUseOnlyGuaranteedItems}. Total guaranteed items: {guaranteedItems?.Count()}");
                var itemsToAddToRun = new HashSet<PickupIndex>();
                var hasGuaranteedItems = guaranteedItems != null && guaranteedItems.Any();

                if (!hasGuaranteedItems || !shouldUseOnlyGuaranteedItems)
                {
                    foreach (var item in itemPoolForTier.ItemsAllowed)
                        itemsToAddToRun.Add(item);
                }

                if (hasGuaranteedItems)
                {
                    foreach (var item in guaranteedItems)
                        itemsToAddToRun.Add(item);
                }

                _itemsInRun[itemTier].Clear();
                _itemsInRun[itemTier].AddRange(itemsToAddToRun);
            }

            _hookStateTracker.IsBuildDropTableDone = true;

            if (!_configSettings.GeneralSettings.ShouldSyncVoidItems)
            {
                SetItemsInRun(run, _configSettings.GeneralSettings.ShouldSyncVoidItems);
                return;
            }

            var voidItemPairs = ItemCatalog.GetItemPairsForRelationship(DLC1Content.ItemRelationshipTypes.ContagiousItem);

            foreach (var voidItemPair in voidItemPairs)
            {
                _logger.LogInfo($"VoidItemLog | Item: {voidItemPair.itemDef1.name} | VoidItem: {voidItemPair.itemDef2.name}");
            }

            var voidItemsToAdd = new List<(ItemDef Item, ItemDef VoidItem)>();
            foreach (var pickup in _itemsInTiers.SelectMany(x => x.ItemsAllowed))
            {
                var itemDef = ItemInfos.GetItemDef(pickup);

                if (!voidItemPairs.Any(x => x.itemDef1.itemIndex == itemDef.itemIndex))
                    continue;

                var pairedItemAndVoidItem = voidItemPairs.First(x => x.itemDef1.itemIndex == itemDef.itemIndex);
                voidItemsToAdd.Add((pairedItemAndVoidItem.itemDef1, pairedItemAndVoidItem.itemDef2));
            }

            _logger.LogInfo($"VoidItemsToLoad Tiers: {string.Join(", ", voidItemsToAdd.Select(x => x.VoidItem.tier.ToString()).Distinct())}");

            foreach (var itemsInTier in _itemsInTiers)
            {
                if (itemsInTier.Tier == ItemTier.Lunar)
                    continue;

                var voidItemsForTier = voidItemsToAdd.Where(x => x.VoidItem.tier.ToString().ToUpper().Contains(itemsInTier.Tier.ToString().ToUpper()))
                                                     .Select(x => x.VoidItem)
                                                     .ToList();

                if (!voidItemsForTier.Any())
                    voidItemsForTier.Add(GetRandomVoidItem(voidItemPairs, itemsInTier));

                var voidTier = voidItemsForTier.First().tier;
                _itemsInRun[voidTier].Clear();
                _itemsInRun[voidTier].AddRange(voidItemsForTier.Select(x => ItemInfos.GetPickupDef(x.itemIndex).pickupIndex));
            }

            SetItemsInRun(run, _configSettings.GeneralSettings.ShouldSyncVoidItems);
        }

        private ItemDef GetRandomVoidItem(HG.ReadOnlyArray<ItemDef.Pair> voidItemPairs, IItemsInTier itemsInTier)
        {
            var voidItemPairsForTier = voidItemPairs.Where(x => x.itemDef1.tier == itemsInTier.Tier).ToList();
            var randomVoidItemPairsForTier = voidItemPairsForTier.OrderBy(_ => Guid.NewGuid()).ToList();
            var firstItemInRandomList = randomVoidItemPairsForTier.FirstOrDefault();
            return firstItemInRandomList.itemDef2;
        }

        internal void GenerateWeightedSelectionArena(Hook.ArenaMonsterItemDropTable.orig_GenerateWeightedSelection orig, ArenaMonsterItemDropTable self, Run run)
        {
            AllowMonsterItemsToGenerate(run, () => orig(self, run));
        }

        public void AllowMonsterDrops(MonsterTeamGainsItemsArtifactManager.orig_OnRunStartGlobal orig, Run self)
        {
            AllowMonsterItemsToGenerate(self, () => orig(self));
        }

        private void AllowMonsterItemsToGenerate(Run run, Action action)
        {
            var tier1Holder = run.availableTier1DropList.ToList();
            var tier2Holder = run.availableTier2DropList.ToList();
            var tier3Holder = run.availableTier3DropList.ToList();

            run.availableTier1DropList.Clear();
            run.availableTier2DropList.Clear();
            run.availableTier3DropList.Clear();

            run.availableTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier1]);
            run.availableTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier2]);
            run.availableTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier3]);

            action();

            run.availableTier1DropList.Clear();
            run.availableTier2DropList.Clear();
            run.availableTier3DropList.Clear();

            run.availableTier1DropList.AddRange(tier1Holder);
            run.availableTier2DropList.AddRange(tier2Holder);
            run.availableTier3DropList.AddRange(tier3Holder);
        }

        public void ResetInstanceDropLists(Run self)
        {
            self.availableTier1DropList.Clear();
            self.availableTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier1]);

            self.availableTier2DropList.Clear();
            self.availableTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier2]);

            self.availableTier3DropList.Clear();
            self.availableTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier3]);

            self.availableBossDropList.Clear();
            self.availableBossDropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Boss]);

            self.availableLunarItemDropList.Clear();
            self.availableLunarItemDropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Lunar]);

            self.availableVoidTier1DropList.Clear();
            self.availableVoidTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.VoidTier1]);

            self.availableVoidTier2DropList.Clear();
            self.availableVoidTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.VoidTier2]);

            self.availableVoidTier3DropList.Clear();
            self.availableVoidTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.VoidTier3]);

            self.availableVoidBossDropList.Clear();
            self.availableVoidBossDropList.AddRange(_allUnlockedItemsByDefault[ItemTier.VoidBoss]);
        }

        private List<IItemsInTier> GetDefaultLoadedItemsInTiers()
        {
            var itemsInTier1 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier1)
                                           .HavingDefaultItems(_allUnlockedItemsByDefault[ItemTier.Tier1])
                                           .HavingDefaultVoidItems(_allUnlockedItemsByDefault[ItemTier.VoidTier1])
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier1ItemCount)
                                           .Create();

            var itemsInTier2 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier2)
                                           .HavingDefaultItems(_allUnlockedItemsByDefault[ItemTier.Tier2])
                                           .HavingDefaultVoidItems(_allUnlockedItemsByDefault[ItemTier.VoidTier2])
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier2ItemCount)
                                           .Create();

            var itemsInTier3 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier3)
                                           .HavingDefaultItems(_allUnlockedItemsByDefault[ItemTier.Tier3])
                                           .HavingDefaultVoidItems(_allUnlockedItemsByDefault[ItemTier.VoidTier3])
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier3ItemCount)
                                           .Create();

            var itemsInBoss = ItemsInTiers.Build(_logger)
                                          .AsTier(ItemTier.Boss)
                                          .HavingDefaultItems(_allUnlockedItemsByDefault[ItemTier.Boss])
                                          .HavingDefaultVoidItems(_allUnlockedItemsByDefault[ItemTier.VoidBoss])
                                          .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.BossItemCount)
                                          .Create();

            var itemsInLunar = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Lunar)
                                           .HavingDefaultItems(_allUnlockedItemsByDefault[ItemTier.Lunar])
                                           .HavingNoVoidItems()
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.LunarItemCount)
                                           .Create();

            return new List<IItemsInTier> { itemsInTier1, itemsInTier2, itemsInTier3, itemsInBoss, itemsInLunar };
        }
    }
}