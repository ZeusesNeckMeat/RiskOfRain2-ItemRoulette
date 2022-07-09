using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ItemRoulette
{
    internal class ItemInfo
    {
        public ItemInfo(ItemTier itemTier, ItemIndex itemIndex, string displayName)
        {
            Tier = itemTier;
            Index = itemIndex;
            DisplayName = displayName;
        }

        public ItemTier Tier { get; private set; }
        public ItemIndex Index { get; private set; }
        public int IndexNumber => (int)Index;
        public string DisplayName { get; private set; }

        public bool IsNameMatch(string displayName)
        {
            return string.Equals(DisplayName, displayName, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class ItemInfos
    {
        private static IDictionary<ItemTier, ReadOnlyCollection<ItemInfo>> _itemInfosByTiers = new Dictionary<ItemTier, ReadOnlyCollection<ItemInfo>>();

        public static void GenerateItemLists()
        {
            var itemInfosByTiers = new Dictionary<ItemTier, List<ItemInfo>>();
            var itemTierDefs = ItemTierCatalog.allItemTierDefs.ToList();

            foreach (var itemIndex in ItemCatalog.allItems)
            {
                var displayName = GetPickupDef(itemIndex).nameToken;
                if (displayName.ToLower().Contains("scrap"))
                    continue;

                var itemDef = GetItemDef(itemIndex);
                                
                if (!itemTierDefs.FirstOrDefault(x => x.tier == itemDef.tier)?.isDroppable ?? false)
                    continue;

                var itemInfo = new ItemInfo(itemDef.tier, itemIndex, Language.GetString(displayName));

                if (!itemInfosByTiers.ContainsKey(itemDef.tier))
                    itemInfosByTiers[itemDef.tier] = new List<ItemInfo> { itemInfo };
                else
                    itemInfosByTiers[itemDef.tier].Add(itemInfo);
            }

            foreach (var itemInfo in itemInfosByTiers)
                _itemInfosByTiers[itemInfo.Key] = itemInfo.Value.AsReadOnly();
        }

        public static IReadOnlyDictionary<ItemTier, ReadOnlyCollection<ItemInfo>> GetItemInfosDictionary()
        {
            return new ReadOnlyDictionary<ItemTier, ReadOnlyCollection<ItemInfo>>(_itemInfosByTiers);
        }

        public static IEnumerable<ItemTag> GetItemTags(ItemDef itemDef)
        {
            if (itemDef == null)
                return new List<ItemTag>();

            return itemDef.tags;
        }

        public static ItemDef GetItemDef(PickupIndex pickupIndex)
        {
            var pickupDef = GetPickupDef(pickupIndex);
            return pickupDef == null ? null : GetItemDef(pickupDef.itemIndex);
        }

        public static PickupDef GetPickupDef(PickupIndex pickupIndex)
        {
            return PickupCatalog.GetPickupDef(pickupIndex);
        }

        public static PickupDef GetPickupDef(ItemIndex itemIndex)
        {
            return PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(itemIndex));
        }

        public static ItemDef GetItemDef(ItemIndex itemIndex)
        {
            return ItemCatalog.GetItemDef(itemIndex);
        }

        public static ItemTier GetItemTier(ItemDef itemDef)
        {
            return itemDef == null ? ItemTier.NoTier : itemDef.tier;
        }
    }
}