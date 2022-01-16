using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ItemRoulette.Configs
{
    internal class ItemTagPercents : ConfigBase
    {
        private const double MIN_PERCENTAGE = 0;
        private const double MAX_ALLOWED_PERCENTAGE = 100;

        private ConfigEntry<double> _percentageOfDamageItems;
        private ConfigEntry<double> _percentageOfHealingItems;
        private ConfigEntry<double> _percentageOfUtilityItems;
        private readonly List<ConfigEntry<double>> _percentagesOfItems = new List<ConfigEntry<double>>();

        public ItemTagPercents(ConfigFile config) : base(config) { }

        public override string SectionName => "Item Tag Percents";
        public override string SectionDescription => "Determines what percent of total items available between Tiers 1, 2, and 3 will be of the {0} type. 0 means no items of that type";

        public double TotalPercentageOfItems => PercentageOfDamageItems + PercentageOfHealingItems + PercentageOfUtilityItems;
        public double PercentageOfDamageItems => _percentageOfDamageItems.Value;
        public double PercentageOfHealingItems => _percentageOfHealingItems.Value;
        public double PercentageOfUtilityItems => _percentageOfUtilityItems.Value;

        public override void Initialize()
        {
            _percentageOfDamageItems = Bind("DamageTypePercentage", 33d, "Damage");
            _percentageOfUtilityItems = Bind("UtilityTypePercentage", 33d, "Utility");
            _percentageOfHealingItems = Bind("HealingTypePercentage", 33d, "Healing");

            _percentagesOfItems.Clear();
            _percentagesOfItems.Add(_percentageOfDamageItems);
            _percentagesOfItems.Add(_percentageOfUtilityItems);
            _percentagesOfItems.Add(_percentageOfHealingItems);

            VerifyValidityOfPercentages();
        }

        private bool ArePercentagesValid()
        {
            return TotalPercentageOfItems > MIN_PERCENTAGE && TotalPercentageOfItems <= MAX_ALLOWED_PERCENTAGE;
        }

        private void VerifyValidityOfPercentages()
        {
            if (!ArePercentagesValid())
            {
                foreach (var percentageOfItems in _percentagesOfItems)
                    percentageOfItems.Value = 33;

                Reload();
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
                Reload();
        }

        private IEnumerable<ConfigEntry<double>> GetConfigPercentagesDefaulted()
        {
            return _percentagesOfItems.Where(x => x.Value < MIN_PERCENTAGE || x.Value > MAX_ALLOWED_PERCENTAGE);
        }
    }
}