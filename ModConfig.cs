namespace RentedToolsRefresh
{
    public sealed class ModConfig
    {
        public bool ModEnabled { get; set; }

        public bool AllowRentBasicLevelTool { get; set; }
        public bool AllowRentCurrentLevelTool { get; set; }

        private int _rentalFee;
        public int RentalFee
                            {
                                get { return _rentalFee; }
                                set { _rentalFee = Math.Max(0, value); }
                            }
        public bool ApplyFeeToBasicLevel { get; set; }

        private int _dailyFee;
        public int DailyFee
                            {
                                get { return _dailyFee; }
                                set { _dailyFee = Math.Max(0, value); }
                            }

        // legacy options
        public bool? modEnabled { private get; set; }
        public int? toolRentalFee { private get; set; }

        public ModConfig()
        {
            ModEnabled = true;

            AllowRentBasicLevelTool = false;
            AllowRentCurrentLevelTool = true;
            
            RentalFee = 0;
            ApplyFeeToBasicLevel = true;

            DailyFee = 0;
        }

        public void ValidateConfigFile()
        {
            UpdateConfigToV110();

            if(RentalFee < 0)
                RentalFee = _rentalFee;

            if(DailyFee < 0)
                DailyFee = _dailyFee;
        }

        private void UpdateConfigToV110()
        {
            if(modEnabled.HasValue)
                ModEnabled = modEnabled.Value;

            if(toolRentalFee.HasValue)
                RentalFee = toolRentalFee.Value;
        }
    }
}