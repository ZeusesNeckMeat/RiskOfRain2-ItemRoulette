using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemRoulette
{
    internal class ItemsInTiers : IAsTier, IWithMaxItemsAllowed, IHavingDefaultVoidItems, IHavingDefaultItems, IItemsInTierCreator, IItemsInTier
    {
        private readonly IReadOnlyCollection<ItemTag> _forbiddenTags = new List<ItemTag>
        {
            ItemTag.AIBlacklist,
            ItemTag.EquipmentRelated,
            ItemTag.SprintRelated,
            ItemTag.OnKillEffect
        }.AsReadOnly();

        private IReadOnlyList<PickupIndex> _defaultItemsCopied;
        private IReadOnlyList<PickupIndex> _defaultVoidItemsCopied;
        private int _maxItemsAllowed;
        private int _currentItemsInTier;
        private bool _areItemsAllowedDefaulted = false;
        private readonly ManualLogSource _logger;

        private ItemsInTiers(ManualLogSource logger) 
        {
            _logger = logger;
            ItemsAllowed = new List<PickupIndex>();
            VoidItemsAllowed = new List<PickupIndex>();
        }

        public ItemTier Tier { get; private set; }
        public List<PickupIndex> ItemsAllowed { get; private set; }
        public List<PickupIndex> VoidItemsAllowed { get; private set; }

        public void GenerateRemainingItems()
        {
            _logger.LogInfo($"{nameof(ItemsInTiers)}.{nameof(GenerateRemainingItems)} Getting randomized list");
            var randomizedList = _defaultItemsCopied.Where(x => !ItemsAllowed.Contains(x))
                                                    .OrderBy(x => Guid.NewGuid())
                                                    .ToList();
            
            if (!ItemsAllowed.Any(IsItemAllowedForMonsters))
            {
                _logger.LogInfo($"{nameof(ItemsInTiers)}.{nameof(GenerateRemainingItems)} Adding at least one item for monsters");
                var itemAllowedForMonsters = randomizedList.First(IsItemAllowedForMonsters);
                ItemsAllowed.Add(itemAllowedForMonsters);
                randomizedList.Remove(itemAllowedForMonsters);
                _currentItemsInTier++;
            }

            var numberOfItemsRemainingToAdd = _maxItemsAllowed - _currentItemsInTier;
            _logger.LogInfo($"{nameof(ItemsInTiers)}.{nameof(GenerateRemainingItems)} Number of items remaining to add: {numberOfItemsRemainingToAdd}");

            var remainingItemsToAdd = randomizedList.Take(numberOfItemsRemainingToAdd);
            _logger.LogInfo($"{nameof(ItemsInTiers)}.{nameof(GenerateRemainingItems)} Remaining items to add: {string.Join(" - ", remainingItemsToAdd.Select(x => ItemInfos.GetPickupDef(x).nameToken))}");

            ItemsAllowed.AddRange(remainingItemsToAdd);
        }

        public List<PickupIndex> GetAllowedItems()
        {
            if (ItemsAllowed.Any())
                return ItemsAllowed.ToList();

            return _defaultItemsCopied.ToList();
        }

        public List<PickupIndex> GetAllowedVoidItems()
        {
            if (VoidItemsAllowed.Any())
                return VoidItemsAllowed.ToList();

            return _defaultVoidItemsCopied.ToList();
        }

        public bool TryAddItemToItemsAllowed(PickupIndex pickupIndex)
        {
            _logger.LogInfo($"Checking if items allowed are defaulted for {Tier}");
            if (_areItemsAllowedDefaulted)
                return false;

            _logger.LogInfo($"Checking if items allowed SHOULD be defaulted for {Tier}");
            if (_maxItemsAllowed == _defaultItemsCopied.Count)
            {
                _logger.LogInfo($"MaxItemsAllowed for {Tier}: {_maxItemsAllowed}");
                _logger.LogInfo($"DefaultItemsCopiedCount for {Tier}: {_defaultItemsCopied.Count}");

                _currentItemsInTier = _defaultItemsCopied.Count;
                ItemsAllowed = _defaultItemsCopied.ToList();
                _areItemsAllowedDefaulted = true;

                return false;
            }

            _logger.LogInfo($"Checking is max item limit for {Tier} has been reached.");
            if (HasItemLimitBeenReached())
                return false;

            _logger.LogInfo($"Checking if {ItemInfos.GetItemDef(pickupIndex).name} has already been added to {Tier}");
            if (ItemsAllowed.Contains(pickupIndex))
                return false;

            _logger.LogInfo($"Checking if {Tier} contains an item allowed for monsters, and if {ItemInfos.GetItemDef(pickupIndex).name} can be that item");
            if (!ItemsAllowed.Any(IsItemAllowedForMonsters) && !IsItemAllowedForMonsters(pickupIndex))
                return false;

            ItemsAllowed.Add(pickupIndex);
            _currentItemsInTier++;
            return true;
        }

        public bool HasItemLimitBeenReached()
        {
            return _currentItemsInTier == _maxItemsAllowed;
        }

        private bool IsItemAllowedForMonsters(PickupIndex pickupIndex)
        {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return false;

            var itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
            if (itemDef == null)
                return false;

            return !_forbiddenTags.Any(x => itemDef.ContainsTag(x));
        }

        public static IAsTier Build(ManualLogSource logger) => new ItemsInTiers(logger);
        public IHavingDefaultItems AsTier(ItemTier itemTier)
        {
            Tier = itemTier;
            return this;
        }        

        public IHavingDefaultVoidItems HavingDefaultItems(List<PickupIndex> defaultItems)
        {
            _defaultItemsCopied = new List<PickupIndex>(defaultItems).AsReadOnly();
            _logger.LogInfo($"Default items loaded: {string.Join(", ", _defaultItemsCopied.Select(x => ItemInfos.GetItemDef(x).name))}");
            return this;
        }

        public IWithMaxItemsAllowed HavingDefaultVoidItems(List<PickupIndex> defaultVoidItems)
        {
            _defaultVoidItemsCopied = new List<PickupIndex>(defaultVoidItems).AsReadOnly();
            _logger.LogInfo($"Default void items loaded: {string.Join(", ", _defaultItemsCopied.Select(x => ItemInfos.GetItemDef(x).name))}");
            return this;
        }

        public IWithMaxItemsAllowed HavingNoVoidItems()
        {
            return this;
        }

        public IItemsInTierCreator WithMaxItemsAllowed(int maxItemsAllowed)
        {
            var isMaxItemsAllowedInValid = maxItemsAllowed <= 0 || maxItemsAllowed >= _defaultItemsCopied.Count;
            _maxItemsAllowed = isMaxItemsAllowedInValid ? _defaultItemsCopied.Count : maxItemsAllowed;
            _currentItemsInTier = 0;
            return this;
        }

        public IItemsInTier Create() => this;
    }

    internal interface IAsTier
    {
        IHavingDefaultItems AsTier(ItemTier itemTier);
    }

    internal interface IHavingDefaultItems
    {
        IHavingDefaultVoidItems HavingDefaultItems(List<PickupIndex> defaultItems);
    }

    internal interface IHavingDefaultVoidItems
    {
        IWithMaxItemsAllowed HavingDefaultVoidItems(List<PickupIndex> defaultVoidItems);
        IWithMaxItemsAllowed HavingNoVoidItems();
    }

    internal interface IWithMaxItemsAllowed
    {
        IItemsInTierCreator WithMaxItemsAllowed(int maxItemsAllowed);
    }    

    internal interface IItemsInTierCreator
    {
        IItemsInTier Create();
    }

    internal interface IItemsInTier
    {
        ItemTier Tier { get; }
        List<PickupIndex> ItemsAllowed { get; }
        List<PickupIndex> VoidItemsAllowed { get; }

        List<PickupIndex> GetAllowedItems();
        List<PickupIndex> GetAllowedVoidItems();
        void GenerateRemainingItems();
        bool TryAddItemToItemsAllowed(PickupIndex pickupIndex);
        bool HasItemLimitBeenReached();
    }
}