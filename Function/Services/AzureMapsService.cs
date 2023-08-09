using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EvacAlert.Data;
using GeoJSON.Text;
using GeoJSON.Text.Feature;
using GeoJSON.Text.Geometry;
using Microsoft.Extensions.Logging;

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
        private ILogger<AzureMapsService> _logger;

        public AzureMapsService(HttpClient httpClient, Options options, ILogger<AzureMapsService> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _logger = logger;
        }

        public async Task<List<GeocodedData>> GeocodeAddressAsync(IEnumerable<AddressData> addressData)
        {
            List<GeocodedData> geocodedResults = new List<GeocodedData>();

            List<AddressData> toGeoCode = new List<AddressData>(addressData);

            int batchCount = 1;
            while (toGeoCode.Count > 0)
            {
                _logger.LogInformation($"Running batch {batchCount}");
                int itemCount = Math.Min(toGeoCode.Count, 100);
                List<AddressData> batchGeoCodeData = toGeoCode.Take(itemCount).ToList();
                toGeoCode.RemoveRange(0, itemCount);

                geocodedResults.AddRange(await GeocodeAdddressBatchAsync(batchGeoCodeData));
                batchCount++;
            }

            return geocodedResults;
        }

        private async Task<List<GeocodedData>> GeocodeAdddressBatchAsync(List<AddressData> batchGeoCodeData)
        {
            string uri = $"https://atlas.microsoft.com/geocode:batch?api-version=2022-02-01-preview&subscription-key={_options.ApiKey}";

            List<GeocodedData> geocodedResults = new List<GeocodedData>();

            Data.Maps.GeoCodingBatchRequest geoCodingRequest = new Data.Maps.GeoCodingBatchRequest()
            {
                BatchItems = batchGeoCodeData.Select(x => new Data.Maps.MapAddressRequest()
                {
                    Address = x.Address
                }).ToList()
            };

            string jsonRequest = JsonSerializer.Serialize(geoCodingRequest);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Invalid response returned from azure maps: {response.StatusCode}");
            }

            string returnJson = await response.Content.ReadAsStringAsync();
            Data.Maps.GeoCodingBatchResponse geocodeResponse = JsonSerializer.Deserialize<Data.Maps.GeoCodingBatchResponse>(returnJson);
            for(int i=0;i<geocodeResponse.BatchItems.Count;i++)
            {
                AddressData geocodedAddress = batchGeoCodeData[i];
                
                    
                try
                {
                    FeatureCollection item = geocodeResponse.BatchItems[i];
                    //find the input data.
                    var firstFeature = item?.Features?.FirstOrDefault();
                    var point = (firstFeature.Geometry as Point)?.Coordinates;

                    if (geocodedAddress != null && point != null)
                    {
                        var geocodedData = new GeocodedData()
                        {
                            Identifier = geocodedAddress.Identifier,
                            Group = geocodedAddress.Group,
                            Coordinate = new Coordinate()
                            {
                                Latitude = point.Latitude,
                                Longitude = point.Longitude
                            }
                        };
                        geocodedResults.Add(geocodedData);
                    }else
                    {
                        var geocodedData = new GeocodedData()
                        {
                            Identifier = geocodedAddress.Identifier,
                            Group = geocodedAddress.Group,
                            Coordinate = null
                        };
                    }
                }
                catch(Exception e)
                {
                    _logger.LogError($"Exception occurred retrieving a geocoded address: {e.Message} {e.StackTrace}.");
                    var geocodedData = new GeocodedData()
                    {
                        Identifier = geocodedAddress.Identifier,
                        Group = geocodedAddress.Group,
                        Coordinate = null
                    };
                }
            }

            return geocodedResults;
        }
    }
}
