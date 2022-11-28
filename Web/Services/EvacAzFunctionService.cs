using System;
using System.Text.Json;
using EvacAlert.Explore.Data;

namespace EvacAlert.Explore.Services
{
    public class EvacAzFunctionService : IEvacuationDataService
    {
        private HttpClient _httpClient;
        public EvacAzFunctionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<EvacuationArea>> GetEvacuationAreasAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://fn-evacalert-dev-cc.azurewebsites.net/api/CurrentEvacZones?code=z4-PxpJOQ-TiEYivozDn_WnpIGtAdi5SS5t7iqUPNq7tAzFuqiswkA==");

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

