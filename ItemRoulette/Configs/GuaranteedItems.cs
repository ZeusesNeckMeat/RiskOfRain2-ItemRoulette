using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ItemRoulette.Configs
{
    internal class GuaranteedItems
    {
        private const string SECTION_KEY = "{0} Guaranteed Items";
        private const string SECTION_DESCRIPTION = "Place the numbers of the items in this field that you want to guarantee to be added to the item pool, with each number separated by a comma. Only one item allowed to be guaranteed. Available items {0}.";

        private readonly ManualLogSource _logger;

        private ConfigEntry<string> _tier1GuaranteedItems;
        private ConfigEntry<string> _tier2GuaranteedItems;
        private ConfigEntry<string> _tier3GuaranteedItems;
        private ConfigEntry<string> _bossGuaranteedItems;
        private ConfigEntry<string> _lunarGuaranteedItems;

        private readonly List<PickupIndex> _tier1GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _tier2GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _tier3GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _bossGuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _lunarGuaranteedPickupIndices = new List<PickupIndex>();

        private static readonly IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> _guaranteedItemsByTier = new Dictionary<ItemTier, ReadOnlyCollection<PickupIndex>>();
        private IReadOnlyDictionary<ItemTier, ReadOnlyCollection<ItemInfo>> _itemInfos;

        public GuaranteedItems(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void Initialize(Func<string, string, string, ConfigEntry<string>> bind)
        {
            _itemInfos = ItemInfos.GetItemInfosDictionary();

            _tier1GuaranteedItems = GetConfigValueOrDefault("Tier 1", _itemInfos[ItemTier.Tier1], bind);
            _tier2GuaranteedItems = GetConfigValueOrDefault("Tier 2", _itemInfos[ItemTier.Tier2], bind);
            _tier3GuaranteedItems = GetConfigValueOrDefault("Tier 3", _itemInfos[ItemTier.Tier3], bind);
            _bossGuaranteedItems = GetConfigValueOrDefault("Boss", _itemInfos[ItemTier.Boss], bind);
            _lunarGuaranteedItems = GetConfigValueOrDefault("Lunar", _itemInfos[ItemTier.Lunar], bind);

            VerifyValidityOfGuaranteedItems();
        }

        public IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> GetGuaranteedItemsDictionary()
        {
            return new Dictionary<ItemTier, ReadOnlyCollection<PickupIndex>>(_guaranteedItemsByTier);
        }

        private ConfigEntry<string> GetConfigValueOrDefault(string keyExtra, IReadOnlyCollection<ItemInfo> itemInfos, Func<string, string, string, ConfigEntry<string>> bind)
        {
            var configEntry = bind(string.Format(SECTION_KEY, keyExtra), 0.ToString(), GetItemsWithNumbersDescription(itemInfos));

            if (string.IsNullOrWhiteSpace(configEntry.Value))
                configEntry.Value = 0.ToString();

            return configEntry;
        }

        private string GetItemsWithNumbersDescription(IReadOnlyCollection<ItemInfo> itemInfos)
        {
            var itemsWithNumbers = itemInfos.OrderBy(x => x.DisplayName).Select(item => $"{item.IndexNumber}: {item.DisplayName}").ToList();
            itemsWithNumbers.Insert(0, "0: Default - No guaranteed item");

            return string.Format(SECTION_DESCRIPTION, string.Join(", ", itemsWithNumbers));
        }

        private void VerifyValidityOfGuaranteedItems()
        {
            var stringBuilder = new StringBuilder();

            _tier1GuaranteedPickupIndices.Clear();
            _tier2GuaranteedPickupIndices.Clear();
            _tier3GuaranteedPickupIndices.Clear();
            _bossGuaranteedPickupIndices.Clear();
            _lunarGuaranteedPickupIndices.Clear();
            _guaranteedItemsByTier.Clear();

            var invalidConfigValues = new List<string>();
            var itemInfos = ItemInfos.GetItemInfosDictionary().SelectMany(x => x.Value);

            AddGuaranteedItems(_tier1GuaranteedItems.Value, ItemTier.Tier1, invalidConfigValues, itemInfos, _tier1GuaranteedPickupIndices);
            AddGuaranteedItems(_tier2GuaranteedItems.Value, ItemTier.Tier2, invalidConfigValues, itemInfos, _tier2GuaranteedPickupIndices);
            AddGuaranteedItems(_tier3GuaranteedItems.Value, ItemTier.Tier3, invalidConfigValues, itemInfos, _tier3GuaranteedPickupIndices);
            AddGuaranteedItems(_bossGuaranteedItems.Value, ItemTier.Boss, invalidConfigValues, itemInfos, _bossGuaranteedPickupIndices);
            AddGuaranteedItems(_lunarGuaranteedItems.Value, ItemTier.Lunar, invalidConfigValues, itemInfos, _lunarGuaranteedPickupIndices);

            if (invalidConfigValues.Any())
            {
                stringBuilder.Append("Some Guaranteed Items config values were invalid: ");

                foreach (var invalidConfigValue in invalidConfigValues)
                    stringBuilder.AppendLine(invalidConfigValue);

                _logger.LogInfo(stringBuilder.ToString());
            }
        }

        private void AddGuaranteedItems(string guaranteedItemsString, ItemTier itemTier, List<string> invalidConfigValues, IEnumerable<ItemInfo> itemInfos, List<PickupIndex> guaranteedPickupIndices)
        {
            foreach (var itemIndexString in guaranteedItemsString.Split(',').Select(x => x.Trim()))
            {
                if (string.IsNullOrWhiteSpace(itemIndexString))
                    continue;

                if (!Enum.TryParse<ItemIndex>(itemIndexString, out var itemIndex))
                {
                    _logger.LogInfo($"Invalid item: {itemIndexString}");
                    invalidConfigValues.Add(itemIndexString);
                    continue;
                }

                if (itemIndex == 0)
                    continue;

                if (!_itemInfos[itemTier].Any(itemInfo => itemInfo.Index == itemIndex))
                {
                    _logger.LogInfo($"Item {itemIndex} not valid for {itemTier}");
                    invalidConfigValues.Add(itemIndexString);
                    continue;
                }

                if (itemInfos.Any(x => x.Index == itemIndex))
                {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    _logger.LogInfo($"Guaranteed item added: {pickupIndex}");
                    guaranteedPickupIndices.Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }

            if (guaranteedPickupIndices.Any())
                _guaranteedItemsByTier[itemTier] = guaranteedPickupIndices.AsReadOnly();
        }
    }
}