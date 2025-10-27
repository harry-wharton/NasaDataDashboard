using System.Text.Json;
using NasaDataDashboard.Data.Objects;

namespace NasaDataDashboard.Data.Parsers
{
    public class ParseToObject
    {
        public List<AsteroidData> Parse(JsonDocument json)
        {
            var asteroidList = new List<AsteroidData>();

            try
            {
                // Navigate through json and map to asteroid data obj
                if (json.RootElement.TryGetProperty("near_earth_objects", out JsonElement nearEarthObjects))
                {
                    // Iterate through each date in near_earth_objects
                    foreach (JsonProperty dateProperty in nearEarthObjects.EnumerateObject())
                    {
                        // Each date contains an array of asteroids
                        foreach (JsonElement asteroid in dateProperty.Value.EnumerateArray())
                        {
                            try
                            {
                                // Get distance as string first
                                var distanceString = asteroid
                                    .GetProperty("close_approach_data")[0]
                                    .GetProperty("miss_distance")
                                    .GetProperty("kilometers")
                                    .GetString();

                                var asteroidData = new AsteroidData
                                {
                                    // Get name and ID of the asteroid
                                    Id = asteroid.GetProperty("id").GetString() ?? "Unknown",
                                    Name = asteroid.GetProperty("name").GetString() ?? "Unknown",

                                    // Get is hazard bool
                                    IsHazardous = asteroid.GetProperty("is_potentially_hazardous_asteroid").GetBoolean(),

                                    // Get min diameter in meters
                                    Diameter = asteroid
                                        .GetProperty("estimated_diameter")
                                        .GetProperty("meters")
                                        .GetProperty("estimated_diameter_min")
                                        .GetDecimal(),

                                    // Get relative velocity in mph
                                    Velocity = decimal.Parse(asteroid
										.GetProperty("close_approach_data")[0]
										.GetProperty("relative_velocity")
										.GetProperty("miles_per_hour")
                                        .GetString()),

                                    // Parse distance string to decimal
                                    DistanceFromEarth = decimal.Parse(distanceString ?? "0")
                                };

                                asteroidList.Add(asteroidData);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing individual asteroid: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }

            return asteroidList;
        }
    }
}