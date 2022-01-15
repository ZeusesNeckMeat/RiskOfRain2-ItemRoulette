using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ItemRoulette
{
    public class ConfigSettings
    {
        private const double MIN_PERCENTAGE = 0;
        private const double MAX_ALLOWED_PERCENTAGE = 100;
        private readonly ManualLogSource _logger;

        public ConfigSettings(ConfigFile config, ManualLogSource logger)
        {
            Config = config;
            _logger = logger;
        }

        public ConfigFile Config { get; private set; }

        public int TotalItemCountForTiers123 => Tier1ItemCount + Tier2ItemCount + Tier3ItemCount;
        private readonly List<ConfigEntry<double>> _percentagesOfItems = new List<ConfigEntry<double>>();
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private static IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> _guaranteedItemsByTier = new Dictionary<ItemTier, ReadOnlyCollection<PickupIndex>>();

        private readonly List<PickupIndex> _tier1GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _tier2GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _tier3GuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _bossGuaranteedPickupIndices = new List<PickupIndex>();
        private readonly List<PickupIndex> _lunarGuaranteedPickupIndices = new List<PickupIndex>();

        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<ItemRefreshOptions> _itemRefreshOptions;

        private ConfigEntry<int> _tier1ItemCount;
        private ConfigEntry<int> _tier2ItemCount;
        private ConfigEntry<int> _tier3ItemCount;
        private ConfigEntry<int> _bossItemCount;
        private ConfigEntry<int> _lunarItemCount;

        private ConfigEntry<double> _percentageOfDamageItems;
        private ConfigEntry<double> _percentageOfHealingItems;
        private ConfigEntry<double> _percentageOfUtilityItems;

        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItems;
        private ConfigEntry<string> _tier1GuaranteedItems;
        private ConfigEntry<string> _tier2GuaranteedItems;
        private ConfigEntry<string> _tier3GuaranteedItems;
        private ConfigEntry<string> _bossGuaranteedItems;
        private ConfigEntry<string> _lunarGuaranteedItems;

        public bool IsModEnabled => _isModEnabled.Value;
        public ItemRefreshOptions ItemRefreshOptions => _itemRefreshOptions.Value;

        public int Tier1ItemCount => _tier1ItemCount.Value;
        public int Tier2ItemCount => _tier2ItemCount.Value;
        public int Tier3ItemCount => _tier3ItemCount.Value;
        public int BossItemCount => _bossItemCount.Value;
        public int LunarItemCount => _lunarItemCount.Value;

        public double TotalPercentageOfItems => PercentageOfDamageItems + PercentageOfHealingItems + PercentageOfUtilityItems;
        public double PercentageOfDamageItems => _percentageOfDamageItems.Value;
        public double PercentageOfHealingItems => _percentageOfHealingItems.Value;
        public double PercentageOfUtilityItems => _percentageOfUtilityItems.Value;

        public bool ShouldOnlyUseGuaranteedItems => _shouldOnlyUseGuaranteedItems.Value;

        public IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> GetGuarnteedItemsDictionary()
        {
            return new Dictionary<ItemTier, ReadOnlyCollection<PickupIndex>>(_guaranteedItemsByTier);
        }

        public bool HasConfigErrors() => _stringBuilder.Length > 0;
        public string GetErrors() => _stringBuilder.ToString();

        public void InitializeConfigFile(bool shouldReloadConfig = false)
        {
            var configDescriptionItemCount = "This is number of items that will be available for {0}. The items in the run will be pulled at random from all unlocked items in this tier. Set to 0 to have all unlocked items in the run.";
            var configDescriptionPercent = "Determines what percent of total items available between Tiers 1, 2, and 3 will be of the {0} type. 0 means no items of that type";
            var configDescriptionRefreshItemPool = "Determines if and when you want the list of items in the run to refresh";
            var guaranteedItemsDescription = "Place the number of the item in this field that you want to guarantee to be added to the item pool. Only one item allowed to be guaranteed. Available items {0}.";

            var newTotalCountsSection = "New Total Item Counts";
            var percentsSection = "Item Tag Percents";
            var generalSection = "General";
            var guaranteedItemsSection = "Guaranteed Items";

            if (shouldReloadConfig)
                Config.Reload();

            _isModEnabled = Config.Bind(generalSection, "IsEnabled", true, "Allows the enabling or disabling of the entire mod without needing to change every value back to default.");
            _itemRefreshOptions = Config.Bind(generalSection, "ItemPoolRefreshOptions", ItemRefreshOptions.NewRun, configDescriptionRefreshItemPool);

            _tier1ItemCount = Config.Bind(newTotalCountsSection, "Tier1NewCount", 5, string.Format(configDescriptionItemCount, "Tier1 items"));
            _tier2ItemCount = Config.Bind(newTotalCountsSection, "Tier2NewCount", 3, string.Format(configDescriptionItemCount, "Tier2 items"));
            _tier3ItemCount = Config.Bind(newTotalCountsSection, "Tier3NewCount", 1, string.Format(configDescriptionItemCount, "Tier3 items"));
            _bossItemCount = Config.Bind(newTotalCountsSection, "BossNewCount", 0, string.Format(configDescriptionItemCount, "Boss items"));
            _lunarItemCount = Config.Bind(newTotalCountsSection, "LunarNewCount", 0, string.Format(configDescriptionItemCount, "Lunar items"));

            _percentageOfDamageItems = Config.Bind(percentsSection, "DamageTypePercentage", 33d, string.Format(configDescriptionPercent, "Damage"));
            _percentageOfUtilityItems = Config.Bind(percentsSection, "UtilityTypePercentage", 33d, string.Format(configDescriptionPercent, "Utility"));
            _percentageOfHealingItems = Config.Bind(percentsSection, "HealingTypePercentage", 33d, string.Format(configDescriptionPercent, "Healing"));

            var itemInfosDictionary = ItemInfos.GetItemInfosDictionary();

            _shouldOnlyUseGuaranteedItems = Config.Bind(guaranteedItemsSection, "Should only use guaranteed items", false, "Set to true if you want to only use the items in the Guaranteed lists. Any values set to 0 will be ignored, and these tiers will be generated like normal");
            _tier1GuaranteedItems = Config.Bind(guaranteedItemsSection, "Tier 1 Guaranteed Items", 0.ToString(), GetConfigDescription(guaranteedItemsDescription, itemInfosDictionary[ItemTier.Tier1]));
            _tier2GuaranteedItems = Config.Bind(guaranteedItemsSection, "Tier 2 Guaranteed Items", 0.ToString(), GetConfigDescription(guaranteedItemsDescription, itemInfosDictionary[ItemTier.Tier2]));
            _tier3GuaranteedItems = Config.Bind(guaranteedItemsSection, "Tier 3 Guaranteed Items", 0.ToString(), GetConfigDescription(guaranteedItemsDescription, itemInfosDictionary[ItemTier.Tier3]));
            _bossGuaranteedItems = Config.Bind(guaranteedItemsSection, "Boss Guaranteed Items", 0.ToString(), GetConfigDescription(guaranteedItemsDescription, itemInfosDictionary[ItemTier.Boss]));
            _lunarGuaranteedItems = Config.Bind(guaranteedItemsSection, "Lunar Guaranteed Items", 0.ToString(), GetConfigDescription(guaranteedItemsDescription, itemInfosDictionary[ItemTier.Lunar]));

            _logger.LogInfo($"Tier1 Guaranteed items {_tier1GuaranteedItems.Value}");
            _logger.LogInfo($"Tier2 Guaranteed items {_tier2GuaranteedItems.Value}");
            _logger.LogInfo($"Tier3 Guaranteed items {_tier3GuaranteedItems.Value}");
            _logger.LogInfo($"Boss Guaranteed items {_bossGuaranteedItems.Value}");
            _logger.LogInfo($"Lunar Guaranteed items {_lunarGuaranteedItems.Value}");

            _percentagesOfItems.Clear();
            _percentagesOfItems.Add(_percentageOfDamageItems);
            _percentagesOfItems.Add(_percentageOfUtilityItems);
            _percentagesOfItems.Add(_percentageOfHealingItems);

            VerifyValidityOfPercentages();
            VerifyValidityOfGuaranteedItems();
        }

        public void RefreshConfigSettings()
        {
            InitializeConfigFile(true);
            Chat.AddMessage($"ItemRoulette reloaded. Mod is enabled: {IsModEnabled}.");
        }

        private void VerifyValidityOfPercentages()
        {
            if (!ArePercentagesValid())
            {
                foreach (var percentageOfItems in _percentagesOfItems)
                    percentageOfItems.Value = 33;

                Config.Reload();
                return;
            }

            var individualConfigPercentagesInvalid = GetConfigPercentagesDefaulted();
            var validPercentConfigs = _percentagesOfItems.Except(individualConfigPercentagesInvalid);
            var totalValidPercentage = validPercentConfigs.Select(x => x.Value).ToList().Sum();
            var defaultPercentage = (MAX_ALLOWED_PERCENTAGE - totalValidPercentage) / individualConfigPercentagesInvalid.Count();
            
            foreach (var individualConfig in individualConfigPercentagesInvalid)
            {
                individualConfig.Value = defaultPercentage;
            }

            if (individualConfigPercentagesInvalid.Any())
                Config.Reload();
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

                _logger.LogInfo($"Guaranteed item should add: {itemIndex}");
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

        private ConfigDescription GetConfigDescription(string description, IReadOnlyCollection<ItemInfo> itemInfos)
        {
            var allowedIndices = itemInfos.Select(x => x.IndexNumber.ToString()).ToList();
            allowedIndices.Insert(0, "0");

            var itemsWithNumbers = itemInfos.Select(item => $"{item.IndexNumber}: {item.DisplayName}").ToList();
            itemsWithNumbers.Insert(0, 0.ToString());

            return new ConfigDescription(string.Format(description, string.Join(", ", itemsWithNumbers)), new AcceptableValueList<string>(allowedIndices.ToArray()));
        }

        private bool ArePercentagesValid()
        {
            return TotalPercentageOfItems > MIN_PERCENTAGE && TotalPercentageOfItems <= MAX_ALLOWED_PERCENTAGE;
        }
        
        private IEnumerable<ConfigEntry<double>> GetConfigPercentagesDefaulted()
        {
            return _percentagesOfItems.Where(x => x.Value < MIN_PERCENTAGE || x.Value > MAX_ALLOWED_PERCENTAGE);
        }
    }

    public enum ItemRefreshOptions
    {
        NewRun,
        EachStage,
        EachLoop
    }
}