using System;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EvacAlert.Explore.Data;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;

namespace EvacAlert.Explore.Services
{
    public class StaticBlobStorageInformationService : IStaticLocationInformationService
    {
        private HttpClient _httpClient;
        private Options _options;

        public class Options
        {
            public string? FacilitiesCSVUrl { get; set; }
            public string? RegionsGeoJsonUrl { get; set; }
        }

        public StaticBlobStorageInformationService(HttpClient httpClient, Options options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<List<Facility>> GetFacilitiesAsync()
        {
            string csvData = await _httpClient.GetStringAsync(_options.FacilitiesCSVUrl);

            // Create CsvHelper configuration
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // CSV file contains a header row
            };

            // Read CSV data and map it to a list of Facility objects
            using (var csvReader = new CsvReader(new StringReader(csvData), csvConfig))
            {
                var facilities = csvReader.GetRecords<Facility>().ToList();
                return facilities!;
            }
        }

        public async Task<List<Region>> GetRegionsAsync()
        {
            List<Region> regions = new List<Region>();
            string geoJsonData = await _httpClient.GetStringAsync(_options.RegionsGeoJsonUrl);
            FeatureCollection geoJsonObject = JsonConvert.DeserializeObject<FeatureCollection>(geoJsonData);

            foreach (var feature in geoJsonObject.Features)
            {
                if (feature.Geometry.Type == GeoJSONObjectType.Polygon)
                {
                    //only supports polygons
                    Region region = new Region()
                    {
                        RegionName = feature.Properties.Where(x => x.Key == "Name").FirstOrDefault().Value.ToString(),

                    };
                    foreach (LineString line in ((Polygon)feature.Geometry).Coordinates)
                    {
                        region.BoundingAreas.Add(new BoundingArea()
                        {
                            Coordinates = line.Coordinates.Select(c =>
                            {
                                return new Coordinate()
                                {
                                    Latitude = c.Latitude,
                                    Longitude = c.Longitude
                                };
                            }).ToList()
                        });
                    }

                    regions.Add(region);
                }
            }

            return regions;
        }
    }
}

