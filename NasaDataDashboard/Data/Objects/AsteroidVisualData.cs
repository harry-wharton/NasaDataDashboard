using System.Text.Json.Serialization;
using static NasaDataDashboard.Data.Objects.Vector3d;

namespace NasaDataDashboard.Data.Objects
{
    public class AsteroidVisualData
    {
        [JsonPropertyName("position")]
        public Vector3d Position { get; set; } = new Vector3d(); // From OrbitalCalculator

        [JsonPropertyName("size")]
        public decimal Size { get; set; } = 0.2m;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#ffffff";
        public List<Vector3d> OrbitPoints { get; set; } = new();

		[JsonPropertyName("name")]
		public string Name { get; set; } = "Unknown";

		[JsonPropertyName("diameter")]
		public decimal Diameter { get; set; } = 0; // in km

		[JsonPropertyName("distanceFromSun")]
		public decimal DistanceFromSun { get; set; } = 0; // in AU

		[JsonPropertyName("orbitalPeriod")]
		public decimal OrbitalPeriod { get; set; } = 0; // in years

		[JsonPropertyName("isPotentiallyHazardous")]
		public bool IsPotentiallyHazardous { get; set; } = false;


	}

}
