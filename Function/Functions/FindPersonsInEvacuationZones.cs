using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using EvacAlert.Data;
using System.Collections.Generic;
using GeoLibrary.Model;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace EvacAlert.Functions
{
    public class FindPersonsInEvacuationZones
    {
        private Services.IEvacuationService _evacService;
        public FindPersonsInEvacuationZones(Services.IEvacuationService evacService)
        {
            _evacService = evacService;
        }

        [FunctionName("FindPersonsInEvacuationZones")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestContent = "";
            if (req.ContentType != "application/json")
            {
                return new BadRequestObjectResult(new { ErrorMessage = "Only json is supported." });
            }

            using (StreamReader sr = new StreamReader(req.Body))
            {
                requestContent = await sr.ReadToEndAsync();
            }

            //should we filter the non-evac'd people out?
            bool outputAllEvacs = false; //false by default
            if (req.Query.TryGetValue("includeAll", out StringValues queryValue))
            {
                if (bool.TryParse(queryValue.FirstOrDefault() ?? "false", out bool parsedResult))
                {
                    outputAllEvacs = parsedResult;
                }
            }

            //first thing, load the evac areas. 

            List<GeocodedData> geocodedPoints = JsonSerializer.Deserialize<List<GeocodedData>>(requestContent, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            List<Evacuee> evacuees = new List<Evacuee>(); 

            var activeEvacZones = await _evacService.GetCurrentEvacuationAreasAsync();
            foreach (GeocodedData geocodedPoint in geocodedPoints)
            {
                if (geocodedPoint.Coordinate == null)
                    continue;

                Point point = new Point(geocodedPoint.Coordinate.Longitude, geocodedPoint.Coordinate.Latitude);
                EvacuationArea insideEvacArea = null; //not null if within.
                foreach (EvacuationArea evacArea in activeEvacZones)
                {
                    foreach (BoundingArea boundingArea in evacArea.BoundingAreas)
                    {
                        Polygon boundingAreaPolygon = new Polygon(boundingArea.Coordinates.Select(c => new Point(c.Longitude, c.Latitude)));
                        if (boundingAreaPolygon.IsPointInside(point))
                        {
                            insideEvacArea = evacArea;
                        }
                    }
                }

                if (insideEvacArea != null)
                {
                    evacuees.Add(new Evacuee()
                    {
                        Identifier = geocodedPoint.Identifier,
                        Group = geocodedPoint.Group,
                        Coordinate = geocodedPoint.Coordinate,
                        EvacAlertId = insideEvacArea.Id,
                        EvacAlertType = insideEvacArea.EventType,
                        EvacAlertName = insideEvacArea.Name,
                        EvacAlertOrderStatus = insideEvacArea.OrderStatus
                    });
                }
                else if (outputAllEvacs)
                {
                    //if not in an inside evac area AND we don't want to filter the non-evac'd people. 
                    evacuees.Add(new Evacuee()
                    {
                        Identifier = geocodedPoint.Identifier,
                        Group = geocodedPoint.Group,
                        Coordinate = geocodedPoint.Coordinate,
                        EvacAlertId = null,
                        EvacAlertType = null,
                        EvacAlertName = null,
                        EvacAlertOrderStatus = null
                    });
                }
            }

            return new OkObjectResult(evacuees);
        }
    }
}

