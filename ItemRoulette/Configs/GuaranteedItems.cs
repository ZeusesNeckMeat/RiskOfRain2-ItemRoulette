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
    internal class GuaranteedItems : ConfigBase
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly ManualLogSource _logger;

        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItems;
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

        public GuaranteedItems(ConfigFile config, ManualLogSource logger) : base(config) 
        { 
            _logger = logger;
        }

        public override string SectionName => "Guaranteed Items";
        public override string SectionDescription => "Place the number of the item in this field that you want to guarantee to be added to the item pool. Only one item allowed to be guaranteed. Available items {0}.";

        public bool ShouldOnlyUseGuaranteedItems => _shouldOnlyUseGuaranteedItems.Value;

        public override void Initialize()
        {
            var itemInfosDictionary = ItemInfos.GetItemInfosDictionary();

            _shouldOnlyUseGuaranteedItems = Bind(SectionName, "Should only use guaranteed items", false, "Set to true if you want to only use the items in the Guaranteed lists. Any values set to 0 will be ignored, and these tiers will be generated like normal");
            _tier1GuaranteedItems = Bind("Tier 1 Guaranteed Items", 0.ToString(), GetConfigDescritionInfo(itemInfosDictionary[ItemTier.Tier1]));
            _tier2GuaranteedItems = Bind("Tier 2 Guaranteed Items", 0.ToString(), GetConfigDescritionInfo(itemInfosDictionary[ItemTier.Tier2]));
            _tier3GuaranteedItems = Bind("Tier 3 Guaranteed Items", 0.ToString(), GetConfigDescritionInfo(itemInfosDictionary[ItemTier.Tier3]));
            _bossGuaranteedItems = Bind("Boss Guaranteed Items", 0.ToString(), GetConfigDescritionInfo(itemInfosDictionary[ItemTier.Boss]));
            _lunarGuaranteedItems = Bind("Lunar Guaranteed Items", 0.ToString(), GetConfigDescritionInfo(itemInfosDictionary[ItemTier.Lunar]));

            VerifyValidityOfGuaranteedItems();
        }

        public IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> GetGuarnteedItemsDictionary()
        {
            return new Dictionary<ItemTier, ReadOnlyCollection<PickupIndex>>(_guaranteedItemsByTier);
        }

        private (string, AcceptableValueList<string>) GetConfigDescritionInfo(IReadOnlyCollection<ItemInfo> itemInfos)
        {
            return (GetItemsWithNumbersDescription(itemInfos), GetAcceptableValues(itemInfos));
        }

        private string GetItemsWithNumbersDescription(IReadOnlyCollection<ItemInfo> itemInfos)
        {
            var itemsWithNumbers = itemInfos.OrderBy(x => x.DisplayName).Select(item => $"{item.IndexNumber}: {item.DisplayName}").ToList();
            itemsWithNumbers.Insert(0, 0.ToString());

            return string.Join(", ", itemsWithNumbers);
        }

        private AcceptableValueList<string> GetAcceptableValues(IReadOnlyCollection<ItemInfo> itemInfos)
        {
            var allowedIndices = itemInfos.Select(x => x.IndexNumber.ToString()).ToList();
            allowedIndices.Insert(0, "0");

            return new AcceptableValueList<string>(allowedIndices.ToArray());
        }        

        private void VerifyValidityOfGuaranteedItems()
        {
            _stringBuilder.Clear();
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
                _stringBuilder.Append("Some Guaranteed Items config values were invalid: ");

                foreach (var invalidConfigValue in invalidConfigValues)
                    _stringBuilder.Append(invalidConfigValue);
            }
        }

        private void AddGuaranteedItems(string guaranteedItemsString, ItemTier itemTier, List<string> invalidConfigValues, IEnumerable<ItemInfo> itemInfos, List<PickupIndex> guaranteedPickupIndices)
        {
            foreach (var itemIndexString in guaranteedItemsString.Split(','))
            {
                if (!Enum.TryParse<ItemIndex>(itemIndexString, out var itemIndex))
                {
                    _logger.LogInfo($"Invalid item: {itemIndexString}");
                    invalidConfigValues.Add(itemIndexString);
                    continue;
                }

                //_logger.LogInfo($"Guaranteed item should add: {itemIndex}");
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