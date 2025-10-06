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

    }
}
