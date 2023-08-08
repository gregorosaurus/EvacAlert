using System;
using System.Text.Json;
using EvacAlert.Explore.Data;

namespace EvacAlert.Explore.Services
{
    public class EvacAzFunctionService : IEvacuationDataService
    {

        public class Options
        {
            public string? EvacAlertFunctionEndpoint { get; set; }
            public string? EvacAlertFunctionKey { get; set; }
        }

        private Options _options;

        private HttpClient _httpClient;
        public EvacAzFunctionService(HttpClient httpClient, Options options)
        {
            _httpClient = httpClient;
            _options = options
        }

        public async Task<List<EvacuationArea>> GetEvacuationAreasAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{_options.EvacAlertFunctionEndpoint}/api/CurrentEvacZones?code={_options.EvacAlertFunctionKey}");

            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<EvacuationArea>>(json, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<EvacuationArea>();
            }

            return new List<EvacuationArea>();
        }
    }
}

