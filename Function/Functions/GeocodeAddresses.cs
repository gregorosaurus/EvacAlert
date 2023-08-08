using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CsvHelper;
using System.Globalization;
using EvacAlert.Data;
using System.Collections.Generic;
using System.Linq;

namespace EvacAlert.Functions
{
    public class GeocodeAddresses
    {
        private Services.IGeoCodingService _geocodingService;

        public GeocodeAddresses(Services.IGeoCodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        [FunctionName("GeocodeAddresses")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Geocoding addresses.");

            List<AddressData> addresses;
            string reqContent = "";
            using (StreamReader sr = new StreamReader(req.Body))
            {
                reqContent = await sr.ReadToEndAsync();
            }

            if (req.ContentType == "text/plain" || req.ContentType == "text/csv")
            {
                TextReader textReader = new StringReader(reqContent);
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    addresses = csvReader.GetRecords<AddressData>().ToList();
                }
            }
            else if (req.ContentType == "application/json")
            {
                addresses = System.Text.Json.JsonSerializer.Deserialize<List<AddressData>>(reqContent);
            }
            else
            {
                return new BadRequestObjectResult(new { ErrorMessage = "Invalid content type." });
            }

            List<GeocodedData> results = new List<GeocodedData>();

            await Parallel.ForEachAsync(addresses, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 10
            }, async (address, cancellationToken) =>
            {
                var geoCodedData = await _geocodingService.GeocodeAddressAsync(address.Identifier, address.Group, address.Address);
                lock (results)
                {
                    results.Add(geoCodedData);
                }
            });

            return new OkObjectResult(results);

        }
    }
}

