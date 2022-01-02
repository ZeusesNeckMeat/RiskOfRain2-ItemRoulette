using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ItemRoulette
{
    public class ConfigSettings
    {
        private const double MIN_PERCENTAGE = 0;
        private const double MAX_ALLOWED_PERCENTAGE = 100;

        public ConfigSettings(ConfigFile config)
        {
            Config = config;
        }

        public ConfigFile Config { get; private set; }

        public int TotalItemCountForTiers123 => Tier1ItemCount + Tier2ItemCount + Tier3ItemCount;
        private List<ConfigEntry<double>> _percentagesOfItems = new List<ConfigEntry<double>>();

        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<int> _tier1ItemCount;
        private ConfigEntry<int> _tier2ItemCount;
        private ConfigEntry<int> _tier3ItemCount;
        private ConfigEntry<int> _bossItemCount;
        private ConfigEntry<int> _lunarItemCount;
        private ConfigEntry<double> _percentageOfDamageItems;
        private ConfigEntry<double> _percentageOfHealingItems;
        private ConfigEntry<double> _percentageOfUtilityItems;
        private ConfigEntry<ItemRefreshOptions> _itemRefreshOptions;

        public bool IsModEnabled => _isModEnabled.Value;
        public int Tier1ItemCount => _tier1ItemCount.Value;
        public int Tier2ItemCount => _tier2ItemCount.Value;
        public int Tier3ItemCount => _tier3ItemCount.Value;
        public int BossItemCount => _bossItemCount.Value;
        public int LunarItemCount => _lunarItemCount.Value;
        public double TotalPercentageOfItems => PercentageOfDamageItems + PercentageOfHealingItems + PercentageOfUtilityItems;
        public double PercentageOfDamageItems => _percentageOfDamageItems.Value;
        public double PercentageOfHealingItems => _percentageOfHealingItems.Value;
        public double PercentageOfUtilityItems => _percentageOfUtilityItems.Value;
        public ItemRefreshOptions ItemRefreshOptions => _itemRefreshOptions.Value;

        public void InitializeConfigFile(bool shouldReloadConfig = false)
        {
            var configDescriptionItemCount = "This is number of items that will be available for {0}. The items in the run will be pulled at random from all unlocked items in this tier. Set to 0 to have all unlocked items in the run.";
            var configDescriptionPercent = "Determines what percent of total items available between Tiers 1, 2, and 3 will be of the {0} type. 0 means no items of that type";
            var configDescriptionRefreshItemPool = "Determines if and when you want the list of items in the run to refresh";

            var newTotalCountsSection = "New Total Item Counts";
            var percentsSection = "Item Tag Percents";
            var generalSection = "General";

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

            _percentagesOfItems.Clear();
            _percentagesOfItems.Add(_percentageOfDamageItems);
            _percentagesOfItems.Add(_percentageOfUtilityItems);
            _percentagesOfItems.Add(_percentageOfHealingItems);

            VerifyValidityOfPercentages();
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