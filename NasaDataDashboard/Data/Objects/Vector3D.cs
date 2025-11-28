using System.Text.Json.Serialization;

namespace NasaDataDashboard.Data.Objects
{
    public class Vector3d
    {
        [JsonPropertyName("x")]
        public decimal X { get; set; }

        [JsonPropertyName("y")]
        public decimal Y { get; set; }

        [JsonPropertyName("z")]
        public decimal Z { get; set; }
    }
}
