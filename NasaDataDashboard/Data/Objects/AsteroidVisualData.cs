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
    }

}
