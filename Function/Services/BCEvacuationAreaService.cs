using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
        const string EvacUrlArcGIS = "https://services6.arcgis.com/ubm4tcTYICKBpist/ArcGIS/rest/services/Evacuation_Orders_and_Alerts/FeatureServer/0/query?where=1%3D1&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=standard&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=EMRG_OAA_SYSID%2C+EVENT_NAME%2C+EVENT_NUMBER%2C+EVENT_TYPE%2C+ORDER_ALERT_STATUS%2C+ISSUING_AGENCY%2C+PREOC_CODE%2C+POPULATION%2C+NUMBER_OF_HOMES%2C+DATE_MODIFIED%2C+FEATURE_AREA_SQM%2C+FEATURE_LENGTH_M&returnGeometry=true&returnCentroid=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=pgeojson&token=";

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
                    DateTime.TryParseExact(dateModifiedString.ToString().Replace("Z",""), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime modifiedDate))
                {
                    evacArea.DateModified = modifiedDate;
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
    }
}

