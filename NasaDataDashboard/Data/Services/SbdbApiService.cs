using Microsoft.Extensions.Caching.Memory;
using NasaDataDashboard.Data.Objects;
using System.Text.Json;
using System.Web;

namespace NasaDataDashboard.Data.Services
{
	public class SbdbApiService
	{
		private readonly HttpClient _httpClient;
		private readonly IMemoryCache _cache;
		private const string SBDB_QUERY_API = "https://ssd-api.jpl.nasa.gov/sbdb_query.api";

		public SbdbApiService(HttpClient httpClient, IMemoryCache cache)
		{
			_httpClient = httpClient;
			_cache = cache;
		}

		// Query asteroids and their orbital elements
		public async Task<List<AsteroidWithOrbitalData>> GetNeoOrbitalDataAsync(
			int limit = 50,
			bool phaOnly = false, // pha = potentially hazardous asteroid
			double? maxDistance = null)
		{
			// Try to get the cached data
			var cacheKey = $"sbdb_neo_{limit}_{phaOnly}_{maxDistance}";
			if (_cache.TryGetValue(cacheKey, out List<AsteroidWithOrbitalData> cached))
				if (cached != null)
					return cached;

			try
			{
				var constraints = BuildConstraintFilters(phaOnly, maxDistance);

				// Create JSON constraint structure
				var constraintJson = JsonSerializer.Serialize(new
				{
					AND = constraints
				});

				// URL encode the constraint
				var encodedConstraint = HttpUtility.UrlEncode(constraintJson);

				// Fields we want returned
				var fields = string.Join(",", new[]
				{
					"spkid",      // SPK-ID
                    "full_name",  // Full name
                    "pdes",       // Primary designation
                    "name",       // IAU name
                    "neo",        // NEO flag
                    "pha",        // PHA flag
                    "class",      // Orbit class
                    "moid",       // Min Orbit Intersection Distance (AU)
                    "diameter",   // Diameter (km)
                    "H",          // Absolute magnitude

                    // Orbital elements
                    "e",          // Eccentricity
                    "a",          // Semi-major axis (AU)
                    "q",          // Perihelion distance (AU)
                    "i",          // Inclination (deg)
                    "om",         // Longitude of ascending node (deg)
                    "w",          // Argument of perihelion (deg)
                    "ma",         // Mean anomaly (deg)
                    "epoch",      // Epoch (JD)
                    "per"         // Orbital period (days)
                });

				// Build query URL with bits from above	
				var url = $"{SBDB_QUERY_API}?fields={fields}&sb-cdata={encodedConstraint}&sort=-moid&limit={limit}&full-prec=true";
				Console.WriteLine($"SBDB Query URL: {url}");

				var response = await _httpClient.GetFromJsonAsync<SbdbQueryResponse>(url);

				if (response?.Data == null || !response.Data.Any())
				{
					Console.WriteLine("No data returned from SBDB Query API");
					return new List<AsteroidWithOrbitalData>();
				}

				var asteroids = MapAsteroidData(response);

				// Cache for 2 hours (orbital data doesn't change frequently)
				_cache.Set(cacheKey, asteroids, TimeSpan.FromHours(2));

				Console.WriteLine($"Successfully fetched {asteroids.Count} asteroids from SBDB Query API");

				return asteroids;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error querying SBDB: {ex.Message}");
				return new List<AsteroidWithOrbitalData>();
			}
		}

		private List<AsteroidWithOrbitalData> MapAsteroidData(SbdbQueryResponse response)
		{
			var asteroids = new List<AsteroidWithOrbitalData>();

			foreach (var row in response.Data)
			{
				try
				{
					var asteroid = new AsteroidWithOrbitalData
					{
						SpkId = GetStringValue(row, 0),
						FullName = GetStringValue(row, 1),
						Designation = GetStringValue(row, 2),
						Name = GetStringValue(row, 3),
						IsNeo = GetStringValue(row, 4) == "Y",
						IsPha = GetStringValue(row, 5) == "Y",
						OrbitClass = GetStringValue(row, 6),
						Moid = GetDecimalValue(row, 7),
						DiameterKm = GetDecimalValue(row, 8),
						AbsoluteMagnitude = GetDecimalValue(row, 9),

						// Orbital elements
						Eccentricity = GetDecimalValue(row, 10),
						SemiMajorAxisAU = GetDecimalValue(row, 11),
						PerihelionDistanceAU = GetDecimalValue(row, 12),
						InclinationDeg = GetDecimalValue(row, 13),
						LongitudeAscNodeDeg = GetDecimalValue(row, 14),
						ArgumentPerihelionDeg = GetDecimalValue(row, 15),
						MeanAnomalyDeg = GetDecimalValue(row, 16),
						EpochJd = GetDecimalValue(row, 17),
						OrbitalPeriodDays = GetDecimalValue(row, 18)
					};

					// Calculate epoch datetime
					asteroid.OrbitalEpoch = JulianDateToDateTime(asteroid.EpochJd);

					// Calculate approximate distance from Earth (using MOID as proxy)
					asteroid.ApproximateDistanceKm = asteroid.Moid * 149597870.7m; // AU to km

					// Estimate velocity based on orbital period
					if (asteroid.OrbitalPeriodDays > 0)
					{
						var circumference = 2m * (decimal)Math.PI * asteroid.SemiMajorAxisAU * 149597870.7m; // Just 2pi * semi major axis in km
						asteroid.ApproximateVelocityKmH = circumference / asteroid.OrbitalPeriodDays / 24m;
					}

					asteroids.Add(asteroid);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error parsing asteroid row: {ex.Message}");
				}
			}

			return asteroids;
		}

		private List<string> BuildConstraintFilters(bool phaOnly, double? maxDistance)
		{
			// Build constraint filters
			var constraints = new List<string>
			{
				"neo|EQ|Y" // Only select Near Earth Objects
            };

			if (phaOnly)
			{
				constraints.Add("pha|EQ|Y"); // Only Potentially Hazardous Asteroids
			}

			if (maxDistance.HasValue)
			{
				constraints.Add($"moid|LT|{maxDistance.Value}"); // Minimum Orbit Intersection Distance with Earth
			}

			return constraints;
		}

		private string GetStringValue(List<object> row, int index)
		{
			if (row == null || index >= row.Count || row[index] == null)
				return "";

			var element = row[index];
			if (element is JsonElement jsonElement)
			{
				if (jsonElement.ValueKind == JsonValueKind.String)
					return jsonElement.GetString() ?? "";
				if (jsonElement.ValueKind == JsonValueKind.Number)
					return jsonElement.GetRawText();
				if (jsonElement.ValueKind == JsonValueKind.Null)
					return "";
			}

			return element.ToString() ?? "";
		}

		private decimal GetDecimalValue(List<object> row, int index)
		{
			var strValue = GetStringValue(row, index);
			if (string.IsNullOrWhiteSpace(strValue))
				return 0;

			return decimal.TryParse(strValue, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
		}

		private DateTime JulianDateToDateTime(decimal julianDate)
		{
			// Crazy magic from the internet
			var jd = (double)julianDate;
			var a = jd + 32044.0;
			var b = Math.Floor((4.0 * a + 3.0) / 146097.0);
			var c = a - Math.Floor((146097.0 * b) / 4.0);
			var d = Math.Floor((4.0 * c + 3.0) / 1461.0);
			var e = c - Math.Floor((1461.0 * d) / 4.0);
			var m = Math.Floor((5.0 * e + 2.0) / 153.0);

			var day = e - Math.Floor((153.0 * m + 2.0) / 5.0) + 1.0;
			var month = m + 3.0 - 12.0 * Math.Floor(m / 10.0);
			var year = 100.0 * b + d - 4800.0 + Math.Floor(m / 10.0);

			return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
