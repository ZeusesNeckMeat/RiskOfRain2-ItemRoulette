using BepInEx.Configuration;

namespace ItemRoulette.Configs
{
    internal class General : ConfigBase
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<bool> _shouldSyncVoidItems;
        private ConfigEntry<ItemRefreshOptions> _itemRefreshOptions;

        public General(ConfigFile config) : base(config) { }

        public override string SectionName => "General";
        public bool IsModEnabled => _isModEnabled.Value;
        public bool ShouldSyncVoidItems => _shouldSyncVoidItems.Value;
        public ItemRefreshOptions ItemRefreshOptions => _itemRefreshOptions.Value;

        public override void Initialize()
        {
            _isModEnabled = Bind("IsEnabled", true, "Allows the enabling or disabling of the entire mod without needing to change every value back to default.");
            _shouldSyncVoidItems = Bind("ShouldSyncVoidItems", false, "When TRUE this will make it so the only void items available are ones paired with non-void items generated through this mod. If none of the randomly generated items for a given tier has a matching void item, then one void item will be selected at random in order to prevent weirdness with having no items for that void tier.");
            _itemRefreshOptions = Bind("ItemPoolRefreshOptions", ItemRefreshOptions.NewRun, "Determines if and when you want the list of items in the run to refresh.");
        }
    }

    public enum ItemRefreshOptions
    {
        NewRun,
        EachStage,
        EachLoop
    }
}