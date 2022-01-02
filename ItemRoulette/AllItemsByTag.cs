using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ItemRoulette
{
    internal class AllItemsByTag : IHavingItemsAllowed, IHavingTotalItemCount, 
        IWithTotalAllowedDamageItemCount, IWithTotalAllowedHealingItemCount, IWithTotalAllowedUtilityItemCount,
        IWithItemsInTiers, IAllItemsByTagCreator, IAllItemsByTag
    {
        private AllItemsByTag(ManualLogSource logger) 
        {
            _logger = logger;
        }

        private readonly ManualLogSource _logger;
        private int _totalCountOfAllItemsAllowed;

        private readonly ReadOnlyCollection<ItemTag> _allowedItemTags = new List<ItemTag> { ItemTag.Healing, ItemTag.Damage, ItemTag.Utility }.AsReadOnly();
        private readonly List<(ItemTier Tier, List<ItemTag> Tags, PickupIndex Item)> _itemsByTagAndTier = new List<(ItemTier Tier, List<ItemTag> Tag, PickupIndex Item)>();
        private readonly IDictionary<ItemTag, int> _minItemsAllowedForTag = new Dictionary<ItemTag, int>();
        private readonly IDictionary<ItemTag, int> _currentCountOfItemsForTag = new Dictionary<ItemTag, int>();

        private List<IItemsInTier> _itemsInTiers = new List<IItemsInTier>();

        public List<IItemsInTier> GetItemPoolForTiers()
        {
            _logger.LogInfo("Starting Generation of items for run in tiers");
            foreach (var itemTagForTier in _minItemsAllowedForTag)
            {
                var tag = itemTagForTier.Key;
                var minCount = itemTagForTier.Value;

                _logger.LogInfo($"Current ItemTag: {tag}. Min Count for ItemTag: {minCount}.");
                foreach (var (itemTier, itemTags, pickupIndex) in _itemsByTagAndTier)
                {
                    if (_currentCountOfItemsForTag[tag] == minCount)
                    {
                        _logger.LogInfo($"Min items added for this {tag}");
                        break;
                    }

                    _logger.LogInfo($"Verifying {tag} exists in list of {GetItemDef(pickupIndex).name}'s tags.");
                    if (!itemTags.Contains(tag))
                    {
                        _logger.LogInfo($"Item not valid for current item tag: {tag} - {GetItemDef(pickupIndex).name}");
                        continue;
                    }

                    _logger.LogInfo($"Attempting to add {GetItemDef(pickupIndex).name} to {itemTier}");
                    var itemsInTier = _itemsInTiers.Single(x => x.Tier == itemTier);
                    if (!itemsInTier.TryAddItemToItemsAllowed(pickupIndex))
                    {
                        _logger.LogInfo($"{GetItemDef(pickupIndex).name} was not added to {itemTier}");
                        continue;
                    }

                    _logger.LogInfo($"Item added to run. Incrementing counters. {itemTier}-{tag}-{GetItemDef(pickupIndex).name}");
                    _currentCountOfItemsForTag[tag]++;

                    _logger.LogInfo($"Current Count of items for {itemTier}: {itemsInTier.ItemsAllowed.Count}");
                    _logger.LogInfo($"Current Count of items for {tag}: {_currentCountOfItemsForTag[tag]}");

                    if (_currentCountOfItemsForTag[tag] == _minItemsAllowedForTag[tag])
                    {
                        _logger.LogInfo($"Min items added for item tag: {tag}.");
                        break;
                    }
                }
            }

            var itemsInTiersNotAtMaxCount = _itemsInTiers.Where(x => !x.HasItemLimitBeenReached());

            _logger.LogInfo($"Tiers to finish filling out: {string.Join("-", itemsInTiersNotAtMaxCount.Select(x => x.Tier.ToString()))}");

            foreach (var itemsInTierNotAtMaxCount in itemsInTiersNotAtMaxCount)
                itemsInTierNotAtMaxCount.GenerateRemainingItems();

            return _itemsInTiers;
        }

        private IEnumerable<ItemTag> GetItemTags(ItemDef itemDef)
        {
            if (itemDef == null)
                return new List<ItemTag>();

            return itemDef.tags;
        }

        private ItemDef GetItemDef(PickupIndex pickupIndex)
        {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            return pickupDef == null ? null : ItemCatalog.GetItemDef(pickupDef.itemIndex);
        }

        private ItemTier GetItemTier(ItemDef itemDef)
        {
            return itemDef == null ? ItemTier.NoTier : itemDef.tier;
        }

        private int GetRoundedCount(double percentageOfAllowedItems)
        {
            if (percentageOfAllowedItems == 0)
                return 0;

            if (percentageOfAllowedItems < 0)
                return -1;
            
            return Convert.ToInt32(Math.Floor(_totalCountOfAllItemsAllowed * (percentageOfAllowedItems / 100)));
        }

        private (ItemTier Tier, List<ItemTag> Tags, PickupIndex Item) GetAllInfoForItem(PickupIndex pickupIndex)
        {
            var itemDef = GetItemDef(pickupIndex);

            var itemTier = GetItemTier(itemDef);
            var itemTags = GetItemTags(itemDef).OrderBy(x => Guid.NewGuid()).ToList();

            return (itemTier, itemTags, pickupIndex);
        }

        public static IHavingItemsAllowed Build(ManualLogSource logger) => new AllItemsByTag(logger);

        public IHavingTotalItemCount UsingItemsForTags(List<PickupIndex> allItemsAllowed)
        {
            _itemsByTagAndTier.AddRange(allItemsAllowed.Select(GetAllInfoForItem)
                                                       .Where(itemInfo => itemInfo.Tags.Any(itemTag => _allowedItemTags.Contains(itemTag)))
                                                       .OrderBy(x => Guid.NewGuid())
                                                       .ToList());
            return this;
        }

        public IWithTotalAllowedDamageItemCount HavingTotalItemCount(int totalCountOfAllItemsAllowed)
        {
            _totalCountOfAllItemsAllowed = totalCountOfAllItemsAllowed;
            return this;
        }

        public IWithTotalAllowedHealingItemCount WithTotalAllowedDamageItemCount(double percentageOfDamageItems)
        {
            var roundedCount = GetRoundedCount(percentageOfDamageItems);

            if (roundedCount < 0)
                roundedCount = _itemsByTagAndTier.Where(x => x.Tags.Contains(ItemTag.Damage)).Count();

            _minItemsAllowedForTag[ItemTag.Damage] = roundedCount;
            _currentCountOfItemsForTag[ItemTag.Damage] = 0;
            _logger.LogInfo($"Min Allowed for {ItemTag.Damage}: {roundedCount}");
            return this;
        }

        public IWithTotalAllowedUtilityItemCount WithTotalAllowedHealingItemCount(double percentageOfHealingItems)
        {
            var roundedCount = GetRoundedCount(percentageOfHealingItems);

            if (roundedCount < 0)
                roundedCount = _itemsByTagAndTier.Where(x => x.Tags.Contains(ItemTag.Healing)).Count();

            _minItemsAllowedForTag[ItemTag.Healing] = roundedCount;
            _currentCountOfItemsForTag[ItemTag.Healing] = 0;
            _logger.LogInfo($"Min Allowed for {ItemTag.Healing}: {roundedCount}");
            return this;
        }

        public IWithItemsInTiers WithTotalAllowedUtilityItemCount(double percentageOfUtilityItems)
        {
            var roundedCount = GetRoundedCount(percentageOfUtilityItems);

            if (roundedCount < 0)
                roundedCount = _itemsByTagAndTier.Where(x => x.Tags.Contains(ItemTag.Utility)).Count();

            _minItemsAllowedForTag[ItemTag.Utility] = roundedCount;
            _currentCountOfItemsForTag[ItemTag.Utility] = 0;
            _logger.LogInfo($"Min Allowed for {ItemTag.Utility}: {roundedCount}");
            return this;
        }

        public IAllItemsByTagCreator WithItemsInTiers(List<IItemsInTier> itemsInTiers)
        {
            _itemsInTiers = itemsInTiers.ToList();
            return this;
        }

        public IAllItemsByTag Create() => this;
    }

    internal interface IHavingItemsAllowed
    {
        IHavingTotalItemCount UsingItemsForTags(List<PickupIndex> allItemsAllowed);
    }

    internal interface IHavingTotalItemCount
    {
        IWithTotalAllowedDamageItemCount HavingTotalItemCount(int totalCountOfAllItemsAllowed);
    }

    internal interface IWithTotalAllowedDamageItemCount
    {
        IWithTotalAllowedHealingItemCount WithTotalAllowedDamageItemCount(double percentageOfDamageItems);
    }

    internal interface IWithTotalAllowedHealingItemCount
    {
        IWithTotalAllowedUtilityItemCount WithTotalAllowedHealingItemCount(double percentageOfHealingItems);
    }

    internal interface IWithTotalAllowedUtilityItemCount
    {
        IWithItemsInTiers WithTotalAllowedUtilityItemCount(double percentageOfUtilityItems);
    }

    internal interface IWithItemsInTiers
    {
        IAllItemsByTagCreator WithItemsInTiers(List<IItemsInTier> itemsInTiers);
    }

    internal interface IAllItemsByTagCreator
    {
        IAllItemsByTag Create();
    }

    internal interface IAllItemsByTag
    {
        List<IItemsInTier> GetItemPoolForTiers();
    }
}