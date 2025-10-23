using System.Text.Json;
using NasaDataDashboard.Data.Objects;

namespace NasaDataDashboard.Data.Services
{
    public class NasaApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "DEMO_KEY"; // when changing to actual api key remember to not commit plain text key

        public NasaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AsteroidData>> GetNeoAsync()
        {
            string url = $"https://api.nasa.gov/neo/rest/v1/feed?start_date=2021-09-07&end_date=2021-09-07&api_key={ApiKey}";
            string json = await _httpClient.GetStringAsync(url);

            // Get the parser and parse to asteroid data obj
            var parser = new Parsers.ParseToObject();
            var parsed = parser.Parse(JsonDocument.Parse(json));
            return parsed;
        }
    }
}
