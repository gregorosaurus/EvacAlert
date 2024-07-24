using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EvacAlert.Data;
using GeoJSON.Text.Feature;
using GeoJSON.Text.Geometry;

namespace EvacAlert.Services
{
    public class BCEvacuationAreaService : IEvacuationService
    {
        private HttpClient _httpClient;

        /// <summary>
        /// The URL for the current evacuations.
        /// In GeoJson
        /// </summary>
        const string EvacUrlWMS = "https://openmaps.gov.bc.ca/geo/pub/wms?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&SRS=EPSG:4326&LAYERS=pub:WHSE_HUMAN_CULTURAL_ECONOMIC.EMRG_ORDER_AND_ALERT_AREAS_SP&STYLES=6885&FORMAT=application/json;type=geojson&TRANSPARENT=TRUE&maxFeatures=200&format_options=KMATTR:true;KMSCORE:25;MODE:refresh;SUPEROVERLAY:false&bbox=-179,-80,80,179&height=10000000&width=10000000";
        const string EvacUrlArcGIS = "https://services6.arcgis.com/ubm4tcTYICKBpist/ArcGIS/rest/services/Evacuation_Orders_and_Alerts/FeatureServer/0/query?where=1%3D1&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=standard&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=EMRG_OAA_SYSID%2C+EVENT_NAME%2C+EVENT_NUMBER%2C+EVENT_TYPE%2C+ORDER_ALERT_STATUS%2C+ISSUING_AGENCY%2C+PREOC_CODE%2C+DATE_MODIFIED%2C+FEATURE_AREA_SQM%2C+FEATURE_LENGTH_M%2C+MULTI_SOURCED_HOMES%2C+MULTI_SOURCED_POPULATION%2C+ORDER_ALERT_NAME%2C+OBJECTID&returnGeometry=true&returnCentroid=true&returnEnvelope=false&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnTrueCurves=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=geojson&token=";

        public BCEvacuationAreaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<EvacuationArea>> GetCurrentEvacuationAreasAsync()
        {
            string[] urlsToTry = new string[]
            {
                EvacUrlWMS,
                EvacUrlArcGIS
            };

            List<Exception> exceptionsOccurred = new List<Exception>();

            foreach (string url in urlsToTry)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Invalid response code from bc gov: {response.StatusCode}");
                    }

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    return GetCurrentEvacuationAreasFromGeoJson(jsonContent);
                }
                catch(Exception e)
                {
                    exceptionsOccurred.Add(e);
                }
            }

            //if we get here, nothing worked, throw the last exception
            throw exceptionsOccurred.Last();
        }

        private List<EvacuationArea> GetCurrentEvacuationAreasFromGeoJson(string jsonContent)
        {
            jsonContent = FixBadGeoJsonIds(jsonContent);
            List<EvacuationArea> evacAreas = new List<EvacuationArea>();

            FeatureCollection evacFeatureCollection = JsonSerializer.Deserialize<FeatureCollection>(jsonContent);

            foreach (Feature evacFeature in evacFeatureCollection.Features)
            {
                EvacuationArea evacArea = new EvacuationArea()
                {
                    Id = evacFeature.Id
                };
                if (evacFeature.Properties.TryGetValue("EVENT_NAME", out object eventName))
                {
                    evacArea.Name = eventName.ToString();
                }
                if (evacFeature.Properties.TryGetValue("EVENT_TYPE", out object eventType))
                {
                    evacArea.EventType = eventType.ToString();
                }
                if (evacFeature.Properties.TryGetValue("ORDER_ALERT_STATUS", out object orderStatus))
                {
                    evacArea.OrderStatus = orderStatus.ToString();
                }
                if (evacFeature.Properties.TryGetValue("ISSUING_AGENCY", out object issuingAgency))
                {
                    evacArea.IssuingAgency = issuingAgency.ToString();
                }
                if (evacFeature.Properties.TryGetValue("NUMBER_OF_HOMES", out object numHomes))
                {
                    evacArea.NumberOfHomesAffected = numHomes as int?;
                }
                if (evacFeature.Properties.TryGetValue("DATE_MODIFIED", out object dateModifiedString) &&
                    DateTime.TryParseExact(dateModifiedString.ToString().Replace("Z", ""), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime modifiedDate))
                {
                    evacArea.DateModified = modifiedDate;
                }
                else if (evacFeature.Properties.TryGetValue("DATE_MODIFIED", out object dateModifiedUnixTime))
                        
                {
                    //if it's a unix timestamp, then make the change
                    var stringValue =dateModifiedString.ToString();
                    if (Int64.TryParse(stringValue, out long epochMilliseconds))
                    {
                        DateTime parsedModifiedDate = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(epochMilliseconds);
                        evacArea.DateModified = parsedModifiedDate;
                    }
                }


                if (evacFeature.Geometry.Type == GeoJSON.Text.GeoJSONObjectType.Polygon)
                {
                    Polygon polygon = (Polygon)evacFeature.Geometry;
                    evacArea.BoundingAreas.Add(new BoundingArea()
                    {
                        Coordinates = polygon.Coordinates.First().Coordinates.Select(c => new Coordinate()
                        {
                            Latitude = c.Latitude,
                            Longitude = c.Longitude
                        }).ToList()
                    });
                }
                else if (evacFeature.Geometry.Type == GeoJSON.Text.GeoJSONObjectType.MultiPolygon)
                {
                    MultiPolygon multiPolygon = (MultiPolygon)evacFeature.Geometry;
                    foreach(Polygon polygon in multiPolygon.Coordinates)
                    {
                        evacArea.BoundingAreas.Add(new BoundingArea()
                        {
                            Coordinates = polygon.Coordinates.First().Coordinates.Select(c => new Coordinate()
                            {
                                Latitude = c.Latitude,
                                Longitude = c.Longitude
                            }).ToList()
                        });
                    }
                }else
                {
                    continue;
                }

                evacAreas.Add(evacArea);
            }

            return evacAreas;
        }


        /// <summary>
        /// some services are incorrectly sending id's as ints, instead of strings.  BAD.
        /// We fix. 
        /// </summary>
        /// <param name="jsonContent"></param>
        /// <returns></returns>
        private string FixBadGeoJsonIds(string jsonContent)
        {
            string pattern = @"""id"":\s*?([0-9]+)";//an id followed by numbers
            string replacement = @"""id"": ""$1"""; //soccurnd the value by quotes
            return Regex.Replace(jsonContent, pattern, replacement);
        }
    }
}

