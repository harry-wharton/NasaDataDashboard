using System.Text.Json;

namespace NasaDataDashboard.Data
{
    public class NasaApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "DEMO_KEY"; // when changing to actual api key remember to not commit plain text key

        public NasaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JsonDocument> GetNeoAsync()
        {
            string url = $"https://api.nasa.gov/neo/rest/v1/feed?start_date=2021-09-07&end_date=2021-09-07&api_key={ApiKey}";
            string json = await _httpClient.GetStringAsync(url);
            return JsonDocument.Parse(json);
        }
    }
}
