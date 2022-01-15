﻿using BepInEx.Logging;
using RoR2;
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

        public void BuildDropTable()
        {
            if (!_configSettings.IsModEnabled)
            {
                _logger.LogInfo("Mod disabled. Not altering drop tables.");
                return;
            }

            var guaranteedPickupIndices = _configSettings.GetGuarnteedItemsDictionary();

            _allUnlockedItemsByDefault[ItemTier.Tier1] = Run.instance.availableTier1DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Tier2] = Run.instance.availableTier2DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Tier3] = Run.instance.availableTier3DropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Boss] = Run.instance.availableBossDropList.ToList();
            _allUnlockedItemsByDefault[ItemTier.Lunar] = Run.instance.availableLunarDropList.ToList();

            var tier1Through3Items = Run.instance.availableTier1DropList.Concat(Run.instance.availableTier2DropList)
                                                                        .Concat(Run.instance.availableTier3DropList)
                                                                        .ToList();

            _logger.LogInfo("Loading default items for Boss and Lunar tiers");
            LoadDefaultItemsInTiers();

            _logger.LogInfo("Loading initial information for AllItemsByType");
            var allItemsByTag = AllItemsByTag.Build(_logger)
                                             .UsingItemsForTags(tier1Through3Items)
                                             .HavingTotalItemCount(_configSettings.TotalItemCountForTiers123)
                                             .WithTotalAllowedDamageItemCount(_configSettings.PercentageOfDamageItems)
                                             .WithTotalAllowedHealingItemCount(_configSettings.PercentageOfHealingItems)
                                             .WithTotalAllowedUtilityItemCount(_configSettings.PercentageOfUtilityItems)
                                             .WithItemsInTiers(_itemsInTiers)
                                             .Create();

            _logger.LogInfo("Getting items for run in Tiers through AllItemsByType");
            _itemsInTiers = allItemsByTag.GetItemPoolForTiers();

            _logger.LogInfo("Overwriting instance items with list of allowed items");
            foreach (var itemPoolForTier in _itemsInTiers)
            {
                if (!_configSettings.ShouldOnlyUseGuaranteedItems
                    || !guaranteedPickupIndices.ContainsKey(itemPoolForTier.Tier)
                    || !guaranteedPickupIndices[itemPoolForTier.Tier].Any())
                {
                    itemPoolForTier.SetItemsInInstance();
                }

                _logger.LogInfo($"Item count for guaranteed items: {guaranteedPickupIndices.Count()}");
                _logger.LogInfo($"Guaranteed Items: {string.Join(" - ", guaranteedPickupIndices.SelectMany(x => x.Value))}");
                if (guaranteedPickupIndices.Any()
                    && guaranteedPickupIndices.ContainsKey(itemPoolForTier.Tier)
                    && guaranteedPickupIndices[itemPoolForTier.Tier].Any())
                {
                    _logger.LogInfo("Adding guaranteed items to instance");

                    if (_configSettings.ShouldOnlyUseGuaranteedItems)
                        itemPoolForTier.SetItemsInInstance(guaranteedPickupIndices[itemPoolForTier.Tier].ToList());
                    else
                        itemPoolForTier.ItemsInInstance.AddRange(guaranteedPickupIndices[itemPoolForTier.Tier].ToList());
                }
            }
        }

        public void OnArenaStartServer(Hook.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            var tier1Holder = Run.instance.availableTier1DropList.ToList();
            var tier2Holder = Run.instance.availableTier2DropList.ToList();
            var tier3Holder = Run.instance.availableTier3DropList.ToList();

            Run.instance.availableTier1DropList.Clear();
            Run.instance.availableTier2DropList.Clear();
            Run.instance.availableTier3DropList.Clear();

            Run.instance.availableTier1DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier1]);
            Run.instance.availableTier2DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier2]);
            Run.instance.availableTier3DropList.AddRange(_allUnlockedItemsByDefault[ItemTier.Tier3]);

            orig(self);

            Run.instance.availableTier1DropList.Clear();
            Run.instance.availableTier2DropList.Clear();
            Run.instance.availableTier3DropList.Clear();

            Run.instance.availableTier1DropList.AddRange(tier1Holder);
            Run.instance.availableTier2DropList.AddRange(tier2Holder);
            Run.instance.availableTier3DropList.AddRange(tier3Holder);
        }

        public void ResetInstanceDropLists()
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

        private void LoadDefaultItemsInTiers()
        {
            var itemsInTier1 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier1)
                                           .HavingDefaultItems(Run.instance.availableTier1DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier1ItemCount)
                                           .Create();

            var itemsInTier2 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier2)
                                           .HavingDefaultItems(Run.instance.availableTier2DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier2ItemCount)
                                           .Create();

            var itemsInTier3 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier3)
                                           .HavingDefaultItems(Run.instance.availableTier3DropList)
                                           .WithMaxItemsAllowed(_configSettings.Tier3ItemCount)
                                           .Create();

            var itemsInBoss = ItemsInTiers.Build(_logger)
                                          .AsTier(ItemTier.Boss)
                                          .HavingDefaultItems(Run.instance.availableBossDropList)
                                          .WithMaxItemsAllowed(_configSettings.BossItemCount)
                                          .Create();

            var itemsInLunar = ItemsInTiers.Build(_logger)
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
    }
}