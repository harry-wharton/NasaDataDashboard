using System.Globalization;
using System.Text.Json;
using NasaDataDashboard.Data.Objects;

namespace NasaDataDashboard.Data.Parsers
{
    public class ParseToObject
    {
        public List<AsteroidData> Parse(JsonDocument json)
        {
            var asteroidList = new List<AsteroidData>();

            if (!json.RootElement.TryGetProperty("near_earth_objects", out JsonElement nearEarthObjects))
                return asteroidList;

            foreach (JsonProperty dateProperty in nearEarthObjects.EnumerateObject())
            {
                foreach (JsonElement asteroid in dateProperty.Value.EnumerateArray())
                {
                    try
                    {
                        // Find Earth close approach (if any)
                        var closeApproach = asteroid
                            .GetProperty("close_approach_data")
                            .EnumerateArray()
                            .FirstOrDefault(ca =>
                                ca.GetProperty("orbiting_body").GetString() == "Earth");

                        if (closeApproach.ValueKind == JsonValueKind.Undefined)
                            continue;

                        var distanceKm = decimal.Parse(
                            closeApproach.GetProperty("miss_distance")
                                .GetProperty("kilometers")
                                .GetString() ?? "0",
                            CultureInfo.InvariantCulture);

                        var velocityKmh = decimal.Parse(
                            closeApproach.GetProperty("relative_velocity")
                                .GetProperty("kilometers_per_hour")
                                .GetString() ?? "0",
                            CultureInfo.InvariantCulture);

                        asteroidList.Add(new AsteroidData
                        {
                            Id = asteroid.GetProperty("id").GetString() ?? "Unknown",
                            Name = asteroid.GetProperty("name").GetString() ?? "Unknown",
                            IsHazardous = asteroid.GetProperty("is_potentially_hazardous_asteroid").GetBoolean(),

                            Diameter = asteroid
                                .GetProperty("estimated_diameter")
                                .GetProperty("meters")
                                .GetProperty("estimated_diameter_min")
                                .GetDecimal(),

                            DistanceFromEarth = distanceKm,
                            Velocity = velocityKmh
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing asteroid: {ex.Message}");
                    }
                }
            }

            return asteroidList;
        }
    }
}
