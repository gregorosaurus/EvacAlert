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
            log.LogInformation("C# HTTP trigger function processed a request.");

            List<Address> addresses;
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
                    addresses = csvReader.GetRecords<Address>().ToList();
                }
            }
            else if (req.ContentType == "application/json")
            {
                addresses = System.Text.Json.JsonSerializer.Deserialize<List<Address>>(reqContent);
            }
            else
            {
                return new BadRequestObjectResult(new { ErrorMessage = "Invalid content type." });
            }

            List<Task<GeocodedData>> geocodeTasks = new List<Task<GeocodedData>>();
            foreach (Address address in addresses)
            {
                //initiate the geocode, helps if we have a lot to encode. 
                geocodeTasks.Add(_geocodingService.GeocodeAddressAsync(address.Name, address.AddressQuery));
            }
            await Task.WhenAll(geocodeTasks);

            return new OkObjectResult(geocodeTasks.Select(x=>x.Result).ToList());

        }
    }
}

