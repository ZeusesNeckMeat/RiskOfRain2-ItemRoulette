namespace ItemRoulette.Hooks
{
    internal class HookStateTracker
    {
        public bool IsBuildDropTableDone { get; set; } = false;
        public bool IsMonsterGenerateAvailableItemsSetDone { get; set; } = false;
        public bool IsItemCatalogInitDone { get; set; } = false;
        public bool IsPickupCatalogInitDone { get; set; } = false;
        
        public int CurrentLoopCount { get; set; } = 0;
    }
}