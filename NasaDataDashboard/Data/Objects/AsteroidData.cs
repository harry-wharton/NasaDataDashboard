namespace NasaDataDashboard.Data.Objects
{
    public class AsteroidData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Diameter { get; set; }
        public decimal DistanceFromEarth { get; set; }
        public bool IsHazardous { get; set; }
    }
}
