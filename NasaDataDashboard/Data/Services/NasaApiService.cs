using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using NasaDataDashboard.Data.Objects;

namespace NasaDataDashboard.Data.Services
{
    public class NasaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "neo_data";
        private readonly string _apiKey;

        public NasaApiService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _httpClient.BaseAddress = new Uri("https://api.nasa.gov/");
            _apiKey = configuration["NasaApi:ApiKey"] ?? "DEMO_KEY";
        }

        public async Task<List<AsteroidData>> GetNeoAsync()
        {
            var neoApiTimer = Stopwatch.StartNew();

            // Try to get from cache
            if (_cache.TryGetValue(CacheKey, out List<AsteroidData>? cachedData)) 
            {
                Console.WriteLine("Returning cached data");

                // Log time taken for cache retrieval
                neoApiTimer.Stop();
                Console.WriteLine($"Cached NEO data response time: {neoApiTimer.ElapsedMilliseconds}ms ({neoApiTimer.ElapsedTicks} ticks)");

                return cachedData!;
            }

            Console.WriteLine("Returning fresh data");

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-6).ToString("yyyy-MM-dd");
                var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                var response = await _httpClient.GetAsync(
                    $"neo/rest/v1/feed?start_date={startDate}&end_date={endDate}&api_key={_apiKey}"
                );

                if (response.IsSuccessStatusCode)
                {
                    // Log time taken for data retrieval
                    neoApiTimer.Stop();
                    Console.WriteLine($"NEO API response time: {neoApiTimer.ElapsedMilliseconds}ms");

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(jsonString);
                    var parser = new Parsers.ParseToObject();
                    var asteroidData = parser.Parse(jsonDocument);

                    // Cache data, 2 hours expiration
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(2))
                        .SetPriority(CacheItemPriority.High);

                    _cache.Set(CacheKey, asteroidData, cacheOptions);

                    Console.WriteLine($"Cached {asteroidData.Count} asteroids");
                    return asteroidData;
                }

                return new List<AsteroidData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching neo data: {ex.Message}");

				if (_cache.TryGetValue(CacheKey, out cachedData))
				{
					Console.WriteLine("exception, but here's the old cached data.");

                    // Log time taken for cache retrieval
                    neoApiTimer.Stop();
                    Console.WriteLine($"Cached NEO data response time: {neoApiTimer.ElapsedMilliseconds}ms ({neoApiTimer.ElapsedTicks} ticks)");

                    return cachedData!;
				}

                return new List<AsteroidData>();
			}
        }
    }
}
