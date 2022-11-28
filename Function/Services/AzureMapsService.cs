using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EvacAlert.Data;
using GeoJSON.Text;
using GeoJSON.Text.Feature;
using GeoJSON.Text.Geometry;

namespace EvacAlert.Services
{
    public class AzureMapsService : IGeoCodingService
    {
        private HttpClient _httpClient;

        public class Options
        {
            public string ApiKey { get; set; }
        }

        private Options _options;

        public AzureMapsService(HttpClient httpClient, Options options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<GeocodedData> GeocodeAddressAsync(string name, string address)
        {
            string uri = $"https://atlas.microsoft.com/geocode?api-version=2022-02-01-preview&query={address}&subscription-key={_options.ApiKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Invalid response returned from azure maps: {response.StatusCode}");
            }

            string returnJson = await response.Content.ReadAsStringAsync();

            FeatureCollection featureCollection = JsonSerializer.Deserialize<FeatureCollection>(returnJson);

            Feature firstFeature = featureCollection.Features.FirstOrDefault();

            if (firstFeature == null)
            {
                return new GeocodedData()
                {
                    Name = name,
                    Coordinate = null
                };
            }

            var point = (firstFeature.Geometry as Point)?.Coordinates;

            return new GeocodedData()
            {
                Name = name,
                Coordinate = new Coordinate()
                {
                    Longitude = point.Longitude,
                    Latitude = point.Latitude
                }
            };
        }
    }
}
