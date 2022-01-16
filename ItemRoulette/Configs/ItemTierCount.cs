using BepInEx.Configuration;

namespace ItemRoulette.Configs
{
    internal class ItemTierCount : ConfigBase
    {
        private ConfigEntry<int> _tier1ItemCount;
        private ConfigEntry<int> _tier2ItemCount;
        private ConfigEntry<int> _tier3ItemCount;
        private ConfigEntry<int> _bossItemCount;
        private ConfigEntry<int> _lunarItemCount;

        public ItemTierCount(ConfigFile configFile) : base(configFile) { }

        public override string SectionName => "New Total Item Counts";
        public override string SectionDescription => "This is number of items that will be available for {0}. The items in the run will be pulled at random from all unlocked items in this tier. Set to 0 to have all unlocked items in the run.";
        
        public int TotalItemCountForTiers123 => Tier1ItemCount + Tier2ItemCount + Tier3ItemCount;
        public int Tier1ItemCount => _tier1ItemCount.Value;
        public int Tier2ItemCount => _tier2ItemCount.Value;
        public int Tier3ItemCount => _tier3ItemCount.Value;
        public int BossItemCount => _bossItemCount.Value;
        public int LunarItemCount => _lunarItemCount.Value;

        public override void Initialize()
        {
            _tier1ItemCount = Bind("Tier1NewCount", 5, "Tier1 items");
            _tier2ItemCount = Bind("Tier2NewCount", 3, "Tier2 items");
            _tier3ItemCount = Bind("Tier3NewCount", 1, "Tier3 items");
            _bossItemCount = Bind("BossNewCount", 0, "Boss items");
            _lunarItemCount = Bind("LunarNewCount", 0, "Lunar items");
        }
    }
}