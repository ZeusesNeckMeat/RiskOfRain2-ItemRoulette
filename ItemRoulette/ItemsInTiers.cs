using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemRoulette
{
    internal class ItemsInTiers : IAsTier, IWithMaxItemsAllowed, IHavingDefaultItems, IItemsInTierCreator, IItemsInTier
    {
        private readonly IReadOnlyCollection<ItemTag> _forbiddenTags = new List<ItemTag>
        {
            ItemTag.AIBlacklist,
            ItemTag.EquipmentRelated,
            ItemTag.SprintRelated,
            ItemTag.OnKillEffect
        }.AsReadOnly();

        private IReadOnlyList<PickupIndex> _defaultItemsCopied;
        private int _maxItemsAllowed;
        private int _currentItemsInTier;
        private bool _areItemsAllowedDefaulted = false;
        private readonly ManualLogSource _logger;

        private ItemsInTiers(ManualLogSource logger) 
        {
            _logger = logger;
            ItemsAllowed = new List<PickupIndex>();
        }

        public ItemTier Tier { get; private set; }
        public List<PickupIndex> ItemsInInstance { get; private set; }
        public List<PickupIndex> ItemsAllowed { get; private set; }

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
            _logger.LogInfo($"{nameof(ItemsInTiers)}.{nameof(GenerateRemainingItems)} Remaining items to add: {string.Join(" - ", remainingItemsToAdd.Select(x => PickupCatalog.GetPickupDef(x).nameToken))}");

            ItemsAllowed.AddRange(remainingItemsToAdd);
        }

        public void SetItemsInInstance()
        {
            ItemsInInstance.Clear();
            ItemsInInstance.AddRange(ItemsAllowed.ToList());
        }

        public void SetItemsInInstance(List<PickupIndex> itemsAllowed)
        {
            ItemsInInstance.Clear();
            ItemsInInstance.AddRange(itemsAllowed.ToList());
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

            _logger.LogInfo($"Checking if {GetItemDef(pickupIndex).name} has already been added to {Tier}");
            if (ItemsAllowed.Contains(pickupIndex))
                return false;

            _logger.LogInfo($"Checking if {Tier} contains an item allowed for monsters, and if {GetItemDef(pickupIndex).name} can be that item");
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

        private ItemDef GetItemDef(PickupIndex pickupIndex)
        {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            return pickupDef == null ? null : ItemCatalog.GetItemDef(pickupDef.itemIndex);
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

        public IWithMaxItemsAllowed HavingDefaultItems(List<PickupIndex> defaultItems)
        {
            ItemsInInstance = defaultItems;
            _defaultItemsCopied = new List<PickupIndex>(defaultItems).AsReadOnly();
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
        IWithMaxItemsAllowed HavingDefaultItems(List<PickupIndex> defaultItems);
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
        List<PickupIndex> ItemsInInstance { get; }
        List<PickupIndex> ItemsAllowed { get; }

        void GenerateRemainingItems();
        void SetItemsInInstance();
        void SetItemsInInstance(List<PickupIndex> itemsAllowed);
        bool TryAddItemToItemsAllowed(PickupIndex pickupIndex);
        bool HasItemLimitBeenReached();
    }
}