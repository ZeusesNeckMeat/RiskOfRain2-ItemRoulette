using BepInEx.Logging;
using ItemRoulette.Configs;
using On.RoR2.Artifacts;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

using Hook = On.RoR2;

namespace ItemRoulette
{
    internal class CustomDropTable
    {
        private readonly ManualLogSource _logger;
        private readonly ConfigSettings _configSettings;
        private List<IItemsInTier> _itemsInTiers = new List<IItemsInTier>();

        private readonly IDictionary<ItemTier, List<PickupIndex>> _allUnlockedItemsByDefault = new Dictionary<ItemTier, List<PickupIndex>>();

        public CustomDropTable(ManualLogSource logger, ConfigSettings configSettings)
        {
            _logger = logger;
            _configSettings = configSettings;
        }

        public void BuildDropTable(Run run)
        {
            if (!_configSettings.GeneralSettings.IsModEnabled)
            {
                _logger.LogInfo("Mod disabled. Not altering drop tables.");
                return;
            }

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.Tier1, out var tier1Items) && (tier1Items == null || !tier1Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.Tier1] = run.availableTier1DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.Tier2, out var tier2Items) && (tier2Items == null || !tier2Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.Tier2] = run.availableTier2DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.Tier3, out var tier3Items) && (tier3Items == null || !tier3Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.Tier3] = run.availableTier3DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.Boss, out var bossItems) && (bossItems == null || !bossItems.Any()))
                _allUnlockedItemsByDefault[ItemTier.Boss] = run.availableBossDropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.Lunar, out var lunarItems) && (lunarItems == null || !lunarItems.Any()))
                _allUnlockedItemsByDefault[ItemTier.Lunar] = run.availableLunarItemDropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.VoidTier1, out var voidTier1Items) && (voidTier1Items == null || !voidTier1Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.VoidTier1] = run.availableVoidTier1DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.VoidTier2, out var voidTier2Items) && (voidTier2Items == null || !voidTier2Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.VoidTier2] = run.availableVoidTier2DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.VoidTier3, out var voidTier3Items) && (voidTier3Items == null || !voidTier3Items.Any()))
                _allUnlockedItemsByDefault[ItemTier.VoidTier3] = run.availableVoidTier3DropList.ToList();

            if (!_allUnlockedItemsByDefault.TryGetValue(ItemTier.VoidBoss, out var voidBossItems) && (voidBossItems == null || !voidBossItems.Any()))
                _allUnlockedItemsByDefault[ItemTier.VoidBoss] = run.availableVoidBossDropList.ToList();

            var tier1Through3Items = run.availableTier1DropList.Concat(run.availableTier2DropList)
                                                               .Concat(run.availableTier3DropList)
                                                               .ToList();

            _logger.LogInfo("Loading default items for Boss and Lunar tiers");

            _itemsInTiers.ForEach(item => item = null);
            _itemsInTiers.Clear();
            var itemsInTiersDefault = LoadDefaultItemsInTiers(run);

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

            var guaranteedItemsSettings = _configSettings.GuaranteedItemsSettings.GetGuaranteedItemSettings();
            foreach (var (itemTier, shouldUseOnlyGuaranteedItems, guaranteedItems) in guaranteedItemsSettings)
            {
                var itemPoolForTier = _itemsInTiers.First(itemsInTier => itemsInTier.Tier == itemTier);

                _logger.LogInfo($"{itemTier}: Should only use guaranteed: {shouldUseOnlyGuaranteedItems}. Total guaranteed items: {guaranteedItems?.Count()}");
                if (guaranteedItems == null || !guaranteedItems.Any())
                {
                    itemPoolForTier.SetItemsInInstance();
                    continue;
                }

                if (shouldUseOnlyGuaranteedItems)
                    itemPoolForTier.SetItemsInInstance(guaranteedItems.ToList());
                else
                {
                    itemPoolForTier.SetItemsInInstance();
                    itemPoolForTier.ItemsInInstance.AddRange(guaranteedItems);
                }
            }

            if (!_configSettings.GeneralSettings.ShouldSyncVoidItems)
                return;

            var voidItemPairs = ItemCatalog.GetItemPairsForRelationship(DLC1Content.ItemRelationshipTypes.ContagiousItem);

            foreach (var voidItemPair in voidItemPairs)
            {
                _logger.LogInfo($"VoidItemLog | Item: {voidItemPair.itemDef1.name} | VoidItem: {voidItemPair.itemDef2.name}");
            }

            var voidItemsToAdd = new List<ItemDef>();
            foreach (var pickup in _itemsInTiers.SelectMany(x => x.ItemsInInstance))
            {
                var itemDef = ItemInfos.GetItemDef(pickup);

                if (!voidItemPairs.Any(x => x.itemDef1.itemIndex == itemDef.itemIndex))
                    continue;

                voidItemsToAdd.Add(voidItemPairs.First(x => x.itemDef1.itemIndex == itemDef.itemIndex).itemDef2);
            }

            _logger.LogInfo($"VoidItemsToLoad Tiers: {string.Join(", ", voidItemsToAdd.Select(x => x.tier.ToString()).Distinct())}");

            foreach (var itemsInTier in _itemsInTiers)
            {
                if (itemsInTier.Tier == ItemTier.Lunar)
                    continue;

                var voidItemsForTier = voidItemsToAdd.Where(x => x.tier.ToString().ToUpper().Contains(itemsInTier.Tier.ToString().ToUpper()))
                                                     .Select(x => ItemInfos.GetPickupDef(x.itemIndex).pickupIndex)
                                                     .ToList();

                if (!voidItemsForTier.Any())
                {
                    var voidItemPairsForTier = voidItemPairs.Where(x => x.itemDef1.tier == itemsInTier.Tier).ToList();
                    var randomVoidItemPairsForTier = voidItemPairsForTier.OrderBy(_ => Guid.NewGuid()).ToList();
                    var firstItemInRandomList = randomVoidItemPairsForTier.FirstOrDefault();
                    var voidItem = firstItemInRandomList.itemDef2.itemIndex;
                    var pickupDef = ItemInfos.GetPickupDef(voidItem);
                    voidItemsForTier.Add(pickupDef.pickupIndex);
                }

                itemsInTier.SetVoidItemsInInstance(voidItemsForTier);
            }
        }

        internal void GenerateWeightedSelectionArena(Hook.ArenaMonsterItemDropTable.orig_GenerateWeightedSelection orig, ArenaMonsterItemDropTable self, Run run)
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

            orig(self, run);

            run.availableTier1DropList.Clear();
            run.availableTier2DropList.Clear();
            run.availableTier3DropList.Clear();

            run.availableTier1DropList.AddRange(tier1Holder);
            run.availableTier2DropList.AddRange(tier2Holder);
            run.availableTier3DropList.AddRange(tier3Holder);
        }

        public void AllowMonsterDrops(MonsterTeamGainsItemsArtifactManager.orig_OnRunStartGlobal orig, Run self)
        {
            var tier1Holder = self.availableTier1DropList.ToList();
            var tier2Holder = self.availableTier2DropList.ToList();
            var tier3Holder = self.availableTier3DropList.ToList();

            self.availableTier1DropList.Clear();
            self.availableTier2DropList.Clear();
            self.availableTier3DropList.Clear();

            self.availableTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier1]);
            self.availableTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier2]);
            self.availableTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier3]);

            orig(self);

            self.availableTier1DropList.Clear();
            self.availableTier2DropList.Clear();
            self.availableTier3DropList.Clear();

            self.availableTier1DropList.AddRange(tier1Holder);
            self.availableTier2DropList.AddRange(tier2Holder);
            self.availableTier3DropList.AddRange(tier3Holder);
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

        private List<IItemsInTier> LoadDefaultItemsInTiers(Run run)
        {
            //might have to update these to require the chest selectors
            var itemsInTier1 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier1)
                                           .HavingDefaultItems(run.availableTier1DropList)
                                           .HavingDefaultVoidItems(run.availableVoidTier1DropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier1ItemCount)
                                           .Create();

            var itemsInTier2 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier2)
                                           .HavingDefaultItems(run.availableTier2DropList)
                                           .HavingDefaultVoidItems(run.availableVoidTier2DropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier2ItemCount)
                                           .Create();

            var itemsInTier3 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier3)
                                           .HavingDefaultItems(run.availableTier3DropList)
                                           .HavingDefaultVoidItems(run.availableVoidTier3DropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier3ItemCount)
                                           .Create();

            var itemsInBoss = ItemsInTiers.Build(_logger)
                                          .AsTier(ItemTier.Boss)
                                          .HavingDefaultItems(run.availableBossDropList)
                                          .HavingDefaultVoidItems(run.availableVoidBossDropList)
                                          .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.BossItemCount)
                                          .Create();

            var itemsInLunar = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Lunar)
                                           .HavingDefaultItems(run.availableLunarItemDropList)
                                           .HavingNoVoidItems()
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.LunarItemCount)
                                           .Create();

            return new List<IItemsInTier> { itemsInTier1, itemsInTier2, itemsInTier3, itemsInBoss, itemsInLunar };
        }
    }
}