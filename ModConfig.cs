namespace RentedToolsRefresh
{
    public sealed class ModConfig
    {
        public bool ModEnabled { get; set; }
        public bool AllowRentBasicLevelTool { get; set; }
        public bool AllowRentCurrentLevelTool { get; set; }

        public int RentalFee { get; set; }

        public bool? modEnabled { private get; set; }
        public int? toolRentalFee { private get; set; }

        public ModConfig()
        {
            ModEnabled = true;

            AllowRentBasicLevelTool = false;
            AllowRentCurrentLevelTool = true;
            
            RentalFee = 0;
        }

        public void UpdateConfigToV110()
        {
            if(modEnabled.HasValue)
                ModEnabled = modEnabled.Value;

            if(toolRentalFee.HasValue)
                RentalFee = toolRentalFee.Value;
        }
    }
}