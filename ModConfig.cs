namespace RentedToolsRefresh
{
    public sealed class ModConfig
    {
        public bool modEnabled { get; set; }

        public int toolRentalFee { get; set; }

        public ModConfig()
        {
            modEnabled = true;
            
            toolRentalFee = 0;
        }
    }
}