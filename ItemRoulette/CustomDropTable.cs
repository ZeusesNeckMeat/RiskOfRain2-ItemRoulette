using BepInEx.Logging;
using ItemRoulette.Configs;
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
            if (!_configSettings.GeneralSettings.IsModEnabled)
            {
                _logger.LogInfo("Mod disabled. Not altering drop tables.");
                return;
            }

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
                                             .HavingTotalItemCount(_configSettings.ItemTierCountSettings.TotalItemCountForTiers123)
                                             .WithTotalAllowedDamageItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfDamageItems)
                                             .WithTotalAllowedHealingItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfHealingItems)
                                             .WithTotalAllowedUtilityItemCount(_configSettings.ItemTagPercentsSettings.PercentageOfUtilityItems)
                                             .WithItemsInTiers(_itemsInTiers)
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
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier1ItemCount)
                                           .Create();

            var itemsInTier2 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier2)
                                           .HavingDefaultItems(Run.instance.availableTier2DropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier2ItemCount)
                                           .Create();

            var itemsInTier3 = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Tier3)
                                           .HavingDefaultItems(Run.instance.availableTier3DropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.Tier3ItemCount)
                                           .Create();

            var itemsInBoss = ItemsInTiers.Build(_logger)
                                          .AsTier(ItemTier.Boss)
                                          .HavingDefaultItems(Run.instance.availableBossDropList)
                                          .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.BossItemCount)
                                          .Create();

            var itemsInLunar = ItemsInTiers.Build(_logger)
                                           .AsTier(ItemTier.Lunar)
                                           .HavingDefaultItems(Run.instance.availableLunarDropList)
                                           .WithMaxItemsAllowed(_configSettings.ItemTierCountSettings.LunarItemCount)
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