using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;

namespace ItemRoulette.Configs
{
    internal class GuaranteedItemsShouldUse
    {
        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItemsTier1;
        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItemsTier2;
        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItemsTier3;
        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItemsBoss;
        private ConfigEntry<bool> _shouldOnlyUseGuaranteedItemsLunar;

        private const string SECTION_KEY = "Should only use guaranteed items for {0}";
        private const string SECTION_DESCRIPTION = "Set to TRUE if you want to ONLY use the items for {0} and nothing else. Set to FALSE if you want the items for {0} to be added to the randomly generated items";
        
        public bool ShouldOnlyUseGuaranteedItemsTier1 => _shouldOnlyUseGuaranteedItemsTier1.Value;
        public bool ShouldOnlyUseGuaranteedItemsTier2 => _shouldOnlyUseGuaranteedItemsTier2.Value;
        public bool ShouldOnlyUseGuaranteedItemsTier3 => _shouldOnlyUseGuaranteedItemsTier3.Value;
        public bool ShouldOnlyUseGuaranteedItemsBoss => _shouldOnlyUseGuaranteedItemsBoss.Value;
        public bool ShouldOnlyUseGuaranteedItemsLunar => _shouldOnlyUseGuaranteedItemsLunar.Value;

        public void Initialize(Func<string, bool, string, ConfigEntry<bool>> bind)
        {
            _shouldOnlyUseGuaranteedItemsTier1 = bind(string.Format(SECTION_KEY, "Tier 1"), false, string.Format(SECTION_DESCRIPTION, "Tier 1"));
            _shouldOnlyUseGuaranteedItemsTier2 = bind(string.Format(SECTION_KEY, "Tier 2"), false, string.Format(SECTION_DESCRIPTION, "Tier 2"));
            _shouldOnlyUseGuaranteedItemsTier3 = bind(string.Format(SECTION_KEY, "Tier 3"), false, string.Format(SECTION_DESCRIPTION, "Tier 3"));
            _shouldOnlyUseGuaranteedItemsBoss = bind(string.Format(SECTION_KEY, "Boss"), false, string.Format(SECTION_DESCRIPTION, "Boss"));
            _shouldOnlyUseGuaranteedItemsLunar = bind(string.Format(SECTION_KEY, "Lunar"), false, string.Format(SECTION_DESCRIPTION, "Lunar"));
        }

        public IDictionary<ItemTier, bool> GetGuaranteedItemsShouldOnlyUseDictionary()
        {
            return new Dictionary<ItemTier, bool>
            {
                { ItemTier.Tier1, ShouldOnlyUseGuaranteedItemsTier1 },
                { ItemTier.Tier2, ShouldOnlyUseGuaranteedItemsTier2 },
                { ItemTier.Tier3, ShouldOnlyUseGuaranteedItemsTier3 },
                { ItemTier.Boss, ShouldOnlyUseGuaranteedItemsBoss },
                { ItemTier.Lunar, ShouldOnlyUseGuaranteedItemsLunar }
            };
        }
    }
}