using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ItemRoulette.Configs
{
    internal class GuaranteedItemsCombined : ConfigBase
    {
        private GuaranteedItems _guaranteedItems;
        private GuaranteedItemsShouldUse _guaranteedItemsShouldUse;
        private IDictionary<ItemTier, ReadOnlyCollection<PickupIndex>> _guaranteedItemsDictionary;
        private IDictionary<ItemTier, bool> _guaranteedItemsShouldUseDictionary;

        public GuaranteedItemsCombined(ConfigFile config, ManualLogSource logger) : base(config) 
        {
            _guaranteedItemsShouldUse = new GuaranteedItemsShouldUse();
            _guaranteedItems = new GuaranteedItems(logger);
        }

        public override string SectionName => "Guaranteed Items";

        public override void Initialize()
        {
            _guaranteedItemsShouldUse.Initialize(Bind);
            _guaranteedItemsShouldUseDictionary = _guaranteedItemsShouldUse.GetGuaranteedItemsShouldOnlyUseDictionary();

            _guaranteedItems.Initialize(Bind);
            _guaranteedItemsDictionary = _guaranteedItems.GetGuaranteedItemsDictionary();
        }

        public List<(ItemTier ItemTier, bool ShouldUseOnlyGuaranteedItems, ReadOnlyCollection<PickupIndex> GuaranteedItems)> GetGuaranteedItemSettings()
        {
            var guaranteedItemSettings = new List<(ItemTier, bool, ReadOnlyCollection<PickupIndex>)>();

            foreach (var guaranteedItemsShouldUse in _guaranteedItemsShouldUseDictionary)
            {
                _guaranteedItemsDictionary.TryGetValue(guaranteedItemsShouldUse.Key, out var guaranteedItems);

                guaranteedItemSettings.Add((guaranteedItemsShouldUse.Key, guaranteedItemsShouldUse.Value, guaranteedItems));
            }

            return guaranteedItemSettings;
        }
    }
}
