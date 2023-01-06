namespace StockSignalScanner.Models
{
    public class ZoneState
    {
        public ZoneState(bool isInZone, bool isAboutOutOfTheZone, bool isAboutEnterTheZone, decimal zoneHigh, decimal zoneLow)
        {
            IsInZone = isInZone;
            IsAboutOutOfTheZone = isAboutOutOfTheZone;
            IsAboutEnterTheZone = isAboutEnterTheZone;
            ZoneHigh = zoneHigh;
            ZoneLow = zoneLow;
        }

        public bool IsInZone { get; }
        public bool IsAboutOutOfTheZone { get; }
        public bool IsAboutEnterTheZone { get; }
        public decimal ZoneHigh { get; }
        public decimal ZoneLow { get; }
    }
}
