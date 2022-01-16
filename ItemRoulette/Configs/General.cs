using BepInEx.Configuration;

namespace ItemRoulette.Configs
{
    internal class General : ConfigBase
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<ItemRefreshOptions> _itemRefreshOptions;

        public General(ConfigFile config) : base(config) { }

        public override string SectionName => "General";
        public bool IsModEnabled => _isModEnabled.Value;
        public ItemRefreshOptions ItemRefreshOptions => _itemRefreshOptions.Value;

        public override void Initialize()
        {
            _isModEnabled = Bind("IsEnabled", true, "Allows the enabling or disabling of the entire mod without needing to change every value back to default.");
            _itemRefreshOptions = Bind("ItemPoolRefreshOptions", ItemRefreshOptions.NewRun, "Determines if and when you want the list of items in the run to refresh");
        }
    }

    public enum ItemRefreshOptions
    {
        NewRun,
        EachStage,
        EachLoop
    }
}