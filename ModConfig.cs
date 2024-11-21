namespace RentedToolsImproved
{
    public sealed class ModConfig
    {
        public bool modEnabled { get; set; }

        public int toolRentalFee { get; set; }

        public ModConfig()
        {
            this.modEnabled = true;
            
            this.toolRentalFee = 0;
        }
    }
}