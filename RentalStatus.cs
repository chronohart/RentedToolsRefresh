namespace RentedToolsRefresh
{
    public sealed class RentalTracking
    {
        public bool HasRentedTool { get; set; }

        public int AccruedDebt { get; set; }

        public RentalTracking()
        {
            HasRentedTool = false;
            AccruedDebt = 0;
        }
    }
}